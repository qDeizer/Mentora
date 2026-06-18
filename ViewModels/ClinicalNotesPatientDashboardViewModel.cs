using Microsoft.AspNetCore.Mvc.Rendering;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNotesPatientDashboardViewModel
    {
        public List<ClinicalNotePatientItemViewModel> Notes { get; set; } = new();
        public List<ClinicalNoteDoctorOptionViewModel> ShareDoctorProfiles { get; set; } = new();
        public ClinicalNotesMyNotesFilterViewModel Filter { get; set; } = new();
        public List<ClinicalNoteDoctorOptionViewModel> FilterDoctorProfiles { get; set; } = new();
        public List<ClinicalNoteDoctorOptionViewModel> AllDoctorProfiles { get; set; } = new();
        public List<SelectListItem> ShareDoctorOptions { get; set; } = new();
        public string? SelectedDoctorId { get; set; }
        public List<int> SelectedNoteIds { get; set; } = new();
        public int SelectedNoteCount => SelectedNoteIds?.Count ?? 0;
    }
}
