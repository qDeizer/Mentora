namespace PsikologProje_Void.ViewModels
{
    public class DoctorWeeklyScheduleViewModel
    {
        public string ViewMode { get; set; } = "week";
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public DateTime PreviousWeekStartDate { get; set; }
        public DateTime NextWeekStartDate { get; set; }
        public string WeekRangeLabel { get; set; } = string.Empty;
        public DateTime MonthDate { get; set; }
        public DateTime PreviousMonthDate { get; set; }
        public DateTime NextMonthDate { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public int ActiveDayIndex { get; set; }
        public int GridStartMinute { get; set; }
        public int GridEndMinute { get; set; }
        public bool HasAnyAppointment { get; set; }
        public List<DoctorWeeklyScheduleDayViewModel> Days { get; set; } = new();
        public List<DoctorWeeklyScheduleSlotViewModel> Slots { get; set; } = new();
        public List<DoctorScheduleMonthDayViewModel> MonthDays { get; set; } = new();
    }
}
