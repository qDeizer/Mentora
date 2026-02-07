using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentRequestFilterModel
    {
        public int? AppointmentId { get; set; }
        public string? PatientId { get; set; }
        public string? DoctorId { get; set; }
        public RequestStatus? Status { get; set; }
        //public string? PatientName { get; set; } // BU SATIR KALDIRILDI
    }
}