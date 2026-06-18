using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IGlobalLocationContextService _globalLocationContextService;

        public PatientDashboardController(
            IAppointmentService appointmentService,
            ApplicationDbContext context,
            UserManager<User> userManager,
            IGlobalLocationContextService globalLocationContextService)
        {
            _appointmentService = appointmentService;
            _context = context;
            _userManager = userManager;
            _globalLocationContextService = globalLocationContextService;
        }

        public async Task<IActionResult> Index([FromQuery] AppointmentFilterModel filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.DoctorId))
            {
                filter.SelectedDoctorIds ??= new List<string>();
                if (!filter.SelectedDoctorIds.Contains(filter.DoctorId))
                {
                    filter.SelectedDoctorIds.Add(filter.DoctorId);
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var globalContext = await _globalLocationContextService.GetContextAsync(User);
            if (globalContext.HasCoordinates)
            {
                filter.Latitude = globalContext.Latitude;
                filter.Longitude = globalContext.Longitude;
            }

            var appointments = await _appointmentService.GetAppointmentsAsync(filter, User);

            var doctors = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Available)
                .Select(a => new
                {
                    Id = a.DoctorId,
                    FullName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                    a.Doctor.ProfilePhotoPath
                })
                .Distinct()
                .OrderBy(d => d.FullName)
                .ToListAsync();

            var specialties = await _context.Specialties
                .OrderBy(s => s.Name)
                .Select(s => new SpecialtyFilterOptionViewModel
                {
                    Id = s.Id,
                    Name = s.Name ?? string.Empty
                })
                .ToListAsync();

            var model = new PatientDashboardViewModel
            {
                Appointments = appointments,
                Filter = filter,
                Doctors = doctors
                    .Select(d => new DoctorFilterOptionViewModel
                    {
                        Id = d.Id,
                        FullName = d.FullName,
                        ProfilePhotoPath = d.ProfilePhotoPath
                    })
                    .ToList(),
                Specialties = specialties,
                ProfileLatitude = currentUser?.Location?.Y,
                ProfileLongitude = currentUser?.Location?.X,
                GlobalLocationLabel = globalContext.Label,
                GlobalLocationSource = globalContext.Source.ToString(),
                GlobalLatitude = globalContext.Latitude,
                GlobalLongitude = globalContext.Longitude
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Appointments()
        {
            await _appointmentService.UpdateExpiredAppointmentsAsync();

            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var now = PsikologProje_Void.Utils.TimeZoneHelper.GetTurkeyNow();
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var items = appointments.Select(a => new PatientAppointmentListItemViewModel
            {
                AppointmentId = a.Id,
                DoctorId = a.DoctorId,
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}".Trim(),
                DoctorProfilePhotoPath = a.Doctor.ProfilePhotoPath,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                TypeText = BuildTypeText(a),
                Status = a.Status,
                StatusText = BuildStatusText(a.Status),
                PriceRange = FormatPriceRange(a.MinPrice, a.MaxPrice),
                MeetingLink = a.Status == AppointmentStatus.Reserved ? a.MeetingLink : null,
                Latitude = a.Location?.Y,
                Longitude = a.Location?.X,
                LocationNote = a.LocationNote,
                Notes = a.Notes,
                Rating = a.Rating,
                Review = a.Review,
                CanRate = a.Status == AppointmentStatus.Completed && !a.Rating.HasValue
            }).ToList();

            var model = new PatientAppointmentsViewModel
            {
                UpcomingAppointments = items
                    .Where(x => x.Status == AppointmentStatus.Reserved && x.StartTime >= now)
                    .OrderBy(x => x.StartTime)
                    .ToList(),
                PastAppointments = items
                    .Where(x => x.Status != AppointmentStatus.Reserved || x.StartTime < now)
                    .OrderByDescending(x => x.StartTime)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> RateAppointment(int appointmentId, int rating, string? review)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Puan 1 ile 5 arasında olmalıdır.";
                return RedirectToAction(nameof(Appointments));
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Appointments));
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                TempData["ErrorMessage"] = "Sadece tamamlanmış randevular değerlendirilebilir.";
                return RedirectToAction(nameof(Appointments));
            }

            if (appointment.Rating.HasValue)
            {
                TempData["ErrorMessage"] = "Bu randevu daha önce değerlendirilmiş.";
                return RedirectToAction(nameof(Appointments));
            }

            appointment.Rating = rating;
            appointment.Review = string.IsNullOrWhiteSpace(review) ? null : review.Trim();
            appointment.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Değerlendirmeniz kaydedildi.";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpGet]
        public async Task<IActionResult> Calendar(int id)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patientId && a.Status == AppointmentStatus.Reserved);

            if (appointment == null)
            {
                return NotFound();
            }

            var title = $"Mentora randevusu - {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
            var description = string.Join("\\n", new[]
            {
                appointment.Notes,
                appointment.IsOnline && !string.IsNullOrWhiteSpace(appointment.MeetingLink) ? $"Çevrim içi link: {appointment.MeetingLink}" : null,
                appointment.LocationNote
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var calendar = new StringBuilder();
            calendar.AppendLine("BEGIN:VCALENDAR");
            calendar.AppendLine("VERSION:2.0");
            calendar.AppendLine("PRODID:-//Mentora//Randevu//TR");
            calendar.AppendLine("BEGIN:VEVENT");
            calendar.AppendLine($"UID:mentora-{appointment.Id}@local");
            calendar.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            calendar.AppendLine($"DTSTART:{appointment.StartTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
            calendar.AppendLine($"DTEND:{appointment.EndTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
            calendar.AppendLine($"SUMMARY:{EscapeIcs(title)}");
            calendar.AppendLine($"DESCRIPTION:{EscapeIcs(description)}");
            calendar.AppendLine("END:VEVENT");
            calendar.AppendLine("END:VCALENDAR");

            return File(Encoding.UTF8.GetBytes(calendar.ToString()), "text/calendar", $"mentora-randevu-{appointment.Id}.ics");
        }

        private static string BuildTypeText(Appointment appointment)
        {
            if (appointment.IsOnline && appointment.IsInPerson)
            {
                return "Çevrim içi + yüz yüze";
            }

            if (appointment.IsOnline)
            {
                return "Çevrim içi";
            }

            return appointment.IsInPerson ? "Yüz yüze" : "-";
        }

        private static string BuildStatusText(AppointmentStatus status)
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

        private static string FormatPriceRange(decimal? minPrice, decimal? maxPrice)
        {
            if (!minPrice.HasValue && !maxPrice.HasValue)
            {
                return "-";
            }

            if (minPrice.HasValue && maxPrice.HasValue)
            {
                return $"{minPrice.Value:0} TL - {maxPrice.Value:0} TL";
            }

            return minPrice.HasValue ? $"{minPrice.Value:0} TL" : $"{maxPrice!.Value:0} TL";
        }

        private static string EscapeIcs(string? value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }
    }
}
