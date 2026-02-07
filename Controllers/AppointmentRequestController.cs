using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<IActionResult> CreateRequest(int appointmentId, string doctorId, string? message)
        {
            try
            {
                var request = new AppointmentRequest
                {
                    AppointmentId = appointmentId,
                    DoctorId = doctorId,
                    PatientId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    RequestMessage = message,
                    Status = RequestStatus.Pending
                };

                var result = await _requestService.CreateAppointmentRequestAsync(request);

                if (result)
                {
                    return RedirectToAction("Index", "PatientDashboard");
                }

                TempData["ErrorMessage"] = "Talep oluşturulamadı. Lütfen bilgileri kontrol edin.";
                return RedirectToAction("Index", "PatientDashboard");
            }
            catch (Exception ex)
            {
                // Hata detayını logla ve kullanıcıya göster
                TempData["ErrorMessage"] = $"Hata oluştu: {ex.Message}";
                return RedirectToAction("Index", "PatientDashboard");
            }
        }
    }
}