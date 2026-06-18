using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public enum AppointmentStatus
    {
        Available,
        Reserved,
        Completed,
        NotCompleted,
        CancelledByConflict
    }

    public enum AppointmentOfferStatus
    {
        None = 0,
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Expired = 4
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = default!;

        public string? PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        public bool IsPrivateOffer { get; set; }

        public string? TargetPatientId { get; set; }

        [ForeignKey("TargetPatientId")]
        public Patient? TargetPatient { get; set; }

        public AppointmentOfferStatus OfferStatus { get; set; } = AppointmentOfferStatus.None;

        [StringLength(500)]
        public string? OfferNoteFromDoctor { get; set; }

        [StringLength(500)]
        public string? OfferResponseNoteFromPatient { get; set; }

        public DateTime? OfferRespondedAtUtc { get; set; }
        public DateTime? OfferExpiresAtUtc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public bool IsOnline { get; set; }
        public bool IsInPerson { get; set; }

        [StringLength(300)]
        public string? MeetingLink { get; set; }

        public string? Notes { get; set; }

        public Point? Location { get; set; }

        [StringLength(300)]
        public string? LocationNote { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;

        public int? Rating { get; set; }
        public string? Review { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? DoctorReminderSentAtUtc { get; set; }
        public DateTime? PatientReminderSentAtUtc { get; set; }
        [StringLength(200)]
        public string? CancelledReason { get; set; }
        public DateTime? CancelledAtUtc { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public ICollection<Appointment_Specialty> AppointmentSpecialties { get; set; } = new List<Appointment_Specialty>();
    }
}
