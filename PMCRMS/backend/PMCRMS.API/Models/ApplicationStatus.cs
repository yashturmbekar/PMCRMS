using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public class ApplicationStatus : BaseEntity
    {
        [Required]
        public int ApplicationId { get; set; }
        
        public ApplicationCurrentStatus Status { get; set; }
        
        [Required]
        public int UpdatedByUserId { get; set; }
        
        [MaxLength(1000)]
        public string? Remarks { get; set; }
        
        [MaxLength(500)]
        public string? RejectionReason { get; set; }
        
        public DateTime StatusDate { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Foreign Keys
        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; } = null!;
        
        [ForeignKey("UpdatedByUserId")]
        public virtual User UpdatedByUser { get; set; } = null!;
    }
}