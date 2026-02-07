using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries; // YENİ EKLENDİ
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using System.Security.Claims;
namespace PsikologProje_Void.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DoctorViewModel>> GetDoctorsAsync(DoctorFilterModel filter, ClaimsPrincipal requester)
        {
            var query = _context.Doctors
                .Where(d => d.IsApproved)
                .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)

                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
            {
                query = query.Where(d => (d.FirstName + " " + d.LastName).Contains(filter.Name));
            }
            if (filter.SpecialtyId.HasValue)
            {
                query = query.Where(d => d.DoctorSpecialties.Any(ds => ds.SpecialtyId == filter.SpecialtyId.Value));
            }
            if (filter.Title.HasValue)
            {
                query = query.Where(d => d.Title == filter.Title.Value);
            }
            if (!string.IsNullOrEmpty(filter.University))
            {
                query = query.Where(d => d.University != null && d.University.Contains(filter.University));
            }

            // HATA CS1061 DÜZELTİLDİ: Mesafe filtrelemesi NetTopologySuite kullanacak şekilde güncellendi.
            if (filter.Latitude.HasValue && filter.Longitude.HasValue && filter.DistanceKm.HasValue)
            {
                var userLocation = new Point(filter.Longitude.Value, filter.Latitude.Value) { SRID = 4326 };
                query = query.Where(d => d.Location != null && d.Location.Distance(userLocation) <= filter.DistanceKm.Value * 1000);
            }

            var doctors = await query.ToListAsync();
            var doctorViewModels = doctors.Select(d =>
            {
                var viewModel = new DoctorViewModel
                {
                    Id = d.Id,
                    FullName = $"{d.FirstName} {d.LastName}",

                    Title = d.Title.ToString(),
                    ProfilePhotoPath = d.ProfilePhotoPath,
                    University = d.University,
                    About = d.About,

                    YearsOfExperience = (DateTime.Now.Year - d.ExperienceStartDate.Year),
                    Specialties = d.DoctorSpecialties.Select(ds => ds.Specialty!.Name ?? "").ToList()
                };

                if (requester.IsInRole("Admin"))
                {

                    viewModel.Email = d.Email;
                    viewModel.PhoneNumber = d.PhoneNumber;
                    viewModel.IsApproved = d.IsApproved;
                }

                return viewModel;
            });
            return doctorViewModels;
        }
    }
}