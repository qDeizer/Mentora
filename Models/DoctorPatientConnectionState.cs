using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public class DoctorPatientConnectionState
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; } = default!;

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [ForeignKey(nameof(PatientId))]
        public Patient Patient { get; set; } = default!;

        public DateTime? DisconnectedAtUtc { get; set; }

        public string? DisconnectedByUserId { get; set; }

        [ForeignKey(nameof(DisconnectedByUserId))]
        public User? DisconnectedByUser { get; set; }

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
