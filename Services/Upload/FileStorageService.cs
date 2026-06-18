using Microsoft.AspNetCore.Hosting;

namespace PsikologProje_Void.Services.Upload
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken cancellationToken = default)
        {
            await using var stream = new MemoryStream(content, writable: false);
            return await SaveAsync(folder, extension, stream, cancellationToken);
        }

        public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken cancellationToken = default)
        {
            var safeFolder = NormalizeFolder(folder);
            var normalizedExtension = NormalizeExtension(extension);
            var fileName = $"{Guid.NewGuid():N}{normalizedExtension}";

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", safeFolder);
            Directory.CreateDirectory(uploadsFolder);

            var fullPath = Path.Combine(uploadsFolder, fileName);
            await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                content.Position = 0;
                await content.CopyToAsync(output, cancellationToken);
            }

            return $"/images/{safeFolder}/{fileName}";
        }

        private static string NormalizeFolder(string folder)
        {
            var safeFolder = folder.Replace("\\", "/", StringComparison.Ordinal).Trim('/');
            if (safeFolder.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Geçersiz hedef klasör.");
            }

            return safeFolder;
        }

        private static string NormalizeExtension(string extension)
        {
            var value = extension.Trim().ToLowerInvariant();
            if (!value.StartsWith('.'))
            {
                value = "." + value;
            }

            if (value.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.'))
            {
                throw new InvalidOperationException("Geçersiz dosya uzantısı.");
            }

            return value;
        }
    }
}
