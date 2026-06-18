using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Patient")]
    [Route("ClinicalNoteBulkActions")]
    public class ClinicalNoteBulkActionsController : Controller
    {
        private readonly IClinicalNoteService _clinicalNoteService;
        private readonly UserManager<User> _userManager;

        public ClinicalNoteBulkActionsController(IClinicalNoteService clinicalNoteService, UserManager<User> userManager)
        {
            _clinicalNoteService = clinicalNoteService;
            _userManager = userManager;
        }

        [HttpPost("Apply")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Apply(ClinicalNoteBulkActionInputViewModel model)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var result = await _clinicalNoteService.ApplyBulkActionsAsync(patientId, model);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Çoklu not işlemi uygulandı." : result.ErrorMessage ?? "Çoklu not işlemi başarısız.";

            return RedirectToAction("MyNotes", "ClinicalNotes");
        }
    }
}
