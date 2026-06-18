using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace PsikologProje_Void.Models
{
    [Flags]
    public enum RoutineWeekDayMask
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64
    }

    public class AppointmentAutomationRoutine
    {
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } = default!;

        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; } = default!;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = default!;

        [Required]
        public RoutineWeekDayMask DaysOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }

        [Range(15, 720)]
        public int DurationInMinutes { get; set; } = 50;

        [Range(1, 90)]
        public int GenerateDaysAhead { get; set; } = 7;

        public bool IsOnline { get; set; } = true;
        public bool IsInPerson { get; set; }

        [Required]
        [StringLength(20)]
        public string InPersonLocationMode { get; set; } = "profile";

        public Point? Location { get; set; }

        [StringLength(300)]
        public string? LocationNote { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxPrice { get; set; }

        public DateOnly ActiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        public DateOnly? ActiveUntil { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsEnabled { get; set; } = true;
        public DateTime? PausedUntilUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<AppointmentAutomationRoutineSpecialty> RoutineSpecialties { get; set; } = new List<AppointmentAutomationRoutineSpecialty>();
    }
}
