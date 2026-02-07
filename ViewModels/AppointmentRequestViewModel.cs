using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentRequestViewModel
    {
        public int RequestId { get; set; }
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string? PatientProfilePhoto { get; set; }
        public string DoctorName { get; set; }
        public string? DoctorProfilePhoto { get; set; }
        public DateTime AppointmentStartTime { get; set; }
        public DateTime RequestDate { get; set; }
        public string? RequestMessage { get; set; }
        public string? ResponseMessage { get; set; }
        public RequestStatus Status { get; set; }
    }
}