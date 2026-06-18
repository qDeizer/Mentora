namespace PsikologProje_Void.ViewModels
{
    public class PersonCardViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserTypeLabel { get; set; } = string.Empty;
        public string? ProfilePhotoPath { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? LastInteractionAt { get; set; }
        public int TotalAppointmentCount { get; set; }
        public int TotalRequestCount { get; set; }
        public int ActiveRequestCount { get; set; }
        public int SharedNoteCount { get; set; }
    }
}
