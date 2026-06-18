using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsikologProje_Void.Models
{
    public enum NotificationType
    {
        Generic = 0,
        IncomingRequest = 1,
        RequestApproved = 2,
        RequestRejected = 3,
        AppointmentReminder = 4,
        PrivateOffer = 5,
        PrivateOfferResponse = 6,
        ClinicalNoteShared = 7,
        ClinicalNoteComment = 8
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = default!;

        [Required]
        [StringLength(140)]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(1200)]
        public string Message { get; set; } = default!;

        [StringLength(300)]
        public string? DeepLink { get; set; }

        [Required]
        public NotificationType Type { get; set; } = NotificationType.Generic;

        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAtUtc { get; set; }
    }
}
