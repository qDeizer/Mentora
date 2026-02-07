using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorId { get; set; }
        public string? DoctorProfilePhotoPath { get; set; }
        public double DoctorAverageRating { get; set; }
        public string? PatientName { get; set; }
        public string? PatientProfileImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationInMinutes { get; set; }
        public string PriceRange { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> AppointmentTypes { get; set; } = new List<string>();
        public string? Notes { get; set; }
        public List<string> Specialties { get; set; } = new List<string>();
        public Point? Location { get; set; }
        public double? DistanceKm { get; set; }
        public bool IsRequestedByCurrentUser { get; set; }
        public int? Rating { get; set; }
        public string? Review { get; set; }
    }
}