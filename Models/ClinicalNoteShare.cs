using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public class ClinicalNoteShare
    {
        public int Id { get; set; }

        [Required]
        public int ClinicalNoteId { get; set; }

        [ForeignKey("ClinicalNoteId")]
        public ClinicalNote ClinicalNote { get; set; } = default!;

        [Required]
        public string SharedByPatientId { get; set; } = default!;

        [ForeignKey("SharedByPatientId")]
        public Patient SharedByPatient { get; set; } = default!;

        [Required]
        public string SharedWithDoctorId { get; set; } = default!;

        [ForeignKey("SharedWithDoctorId")]
        public Doctor SharedWithDoctor { get; set; } = default!;

        public DateTime SharedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }

        public string? RevokedByPatientId { get; set; }

        [ForeignKey("RevokedByPatientId")]
        public Patient? RevokedByPatient { get; set; }
    }
}
