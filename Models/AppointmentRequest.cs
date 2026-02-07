using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public class AppointmentRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }
        // YENİ: Navigasyon özelliği
        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }

        [Required]
        public string DoctorId { get; set; }
        // YENİ: Navigasyon özelliği
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; }

        [Required]
        public string PatientId { get; set; }
        // YENİ: Navigasyon özelliği
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; }

        public string? RequestMessage { get; set; }

        public string? ResponseMessage { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        // YENİ: Talebin ne zaman oluşturulduğunu takip etmek için
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}