using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNotePatientItemViewModel
    {
        public int Id { get; set; }
        public string AuthorDoctorId { get; set; } = string.Empty;
        public string AuthorDoctorName { get; set; } = string.Empty;
        public string? AuthorDoctorProfilePhotoPath { get; set; }
        public string Content { get; set; } = string.Empty;
        public string PreviewContent { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public ClinicalNoteVisibility Visibility { get; set; }
        public string VisibilityLabel { get; set; } = "Gizli";
        public bool IsPublic => Visibility == ClinicalNoteVisibility.Public;
        public bool IsPrivate => Visibility == ClinicalNoteVisibility.Private;
        public bool IsShared => Visibility == ClinicalNoteVisibility.Shared;
        public bool IsLockedForPatient { get; set; }
        public bool CanReadContent { get; set; }
        public bool CanComment { get; set; }
        public List<ClinicalNoteShareAuditItemViewModel> ShareAudit { get; set; } = new();
        public List<ClinicalNoteDoctorOptionViewModel> SharedDoctors { get; set; } = new();
        public List<ClinicalNoteDoctorOptionViewModel> BlockedDoctors { get; set; } = new();
        public List<ClinicalNoteCommentViewModel> Comments { get; set; } = new();
    }
}
