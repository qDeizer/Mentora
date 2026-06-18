using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class AutomationController : Controller
    {
        private readonly IAppointmentAutomationService _automationService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AutomationController(IAppointmentAutomationService automationService, ApplicationDbContext context, UserManager<User> userManager)
        {
            _automationService = automationService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var model = await BuildDashboardModelAsync(doctorId, new AutomationRoutineInputViewModel());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] AutomationRoutineInputViewModel model)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildDashboardModelAsync(doctorId, model);
                return View("Index", invalidModel);
            }

            var result = await _automationService.CreateRoutineAsync(doctorId, model);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Otomatik randevu rutini oluşturuldu." : result.ErrorMessage ?? "Rutin oluşturulamadı.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var model = await _automationService.GetRoutineForEditAsync(doctorId, id);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.SpecialtyOptions = await LoadSpecialtyOptionsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Edit(AutomationRoutineInputViewModel model)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SpecialtyOptions = await LoadSpecialtyOptionsAsync();
                return View(model);
            }

            var result = await _automationService.UpdateRoutineAsync(doctorId, model);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Rutin güncellendi." : result.ErrorMessage ?? "Rutin güncellenemedi.";

            if (!result.Succeeded)
            {
                ViewBag.SpecialtyOptions = await LoadSpecialtyOptionsAsync();
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Pause(int id, int? pauseDays, DateTime? pauseUntilLocal)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _automationService.PauseRoutineAsync(doctorId, id, pauseDays, pauseUntilLocal);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Rutin duraklatildi." : result.ErrorMessage ?? "Rutin duraklatilamadi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Resume(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _automationService.ResumeRoutineAsync(doctorId, id);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Rutin tekrar aktif edildi." : result.ErrorMessage ?? "Rutin aktif edilemedi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Delete(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _automationService.DeleteRoutineAsync(doctorId, id);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Rutin silindi." : result.ErrorMessage ?? "Rutin silinemedi.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<AutomationDashboardViewModel> BuildDashboardModelAsync(string doctorId, AutomationRoutineInputViewModel form)
        {
            return new AutomationDashboardViewModel
            {
                Routines = await _automationService.GetDoctorRoutinesAsync(doctorId),
                CreateForm = form,
                SpecialtyOptions = await LoadSpecialtyOptionsAsync()
            };
        }

        private async Task<List<SelectListItem>> LoadSpecialtyOptionsAsync()
        {
            return await _context.Specialties
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();
        }
    }
}
