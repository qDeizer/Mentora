namespace PsikologProje_Void.Models
{
    public class AppointmentAutomationRoutineSpecialty
    {
        public int RoutineId { get; set; }
        public AppointmentAutomationRoutine Routine { get; set; } = default!;

        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; } = default!;
    }
}
