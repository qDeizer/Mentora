using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.Utils;

namespace PsikologProje_Void.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(IServiceScopeFactory scopeFactory, ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    await ProcessRemindersAsync(context, outbox, notificationService, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Randevu hatırlatma servisi çalışırken hata oluştu.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private static async Task ProcessRemindersAsync(
            ApplicationDbContext context,
            IEmailOutboxService outbox,
            INotificationService notificationService,
            CancellationToken cancellationToken)
        {
            var turkeyNow = TimeZoneHelper.GetTurkeyNow();
            var searchEnd = turkeyNow.AddHours(24);

            var appointments = await context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.Status == Models.AppointmentStatus.Reserved && a.StartTime > turkeyNow && a.StartTime <= searchEnd)
                .ToListAsync(cancellationToken);

            if (appointments.Count == 0)
            {
                return;
            }

            var userIds = appointments
                .SelectMany(a => new[] { a.DoctorId, a.PatientId })
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .Distinct()
                .ToList();

            var preferences = await context.UserNotificationPreferences
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, cancellationToken);

            var utcNow = DateTime.UtcNow;
            var hasChanges = false;

            foreach (var appointment in appointments)
            {
                if (appointment.Patient == null)
                {
                    continue;
                }

                preferences.TryGetValue(appointment.DoctorId, out var doctorPreference);
                if (ShouldSendReminder(appointment.DoctorReminderSentAtUtc, doctorPreference, appointment.StartTime, turkeyNow))
                {
                    if (!string.IsNullOrWhiteSpace(appointment.Doctor.Email) && (doctorPreference?.EmailEnabled ?? true))
                    {
                        await outbox.QueueAsync(new EmailMessage
                        {
                            To = appointment.Doctor.Email!,
                            Subject = "Mentora - Yaklasan randevu",
                            HtmlBody = $"<p><strong>{appointment.StartTime:dd.MM.yyyy HH:mm}</strong> saatinde hasta randevunuz bulunuyor.</p>"
                        }, cancellationToken);
                    }

                    await notificationService.CreateAsync(
                        appointment.DoctorId,
                        NotificationType.AppointmentReminder,
                        "Yaklasan randevu",
                        $"{appointment.StartTime:dd.MM.yyyy HH:mm} saatinde randevunuz var.",
                        "/DoctorDashboard");

                    appointment.DoctorReminderSentAtUtc = utcNow;
                    hasChanges = true;
                }

                preferences.TryGetValue(appointment.PatientId!, out var patientPreference);
                if (ShouldSendReminder(appointment.PatientReminderSentAtUtc, patientPreference, appointment.StartTime, turkeyNow))
                {
                    if (!string.IsNullOrWhiteSpace(appointment.Patient.Email) && (patientPreference?.EmailEnabled ?? true))
                    {
                        await outbox.QueueAsync(new EmailMessage
                        {
                            To = appointment.Patient.Email!,
                            Subject = "Mentora - Yaklasan randevunuz var",
                            HtmlBody = $"<p><strong>{appointment.StartTime:dd.MM.yyyy HH:mm}</strong> saatinde randevunuz var.</p>"
                        }, cancellationToken);
                    }

                    await notificationService.CreateAsync(
                        appointment.PatientId!,
                        NotificationType.AppointmentReminder,
                        "Yaklasan randevu",
                        $"{appointment.StartTime:dd.MM.yyyy HH:mm} saatinde randevunuz var.",
                        "/Request/MyRequests");

                    appointment.PatientReminderSentAtUtc = utcNow;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private static bool ShouldSendReminder(DateTime? sentAtUtc, Models.UserNotificationPreference? preference, DateTime appointmentStart, DateTime now)
        {
            if (sentAtUtc.HasValue)
            {
                return false;
            }

            if (preference != null && !preference.AppointmentReminderEnabled)
            {
                return false;
            }

            var reminderMinutes = preference?.ReminderMinutesBefore ?? 60;
            return appointmentStart <= now.AddMinutes(reminderMinutes);
        }
    }
}
