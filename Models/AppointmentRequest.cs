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

        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; } = default!;

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = default!;

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = default!;

        public string? RequestMessage { get; set; }
        [StringLength(500)]
        public string? ReasonForVisit { get; set; }
        [StringLength(300)]
        public string? PreviousSupportInfo { get; set; }
        [StringLength(40)]
        public string? UrgencyLevel { get; set; }
        [StringLength(500)]
        public string? Expectations { get; set; }
        public string? ResponseMessage { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
