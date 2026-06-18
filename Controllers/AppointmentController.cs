using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System.Globalization;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentController(IAppointmentService appointmentService, ApplicationDbContext context, UserManager<User> userManager)
        {
            _appointmentService = appointmentService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(DateTime? date, string? targetPatientId, bool isPrivateOffer = false)
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            ViewBag.TargetPatients = await GetLinkedPatientsAsync();

            var model = new CreateAppointmentViewModel();
            if (date.HasValue)
            {
                model.StartTime = DateTime.SpecifyKind(date.Value, DateTimeKind.Unspecified);
            }

            if (!string.IsNullOrWhiteSpace(targetPatientId))
            {
                model.TargetPatientId = targetPatientId;
                model.IsPrivateOffer = true;
            }
            else
            {
                model.IsPrivateOffer = isPrivateOffer;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            if (!model.EndTime.HasValue && !model.DurationInMinutes.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Lütfen randevu bitiş saatini veya randevu süresini belirtin.");
            }

            if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime.Value <= model.StartTime.Value)
            {
                ModelState.AddModelError(nameof(model.EndTime), "Bitiş saati başlangıç saatinden sonra olmalıdır.");
            }

            if (ModelState.IsValid)
            {
                var result = await _appointmentService.CreateAppointmentAsync(model, User);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu.";
                    return RedirectToAction("Index", "DoctorDashboard");
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Randevu oluşturulamadı.");
            }

            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            ViewBag.TargetPatients = await GetLinkedPatientsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Delete(int appointmentId)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _appointmentService.DeleteAppointmentAsync(appointmentId, doctorId);

            if (result)
            {
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevu silinirken hata oluştu veya yetkiniz yok.";
            }

            return RedirectToAction("Index", "DoctorDashboard");
        }

        private async Task<List<ClinicalNotePatientOptionViewModel>> GetLinkedPatientsAsync()
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return new List<ClinicalNotePatientOptionViewModel>();
            }

            var patientIds = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.PatientId != null)
                .Select(a => a.PatientId!)
                .Union(_context.AppointmentRequests.Where(r => r.DoctorId == doctorId).Select(r => r.PatientId))
                .Distinct()
                .ToListAsync();

            return await _context.Patients
                .Where(p => patientIds.Contains(p.Id))
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new ClinicalNotePatientOptionViewModel
                {
                    Id = p.Id,
                    Name = p.FirstName + " " + p.LastName,
                    ProfilePhotoPath = p.ProfilePhotoPath
                })
                .ToListAsync();
        }
    }
}
