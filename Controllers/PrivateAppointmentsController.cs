using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PrivateAppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<User> _userManager;

        public PrivateAppointmentsController(IAppointmentService appointmentService, UserManager<User> userManager)
        {
            _appointmentService = appointmentService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var items = await _appointmentService.GetPrivateOffersForPatientAsync(patientId);
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Respond(int appointmentId, bool accept, string? responseMessage)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var result = await _appointmentService.RespondPrivateOfferAsync(appointmentId, patientId, accept, responseMessage);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded
                    ? (accept ? "Özel randevu teklifi kabul edildi." : "Özel randevu teklifi reddedildi.")
                    : result.ErrorMessage ?? "İşlem başarısız.";

            return RedirectToAction(nameof(Index));
        }
    }
}
