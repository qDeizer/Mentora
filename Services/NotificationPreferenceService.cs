using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly ApplicationDbContext _context;

        public NotificationPreferenceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserNotificationPreference> GetOrCreateAsync(string userId)
        {
            var preference = await _context.UserNotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (preference != null)
            {
                return preference;
            }

            preference = new UserNotificationPreference
            {
                UserId = userId,
                EmailEnabled = true,
                AppointmentReminderEnabled = true,
                RequestStatusEmailsEnabled = true,
                InAppEnabled = true,
                IncomingRequestInAppEnabled = true,
                PrivateOfferInAppEnabled = true,
                ClinicalNoteShareInAppEnabled = true,
                ClinicalNoteCommentInAppEnabled = true,
                DefaultClinicalNoteVisibility = ClinicalNoteVisibility.Private,
                ReminderMinutesBefore = 60,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.UserNotificationPreferences.Add(preference);
            await _context.SaveChangesAsync();
            return preference;
        }

        public async Task UpdateAsync(string userId, NotificationSettingsViewModel model)
        {
            var preference = await GetOrCreateAsync(userId);
            preference.EmailEnabled = model.EmailEnabled;
            preference.AppointmentReminderEnabled = model.AppointmentReminderEnabled;
            preference.RequestStatusEmailsEnabled = model.RequestStatusEmailsEnabled;
            preference.InAppEnabled = model.InAppEnabled;
            preference.IncomingRequestInAppEnabled = model.IncomingRequestInAppEnabled;
            preference.PrivateOfferInAppEnabled = model.PrivateOfferInAppEnabled;
            preference.ClinicalNoteShareInAppEnabled = model.ClinicalNoteShareInAppEnabled;
            preference.ClinicalNoteCommentInAppEnabled = model.ClinicalNoteCommentInAppEnabled;
            preference.DefaultClinicalNoteVisibility = model.DefaultClinicalNoteVisibility;
            preference.EmailDigestMode = model.EmailDigestMode;
            preference.TwoFactorViaEmailEnabled = model.TwoFactorViaEmailEnabled;
            preference.ReminderMinutesBefore = Math.Clamp(model.ReminderMinutesBefore, 10, 720);
            preference.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
