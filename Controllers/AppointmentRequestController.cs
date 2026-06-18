using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using System.Security.Claims;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Patient")]
    public class AppointmentRequestController : Controller
    {
        private readonly IAppointmentRequestService _requestService;

        public AppointmentRequestController(IAppointmentRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> CreateRequest(
            int appointmentId,
            string doctorId,
            string? message,
            string? reasonForVisit,
            string? previousSupportInfo,
            string? urgencyLevel,
            string? expectations)
        {
            var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Unauthorized();
            }

            var request = new AppointmentRequest
            {
                AppointmentId = appointmentId,
                DoctorId = doctorId,
                PatientId = patientId,
                RequestMessage = message,
                ReasonForVisit = reasonForVisit,
                PreviousSupportInfo = previousSupportInfo,
                UrgencyLevel = urgencyLevel,
                Expectations = expectations,
                Status = RequestStatus.Pending
            };

            var result = await _requestService.CreateAppointmentRequestAsync(request);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Randevu talebiniz gönderildi.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Talep oluşturulamadı.";
            }

            return RedirectToAction("Index", "PatientDashboard");
        }
    }
}
