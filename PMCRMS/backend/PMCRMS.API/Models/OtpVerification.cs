using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public class OtpVerification : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Identifier { get; set; } = string.Empty; // Email or Phone
        
        [Required]
        [MaxLength(10)]
        public string OtpCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Purpose { get; set; } = string.Empty; // LOGIN, REGISTRATION, CERTIFICATE_DOWNLOAD
        
        public DateTime ExpiryTime { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        
        public int AttemptCount { get; set; } = 0;
        
        public DateTime? VerifiedAt { get; set; }
        
        [MaxLength(50)]
        public string? SessionToken { get; set; }
    }
}
