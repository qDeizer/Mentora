using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;
using PsikologProje_Void.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PsikologProje_Void.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ApplicationDbContext _context;

        public PatientDashboardController(IAppointmentService appointmentService, ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _context = context;
        }

        public async Task<IActionResult> Index([FromQuery] AppointmentFilterModel filter)
        {
            // Servis metodu zaten kendi içinde güncelleme işlemini tetikleyecektir.
            var appointments = await _appointmentService.GetAppointmentsAsync(filter, User);

            var doctors = await _context.Doctors
                .Select(d => new { d.Id, FullName = d.FirstName + " " + d.LastName })
                .ToListAsync();
            var specialties = await _context.Specialties.ToListAsync();

            var model = new PatientDashboardViewModel
            {
                Appointments = appointments,
                Filter = filter,
                Doctors = new SelectList(doctors, "Id", "FullName", filter.DoctorId),
                Specialties = new SelectList(specialties, "Id", "Name", filter.SpecialtyId)
            };
            return View(model);
        }
    }
}