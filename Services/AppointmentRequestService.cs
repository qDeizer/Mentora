using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public class AppointmentRequestService : IAppointmentRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppointmentService _appointmentService;
        private readonly IEmailOutboxService _emailOutboxService;
        private readonly INotificationService _notificationService;

        public AppointmentRequestService(
            ApplicationDbContext context,
            IAppointmentService appointmentService,
            IEmailOutboxService emailOutboxService,
            INotificationService notificationService)
        {
            _context = context;
            _appointmentService = appointmentService;
            _emailOutboxService = emailOutboxService;
            _notificationService = notificationService;
        }

        public async Task<RequestsViewModel> GetRequestsAsync(AppointmentRequestFilterModel filter)
        {
            var query = _context.AppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Include(r => r.Appointment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.DoctorId))
            {
                query = query.Where(r => r.DoctorId == filter.DoctorId);
            }

            if (!string.IsNullOrWhiteSpace(filter.PatientId))
            {
                query = query.Where(r => r.PatientId == filter.PatientId);
            }

            if (filter.AppointmentId.HasValue)
            {
                query = query.Where(r => r.AppointmentId == filter.AppointmentId.Value);
            }

            var optionQuery = query;

            var selectedPatientIds = (filter.SelectedPatientIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            if (!string.IsNullOrWhiteSpace(filter.SelectedPatientId) && !selectedPatientIds.Contains(filter.SelectedPatientId))
            {
                selectedPatientIds.Add(filter.SelectedPatientId);
            }

            var selectedDoctorIds = (filter.SelectedDoctorIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            if (!string.IsNullOrWhiteSpace(filter.SelectedDoctorId) && !selectedDoctorIds.Contains(filter.SelectedDoctorId))
            {
                selectedDoctorIds.Add(filter.SelectedDoctorId);
            }

            if (selectedPatientIds.Count > 0)
            {
                query = query.Where(r => selectedPatientIds.Contains(r.PatientId));
            }

            if (selectedDoctorIds.Count > 0)
            {
                query = query.Where(r => selectedDoctorIds.Contains(r.DoctorId));
            }

            var selectedStatuses = (filter.SelectedStatuses ?? new List<RequestStatus>())
                .Distinct()
                .ToList();
            var allStatuses = Enum.GetValues<RequestStatus>().ToList();
            if (selectedStatuses.Count > 0 && selectedStatuses.Count < allStatuses.Count)
            {
                query = query.Where(r => selectedStatuses.Contains(r.Status));
            }

            if (filter.RequestDateFrom.HasValue)
            {
                var requestFromUtc = DateTime.SpecifyKind(filter.RequestDateFrom.Value, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(r => r.CreatedAt >= requestFromUtc);
            }

            if (filter.RequestDateTo.HasValue)
            {
                var requestToUtc = DateTime.SpecifyKind(filter.RequestDateTo.Value, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(r => r.CreatedAt <= requestToUtc);
            }

            if (filter.AppointmentDateFrom.HasValue)
            {
                query = query.Where(r => r.Appointment.StartTime >= filter.AppointmentDateFrom.Value);
            }

            if (filter.AppointmentDateTo.HasValue)
            {
                query = query.Where(r => r.Appointment.StartTime <= filter.AppointmentDateTo.Value);
            }

            var projectedQuery = query.Select(r => new AppointmentRequestViewModel
            {
                RequestId = r.Id,
                AppointmentId = r.AppointmentId,
                PatientId = r.PatientId,
                PatientName = r.Patient.FirstName + " " + r.Patient.LastName,
                PatientProfilePhoto = r.Patient.ProfilePhotoPath,
                DoctorId = r.DoctorId,
                DoctorName = r.Doctor.FirstName + " " + r.Doctor.LastName,
                DoctorProfilePhoto = r.Doctor.ProfilePhotoPath,
                AppointmentStartTime = r.Appointment.StartTime,
                AppointmentEndTime = r.Appointment.EndTime,
                AppointmentTypeText = (r.Appointment.IsOnline && r.Appointment.IsInPerson)
                    ? "Çevrim içi + yüz yüze"
                    : r.Appointment.IsOnline
                        ? "Çevrim içi"
                        : r.Appointment.IsInPerson
                            ? "Yüz yüze"
                            : "-",
                AppointmentMinPrice = r.Appointment.MinPrice,
                AppointmentMaxPrice = r.Appointment.MaxPrice,
                RequestDate = r.CreatedAt,
                RequestMessage = r.RequestMessage,
                ReasonForVisit = r.ReasonForVisit,
                PreviousSupportInfo = r.PreviousSupportInfo,
                UrgencyLevel = r.UrgencyLevel,
                Expectations = r.Expectations,
                ResponseMessage = r.ResponseMessage,
                Status = r.Status,
                IsPrivateOffer = r.Appointment.IsPrivateOffer,
                AppointmentLatitude = r.Appointment.Location != null ? r.Appointment.Location.Y : null,
                AppointmentLongitude = r.Appointment.Location != null ? r.Appointment.Location.X : null,
                AppointmentLocationNote = r.Appointment.LocationNote,
                MeetingLink = r.Appointment.MeetingLink
            });

            var sortBy = (filter.SortBy ?? "requestDate").Trim().ToLowerInvariant();
            var sortDirection = (filter.SortDirection ?? "desc").Trim().ToLowerInvariant();
            var isDesc = sortDirection != "asc";

            projectedQuery = sortBy switch
            {
                "appointmentdate" => isDesc
                    ? projectedQuery.OrderByDescending(r => r.AppointmentStartTime)
                    : projectedQuery.OrderBy(r => r.AppointmentStartTime),
                "patient" => isDesc
                    ? projectedQuery.OrderByDescending(r => r.PatientName)
                    : projectedQuery.OrderBy(r => r.PatientName),
                "doctor" => isDesc
                    ? projectedQuery.OrderByDescending(r => r.DoctorName)
                    : projectedQuery.OrderBy(r => r.DoctorName),
                _ => isDesc
                    ? projectedQuery.OrderByDescending(r => r.RequestDate)
                    : projectedQuery.OrderBy(r => r.RequestDate)
            };

            var requests = await projectedQuery.ToListAsync();
            await FillApprovalPreviewAsync(requests);

            var patientOptions = await optionQuery
                .GroupBy(r => new { r.PatientId, FullName = r.Patient.FirstName + " " + r.Patient.LastName, r.Patient.ProfilePhotoPath })
                .Select(g => new RequestPartyOptionViewModel
                {
                    Id = g.Key.PatientId,
                    FullName = g.Key.FullName,
                    ProfilePhotoPath = g.Key.ProfilePhotoPath
                })
                .OrderBy(x => x.FullName)
                .ToListAsync();

            var doctorOptions = await optionQuery
                .GroupBy(r => new { r.DoctorId, FullName = r.Doctor.FirstName + " " + r.Doctor.LastName, r.Doctor.ProfilePhotoPath })
                .Select(g => new RequestPartyOptionViewModel
                {
                    Id = g.Key.DoctorId,
                    FullName = g.Key.FullName,
                    ProfilePhotoPath = g.Key.ProfilePhotoPath
                })
                .OrderBy(x => x.FullName)
                .ToListAsync();

            filter.SelectedPatientIds = selectedPatientIds;
            filter.SelectedDoctorIds = selectedDoctorIds;

            return new RequestsViewModel
            {
                Requests = requests,
                Filter = filter,
                PatientOptions = patientOptions,
                DoctorOptions = doctorOptions
            };
        }

        private async Task FillApprovalPreviewAsync(List<AppointmentRequestViewModel> requests)
        {
            if (requests.Count == 0)
            {
                return;
            }

            foreach (var request in requests)
            {
                request.PriceRange = FormatPriceRange(request.AppointmentMinPrice, request.AppointmentMaxPrice);
            }

            var appointmentIds = requests.Select(r => r.AppointmentId).Distinct().ToList();
            var pendingApplicants = await _context.AppointmentRequests
                .Include(r => r.Patient)
                .Where(r => appointmentIds.Contains(r.AppointmentId) && r.Status == RequestStatus.Pending)
                .Select(r => new
                {
                    r.AppointmentId,
                    r.Id,
                    PatientName = r.Patient.FirstName + " " + r.Patient.LastName,
                    r.Patient.ProfilePhotoPath,
                    r.Patient.Email,
                    r.Patient.PhoneNumber
                })
                .ToListAsync();

            var pendingByAppointment = pendingApplicants
                .GroupBy(x => x.AppointmentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var doctorIds = requests.Select(r => r.DoctorId).Distinct().ToList();
            var availableSlots = await _context.Appointments
                .Where(a => doctorIds.Contains(a.DoctorId) && a.Status == AppointmentStatus.Available)
                .Select(a => new
                {
                    a.Id,
                    a.DoctorId,
                    a.StartTime,
                    a.EndTime
                })
                .ToListAsync();

            var availableSlotIds = availableSlots.Select(a => a.Id).ToList();
            var pendingCountsBySlot = await _context.AppointmentRequests
                .Where(r => availableSlotIds.Contains(r.AppointmentId) && r.Status == RequestStatus.Pending)
                .GroupBy(r => r.AppointmentId)
                .Select(g => new { AppointmentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AppointmentId, x => x.Count);

            foreach (var request in requests)
            {
                if (pendingByAppointment.TryGetValue(request.AppointmentId, out var applicants))
                {
                    request.OtherPendingApplicants = applicants
                        .Where(a => a.Id != request.RequestId)
                        .Select(a => new RequestApprovalApplicantViewModel
                        {
                            RequestId = a.Id,
                            PatientName = a.PatientName,
                            PatientProfilePhoto = a.ProfilePhotoPath,
                            PatientEmail = a.Email,
                            PatientPhone = a.PhoneNumber
                        })
                        .ToList();
                }

                var conflicts = availableSlots
                    .Where(slot =>
                        slot.Id != request.AppointmentId &&
                        slot.DoctorId == request.DoctorId &&
                        slot.StartTime < request.AppointmentEndTime &&
                        slot.EndTime > request.AppointmentStartTime)
                    .OrderBy(slot => slot.StartTime)
                    .ToList();

                request.ConflictingSlots = conflicts
                    .Select(slot => new RequestApprovalConflictSlotViewModel
                    {
                        AppointmentId = slot.Id,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        PendingRequestCount = pendingCountsBySlot.TryGetValue(slot.Id, out var count) ? count : 0
                    })
                    .ToList();

                request.ConflictPendingRequestCount = request.ConflictingSlots.Sum(s => s.PendingRequestCount);
                request.EstimatedMailCount = 1 + request.OtherPendingApplicants.Count + request.ConflictPendingRequestCount;
            }
        }

        public async Task<ServiceResult> CreateAppointmentRequestAsync(AppointmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PatientId))
            {
                return ServiceResult.Failure("Hasta bilgisi bulunamadı.");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

            if (appointment == null)
            {
                return ServiceResult.Failure("Randevu bulunamadı.");
            }

            if (appointment.DoctorId != request.DoctorId)
            {
                return ServiceResult.Failure("Randevu ve doktor bilgisi eşleşmiyor.");
            }

            if (appointment.Status != AppointmentStatus.Available)
            {
                return ServiceResult.Failure("Randevu artık müsait değil.");
            }

            var existingRequest = await _context.AppointmentRequests.AnyAsync(r =>
                r.AppointmentId == request.AppointmentId &&
                r.PatientId == request.PatientId &&
                (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved));

            if (existingRequest)
            {
                return ServiceResult.Failure("Bu randevu için zaten aktif bir talebiniz var.");
            }

            var hasPatientConflict = await _appointmentService.HasScheduleConflictForPatientAsync(request.PatientId, appointment.StartTime, appointment.EndTime);
            if (hasPatientConflict)
            {
                return ServiceResult.Failure("Bu saatler arasında zaten randevunuz var.");
            }

            request.Status = RequestStatus.Pending;
            request.CreatedAt = DateTime.UtcNow;
            request.RequestMessage = string.IsNullOrWhiteSpace(request.RequestMessage) ? null : request.RequestMessage.Trim();
            request.ReasonForVisit = string.IsNullOrWhiteSpace(request.ReasonForVisit) ? null : request.ReasonForVisit.Trim();
            request.PreviousSupportInfo = string.IsNullOrWhiteSpace(request.PreviousSupportInfo) ? null : request.PreviousSupportInfo.Trim();
            request.UrgencyLevel = string.IsNullOrWhiteSpace(request.UrgencyLevel) ? null : request.UrgencyLevel.Trim();
            request.Expectations = string.IsNullOrWhiteSpace(request.Expectations) ? null : request.Expectations.Trim();

            _context.AppointmentRequests.Add(request);
            await _context.SaveChangesAsync();

            var patient = await _context.Users
                .OfType<Patient>()
                .Where(p => p.Id == request.PatientId)
                .Select(p => new { p.FirstName, p.LastName, p.Email, p.PhoneNumber })
                .FirstOrDefaultAsync();
            var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Bir hasta";
            var trimmedRequestMessage = string.IsNullOrWhiteSpace(request.RequestMessage)
                ? null
                : request.RequestMessage.Trim();

            if (!string.IsNullOrWhiteSpace(appointment.Doctor.Email))
            {
                var bodyBuilder = new System.Text.StringBuilder();
                bodyBuilder.Append("<h2 style='font-family:sans-serif;'>Yeni Randevu Talebi</h2>");
                bodyBuilder.Append("<p style='font-family:sans-serif;'>Aşağıdaki hastadan yeni bir randevu talebi aldınız:</p>");
                bodyBuilder.Append("<table style='font-family:sans-serif;border-collapse:collapse;'>");
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Hasta:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(patientName)}</td></tr>");
                if (!string.IsNullOrWhiteSpace(patient?.Email))
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>E-posta:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(patient!.Email!)}</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(patient?.PhoneNumber))
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Telefon:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(patient!.PhoneNumber!)}</td></tr>");
                }
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Tarih/Saat:</strong></td><td style='padding:4px 8px;'>{appointment.StartTime:dd.MM.yyyy HH:mm} - {appointment.EndTime:HH:mm}</td></tr>");
                if (appointment.IsOnline)
                {
                    bodyBuilder.Append("<tr><td style='padding:4px 8px;'><strong>Tür:</strong></td><td style='padding:4px 8px;'>Çevrim içi</td></tr>");
                }
                if (appointment.IsInPerson)
                {
                    bodyBuilder.Append("<tr><td style='padding:4px 8px;'><strong>Tür:</strong></td><td style='padding:4px 8px;'>Yüz yüze</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(appointment.LocationNote))
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Konum:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(appointment.LocationNote)}</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(trimmedRequestMessage))
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;vertical-align:top;'><strong>Mesaj:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(trimmedRequestMessage)}</td></tr>");
                }
                AppendOptionalInfoRow(bodyBuilder, "Başvuru sebebi", request.ReasonForVisit);
                AppendOptionalInfoRow(bodyBuilder, "Önceki destek", request.PreviousSupportInfo);
                AppendOptionalInfoRow(bodyBuilder, "Aciliyet", request.UrgencyLevel);
                AppendOptionalInfoRow(bodyBuilder, "Beklenti", request.Expectations);
                bodyBuilder.Append("</table>");
                bodyBuilder.Append("<p style='font-family:sans-serif;'>Talebi değerlendirmek için Mentora paneline giriş yapın.</p>");

                await QueueEmailIfAllowedAsync(
                    appointment.DoctorId,
                    appointment.Doctor.Email!,
                    $"Mentora - Yeni randevu talebi ({patientName})",
                    bodyBuilder.ToString(),
                    requireRequestStatusEmail: true);
            }

            var inAppMessage = string.IsNullOrWhiteSpace(trimmedRequestMessage)
                ? $"{patientName} - {appointment.StartTime:dd.MM.yyyy HH:mm} için yeni talep gönderdi."
                : $"{patientName} - {appointment.StartTime:dd.MM.yyyy HH:mm} için talep gönderdi. Mesaj: {(trimmedRequestMessage!.Length > 60 ? trimmedRequestMessage[..60] + "..." : trimmedRequestMessage)}";

            await _notificationService.CreateAsync(
                appointment.DoctorId,
                NotificationType.IncomingRequest,
                "Yeni randevu talebi",
                inAppMessage,
                $"/Request?AppointmentId={appointment.Id}");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ApproveRequestAsync(int requestId, string doctorId, string? responseMessage = null, string? autoRejectMessage = null)
        {
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return ServiceResult.Failure("Doktor kimliği bulunamadı.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            var request = await _context.AppointmentRequests
                .Include(r => r.Appointment)
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return ServiceResult.Failure("Talep bulunamadı.");
            }

            if (request.DoctorId != doctorId)
            {
                return ServiceResult.Failure("Bu talebi onaylama yetkiniz yok.");
            }

            if (request.Status != RequestStatus.Pending)
            {
                if (request.Status == RequestStatus.Approved &&
                    request.Appointment.Status == AppointmentStatus.Reserved &&
                    request.Appointment.PatientId == request.PatientId)
                {
                    return ServiceResult.Success("Bu talep daha önce onaylanmış, randevu zaten rezerve durumda.");
                }

                return ServiceResult.Failure("Talep beklemede değil.");
            }

            var appointment = request.Appointment;
            if (appointment.Status != AppointmentStatus.Available)
            {
                return ServiceResult.Failure("Randevu artık müsait değil.");
            }

            var patientConflict = await _appointmentService.HasScheduleConflictForPatientAsync(request.PatientId, appointment.StartTime, appointment.EndTime, appointment.Id);
            if (patientConflict)
            {
                return ServiceResult.Failure("Hastanın bu saatler arasında başka bir randevusu var.");
            }

            var automaticRejectMessage = string.IsNullOrWhiteSpace(autoRejectMessage)
                ? "Bu randevu başka bir hastaya verildi. Dilerseniz diğer uygun randevulara tekrar başvurabilirsiniz."
                : autoRejectMessage.Trim();

            appointment.PatientId = request.PatientId;
            appointment.Status = AppointmentStatus.Reserved;
            appointment.UpdatedAtUtc = DateTime.UtcNow;

            request.Status = RequestStatus.Approved;
            request.ResponseMessage = string.IsNullOrWhiteSpace(responseMessage)
                ? "Talebiniz onaylandı."
                : responseMessage.Trim();

            var otherPendingRequests = await _context.AppointmentRequests
                .Include(r => r.Patient)
                .Where(r => r.AppointmentId == appointment.Id && r.Status == RequestStatus.Pending && r.Id != request.Id)
                .ToListAsync();

            var sameAppointmentRejectedPatients = new List<(string UserId, string? Email, string Name)>();
            foreach (var pending in otherPendingRequests)
            {
                sameAppointmentRejectedPatients.Add((
                    pending.PatientId,
                    pending.Patient?.Email,
                    pending.Patient != null ? $"{pending.Patient.FirstName} {pending.Patient.LastName}".Trim() : "Hasta"));

                pending.Status = RequestStatus.Rejected;
                pending.ResponseMessage = automaticRejectMessage;
            }

            var overlappingAppointments = await _context.Appointments
                .Where(a =>
                    a.Id != appointment.Id &&
                    a.DoctorId == appointment.DoctorId &&
                    a.Status == AppointmentStatus.Available &&
                    a.StartTime < appointment.EndTime &&
                    a.EndTime > appointment.StartTime)
                .ToListAsync();

            var affectedPatients = new List<(string UserId, string? Email, DateTime StartTime)>();
            var conflictRejectedRequestCount = 0;
            if (overlappingAppointments.Count > 0)
            {
                var overlappingIds = overlappingAppointments.Select(a => a.Id).ToList();

                var relatedPendingRequests = await _context.AppointmentRequests
                    .Include(r => r.Patient)
                    .Include(r => r.Appointment)
                    .Where(r => overlappingIds.Contains(r.AppointmentId) && r.Status == RequestStatus.Pending)
                    .ToListAsync();

                foreach (var pendingRequest in relatedPendingRequests)
                {
                    affectedPatients.Add((pendingRequest.PatientId, pendingRequest.Patient?.Email, pendingRequest.Appointment.StartTime));

                    pendingRequest.Status = RequestStatus.Rejected;
                    pendingRequest.ResponseMessage = "Çakışan randevu slotu kapatıldığı için talep otomatik reddedildi.";
                    conflictRejectedRequestCount++;
                }

                var utcNow = DateTime.UtcNow;
                foreach (var overlapping in overlappingAppointments)
                {
                    overlapping.Status = AppointmentStatus.CancelledByConflict;
                    overlapping.CancelledReason = "Onaylanan randevu ile çakışıyor.";
                    overlapping.CancelledAtUtc = utcNow;
                    overlapping.UpdatedAtUtc = utcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var doctorName = request.Doctor != null
                ? $"{request.Doctor.FirstName} {request.Doctor.LastName}".Trim()
                : "Doktor";
            var approvedPatientName = request.Patient != null
                ? $"{request.Patient.FirstName} {request.Patient.LastName}".Trim()
                : "Hasta";
            var queuedEmailCount = 0;

            if (!string.IsNullOrWhiteSpace(request.Patient?.Email))
            {
                var bodyBuilder = new System.Text.StringBuilder();
                bodyBuilder.Append("<h2 style='font-family:sans-serif;color:#15803d;'>Randevu Talebiniz Onaylandı</h2>");
                bodyBuilder.Append("<table style='font-family:sans-serif;border-collapse:collapse;'>");
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Doktor:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(doctorName)}</td></tr>");
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Tarih/Saat:</strong></td><td style='padding:4px 8px;'>{appointment.StartTime:dd.MM.yyyy HH:mm} - {appointment.EndTime:HH:mm}</td></tr>");
                if (appointment.IsOnline)
                {
                    bodyBuilder.Append("<tr><td style='padding:4px 8px;'><strong>Tür:</strong></td><td style='padding:4px 8px;'>Çevrim içi</td></tr>");
                    if (!string.IsNullOrWhiteSpace(appointment.MeetingLink))
                    {
                        bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Görüşme linki:</strong></td><td style='padding:4px 8px;'><a href='{System.Net.WebUtility.HtmlEncode(appointment.MeetingLink)}'>{System.Net.WebUtility.HtmlEncode(appointment.MeetingLink)}</a></td></tr>");
                    }
                }
                if (appointment.IsInPerson)
                {
                    bodyBuilder.Append("<tr><td style='padding:4px 8px;'><strong>Tür:</strong></td><td style='padding:4px 8px;'>Yüz yüze</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(appointment.LocationNote))
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Konum:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(appointment.LocationNote)}</td></tr>");
                }
                if (!string.IsNullOrWhiteSpace(request.ResponseMessage) && request.ResponseMessage != "Talebiniz onaylandı.")
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;vertical-align:top;'><strong>Doktor mesajı:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(request.ResponseMessage)}</td></tr>");
                }
                bodyBuilder.Append("</table>");
                bodyBuilder.Append("<p style='font-family:sans-serif;'>Detay için Mentora paneline giriş yapın.</p>");

                if (await QueueEmailIfAllowedAsync(
                    request.PatientId,
                    request.Patient.Email!,
                    $"Mentora - Randevunuz onaylandı ({appointment.StartTime:dd.MM.yyyy HH:mm})",
                    bodyBuilder.ToString(),
                    requireRequestStatusEmail: true))
                {
                    queuedEmailCount++;
                }
            }

            await _notificationService.CreateAsync(
                request.PatientId,
                NotificationType.RequestApproved,
                "Randevu talebiniz onaylandı",
                $"{doctorName} - {appointment.StartTime:dd.MM.yyyy HH:mm} randevunuz onaylandı.",
                "/Request/MyRequests");

            foreach (var rejected in sameAppointmentRejectedPatients.DistinctBy(a => a.UserId))
            {
                if (!string.IsNullOrWhiteSpace(rejected.Email))
                {
                    if (await QueueEmailIfAllowedAsync(
                        rejected.UserId,
                        rejected.Email!,
                        "Mentora - Randevu talebiniz güncellendi",
                        $"<p>{appointment.StartTime:dd.MM.yyyy HH:mm} saatindeki randevu başka bir hastaya verildi.</p><p>{System.Net.WebUtility.HtmlEncode(automaticRejectMessage)}</p>",
                        requireRequestStatusEmail: true))
                    {
                        queuedEmailCount++;
                    }
                }

                await _notificationService.CreateAsync(
                    rejected.UserId,
                    NotificationType.RequestRejected,
                    "Talebiniz otomatik reddedildi",
                    $"{appointment.StartTime:dd.MM.yyyy HH:mm} randevusu başka bir hastaya verildi.",
                    "/Request/MyRequests");
            }

            foreach (var affected in affectedPatients.DistinctBy(a => a.UserId))
            {
                if (!string.IsNullOrWhiteSpace(affected.Email))
                {
                    if (await QueueEmailIfAllowedAsync(
                        affected.UserId,
                        affected.Email!,
                        "Mentora - Randevu slotu güncellendi",
                        $"<p>{affected.StartTime:dd.MM.yyyy HH:mm} saatindeki talep ettiğiniz slot doktorun onaylı randevusuyla çakışacağı için kapatıldı.</p>",
                        requireRequestStatusEmail: true))
                    {
                        queuedEmailCount++;
                    }
                }

                await _notificationService.CreateAsync(
                    affected.UserId,
                    NotificationType.RequestRejected,
                    "Talebiniz otomatik reddedildi",
                    $"{affected.StartTime:dd.MM.yyyy HH:mm} slotu çakışma nedeniyle kapatıldı.",
                    "/Request/MyRequests");
            }

            var rejectedCount = sameAppointmentRejectedPatients.Count + conflictRejectedRequestCount;
            var successMessage = $"{approvedPatientName} adlı hastanın talebi onaylandı. " +
                $"{sameAppointmentRejectedPatients.Count} aynı randevu talebi reddedildi. " +
                $"{overlappingAppointments.Count} çakışan slot kapatıldı. " +
                $"{conflictRejectedRequestCount} çakışma talebi reddedildi. " +
                $"{queuedEmailCount} e-posta kuyruğa alındı.";

            if (rejectedCount == 0 && overlappingAppointments.Count == 0)
            {
                successMessage = $"{approvedPatientName} adlı hastanın talebi onaylandı. {queuedEmailCount} e-posta kuyruğa alındı.";
            }

            return ServiceResult.Success(successMessage);
        }

        public async Task<ServiceResult> RejectRequestAsync(int requestId, string responseMessage, string doctorId)
        {
            if (string.IsNullOrWhiteSpace(responseMessage))
            {
                return ServiceResult.Failure("Reddetme açıklaması boş olamaz.");
            }

            var request = await _context.AppointmentRequests
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return ServiceResult.Failure("Talep bulunamadi.");
            }

            if (request.DoctorId != doctorId)
            {
                return ServiceResult.Failure("Bu talebi reddetme yetkiniz yok.");
            }

            if (request.Status != RequestStatus.Pending)
            {
                return ServiceResult.Failure("Talep beklemede degil.");
            }

            request.Status = RequestStatus.Rejected;
            request.ResponseMessage = responseMessage;
            await _context.SaveChangesAsync();

            var rejectAppointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);
            var rejectDoctorName = rejectAppointment?.Doctor != null
                ? $"{rejectAppointment.Doctor.FirstName} {rejectAppointment.Doctor.LastName}".Trim()
                : "Doktor";

            if (!string.IsNullOrWhiteSpace(request.Patient?.Email))
            {
                var bodyBuilder = new System.Text.StringBuilder();
                bodyBuilder.Append("<h2 style='font-family:sans-serif;color:#b91c1c;'>Randevu Talebiniz Reddedildi</h2>");
                bodyBuilder.Append("<table style='font-family:sans-serif;border-collapse:collapse;'>");
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Doktor:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(rejectDoctorName)}</td></tr>");
                if (rejectAppointment != null)
                {
                    bodyBuilder.Append($"<tr><td style='padding:4px 8px;'><strong>Tarih/Saat:</strong></td><td style='padding:4px 8px;'>{rejectAppointment.StartTime:dd.MM.yyyy HH:mm} - {rejectAppointment.EndTime:HH:mm}</td></tr>");
                }
                bodyBuilder.Append($"<tr><td style='padding:4px 8px;vertical-align:top;'><strong>Açıklama:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(responseMessage)}</td></tr>");
                bodyBuilder.Append("</table>");
                bodyBuilder.Append("<p style='font-family:sans-serif;'>Diğer uygun randevuları incelemek için Mentora paneline giriş yapın.</p>");

                await QueueEmailIfAllowedAsync(
                    request.PatientId,
                    request.Patient.Email!,
                    "Mentora - Randevu talebiniz reddedildi",
                    bodyBuilder.ToString(),
                    requireRequestStatusEmail: true);
            }

            var notifMessage = rejectAppointment != null
                ? $"{rejectDoctorName} - {rejectAppointment.StartTime:dd.MM.yyyy HH:mm} talebinizi reddetti. Neden: {(responseMessage.Length > 80 ? responseMessage[..80] + "..." : responseMessage)}"
                : $"{rejectDoctorName} talebinizi reddetti. Neden: {(responseMessage.Length > 80 ? responseMessage[..80] + "..." : responseMessage)}";

            await _notificationService.CreateAsync(
                request.PatientId,
                NotificationType.RequestRejected,
                "Randevu talebiniz reddedildi",
                notifMessage,
                "/Request/MyRequests");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> CancelRequestAsync(int requestId, string patientId)
        {
            var request = await _context.AppointmentRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return ServiceResult.Failure("Talep bulunamadi.");
            }

            if (request.PatientId != patientId)
            {
                return ServiceResult.Failure("Bu talebi iptal etme yetkiniz yok.");
            }

            if (request.Status != RequestStatus.Pending)
            {
                return ServiceResult.Failure("Sadece bekleyen talepler iptal edilebilir.");
            }

            _context.AppointmentRequests.Remove(request);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        private async Task<bool> QueueEmailIfAllowedAsync(string userId, string recipientEmail, string subject, string htmlBody, bool requireRequestStatusEmail)
        {
            var preference = await _context.UserNotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (preference != null)
            {
                if (!preference.EmailEnabled)
                {
                    return false;
                }

                if (requireRequestStatusEmail && !preference.RequestStatusEmailsEnabled)
                {
                    return false;
                }
            }

            var scheduledFor = ComputeNextDigestSlot(preference?.EmailDigestMode ?? EmailDigestMode.Instant);

            await _emailOutboxService.QueueAsync(new EmailMessage
            {
                To = recipientEmail,
                Subject = subject,
                HtmlBody = htmlBody
            }, scheduledFor);

            return true;
        }

        private static string FormatPriceRange(decimal? minPrice, decimal? maxPrice)
        {
            if (!minPrice.HasValue && !maxPrice.HasValue)
            {
                return "-";
            }

            if (minPrice.HasValue && maxPrice.HasValue)
            {
                return $"{minPrice.Value:0} TL - {maxPrice.Value:0} TL";
            }

            return minPrice.HasValue
                ? $"{minPrice.Value:0} TL"
                : $"{maxPrice!.Value:0} TL";
        }

        private static void AppendOptionalInfoRow(System.Text.StringBuilder bodyBuilder, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            bodyBuilder.Append($"<tr><td style='padding:4px 8px;vertical-align:top;'><strong>{System.Net.WebUtility.HtmlEncode(label)}:</strong></td><td style='padding:4px 8px;'>{System.Net.WebUtility.HtmlEncode(value)}</td></tr>");
        }

        internal static DateTime ComputeNextDigestSlot(EmailDigestMode mode)
        {
            var utcNow = DateTime.UtcNow;
            return mode switch
            {
                EmailDigestMode.Hourly => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc).AddHours(1),
                EmailDigestMode.Daily => new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 6, 0, 0, DateTimeKind.Utc).AddDays(utcNow.Hour >= 6 ? 1 : 0),
                _ => utcNow
            };
        }
    }
}


