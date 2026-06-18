using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    public class ClinicalNotesController : Controller
    {
        private readonly IClinicalNoteService _clinicalNoteService;
        private readonly UserManager<User> _userManager;

        public ClinicalNotesController(IClinicalNoteService clinicalNoteService, UserManager<User> userManager)
        {
            _clinicalNoteService = clinicalNoteService;
            _userManager = userManager;
        }

        [Authorize(Roles = "Doctor")]
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] List<string>? patientIds,
            [FromQuery] List<string>? selectedPatientIds,
            string? query,
            string? sortBy,
            string? sortDirection)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var effectivePatientIds = selectedPatientIds is { Count: > 0 } ? selectedPatientIds : patientIds;
            var model = await _clinicalNoteService.GetDoctorDashboardAsync(
                doctorId,
                effectivePatientIds,
                query,
                sortBy,
                sortDirection);
            return View(model);
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] ClinicalNoteCreateViewModel model)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Not bilgileri gecersiz.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _clinicalNoteService.CreateNoteAsync(doctorId, model);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Klinik not kaydedildi." : result.ErrorMessage ?? "Not kaydedilemedi.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Update(int noteId, string content)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.UpdateNoteAsync(doctorId, noteId, content);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Klinik not güncellendi." : result.ErrorMessage ?? "Not güncellenemedi.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> ToggleLock(int noteId, bool isLockedForPatient)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.ToggleLockAsync(doctorId, noteId, isLockedForPatient);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? (isLockedForPatient ? "Not hasta icin kilitlendi." : "Not kilidi kaldirildi.")
                    : result.ErrorMessage ?? "Kilit güncellenemedi.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Patient")]
        [HttpGet]
        public async Task<IActionResult> MyNotes([FromQuery] ClinicalNotesMyNotesFilterViewModel filter)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var model = await _clinicalNoteService.GetPatientDashboardAsync(patientId, filter);
            return View(model);
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Share(string targetDoctorId, List<int> noteIds)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            noteIds ??= new List<int>();

            var result = await _clinicalNoteService.ShareNotesAsync(patientId, targetDoctorId, noteIds);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Notlar seçilen doktorla paylaşıldı." : result.ErrorMessage ?? "Paylaşım yapılamadı.";

            return RedirectToAction(nameof(MyNotes));
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> UpdateAccessRule(ClinicalNoteAccessRuleCommandViewModel model)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.UpdateAccessRuleAsync(patientId, model);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Not yetkisi güncellendi." : result.ErrorMessage ?? "Not yetkisi güncellenemedi.";

            return RedirectToAction(nameof(MyNotes));
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> RevokeShare(string targetDoctorId, List<int> noteIds)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            noteIds ??= new List<int>();
            var result = await _clinicalNoteService.RevokeShareAsync(patientId, targetDoctorId, noteIds);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Not paylasimi geri alindi." : result.ErrorMessage ?? "Paylasim geri alinamadi.";

            return RedirectToAction(nameof(MyNotes));
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> UpdateVisibility(int noteId, ClinicalNoteVisibility visibility)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.UpdateNoteVisibilityAsync(patientId, noteId, visibility);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Not görünürlüğü güncellendi." : result.ErrorMessage ?? "Not görünürlüğü güncellenemedi.";

            return RedirectToAction(nameof(MyNotes));
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> BulkUpdateVisibility(ClinicalNoteVisibility visibility, List<int> noteIds)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            noteIds ??= new List<int>();
            var result = await _clinicalNoteService.BulkUpdateVisibilityAsync(patientId, noteIds, visibility);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Seçili notların görünürlüğü güncellendi." : result.ErrorMessage ?? "Seçili notlar güncellenemedi.";

            return RedirectToAction(nameof(MyNotes));
        }
    }
}
