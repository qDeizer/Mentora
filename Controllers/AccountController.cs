using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;
namespace PsikologProje_Void.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // DÜZELTME: Kullanıcıyı veritabanına eklemeden önce e-posta ve telefon numarasının
            // zaten kullanımda olup olmadığını kontrol et.
            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                return View(model);
            }

            var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (existingUserByPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "Bu telefon numarası zaten kullanılıyor.");
                return View(model);
            }

            Point? userLocation = null;
            if (!string.IsNullOrEmpty(model.Latitude) && !string.IsNullOrEmpty(model.Longitude))
            {
                if (double.TryParse(model.Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(model.Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    userLocation
= new Point(Math.Round(lon, 6), Math.Round(lat, 6))
{
    SRID = 4326
};
                }
                else
                {
                    ModelState.AddModelError("Location", "Geçersiz konum formatı.");
                    return View(model);
                }
            }

            User user;
            if (model.UserType == UserType.Doctor)
            {
                var doctor = new Doctor
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,

                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BirthDate = model.BirthDate,

                    Gender = model.Gender,
                    Location = userLocation,
                    About = model.About,
                    UserType = model.UserType,
                    IsApproved = false,

                    ExperienceStartDate = model.ExperienceStartDate ??
DateTime.Now,
                    Title = model.Title ??
DoctorTitle.Psychologist,
                    University = model.University ??
""
                };
                if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Any())
                {
                    foreach (var specialtyId in model.SelectedSpecialtyIds)
                    {
                        doctor.DoctorSpecialties.Add(new DoctorSpecialty { SpecialtyId = specialtyId });
                    }
                }

                if (model.Certificates != null && model.Certificates.Any())
                {
                    foreach (var certFile in model.Certificates)
                    {

                        var certPath = await ProcessUploadedFile(certFile, "certificates");
                        if (!string.IsNullOrEmpty(certPath))
                        {
                            doctor.Certificates.Add(new DoctorCertificate { CertificateImagePath = certPath });
                        }
                    }
                }
                user = doctor;
            }
            else
            {
                user = new Patient
                {
                    FirstName = model.FirstName,

                    LastName = model.LastName,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BirthDate = model.BirthDate,

                    Gender = model.Gender,
                    Location = userLocation,
                    About = model.About,
                    UserType = model.UserType,
                    IsApproved
= true
                };
            }

            if (model.ProfilePhoto != null)
            {
                user.ProfilePhotoPath = await ProcessUploadedFile(model.ProfilePhoto, "profiles");
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                string role = user.UserType.ToString();
                if (await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid && model.Email != null && model.Password != null)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                return View(model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<string?> ProcessUploadedFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{folder}/{uniqueFileName}";
        }
    }
}