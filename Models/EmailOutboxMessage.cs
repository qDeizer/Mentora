using System.ComponentModel.DataAnnotations;

namespace PsikologProje_Void.Models
{
    public enum EmailOutboxStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3
    }

    public class EmailOutboxMessage
    {
        public long Id { get; set; }

        [Required]
        [StringLength(320)]
        public string To { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(16000)]
        public string HtmlBody { get; set; } = string.Empty;

        [Required]
        public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;

        [Range(0, 20)]
        public int RetryCount { get; set; }

        [Range(1, 20)]
        public int MaxRetryCount { get; set; } = 5;

        public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessingStartedAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }

        [StringLength(2000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
