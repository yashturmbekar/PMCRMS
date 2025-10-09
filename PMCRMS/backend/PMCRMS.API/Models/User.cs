using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public enum UserRole
    {
        Applicant = 1,
        JuniorEngineer = 2,
        AssistantEngineer = 3,
        ExecutiveEngineer = 4,
        CityEngineer = 5,
        Clerk = 6,
        Admin = 7
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
        
        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public UserRole Role { get; set; } = UserRole.Applicant;
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        // Navigation properties
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
        public virtual ICollection<ApplicationStatus> StatusUpdates { get; set; } = new List<ApplicationStatus>();
    }
}