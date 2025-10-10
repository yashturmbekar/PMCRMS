using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public enum UserRole
    {
        Admin = 1,
        User = 2,
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

    public class User : BaseEntity
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
        
        public UserRole Role { get; set; } = UserRole.User;
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        // Password hash for officer login (only for non-applicant users)
        [MaxLength(500)]
        public string? PasswordHash { get; set; }
        
        // Employee ID for officers
        [MaxLength(50)]
        public string? EmployeeId { get; set; }
        
        // Last login tracking
        public DateTime? LastLoginAt { get; set; }
        
        public int LoginAttempts { get; set; } = 0;
        
        public DateTime? LockedUntil { get; set; }
        
        // Navigation properties
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
        public virtual ICollection<ApplicationStatus> StatusUpdates { get; set; } = new List<ApplicationStatus>();
    }
}
