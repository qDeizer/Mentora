using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    [Route("ClinicalNoteComments")]
    public class ClinicalNoteCommentsController : Controller
    {
        private readonly IClinicalNoteService _clinicalNoteService;
        private readonly UserManager<User> _userManager;

        public ClinicalNoteCommentsController(IClinicalNoteService clinicalNoteService, UserManager<User> userManager)
        {
            _clinicalNoteService = clinicalNoteService;
            _userManager = userManager;
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Create(int noteId, string content, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.AddCommentAsync(userId, User.IsInRole("Doctor"), noteId, content);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Yorum eklendi." : result.ErrorMessage ?? "Yorum eklenemedi.";

            return RedirectToLocal(returnUrl);
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Update(int commentId, string content, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.UpdateCommentAsync(userId, User.IsInRole("Doctor"), commentId, content);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Yorum güncellendi." : result.ErrorMessage ?? "Yorum güncellenemedi.";

            return RedirectToLocal(returnUrl);
        }

        [HttpPost("ToggleLock")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> ToggleLock(int commentId, bool isLockedForPatient, string? returnUrl = null)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.ToggleCommentLockAsync(doctorId, commentId, isLockedForPatient);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? (isLockedForPatient ? "Yorum hastaya kilitlendi." : "Yorum kilidi acildi.")
                    : result.ErrorMessage ?? "İşlem başarısız.";

            return RedirectToLocal(returnUrl);
        }

        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Delete(int commentId, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.DeleteCommentAsync(userId, User.IsInRole("Doctor"), commentId);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Yorum silindi." : result.ErrorMessage ?? "Yorum silinemedi.";

            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return User.IsInRole("Doctor")
                ? RedirectToAction("Index", "ClinicalNotes")
                : RedirectToAction("MyNotes", "ClinicalNotes");
        }
    }
}
