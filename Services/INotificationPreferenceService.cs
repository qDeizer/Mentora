using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public interface INotificationPreferenceService
    {
        Task<UserNotificationPreference> GetOrCreateAsync(string userId);
        Task UpdateAsync(string userId, NotificationSettingsViewModel model);
    }
}
