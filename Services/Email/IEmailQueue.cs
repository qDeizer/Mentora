namespace PsikologProje_Void.Services.Email
{
    public interface IEmailQueue
    {
        ValueTask QueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
        IAsyncEnumerable<EmailMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
