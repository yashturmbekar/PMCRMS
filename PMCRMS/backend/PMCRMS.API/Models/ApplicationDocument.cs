using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum DocumentType
    {
        SitePlan = 1,
        FloorPlan = 2,
        ElevationPlan = 3,
        StructuralPlan = 4,
        TitleDeed = 5,
        NOC = 6,
        IdentityProof = 7,
        AddressProof = 8,
        Other = 9
    }

    public class ApplicationDocument : BaseEntity
    {
        [Required]
        public int ApplicationId { get; set; }
        
        public DocumentType Type { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ContentType { get; set; }
        
        public long FileSize { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public bool IsRequired { get; set; } = true;
        
        public bool IsVerified { get; set; } = false;
        
        public int? VerifiedBy { get; set; }
        
        public DateTime? VerifiedDate { get; set; }
        
        [MaxLength(500)]
        public string? VerificationRemarks { get; set; }
        
        // Foreign Keys
        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; } = null!;
        
        [ForeignKey("VerifiedBy")]
        public virtual User? VerifiedByUser { get; set; }
    }
}