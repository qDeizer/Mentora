using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public class ClinicalNoteLock
    {
        [Key]
        public int ClinicalNoteId { get; set; }

        [ForeignKey(nameof(ClinicalNoteId))]
        public ClinicalNote ClinicalNote { get; set; } = default!;

        [Required]
        public string LockedByDoctorId { get; set; } = default!;

        [ForeignKey(nameof(LockedByDoctorId))]
        public Doctor LockedByDoctor { get; set; } = default!;

        public bool IsLockedForPatient { get; set; } = true;

        public DateTime LockedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
