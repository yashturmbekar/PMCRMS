using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public enum OfficerRole
    {
        JuniorArchitect = 3,
        AssistantArchitect = 4,
        JuniorLicenceEngineer = 5,
        AssistantLicenceEngineer = 6,
        JuniorStructuralEngineer = 7,
        AssistantStructuralEngineer = 8,
        JuniorSupervisor1 = 9,
        AssistantSupervisor1 = 10,
        JuniorSupervisor2 = 11,
        AssistantSupervisor2 = 12,
        ExecutiveEngineer = 13,
        CityEngineer = 14,
        Clerk = 15
    }

    /// <summary>
    /// Represents an Officer who processes applications
    /// </summary>
    public class Officer : BaseEntity
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
        
        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(100)]
        public string? Department { get; set; }
        
        // Login tracking
        public DateTime? LastLoginAt { get; set; }
        
        public int LoginAttempts { get; set; } = 0;
        
        public DateTime? LockedUntil { get; set; }
        
        // Password management
        public bool MustChangePassword { get; set; } = true;
        
        public DateTime? PasswordChangedAt { get; set; }
        
        // Link to invitation
        public int? InvitationId { get; set; }
        public virtual OfficerInvitation? Invitation { get; set; }
        
        // Navigation properties
        public virtual ICollection<ApplicationStatus> StatusUpdates { get; set; } = new List<ApplicationStatus>();
    }
}
