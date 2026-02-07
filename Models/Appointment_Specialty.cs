namespace PsikologProje_Void.Models
{
    public class Appointment_Specialty
    {
        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; }
    }
}