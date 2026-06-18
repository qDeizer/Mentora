using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.Services.Email;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailOutboxService _emailOutboxService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IEmailOutboxService emailOutboxService,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailOutboxService = emailOutboxService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.DoctorCount = await _context.Doctors.CountAsync();
            ViewBag.PatientCount = await _context.Patients.CountAsync();
            ViewBag.AppointmentCount = await _context.Appointments.CountAsync();
            ViewBag.NoteCount = await _context.ClinicalNotes.CountAsync();
            ViewBag.PendingRequestCount = await _context.AppointmentRequests.CountAsync(r => r.Status == RequestStatus.Pending);
            ViewBag.PendingEmailCount = await _context.Set<EmailOutboxMessage>().CountAsync(m => m.Status == EmailOutboxStatus.Pending || m.Status == EmailOutboxStatus.Failed);

            ViewBag.RecentNotifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(20)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.CreatedAtUtc,
                    n.IsRead,
                    n.UserId,
                    UserName = n.User.FirstName + " " + n.User.LastName
                })
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? q)
        {
            var users = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                users = users.Where(u =>
                    u.FirstName.ToLower().Contains(lower) ||
                    u.LastName.ToLower().Contains(lower) ||
                    (u.Email ?? string.Empty).ToLower().Contains(lower) ||
                    (u.UserName ?? string.Empty).ToLower().Contains(lower));
            }

            var list = await users
                .OrderBy(u => u.UserType)
                .ThenBy(u => u.FirstName)
                .Take(500)
                .ToListAsync();

            ViewBag.Query = q ?? string.Empty;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Notes(string? q)
        {
            var query = _context.ClinicalNotes
                .Include(n => n.AuthorDoctor)
                .Include(n => n.Patient)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                query = query.Where(n =>
                    n.Content.ToLower().Contains(lower) ||
                    n.AuthorDoctor.FirstName.ToLower().Contains(lower) ||
                    n.AuthorDoctor.LastName.ToLower().Contains(lower) ||
                    n.Patient.FirstName.ToLower().Contains(lower) ||
                    n.Patient.LastName.ToLower().Contains(lower));
            }

            var list = await query
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(200)
                .ToListAsync();

            ViewBag.Query = q ?? string.Empty;
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> DeleteNote(int noteId, string confirmText)
        {
            if (string.IsNullOrWhiteSpace(confirmText) || !string.Equals(confirmText.Trim(), "SIL", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Notu silmek icin onay metni dogru girilmemis.";
                return RedirectToAction(nameof(Notes));
            }

            var note = await _context.ClinicalNotes
                .Include(n => n.AuthorDoctor)
                .Include(n => n.Patient)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                TempData["ErrorMessage"] = "Not bulunamadi.";
                return RedirectToAction(nameof(Notes));
            }

            var authorEmail = note.AuthorDoctor?.Email;
            var authorName = note.AuthorDoctor != null ? $"{note.AuthorDoctor.FirstName} {note.AuthorDoctor.LastName}" : "Yazar";
            var patientName = note.Patient != null ? $"{note.Patient.FirstName} {note.Patient.LastName}" : "-";
            var originalContent = note.Content;
            var createdAt = note.CreatedAtUtc;
            var noteId2 = note.Id;

            _context.ClinicalNotes.Remove(note);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(authorEmail))
            {
                var body = new System.Text.StringBuilder();
                body.Append("<h2 style='font-family:sans-serif;'>Klinik Notunuz Admin Tarafindan Silindi</h2>");
                body.Append("<p style='font-family:sans-serif;'>Aşağıdaki notunuz sistem yöneticisi tarafından silindi. Notun tüm içeriği referans amacıyla bu mailde yer almaktadır.</p>");
                body.Append("<table style='font-family:sans-serif;border-collapse:collapse;'>");
                body.Append($"<tr><td style='padding:4px 8px;'><strong>Not ID:</strong></td><td style='padding:4px 8px;'>{noteId2}</td></tr>");
                body.Append($"<tr><td style='padding:4px 8px;'><strong>Hasta:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(patientName)}</td></tr>");
                body.Append($"<tr><td style='padding:4px 8px;'><strong>Oluşturulma:</strong></td><td style='padding:4px 8px;'>{createdAt:dd.MM.yyyy HH:mm} UTC</td></tr>");
                body.Append($"<tr><td style='padding:4px 8px;'><strong>Silinme:</strong></td><td style='padding:4px 8px;'>{DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC</td></tr>");
                body.Append("</table>");
                body.Append("<h3 style='font-family:sans-serif;'>Tam İçerik</h3>");
                body.Append("<pre style='font-family:monospace;background:#f1f5f9;padding:12px;border-radius:8px;white-space:pre-wrap;'>");
                body.Append(System.Net.WebUtility.HtmlEncode(originalContent));
                body.Append("</pre>");
                body.Append("<p style='font-family:sans-serif;'>İtirazlarınız için sistem yöneticisi ile iletişime geçin.</p>");

                await _emailOutboxService.QueueAsync(new EmailMessage
                {
                    To = authorEmail,
                    Subject = $"Mentora - Klinik notunuz silindi (Not #{noteId2})",
                    HtmlBody = body.ToString()
                });
            }

            if (note.AuthorDoctorId != null)
            {
                await _notificationService.CreateAsync(
                    note.AuthorDoctorId,
                    NotificationType.Generic,
                    "Klinik notunuz silindi",
                    $"Not #{noteId2} sistem yöneticisi tarafından silindi. Detay için e-postanıza bakın.",
                    "/ClinicalNotes/Index");
            }

            _logger.LogWarning("Admin {AdminUser} klinik not {NoteId} sildi. Yazar={AuthorName}, Hasta={PatientName}",
                _userManager.GetUserName(User), noteId2, authorName, patientName);

            TempData["SuccessMessage"] = $"Not #{noteId2} silindi. Yazara tam içerik mailendi.";
            return RedirectToAction(nameof(Notes));
        }

        [HttpGet]
        public async Task<IActionResult> Appointments(string? q)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TargetPatient)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                query = query.Where(a =>
                    a.Doctor.FirstName.ToLower().Contains(lower) ||
                    a.Doctor.LastName.ToLower().Contains(lower) ||
                    (a.Patient != null && (a.Patient.FirstName.ToLower().Contains(lower) || a.Patient.LastName.ToLower().Contains(lower))));
            }

            var list = await query
                .OrderByDescending(a => a.StartTime)
                .Take(200)
                .ToListAsync();

            ViewBag.Query = q ?? string.Empty;
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> ToggleUserApproval(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Users));
            }

            user.IsApproved = !user.IsApproved;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = $"{user.FirstName} {user.LastName}: {(user.IsApproved ? "onaylandı" : "onayı kaldırıldı")}.";
            return RedirectToAction(nameof(Users));
        }
    }
}
