using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public enum ClinicalNoteAccessRuleType
    {
        Share = 0,
        Block = 1
    }

    public class ClinicalNoteAccessRule
    {
        public int Id { get; set; }

        [Required]
        public int ClinicalNoteId { get; set; }

        [ForeignKey(nameof(ClinicalNoteId))]
        public ClinicalNote ClinicalNote { get; set; } = default!;

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey(nameof(DoctorId))]
        public Doctor Doctor { get; set; } = default!;

        [Required]
        public ClinicalNoteAccessRuleType RuleType { get; set; }

        [Required]
        public string CreatedByPatientId { get; set; } = default!;

        [ForeignKey(nameof(CreatedByPatientId))]
        public Patient CreatedByPatient { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }
    }
}
