using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public enum UserRole
    {
        Applicant = 1,
        User = 2,
        Clerk = 3,
        JuniorArchitect = 4,
        AssistantArchitect = 5,
        JuniorLicenceEngineer = 6,
        AssistantLicenceEngineer = 7,
        JuniorStructuralEngineer = 8,
        AssistantStructuralEngineer = 9,
        JuniorSupervisor1 = 10,
        AssistantSupervisor1 = 11,
        JuniorSupervisor2 = 12,
        AssistantSupervisor2 = 13,
        ExecutiveEngineer = 14,
        CityEngineer = 15,
        Admin = 16
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
        
        public UserRole Role { get; set; } = UserRole.Applicant;
        
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