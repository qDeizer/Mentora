using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationPreferenceService _notificationPreferenceService;
        private readonly IEmailOutboxService _emailOutboxService;
        private readonly ISmtpConfigurationValidator _smtpConfigurationValidator;

        public SettingsController(
            UserManager<User> userManager,
            INotificationPreferenceService notificationPreferenceService,
            IEmailOutboxService emailOutboxService,
            ISmtpConfigurationValidator smtpConfigurationValidator)
        {
            _userManager = userManager;
            _notificationPreferenceService = notificationPreferenceService;
            _emailOutboxService = emailOutboxService;
            _smtpConfigurationValidator = smtpConfigurationValidator;
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var preference = await _notificationPreferenceService.GetOrCreateAsync(userId);
            var model = new NotificationSettingsViewModel
            {
                EmailEnabled = preference.EmailEnabled,
                AppointmentReminderEnabled = preference.AppointmentReminderEnabled,
                RequestStatusEmailsEnabled = preference.RequestStatusEmailsEnabled,
                InAppEnabled = preference.InAppEnabled,
                IncomingRequestInAppEnabled = preference.IncomingRequestInAppEnabled,
                PrivateOfferInAppEnabled = preference.PrivateOfferInAppEnabled,
                ClinicalNoteShareInAppEnabled = preference.ClinicalNoteShareInAppEnabled,
                ClinicalNoteCommentInAppEnabled = preference.ClinicalNoteCommentInAppEnabled,
                DefaultClinicalNoteVisibility = preference.DefaultClinicalNoteVisibility,
                EmailDigestMode = preference.EmailDigestMode,
                TwoFactorViaEmailEnabled = preference.TwoFactorViaEmailEnabled,
                ReminderMinutesBefore = preference.ReminderMinutesBefore
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Notifications(NotificationSettingsViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Bildirim ayarları geçersiz.";
                return View(model);
            }

            await _notificationPreferenceService.UpdateAsync(userId, model);
            TempData["SuccessMessage"] = "Bildirim ayarlarınız güncellendi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost("/Settings/Notifications/TestEmail")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> TestEmail(string? testRecipient)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var (isValid, error) = _smtpConfigurationValidator.Validate();
            if (!isValid)
            {
                TempData["ErrorMessage"] = $"SMTP ayarları geçersiz: {error}";
                return RedirectToAction(nameof(Notifications));
            }

            var recipient = string.IsNullOrWhiteSpace(testRecipient) ? user.Email : testRecipient.Trim();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                TempData["ErrorMessage"] = "Test e-postası için hedef adres gerekli.";
                return RedirectToAction(nameof(Notifications));
            }

            try
            {
                _ = new System.Net.Mail.MailAddress(recipient);
            }
            catch
            {
                TempData["ErrorMessage"] = "Geçersiz e-posta adresi.";
                return RedirectToAction(nameof(Notifications));
            }

            await _emailOutboxService.QueueAsync(new EmailMessage
            {
                To = recipient,
                Subject = "Mentora - SMTP test e-postası",
                HtmlBody = "<p>SMTP altyapısı başarıyla çalışıyor.</p>"
            });

            TempData["SuccessMessage"] = $"Test e-postası kuyruğa eklendi: {recipient}";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Appearance(string? themePreference, string? layoutDensity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var normalizedTheme = NormalizeTheme(themePreference);
            var normalizedDensity = NormalizeDensity(layoutDensity);

            user.ThemePreference = normalizedTheme;
            user.LayoutDensity = normalizedDensity;
            await _userManager.UpdateAsync(user);

            return Json(new
            {
                ok = true,
                themePreference = normalizedTheme,
                layoutDensity = normalizedDensity
            });
        }

        private static string NormalizeTheme(string? value)
        {
            return value switch
            {
                "light" => "light",
                "dark" => "dark",
                _ => "system"
            };
        }

        private static string NormalizeDensity(string? value)
        {
            return value == "compact" ? "compact" : "comfortable";
        }
    }
}
