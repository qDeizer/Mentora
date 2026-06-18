using PsikologProje_Void.Models;
using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.ViewModels
{
    public class NotificationSettingsViewModel
    {
        public bool EmailEnabled { get; set; }
        public bool AppointmentReminderEnabled { get; set; }
        public bool RequestStatusEmailsEnabled { get; set; }
        public bool InAppEnabled { get; set; } = true;
        public bool IncomingRequestInAppEnabled { get; set; } = true;
        public bool PrivateOfferInAppEnabled { get; set; } = true;
        public bool ClinicalNoteShareInAppEnabled { get; set; } = true;
        public bool ClinicalNoteCommentInAppEnabled { get; set; } = true;
        public ClinicalNoteVisibility DefaultClinicalNoteVisibility { get; set; } = ClinicalNoteVisibility.Private;

        public EmailDigestMode EmailDigestMode { get; set; } = EmailDigestMode.Instant;

        public bool TwoFactorViaEmailEnabled { get; set; } = false;

        [Range(10, 720, ErrorMessage = "Hatirlatma suresi 10 ile 720 dakika arasinda olmalidir.")]
        public int ReminderMinutesBefore { get; set; } = 60;
    }
}
