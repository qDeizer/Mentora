using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta veya kullanıcı adı gereklidir")]
        [Display(Name = "E-posta veya Kullanıcı Adı")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
