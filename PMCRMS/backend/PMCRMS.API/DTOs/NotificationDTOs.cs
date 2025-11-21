using System.ComponentModel.DataAnnotations;
using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? ApplicationId { get; set; }
        public string? ApplicationNumber { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActorName { get; set; }
        public string? ActorRole { get; set; }
        public NotificationPriority Priority { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateNotificationRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public int? ApplicationId { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActorName { get; set; }
        public string? ActorRole { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    public class MarkNotificationReadRequest
    {
        [Required]
        public List<int> NotificationIds { get; set; } = new List<int>();
    }

    public class EmailNotificationRequest
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public string? ApplicationNumber { get; set; }
        public string? UserName { get; set; }
    }
}
