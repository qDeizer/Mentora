namespace PsikologProje_Void.Services.Upload
{
    public interface IFileStorageService
    {
        Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken cancellationToken = default);
        Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken cancellationToken = default);
    }
}
