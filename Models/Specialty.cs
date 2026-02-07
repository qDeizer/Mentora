using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.Models
{
    public class Specialty
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }
        public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    }
}
