namespace PsikologProje_Void.ViewModels
{
    public class PersonProfileRequestItemViewModel
    {
        public int RequestId { get; set; }
        public int AppointmentId { get; set; }
        public DateTime AppointmentStartTime { get; set; }
        public DateTime RequestedAt { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? RequestMessage { get; set; }
        public string? ResponseMessage { get; set; }
    }
}
