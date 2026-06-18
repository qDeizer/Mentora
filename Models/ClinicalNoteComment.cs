using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public class ClinicalNoteComment
    {
        public int Id { get; set; }

        [Required]
        public int ClinicalNoteId { get; set; }

        [ForeignKey(nameof(ClinicalNoteId))]
        public ClinicalNote ClinicalNote { get; set; } = default!;

        public string? DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public Doctor? Doctor { get; set; }

        public string? PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = default!;

        public bool IsLockedForPatient { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
