// FILE: \ViewModels\DoctorFilterModel.cs

using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class DoctorFilterModel
    {
        public string? Name { get; set; } // Ad veya soyada göre arama
        public int? SpecialtyId { get; set; } // Uzmanlık alanına göre
        public DoctorTitle? Title { get; set; } // Unvana göre
        public string? University { get; set; } // Üniversiteye göre
        public double? Latitude { get; set; } // Konum-bazlı arama için
        public double? Longitude { get; set; } // Konum-bazlı arama için
        public double? DistanceKm { get; set; } // Maksimum uzaklık (km)
    }
}