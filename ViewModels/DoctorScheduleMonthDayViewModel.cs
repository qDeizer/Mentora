namespace PsikologProje_Void.ViewModels
{
    public class DoctorScheduleMonthDayViewModel
    {
        public DateTime Date { get; set; }
        public DateTime WeekStartDate { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool IsSelectedWeek { get; set; }
        public int TotalSessionCount { get; set; }
        public int AvailableSessionCount { get; set; }
        public int AssignedSessionCount { get; set; }
    }
}
