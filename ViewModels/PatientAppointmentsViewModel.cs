using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class PatientAppointmentsViewModel
    {
        public List<PatientAppointmentListItemViewModel> UpcomingAppointments { get; set; } = new();
        public List<PatientAppointmentListItemViewModel> PastAppointments { get; set; } = new();
    }

    public class PatientAppointmentListItemViewModel
    {
        public int AppointmentId { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorProfilePhotoPath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TypeText { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public string PriceRange { get; set; } = "-";
        public string? MeetingLink { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationNote { get; set; }
        public string? Notes { get; set; }
        public int? Rating { get; set; }
        public string? Review { get; set; }
        public bool CanRate { get; set; }
    }
}
