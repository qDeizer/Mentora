using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.Services.Upload;
using PsikologProje_Void.ViewModels;
using PsikologProje_Void.Services.EmailVerification;
using System.Globalization;
using System.Text;

namespace PsikologProje_Void.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileValidationService _fileValidationService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEmailOutboxService _emailOutboxService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IFileValidationService fileValidationService,
            IImageProcessingService imageProcessingService,
            IFileStorageService fileStorageService,
            IEmailOutboxService emailOutboxService,
            IEmailVerificationService emailVerificationService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _fileValidationService = fileValidationService;
            _imageProcessingService = imageProcessingService;
            _fileStorageService = fileStorageService;
            _emailOutboxService = emailOutboxService;
            _emailVerificationService = emailVerificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Specialties = await _context.Specialties.ToListAsync();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullaniliyor.");
                return View(model);
            }

            var normalizedUserName = model.UserName.Trim();
            var existingUserByUserName = await _userManager.FindByNameAsync(normalizedUserName);
            if (existingUserByUserName != null)
            {
                ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten kullanılıyor.");
                return View(model);
            }

            var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (existingUserByPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "Bu telefon numarasi zaten kullaniliyor.");
                return View(model);
            }

            Point? userLocation = null;
            if (!string.IsNullOrEmpty(model.Latitude) && !string.IsNullOrEmpty(model.Longitude))
            {
                if (double.TryParse(model.Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(model.Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    userLocation = new Point(Math.Round(lon, 6), Math.Round(lat, 6))
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
                    UserName = normalizedUserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BirthDate = model.BirthDate,
                    Gender = model.Gender,
                    Location = userLocation,
                    About = model.About,
                    UserType = model.UserType,
                    IsApproved = false,
                    ExperienceStartDate = model.ExperienceStartDate ?? DateTime.Now,
                    Title = model.Title ?? DoctorTitle.Psychologist,
                    University = model.University ?? string.Empty
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
                    var validFiles = new List<(IFormFile File, FileValidationResult Validation)>();
                    foreach (var certFile in model.Certificates.Where(f => f.Length > 0))
                    {
                        var validation = await _fileValidationService.ValidateAsync(certFile, UploadCategory.Certificate);
                        if (!validation.IsValid)
                        {
                            ModelState.AddModelError("Certificates", validation.ErrorMessage ?? "Sertifika dosyasi gecersiz.");
                            return View(model);
                        }

                        validFiles.Add((certFile, validation));
                    }

                    foreach (var item in validFiles)
                    {
                        await using var stream = item.File.OpenReadStream();
                        var certPath = await _fileStorageService.SaveAsync("certificates", item.Validation.NormalizedExtension, stream);
                        doctor.Certificates.Add(new DoctorCertificate
                        {
                            CertificateImagePath = certPath
                        });
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
                    UserName = normalizedUserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BirthDate = model.BirthDate,
                    Gender = model.Gender,
                    Location = userLocation,
                    About = model.About,
                    UserType = model.UserType,
                    IsApproved = true
                };
            }

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                var validation = await _fileValidationService.ValidateAsync(model.ProfilePhoto, UploadCategory.ProfilePhoto);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("ProfilePhoto", validation.ErrorMessage ?? "Profil fotografi gecersiz.");
                    return View(model);
                }

                try
                {
                    var normalizedProfilePhoto = await _imageProcessingService.NormalizeProfilePhotoAsync(model.ProfilePhoto);
                    user.ProfilePhotoPath = await _fileStorageService.SaveAsync("profiles", ".jpg", normalizedProfilePhoto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profil fotoğrafı işlenirken hata oluştu.");
                    ModelState.AddModelError("ProfilePhoto", "Profil fotografisi islenemedi.");
                    return View(model);
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var role = user.UserType.ToString();
                if (await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                await _emailVerificationService.SendVerificationCodeAsync(user);
                TempData["SuccessMessage"] = "Hesabınız oluşturuldu. Lütfen e-posta adresinize gönderilen 6 haneli kodu giriniz.";
                return RedirectToAction("VerifyEmail", "Account", new { email = user.Email });
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
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid && model.Email != null && model.Password != null)
            {
                var loginIdentifier = model.Email.Trim();
                var user = loginIdentifier.Contains("@")
                    ? await _userManager.FindByEmailAsync(loginIdentifier)
                    : await _userManager.FindByNameAsync(loginIdentifier);

                var userNameForSignIn = user?.UserName;
                if (string.IsNullOrWhiteSpace(userNameForSignIn))
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }

                var passwordOk = user != null && await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordOk || user == null)
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    TempData["WarningMessage"] = "Giriş yapabilmek için e-postanızı doğrulamalısınız.";
                    return RedirectToAction("VerifyEmail", "Account", new { email = user.Email });
                }

                var preference = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                var twoFactorEnabled = preference?.TwoFactorViaEmailEnabled == true;

                if (twoFactorEnabled && !string.IsNullOrWhiteSpace(user.Email))
                {
                    await _emailVerificationService.SendVerificationCodeAsync(user);
                    HttpContext.Session.SetString("mentora.pending2fa.userId", user.Id);
                    HttpContext.Session.SetString("mentora.pending2fa.remember", model.RememberMe ? "1" : "0");
                    HttpContext.Session.SetString("mentora.pending2fa.returnUrl", returnUrl ?? string.Empty);
                    TempData["SuccessMessage"] = "E-posta adresinize bir doğrulama kodu gönderdik. Lütfen aşağıya girin.";
                    return RedirectToAction(nameof(Login2FA));
                }

                var result = await _signInManager.PasswordSignInAsync(userNameForSignIn, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login2FA()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("mentora.pending2fa.userId")))
            {
                return RedirectToAction(nameof(Login));
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login2FA(string code)
        {
            var pendingUserId = HttpContext.Session.GetString("mentora.pending2fa.userId");
            if (string.IsNullOrEmpty(pendingUserId))
            {
                TempData["ErrorMessage"] = "Oturum süresi doldu. Lütfen tekrar giriş yapın.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(pendingUserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, "Doğrulama kodu zorunludur.");
                return View();
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code.Trim());
            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Doğrulama kodu yanlış veya süresi dolmuş.");
                return View();
            }

            var rememberMe = HttpContext.Session.GetString("mentora.pending2fa.remember") == "1";
            var returnUrl = HttpContext.Session.GetString("mentora.pending2fa.returnUrl");

            HttpContext.Session.Remove("mentora.pending2fa.userId");
            HttpContext.Session.Remove("mentora.pending2fa.remember");
            HttpContext.Session.Remove("mentora.pending2fa.returnUrl");

            await _signInManager.SignInAsync(user, isPersistent: rememberMe);
            return RedirectToLocal(string.IsNullOrEmpty(returnUrl) ? null : returnUrl);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var model = new ProfileEditViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                About = user.About,
                ExistingProfilePhotoPath = user.ProfilePhotoPath,
                Email = user.Email ?? string.Empty,
                IsEmailConfirmed = user.EmailConfirmed,
                Latitude = user.Location?.Y.ToString("0.######", CultureInfo.InvariantCulture),
                Longitude = user.Location?.X.ToString("0.######", CultureInfo.InvariantCulture)
            };

            if (user is Doctor doctor)
            {
                model.ExperienceStartDate = doctor.ExperienceStartDate;
                model.Title = doctor.Title;
                model.University = doctor.University;

                model.Certificates = await _context.DoctorCertificates
                    .Where(c => c.DoctorId == doctor.Id)
                    .OrderByDescending(c => c.UploadedAt)
                    .Select(c => new DoctorCertificateItem
                    {
                        Id = c.Id,
                        CertificateImagePath = c.CertificateImagePath ?? string.Empty,
                        Description = c.Description,
                        UploadedAt = c.UploadedAt
                    })
                    .ToListAsync();
            }

            model.ProfileChangeCountInWindow = user.ProfileChangeCountInWindow;
            model.ProfileChangeWindowStartUtc = user.ProfileChangeWindowStartUtc;
            model.ProfileChangeBlockedUntilUtc = user.ProfileChangeBlockedUntilUtc;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> Profile(ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var nowUtc = DateTime.UtcNow;
            if (user.ProfileChangeBlockedUntilUtc.HasValue && user.ProfileChangeBlockedUntilUtc.Value > nowUtc)
            {
                TempData["ErrorMessage"] = $"Çok sık profil değişikliği yaptığınız için güncellemeleriniz {user.ProfileChangeBlockedUntilUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm} saatine kadar geçici olarak engellendi. Lütfen sistem yöneticisi ile iletişime geçiniz.";
                return RedirectToAction(nameof(Profile));
            }

            model.ExistingProfilePhotoPath = user.ProfilePhotoPath;
            model.Email = user.Email ?? string.Empty;
            model.IsEmailConfirmed = user.EmailConfirmed;

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Telefon numarasi zorunludur.");
            }

            var phoneExists = await _context.Users.AnyAsync(u =>
                u.PhoneNumber == model.PhoneNumber &&
                u.Id != user.Id);

            if (phoneExists)
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Bu telefon numarasi baska bir hesap tarafindan kullaniliyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber.Trim();
            user.BirthDate = model.BirthDate;
            user.Gender = model.Gender;
            user.About = model.About?.Trim();

            if (model.ClearLocation)
            {
                user.Location = null;
            }
            else if (!string.IsNullOrWhiteSpace(model.Latitude) || !string.IsNullOrWhiteSpace(model.Longitude))
            {
                if (double.TryParse(model.Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(model.Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    user.Location = new Point(Math.Round(lon, 6), Math.Round(lat, 6))
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

            if (user is Doctor doctor)
            {
                if (model.ExperienceStartDate.HasValue)
                {
                    doctor.ExperienceStartDate = model.ExperienceStartDate.Value;
                }

                if (model.Title.HasValue)
                {
                    doctor.Title = model.Title.Value;
                }

                doctor.University = model.University?.Trim();
            }

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                var validation = await _fileValidationService.ValidateAsync(model.ProfilePhoto, UploadCategory.ProfilePhoto);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError(nameof(model.ProfilePhoto), validation.ErrorMessage ?? "Profil fotografi gecersiz.");
                    return View(model);
                }

                try
                {
                    var normalizedProfilePhoto = await _imageProcessingService.NormalizeProfilePhotoAsync(model.ProfilePhoto);
                    user.ProfilePhotoPath = await _fileStorageService.SaveAsync("profiles", ".jpg", normalizedProfilePhoto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profil fotoğrafı güncellenirken hata oluştu.");
                    ModelState.AddModelError(nameof(model.ProfilePhoto), "Profil fotografisi islenemedi.");
                    return View(model);
                }
            }

            // Profil degisikligi rate limit takibi (1 saatlik pencere, 10 degisiklik sonrasi 24 saat blok)
            const int maxChangesInWindow = 10;
            var windowDuration = TimeSpan.FromHours(1);
            var blockDuration = TimeSpan.FromHours(24);

            if (!user.ProfileChangeWindowStartUtc.HasValue || (nowUtc - user.ProfileChangeWindowStartUtc.Value) > windowDuration)
            {
                user.ProfileChangeWindowStartUtc = nowUtc;
                user.ProfileChangeCountInWindow = 0;
            }

            user.ProfileChangeCountInWindow += 1;
            if (user.ProfileChangeCountInWindow > maxChangesInWindow)
            {
                user.ProfileChangeBlockedUntilUtc = nowUtc.Add(blockDuration);
                _logger.LogWarning("Kullanıcı {UserId} çok fazla profil değişikliği yaptığı için {BlockedUntil} saatine kadar bloklandı.", user.Id, user.ProfileChangeBlockedUntilUtc);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            if (user.ProfileChangeBlockedUntilUtc.HasValue && user.ProfileChangeBlockedUntilUtc.Value > nowUtc)
            {
                TempData["WarningMessage"] = $"Profil bilgileriniz güncellendi ancak çok fazla değişiklik yaptınız; bir sonraki güncelleme {user.ProfileChangeBlockedUntilUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm} saatine kadar engellendi. Sistem yöneticisi ile iletişime geçiniz.";
            }
            else
            {
                TempData["SuccessMessage"] = "Profil bilgileriniz güncellendi.";
            }
            return RedirectToAction(nameof(Profile));
        }

        // Doktorlar icin diploma/sertifika yonetimi
        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> UploadCertificate(IFormFile certificate, string? description, string confirmText)
        {
            if (!string.Equals(confirmText?.Trim(), "EKLE", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Sertifika eklemek icin onay metni dogru girilmemis.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is not Doctor doctor)
            {
                return Unauthorized();
            }

            if (certificate == null || certificate.Length == 0)
            {
                TempData["ErrorMessage"] = "Sertifika dosyasi gerekli.";
                return RedirectToAction(nameof(Profile));
            }

            var validation = await _fileValidationService.ValidateAsync(certificate, UploadCategory.Certificate);
            if (!validation.IsValid)
            {
                TempData["ErrorMessage"] = validation.ErrorMessage ?? "Sertifika dosyasi gecersiz.";
                return RedirectToAction(nameof(Profile));
            }

            await using var stream = certificate.OpenReadStream();
            var certPath = await _fileStorageService.SaveAsync("certificates", validation.NormalizedExtension, stream);

            _context.DoctorCertificates.Add(new DoctorCertificate
            {
                DoctorId = doctor.Id,
                CertificateImagePath = certPath,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sertifika eklendi.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> DeleteCertificate(int certificateId, string confirmText)
        {
            if (!string.Equals(confirmText?.Trim(), "SIL", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Sertifikayi silmek icin onay metni dogru girilmemis.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is not Doctor doctor)
            {
                return Unauthorized();
            }

            var certificate = await _context.DoctorCertificates
                .FirstOrDefaultAsync(c => c.Id == certificateId && c.DoctorId == doctor.Id);

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Sertifika bulunamadi.";
                return RedirectToAction(nameof(Profile));
            }

            _context.DoctorCertificates.Remove(certificate);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sertifika silindi.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { token = encodedToken, email = user.Email },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(callbackUrl) && !string.IsNullOrWhiteSpace(user.Email))
                {
                    await _emailOutboxService.QueueAsync(new EmailMessage
                    {
                        To = user.Email!,
                        Subject = "Mentora - Şifre sıfırlama",
                        HtmlBody = $"<p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p><p><a href=\"{callbackUrl}\">Şifremi sıfırla</a></p>"
                    });
                }
            }

            TempData["SuccessMessage"] = "Eğer e-posta sistemde kayıtlıysa sıfırlama bağlantısı gönderilecektir.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                TempData["ErrorMessage"] = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return RedirectToAction(nameof(Login));
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            }
            catch
            {
                TempData["ErrorMessage"] = "Şifre sıfırlama bağlantısı geçersiz.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new VerifyEmailViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz işlem.");
                return View(model);
            }

            var isVerified = await _emailVerificationService.VerifyCodeAsync(user, model.Code);
            if (isVerified)
            {
                TempData["SuccessMessage"] = "E-posta adresiniz başarıyla doğrulandı. Şimdi giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }

            ModelState.AddModelError(string.Empty, "Girdiğiniz doğrulama kodu hatalı veya süresi dolmuş.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                // If it's an authenticated request from profile
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    email = currentUser.Email ?? string.Empty;
                }
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    return RedirectToAction(nameof(Login));
                }
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Fake success message to prevent user enumeration
                TempData["SuccessMessage"] = "Doğrulama kodu e-posta adresinize gönderildi.";
                return RedirectToAction(nameof(VerifyEmail), new { email = email });
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = "E-posta adresiniz zaten doğrulanmış.";
                return RedirectToAction(nameof(Login));
            }

            await _emailVerificationService.SendVerificationCodeAsync(user);
            TempData["SuccessMessage"] = "Doğrulama kodu e-posta adresinize tekrar gönderildi.";
            
            // If the user is authenticated, redirect them back to their profile, else to VerifyEmail
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Profile));
            }
            
            return RedirectToAction(nameof(VerifyEmail), new { email = email });
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

            return RedirectToAction("Index", "Home");
        }
    }
}
