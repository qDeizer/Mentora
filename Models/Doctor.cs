using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.Models
{
    public class Doctor : User
    {
        [Required]
        [Display(Name = "Mesleðe Baþlangýç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime ExperienceStartDate { get; set; }

        [Required]
        [Display(Name = "Statü")]
        public DoctorTitle Title { get; set; }

        [Display(Name = "Üniversite")]
        [StringLength(100)]
        // Üniversite alaný da isteðe baðlý olabileceðinden nullable (string?) yapýldý.
        public string? University { get; set; }

        // Navigation properties
        public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
        public ICollection<DoctorCertificate> Certificates { get; set; } = new List<DoctorCertificate>();
    }

    public enum DoctorTitle
    {
        [Display(Name = "Psikolog")]
        Psychologist = 1,
        [Display(Name = "Klinik Psikolog")]
        ClinicalPsychologist = 2,
        [Display(Name = "Dr. Psikolog")]
        DrPsychologist = 3,
        [Display(Name = "Doç. Dr.")]
        AssociateProfessor = 4,
        [Display(Name = "Prof. Dr.")]
        Professor = 5
    }
}
