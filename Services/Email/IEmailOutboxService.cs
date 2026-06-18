using PsikologProje_Void.Models;

namespace PsikologProje_Void.Services.Email
{
    public interface IEmailOutboxService
    {
        Task QueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
        Task QueueAsync(EmailMessage message, DateTime scheduledForUtc, CancellationToken cancellationToken = default);
        Task<List<EmailOutboxMessage>> ClaimBatchAsync(int batchSize, CancellationToken cancellationToken = default);
        Task MarkSentAsync(long outboxId, CancellationToken cancellationToken = default);
        Task MarkFailedAsync(long outboxId, string error, CancellationToken cancellationToken = default);
    }
}
