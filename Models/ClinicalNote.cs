using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public enum ClinicalNoteVisibility
    {
        [Display(Name = "Gizli")]
        Private = 0,
        [Display(Name = "Açık")]
        Public = 1,
        [Display(Name = "Paylaşılan")]
        Shared = 2
    }

    public class ClinicalNote
    {
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = default!;

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = default!;

        [Required]
        public string AuthorDoctorId { get; set; } = default!;

        [ForeignKey("AuthorDoctorId")]
        public Doctor AuthorDoctor { get; set; } = default!;

        public int? AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [Required]
        [StringLength(50000)]
        public string Content { get; set; } = default!;

        [Required]
        public ClinicalNoteVisibility Visibility { get; set; } = ClinicalNoteVisibility.Private;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ClinicalNoteShare> Shares { get; set; } = new List<ClinicalNoteShare>();
        public ClinicalNoteLock? Lock { get; set; }
        public ICollection<ClinicalNoteComment> Comments { get; set; } = new List<ClinicalNoteComment>();
        public ICollection<ClinicalNoteAccessRule> AccessRules { get; set; } = new List<ClinicalNoteAccessRule>();
    }
}
