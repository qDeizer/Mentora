using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PsikologProje_Void.Models
{
    public enum AppointmentStatus { Available, Reserved, Completed, NotCompleted }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } = default!;
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = default!;

        public string? PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public bool IsOnline { get; set; }
        public bool IsInPerson { get; set; }

        public string? Notes { get; set; }

        // DÜZELTME: 'Location' özelliği nullable (?) yapıldı.
        // Bu, veritabanı migration hatasını çözecektir.
        public Point? Location { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;

        public int? Rating { get; set; }
        public string? Review { get; set; }

        public ICollection<Appointment_Specialty> AppointmentSpecialties { get; set; } = new List<Appointment_Specialty>();
    }
}