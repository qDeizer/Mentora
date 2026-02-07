// FILE: \ViewModels\DoctorViewModel.cs

namespace PsikologProje_Void.ViewModels
{
    public class DoctorViewModel
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? ProfilePhotoPath { get; set; }
        public string? University { get; set; }
        public int YearsOfExperience { get; set; }
        public string? About { get; set; }
        public List<string> Specialties { get; set; } = new List<string>();

        // --- Sadece Admin tarafından görülebilecek alanlar ---
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsApproved { get; set; }
    }
}