using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Create()
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            return View(new CreateAppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            if (!model.EndTime.HasValue && !model.DurationInMinutes.HasValue)
            {
                ModelState.AddModelError("", "Lütfen randevu bitiş saatini veya randevu süresini belirtin.");
            }
            if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime.Value <= model.StartTime.Value)
            {
                ModelState.AddModelError("EndTime", "Bitiş saati, başlangıç saatinden sonra olmalıdır.");
            }

            if (ModelState.IsValid)
            {
                if (model.IsInPerson)
                {
                    var doctor = await _userManager.GetUserAsync(User) as Doctor;
                    if (doctor?.Location == null)
                    {
                        ModelState.AddModelError("", "Yüz yüze randevu oluşturamazsınız çünkü profilinizde kayıtlı bir konum bulunmuyor. Lütfen profilinizi güncelleyin.");
                        ViewBag.Specialties = await _context.Specialties.ToListAsync();
                        return View(model);
                    }
                }

                var result = await _appointmentService.CreateAppointmentAsync(model, User);
                if (result)
                {
                    TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu.";
                    return RedirectToAction("Index", "DoctorDashboard");
                }
                ModelState.AddModelError("", "Randevu oluşturulurken bir hata oluştu. Lütfen bilgileri kontrol edip tekrar deneyin.");
            }

            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            return View(model);
        }

        // YENİ EKLENDİ: Randevu silme action'ı
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int appointmentId)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return Unauthorized();

            var result = await _appointmentService.DeleteAppointmentAsync(appointmentId, doctorId);

            if (result)
            {
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevu silinirken bir hata oluştu veya bu işlem için yetkiniz yok.";
            }
            return RedirectToAction("Index", "DoctorDashboard");
        }
    }
}