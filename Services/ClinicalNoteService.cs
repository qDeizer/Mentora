using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Data;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services.Email;
using PsikologProje_Void.Utils;
using PsikologProje_Void.ViewModels;

namespace PsikologProje_Void.Services
{
    public class ClinicalNoteService : IClinicalNoteService
    {
        private const int MaxNoteLength = 50000;
        private const int MaxCommentLength = 4000;

        private readonly ApplicationDbContext _context;
        private readonly IEmailOutboxService _emailOutboxService;
        private readonly INotificationService _notificationService;

        public ClinicalNoteService(
            ApplicationDbContext context,
            IEmailOutboxService emailOutboxService,
            INotificationService notificationService)
        {
            _context = context;
            _emailOutboxService = emailOutboxService;
            _notificationService = notificationService;
        }

        public async Task<ClinicalNotesDoctorDashboardViewModel> GetDoctorDashboardAsync(
            string doctorId,
            List<string>? patientIds = null,
            string? query = null,
            string? sortBy = null,
            string? sortDirection = null)
        {
            var relatedPatientIds = await GetRelatedPatientIdsForDoctorAsync(doctorId);
            var selectedPatientIds = (patientIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToHashSet();

            var normalizedQuery = (query ?? string.Empty).Trim().ToLowerInvariant();

            var notes = await _context.ClinicalNotes
                .Include(n => n.Patient)
                .Include(n => n.AuthorDoctor)
                .Include(n => n.Lock)
                .Include(n => n.Shares)
                    .ThenInclude(s => s.SharedByPatient)
                .Include(n => n.Shares)
                    .ThenInclude(s => s.SharedWithDoctor)
                .Include(n => n.AccessRules)
                    .ThenInclude(r => r.Doctor)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Doctor)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Patient)
                .AsNoTracking()
                .ToListAsync();

            var filtered = notes
                .Where(n => CanDoctorReadNote(n, doctorId, relatedPatientIds.Contains(n.PatientId)))
                .Where(n => selectedPatientIds.Count == 0 || selectedPatientIds.Contains(n.PatientId))
                .Where(n =>
                    string.IsNullOrWhiteSpace(normalizedQuery) ||
                    n.Content.ToLower().Contains(normalizedQuery) ||
                    ($"{n.Patient.FirstName} {n.Patient.LastName}").ToLower().Contains(normalizedQuery))
                .ToList();

            var normalizedSortBy = (sortBy ?? "createdAt").Trim().ToLowerInvariant();
            var normalizedSortDirection = (sortDirection ?? "desc").Trim().ToLowerInvariant();
            var sortDesc = normalizedSortDirection != "asc";

            filtered = normalizedSortBy switch
            {
                "updatedat" => sortDesc
                    ? filtered.OrderByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.CreatedAtUtc).ToList()
                    : filtered.OrderBy(n => n.UpdatedAtUtc).ThenBy(n => n.CreatedAtUtc).ToList(),
                "patient" => sortDesc
                    ? filtered.OrderByDescending(n => n.Patient.FirstName).ThenByDescending(n => n.Patient.LastName).ToList()
                    : filtered.OrderBy(n => n.Patient.FirstName).ThenBy(n => n.Patient.LastName).ToList(),
                _ => sortDesc
                    ? filtered.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.UpdatedAtUtc).ToList()
                    : filtered.OrderBy(n => n.CreatedAtUtc).ThenBy(n => n.UpdatedAtUtc).ToList()
            };

            var patientProfiles = await _context.Users
                .OfType<Patient>()
                .Where(p => relatedPatientIds.Contains(p.Id))
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new ClinicalNotePatientOptionViewModel
                {
                    Id = p.Id,
                    Name = p.FirstName + " " + p.LastName,
                    ProfilePhotoPath = p.ProfilePhotoPath
                })
                .ToListAsync();

            var patientOptions = patientProfiles
                .Select(p => new SelectListItem
                {
                    Value = p.Id,
                    Text = p.Name
                })
                .ToList();

            return new ClinicalNotesDoctorDashboardViewModel
            {
                Notes = filtered.Select(n =>
                {
                    var activeShare = n.Shares
                        .Where(s => s.SharedWithDoctorId == doctorId && s.RevokedAtUtc == null)
                        .OrderByDescending(s => s.SharedAtUtc)
                        .FirstOrDefault();

                    return new ClinicalNoteDoctorItemViewModel
                    {
                        Id = n.Id,
                        PatientId = n.PatientId,
                        PatientName = n.Patient.FirstName + " " + n.Patient.LastName,
                        PatientProfilePhotoPath = n.Patient.ProfilePhotoPath,
                        PatientEmail = n.Patient.Email,
                        PatientPhoneNumber = n.Patient.PhoneNumber,
                        PatientBirthDate = n.Patient.BirthDate,
                        AuthorDoctorName = n.AuthorDoctor.FirstName + " " + n.AuthorDoctor.LastName,
                        SourceLabel = n.AuthorDoctorId == doctorId
                            ? "Sizin notunuz"
                            : (n.Visibility == ClinicalNoteVisibility.Public ? "Açık not" : "Paylasilan not"),
                        SharedByPatientName = activeShare != null
                            ? activeShare.SharedByPatient.FirstName + " " + activeShare.SharedByPatient.LastName
                            : null,
                        SharedAtUtc = activeShare?.SharedAtUtc,
                        Content = n.Content,
                        PreviewContent = BuildPreview(n.Content, 280),
                        CreatedAtUtc = n.CreatedAtUtc,
                        UpdatedAtUtc = n.UpdatedAtUtc,
                        CanEdit = n.AuthorDoctorId == doctorId,
                        CanToggleLock = n.AuthorDoctorId == doctorId,
                        IsLockedForPatient = n.Lock?.IsLockedForPatient ?? false,
                        Comments = MapCommentsForDoctor(n, doctorId)
                    };
                }).ToList(),
                PatientOptions = patientOptions,
                PatientProfiles = patientProfiles,
                CreateForm = new ClinicalNoteCreateViewModel(),
                FilterPatientIds = selectedPatientIds.ToList(),
                SearchTerm = query,
                SortBy = normalizedSortBy,
                SortDirection = normalizedSortDirection
            };
        }

        public async Task<ServiceResult> CreateNoteAsync(string doctorId, ClinicalNoteCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PatientId))
            {
                return ServiceResult.Failure("Hasta seçmelisiniz.");
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return ServiceResult.Failure("Not içeriği boş olamaz.");
            }

            var content = model.Content.Trim();
            if (content.Length > MaxNoteLength)
            {
                return ServiceResult.Failure($"Not icerigi {MaxNoteLength} karakteri gecemez.");
            }

            var patient = await _context.Users.OfType<Patient>()
                .FirstOrDefaultAsync(p => p.Id == model.PatientId);
            if (patient == null)
            {
                return ServiceResult.Failure("Hasta bulunamadi.");
            }

            var doctorHasRelation = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.PatientId == model.PatientId);

            if (!doctorHasRelation)
            {
                var requestRelation = await _context.AppointmentRequests.AnyAsync(r =>
                    r.DoctorId == doctorId && r.PatientId == model.PatientId);
                if (!requestRelation)
                {
                    return ServiceResult.Failure("Bu hasta için not oluşturma yetkiniz yok.");
                }
            }

            if (model.AppointmentId.HasValue)
            {
                var appointmentValid = await _context.Appointments.AnyAsync(a =>
                    a.Id == model.AppointmentId.Value &&
                    a.DoctorId == doctorId &&
                    a.PatientId == model.PatientId);

                if (!appointmentValid)
                {
                    return ServiceResult.Failure("Seçilen randevu bu hasta ve doktorla eşleşmiyor.");
                }
            }

            var defaultVisibility = await _context.UserNotificationPreferences
                .Where(p => p.UserId == model.PatientId)
                .Select(p => (ClinicalNoteVisibility?)p.DefaultClinicalNoteVisibility)
                .FirstOrDefaultAsync() ?? ClinicalNoteVisibility.Private;

            var now = DateTime.UtcNow;
            var note = new ClinicalNote
            {
                PatientId = model.PatientId,
                AuthorDoctorId = doctorId,
                AppointmentId = model.AppointmentId,
                Content = content,
                Visibility = defaultVisibility,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.ClinicalNotes.Add(note);
            await _context.SaveChangesAsync();

            if (model.IsLockedForPatient)
            {
                _context.ClinicalNoteLocks.Add(new ClinicalNoteLock
                {
                    ClinicalNoteId = note.Id,
                    LockedByDoctorId = doctorId,
                    IsLockedForPatient = true,
                    LockedAtUtc = now,
                    UpdatedAtUtc = now
                });
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateNoteAsync(string doctorId, int noteId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ServiceResult.Failure("Not içeriği boş olamaz.");
            }

            var normalized = content.Trim();
            if (normalized.Length > MaxNoteLength)
            {
                return ServiceResult.Failure($"Not icerigi {MaxNoteLength} karakteri gecemez.");
            }

            var note = await _context.ClinicalNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return ServiceResult.Failure("Not bulunamadi.");
            }

            if (note.AuthorDoctorId != doctorId)
            {
                return ServiceResult.Failure("Sadece sizin oluşturduğunuz notları düzenleyebilirsiniz.");
            }

            note.Content = normalized;
            note.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ToggleLockAsync(string doctorId, int noteId, bool isLockedForPatient)
        {
            var note = await _context.ClinicalNotes
                .Include(n => n.Lock)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return ServiceResult.Failure("Not bulunamadi.");
            }

            if (note.AuthorDoctorId != doctorId)
            {
                return ServiceResult.Failure("Bu notun kilidini degistirme yetkiniz yok.");
            }

            var now = DateTime.UtcNow;
            if (note.Lock == null)
            {
                _context.ClinicalNoteLocks.Add(new ClinicalNoteLock
                {
                    ClinicalNoteId = note.Id,
                    LockedByDoctorId = doctorId,
                    IsLockedForPatient = isLockedForPatient,
                    LockedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                note.Lock.IsLockedForPatient = isLockedForPatient;
                note.Lock.UpdatedAtUtc = now;
                note.Lock.LockedByDoctorId = doctorId;
            }

            note.UpdatedAtUtc = now;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }
        public async Task<ClinicalNotesPatientDashboardViewModel> GetPatientDashboardAsync(string patientId, ClinicalNotesMyNotesFilterViewModel? filter = null)
        {
            filter ??= new ClinicalNotesMyNotesFilterViewModel();
            filter.SortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "createdAt" : filter.SortBy.Trim();
            filter.SortDirection = string.IsNullOrWhiteSpace(filter.SortDirection) ? "desc" : filter.SortDirection.Trim().ToLowerInvariant();

            var selectedVisibilities = (filter.SelectedVisibilities ?? new List<ClinicalNoteVisibility>())
                .Distinct()
                .ToList();

            var selectedDoctorIds = (filter.SelectedDoctorIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var normalizedSearch = (filter.SearchTerm ?? string.Empty).Trim().ToLowerInvariant();

            var noteQuery = _context.ClinicalNotes
                .Include(n => n.AuthorDoctor)
                .Include(n => n.Lock)
                .Include(n => n.Shares)
                    .ThenInclude(s => s.SharedWithDoctor)
                .Include(n => n.AccessRules)
                    .ThenInclude(r => r.Doctor)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Doctor)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Patient)
                .Where(n => n.PatientId == patientId)
                .AsQueryable();

            // Varsayılan: hastalar gizli notları görmesin
            if (selectedVisibilities.Count > 0)
            {
                if (selectedVisibilities.Count < Enum.GetValues<ClinicalNoteVisibility>().Length)
                {
                    noteQuery = noteQuery.Where(n => selectedVisibilities.Contains(n.Visibility));
                }
            }
            else
            {
                noteQuery = noteQuery.Where(n => n.Visibility != ClinicalNoteVisibility.Private);
            }

            if (selectedDoctorIds.Count > 0)
            {
                noteQuery = noteQuery.Where(n =>
                    selectedDoctorIds.Contains(n.AuthorDoctorId) ||
                    n.AccessRules.Any(r => selectedDoctorIds.Contains(r.DoctorId) && r.RevokedAtUtc == null) ||
                    n.Shares.Any(s => selectedDoctorIds.Contains(s.SharedWithDoctorId) && s.RevokedAtUtc == null));
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                noteQuery = noteQuery.Where(n =>
                    n.Content.ToLower().Contains(normalizedSearch) ||
                    (n.AuthorDoctor.FirstName + " " + n.AuthorDoctor.LastName).ToLower().Contains(normalizedSearch));
            }

            var directionDesc = filter.SortDirection != "asc";
            var normalizedSortBy = filter.SortBy.Trim().ToLowerInvariant();
            noteQuery = normalizedSortBy switch
            {
                "doctor" => directionDesc
                    ? noteQuery.OrderByDescending(n => n.AuthorDoctor.FirstName).ThenByDescending(n => n.AuthorDoctor.LastName)
                    : noteQuery.OrderBy(n => n.AuthorDoctor.FirstName).ThenBy(n => n.AuthorDoctor.LastName),
                "updatedat" => directionDesc
                    ? noteQuery.OrderByDescending(n => n.UpdatedAtUtc)
                    : noteQuery.OrderBy(n => n.UpdatedAtUtc),
                _ => directionDesc
                    ? noteQuery.OrderByDescending(n => n.CreatedAtUtc)
                    : noteQuery.OrderBy(n => n.CreatedAtUtc)
            };

            var notes = await noteQuery.ToListAsync();
            var doctors = await BuildDoctorOptionsForPatientAsync(patientId);

            return new ClinicalNotesPatientDashboardViewModel
            {
                Notes = notes.Select(n =>
                {
                    var isLockedForPatient = n.Lock?.IsLockedForPatient ?? false;
                    var activeShareRules = n.AccessRules
                        .Where(r => r.RuleType == ClinicalNoteAccessRuleType.Share && r.RevokedAtUtc == null)
                        .GroupBy(r => r.DoctorId)
                        .Select(g => g.First())
                        .ToList();

                    var legacyShares = n.Shares
                        .Where(s => s.RevokedAtUtc == null)
                        .Select(s => new ClinicalNoteDoctorOptionViewModel
                        {
                            Id = s.SharedWithDoctorId,
                            Name = s.SharedWithDoctor.FirstName + " " + s.SharedWithDoctor.LastName,
                            ProfilePhotoPath = s.SharedWithDoctor.ProfilePhotoPath
                        })
                        .ToList();

                    var sharedDoctors = activeShareRules
                        .Select(r => new ClinicalNoteDoctorOptionViewModel
                        {
                            Id = r.DoctorId,
                            Name = r.Doctor.FirstName + " " + r.Doctor.LastName,
                            ProfilePhotoPath = r.Doctor.ProfilePhotoPath
                        })
                        .Concat(legacyShares)
                        .GroupBy(x => x.Id)
                        .Select(g => g.First())
                        .OrderBy(x => x.Name)
                        .ToList();

                    var blockedDoctors = n.AccessRules
                        .Where(r => r.RuleType == ClinicalNoteAccessRuleType.Block && r.RevokedAtUtc == null)
                        .GroupBy(r => r.DoctorId)
                        .Select(g => g.First())
                        .Select(r => new ClinicalNoteDoctorOptionViewModel
                        {
                            Id = r.DoctorId,
                            Name = r.Doctor.FirstName + " " + r.Doctor.LastName,
                            ProfilePhotoPath = r.Doctor.ProfilePhotoPath
                        })
                        .OrderBy(x => x.Name)
                        .ToList();

                    var readable = !isLockedForPatient;

                    return new ClinicalNotePatientItemViewModel
                    {
                        Id = n.Id,
                        AuthorDoctorId = n.AuthorDoctorId,
                        AuthorDoctorName = n.AuthorDoctor.FirstName + " " + n.AuthorDoctor.LastName,
                        AuthorDoctorProfilePhotoPath = n.AuthorDoctor.ProfilePhotoPath,
                        Content = readable ? n.Content : "Bu not doktor tarafindan kilitlendi.",
                        PreviewContent = readable ? BuildPreview(n.Content, 220) : "Bu not doktor tarafindan kilitlendi.",
                        CreatedAtUtc = n.CreatedAtUtc,
                        UpdatedAtUtc = n.UpdatedAtUtc,
                        Visibility = n.Visibility,
                        VisibilityLabel = ToVisibilityLabel(n.Visibility),
                        IsLockedForPatient = isLockedForPatient,
                        CanReadContent = readable,
                        CanComment = readable,
                        ShareAudit = n.Shares
                            .OrderByDescending(s => s.SharedAtUtc)
                            .Select(s => new ClinicalNoteShareAuditItemViewModel
                            {
                                DoctorId = s.SharedWithDoctorId,
                                DoctorName = s.SharedWithDoctor.FirstName + " " + s.SharedWithDoctor.LastName,
                                DoctorProfilePhotoPath = s.SharedWithDoctor.ProfilePhotoPath,
                                SharedAtUtc = s.SharedAtUtc,
                                RevokedAtUtc = s.RevokedAtUtc
                            })
                            .ToList(),
                        SharedDoctors = sharedDoctors,
                        BlockedDoctors = blockedDoctors,
                        Comments = MapCommentsForPatient(n, patientId, readable)
                    };
                }).ToList(),
                Filter = filter,
                ShareDoctorOptions = doctors
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id,
                        Text = d.Name
                    })
                    .ToList(),
                ShareDoctorProfiles = doctors,
                FilterDoctorProfiles = doctors,
                AllDoctorProfiles = doctors
            };
        }

        public async Task<ServiceResult> ShareNotesAsync(string patientId, string targetDoctorId, List<int> noteIds)
        {
            targetDoctorId = (targetDoctorId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(targetDoctorId))
            {
                return ServiceResult.Failure("Paylaşım için doktor seçmelisiniz.");
            }

            var uniqueNoteIds = (noteIds ?? new List<int>()).Distinct().ToList();
            if (uniqueNoteIds.Count == 0)
            {
                return ServiceResult.Failure("Paylaşılacak en az bir not seçmelisiniz.");
            }

            var doctor = await _context.Users.OfType<Doctor>().FirstOrDefaultAsync(d => d.Id == targetDoctorId);
            if (doctor == null)
            {
                return ServiceResult.Failure("Seçilen doktor bulunamadı.");
            }

            var notes = await _context.ClinicalNotes
                .Include(n => n.AccessRules)
                .Include(n => n.Shares)
                .Where(n => n.PatientId == patientId && uniqueNoteIds.Contains(n.Id))
                .ToListAsync();

            if (notes.Count != uniqueNoteIds.Count)
            {
                return ServiceResult.Failure("Bazi notlar size ait degil veya bulunamadi.");
            }

            var now = DateTime.UtcNow;
            var changed = 0;

            foreach (var note in notes)
            {
                var applyResult = await ApplyRuleAsync(note, patientId, targetDoctorId, ClinicalNoteAccessRuleType.Share, true, now);
                if (!applyResult.Succeeded)
                {
                    return applyResult;
                }

                var share = note.Shares.FirstOrDefault(s => s.SharedWithDoctorId == targetDoctorId);
                if (share == null)
                {
                    note.Shares.Add(new ClinicalNoteShare
                    {
                        ClinicalNoteId = note.Id,
                        SharedByPatientId = patientId,
                        SharedWithDoctorId = targetDoctorId,
                        SharedAtUtc = now
                    });
                    changed++;
                }
                else if (share.RevokedAtUtc.HasValue)
                {
                    share.RevokedAtUtc = null;
                    share.RevokedByPatientId = null;
                    share.SharedAtUtc = now;
                    changed++;
                }
            }

            await _context.SaveChangesAsync();

            if (changed > 0)
            {
                await _notificationService.CreateAsync(
                    doctor.Id,
                    NotificationType.ClinicalNoteShared,
                    "Yeni not paylasimi",
                    $"Bir hasta sizinle {changed} not paylasti.",
                    "/ClinicalNotes");

                if (!string.IsNullOrWhiteSpace(doctor.Email))
                {
                    await _emailOutboxService.QueueAsync(new EmailMessage
                    {
                        To = doctor.Email!,
                        Subject = "Mentora - Yeni paylasilan hasta notu",
                        HtmlBody = $"<p>Bir hasta sizinle <strong>{changed}</strong> adet not paylaştı.</p><p>Detaylar için Mentora paneline giriş yapın.</p>"
                    });
                }
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RevokeShareAsync(string patientId, string targetDoctorId, List<int> noteIds)
        {
            targetDoctorId = (targetDoctorId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(targetDoctorId))
            {
                return ServiceResult.Failure("Geri alma için doktor seçmelisiniz.");
            }

            var uniqueNoteIds = (noteIds ?? new List<int>()).Distinct().ToList();
            if (uniqueNoteIds.Count == 0)
            {
                return ServiceResult.Failure("Geri alınacak en az bir not seçmelisiniz.");
            }

            var notes = await _context.ClinicalNotes
                .Include(n => n.AccessRules)
                .Include(n => n.Shares)
                .Where(n => n.PatientId == patientId && uniqueNoteIds.Contains(n.Id))
                .ToListAsync();

            if (notes.Count != uniqueNoteIds.Count)
            {
                return ServiceResult.Failure("Bazi notlar bulunamadi veya size ait degil.");
            }

            var now = DateTime.UtcNow;
            foreach (var note in notes)
            {
                var applyResult = await ApplyRuleAsync(note, patientId, targetDoctorId, ClinicalNoteAccessRuleType.Share, false, now);
                if (!applyResult.Succeeded)
                {
                    return applyResult;
                }

                var share = note.Shares.FirstOrDefault(s => s.SharedWithDoctorId == targetDoctorId && s.RevokedAtUtc == null);
                if (share != null)
                {
                    share.RevokedAtUtc = now;
                    share.RevokedByPatientId = patientId;
                }
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }
        public async Task<ServiceResult> UpdateNoteVisibilityAsync(string patientId, int noteId, ClinicalNoteVisibility visibility)
        {
            var note = await _context.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.PatientId == patientId);
            if (note == null)
            {
                return ServiceResult.Failure("Not bulunamadi.");
            }

            note.Visibility = visibility;
            note.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> BulkUpdateVisibilityAsync(string patientId, List<int> noteIds, ClinicalNoteVisibility visibility)
        {
            var uniqueIds = noteIds.Distinct().ToList();
            if (uniqueIds.Count == 0)
            {
                return ServiceResult.Failure("En az bir not seçmelisiniz.");
            }

            var notes = await _context.ClinicalNotes
                .Where(n => n.PatientId == patientId && uniqueIds.Contains(n.Id))
                .ToListAsync();

            if (notes.Count != uniqueIds.Count)
            {
                return ServiceResult.Failure("Bazi notlar bulunamadi veya size ait degil.");
            }

            var now = DateTime.UtcNow;
            foreach (var note in notes)
            {
                note.Visibility = visibility;
                note.UpdatedAtUtc = now;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateAccessRuleAsync(string patientId, ClinicalNoteAccessRuleCommandViewModel model)
        {
            var note = await _context.ClinicalNotes
                .Include(n => n.AccessRules)
                .Include(n => n.Shares)
                .FirstOrDefaultAsync(n => n.Id == model.NoteId && n.PatientId == patientId);

            if (note == null)
            {
                return ServiceResult.Failure("Not bulunamadi.");
            }

            var now = DateTime.UtcNow;
            var result = await ApplyRuleAsync(note, patientId, model.DoctorId, model.RuleType, model.Enabled, now);
            if (!result.Succeeded)
            {
                return result;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ApplyBulkActionsAsync(string patientId, ClinicalNoteBulkActionInputViewModel model)
        {
            var uniqueIds = (model.NoteIds ?? new List<int>()).Distinct().ToList();
            if (uniqueIds.Count == 0)
            {
                return ServiceResult.Failure("Çoklu işlem için en az bir not seçmelisiniz.");
            }

            var notes = await _context.ClinicalNotes
                .Include(n => n.AccessRules)
                .Include(n => n.Shares)
                .Where(n => n.PatientId == patientId && uniqueIds.Contains(n.Id))
                .ToListAsync();

            if (notes.Count != uniqueIds.Count)
            {
                return ServiceResult.Failure("Seçilen notlardan bazıları bulunamadı.");
            }

            var now = DateTime.UtcNow;
            foreach (var note in notes)
            {
                if (model.Visibility.HasValue)
                {
                    note.Visibility = model.Visibility.Value;
                    note.UpdatedAtUtc = now;
                }

                foreach (var doctorId in (model.AddShareDoctorIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    var result = await ApplyRuleAsync(note, patientId, doctorId, ClinicalNoteAccessRuleType.Share, true, now);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }

                foreach (var doctorId in (model.RemoveShareDoctorIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    var result = await ApplyRuleAsync(note, patientId, doctorId, ClinicalNoteAccessRuleType.Share, false, now);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }

                foreach (var doctorId in (model.AddBlockDoctorIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    var result = await ApplyRuleAsync(note, patientId, doctorId, ClinicalNoteAccessRuleType.Block, true, now);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }

                foreach (var doctorId in (model.RemoveBlockDoctorIds ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    var result = await ApplyRuleAsync(note, patientId, doctorId, ClinicalNoteAccessRuleType.Block, false, now);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> AddCommentAsync(string actorUserId, bool actorIsDoctor, int noteId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ServiceResult.Failure("Yorum içeriği boş olamaz.");
            }

            var normalized = content.Trim();
            if (normalized.Length > MaxCommentLength)
            {
                return ServiceResult.Failure($"Yorum {MaxCommentLength} karakteri gecemez.");
            }

            var note = await _context.ClinicalNotes
                .Include(n => n.Patient)
                .Include(n => n.AuthorDoctor)
                .Include(n => n.Lock)
                .Include(n => n.Shares)
                .Include(n => n.AccessRules)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return ServiceResult.Failure("Not bulunamadi.");
            }

            var access = await CanActorReadNoteAsync(note, actorUserId, actorIsDoctor);
            if (!access.Succeeded)
            {
                return access;
            }

            var comment = new ClinicalNoteComment
            {
                ClinicalNoteId = note.Id,
                Content = normalized,
                DoctorId = actorIsDoctor ? actorUserId : null,
                PatientId = actorIsDoctor ? null : actorUserId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.ClinicalNoteComments.Add(comment);
            await _context.SaveChangesAsync();

            if (actorIsDoctor)
            {
                await _notificationService.CreateAsync(
                    note.PatientId,
                    NotificationType.ClinicalNoteComment,
                    "Klinik nota yeni yorum",
                    $"{note.AuthorDoctor.FirstName} {note.AuthorDoctor.LastName} bir nota yorum ekledi.",
                    "/ClinicalNotes/MyNotes");
            }
            else
            {
                await _notificationService.CreateAsync(
                    note.AuthorDoctorId,
                    NotificationType.ClinicalNoteComment,
                    "Hasta yorum ekledi",
                    "Hastaniz bir klinik nota yorum ekledi.",
                    "/ClinicalNotes");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateCommentAsync(string actorUserId, bool actorIsDoctor, int commentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ServiceResult.Failure("Yorum içeriği boş olamaz.");
            }

            var normalized = content.Trim();
            if (normalized.Length > MaxCommentLength)
            {
                return ServiceResult.Failure($"Yorum {MaxCommentLength} karakteri gecemez.");
            }

            var comment = await _context.ClinicalNoteComments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return ServiceResult.Failure("Yorum bulunamadi.");
            }

            if (actorIsDoctor)
            {
                if (comment.DoctorId != actorUserId)
                {
                    return ServiceResult.Failure("Bu yorumu düzenleme yetkiniz yok.");
                }
            }
            else
            {
                if (comment.PatientId != actorUserId)
                {
                    return ServiceResult.Failure("Bu yorumu düzenleme yetkiniz yok.");
                }
            }

            comment.Content = normalized;
            comment.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteCommentAsync(string actorUserId, bool actorIsDoctor, int commentId)
        {
            var comment = await _context.ClinicalNoteComments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return ServiceResult.Failure("Yorum bulunamadi.");
            }

            if (actorIsDoctor)
            {
                if (comment.DoctorId != actorUserId)
                {
                    return ServiceResult.Failure("Bu yorumu silme yetkiniz yok.");
                }
            }
            else
            {
                if (comment.PatientId != actorUserId)
                {
                    return ServiceResult.Failure("Bu yorumu silme yetkiniz yok.");
                }
            }

            _context.ClinicalNoteComments.Remove(comment);
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ToggleCommentLockAsync(string doctorId, int commentId, bool isLockedForPatient)
        {
            var comment = await _context.ClinicalNoteComments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return ServiceResult.Failure("Yorum bulunamadi.");
            }

            if (string.IsNullOrWhiteSpace(comment.DoctorId))
            {
                return ServiceResult.Failure("Sadece doktor yorumlari hastaya kilitlenebilir.");
            }

            if (comment.DoctorId != doctorId)
            {
                return ServiceResult.Failure("Bu yorumun kilidini degistirme yetkiniz yok.");
            }

            comment.IsLockedForPatient = isLockedForPatient;
            comment.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Success();
        }

        private async Task<HashSet<string>> GetRelatedPatientIdsForDoctorAsync(string doctorId)
        {
            var appointmentPatientIds = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.PatientId != null)
                .Select(a => a.PatientId!)
                .Distinct()
                .ToListAsync();

            var requestPatientIds = await _context.AppointmentRequests
                .Where(r => r.DoctorId == doctorId)
                .Select(r => r.PatientId)
                .Distinct()
                .ToListAsync();

            return appointmentPatientIds
                .Union(requestPatientIds)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet();
        }

        private async Task<List<ClinicalNoteDoctorOptionViewModel>> BuildDoctorOptionsForPatientAsync(string patientId)
        {
            var relatedDoctorIds = await _context.Appointments
                .Where(a => a.PatientId == patientId)
                .Select(a => a.DoctorId)
                .Union(_context.AppointmentRequests.Where(r => r.PatientId == patientId).Select(r => r.DoctorId))
                .Union(_context.ClinicalNotes.Where(n => n.PatientId == patientId).Select(n => n.AuthorDoctorId))
                .Union(_context.ClinicalNoteShares.Where(s => s.ClinicalNote.PatientId == patientId).Select(s => s.SharedWithDoctorId))
                .Union(_context.ClinicalNoteAccessRules.Where(r => r.ClinicalNote.PatientId == patientId).Select(r => r.DoctorId))
                .Distinct()
                .ToListAsync();

            return await _context.Users
                .OfType<Doctor>()
                .Select(d => new
                {
                    Doctor = d,
                    IsRelated = relatedDoctorIds.Contains(d.Id)
                })
                .OrderByDescending(x => x.IsRelated)
                .ThenBy(x => x.Doctor.FirstName)
                .ThenBy(x => x.Doctor.LastName)
                .Select(x => new ClinicalNoteDoctorOptionViewModel
                {
                    Id = x.Doctor.Id,
                    Name = x.Doctor.FirstName + " " + x.Doctor.LastName,
                    ProfilePhotoPath = x.Doctor.ProfilePhotoPath
                })
                .ToListAsync();
        }

        private static bool CanDoctorReadNote(ClinicalNote note, string doctorId, bool doctorHasPatientRelation)
        {
            var isAuthor = note.AuthorDoctorId == doctorId;
            if (isAuthor)
            {
                return true;
            }

            var isBlocked = note.AccessRules.Any(r =>
                r.DoctorId == doctorId &&
                r.RuleType == ClinicalNoteAccessRuleType.Block &&
                r.RevokedAtUtc == null);
            if (isBlocked)
            {
                return false;
            }

            var hasShareRule = note.AccessRules.Any(r =>
                r.DoctorId == doctorId &&
                r.RuleType == ClinicalNoteAccessRuleType.Share &&
                r.RevokedAtUtc == null);

            var hasLegacyShare = note.Shares.Any(s =>
                s.SharedWithDoctorId == doctorId &&
                s.RevokedAtUtc == null);

            if (hasShareRule || hasLegacyShare)
            {
                return true;
            }

            return note.Visibility == ClinicalNoteVisibility.Public && doctorHasPatientRelation;
        }

        private async Task<ServiceResult> CanActorReadNoteAsync(ClinicalNote note, string actorUserId, bool actorIsDoctor)
        {
            if (actorIsDoctor)
            {
                var hasRelation = await _context.Appointments.AnyAsync(a => a.DoctorId == actorUserId && a.PatientId == note.PatientId) ||
                                  await _context.AppointmentRequests.AnyAsync(r => r.DoctorId == actorUserId && r.PatientId == note.PatientId);

                if (!CanDoctorReadNote(note, actorUserId, hasRelation))
                {
                    return ServiceResult.Failure("Bu notu görüntüleme yetkiniz yok.");
                }

                return ServiceResult.Success();
            }

            if (note.PatientId != actorUserId)
            {
                return ServiceResult.Failure("Bu notu görüntüleme yetkiniz yok.");
            }

            if (note.Lock?.IsLockedForPatient == true)
            {
                return ServiceResult.Failure("Bu not doktor tarafindan kilitlendi.");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ApplyRuleAsync(
            ClinicalNote note,
            string patientId,
            string doctorId,
            ClinicalNoteAccessRuleType ruleType,
            bool enabled,
            DateTime now)
        {
            if (string.IsNullOrWhiteSpace(doctorId))
            {
                return ServiceResult.Failure("Doktor seçimi geçersiz.");
            }

            var doctorExists = await _context.Users.OfType<Doctor>().AnyAsync(d => d.Id == doctorId);
            if (!doctorExists)
            {
                return ServiceResult.Failure("Seçilen doktor bulunamadı.");
            }

            if (ruleType == ClinicalNoteAccessRuleType.Block && note.AuthorDoctorId == doctorId && enabled)
            {
                return ServiceResult.Failure("Notun yazari doktoru bloklayamazsiniz.");
            }

            var existing = note.AccessRules
                .Where(r => r.DoctorId == doctorId && r.RuleType == ruleType)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefault();

            if (enabled)
            {
                if (existing == null)
                {
                    note.AccessRules.Add(new ClinicalNoteAccessRule
                    {
                        ClinicalNoteId = note.Id,
                        DoctorId = doctorId,
                        RuleType = ruleType,
                        CreatedByPatientId = patientId,
                        CreatedAtUtc = now,
                        RevokedAtUtc = null
                    });
                }
                else
                {
                    existing.RevokedAtUtc = null;
                    existing.CreatedByPatientId = patientId;
                    if (existing.CreatedAtUtc == default)
                    {
                        existing.CreatedAtUtc = now;
                    }
                }

                if (ruleType == ClinicalNoteAccessRuleType.Block)
                {
                    var shareRule = note.AccessRules
                        .Where(r => r.DoctorId == doctorId && r.RuleType == ClinicalNoteAccessRuleType.Share && r.RevokedAtUtc == null)
                        .ToList();
                    foreach (var share in shareRule)
                    {
                        share.RevokedAtUtc = now;
                    }

                    var legacyShare = note.Shares.FirstOrDefault(s => s.SharedWithDoctorId == doctorId && s.RevokedAtUtc == null);
                    if (legacyShare != null)
                    {
                        legacyShare.RevokedAtUtc = now;
                        legacyShare.RevokedByPatientId = patientId;
                    }
                }
            }
            else
            {
                if (existing != null && existing.RevokedAtUtc == null)
                {
                    existing.RevokedAtUtc = now;
                }
            }

            note.UpdatedAtUtc = now;
            return ServiceResult.Success();
        }

        private static List<ClinicalNoteCommentViewModel> MapCommentsForDoctor(ClinicalNote note, string doctorId)
        {
            return note.Comments
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new ClinicalNoteCommentViewModel
                {
                    Id = c.Id,
                    IsDoctorComment = !string.IsNullOrWhiteSpace(c.DoctorId),
                    AuthorId = c.DoctorId ?? c.PatientId ?? string.Empty,
                    AuthorName = c.Doctor != null
                        ? $"{c.Doctor.FirstName} {c.Doctor.LastName}"
                        : (c.Patient != null ? $"{c.Patient.FirstName} {c.Patient.LastName}" : "Kullanıcı"),
                    AuthorProfilePhotoPath = c.Doctor?.ProfilePhotoPath ?? c.Patient?.ProfilePhotoPath,
                    Content = c.Content,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc,
                    CanEdit = c.DoctorId == doctorId,
                    CanDelete = c.DoctorId == doctorId,
                    IsLockedForPatient = c.IsLockedForPatient,
                    CanToggleLock = c.DoctorId == doctorId,
                    VisibleToPatient = !c.IsLockedForPatient
                })
                .ToList();
        }

        private static List<ClinicalNoteCommentViewModel> MapCommentsForPatient(ClinicalNote note, string patientId, bool noteReadable)
        {
            if (!noteReadable)
            {
                return new List<ClinicalNoteCommentViewModel>();
            }

            return note.Comments
                .Where(c => !(c.IsLockedForPatient && !string.IsNullOrWhiteSpace(c.DoctorId)))
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new ClinicalNoteCommentViewModel
                {
                    Id = c.Id,
                    IsDoctorComment = !string.IsNullOrWhiteSpace(c.DoctorId),
                    AuthorId = c.DoctorId ?? c.PatientId ?? string.Empty,
                    AuthorName = c.Doctor != null
                        ? $"{c.Doctor.FirstName} {c.Doctor.LastName}"
                        : (c.Patient != null ? $"{c.Patient.FirstName} {c.Patient.LastName}" : "Kullanıcı"),
                    AuthorProfilePhotoPath = c.Doctor?.ProfilePhotoPath ?? c.Patient?.ProfilePhotoPath,
                    Content = c.Content,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc,
                    CanEdit = c.PatientId == patientId,
                    CanDelete = c.PatientId == patientId,
                    IsLockedForPatient = false,
                    CanToggleLock = false,
                    VisibleToPatient = true
                })
                .ToList();
        }

        private static string ToVisibilityLabel(ClinicalNoteVisibility visibility)
        {
            return visibility switch
            {
                ClinicalNoteVisibility.Private => "Gizli",
                ClinicalNoteVisibility.Public => "Açık",
                ClinicalNoteVisibility.Shared => "Paylaşılan",
                _ => visibility.ToString()
            };
        }

        private static string BuildPreview(string content, int limit)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = content.Trim();
            if (normalized.Length <= limit)
            {
                return normalized;
            }

            return normalized.Substring(0, limit).TrimEnd() + "...";
        }
    }
}
