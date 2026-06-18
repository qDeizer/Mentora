namespace PsikologProje_Void.Services.Email
{
    public class EmailQueueDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailQueueDispatcherService> _logger;

        public EmailQueueDispatcherService(IServiceScopeFactory scopeFactory, ILogger<EmailQueueDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email outbox dispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var outboxService = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var messages = await outboxService.ClaimBatchAsync(20, stoppingToken);
                    if (messages.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        try
                        {
                            await emailSender.SendAsync(new EmailMessage
                            {
                                To = message.To,
                                Subject = message.Subject,
                                HtmlBody = message.HtmlBody
                            }, stoppingToken);

                            await outboxService.MarkSentAsync(message.Id, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            await outboxService.MarkFailedAsync(message.Id, ex.Message, stoppingToken);
                            _logger.LogError(ex, "E-posta gönderim hatası. OutboxId={OutboxId}, Alıcı={Recipient}", message.Id, message.To);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "E-posta outbox döngüsünde hata oluştu.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Email outbox dispatcher stopped.");
        }
    }
}
