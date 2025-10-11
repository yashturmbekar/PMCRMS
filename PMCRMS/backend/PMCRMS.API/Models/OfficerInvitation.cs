using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public enum InvitationStatus
    {
        Pending = 1,
        Accepted = 2,
        Expired = 3,
        Revoked = 4
    }

    public class OfficerInvitation : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        [Required]
        public OfficerRole Role { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        [Required]
        [MaxLength(500)]
        public string TemporaryPassword { get; set; } = string.Empty;

        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcceptedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public int InvitedByAdminId { get; set; }

        // Navigation property - invited by system admin
        public virtual SystemAdmin? InvitedByAdmin { get; set; }

        // Linked officer ID once they accept the invitation
        public int? OfficerId { get; set; }

        public virtual Officer? Officer { get; set; }
    }
}
