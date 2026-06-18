using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;
using System.Globalization;

namespace PsikologProje_Void.Services
{
    public class AppointmentAutomationService : IAppointmentAutomationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentAutomationService> _logger;

        public AppointmentAutomationService(ApplicationDbContext context, ILogger<AppointmentAutomationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<AutomationRoutineListItemViewModel>> GetDoctorRoutinesAsync(string doctorId)
        {
            var routines = await _context.AppointmentAutomationRoutines
                .Include(r => r.RoutineSpecialties)
                .ThenInclude(rs => rs.Specialty)
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();

            return routines.Select(MapToListItem).ToList();
        }

        public async Task<AutomationRoutineInputViewModel?> GetRoutineForEditAsync(string doctorId, int routineId)
        {
            var routine = await _context.AppointmentAutomationRoutines
                .Include(r => r.RoutineSpecialties)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.DoctorId == doctorId);

            if (routine == null)
            {
                return null;
            }

            return new AutomationRoutineInputViewModel
            {
                Id = routine.Id,
                Name = routine.Name,
                StartTime = routine.StartTime,
                DurationInMinutes = routine.DurationInMinutes,
                GenerateDaysAhead = routine.GenerateDaysAhead,
                ActiveFrom = routine.ActiveFrom.ToDateTime(TimeOnly.MinValue),
                ActiveUntil = routine.ActiveUntil?.ToDateTime(TimeOnly.MinValue),
                SelectedDays = ToSelectedDayValues(routine.DaysOfWeek),
                IsOnline = routine.IsOnline,
                IsInPerson = routine.IsInPerson,
                MinPrice = routine.MinPrice,
                MaxPrice = routine.MaxPrice,
                Notes = routine.Notes,
                InPersonLocationMode = routine.InPersonLocationMode,
                InPersonLatitude = routine.Location?.Y.ToString("0.######", CultureInfo.InvariantCulture),
                InPersonLongitude = routine.Location?.X.ToString("0.######", CultureInfo.InvariantCulture),
                LocationNote = routine.LocationNote,
                SelectedSpecialtyIds = routine.RoutineSpecialties.Select(s => s.SpecialtyId).ToList()
            };
        }

        public async Task<ServiceResult> CreateRoutineAsync(string doctorId, AutomationRoutineInputViewModel model)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                return ServiceResult.Failure("Doktor bulunamadi.");
            }

            var (routineLocation, locationMode, locationError) = ResolveRoutineLocation(model, doctor.Location);
            if (!string.IsNullOrWhiteSpace(locationError))
            {
                return ServiceResult.Failure(locationError);
            }

            var daysMask = ToDaysMask(model.SelectedDays ?? new List<int>());
            if (daysMask == RoutineWeekDayMask.None)
            {
                return ServiceResult.Failure("En az bir gün seçmelisiniz.");
            }

            var routine = new AppointmentAutomationRoutine
            {
                DoctorId = doctorId,
                Name = model.Name.Trim(),
                StartTime = model.StartTime,
                DurationInMinutes = model.DurationInMinutes,
                GenerateDaysAhead = model.GenerateDaysAhead,
                ActiveFrom = DateOnly.FromDateTime(model.ActiveFrom.Date),
                ActiveUntil = model.ActiveUntil.HasValue ? DateOnly.FromDateTime(model.ActiveUntil.Value.Date) : null,
                DaysOfWeek = daysMask,
                IsOnline = model.IsOnline,
                IsInPerson = model.IsInPerson,
                InPersonLocationMode = locationMode,
                Location = routineLocation,
                LocationNote = string.IsNullOrWhiteSpace(model.LocationNote) ? null : model.LocationNote.Trim(),
                MinPrice = model.MinPrice,
                MaxPrice = model.MaxPrice,
                Notes = model.Notes,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            foreach (var specialtyId in (model.SelectedSpecialtyIds ?? new List<int>()).Distinct())
            {
                routine.RoutineSpecialties.Add(new AppointmentAutomationRoutineSpecialty { SpecialtyId = specialtyId });
            }

            _context.AppointmentAutomationRoutines.Add(routine);
            await _context.SaveChangesAsync();

            try
            {
                routine.Doctor ??= doctor;
                var generated = await GenerateForRoutineInternalAsync(routine, CancellationToken.None);
                if (generated > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni rutin {RoutineId} için anlık randevu üretiminde hata.", routine.Id);
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateRoutineAsync(string doctorId, AutomationRoutineInputViewModel model)
        {
            if (!model.Id.HasValue)
            {
                return ServiceResult.Failure("Rutin kimligi bulunamadi.");
            }

            var routine = await _context.AppointmentAutomationRoutines
                .Include(r => r.RoutineSpecialties)
                .FirstOrDefaultAsync(r => r.Id == model.Id.Value && r.DoctorId == doctorId);

            if (routine == null)
            {
                return ServiceResult.Failure("Rutin bulunamadi.");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                return ServiceResult.Failure("Doktor bulunamadi.");
            }

            var (routineLocation, locationMode, locationError) = ResolveRoutineLocation(model, doctor.Location);
            if (!string.IsNullOrWhiteSpace(locationError))
            {
                return ServiceResult.Failure(locationError);
            }

            var daysMask = ToDaysMask(model.SelectedDays ?? new List<int>());
            if (daysMask == RoutineWeekDayMask.None)
            {
                return ServiceResult.Failure("En az bir gün seçmelisiniz.");
            }

            routine.Name = model.Name.Trim();
            routine.StartTime = model.StartTime;
            routine.DurationInMinutes = model.DurationInMinutes;
            routine.GenerateDaysAhead = model.GenerateDaysAhead;
            routine.ActiveFrom = DateOnly.FromDateTime(model.ActiveFrom.Date);
            routine.ActiveUntil = model.ActiveUntil.HasValue ? DateOnly.FromDateTime(model.ActiveUntil.Value.Date) : null;
            routine.DaysOfWeek = daysMask;
            routine.IsOnline = model.IsOnline;
            routine.IsInPerson = model.IsInPerson;
            routine.InPersonLocationMode = locationMode;
            routine.Location = routineLocation;
            routine.LocationNote = string.IsNullOrWhiteSpace(model.LocationNote) ? null : model.LocationNote.Trim();
            routine.MinPrice = model.MinPrice;
            routine.MaxPrice = model.MaxPrice;
            routine.Notes = model.Notes;
            routine.UpdatedAtUtc = DateTime.UtcNow;

            _context.AppointmentAutomationRoutineSpecialties.RemoveRange(routine.RoutineSpecialties);
            routine.RoutineSpecialties.Clear();
            foreach (var specialtyId in (model.SelectedSpecialtyIds ?? new List<int>()).Distinct())
            {
                routine.RoutineSpecialties.Add(new AppointmentAutomationRoutineSpecialty
                {
                    RoutineId = routine.Id,
                    SpecialtyId = specialtyId
                });
            }

            await _context.SaveChangesAsync();

            try
            {
                routine.Doctor ??= doctor;
                var generated = await GenerateForRoutineInternalAsync(routine, CancellationToken.None);
                if (generated > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncellenen rutin {RoutineId} için anlık randevu üretiminde hata.", routine.Id);
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> PauseRoutineAsync(string doctorId, int routineId, int? pauseDays, DateTime? pauseUntilLocal)
        {
            var routine = await _context.AppointmentAutomationRoutines
                .FirstOrDefaultAsync(r => r.Id == routineId && r.DoctorId == doctorId);

            if (routine == null)
            {
                return ServiceResult.Failure("Rutin bulunamadi.");
            }

            var turkeyNow = TimeZoneHelper.GetTurkeyNow();
            DateTime pauseUntil;
            if (pauseUntilLocal.HasValue)
            {
                pauseUntil = pauseUntilLocal.Value;
            }
            else if (pauseDays.HasValue && pauseDays.Value > 0)
            {
                pauseUntil = turkeyNow.AddDays(pauseDays.Value);
            }
            else
            {
                return ServiceResult.Failure("Duraklatma için gün veya bitiş tarihi belirtmelisiniz.");
            }

            if (pauseUntil <= turkeyNow)
            {
                return ServiceResult.Failure("Duraklatma bitiş tarihi şu andan ileri olmalıdır.");
            }

            var turkeyTz = TimeZoneHelper.GetTurkeyTimeZone();
            var unspecified = DateTime.SpecifyKind(pauseUntil, DateTimeKind.Unspecified);
            routine.PausedUntilUtc = TimeZoneInfo.ConvertTimeToUtc(unspecified, turkeyTz);
            routine.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ResumeRoutineAsync(string doctorId, int routineId)
        {
            var routine = await _context.AppointmentAutomationRoutines
                .FirstOrDefaultAsync(r => r.Id == routineId && r.DoctorId == doctorId);

            if (routine == null)
            {
                return ServiceResult.Failure("Rutin bulunamadi.");
            }

            routine.PausedUntilUtc = null;
            routine.IsEnabled = true;
            routine.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            try
            {
                var fullRoutine = await _context.AppointmentAutomationRoutines
                    .Include(r => r.Doctor)
                    .Include(r => r.RoutineSpecialties)
                    .FirstOrDefaultAsync(r => r.Id == routine.Id);
                if (fullRoutine != null)
                {
                    var generated = await GenerateForRoutineInternalAsync(fullRoutine, CancellationToken.None);
                    if (generated > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif edilen rutin {RoutineId} için anlık randevu üretiminde hata.", routine.Id);
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteRoutineAsync(string doctorId, int routineId)
        {
            var routine = await _context.AppointmentAutomationRoutines
                .FirstOrDefaultAsync(r => r.Id == routineId && r.DoctorId == doctorId);

            if (routine == null)
            {
                return ServiceResult.Failure("Rutin bulunamadi.");
            }

            _context.AppointmentAutomationRoutines.Remove(routine);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<int> GenerateAppointmentsForRoutineAsync(int routineId, CancellationToken cancellationToken = default)
        {
            var routine = await _context.AppointmentAutomationRoutines
                .Include(r => r.Doctor)
                .Include(r => r.RoutineSpecialties)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.IsEnabled, cancellationToken);

            if (routine == null)
            {
                return 0;
            }

            var generated = await GenerateForRoutineInternalAsync(routine, cancellationToken);
            if (generated > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Rutin {RoutineId} için {Count} adet randevu hemen oluşturuldu.", routine.Id, generated);
            }
            return generated;
        }

        public async Task<int> GenerateAppointmentsAsync(CancellationToken cancellationToken = default)
        {
            var routines = await _context.AppointmentAutomationRoutines
                .Include(r => r.Doctor)
                .Include(r => r.RoutineSpecialties)
                .Where(r => r.IsEnabled)
                .ToListAsync(cancellationToken);

            var generatedCount = 0;

            foreach (var routine in routines)
            {
                generatedCount += await GenerateForRoutineInternalAsync(routine, cancellationToken);
            }

            if (generatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Otomasyon ile {Count} adet randevu oluşturuldu.", generatedCount);
            }

            return generatedCount;
        }

        private async Task<int> GenerateForRoutineInternalAsync(AppointmentAutomationRoutine routine, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var turkeyNow = TimeZoneHelper.GetTurkeyNow();
            var today = DateOnly.FromDateTime(turkeyNow);

            var routineGenerated = 0;
            var skippedDayMask = 0;
            var skippedPastTime = 0;
            var skippedAlreadyExists = 0;
            var skippedReservedConflict = 0;

            if (routine.PausedUntilUtc.HasValue && routine.PausedUntilUtc.Value > utcNow)
            {
                _logger.LogInformation("Routine {RoutineId} atlandi. Reason=PausedUntil, UntilUtc={PausedUntilUtc}", routine.Id, routine.PausedUntilUtc);
                return 0;
            }

            if (routine.IsInPerson && routine.Location == null && routine.Doctor.Location == null)
            {
                _logger.LogWarning("Routine {RoutineId} atlandi. Reason=MissingDoctorLocation", routine.Id);
                return 0;
            }

            var horizon = today.AddDays(Math.Clamp(routine.GenerateDaysAhead, 1, 90));
            var startDate = routine.ActiveFrom > today ? routine.ActiveFrom : today;
            var endDate = routine.ActiveUntil.HasValue && routine.ActiveUntil.Value < horizon
                ? routine.ActiveUntil.Value
                : horizon;

            if (endDate < startDate)
            {
                _logger.LogWarning("Routine {RoutineId} atlandi. Reason=InvalidDateRange, StartDate={StartDate}, EndDate={EndDate}", routine.Id, startDate, endDate);
                return 0;
            }

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (!IsDateEnabled(routine.DaysOfWeek, date.DayOfWeek))
                {
                    skippedDayMask++;
                    continue;
                }

                var start = date.ToDateTime(routine.StartTime);
                if (start <= turkeyNow.AddMinutes(1))
                {
                    skippedPastTime++;
                    continue;
                }

                var end = start.AddMinutes(routine.DurationInMinutes);

                var alreadyExists = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == routine.DoctorId &&
                    a.StartTime == start &&
                    a.EndTime == end,
                    cancellationToken);

                if (alreadyExists)
                {
                    skippedAlreadyExists++;
                    continue;
                }

                var overlapsReserved = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == routine.DoctorId &&
                    a.Status == AppointmentStatus.Reserved &&
                    a.StartTime < end &&
                    a.EndTime > start,
                    cancellationToken);

                if (overlapsReserved)
                {
                    skippedReservedConflict++;
                    continue;
                }

                var appointment = new Appointment
                {
                    DoctorId = routine.DoctorId,
                    StartTime = start,
                    EndTime = end,
                    IsOnline = routine.IsOnline,
                    IsInPerson = routine.IsInPerson,
                    MinPrice = routine.MinPrice,
                    MaxPrice = routine.MaxPrice,
                    Notes = routine.Notes,
                    Status = AppointmentStatus.Available,
                    Location = routine.IsInPerson
                        ? (routine.Location ?? routine.Doctor.Location)
                        : null,
                    LocationNote = routine.LocationNote,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };

                foreach (var routineSpecialty in routine.RoutineSpecialties)
                {
                    appointment.AppointmentSpecialties.Add(new Appointment_Specialty
                    {
                        SpecialtyId = routineSpecialty.SpecialtyId
                    });
                }

                _context.Appointments.Add(appointment);
                routineGenerated++;
            }

            _logger.LogInformation(
                "Routine {RoutineId} processed. Generated={Generated}, SkippedDayMask={SkippedDayMask}, SkippedPast={SkippedPastTime}, SkippedExists={SkippedAlreadyExists}, SkippedReservedConflict={SkippedReservedConflict}",
                routine.Id,
                routineGenerated,
                skippedDayMask,
                skippedPastTime,
                skippedAlreadyExists,
                skippedReservedConflict);

            return routineGenerated;
        }

        private static AutomationRoutineListItemViewModel MapToListItem(AppointmentAutomationRoutine routine)
        {
            var endTime = routine.StartTime.AddMinutes(routine.DurationInMinutes);
            return new AutomationRoutineListItemViewModel
            {
                Id = routine.Id,
                Name = routine.Name,
                DaysText = DaysToText(routine.DaysOfWeek),
                TimeRange = $"{routine.StartTime:HH\\:mm} - {endTime:HH\\:mm}",
                GenerateDaysAhead = routine.GenerateDaysAhead,
                AppointmentTypes = BuildAppointmentTypes(routine.IsOnline, routine.IsInPerson),
                PriceRange = routine.MinPrice.HasValue || routine.MaxPrice.HasValue ? $"{routine.MinPrice:N2} TL - {routine.MaxPrice:N2} TL" : "-",
                IsEnabled = routine.IsEnabled,
                PausedUntilUtc = routine.PausedUntilUtc,
                ActiveFrom = routine.ActiveFrom.ToDateTime(TimeOnly.MinValue),
                ActiveUntil = routine.ActiveUntil?.ToDateTime(TimeOnly.MinValue),
                Notes = routine.Notes,
                LocationNote = routine.LocationNote,
                InPersonLocationMode = ToLocationModeText(routine.InPersonLocationMode),
                Specialties = routine.RoutineSpecialties.Select(rs => rs.Specialty.Name ?? string.Empty).ToList()
            };
        }

        private static string BuildAppointmentTypes(bool isOnline, bool isInPerson)
        {
            if (isOnline && isInPerson)
            {
                return "Çevrim içi + yüz yüze";
            }

            if (isOnline)
            {
                return "Çevrim içi";
            }

            if (isInPerson)
            {
                return "Yüz yüze";
            }

            return "-";
        }

        private static string ToLocationModeText(string? mode)
        {
            return (mode ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "profile" => "Profil konumu",
                "device" => "Cihaz konumu",
                "manual" => "Manuel seçim",
                _ => "Profil konumu"
            };
        }

        private static (Point? Location, string Mode, string? ErrorMessage) ResolveRoutineLocation(AutomationRoutineInputViewModel model, Point? doctorProfileLocation)
        {
            var mode = (model.InPersonLocationMode ?? "profile").Trim().ToLowerInvariant();
            if (mode != "profile" && mode != "device" && mode != "manual")
            {
                mode = "profile";
            }

            if (!model.IsInPerson)
            {
                return (null, mode, null);
            }

            if (mode == "profile")
            {
                if (doctorProfileLocation == null)
                {
                    return (null, mode, "Profil konumu seçildiği için doktor profilinde konum olmalıdır.");
                }

                var profileLocation = new Point(doctorProfileLocation.X, doctorProfileLocation.Y) { SRID = 4326 };
                return (profileLocation, mode, null);
            }

            if (!TryCreatePoint(model.InPersonLatitude, model.InPersonLongitude, out var selectedPoint))
            {
                return (null, mode, "Seçilen konum kaynağı için geçerli enlem ve boylam girilmelidir.");
            }

            return (selectedPoint, mode, null);
        }

        private static bool TryCreatePoint(string? latitudeText, string? longitudeText, out Point? point)
        {
            point = null;
            if (string.IsNullOrWhiteSpace(latitudeText) || string.IsNullOrWhiteSpace(longitudeText))
            {
                return false;
            }

            var latOk = double.TryParse(latitudeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude);
            var lonOk = double.TryParse(longitudeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude);
            if (!latOk || !lonOk)
            {
                return false;
            }

            point = new Point(Math.Round(longitude, 6), Math.Round(latitude, 6)) { SRID = 4326 };
            return true;
        }

        private static RoutineWeekDayMask ToDaysMask(IEnumerable<int>? selectedDayValues)
        {
            var mask = RoutineWeekDayMask.None;
            if (selectedDayValues == null)
            {
                return mask;
            }

            foreach (var dayValue in selectedDayValues.Distinct())
            {
                if (!Enum.IsDefined(typeof(DayOfWeek), dayValue))
                {
                    continue;
                }

                var day = (DayOfWeek)dayValue;
                mask |= day switch
                {
                    DayOfWeek.Monday => RoutineWeekDayMask.Monday,
                    DayOfWeek.Tuesday => RoutineWeekDayMask.Tuesday,
                    DayOfWeek.Wednesday => RoutineWeekDayMask.Wednesday,
                    DayOfWeek.Thursday => RoutineWeekDayMask.Thursday,
                    DayOfWeek.Friday => RoutineWeekDayMask.Friday,
                    DayOfWeek.Saturday => RoutineWeekDayMask.Saturday,
                    DayOfWeek.Sunday => RoutineWeekDayMask.Sunday,
                    _ => RoutineWeekDayMask.None
                };
            }

            return mask;
        }

        private static List<int> ToSelectedDayValues(RoutineWeekDayMask mask)
        {
            var days = new List<int>();
            if (mask.HasFlag(RoutineWeekDayMask.Monday)) days.Add((int)DayOfWeek.Monday);
            if (mask.HasFlag(RoutineWeekDayMask.Tuesday)) days.Add((int)DayOfWeek.Tuesday);
            if (mask.HasFlag(RoutineWeekDayMask.Wednesday)) days.Add((int)DayOfWeek.Wednesday);
            if (mask.HasFlag(RoutineWeekDayMask.Thursday)) days.Add((int)DayOfWeek.Thursday);
            if (mask.HasFlag(RoutineWeekDayMask.Friday)) days.Add((int)DayOfWeek.Friday);
            if (mask.HasFlag(RoutineWeekDayMask.Saturday)) days.Add((int)DayOfWeek.Saturday);
            if (mask.HasFlag(RoutineWeekDayMask.Sunday)) days.Add((int)DayOfWeek.Sunday);
            return days;
        }

        private static bool IsDateEnabled(RoutineWeekDayMask mask, DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => mask.HasFlag(RoutineWeekDayMask.Monday),
                DayOfWeek.Tuesday => mask.HasFlag(RoutineWeekDayMask.Tuesday),
                DayOfWeek.Wednesday => mask.HasFlag(RoutineWeekDayMask.Wednesday),
                DayOfWeek.Thursday => mask.HasFlag(RoutineWeekDayMask.Thursday),
                DayOfWeek.Friday => mask.HasFlag(RoutineWeekDayMask.Friday),
                DayOfWeek.Saturday => mask.HasFlag(RoutineWeekDayMask.Saturday),
                DayOfWeek.Sunday => mask.HasFlag(RoutineWeekDayMask.Sunday),
                _ => false
            };
        }

        private static string DaysToText(RoutineWeekDayMask mask)
        {
            var dayNames = new List<string>();
            if (mask.HasFlag(RoutineWeekDayMask.Monday)) dayNames.Add("Pzt");
            if (mask.HasFlag(RoutineWeekDayMask.Tuesday)) dayNames.Add("Sal");
            if (mask.HasFlag(RoutineWeekDayMask.Wednesday)) dayNames.Add("Car");
            if (mask.HasFlag(RoutineWeekDayMask.Thursday)) dayNames.Add("Per");
            if (mask.HasFlag(RoutineWeekDayMask.Friday)) dayNames.Add("Cum");
            if (mask.HasFlag(RoutineWeekDayMask.Saturday)) dayNames.Add("Cmt");
            if (mask.HasFlag(RoutineWeekDayMask.Sunday)) dayNames.Add("Paz");
            return dayNames.Count == 0 ? "-" : string.Join(", ", dayNames);
        }
    }
}
