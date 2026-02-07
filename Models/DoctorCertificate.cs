using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.Models
{
    // Models/DoctorCertificate.cs
    public class DoctorCertificate
    {
        public int Id { get; set; }
        public string? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }
        [Required]
        public string? CertificateImagePath { get; set; }
        [StringLength(200)]
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
