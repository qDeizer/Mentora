using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PsikologProje_Void.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(ApplicationDbContext context, UserManager<User> userManager, ILogger<AppointmentService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task UpdateExpiredAppointmentsAsync()
        {
            TimeZoneInfo turkeyTimeZone;
            try
            {
                // Windows için saat dilimi ID'si
                turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Linux/macOS için saat dilimi ID'si
                turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }

            // Mevcut UTC zamanını Türkiye saatine çeviriyoruz
            var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

            // Veritabanındaki bitiş zamanı, şu anki Türkiye saatinden küçük olan randevuları buluyoruz.
            // Bu, saat dilimi farkını ortadan kaldırır.
            var appointmentsToUpdate = await _context.Appointments
                .Where(a => a.EndTime < turkeyNow && (a.Status == AppointmentStatus.Available || a.Status == AppointmentStatus.Reserved))
                .ToListAsync();

            if (!appointmentsToUpdate.Any()) return;

            foreach (var appointment in appointmentsToUpdate)
            {
                if (appointment.Status == AppointmentStatus.Reserved)
                {
                    appointment.Status = AppointmentStatus.Completed;
                }
                else if (appointment.Status == AppointmentStatus.Available)
                {
                    appointment.Status = AppointmentStatus.NotCompleted;

                    var pendingRequests = await _context.AppointmentRequests
                        .Where(r => r.AppointmentId == appointment.Id && r.Status == RequestStatus.Pending)
                        .ToListAsync();

                    foreach (var request in pendingRequests)
                    {
                        request.Status = RequestStatus.Rejected;
                        request.ResponseMessage = "Randevu gerçekleşmedi";
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet süresi geçmiş randevunun durumu güncellendi.", appointmentsToUpdate.Count);
        }


        public async Task<IEnumerable<AppointmentViewModel>> GetAppointmentsAsync(AppointmentFilterModel filter, ClaimsPrincipal requester)
        {
            await UpdateExpiredAppointmentsAsync();

            var userId = _userManager.GetUserId(requester);
            var activeUserRequestAppointmentIdsList = await _context.AppointmentRequests
                .Where(r => r.PatientId == userId && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved))
                .Select(r => r.AppointmentId)
                .ToListAsync();
            var activeUserRequestAppointmentIds = new HashSet<int>(activeUserRequestAppointmentIdsList);

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.AppointmentSpecialties)
                .ThenInclude(aps => aps.Specialty)
                .AsQueryable();

            // Eğer arayan kişi bir doktor değilse veya kendi randevularına bakmıyorsa sadece "Müsait" olanları göster
            if (string.IsNullOrEmpty(filter.DoctorId) || !_userManager.GetUserId(requester).Equals(filter.DoctorId))
            {
                query = query.Where(a => a.Status == AppointmentStatus.Available);
            }

            Point? requesterLocation = null;
            if (filter.UseProfileLocation)
            {
                var user = await _userManager.GetUserAsync(requester);
                if (user?.Location != null)
                {
                    requesterLocation = user.Location;
                }
            }
            else
            {
                if (filter.Latitude.HasValue && filter.Longitude.HasValue)
                {
                    requesterLocation = new Point(filter.Longitude.Value, filter.Latitude.Value) { SRID = 4326 };
                }
            }

            if (!string.IsNullOrEmpty(filter.DoctorId))
                query = query.Where(a => a.DoctorId == filter.DoctorId);
            if (filter.SpecialtyId.HasValue)
                query = query.Where(a => a.AppointmentSpecialties.Any(aps => aps.SpecialtyId == filter.SpecialtyId.Value));
            if (filter.MinPrice.HasValue)
                query = query.Where(a => a.MaxPrice >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(a => a.MinPrice <= filter.MaxPrice.Value);
            if (filter.StartDate.HasValue)
                query = query.Where(a => a.StartTime.Date >= filter.StartDate.Value.Date);
            if (filter.EndDate.HasValue)
                query = query.Where(a => a.StartTime.Date <= filter.EndDate.Value.Date);
            if (filter.IsOnline != filter.IsInPerson)
            {
                if (filter.IsOnline) query = query.Where(a => a.IsOnline);
                else query = query.Where(a => a.IsInPerson);
            }

            if (requesterLocation != null && filter.DistanceKm.HasValue && filter.DistanceKm > 0)
            {
                query = query.Where(a => a.IsInPerson && a.Location != null && a.Location.Distance(requesterLocation) <= filter.DistanceKm.Value * 1000);
            }

            var projectedQuery = query.Select(a => new
            {
                Appointment = a,
                Doctor = a.Doctor,
                Patient = a.Patient,
                AppointmentSpecialties = a.AppointmentSpecialties,
                DistanceMeters = requesterLocation != null && a.Location != null ? a.Location.Distance(requesterLocation) : (double?)null
            });
            var results = await projectedQuery.OrderByDescending(r => r.Appointment.StartTime).ToListAsync();

            var viewModels = results.Select(r =>
            {
                var a = r.Appointment;
                var types = new List<string>();
                if (a.IsOnline) types.Add("Online");
                if (a.IsInPerson) types.Add("Yüz Yüze");

                return new AppointmentViewModel
                {
                    Id = a.Id,
                    DoctorName = $"{r.Doctor.FirstName} {r.Doctor.LastName}",
                    DoctorId = a.DoctorId,
                    DoctorProfilePhotoPath = r.Doctor.ProfilePhotoPath,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "-",
                    PatientProfileImageUrl = r.Patient?.ProfilePhotoPath,
                    DoctorAverageRating = _context.Appointments.Where(app => app.DoctorId == a.DoctorId && app.Status == AppointmentStatus.Completed && app.Rating.HasValue).Average(app => (double?)app.Rating) ?? 0,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    DurationInMinutes = (int)(a.EndTime - a.StartTime).TotalMinutes,
                    PriceRange = (a.MinPrice.HasValue || a.MaxPrice.HasValue) ? $"{a.MinPrice:C} - {a.MaxPrice:C}" : "-",
                    Status = GetStatusInTurkish(a.Status),
                    AppointmentTypes = types,
                    Notes = !string.IsNullOrEmpty(a.Notes) ? a.Notes : "-",
                    Specialties = r.AppointmentSpecialties.Select(aps => aps.Specialty.Name ?? "").ToList(),
                    Location = a.Location,
                    DistanceKm = r.DistanceMeters / 1000,
                    IsRequestedByCurrentUser = activeUserRequestAppointmentIds.Contains(a.Id)
                };
            }).ToList();
            return viewModels;
        }

        public async Task<bool> DeleteAppointmentAsync(int appointmentId, string doctorId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctorId);
            if (appointment == null)
            {
                return false;
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateAppointmentAsync(CreateAppointmentViewModel model, ClaimsPrincipal userPrincipal)
        {
            var doctor = await _userManager.GetUserAsync(userPrincipal) as Doctor;
            if (doctor == null) return false;

            if (model.IsInPerson && doctor.Location == null)
            {
                return false;
            }

            if (!model.StartTime.HasValue) return false;
            DateTime startTime = model.StartTime.Value;

            TimeZoneInfo turkeyTimeZone;
            try { turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
            catch (TimeZoneNotFoundException) { turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
            var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

            if (startTime < turkeyNow) return false;

            DateTime endTime;
            if (model.EndTime.HasValue) endTime = model.EndTime.Value;
            else if (model.DurationInMinutes.HasValue) endTime = startTime.AddMinutes(model.DurationInMinutes.Value);
            else return false;

            if (endTime <= startTime) return false;
            var appointment = new Appointment
            {
                DoctorId = doctor.Id,
                StartTime = startTime,
                EndTime = endTime,
                MinPrice = model.MinPrice,
                MaxPrice = model.MaxPrice,
                IsOnline = model.IsOnline,
                IsInPerson = model.IsInPerson,
                Notes = model.Notes,
                Status = AppointmentStatus.Available,
                Location = model.IsInPerson ? doctor.Location : null
            };
            if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Any())
            {
                foreach (var specialtyId in model.SelectedSpecialtyIds)
                {
                    appointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = specialtyId });
                }
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetStatusInTurkish(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Available => "Müsait",
                AppointmentStatus.Reserved => "Rezerve Edildi",
                AppointmentStatus.Completed => "Tamamlandı",
                AppointmentStatus.NotCompleted => "Gerçekleşmedi",
                _ => status.ToString()
            };
        }
    }
}