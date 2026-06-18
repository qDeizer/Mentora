namespace PsikologProje_Void.Services
{
    public class AppointmentAutomationRunnerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentAutomationRunnerService> _logger;

        public AppointmentAutomationRunnerService(IServiceScopeFactory scopeFactory, ILogger<AppointmentAutomationRunnerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment automation runner started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAppointmentAutomationService>();
                    await service.GenerateAppointmentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while generating automatic appointments.");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("Appointment automation runner stopped.");
        }
    }
}
