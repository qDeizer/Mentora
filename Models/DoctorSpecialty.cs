namespace PsikologProje_Void.Models
{
    // Models/DoctorSpecialty.cs
    public class DoctorSpecialty
    {
        public string? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }
        public int SpecialtyId { get; set; }
        public Specialty? Specialty { get; set; }
    }
}
