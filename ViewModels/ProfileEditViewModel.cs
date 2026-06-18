using Microsoft.AspNetCore.Http;
using PsikologProje_Void.Models;
using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.ViewModels
{
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "İsim zorunludur.")]
        [StringLength(50)]
        [Display(Name = "İsim")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Soyisim")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [Phone]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Doğum Tarihi")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Cinsiyet zorunludur.")]
        [Display(Name = "Cinsiyet")]
        public Gender Gender { get; set; }

        [StringLength(1000)]
        [Display(Name = "Hakkımda")]
        public string? About { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public IFormFile? ProfilePhoto { get; set; }

        [Display(Name = "Enlem")]
        public string? Latitude { get; set; }

        [Display(Name = "Boylam")]
        public string? Longitude { get; set; }

        [Display(Name = "Konumu Temizle")]
        public bool ClearLocation { get; set; }

        [Display(Name = "Mesleğe Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? ExperienceStartDate { get; set; }

        [Display(Name = "Statü")]
        public DoctorTitle? Title { get; set; }

        [StringLength(100)]
        [Display(Name = "Üniversite")]
        public string? University { get; set; }

        public string? ExistingProfilePhotoPath { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }

        public List<DoctorCertificateItem> Certificates { get; set; } = new();
        public int ProfileChangeCountInWindow { get; set; }
        public DateTime? ProfileChangeWindowStartUtc { get; set; }
        public DateTime? ProfileChangeBlockedUntilUtc { get; set; }
    }

    public class DoctorCertificateItem
    {
        public int Id { get; set; }
        public string CertificateImagePath { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
