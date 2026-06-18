using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace PsikologProje_Void.Services.Upload
{
    public class FileValidationService : IFileValidationService
    {
        private static readonly HashSet<string> ProfileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly HashSet<string> CertificateExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".pdf"
        };

        private readonly UploadPolicyOptions _options;

        public FileValidationService(IOptions<UploadPolicyOptions> options)
        {
            _options = options.Value;
        }

        public async Task<FileValidationResult> ValidateAsync(IFormFile file, UploadCategory category, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return Invalid("Dosya boş olamaz.");
            }

            var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Invalid("Dosya uzantisi bulunamadi.");
            }

            var maxBytes = category == UploadCategory.ProfilePhoto ? _options.ProfilePhotoMaxBytes : _options.CertificateMaxBytes;
            if (file.Length > maxBytes)
            {
                var mb = Math.Round(maxBytes / 1024d / 1024d, 1);
                return Invalid($"Dosya boyutu limiti asildi. En fazla {mb} MB olabilir.");
            }

            var allowedExtensions = category == UploadCategory.ProfilePhoto ? ProfileExtensions : CertificateExtensions;
            if (!allowedExtensions.Contains(extension))
            {
                return Invalid("Dosya turu desteklenmiyor.");
            }

            var signatureResult = await ValidateSignatureAsync(file, extension, category, cancellationToken);
            if (!signatureResult.IsValid)
            {
                return signatureResult;
            }

            return signatureResult;
        }

        private static FileValidationResult Invalid(string error) => new()
        {
            IsValid = false,
            ErrorMessage = error
        };

        private static async Task<FileValidationResult> ValidateSignatureAsync(IFormFile file, string extension, UploadCategory category, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream();
            var header = new byte[16];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read <= 0)
            {
                return Invalid("Dosya okunamadi.");
            }

            var actualType = DetectFileType(header, read);
            if (actualType == null)
            {
                return Invalid("Dosya imzasi gecersiz.");
            }

            var normalizedExtension = actualType switch
            {
                "jpg" => ".jpg",
                "png" => ".png",
                "webp" => ".webp",
                "pdf" => ".pdf",
                _ => string.Empty
            };

            if (category == UploadCategory.ProfilePhoto && actualType == "pdf")
            {
                return Invalid("Profil fotografi dosyasi yalnizca gorsel olabilir.");
            }

            if (!string.Equals(extension, normalizedExtension, StringComparison.OrdinalIgnoreCase))
            {
                if (!(string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) && string.Equals(normalizedExtension, ".jpg", StringComparison.OrdinalIgnoreCase)))
                {
                    return Invalid("Dosya uzantisi ile dosya icerigi uyusmuyor.");
                }
            }

            return new FileValidationResult
            {
                IsValid = true,
                NormalizedExtension = normalizedExtension,
                IsImage = actualType is "jpg" or "png" or "webp"
            };
        }

        private static string? DetectFileType(byte[] header, int read)
        {
            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return "jpg";
            }

            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return "png";
            }

            if (read >= 12)
            {
                var riff = Encoding.ASCII.GetString(header, 0, 4);
                var webp = Encoding.ASCII.GetString(header, 8, 4);
                if (riff == "RIFF" && webp == "WEBP")
                {
                    return "webp";
                }
            }

            if (read >= 5)
            {
                var pdf = Encoding.ASCII.GetString(header, 0, 5);
                if (pdf == "%PDF-")
                {
                    return "pdf";
                }
            }

            return null;
        }
    }
}
