namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNoteShareAuditItemViewModel
    {
        public string DoctorId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorProfilePhotoPath { get; set; }
        public DateTime SharedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public bool IsActive => !RevokedAtUtc.HasValue;
    }
}
