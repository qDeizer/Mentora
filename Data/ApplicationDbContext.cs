using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PsikologProje_Void.Models;

namespace PsikologProje_Void.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; }
        public DbSet<DoctorCertificate> DoctorCertificates { get; set; }
        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Appointment_Specialty> AppointmentSpecialties { get; set; }
        public DbSet<AppointmentAutomationRoutine> AppointmentAutomationRoutines { get; set; }
        public DbSet<AppointmentAutomationRoutineSpecialty> AppointmentAutomationRoutineSpecialties { get; set; }
        public DbSet<ClinicalNote> ClinicalNotes { get; set; }
        public DbSet<ClinicalNoteShare> ClinicalNoteShares { get; set; }
        public DbSet<ClinicalNoteLock> ClinicalNoteLocks { get; set; }
        public DbSet<ClinicalNoteComment> ClinicalNoteComments { get; set; }
        public DbSet<ClinicalNoteAccessRule> ClinicalNoteAccessRules { get; set; }
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
        public DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; }
        public DbSet<DoctorPatientConnectionState> DoctorPatientConnectionStates { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasDiscriminator<UserType>("UserType")
                .HasValue<User>(UserType.Admin)
                .HasValue<Doctor>(UserType.Doctor)
                .HasValue<Patient>(UserType.Patient);

            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired();
                entity.Property(e => e.About).HasMaxLength(1000);
                entity.Property(e => e.ThemePreference).HasMaxLength(20).HasDefaultValue("system");
                entity.Property(e => e.LayoutDensity).HasMaxLength(20).HasDefaultValue("comfortable");
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });

            builder.Entity<Doctor>(entity =>
            {
                entity.Property(e => e.University).HasMaxLength(100);
                entity.HasIndex(e => e.ExperienceStartDate);
            });

            builder.Entity<Specialty>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            builder.Entity<DoctorSpecialty>(entity =>
            {
                entity.HasKey(ds => new { ds.DoctorId, ds.SpecialtyId });
                entity.HasOne(ds => ds.Doctor)
                    .WithMany(d => d.DoctorSpecialties)
                    .HasForeignKey(ds => ds.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ds => ds.Specialty)
                    .WithMany(s => s.DoctorSpecialties)
                    .HasForeignKey(ds => ds.SpecialtyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DoctorCertificate>(entity =>
            {
                entity.Property(e => e.CertificateImagePath).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasOne(dc => dc.Doctor)
                    .WithMany(d => d.Certificates)
                    .HasForeignKey(dc => dc.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DoctorId);
            });

            builder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.Patient)
                    .WithMany()
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Doctor)
                    .WithMany()
                    .HasForeignKey(a => a.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.TargetPatient)
                    .WithMany()
                    .HasForeignKey(a => a.TargetPatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new { a.DoctorId, a.StartTime, a.EndTime }).IsUnique();
                entity.HasIndex(a => new { a.PatientId, a.StartTime, a.EndTime });
                entity.HasIndex(a => new { a.TargetPatientId, a.OfferStatus, a.StartTime });
                entity.Property(a => a.RowVersion).IsRowVersion();
                entity.Property(a => a.CancelledReason).HasMaxLength(200);
                entity.Property(a => a.LocationNote).HasMaxLength(300);
                entity.Property(a => a.MeetingLink).HasMaxLength(300);
                entity.Property(a => a.OfferStatus).HasConversion<int>().HasDefaultValue(AppointmentOfferStatus.None);
            });

            builder.Entity<AppointmentRequest>(entity =>
            {
                entity.HasKey(ar => ar.Id);

                entity.HasOne(ar => ar.Doctor)
                    .WithMany()
                    .HasForeignKey(ar => ar.DoctorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(ar => ar.Patient)
                    .WithMany()
                    .HasForeignKey(ar => ar.PatientId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(ar => ar.Appointment)
                    .WithMany()
                    .HasForeignKey(ar => ar.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ar => new { ar.AppointmentId, ar.PatientId });
                entity.HasIndex(ar => new { ar.PatientId, ar.Status });
                entity.HasIndex(ar => new { ar.DoctorId, ar.Status });
                entity.Property(ar => ar.ReasonForVisit).HasMaxLength(500);
                entity.Property(ar => ar.PreviousSupportInfo).HasMaxLength(300);
                entity.Property(ar => ar.UrgencyLevel).HasMaxLength(40);
                entity.Property(ar => ar.Expectations).HasMaxLength(500);
            });

            builder.Entity<Appointment_Specialty>(entity =>
            {
                entity.HasKey(aps => new { aps.AppointmentId, aps.SpecialtyId });

                entity.HasOne(aps => aps.Appointment)
                    .WithMany(a => a.AppointmentSpecialties)
                    .HasForeignKey(aps => aps.AppointmentId);

                entity.HasOne(aps => aps.Specialty)
                    .WithMany()
                    .HasForeignKey(aps => aps.SpecialtyId);
            });

            builder.Entity<AppointmentAutomationRoutine>(entity =>
            {
                entity.Property(r => r.Name).IsRequired().HasMaxLength(120);
                entity.Property(r => r.Notes).HasMaxLength(500);
                entity.Property(r => r.LocationNote).HasMaxLength(300);
                entity.Property(r => r.InPersonLocationMode).HasMaxLength(20).HasDefaultValue("profile");
                entity.Property(r => r.GenerateDaysAhead).HasDefaultValue(7);
                entity.Property(r => r.DaysOfWeek).HasConversion<int>();

                entity.HasOne(r => r.Doctor)
                    .WithMany()
                    .HasForeignKey(r => r.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.DoctorId, r.IsEnabled });
                entity.HasIndex(r => r.PausedUntilUtc);
            });

            builder.Entity<AppointmentAutomationRoutineSpecialty>(entity =>
            {
                entity.HasKey(rs => new { rs.RoutineId, rs.SpecialtyId });

                entity.HasOne(rs => rs.Routine)
                    .WithMany(r => r.RoutineSpecialties)
                    .HasForeignKey(rs => rs.RoutineId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Specialty)
                    .WithMany()
                    .HasForeignKey(rs => rs.SpecialtyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ClinicalNote>(entity =>
            {
                entity.Property(n => n.Content).IsRequired().HasMaxLength(50000);
                entity.Property(n => n.Visibility)
                    .HasConversion<int>()
                    .HasDefaultValue(ClinicalNoteVisibility.Private);

                entity.HasOne(n => n.AuthorDoctor)
                    .WithMany()
                    .HasForeignKey(n => n.AuthorDoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Patient)
                    .WithMany()
                    .HasForeignKey(n => n.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.Appointment)
                    .WithMany()
                    .HasForeignKey(n => n.AppointmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(n => new { n.PatientId, n.CreatedAtUtc });
                entity.HasIndex(n => new { n.AuthorDoctorId, n.CreatedAtUtc });
                entity.HasIndex(n => new { n.PatientId, n.Visibility, n.CreatedAtUtc });
            });

            builder.Entity<ClinicalNoteLock>(entity =>
            {
                entity.HasKey(x => x.ClinicalNoteId);
                entity.HasOne(x => x.ClinicalNote)
                    .WithOne(n => n.Lock)
                    .HasForeignKey<ClinicalNoteLock>(x => x.ClinicalNoteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.LockedByDoctor)
                    .WithMany()
                    .HasForeignKey(x => x.LockedByDoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ClinicalNoteComment>(entity =>
            {
                entity.Property(x => x.Content).IsRequired().HasMaxLength(4000);
                entity.HasOne(x => x.ClinicalNote)
                    .WithMany(n => n.Comments)
                    .HasForeignKey(x => x.ClinicalNoteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Doctor)
                    .WithMany()
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Patient)
                    .WithMany()
                    .HasForeignKey(x => x.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => new { x.ClinicalNoteId, x.CreatedAtUtc });
            });

            builder.Entity<ClinicalNoteAccessRule>(entity =>
            {
                entity.Property(x => x.RuleType).HasConversion<int>();
                entity.HasOne(x => x.ClinicalNote)
                    .WithMany(n => n.AccessRules)
                    .HasForeignKey(x => x.ClinicalNoteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Doctor)
                    .WithMany()
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.CreatedByPatient)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByPatientId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => new { x.ClinicalNoteId, x.DoctorId, x.RuleType });
                entity.HasIndex(x => new { x.DoctorId, x.RuleType, x.RevokedAtUtc });
            });

            builder.Entity<ClinicalNoteShare>(entity =>
            {
                entity.HasOne(s => s.ClinicalNote)
                    .WithMany(n => n.Shares)
                    .HasForeignKey(s => s.ClinicalNoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.SharedByPatient)
                    .WithMany()
                    .HasForeignKey(s => s.SharedByPatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.SharedWithDoctor)
                    .WithMany()
                    .HasForeignKey(s => s.SharedWithDoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.RevokedByPatient)
                    .WithMany()
                    .HasForeignKey(s => s.RevokedByPatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => new { s.ClinicalNoteId, s.SharedWithDoctorId }).IsUnique();
                entity.HasIndex(s => new { s.SharedWithDoctorId, s.RevokedAtUtc });
            });

            builder.Entity<UserNotificationPreference>(entity =>
            {
                entity.HasKey(p => p.UserId);
                entity.Property(p => p.DefaultClinicalNoteVisibility)
                    .HasConversion<int>()
                    .HasDefaultValue(ClinicalNoteVisibility.Private);

                entity.HasOne(p => p.User)
                    .WithOne()
                    .HasForeignKey<UserNotificationPreference>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Notification>(entity =>
            {
                entity.Property(x => x.Title).IsRequired().HasMaxLength(140);
                entity.Property(x => x.Message).IsRequired().HasMaxLength(1200);
                entity.Property(x => x.DeepLink).HasMaxLength(300);
                entity.Property(x => x.Type).HasConversion<int>();
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAtUtc });
            });

            builder.Entity<EmailOutboxMessage>(entity =>
            {
                entity.Property(x => x.To).IsRequired().HasMaxLength(320);
                entity.Property(x => x.Subject).IsRequired().HasMaxLength(200);
                entity.Property(x => x.HtmlBody).IsRequired().HasMaxLength(16000);
                entity.Property(x => x.Status).HasConversion<int>();
                entity.Property(x => x.LastError).HasMaxLength(2000);
                entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
            });

            builder.Entity<DoctorPatientConnectionState>(entity =>
            {
                entity.HasOne(x => x.Doctor)
                    .WithMany()
                    .HasForeignKey(x => x.DoctorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.Patient)
                    .WithMany()
                    .HasForeignKey(x => x.PatientId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.DisconnectedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.DisconnectedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => new { x.DoctorId, x.PatientId }).IsUnique();
                entity.HasIndex(x => new { x.DoctorId, x.DisconnectedAtUtc });
                entity.HasIndex(x => new { x.PatientId, x.DisconnectedAtUtc });
            });

            SeedData(builder);
        }

        private static void SeedData(ModelBuilder builder)
        {
            builder.Entity<Specialty>().HasData(
                new Specialty { Id = 1, Name = "Çocuk Psikolojisi" },
                new Specialty { Id = 2, Name = "Aile Terapisi" },
                new Specialty { Id = 3, Name = "Depresyon Tedavisi" },
                new Specialty { Id = 4, Name = "Anksiyete Bozuklukları" },
                new Specialty { Id = 5, Name = "Travma Terapisi" },
                new Specialty { Id = 6, Name = "Çift Terapisi" },
                new Specialty { Id = 7, Name = "Yeme Bozuklukları" },
                new Specialty { Id = 8, Name = "Bağımlılık Tedavisi" },
                new Specialty { Id = 9, Name = "Kişilik Bozuklukları" },
                new Specialty { Id = 10, Name = "Yaşlı Psikolojisi" },
                new Specialty { Id = 11, Name = "Ergen Psikolojisi" },
                new Specialty { Id = 12, Name = "Kariyer Danışmanlığı" },
                new Specialty { Id = 13, Name = "Stres Yönetimi" },
                new Specialty { Id = 14, Name = "Öfke Yönetimi" },
                new Specialty { Id = 15, Name = "Sosyal Fobi" }
            );
        }
    }
}
