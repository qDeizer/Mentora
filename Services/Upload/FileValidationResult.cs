namespace PsikologProje_Void.Services.Upload
{
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string NormalizedExtension { get; set; } = string.Empty;
        public bool IsImage { get; set; }
    }
}
