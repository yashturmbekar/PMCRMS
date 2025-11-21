using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    public class OfficerPasswordReset : BaseEntity
    {
        [Required]
        public int OfficerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ResetToken { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        // Navigation property
        public virtual Officer? Officer { get; set; }
    }
}
