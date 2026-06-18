using Microsoft.Extensions.Options;

namespace PsikologProje_Void.Services.Email
{
    public class SmtpConfigurationValidator : ISmtpConfigurationValidator
    {
        private readonly SmtpSettings _settings;

        public SmtpConfigurationValidator(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                return (false, "SMTP host ayarı boş.");
            }

            if (_settings.Port <= 0 || _settings.Port > 65535)
            {
                return (false, "SMTP port ayari gecersiz.");
            }

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                return (false, "SMTP gönderici e-posta ayarı boş.");
            }

            if (_settings.Host.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
                _settings.FromEmail.Contains("mentora.local", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "SMTP ayarları örnek değerler içeriyor. Gerçek değerlerle güncelleyin.");
            }

            return (true, null);
        }
    }
}
