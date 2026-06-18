using PsikologProje_Void.Models;
using System;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentFilterModel
    {
        public string? DoctorId { get; set; }
        public List<string> SelectedDoctorIds { get; set; } = new();
        public int? SpecialtyId { get; set; }
        public List<int> SelectedSpecialtyIds { get; set; } = new();
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

        public List<int> SelectedDays { get; set; } = new();
    }
}
