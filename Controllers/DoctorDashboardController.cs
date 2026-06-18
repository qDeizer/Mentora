// FILE: \Controllers\DoctorDashboardController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;
using System;
using System.Globalization;
using System.Threading.Tasks;
namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public DoctorDashboardController(
            IAppointmentService appointmentService,
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await _appointmentService.UpdateExpiredAppointmentsAsync();

            var doctorId = _userManager.GetUserId(User);
            var filter = new AppointmentFilterModel
            {
                DoctorId = doctorId
            };
            var appointments = await _appointmentService.GetAppointmentsAsync(filter, User);

            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            var model = new DoctorDashboardViewModel
            {
                Appointments = appointments,
                CreateAppointment = new CreateAppointmentViewModel()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Scheduler(DateTime? weekStart, DateTime? monthDate, string view = "week")
        {
            await _appointmentService.UpdateExpiredAppointmentsAsync();

            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var turkeyToday = TimeZoneHelper.GetTurkeyNow().Date;
            var requestedDate = (weekStart ?? monthDate ?? turkeyToday).Date;
            var normalizedView = string.Equals(view, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";
            var weekStartDate = StartOfWeek(requestedDate, DayOfWeek.Monday);
            var weekEndDate = weekStartDate.AddDays(7);
            var monthAnchor = (monthDate ?? weekStartDate).Date;
            var monthStart = new DateTime(monthAnchor.Year, monthAnchor.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthGridStart = StartOfWeek(monthStart, DayOfWeek.Monday);
            var monthGridEnd = StartOfWeek(monthEnd, DayOfWeek.Monday).AddDays(6);

            var weekAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.TargetPatient)
                .Where(a =>
                    a.DoctorId == doctorId &&
                    a.StartTime >= weekStartDate &&
                    a.StartTime < weekEndDate &&
                    a.Status != AppointmentStatus.NotCompleted)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var monthAppointments = await _context.Appointments
                .Where(a =>
                    a.DoctorId == doctorId &&
                    a.StartTime >= monthGridStart &&
                    a.StartTime < monthGridEnd.AddDays(1) &&
                    a.Status != AppointmentStatus.NotCompleted)
                .ToListAsync();

            var hasAnyAppointment = weekAppointments.Count > 0;
            var gridStartMinute = hasAnyAppointment
                ? RoundDownToHalfHour(weekAppointments.Min(a => (int)a.StartTime.TimeOfDay.TotalMinutes))
                : 0;
            var gridEndMinute = hasAnyAppointment
                ? RoundUpToHalfHour(weekAppointments.Max(a => (int)a.EndTime.TimeOfDay.TotalMinutes))
                : (24 * 60) - 1;

            if (hasAnyAppointment && gridEndMinute <= gridStartMinute)
            {
                gridEndMinute = gridStartMinute + 30;
            }

            var culture = new CultureInfo("tr-TR");
            var days = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var dayDate = weekStartDate.AddDays(i);
                    var dayAppointments = weekAppointments.Where(a => a.StartTime.Date == dayDate).ToList();
                    var availableCount = dayAppointments.Count(a => string.IsNullOrWhiteSpace(a.PatientId) && a.Status == AppointmentStatus.Available);
                    var assignedCount = dayAppointments.Count(a => a.PatientId != null || a.Status == AppointmentStatus.Reserved || a.Status == AppointmentStatus.Completed);

                    return new DoctorWeeklyScheduleDayViewModel
                    {
                        DayIndex = i,
                        Date = dayDate,
                        DayName = culture.TextInfo.ToTitleCase(dayDate.ToString("dddd", culture)),
                        TotalSessionCount = dayAppointments.Count,
                        AvailableSessionCount = availableCount,
                        AssignedSessionCount = assignedCount,
                        TotalHours = Math.Round(dayAppointments.Sum(a => (a.EndTime - a.StartTime).TotalHours), 1)
                    };
                })
                .ToList();

            var slots = weekAppointments.Select(a =>
            {
                string? slotPatientName;
                string? slotPatientId;
                if (a.Patient != null)
                {
                    slotPatientName = $"{a.Patient.FirstName} {a.Patient.LastName}";
                    slotPatientId = a.Patient.Id;
                }
                else if (a.IsPrivateOffer && a.TargetPatient != null)
                {
                    slotPatientName = $"{a.TargetPatient.FirstName} {a.TargetPatient.LastName}";
                    slotPatientId = a.TargetPatient.Id;
                }
                else
                {
                    slotPatientName = "Atanmamış seans";
                    slotPatientId = null;
                }

                return new DoctorWeeklyScheduleSlotViewModel
                {
                    AppointmentId = a.Id,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    DayIndex = (int)(a.StartTime.Date - weekStartDate).TotalDays,
                    StartMinuteOfDay = (int)a.StartTime.TimeOfDay.TotalMinutes,
                    EndMinuteOfDay = (int)a.EndTime.TimeOfDay.TotalMinutes,
                    IsAssigned = a.PatientId != null || a.Status == AppointmentStatus.Reserved || a.Status == AppointmentStatus.Completed || (a.IsPrivateOffer && a.OfferStatus == AppointmentOfferStatus.Pending),
                    StatusLabel = AppointmentService.GetUserFacingStatus(a),
                    PatientId = slotPatientId,
                    PatientName = slotPatientName,
                    PatientProfilePhotoPath = a.Patient?.ProfilePhotoPath ?? (a.IsPrivateOffer ? a.TargetPatient?.ProfilePhotoPath : null),
                    TimeLabel = $"{a.StartTime:HH:mm} - {a.EndTime:HH:mm}",
                    PriceLabel = (a.MinPrice.HasValue || a.MaxPrice.HasValue)
                        ? $"{a.MinPrice:N2} TL - {a.MaxPrice:N2} TL"
                        : "-",
                    AppointmentTypeLabel = string.Join(", ", new[]
                    {
                        a.IsOnline ? "Çevrim içi" : string.Empty,
                        a.IsInPerson ? "Yüz yüze" : string.Empty
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Notes = a.Notes,
                    LocationNote = a.LocationNote,
                    RelativeDayLabel = BuildRelativeDayLabel(a.StartTime)
                };
            }).ToList();

            var activeDayIndex = days.FirstOrDefault(d => d.Date == turkeyToday)?.DayIndex ?? 0;
            if (turkeyToday < weekStartDate || turkeyToday >= weekEndDate)
            {
                activeDayIndex = 0;
            }

            var monthDays = Enumerable.Range(0, (monthGridEnd - monthGridStart).Days + 1)
                .Select(i =>
                {
                    var date = monthGridStart.AddDays(i);
                    var dayAppointments = monthAppointments.Where(a => a.StartTime.Date == date).ToList();
                    var availableCount = dayAppointments.Count(a => string.IsNullOrWhiteSpace(a.PatientId) && a.Status == AppointmentStatus.Available);
                    var assignedCount = dayAppointments.Count(a => a.PatientId != null || a.Status == AppointmentStatus.Reserved || a.Status == AppointmentStatus.Completed);
                    return new DoctorScheduleMonthDayViewModel
                    {
                        Date = date,
                        WeekStartDate = StartOfWeek(date, DayOfWeek.Monday),
                        IsCurrentMonth = date.Month == monthStart.Month,
                        IsToday = date == turkeyToday,
                        IsSelectedWeek = date >= weekStartDate && date < weekEndDate,
                        TotalSessionCount = dayAppointments.Count,
                        AvailableSessionCount = availableCount,
                        AssignedSessionCount = assignedCount
                    };
                })
                .ToList();

            var model = new DoctorWeeklyScheduleViewModel
            {
                ViewMode = normalizedView,
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate.AddDays(-1),
                PreviousWeekStartDate = weekStartDate.AddDays(-7),
                NextWeekStartDate = weekStartDate.AddDays(7),
                WeekRangeLabel = $"{weekStartDate:dd MMM} - {weekEndDate.AddDays(-1):dd MMM yyyy}",
                MonthDate = monthAnchor,
                PreviousMonthDate = monthStart.AddMonths(-1),
                NextMonthDate = monthStart.AddMonths(1),
                MonthLabel = monthStart.ToString("MMMM yyyy", culture),
                ActiveDayIndex = activeDayIndex,
                GridStartMinute = gridStartMinute,
                GridEndMinute = gridEndMinute,
                HasAnyAppointment = hasAnyAppointment,
                Days = days,
                Slots = slots,
                MonthDays = monthDays
            };

            return View(model);
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }

        private static int RoundDownToHalfHour(int minute)
        {
            if (minute <= 0)
            {
                return 0;
            }

            return minute - (minute % 30);
        }

        private static int RoundUpToHalfHour(int minute)
        {
            if (minute <= 0)
            {
                return 30;
            }

            var remainder = minute % 30;
            if (remainder == 0)
            {
                return minute;
            }

            return minute + (30 - remainder);
        }

        private static string ToStatusLabel(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Available => "Müsait",
                AppointmentStatus.Reserved => "Rezerve",
                AppointmentStatus.Completed => "Tamamlandı",
                AppointmentStatus.NotCompleted => "Gerçekleşmedi",
                AppointmentStatus.CancelledByConflict => "Çakışma nedeniyle kapatıldı",
                _ => status.ToString()
            };
        }

        private static string BuildRelativeDayLabel(DateTime startTime)
        {
            var culture = new CultureInfo("tr-TR");
            var today = TimeZoneHelper.GetTurkeyNow().Date;
            var target = startTime.Date;
            var dayDiff = (target - today).Days;

            return dayDiff switch
            {
                0 => "Bugün",
                1 => "Yarın",
                2 => "2 gün sonra",
                >= 3 and <= 6 => target.ToString("dddd", culture),
                >= 7 and <= 13 => $"Haftaya {target.ToString("dddd", culture)}",
                _ => target.ToString("dddd", culture)
            };
        }
    }
}
