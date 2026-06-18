namespace PsikologProje_Void.ViewModels
{
    public class PersonProfileViewModel
    {
        public bool ViewerIsDoctor { get; set; }
        public string ViewerUserId { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetFullName { get; set; } = string.Empty;
        public string TargetRoleLabel { get; set; } = string.Empty;
        public string? TargetProfilePhotoPath { get; set; }
        public string? TargetEmail { get; set; }
        public string? TargetPhoneNumber { get; set; }
        public DateTime? TargetBirthDate { get; set; }
        public string? TargetAbout { get; set; }
        public bool IsDisconnected { get; set; }
        public DateTime? LastInteractionAt { get; set; }
        public int TotalAppointmentCount { get; set; }
        public int PendingRequestCount { get; set; }
        public int ApprovedRequestCount { get; set; }
        public int RejectedRequestCount { get; set; }
        public int SharedNoteCount { get; set; }
        public List<PersonProfileAppointmentItemViewModel> RecentAppointments { get; set; } = new();
        public List<PersonProfileRequestItemViewModel> RecentRequests { get; set; } = new();
    }
}
