namespace PsikologProje_Void.Services.Upload
{
    public class UploadPolicyOptions
    {
        public const string SectionName = "UploadPolicy";

        public long ProfilePhotoMaxBytes { get; set; } = 2 * 1024 * 1024;
        public int ProfilePhotoSizePx { get; set; } = 512;
        public long CertificateMaxBytes { get; set; } = 5 * 1024 * 1024;
    }
}
