using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNoteCommentViewModel
    {
        public int Id { get; set; }
        public bool IsDoctorComment { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorProfilePhotoPath { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool IsLockedForPatient { get; set; }
        public bool CanToggleLock { get; set; }
        public bool VisibleToPatient { get; set; } = true;
    }

    public class ClinicalNoteAccessRuleCommandViewModel
    {
        public int NoteId { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public ClinicalNoteAccessRuleType RuleType { get; set; }
        public bool Enabled { get; set; }
    }

    public class ClinicalNoteBulkActionInputViewModel
    {
        public List<int> NoteIds { get; set; } = new();
        public ClinicalNoteVisibility? Visibility { get; set; }
        public List<string> AddShareDoctorIds { get; set; } = new();
        public List<string> RemoveShareDoctorIds { get; set; } = new();
        public List<string> AddBlockDoctorIds { get; set; } = new();
        public List<string> RemoveBlockDoctorIds { get; set; } = new();
    }
}
