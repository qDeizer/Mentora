using PsikologProje_Void.Models;
using System;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentFilterModel
    {
        public string? DoctorId { get; set; }
        public int? SpecialtyId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsOnline { get; set; }
        public bool IsInPerson { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }

        // YENİ EKLENDİ: Hangi konumun kullanılacağını belirten bayrak.
        public bool UseProfileLocation { get; set; }
    }
}