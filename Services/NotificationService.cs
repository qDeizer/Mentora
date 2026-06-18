using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;

namespace PsikologProje_Void.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string userId, NotificationType type, string title, string message, string? deepLink = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            if (!await ShouldCreateInAppNotificationAsync(userId, type, cancellationToken))
            {
                return;
            }

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title.Trim(),
                Message = message.Trim(),
                DeepLink = string.IsNullOrWhiteSpace(deepLink) ? null : deepLink.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateManyAsync(IEnumerable<string> userIds, NotificationType type, string title, string message, string? deepLink = null, CancellationToken cancellationToken = default)
        {
            var normalizedIds = userIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return;
            }

            var preferences = await _context.UserNotificationPreferences
                .Where(p => normalizedIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var userId in normalizedIds)
            {
                if (!ShouldCreateByPreference(preferences.GetValueOrDefault(userId), type))
                {
                    continue;
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title.Trim(),
                    Message = message.Trim(),
                    DeepLink = string.IsNullOrWhiteSpace(deepLink) ? null : deepLink.Trim(),
                    CreatedAtUtc = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int limit = 12, CancellationToken cancellationToken = default)
        {
            var safeLimit = Math.Clamp(limit, 1, 50);
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(safeLimit)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        }

        public async Task MarkAsReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

            if (notification == null || notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (unread.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var item in unread)
            {
                item.IsRead = true;
                item.ReadAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> ShouldCreateInAppNotificationAsync(string userId, NotificationType type, CancellationToken cancellationToken)
        {
            var preference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            return ShouldCreateByPreference(preference, type);
        }

        private static bool ShouldCreateByPreference(UserNotificationPreference? preference, NotificationType type)
        {
            if (preference == null)
            {
                return true;
            }

            if (!preference.InAppEnabled)
            {
                return false;
            }

            return type switch
            {
                NotificationType.IncomingRequest => preference.IncomingRequestInAppEnabled,
                NotificationType.PrivateOffer or NotificationType.PrivateOfferResponse => preference.PrivateOfferInAppEnabled,
                NotificationType.ClinicalNoteShared => preference.ClinicalNoteShareInAppEnabled,
                NotificationType.ClinicalNoteComment => preference.ClinicalNoteCommentInAppEnabled,
                NotificationType.AppointmentReminder => preference.AppointmentReminderEnabled,
                _ => true
            };
        }
    }
}
