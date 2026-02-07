using PsikologProje_Void.Services;

namespace PsikologProje_Void.Services
{
    public class AppointmentStatusUpdaterService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentStatusUpdaterService> _logger;

        public AppointmentStatusUpdaterService(IServiceScopeFactory scopeFactory, ILogger<AppointmentStatusUpdaterService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Status Updater Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
                        await appointmentService.UpdateExpiredAppointmentsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during the scheduled appointment status update.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Appointment Status Updater Service is stopping.");
        }
    }
}