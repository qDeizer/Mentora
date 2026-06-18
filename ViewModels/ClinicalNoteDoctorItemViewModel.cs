namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNoteDoctorItemViewModel
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientProfilePhotoPath { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public DateTime PatientBirthDate { get; set; }
        public string AuthorDoctorName { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = string.Empty;
        public string? SharedByPatientName { get; set; }
        public DateTime? SharedAtUtc { get; set; }
        public string Content { get; set; } = string.Empty;
        public string PreviewContent { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool CanEdit { get; set; }
        public bool CanToggleLock { get; set; }
        public bool IsLockedForPatient { get; set; }
        public List<ClinicalNoteCommentViewModel> Comments { get; set; } = new();
    }
}
