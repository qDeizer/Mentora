using Microsoft.AspNetCore.Mvc.Rendering;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNotesDoctorDashboardViewModel
    {
        public List<ClinicalNoteDoctorItemViewModel> Notes { get; set; } = new();
        public ClinicalNoteCreateViewModel CreateForm { get; set; } = new();
        public List<SelectListItem> PatientOptions { get; set; } = new();
        public List<ClinicalNotePatientOptionViewModel> PatientProfiles { get; set; } = new();
        public string? FilterPatientId { get; set; }
        public List<string> FilterPatientIds { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortDirection { get; set; } = "desc";
    }
}
