namespace PsikologProje_Void.ViewModels
{
    public class DoctorWeeklyScheduleDayViewModel
    {
        public int DayIndex { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int TotalSessionCount { get; set; }
        public int AvailableSessionCount { get; set; }
        public int AssignedSessionCount { get; set; }
        public double TotalHours { get; set; }
    }
}
