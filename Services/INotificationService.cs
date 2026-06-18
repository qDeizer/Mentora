using PsikologProje_Void.Models;

namespace PsikologProje_Void.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, NotificationType type, string title, string message, string? deepLink = null, CancellationToken cancellationToken = default);
        Task CreateManyAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? deepLink = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int limit = 12, CancellationToken cancellationToken = default);
        Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    }
}
