using PsikologProje_Void.Models;

namespace PsikologProje_Void.ViewModels
{
    public class AppointmentRequestViewModel
    {
        public int RequestId { get; set; }
        public int AppointmentId { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientProfilePhoto { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string? DoctorProfilePhoto { get; set; }
        public DateTime AppointmentStartTime { get; set; }
        public DateTime AppointmentEndTime { get; set; }
        public string AppointmentTypeText { get; set; } = string.Empty;
        public string PriceRange { get; set; } = string.Empty;
        public decimal? AppointmentMinPrice { get; set; }
        public decimal? AppointmentMaxPrice { get; set; }
        public DateTime RequestDate { get; set; }
        public string? RequestMessage { get; set; }
        public string? ReasonForVisit { get; set; }
        public string? PreviousSupportInfo { get; set; }
        public string? UrgencyLevel { get; set; }
        public string? Expectations { get; set; }
        public string? ResponseMessage { get; set; }
        public RequestStatus Status { get; set; }
        public bool IsPrivateOffer { get; set; }
        public double? AppointmentLatitude { get; set; }
        public double? AppointmentLongitude { get; set; }
        public string? AppointmentLocationNote { get; set; }
        public string? MeetingLink { get; set; }
        public List<RequestApprovalApplicantViewModel> OtherPendingApplicants { get; set; } = new();
        public List<RequestApprovalConflictSlotViewModel> ConflictingSlots { get; set; } = new();
        public int ConflictPendingRequestCount { get; set; }
        public int EstimatedMailCount { get; set; }
    }

    public class RequestApprovalApplicantViewModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientProfilePhoto { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientPhone { get; set; }
    }

    public class RequestApprovalConflictSlotViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PendingRequestCount { get; set; }
        public string Label => $"{StartTime:dd.MM.yyyy HH:mm} - {EndTime:HH:mm}";
    }
}
