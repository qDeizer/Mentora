using PsikologProje_Void.Models;

namespace PsikologProje_Void.Services.EmailVerification
{
    public interface IEmailVerificationService
    {
        Task SendVerificationCodeAsync(User user);
        Task<bool> VerifyCodeAsync(User user, string code);
    }
}
