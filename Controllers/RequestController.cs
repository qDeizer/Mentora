using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Approve(int requestId)
        {
            var doctorId = _userManager.GetUserId(User);
            var result = await _requestService.ApproveRequestAsync(requestId, doctorId);
            if (result) TempData["SuccessMessage"] = "Randevu talebi başarıyla onaylandı.";
            else TempData["ErrorMessage"] = "İşlem sırasında bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int requestId, string responseMessage)
        {
            if (string.IsNullOrWhiteSpace(responseMessage))
            {
                TempData["ErrorMessage"] = "Reddetme sebebi boş bırakılamaz.";
                return RedirectToAction(nameof(Index));
            }
            var doctorId = _userManager.GetUserId(User);
            var result = await _requestService.RejectRequestAsync(requestId, responseMessage, doctorId);
            if (result) TempData["SuccessMessage"] = "Randevu talebi reddedildi.";
            else TempData["ErrorMessage"] = "İşlem sırasında bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId)
        {
            var patientId = _userManager.GetUserId(User);
            var result = await _requestService.CancelRequestAsync(requestId, patientId);
            if (result) TempData["SuccessMessage"] = "Randevu talebiniz başarıyla iptal edildi.";
            else TempData["ErrorMessage"] = "İşlem sırasında bir hata oluştu veya bu talebi iptal etme yetkiniz yok.";
            return RedirectToAction(nameof(MyRequests));
        }
    }
}