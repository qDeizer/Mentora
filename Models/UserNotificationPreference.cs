using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public enum EmailDigestMode
    {
        Instant = 0,
        Hourly = 1,
        Daily = 2
    }

    public class UserNotificationPreference
    {
        [Key]
        public string UserId { get; set; } = default!;

        [ForeignKey("UserId")]
        public User User { get; set; } = default!;

        public bool EmailEnabled { get; set; } = true;
        public bool AppointmentReminderEnabled { get; set; } = true;
        public bool RequestStatusEmailsEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;
        public bool IncomingRequestInAppEnabled { get; set; } = true;
        public bool PrivateOfferInAppEnabled { get; set; } = true;
        public bool ClinicalNoteShareInAppEnabled { get; set; } = true;
        public bool ClinicalNoteCommentInAppEnabled { get; set; } = true;
        public ClinicalNoteVisibility DefaultClinicalNoteVisibility { get; set; } = ClinicalNoteVisibility.Private;

        public EmailDigestMode EmailDigestMode { get; set; } = EmailDigestMode.Instant;

        public bool TwoFactorViaEmailEnabled { get; set; } = false;

        [Range(5, 1440)]
        public int ReminderMinutesBefore { get; set; } = 60;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
