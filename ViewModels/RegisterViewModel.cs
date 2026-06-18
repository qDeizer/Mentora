using Microsoft.AspNetCore.Http;
using PsikologProje_Void.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace PsikologProje_Void.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Kullanıcı Tipi")]
        public UserType UserType { get; set; }

        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [Display(Name = "İsim")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı Adı")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3 ile 32 karakter arasında olmalıdır.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam, nokta, alt çizgi ve tire içerebilir.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soy isim alanı zorunludur.")]
        [Display(Name = "Soy İsim")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon alanı zorunludur.")]
        [Phone]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Doğum Tarihi")]
        public DateTime BirthDate { get; set; } = new DateTime(2009, 1, 1);

        [Required(ErrorMessage = "Cinsiyet seçimi zorunludur.")]
        [Display(Name = "Cinsiyet")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter olmalıdır.", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrar")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Profil Fotoğrafı")]
        public IFormFile? ProfilePhoto { get; set; }

        // DÜZELTME: Kültür sorununu çözmek için tipler string'e çevrildi.
        // Model binder artık sayısal dönüşüm yapmayacak, bu işlemi controller'da biz yapacağız.
        [Display(Name = "Enlem")]
        public string? Latitude { get; set; }

        [Display(Name = "Boylam")]
        public string? Longitude { get; set; }

        [Display(Name = "Hakkımda")]
        [StringLength(1000)]
        public string? About { get; set; } = string.Empty;

        // --- Doktorlara özel alanlar ---
        [Display(Name = "Mesleğe Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? ExperienceStartDate { get; set; }

        [Display(Name = "Statü")]
        public DoctorTitle? Title { get; set; }

        [Display(Name = "Üniversite")]
        [StringLength(100)]
        public string? University { get; set; } = string.Empty;

        [Display(Name = "Uzmanlık Alanları")]
        public List<int>? SelectedSpecialtyIds { get; set; }

        [Display(Name = "Sertifika/Diploma")]
        public List<IFormFile>? Certificates { get; set; }
    }
}
