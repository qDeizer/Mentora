using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;

namespace PsikologProje_Void.Services.Email
{
    public class EmailOutboxService : IEmailOutboxService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailOutboxService> _logger;

        public EmailOutboxService(ApplicationDbContext context, ILogger<EmailOutboxService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => QueueAsync(message, DateTime.UtcNow, cancellationToken);

        public async Task QueueAsync(EmailMessage message, DateTime scheduledForUtc, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var nextAttempt = scheduledForUtc < utcNow ? utcNow : scheduledForUtc;

            var outbox = new EmailOutboxMessage
            {
                To = message.To.Trim(),
                Subject = message.Subject.Trim(),
                HtmlBody = message.HtmlBody,
                Status = EmailOutboxStatus.Pending,
                RetryCount = 0,
                MaxRetryCount = 5,
                NextAttemptAtUtc = nextAttempt,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow
            };

            _context.Set<EmailOutboxMessage>().Add(outbox);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<EmailOutboxMessage>> ClaimBatchAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var candidates = await _context.Set<EmailOutboxMessage>()
                .Where(m =>
                    (m.Status == EmailOutboxStatus.Pending || m.Status == EmailOutboxStatus.Failed) &&
                    m.NextAttemptAtUtc <= utcNow &&
                    m.RetryCount < m.MaxRetryCount)
                .OrderBy(m => m.NextAttemptAtUtc)
                .ThenBy(m => m.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return candidates;
            }

            foreach (var candidate in candidates)
            {
                candidate.Status = EmailOutboxStatus.Processing;
                candidate.ProcessingStartedAtUtc = utcNow;
                candidate.UpdatedAtUtc = utcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return candidates;
        }

        public async Task MarkSentAsync(long outboxId, CancellationToken cancellationToken = default)
        {
            var message = await _context.Set<EmailOutboxMessage>().FirstOrDefaultAsync(m => m.Id == outboxId, cancellationToken);
            if (message == null)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;
            message.Status = EmailOutboxStatus.Sent;
            message.SentAtUtc = utcNow;
            message.LastError = null;
            message.UpdatedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkFailedAsync(long outboxId, string error, CancellationToken cancellationToken = default)
        {
            var message = await _context.Set<EmailOutboxMessage>().FirstOrDefaultAsync(m => m.Id == outboxId, cancellationToken);
            if (message == null)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;
            message.RetryCount += 1;
            message.LastError = error.Length > 2000 ? error[..2000] : error;

            if (message.RetryCount >= message.MaxRetryCount)
            {
                message.Status = EmailOutboxStatus.Failed;
                message.NextAttemptAtUtc = DateTime.MaxValue;
                _logger.LogError("E-posta outbox mesajı kalıcı olarak başarısız oldu. Id={OutboxId}, Alıcı={To}", message.Id, message.To);
            }
            else
            {
                message.Status = EmailOutboxStatus.Failed;
                message.NextAttemptAtUtc = utcNow.Add(GetBackoff(message.RetryCount));
            }

            message.UpdatedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static TimeSpan GetBackoff(int retryCount)
        {
            var seconds = Math.Min(900, Math.Pow(2, retryCount) * 15);
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
