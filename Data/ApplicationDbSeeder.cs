using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using PsikologProje_Void.Models;

namespace PsikologProje_Void.Data
{
    public static class ApplicationDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedDemoData = true)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            await EnsureRolesAsync(roleManager);
            const string demoPassword = "asdasd";
            await EnsureAdminUserAsync(userManager, "admin", demoPassword);

            if (!seedDemoData)
            {
                return;
            }

            var doctor1Email = configuration["Seed:DemoDoctorEmail"];
            if (string.IsNullOrWhiteSpace(doctor1Email))
            {
                doctor1Email = configuration["Smtp:FromEmail"];
            }
            if (string.IsNullOrWhiteSpace(doctor1Email))
            {
                doctor1Email = "demo.doktor1@mentora.local";
            }

            var patient1Email = configuration["Seed:DemoPatientEmail"];
            if (string.IsNullOrWhiteSpace(patient1Email) || string.Equals(patient1Email, doctor1Email, StringComparison.OrdinalIgnoreCase))
            {
                patient1Email = "demo.hasta1@mentora.local";
            }

            var doctor1 = await EnsureDoctorAsync(userManager, doctor1Email, demoPassword, "Deniz", "Yılmaz", "+905000000001", 41.015137, 28.979530);
            var doctor2 = await EnsureDoctorAsync(userManager, "demo.doktor2@mentora.local", demoPassword, "Ayşe", "Kara", "+905000000003", 41.008240, 28.978359);
            var patient1 = await EnsurePatientAsync(userManager, patient1Email, demoPassword, "Mert", "Demir", "+905000000002", 41.036896, 28.985000);
            var patient2 = await EnsurePatientAsync(userManager, "demo.hasta2@mentora.local", demoPassword, "Elif", "Şahin", "+905000000004", 40.992300, 29.027500);

            await EnsureDoctorSpecialtyAsync(context, doctor1.Id, 3);
            await EnsureDoctorSpecialtyAsync(context, doctor1.Id, 4);
            await EnsureDoctorSpecialtyAsync(context, doctor2.Id, 1);
            await EnsureDoctorSpecialtyAsync(context, doctor2.Id, 6);

            await EnsureNotificationPreferenceAsync(context, doctor1.Id);
            await EnsureNotificationPreferenceAsync(context, doctor2.Id);
            await EnsureNotificationPreferenceAsync(context, patient1.Id);
            await EnsureNotificationPreferenceAsync(context, patient2.Id);

            await EnsureSampleAppointmentsAsync(context, doctor1, doctor2, patient1, patient2);
            await EnsureSampleAutomationRoutineAsync(context, doctor1.Id);
            await EnsureSampleClinicalNoteAsync(context, doctor1.Id, doctor2.Id, patient1.Id);

            await context.SaveChangesAsync();
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "Doctor", "Patient" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task EnsureAdminUserAsync(UserManager<User> userManager, string username, string password)
        {
            var existing = await userManager.FindByNameAsync(username);
            if (existing != null)
            {
                if (!await userManager.IsInRoleAsync(existing, "Admin"))
                {
                    await userManager.AddToRoleAsync(existing, "Admin");
                }
                if (!existing.EmailConfirmed)
                {
                    existing.EmailConfirmed = true;
                }
                existing.FirstName = "Sistem";
                existing.LastName = "Yöneticisi";
                existing.IsApproved = true;
                await userManager.UpdateAsync(existing);
                await EnsurePasswordAsync(userManager, existing, password);
                return;
            }

            var admin = new User
            {
                UserName = username,
                Email = "admin@mentora.local",
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                PhoneNumber = "+900000000000",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Other,
                UserType = UserType.Admin,
                IsApproved = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Admin kullanıcısı oluşturulamadı: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, "Admin");
        }

        private static async Task EnsurePasswordAsync(UserManager<User> userManager, User user, string password)
        {
            // Demo ortamında hızlı test için hesap şifreleri tek yerde eşitleniyor.
            if (await userManager.HasPasswordAsync(user))
            {
                var removeResult = await userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException("Demo şifresi sıfırlanamadı: " + string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            var addResult = await userManager.AddPasswordAsync(user, password);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException("Demo şifresi atanamadı: " + string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }

        private static async Task<Doctor> EnsureDoctorAsync(
            UserManager<User> userManager,
            string email,
            string password,
            string firstName,
            string lastName,
            string phoneNumber,
            double latitude,
            double longitude)
        {
            var existing = await userManager.FindByEmailAsync(email);
            var existingDoctor = existing as Doctor
                ?? await userManager.Users.OfType<Doctor>().FirstOrDefaultAsync(d => d.PhoneNumber == phoneNumber);
            if (existingDoctor != null)
            {
                existingDoctor.FirstName = firstName;
                existingDoctor.LastName = lastName;
                existingDoctor.UserName = email;
                existingDoctor.Email = email;
                existingDoctor.PhoneNumber = phoneNumber;
                existingDoctor.Location = new Point(longitude, latitude) { SRID = 4326 };
                existingDoctor.About = "Öğrenci demosu için hazırlanmış gerçekçi doktor profili. Kaygı, aile ilişkileri ve günlük stres yönetimi alanlarında çalışır.";
                existingDoctor.UserType = UserType.Doctor;
                existingDoctor.IsApproved = true;
                existingDoctor.EmailConfirmed = true;
                existingDoctor.ExperienceStartDate = DateTime.Today.AddYears(-8);
                existingDoctor.Title = DoctorTitle.ClinicalPsychologist;
                existingDoctor.University = firstName == "Deniz" ? "Boğaziçi Üniversitesi" : "İstanbul Üniversitesi";
                await userManager.UpdateAsync(existingDoctor);
                await EnsurePasswordAsync(userManager, existingDoctor, password);
                if (!await userManager.IsInRoleAsync(existingDoctor, "Doctor"))
                {
                    await userManager.AddToRoleAsync(existingDoctor, "Doctor");
                }
                return existingDoctor;
            }

            var doctor = new Doctor
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = email,
                Email = email,
                PhoneNumber = phoneNumber,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Other,
                Location = new Point(longitude, latitude) { SRID = 4326 },
                About = "Öğrenci demosu için hazırlanmış gerçekçi doktor profili. Kaygı, aile ilişkileri ve günlük stres yönetimi alanlarında çalışır.",
                UserType = UserType.Doctor,
                IsApproved = true,
                ExperienceStartDate = DateTime.Today.AddYears(-8),
                Title = DoctorTitle.ClinicalPsychologist,
                University = firstName == "Deniz" ? "Boğaziçi Üniversitesi" : "İstanbul Üniversitesi"
            };
            doctor.EmailConfirmed = true;

            var createResult = await userManager.CreateAsync(doctor, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Demo doktor kullanıcısı oluşturulamadı: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(doctor, "Doctor");
            return doctor;
        }

        private static async Task<Patient> EnsurePatientAsync(
            UserManager<User> userManager,
            string email,
            string password,
            string firstName,
            string lastName,
            string phoneNumber,
            double latitude,
            double longitude)
        {
            var existing = await userManager.FindByEmailAsync(email);
            var existingPatient = existing as Patient
                ?? await userManager.Users.OfType<Patient>().FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
            if (existingPatient != null)
            {
                existingPatient.FirstName = firstName;
                existingPatient.LastName = lastName;
                existingPatient.UserName = email;
                existingPatient.Email = email;
                existingPatient.PhoneNumber = phoneNumber;
                existingPatient.Location = new Point(longitude, latitude) { SRID = 4326 };
                existingPatient.About = "Demo hasta profili. Randevu, talep, not paylaşımı ve bildirim akışlarını test etmek için kullanılır.";
                existingPatient.UserType = UserType.Patient;
                existingPatient.IsApproved = true;
                existingPatient.EmailConfirmed = true;
                await userManager.UpdateAsync(existingPatient);
                await EnsurePasswordAsync(userManager, existingPatient, password);
                if (!await userManager.IsInRoleAsync(existingPatient, "Patient"))
                {
                    await userManager.AddToRoleAsync(existingPatient, "Patient");
                }
                return existingPatient;
            }

            var patient = new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = email,
                Email = email,
                PhoneNumber = phoneNumber,
                BirthDate = new DateTime(1996, 1, 1),
                Gender = Gender.Other,
                Location = new Point(longitude, latitude) { SRID = 4326 },
                About = "Demo hasta profili. Randevu, talep, not paylaşımı ve bildirim akışlarını test etmek için kullanılır.",
                UserType = UserType.Patient,
                IsApproved = true
            };
            patient.EmailConfirmed = true;

            var createResult = await userManager.CreateAsync(patient, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Demo hasta kullanıcısı oluşturulamadı: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(patient, "Patient");
            return patient;
        }

        private static async Task EnsureDoctorSpecialtyAsync(ApplicationDbContext context, string doctorId, int specialtyId)
        {
            var exists = await context.DoctorSpecialties.AnyAsync(ds => ds.DoctorId == doctorId && ds.SpecialtyId == specialtyId);
            if (exists)
            {
                return;
            }

            context.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorId = doctorId,
                SpecialtyId = specialtyId
            });
        }

        private static async Task EnsureNotificationPreferenceAsync(ApplicationDbContext context, string userId)
        {
            var exists = await context.UserNotificationPreferences.AnyAsync(p => p.UserId == userId);
            if (exists)
            {
                return;
            }

            context.UserNotificationPreferences.Add(new UserNotificationPreference
            {
                UserId = userId,
                EmailEnabled = true,
                AppointmentReminderEnabled = true,
                RequestStatusEmailsEnabled = true,
                ReminderMinutesBefore = 60,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        private static async Task EnsureSampleAppointmentsAsync(ApplicationDbContext context, Doctor doctor1, Doctor doctor2, Patient patient1, Patient patient2)
        {
            var now = Utils.TimeZoneHelper.GetTurkeyNow();
            var tomorrow = now.Date.AddDays(1);

            var hasSample = await context.Appointments.AnyAsync(a => a.Notes != null && a.Notes.Contains("Demo okul"));
            if (hasSample)
            {
                return;
            }

            var sharedStart = tomorrow.AddHours(10);
            var conflictStart = tomorrow.AddHours(10).AddMinutes(30);
            var onlineStart = tomorrow.AddHours(14);
            var inPersonStart = tomorrow.AddDays(1).AddHours(11);
            var reservedStart = tomorrow.AddHours(16);
            var completedStart = now.Date.AddDays(-2).AddHours(13);
            var offerStart = tomorrow.AddDays(2).AddHours(15);

            var sharedAppointment = new Appointment
            {
                DoctorId = doctor1.Id,
                StartTime = sharedStart,
                EndTime = sharedStart.AddMinutes(50),
                IsOnline = true,
                IsInPerson = true,
                MeetingLink = "https://meet.google.com/mentora-demo",
                MinPrice = 900,
                MaxPrice = 1200,
                Notes = "Demo okul: iki hastanın talep attığı karma randevu",
                LocationNote = "Nişantaşı kliniği, giriş kat resepsiyon",
                Status = AppointmentStatus.Available,
                Location = doctor1.Location,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            sharedAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 3 });

            var conflictAppointment = new Appointment
            {
                DoctorId = doctor1.Id,
                StartTime = conflictStart,
                EndTime = conflictStart.AddMinutes(50),
                IsOnline = false,
                IsInPerson = true,
                MinPrice = 850,
                MaxPrice = 1100,
                Notes = "Demo okul: onay sonrası çakışma nedeniyle kapanacak slot",
                LocationNote = "Nişantaşı kliniği, B blok 3. kat",
                Status = AppointmentStatus.Available,
                Location = doctor1.Location,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            conflictAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 4 });

            var onlineAppointment = new Appointment
            {
                DoctorId = doctor2.Id,
                StartTime = onlineStart,
                EndTime = onlineStart.AddMinutes(45),
                IsOnline = true,
                IsInPerson = false,
                MeetingLink = "https://zoom.us/j/123456789",
                MinPrice = 700,
                MaxPrice = 900,
                Notes = "Demo okul: sadece online görüşme",
                Status = AppointmentStatus.Available,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            onlineAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 6 });

            var inPersonAppointment = new Appointment
            {
                DoctorId = doctor2.Id,
                StartTime = inPersonStart,
                EndTime = inPersonStart.AddMinutes(50),
                IsOnline = false,
                IsInPerson = true,
                MinPrice = 800,
                MaxPrice = 1000,
                Notes = "Demo okul: yüz yüze randevu",
                LocationNote = "Kadıköy danışmanlık merkezi, 2. kat",
                Status = AppointmentStatus.Available,
                Location = doctor2.Location,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            inPersonAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 1 });

            var reservedAppointment = new Appointment
            {
                DoctorId = doctor1.Id,
                PatientId = patient1.Id,
                StartTime = reservedStart,
                EndTime = reservedStart.AddMinutes(50),
                IsOnline = true,
                IsInPerson = false,
                MeetingLink = "https://meet.google.com/mentora-onayli",
                MinPrice = 1000,
                MaxPrice = 1300,
                Notes = "Demo okul: hasta ekranında yaklaşan randevu olarak görünür",
                Status = AppointmentStatus.Reserved,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            reservedAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 4 });

            var completedAppointment = new Appointment
            {
                DoctorId = doctor1.Id,
                PatientId = patient1.Id,
                StartTime = completedStart,
                EndTime = completedStart.AddMinutes(50),
                IsOnline = false,
                IsInPerson = true,
                MinPrice = 950,
                MaxPrice = 1200,
                Notes = "Demo okul: puan verilebilecek geçmiş randevu",
                LocationNote = "Nişantaşı kliniği",
                Status = AppointmentStatus.Completed,
                Location = doctor1.Location,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
            };
            completedAppointment.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 3 });

            var privateOffer = new Appointment
            {
                DoctorId = doctor2.Id,
                TargetPatientId = patient2.Id,
                StartTime = offerStart,
                EndTime = offerStart.AddMinutes(45),
                IsPrivateOffer = true,
                OfferStatus = AppointmentOfferStatus.Pending,
                OfferNoteFromDoctor = "Demo okul: bu özel teklif sadece Elif Şahin tarafından görülür.",
                OfferExpiresAtUtc = offerStart.ToUniversalTime().AddHours(-2),
                IsOnline = true,
                IsInPerson = true,
                MeetingLink = "https://meet.google.com/mentora-ozel",
                MinPrice = 750,
                MaxPrice = 950,
                Notes = "Demo okul: özel randevu teklifi",
                LocationNote = "Kadıköy danışmanlık merkezi",
                Status = AppointmentStatus.Available,
                Location = doctor2.Location,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            privateOffer.AppointmentSpecialties.Add(new Appointment_Specialty { SpecialtyId = 6 });

            context.Appointments.AddRange(sharedAppointment, conflictAppointment, onlineAppointment, inPersonAppointment, reservedAppointment, completedAppointment, privateOffer);
            await context.SaveChangesAsync();

            context.AppointmentRequests.AddRange(
                new AppointmentRequest
                {
                    AppointmentId = sharedAppointment.Id,
                    DoctorId = doctor1.Id,
                    PatientId = patient1.Id,
                    RequestMessage = "İlk görüşme için uygun görünüyor.",
                    ReasonForVisit = "Son haftalarda kaygı ve uyku düzeni problemi",
                    PreviousSupportInfo = "Daha önce kısa süreli destek aldı.",
                    UrgencyLevel = "Orta",
                    Expectations = "Kaygıyı yönetmek için yol haritası çıkarmak istiyor.",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new AppointmentRequest
                {
                    AppointmentId = sharedAppointment.Id,
                    DoctorId = doctor1.Id,
                    PatientId = patient2.Id,
                    RequestMessage = "Aynı saate başvuran ikinci hasta demo kaydı.",
                    ReasonForVisit = "İş stresi ve odaklanma sorunu",
                    PreviousSupportInfo = "Daha önce destek almadı.",
                    UrgencyLevel = "Düşük",
                    Expectations = "İlk değerlendirme yapmak istiyor.",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20)
                },
                new AppointmentRequest
                {
                    AppointmentId = conflictAppointment.Id,
                    DoctorId = doctor1.Id,
                    PatientId = patient2.Id,
                    RequestMessage = "Bu talep, çakışan slot kapanırsa otomatik reddedilecek.",
                    ReasonForVisit = "Demo çakışma testi",
                    UrgencyLevel = "Düşük",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                });
        }

        private static async Task EnsureSampleAutomationRoutineAsync(ApplicationDbContext context, string doctorId)
        {
            var exists = await context.AppointmentAutomationRoutines.AnyAsync(r => r.DoctorId == doctorId);
            if (exists)
            {
                return;
            }

            var doctorLocation = await context.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => d.Location)
                .FirstOrDefaultAsync();

            var routine = new AppointmentAutomationRoutine
            {
                DoctorId = doctorId,
                Name = "Hafta ici sabah slotu",
                StartTime = new TimeOnly(9, 0),
                DurationInMinutes = 50,
                GenerateDaysAhead = 7,
                ActiveFrom = DateOnly.FromDateTime(DateTime.Today),
                DaysOfWeek = RoutineWeekDayMask.Monday | RoutineWeekDayMask.Tuesday | RoutineWeekDayMask.Wednesday | RoutineWeekDayMask.Thursday | RoutineWeekDayMask.Friday,
                IsOnline = true,
                IsInPerson = true,
                InPersonLocationMode = "profile",
                Location = doctorLocation,
                LocationNote = "Klinik girisi B blok, 3. kat",
                MinPrice = 900,
                MaxPrice = 1200,
                Notes = "Otomatik rutin demo kaydi",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            routine.RoutineSpecialties.Add(new AppointmentAutomationRoutineSpecialty { SpecialtyId = 3 });

            context.AppointmentAutomationRoutines.Add(routine);
        }

        private static async Task EnsureSampleClinicalNoteAsync(ApplicationDbContext context, string doctorId, string secondDoctorId, string patientId)
        {
            var exists = await context.ClinicalNotes.AnyAsync(n => n.AuthorDoctorId == doctorId && n.PatientId == patientId && n.Content.Contains("Demo okul"));
            if (exists)
            {
                return;
            }

            var publicNote = new ClinicalNote
            {
                AuthorDoctorId = doctorId,
                PatientId = patientId,
                Content = "Demo okul: Hasta son görüşmede uyku düzensizliği ve kaygı belirtileri bildirdi. Takip görüşmesinde ölçeklendirme yapılacak.",
                Visibility = ClinicalNoteVisibility.Public,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var lockedNote = new ClinicalNote
            {
                AuthorDoctorId = doctorId,
                PatientId = patientId,
                Content = "Demo okul: Bu not hasta tarafından kilitli görülecek. İçerik sadece ilgili doktor tarafında okunmalıdır.",
                Visibility = ClinicalNoteVisibility.Private,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Lock = new ClinicalNoteLock
                {
                    LockedByDoctorId = doctorId,
                    IsLockedForPatient = true,
                    LockedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            };

            var sharedNote = new ClinicalNote
            {
                AuthorDoctorId = doctorId,
                PatientId = patientId,
                Content = "Demo okul: Hasta bu notu ikinci doktorla paylaşmış kabul edilir.",
                Visibility = ClinicalNoteVisibility.Shared,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            sharedNote.Shares.Add(new ClinicalNoteShare
            {
                SharedByPatientId = patientId,
                SharedWithDoctorId = secondDoctorId,
                SharedAtUtc = DateTime.UtcNow
            });

            sharedNote.Comments.Add(new ClinicalNoteComment
            {
                DoctorId = doctorId,
                Content = "Demo okul: İlk doktor yorumu.",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            sharedNote.Comments.Add(new ClinicalNoteComment
            {
                PatientId = patientId,
                Content = "Demo okul: Hasta yorumu.",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            context.ClinicalNotes.AddRange(publicNote, lockedNote, sharedNote);
        }
    }
}
