using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PsikologProje_Void.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning("SMTP ayarları eksik olduğu için e-posta gönderimi atlandı. Subject: {Subject}", message.Subject);
                return;
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;
            mimeMessage.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;

            var sslOption = _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await smtp.ConnectAsync(_settings.Host, _settings.Port, sslOption, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await smtp.SendAsync(mimeMessage, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }
    }
}
