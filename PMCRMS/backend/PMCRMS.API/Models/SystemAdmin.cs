using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    /// <summary>
    /// Represents a System Administrator with full system access
    /// </summary>
    public class SystemAdmin : BaseEntity
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
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? EmployeeId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsSuperAdmin { get; set; } = false; // For root admin
        
        // Login tracking
        public DateTime? LastLoginAt { get; set; }
        
        public int LoginAttempts { get; set; } = 0;
        
        public DateTime? LockedUntil { get; set; }
        
        [MaxLength(255)]
        public string? Department { get; set; }
        
        [MaxLength(100)]
        public string? Designation { get; set; }
    }
}
