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
    public class RequestController : Controller
    {
        private readonly IAppointmentRequestService _requestService;
        private readonly UserManager<User> _userManager;

        public RequestController(IAppointmentRequestService requestService, UserManager<User> userManager)
        {
            _requestService = requestService;
            _userManager = userManager;
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Index([FromQuery] AppointmentRequestFilterModel filter)
        {
            filter.DoctorId = _userManager.GetUserId(User);
            var model = await _requestService.GetRequestsAsync(filter);
            return View("DoctorRequests", model);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> MyRequests([FromQuery] AppointmentRequestFilterModel filter)
        {
            filter.PatientId = _userManager.GetUserId(User);
            var model = await _requestService.GetRequestsAsync(filter);
            return View("PatientRequests", model);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Approve(int requestId, string? responseMessage, string? autoRejectMessage)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _requestService.ApproveRequestAsync(requestId, doctorId, responseMessage, autoRejectMessage);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? result.SuccessMessage ?? "Randevu talebi onaylandı." : result.ErrorMessage ?? "İşlem sırasında hata oluştu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Reject(int requestId, string responseMessage)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return Unauthorized();
            }

            var result = await _requestService.RejectRequestAsync(requestId, responseMessage, doctorId);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Randevu talebi reddedildi." : result.ErrorMessage ?? "İşlem sırasında hata oluştu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Cancel(int requestId)
        {
            var patientId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var result = await _requestService.CancelRequestAsync(requestId, patientId);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "Randevu talebiniz iptal edildi." : result.ErrorMessage ?? "İşlem sırasında hata oluştu.";

            return RedirectToAction(nameof(MyRequests));
        }
    }
}
