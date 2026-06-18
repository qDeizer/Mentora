using Microsoft.AspNetCore.Identity;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services.Email;

namespace PsikologProje_Void.Services.EmailVerification
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailOutboxService _emailOutboxService;

        public EmailVerificationService(UserManager<User> userManager, IEmailOutboxService emailOutboxService)
        {
            _userManager = userManager;
            _emailOutboxService = emailOutboxService;
        }

        public async Task SendVerificationCodeAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            // Generate a 6-digit TOTP code using the built-in "Email" provider
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            var emailMessage = new EmailMessage
            {
                To = user.Email,
                Subject = "Mentora - E-Posta Doğrulama Kodu",
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eaeaea; border-radius: 8px;'>
                        <h2 style='color: #4a90e2; text-align: center;'>Mentora Hesabinizi Dogrulayin</h2>
                        <p style='font-size: 16px; color: #333;'>Merhaba {user.FirstName},</p>
                        <p style='font-size: 16px; color: #333;'>Sisteme giris yapabilmek icin e-posta adresinizi dogrulamaniz gerekmektedir. Lutfen asagidaki 6 haneli dogrulama kodunu ekrana giriniz:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <span style='display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #333; background-color: #f5f5f5; padding: 15px 30px; border-radius: 8px;'>{code}</span>
                        </div>
                        <p style='font-size: 14px; color: #888;'>Bu kodun geçerlilik süresi sınırlıdır. Eğer bu işlemi siz yapmadıysanız, bu mesajı güvenle silebilirsiniz.</p>
                        <hr style='border: none; border-top: 1px solid #eaeaea; margin: 20px 0;' />
                        <p style='font-size: 12px; color: #aaa; text-align: center;'>&copy; {DateTime.Now.Year} Mentora. Tüm hakları saklıdır.</p>
                    </div>"
            };

            await _emailOutboxService.QueueAsync(emailMessage);
        }

        public async Task<bool> VerifyCodeAsync(User user, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            // Verify the 6-digit TOTP code
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
            
            if (isValid)
            {
                // Set email as confirmed
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            return isValid;
        }
    }
}
