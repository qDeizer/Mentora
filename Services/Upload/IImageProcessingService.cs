using Microsoft.AspNetCore.Http;

namespace PsikologProje_Void.Services.Upload
{
    public interface IImageProcessingService
    {
        Task<byte[]> NormalizeProfilePhotoAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
