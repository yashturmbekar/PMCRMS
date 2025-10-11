using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum FormType
    {
        BuildingPermit = 1,
        StructuralEngineerLicense = 2,
        ArchitectLicense = 3,
        PlumbingPermit = 4,
        ElectricalPermit = 5,
        OccupancyCertificate = 6,
        DemolitionPermit = 7
    }

    public class FormConfiguration : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FormName { get; set; } = string.Empty;

        [Required]
        public FormType FormType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProcessingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool AllowOnlineSubmission { get; set; } = true;

        public int ProcessingDays { get; set; } = 30;

        // Custom fields stored as JSON
        [Column(TypeName = "jsonb")]
        public string? CustomFields { get; set; }

        // Required documents stored as JSON array
        [Column(TypeName = "jsonb")]
        public string? RequiredDocuments { get; set; }

        public int? MaxFileSizeMB { get; set; } = 5;

        public int? MaxFilesAllowed { get; set; } = 10;

        // Navigation properties
        public virtual ICollection<FormFeeHistory> FeeHistory { get; set; } = new List<FormFeeHistory>();
    }

    public class FormFeeHistory : BaseEntity
    {
        public int FormConfigurationId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldBaseFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewBaseFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldProcessingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewProcessingFee { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public int ChangedByAdminId { get; set; }
        
        // TEMPORARY: Kept for backward compatibility during migration
        public int? ChangedByUserId { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        // Navigation properties
        public virtual FormConfiguration? FormConfiguration { get; set; }
        public virtual SystemAdmin? ChangedByAdmin { get; set; }
        
        // TEMPORARY: Kept for backward compatibility during migration
        public virtual User? ChangedByUser { get; set; }
    }
}
