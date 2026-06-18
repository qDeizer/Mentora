using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNotesMyNotesFilterViewModel
    {
        public List<string> SelectedDoctorIds { get; set; } = new();
        public List<ClinicalNoteVisibility> SelectedVisibilities { get; set; } = new();
        public string SortBy { get; set; } = "createdAt";
        public string SortDirection { get; set; } = "desc";
        public string? SearchTerm { get; set; }
    }
}
