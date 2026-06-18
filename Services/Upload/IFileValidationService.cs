using Microsoft.AspNetCore.Http;

namespace PsikologProje_Void.Services.Upload
{
    public interface IFileValidationService
    {
        Task<FileValidationResult> ValidateAsync(IFormFile file, UploadCategory category, CancellationToken cancellationToken = default);
    }
}
