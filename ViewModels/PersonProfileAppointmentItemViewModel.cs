namespace PsikologProje_Void.ViewModels
{
    public class PersonProfileAppointmentItemViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string PriceLabel { get; set; } = string.Empty;
        public string? LocationNote { get; set; }
    }
}
