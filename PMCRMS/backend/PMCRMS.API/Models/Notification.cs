using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public class Notification : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty; // Approval, Rejection, Assignment, StatusChange, Comment

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public int? ApplicationId { get; set; }

        [MaxLength(50)]
        public string? ApplicationNumber { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [MaxLength(100)]
        public string? ActionUrl { get; set; } // URL to navigate to when notification is clicked

        [MaxLength(100)]
        public string? ActorName { get; set; } // Name of the person who triggered the notification

        [MaxLength(50)]
        public string? ActorRole { get; set; } // Role of the person who triggered the notification

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ApplicationId")]
        public virtual Application? Application { get; set; }
    }

    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public static class NotificationType
    {
        public const string Submission = "Submission";
        public const string Assignment = "Assignment";
        public const string Approval = "Approval";
        public const string Rejection = "Rejection";
        public const string StatusChange = "StatusChange";
        public const string Comment = "Comment";
        public const string DocumentUpdate = "DocumentUpdate";
        public const string PaymentReceived = "PaymentReceived";
    }
}
