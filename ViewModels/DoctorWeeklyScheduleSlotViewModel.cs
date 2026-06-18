namespace PsikologProje_Void.ViewModels
{
    public class DoctorWeeklyScheduleSlotViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DayIndex { get; set; }
        public int StartMinuteOfDay { get; set; }
        public int EndMinuteOfDay { get; set; }
        public bool IsAssigned { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientProfilePhotoPath { get; set; }
        public string TimeLabel { get; set; } = string.Empty;
        public string PriceLabel { get; set; } = string.Empty;
        public string AppointmentTypeLabel { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? LocationNote { get; set; }
        public string? RelativeDayLabel { get; set; }
    }
}
