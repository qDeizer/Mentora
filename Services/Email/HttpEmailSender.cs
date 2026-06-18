using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PsikologProje_Void.Services.Email
{
    public class HttpEmailSender : IEmailSender
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly SmtpSettings _settings;
        private readonly ILogger<HttpEmailSender> _logger;
        private readonly string _apiProvider;

        public HttpEmailSender(IOptions<SmtpSettings> options, ILogger<HttpEmailSender> logger, IConfiguration configuration)
        {
            _settings = options.Value;
            _logger = logger;
            _apiProvider = (configuration["Smtp:ApiProvider"] ?? "brevo").ToLowerInvariant();
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning("Email ayarları eksik, gönderim atlandı. Subject: {Subject}", message.Subject);
                return;
            }

            switch (_apiProvider)
            {
                case "brevo":
                    await SendViaBrevoAsync(message, cancellationToken);
                    break;
                case "sendgrid":
                    await SendViaSendGridAsync(message, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Bilinmeyen API sağlayıcısı: {_apiProvider}. Desteklenenler: brevo, sendgrid");
            }
        }

        private async Task SendViaBrevoAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            var payload = new
            {
                sender = new { email = _settings.FromEmail, name = _settings.FromName },
                to = new[] { new { email = message.To } },
                subject = message.Subject,
                htmlContent = message.HtmlBody
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", _settings.Password);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Brevo API hatası: {response.StatusCode} - {body}");
            }

            _logger.LogInformation("E-posta Brevo API üzerinden gönderildi. Alıcı: {Recipient}, Konu: {Subject}", message.To, message.Subject);
        }

        private async Task SendViaSendGridAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = message.To } },
                        subject = message.Subject
                    }
                },
                from = new { email = _settings.FromEmail, name = _settings.FromName },
                content = new[]
                {
                    new { type = "text/html", value = message.HtmlBody }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_settings.Password}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"SendGrid API hatası: {response.StatusCode} - {body}");
            }

            _logger.LogInformation("E-posta SendGrid API üzerinden gönderildi. Alıcı: {Recipient}, Konu: {Subject}", message.To, message.Subject);
        }
    }
}
