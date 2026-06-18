using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.ViewModels
{
    public class ClinicalNoteCreateViewModel
    {
        [Required]
        public string PatientId { get; set; } = string.Empty;

        public int? AppointmentId { get; set; }

        [Required(ErrorMessage = "Not icerigi zorunludur.")]
        [StringLength(50000)]
        public string Content { get; set; } = string.Empty;

        public bool IsLockedForPatient { get; set; } = false;
    }
}
