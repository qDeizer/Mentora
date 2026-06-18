using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;
using System.Globalization;
using System.Security.Claims;

namespace PsikologProje_Void.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AppointmentService> _logger;
        private readonly INotificationService _notificationService;

        public AppointmentService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<AppointmentService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task UpdateExpiredAppointmentsAsync()
        {
            var turkeyNow = TimeZoneHelper.GetTurkeyNow();
            await ExpirePendingPrivateOffersAsync();

            var appointmentsToUpdate = await _context.Appointments
                .Where(a => a.EndTime < turkeyNow && (a.Status == AppointmentStatus.Available || a.Status == AppointmentStatus.Reserved))
                .ToListAsync();

            if (appointmentsToUpdate.Count == 0)
            {
                return;
            }

            foreach (var appointment in appointmentsToUpdate)
            {
                if (appointment.Status == AppointmentStatus.Reserved)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    appointment.UpdatedAtUtc = DateTime.UtcNow;
                    continue;
                }

                appointment.Status = AppointmentStatus.NotCompleted;
                appointment.UpdatedAtUtc = DateTime.UtcNow;

                var pendingRequests = await _context.AppointmentRequests
                    .Where(r => r.AppointmentId == appointment.Id && r.Status == RequestStatus.Pending)
                    .ToListAsync();

                foreach (var request in pendingRequests)
                {
                    request.Status = RequestStatus.Rejected;
                    request.ResponseMessage = "Randevu süresi geçtiği için otomatik olarak kapatıldı.";
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet randevu süresi geçtiği için güncellendi.", appointmentsToUpdate.Count);
        }

        public async Task<IEnumerable<AppointmentViewModel>> GetAppointmentsAsync(AppointmentFilterModel filter, ClaimsPrincipal requester)
        {
            await UpdateExpiredAppointmentsAsync();

            var requesterUserId = _userManager.GetUserId(requester);
            var activeUserRequestAppointmentIds = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(requesterUserId))
            {
                var activeRequestIds = await _context.AppointmentRequests
                    .Where(r => r.PatientId == requesterUserId && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved))
                    .Select(r => r.AppointmentId)
                    .ToListAsync();

                activeUserRequestAppointmentIds = new HashSet<int>(activeRequestIds);
            }

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.TargetPatient)
                .Include(a => a.AppointmentSpecialties)
                .ThenInclude(aps => aps.Specialty)
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(filter.DoctorId) || requesterUserId != filter.DoctorId)
            {
                query = query.Where(a =>
                    a.Status == AppointmentStatus.Available &&
                    !a.IsPrivateOffer);
            }

            Point? requesterLocation = null;
            if (filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                requesterLocation = new Point(filter.Longitude.Value, filter.Latitude.Value) { SRID = 4326 };
            }

            var selectedDoctorIds = (filter.SelectedDoctorIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            if (!string.IsNullOrWhiteSpace(filter.DoctorId) && !selectedDoctorIds.Contains(filter.DoctorId))
            {
                selectedDoctorIds.Add(filter.DoctorId);
            }

            if (selectedDoctorIds.Count > 0)
            {
                query = query.Where(a => selectedDoctorIds.Contains(a.DoctorId));
            }

            var selectedSpecialtyIds = (filter.SelectedSpecialtyIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (filter.SpecialtyId.HasValue && !selectedSpecialtyIds.Contains(filter.SpecialtyId.Value))
            {
                selectedSpecialtyIds.Add(filter.SpecialtyId.Value);
            }

            if (selectedSpecialtyIds.Count > 0)
            {
                query = query.Where(a => a.AppointmentSpecialties.Any(aps => selectedSpecialtyIds.Contains(aps.SpecialtyId)));
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(a => a.MaxPrice >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(a => a.MinPrice <= filter.MaxPrice.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(a => a.StartTime.Date >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(a => a.StartTime.Date <= filter.EndDate.Value.Date);
            }

            if (filter.IsOnline != filter.IsInPerson)
            {
                query = filter.IsOnline
                    ? query.Where(a => a.IsOnline)
                    : query.Where(a => a.IsInPerson);
            }

            if (requesterLocation != null && filter.DistanceKm.HasValue && filter.DistanceKm > 0)
            {
                query = query.Where(a => a.IsInPerson && a.Location != null && a.Location.Distance(requesterLocation) <= filter.DistanceKm.Value * 1000);
            }

            var ratings = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.Rating.HasValue)
                .GroupBy(a => a.DoctorId)
                .Select(g => new { g.Key, Avg = g.Average(x => (double?)x.Rating) ?? 0 })
                .ToDictionaryAsync(x => x.Key, x => x.Avg);

            var appointments = await query
                .Select(a => new
                {
                    Appointment = a,
                    Doctor = a.Doctor,
                    Patient = a.Patient,
                    Specialties = a.AppointmentSpecialties,
                    DistanceMeters = requesterLocation != null && a.Location != null ? a.Location.Distance(requesterLocation) : (double?)null
                })
                .ToListAsync();

            if (filter.SelectedDays != null && filter.SelectedDays.Count > 0)
            {
                var selectedDays = filter.SelectedDays
                    .Where(day => day >= 0 && day <= 6)
                    .Distinct()
                    .ToHashSet();

                appointments = appointments
                    .Where(item => selectedDays.Contains((int)item.Appointment.StartTime.DayOfWeek))
                    .ToList();
            }

            var viewModels = appointments.Select(item =>
            {
                var appointment = item.Appointment;
                var appointmentTypes = new List<string>();
                if (appointment.IsOnline) appointmentTypes.Add("Çevrim içi");
                if (appointment.IsInPerson) appointmentTypes.Add("Yüz yüze");

                var avgRating = ratings.TryGetValue(appointment.DoctorId, out var value) ? value : 0;

                return new AppointmentViewModel
                {
                    Id = appointment.Id,
                    DoctorName = item.Doctor.FirstName + " " + item.Doctor.LastName,
                    DoctorId = appointment.DoctorId,
                    DoctorProfilePhotoPath = item.Doctor.ProfilePhotoPath,
                    DoctorAverageRating = avgRating,
                    PatientName = item.Patient != null ? item.Patient.FirstName + " " + item.Patient.LastName : "-",
                    PatientId = item.Patient?.Id,
                    PatientProfileImageUrl = item.Patient?.ProfilePhotoPath,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    DurationInMinutes = (int)(appointment.EndTime - appointment.StartTime).TotalMinutes,
                    PriceRange = appointment.MinPrice.HasValue || appointment.MaxPrice.HasValue ? $"{appointment.MinPrice:N2} ₺ - {appointment.MaxPrice:N2} ₺" : "-",
                    Status = GetUserFacingStatus(appointment),
                    AppointmentTypes = appointmentTypes,
                    MeetingLink = appointment.PatientId == requesterUserId || appointment.DoctorId == requesterUserId ? appointment.MeetingLink : null,
                    Notes = string.IsNullOrWhiteSpace(appointment.Notes) ? "-" : appointment.Notes,
                    Specialties = item.Specialties.Select(s => s.Specialty.Name ?? string.Empty).ToList(),
                    Location = appointment.Location,
                    LocationNote = appointment.LocationNote,
                    DistanceKm = item.DistanceMeters.HasValue ? item.DistanceMeters.Value / 1000 : null,
                    IsRequestedByCurrentUser = activeUserRequestAppointmentIds.Contains(appointment.Id),
                    Rating = appointment.Rating,
                    Review = appointment.Review,
                    RelativeDayLabel = BuildRelativeDayLabel(appointment.StartTime),
                    IsPrivateOffer = appointment.IsPrivateOffer,
                    OfferStatus = appointment.OfferStatus,
                    TargetPatientId = appointment.TargetPatientId,
                    OfferNoteFromDoctor = appointment.OfferNoteFromDoctor,
                    OfferResponseNoteFromPatient = appointment.OfferResponseNoteFromPatient
                };
            });

            var sorted = ApplySorting(viewModels, filter);
            return sorted;
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

        public async Task<ServiceResult> CreateAppointmentAsync(CreateAppointmentViewModel model, ClaimsPrincipal userPrincipal)
        {
            var doctor = await _userManager.GetUserAsync(userPrincipal) as Doctor;
            if (doctor == null)
            {
                return ServiceResult.Failure("Doktor bilgisi bulunamadı.");
            }

            var isPrivateOffer = model.IsPrivateOffer;
            if (isPrivateOffer && string.IsNullOrWhiteSpace(model.TargetPatientId))
            {
                return ServiceResult.Failure("Özel randevu için hedef hasta seçmelisiniz.");
            }

            if (isPrivateOffer && model.TargetPatientId == doctor.Id)
            {
                return ServiceResult.Failure("Hedef hasta seçimi geçersiz.");
            }

            if (isPrivateOffer)
            {
                var hasRelationship = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == doctor.Id &&
                    a.PatientId == model.TargetPatientId);

                if (!hasRelationship)
                {
                    var hasRequestRelation = await _context.AppointmentRequests.AnyAsync(r =>
                        r.DoctorId == doctor.Id && r.PatientId == model.TargetPatientId);

                    if (!hasRequestRelation)
                    {
                        return ServiceResult.Failure("Hedef hastayla aktif/geçmiş bağlantı bulunamadı.");
                    }
                }
            }

            if (!model.IsOnline && !model.IsInPerson)
            {
                return ServiceResult.Failure("En az bir randevu türü seçmelisiniz.");
            }

            var meetingLink = NormalizeMeetingLink(model.MeetingLink);
            if (model.IsOnline && !string.IsNullOrWhiteSpace(model.MeetingLink) && string.IsNullOrWhiteSpace(meetingLink))
            {
                return ServiceResult.Failure("Çevrim içi görüşme linki geçerli bir http/https bağlantısı olmalıdır.");
            }

            Point? inPersonLocation = null;
            if (model.IsInPerson)
            {
                var locationMode = (model.InPersonLocationMode ?? "profile").Trim().ToLowerInvariant();
                if (locationMode == "profile")
                {
                    if (doctor.Location == null)
                    {
                        return ServiceResult.Failure("Profil konumu seçildiği için doktor profilinde konum zorunludur.");
                    }

                    inPersonLocation = new Point(doctor.Location.X, doctor.Location.Y) { SRID = 4326 };
                }
                else
                {
                    if (!model.TryGetInPersonCoordinates(out var latitude, out var longitude))
                    {
                        return ServiceResult.Failure("Seçilen konum kaynağı için geçerli enlem ve boylam gereklidir.");
                    }

                    inPersonLocation = new Point(Math.Round(longitude, 6), Math.Round(latitude, 6)) { SRID = 4326 };
                }
            }

            if (!model.StartTime.HasValue)
            {
                return ServiceResult.Failure("Başlangıç saati zorunludur.");
            }

            var startTime = model.StartTime.Value;
            var turkeyNow = TimeZoneHelper.GetTurkeyNow();
            if (startTime < turkeyNow)
            {
                return ServiceResult.Failure("Geçmiş tarihli randevu oluşturamazsınız.");
            }

            DateTime endTime;
            if (model.EndTime.HasValue)
            {
                endTime = model.EndTime.Value;
            }
            else if (model.DurationInMinutes.HasValue)
            {
                endTime = startTime.AddMinutes(model.DurationInMinutes.Value);
            }
            else
            {
                return ServiceResult.Failure("Bitiş saati veya süre bilgisi zorunludur.");
            }

            if (endTime <= startTime)
            {
                return ServiceResult.Failure("Bitiş saati başlangıç saatinden büyük olmalıdır.");
            }

            var hasDoctorConflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctor.Id &&
                (a.Status == AppointmentStatus.Available || a.Status == AppointmentStatus.Reserved) &&
                a.StartTime < endTime &&
                a.EndTime > startTime);

            if (hasDoctorConflict)
            {
                return ServiceResult.Failure("Bu saatler arasında mevcut bir randevunuz var.");
            }

            if (model.MinPrice.HasValue && model.MaxPrice.HasValue && model.MaxPrice < model.MinPrice)
            {
                return ServiceResult.Failure("Maksimum fiyat minimum fiyattan küçük olamaz.");
            }

            var appointment = new Appointment
            {
                DoctorId = doctor.Id,
                StartTime = startTime,
                EndTime = endTime,
                MinPrice = model.MinPrice,
                MaxPrice = model.MaxPrice,
                IsOnline = model.IsOnline,
                IsInPerson = model.IsInPerson,
                MeetingLink = model.IsOnline ? meetingLink : null,
                Notes = model.Notes,
                Status = AppointmentStatus.Available,
                Location = model.IsInPerson ? inPersonLocation : null,
                LocationNote = string.IsNullOrWhiteSpace(model.LocationNote) ? null : model.LocationNote.Trim(),
                IsPrivateOffer = isPrivateOffer,
                TargetPatientId = isPrivateOffer ? model.TargetPatientId : null,
                OfferStatus = isPrivateOffer ? AppointmentOfferStatus.Pending : AppointmentOfferStatus.None,
                OfferNoteFromDoctor = isPrivateOffer ? model.OfferNoteFromDoctor?.Trim() : null,
                OfferExpiresAtUtc = isPrivateOffer ? startTime.ToUniversalTime().AddHours(-2) : null,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            if (model.SelectedSpecialtyIds != null)
            {
                foreach (var specialtyId in model.SelectedSpecialtyIds.Distinct())
                {
                    appointment.AppointmentSpecialties.Add(new Appointment_Specialty
                    {
                        SpecialtyId = specialtyId
                    });
                }
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            if (isPrivateOffer && !string.IsNullOrWhiteSpace(model.TargetPatientId))
            {
                await _notificationService.CreateAsync(
                    model.TargetPatientId,
                    NotificationType.PrivateOffer,
                    "Yeni özel randevu teklifi",
                    $"{doctor.FirstName} {doctor.LastName} size {startTime:dd.MM.yyyy HH:mm} için özel randevu teklif etti.",
                    "/PrivateAppointments");
            }

            return ServiceResult.Success();
        }

        public async Task<bool> HasScheduleConflictForPatientAsync(string patientId, DateTime startTime, DateTime endTime, int? ignoredAppointmentId = null)
        {
            var query = _context.Appointments.Where(a =>
                a.PatientId == patientId &&
                a.Status == AppointmentStatus.Reserved &&
                a.StartTime < endTime &&
                a.EndTime > startTime);

            if (ignoredAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != ignoredAppointmentId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<AppointmentViewModel>> GetPrivateOffersForPatientAsync(string patientId)
        {
            var offers = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.AppointmentSpecialties)
                .ThenInclude(aps => aps.Specialty)
                .Where(a =>
                    a.IsPrivateOffer &&
                    a.TargetPatientId == patientId &&
                    a.OfferStatus == AppointmentOfferStatus.Pending &&
                    a.Status == AppointmentStatus.Available &&
                    a.StartTime > TimeZoneHelper.GetTurkeyNow())
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            return offers.Select(a => new AppointmentViewModel
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                DoctorProfilePhotoPath = a.Doctor.ProfilePhotoPath,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                DurationInMinutes = (int)(a.EndTime - a.StartTime).TotalMinutes,
                PriceRange = (a.MinPrice.HasValue || a.MaxPrice.HasValue) ? $"{a.MinPrice:N2} ₺ - {a.MaxPrice:N2} ₺" : "-",
                Status = GetStatusInTurkish(a.Status),
                AppointmentTypes = new List<string> { a.IsOnline ? "Çevrim içi" : string.Empty, a.IsInPerson ? "Yüz yüze" : string.Empty }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList(),
                MeetingLink = null,
                Notes = a.Notes,
                Specialties = a.AppointmentSpecialties.Select(s => s.Specialty.Name ?? string.Empty).ToList(),
                Location = a.Location,
                LocationNote = a.LocationNote,
                IsPrivateOffer = true,
                OfferStatus = a.OfferStatus,
                OfferNoteFromDoctor = a.OfferNoteFromDoctor,
                RelativeDayLabel = BuildRelativeDayLabel(a.StartTime)
            });
        }

        public Task<int> CountPendingPrivateOffersAsync(string patientId)
        {
            return _context.Appointments.CountAsync(a =>
                a.IsPrivateOffer &&
                a.TargetPatientId == patientId &&
                a.OfferStatus == AppointmentOfferStatus.Pending &&
                a.Status == AppointmentStatus.Available &&
                a.StartTime > TimeZoneHelper.GetTurkeyNow());
        }

        public async Task<ServiceResult> RespondPrivateOfferAsync(int appointmentId, string patientId, bool accept, string? responseMessage)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null || !appointment.IsPrivateOffer || appointment.TargetPatientId != patientId)
            {
                return ServiceResult.Failure("Özel randevu teklifi bulunamadı.");
            }

            if (appointment.OfferStatus != AppointmentOfferStatus.Pending || appointment.Status != AppointmentStatus.Available)
            {
                return ServiceResult.Failure("Bu teklif artık beklemede değil.");
            }

            if (appointment.OfferExpiresAtUtc.HasValue && DateTime.UtcNow >= appointment.OfferExpiresAtUtc.Value)
            {
                appointment.OfferStatus = AppointmentOfferStatus.Expired;
                appointment.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return ServiceResult.Failure("Teklifin süresi doldu.");
            }

            if (accept)
            {
                var conflict = await HasScheduleConflictForPatientAsync(patientId, appointment.StartTime, appointment.EndTime);
                if (conflict)
                {
                    return ServiceResult.Failure("Bu saatler arasında zaten başka randevunuz var.");
                }

                appointment.PatientId = patientId;
                appointment.Status = AppointmentStatus.Reserved;
                appointment.OfferStatus = AppointmentOfferStatus.Accepted;
            }
            else
            {
                appointment.OfferStatus = AppointmentOfferStatus.Rejected;
            }

            appointment.OfferResponseNoteFromPatient = string.IsNullOrWhiteSpace(responseMessage) ? null : responseMessage.Trim();
            appointment.OfferRespondedAtUtc = DateTime.UtcNow;
            appointment.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var requestRecord = new AppointmentRequest
            {
                AppointmentId = appointment.Id,
                DoctorId = appointment.DoctorId,
                PatientId = patientId,
                RequestMessage = appointment.OfferNoteFromDoctor,
                ResponseMessage = appointment.OfferResponseNoteFromPatient,
                Status = accept ? RequestStatus.Approved : RequestStatus.Rejected,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppointmentRequests.Add(requestRecord);
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                appointment.DoctorId,
                NotificationType.PrivateOfferResponse,
                "Özel randevuya yanıt geldi",
                $"Hasta teklifi {(accept ? "kabul etti" : "reddetti")}: {appointment.StartTime:dd.MM.yyyy HH:mm}",
                "/Request");

            return ServiceResult.Success();
        }

        public async Task<int> ExpirePendingPrivateOffersAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var offers = await _context.Appointments
                .Where(a =>
                    a.IsPrivateOffer &&
                    a.OfferStatus == AppointmentOfferStatus.Pending &&
                    a.Status == AppointmentStatus.Available &&
                    a.OfferExpiresAtUtc.HasValue &&
                    a.OfferExpiresAtUtc <= now)
                .ToListAsync(cancellationToken);

            if (offers.Count == 0)
            {
                return 0;
            }

            foreach (var offer in offers)
            {
                offer.OfferStatus = AppointmentOfferStatus.Expired;
                offer.UpdatedAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return offers.Count;
        }

        private static IEnumerable<AppointmentViewModel> ApplySorting(IEnumerable<AppointmentViewModel> source, AppointmentFilterModel filter)
        {
            var sortBy = filter.SortBy?.ToLowerInvariant();
            var sortDirection = filter.SortDirection?.ToLowerInvariant();
            var isDescending = sortDirection == "desc";

            return sortBy switch
            {
                "price" => isDescending
                    ? source.OrderByDescending(a => ExtractPriceForSorting(a.PriceRange))
                    : source.OrderBy(a => ExtractPriceForSorting(a.PriceRange)),
                "rating" => isDescending
                    ? source.OrderByDescending(a => a.DoctorAverageRating)
                    : source.OrderBy(a => a.DoctorAverageRating),
                "doctor" => isDescending
                    ? source.OrderByDescending(a => a.DoctorName)
                    : source.OrderBy(a => a.DoctorName),
                "distance" => isDescending
                    ? source.OrderByDescending(a => a.DistanceKm ?? double.MaxValue)
                    : source.OrderBy(a => a.DistanceKm ?? double.MaxValue),
                "date" => isDescending
                    ? source.OrderByDescending(a => a.StartTime)
                    : source.OrderBy(a => a.StartTime),
                _ => source.OrderBy(a => a.StartTime)
            };
        }

        private static decimal ExtractPriceForSorting(string priceRange)
        {
            if (string.IsNullOrWhiteSpace(priceRange) || priceRange == "-")
            {
                return decimal.MaxValue;
            }

            var clean = priceRange.Split('-')[0].Trim();
            var digits = new string(clean.Where(ch => char.IsDigit(ch) || ch == ',' || ch == '.').ToArray());

            if (decimal.TryParse(digits, out var value))
            {
                return value;
            }

            return decimal.MaxValue;
        }

        private static string GetStatusInTurkish(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Available => "Müsait",
                AppointmentStatus.Reserved => "Rezerve edildi",
                AppointmentStatus.Completed => "Tamamlandı",
                AppointmentStatus.NotCompleted => "Gerçekleşmedi",
                AppointmentStatus.CancelledByConflict => "Çakışma nedeniyle kapatıldı",
                _ => status.ToString()
            };
        }

        public static string GetUserFacingStatus(Appointment appointment)
        {
            if (appointment.IsPrivateOffer)
            {
                return appointment.OfferStatus switch
                {
                    AppointmentOfferStatus.Pending => "Hasta Onayı Bekliyor",
                    AppointmentOfferStatus.Accepted => "Onaylandı",
                    AppointmentOfferStatus.Rejected => "Reddedildi",
                    AppointmentOfferStatus.Expired => "Süresi Doldu",
                    _ => GetStatusInTurkish(appointment.Status)
                };
            }

            return GetStatusInTurkish(appointment.Status);
        }

        private static string BuildRelativeDayLabel(DateTime startTime)
        {
            var culture = new CultureInfo("tr-TR");
            var today = TimeZoneHelper.GetTurkeyNow().Date;
            var target = startTime.Date;
            var dayDiff = (target - today).Days;

            return dayDiff switch
            {
                0 => "Bugün",
                1 => "Yarın",
                2 => "2 gün sonra",
                >= 3 and <= 6 => target.ToString("dddd", culture),
                >= 7 and <= 13 => $"Haftaya {target.ToString("dddd", culture)}",
                _ => target.ToString("dddd", culture)
            };
        }

        private static string? NormalizeMeetingLink(string? rawLink)
        {
            if (string.IsNullOrWhiteSpace(rawLink))
            {
                return null;
            }

            var trimmed = rawLink.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return uri.Scheme is "http" or "https" ? trimmed : null;
        }
    }
}
