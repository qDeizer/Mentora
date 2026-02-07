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
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });
            builder.Entity<Doctor>(entity =>
            {
                entity.Property(e => e.University).IsRequired().HasMaxLength(100);
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
                entity.HasOne(ds => ds.Doctor).WithMany(d => d.DoctorSpecialties).HasForeignKey(ds => ds.DoctorId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ds => ds.Specialty).WithMany(s => s.DoctorSpecialties).HasForeignKey(ds => ds.SpecialtyId).OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<DoctorCertificate>(entity =>
            {
                entity.Property(e => e.CertificateImagePath).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasOne(dc => dc.Doctor).WithMany(d => d.Certificates).HasForeignKey(dc => dc.DoctorId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.DoctorId);
            });

            builder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Doctor).WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Cascade);
            });

            // HATA DÜZELTMESİ: AppointmentRequest ilişkileri için kurallar burada açıkça belirtildi.
            builder.Entity<AppointmentRequest>(entity =>
            {
                entity.HasKey(ar => ar.Id);

                // Bu ilişki döngüye neden olduğu için Cascade yerine NoAction olarak ayarlandı.
                entity.HasOne(ar => ar.Doctor)
                      .WithMany()
                      .HasForeignKey(ar => ar.DoctorId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Bu ilişki döngüye neden olduğu için Cascade yerine NoAction olarak ayarlandı.
                entity.HasOne(ar => ar.Patient)
                      .WithMany()
                      .HasForeignKey(ar => ar.PatientId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Ana ilişki Cascade olarak kalabilir.
                entity.HasOne(ar => ar.Appointment)
                    .WithMany()
                    .HasForeignKey(ar => ar.AppointmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Appointment_Specialty>(entity =>
            {
                entity.HasKey(aps => new { aps.AppointmentId, aps.SpecialtyId });
                entity.HasOne(aps => aps.Appointment).WithMany(a => a.AppointmentSpecialties).HasForeignKey(aps => aps.AppointmentId);
                entity.HasOne(aps => aps.Specialty).WithMany().HasForeignKey(aps => aps.SpecialtyId);
            });

            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
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