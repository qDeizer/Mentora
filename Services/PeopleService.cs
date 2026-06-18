using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public class PeopleService : IPeopleService
    {
        private const int RejectedRequestVisibilityDays = 4;
        private readonly ApplicationDbContext _context;

        public PeopleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PeopleIndexViewModel> GetPeopleIndexAsync(string viewerUserId, bool viewerIsDoctor, string? searchTerm)
        {
            var normalizedSearch = (searchTerm ?? string.Empty).Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            if (viewerIsDoctor)
            {
                var pairData = await GetDoctorSidePairDataAsync(viewerUserId);
                var shareCounts = await GetDoctorSharedNoteCountsAsync(viewerUserId);

                var cards = pairData
                    .Select(x =>
                    {
                        var evaluation = EvaluateAccess(x.Appointments, x.Requests, x.ConnectionState, now);
                        if (!evaluation.HasAccess)
                        {
                            return null;
                        }

                        if (!string.IsNullOrWhiteSpace(normalizedSearch) &&
                            !(x.Target.FirstName + " " + x.Target.LastName).ToLowerInvariant().Contains(normalizedSearch) &&
                            !(x.Target.Email ?? string.Empty).ToLowerInvariant().Contains(normalizedSearch))
                        {
                            return null;
                        }

                        shareCounts.TryGetValue(x.Target.Id, out var noteCount);

                        return new PersonCardViewModel
                        {
                            UserId = x.Target.Id,
                            FullName = $"{x.Target.FirstName} {x.Target.LastName}",
                            UserTypeLabel = "Hasta",
                            ProfilePhotoPath = x.Target.ProfilePhotoPath,
                            Email = x.Target.Email,
                            PhoneNumber = x.Target.PhoneNumber,
                            LastInteractionAt = evaluation.LastInteractionAt,
                            TotalAppointmentCount = x.Appointments.Count,
                            TotalRequestCount = x.Requests.Count,
                            ActiveRequestCount = x.Requests.Count(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved),
                            SharedNoteCount = noteCount
                        };
                    })
                    .Where(x => x != null)
                    .Cast<PersonCardViewModel>()
                    .OrderByDescending(x => x.LastInteractionAt ?? DateTime.MinValue)
                    .ThenBy(x => x.FullName)
                    .ToList();

                return new PeopleIndexViewModel
                {
                    IsDoctorView = true,
                    SearchTerm = searchTerm ?? string.Empty,
                    People = cards
                };
            }
            else
            {
                var pairData = await GetPatientSidePairDataAsync(viewerUserId);
                var shareCounts = await GetPatientSharedNoteCountsAsync(viewerUserId);

                var cards = pairData
                    .Select(x =>
                    {
                        var evaluation = EvaluateAccess(x.Appointments, x.Requests, x.ConnectionState, now);
                        if (!evaluation.HasAccess)
                        {
                            return null;
                        }

                        if (!string.IsNullOrWhiteSpace(normalizedSearch) &&
                            !(x.Target.FirstName + " " + x.Target.LastName).ToLowerInvariant().Contains(normalizedSearch) &&
                            !(x.Target.Email ?? string.Empty).ToLowerInvariant().Contains(normalizedSearch))
                        {
                            return null;
                        }

                        shareCounts.TryGetValue(x.Target.Id, out var noteCount);

                        return new PersonCardViewModel
                        {
                            UserId = x.Target.Id,
                            FullName = $"{x.Target.FirstName} {x.Target.LastName}",
                            UserTypeLabel = "Doktor",
                            ProfilePhotoPath = x.Target.ProfilePhotoPath,
                            Email = x.Target.Email,
                            PhoneNumber = x.Target.PhoneNumber,
                            LastInteractionAt = evaluation.LastInteractionAt,
                            TotalAppointmentCount = x.Appointments.Count,
                            TotalRequestCount = x.Requests.Count,
                            ActiveRequestCount = x.Requests.Count(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved),
                            SharedNoteCount = noteCount
                        };
                    })
                    .Where(x => x != null)
                    .Cast<PersonCardViewModel>()
                    .OrderByDescending(x => x.LastInteractionAt ?? DateTime.MinValue)
                    .ThenBy(x => x.FullName)
                    .ToList();

                return new PeopleIndexViewModel
                {
                    IsDoctorView = false,
                    SearchTerm = searchTerm ?? string.Empty,
                    People = cards
                };
            }
        }

        public async Task<PersonProfileViewModel?> GetPersonProfileAsync(string viewerUserId, bool viewerIsDoctor, string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(targetUserId) || viewerUserId == targetUserId)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            if (viewerIsDoctor)
            {
                var pairData = await GetDoctorSidePairDataAsync(viewerUserId, targetUserId);
                var pair = pairData.FirstOrDefault();
                if (pair == null)
                {
                    return null;
                }

                var evaluation = EvaluateAccess(pair.Appointments, pair.Requests, pair.ConnectionState, now);
                if (!evaluation.HasAccess)
                {
                    return null;
                }

                var sharedNoteCount = await _context.ClinicalNoteShares
                    .CountAsync(s =>
                        s.ClinicalNote.PatientId == pair.Target.Id &&
                        s.SharedWithDoctorId == viewerUserId &&
                        s.RevokedAtUtc == null);

                return BuildProfileModel(
                    viewerIsDoctor: true,
                    viewerUserId,
                    pair.Target,
                    pair.Appointments,
                    pair.Requests,
                    sharedNoteCount,
                    evaluation);
            }
            else
            {
                var pairData = await GetPatientSidePairDataAsync(viewerUserId, targetUserId);
                var pair = pairData.FirstOrDefault();
                if (pair == null)
                {
                    return null;
                }

                var evaluation = EvaluateAccess(pair.Appointments, pair.Requests, pair.ConnectionState, now);
                if (!evaluation.HasAccess)
                {
                    return null;
                }

                var sharedNoteCount = await _context.ClinicalNoteShares
                    .CountAsync(s =>
                        s.ClinicalNote.PatientId == viewerUserId &&
                        s.SharedWithDoctorId == pair.Target.Id &&
                        s.RevokedAtUtc == null);

                return BuildProfileModel(
                    viewerIsDoctor: false,
                    viewerUserId,
                    pair.Target,
                    pair.Appointments,
                    pair.Requests,
                    sharedNoteCount,
                    evaluation);
            }
        }

        public async Task<ServiceResult> DisconnectAsync(string actorUserId, bool actorIsDoctor, string targetUserId)
        {
            if (string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(targetUserId))
            {
                return ServiceResult.Failure("Geçersiz kullanıcı bilgisi.");
            }

            string doctorId;
            string patientId;

            if (actorIsDoctor)
            {
                var patientExists = await _context.Patients.AnyAsync(x => x.Id == targetUserId);
                if (!patientExists)
                {
                    return ServiceResult.Failure("Hasta bulunamadi.");
                }

                doctorId = actorUserId;
                patientId = targetUserId;
            }
            else
            {
                var doctorExists = await _context.Doctors.AnyAsync(x => x.Id == targetUserId);
                if (!doctorExists)
                {
                    return ServiceResult.Failure("Doktor bulunamadi.");
                }

                doctorId = targetUserId;
                patientId = actorUserId;
            }

            var state = await _context.DoctorPatientConnectionStates
                .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.PatientId == patientId);

            if (state == null)
            {
                state = new DoctorPatientConnectionState
                {
                    DoctorId = doctorId,
                    PatientId = patientId
                };
                _context.DoctorPatientConnectionStates.Add(state);
            }

            state.DisconnectedAtUtc = DateTime.UtcNow;
            state.DisconnectedByUserId = actorUserId;
            state.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        private async Task<List<PairData>> GetDoctorSidePairDataAsync(string doctorId, string? onlyPatientId = null)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.PatientId != null)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(onlyPatientId))
            {
                appointments = appointments.Where(a => a.PatientId == onlyPatientId).ToList();
            }

            var requests = await _context.AppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Appointment)
                .Where(r => r.DoctorId == doctorId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(onlyPatientId))
            {
                requests = requests.Where(r => r.PatientId == onlyPatientId).ToList();
            }

            var patientIds = appointments.Select(a => a.PatientId!)
                .Union(requests.Select(r => r.PatientId))
                .Distinct()
                .ToList();

            if (patientIds.Count == 0)
            {
                return new List<PairData>();
            }

            var patients = await _context.Patients
                .Where(p => patientIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var states = await _context.DoctorPatientConnectionStates
                .Where(x => x.DoctorId == doctorId && patientIds.Contains(x.PatientId))
                .ToDictionaryAsync(x => x.PatientId);

            return patientIds
                .Where(patients.ContainsKey)
                .Select(patientId => new PairData
                {
                    Target = patients[patientId],
                    Appointments = appointments.Where(a => a.PatientId == patientId).ToList(),
                    Requests = requests.Where(r => r.PatientId == patientId).ToList(),
                    ConnectionState = states.TryGetValue(patientId, out var state) ? state : null
                })
                .ToList();
        }

        private async Task<List<PairData>> GetPatientSidePairDataAsync(string patientId, string? onlyDoctorId = null)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(onlyDoctorId))
            {
                appointments = appointments.Where(a => a.DoctorId == onlyDoctorId).ToList();
            }

            var requests = await _context.AppointmentRequests
                .Include(r => r.Doctor)
                .Include(r => r.Appointment)
                .Where(r => r.PatientId == patientId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(onlyDoctorId))
            {
                requests = requests.Where(r => r.DoctorId == onlyDoctorId).ToList();
            }

            var doctorIds = appointments.Select(a => a.DoctorId)
                .Union(requests.Select(r => r.DoctorId))
                .Distinct()
                .ToList();

            if (doctorIds.Count == 0)
            {
                return new List<PairData>();
            }

            var doctors = await _context.Doctors
                .Where(d => doctorIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id);

            var states = await _context.DoctorPatientConnectionStates
                .Where(x => doctorIds.Contains(x.DoctorId) && x.PatientId == patientId)
                .ToDictionaryAsync(x => x.DoctorId);

            return doctorIds
                .Where(doctors.ContainsKey)
                .Select(doctorId => new PairData
                {
                    Target = doctors[doctorId],
                    Appointments = appointments.Where(a => a.DoctorId == doctorId).ToList(),
                    Requests = requests.Where(r => r.DoctorId == doctorId).ToList(),
                    ConnectionState = states.TryGetValue(doctorId, out var state) ? state : null
                })
                .ToList();
        }

        private async Task<Dictionary<string, int>> GetDoctorSharedNoteCountsAsync(string doctorId)
        {
            return await _context.ClinicalNoteShares
                .Where(s => s.SharedWithDoctorId == doctorId && s.RevokedAtUtc == null)
                .GroupBy(s => s.ClinicalNote.PatientId)
                .Select(g => new { PatientId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PatientId, x => x.Count);
        }

        private async Task<Dictionary<string, int>> GetPatientSharedNoteCountsAsync(string patientId)
        {
            return await _context.ClinicalNoteShares
                .Where(s => s.ClinicalNote.PatientId == patientId && s.RevokedAtUtc == null)
                .GroupBy(s => s.SharedWithDoctorId)
                .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DoctorId, x => x.Count);
        }

        private static PairAccessEvaluation EvaluateAccess(
            List<Appointment> appointments,
            List<AppointmentRequest> requests,
            DoctorPatientConnectionState? connectionState,
            DateTime utcNow)
        {
            var oneYearAgo = utcNow.AddYears(-1);
            var rejectedCutoff = utcNow.AddDays(-RejectedRequestVisibilityDays);

            var hasRecentAppointment = appointments.Any(a =>
                a.StartTime.ToUniversalTime() >= oneYearAgo ||
                a.EndTime.ToUniversalTime() >= oneYearAgo);

            var hasPendingOrApprovedRequest = requests.Any(r =>
                r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved);

            var hasRecentRejectedRequest = requests.Any(r =>
                r.Status == RequestStatus.Rejected && r.CreatedAt >= rejectedCutoff);

            var latestAppointmentTime = appointments
                .Select(a => (DateTime?)a.StartTime.ToUniversalTime())
                .DefaultIfEmpty()
                .Max();

            var latestRequestTime = requests
                .Select(r => (DateTime?)r.CreatedAt)
                .DefaultIfEmpty()
                .Max();

            var lastInteractionAt = latestAppointmentTime.HasValue && latestRequestTime.HasValue
                ? (latestAppointmentTime > latestRequestTime ? latestAppointmentTime : latestRequestTime)
                : (latestAppointmentTime ?? latestRequestTime);

            var disconnectedAt = connectionState?.DisconnectedAtUtc;
            var disconnectedByState = disconnectedAt.HasValue &&
                                      (!lastInteractionAt.HasValue || lastInteractionAt <= disconnectedAt);

            var hasAccess = !disconnectedByState &&
                            (hasRecentAppointment || hasPendingOrApprovedRequest || hasRecentRejectedRequest);

            return new PairAccessEvaluation
            {
                HasAccess = hasAccess,
                LastInteractionAt = lastInteractionAt,
                IsDisconnectedByState = disconnectedByState
            };
        }

        private static PersonProfileViewModel BuildProfileModel(
            bool viewerIsDoctor,
            string viewerUserId,
            User target,
            List<Appointment> appointments,
            List<AppointmentRequest> requests,
            int sharedNoteCount,
            PairAccessEvaluation accessEvaluation)
        {
            return new PersonProfileViewModel
            {
                ViewerIsDoctor = viewerIsDoctor,
                ViewerUserId = viewerUserId,
                TargetUserId = target.Id,
                TargetFullName = $"{target.FirstName} {target.LastName}",
                TargetRoleLabel = target.UserType == UserType.Doctor ? "Doktor" : "Hasta",
                TargetProfilePhotoPath = target.ProfilePhotoPath,
                TargetEmail = target.Email,
                TargetPhoneNumber = target.PhoneNumber,
                TargetBirthDate = target.BirthDate,
                TargetAbout = target.About,
                IsDisconnected = accessEvaluation.IsDisconnectedByState,
                LastInteractionAt = accessEvaluation.LastInteractionAt,
                TotalAppointmentCount = appointments.Count,
                PendingRequestCount = requests.Count(r => r.Status == RequestStatus.Pending),
                ApprovedRequestCount = requests.Count(r => r.Status == RequestStatus.Approved),
                RejectedRequestCount = requests.Count(r => r.Status == RequestStatus.Rejected),
                SharedNoteCount = sharedNoteCount,
                RecentAppointments = appointments
                    .OrderByDescending(a => a.StartTime)
                    .Take(12)
                    .Select(a => new PersonProfileAppointmentItemViewModel
                    {
                        AppointmentId = a.Id,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        StatusLabel = ToAppointmentStatusLabel(a.Status),
                        PriceLabel = (a.MinPrice.HasValue || a.MaxPrice.HasValue)
                            ? $"{a.MinPrice:N2} TL - {a.MaxPrice:N2} TL"
                            : "-",
                        LocationNote = a.LocationNote
                    })
                    .ToList(),
                RecentRequests = requests
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(12)
                    .Select(r => new PersonProfileRequestItemViewModel
                    {
                        RequestId = r.Id,
                        AppointmentId = r.AppointmentId,
                        AppointmentStartTime = r.Appointment.StartTime,
                        RequestedAt = r.CreatedAt.ToLocalTime(),
                        StatusLabel = ToRequestStatusLabel(r.Status),
                        RequestMessage = r.RequestMessage,
                        ResponseMessage = r.ResponseMessage
                    })
                    .ToList()
            };
        }

        private static string ToAppointmentStatusLabel(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Available => "Müsait",
                AppointmentStatus.Reserved => "Rezerve",
                AppointmentStatus.Completed => "Tamamlandı",
                AppointmentStatus.NotCompleted => "Gerçekleşmedi",
                AppointmentStatus.CancelledByConflict => "Çakışma nedeniyle kapatıldı",
                _ => status.ToString()
            };
        }

        private static string ToRequestStatusLabel(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Pending => "Beklemede",
                RequestStatus.Approved => "Onaylandı",
                RequestStatus.Rejected => "Reddedildi",
                _ => status.ToString()
            };
        }

        private sealed class PairData
        {
            public User Target { get; set; } = default!;
            public List<Appointment> Appointments { get; set; } = new();
            public List<AppointmentRequest> Requests { get; set; } = new();
            public DoctorPatientConnectionState? ConnectionState { get; set; }
        }

        private sealed class PairAccessEvaluation
        {
            public bool HasAccess { get; set; }
            public bool IsDisconnectedByState { get; set; }
            public DateTime? LastInteractionAt { get; set; }
        }
    }
}
