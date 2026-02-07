// FILE: \Controllers\DoctorDashboardController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System;
using System.Threading.Tasks;
namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public DoctorDashboardController(
            IAppointmentService appointmentService,
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await _appointmentService.UpdateExpiredAppointmentsAsync();

            var doctorId = _userManager.GetUserId(User);
            var filter = new AppointmentFilterModel
            {
                DoctorId = doctorId
            };
            var appointments = await _appointmentService.GetAppointmentsAsync(filter, User);

            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            var model = new DoctorDashboardViewModel
            {
                Appointments = appointments,
                CreateAppointment = new CreateAppointmentViewModel()
            };
            return View(model);
        }
    }
}