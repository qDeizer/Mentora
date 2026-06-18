namespace PsikologProje_Void.ViewModels
{
    public class AutomationRoutineListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DaysText { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public int GenerateDaysAhead { get; set; }
        public string AppointmentTypes { get; set; } = string.Empty;
        public string PriceRange { get; set; } = "-";
        public bool IsEnabled { get; set; }
        public DateTime? PausedUntilUtc { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveUntil { get; set; }
        public string? Notes { get; set; }
        public string? LocationNote { get; set; }
        public string InPersonLocationMode { get; set; } = "profile";
        public List<string> Specialties { get; set; } = new();
    }
}
