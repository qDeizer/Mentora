using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentRequestFilterModel
    {
        public int? AppointmentId { get; set; }
        public string? PatientId { get; set; }
        public string? DoctorId { get; set; }
        public List<string> SelectedPatientIds { get; set; } = new();
        public List<string> SelectedDoctorIds { get; set; } = new();
        public string? SelectedPatientId { get; set; }
        public string? SelectedDoctorId { get; set; }
        public List<RequestStatus> SelectedStatuses { get; set; } = new();
        public DateTime? RequestDateFrom { get; set; }
        public DateTime? RequestDateTo { get; set; }
        public DateTime? AppointmentDateFrom { get; set; }
        public DateTime? AppointmentDateTo { get; set; }
        public string SortBy { get; set; } = "requestDate";
        public string SortDirection { get; set; } = "desc";
    }
}
