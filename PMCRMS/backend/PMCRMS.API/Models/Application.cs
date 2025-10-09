using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum ApplicationType
    {
        BuildingPermit = 1,
        OccupancyCertificate = 2,
        CompletionCertificate = 3,
        DemolitionPermit = 4
    }

    public enum ApplicationCurrentStatus
    {
        Draft = 1,
        Submitted = 2,
        UnderReviewByJE = 3,
        ApprovedByJE = 4,
        RejectedByJE = 5,
        UnderReviewByAE = 6,
        ApprovedByAE = 7,
        RejectedByAE = 8,
        UnderReviewByEE1 = 9,
        ApprovedByEE1 = 10,
        RejectedByEE1 = 11,
        UnderReviewByCE1 = 12,
        ApprovedByCE1 = 13,
        RejectedByCE1 = 14,
        PaymentPending = 15,
        PaymentCompleted = 16,
        UnderProcessingByClerk = 17,
        ProcessedByClerk = 18,
        UnderDigitalSignatureByEE2 = 19,
        DigitalSignatureCompletedByEE2 = 20,
        UnderFinalApprovalByCE2 = 21,
        CertificateIssued = 22,
        Completed = 23
    }

    public class Application : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string ApplicationNumber { get; set; } = string.Empty;
        
        public ApplicationType Type { get; set; }
        
        public ApplicationCurrentStatus CurrentStatus { get; set; } = ApplicationCurrentStatus.Draft;
        
        [Required]
        public int ApplicantId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string ProjectTitle { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string ProjectDescription { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string SiteAddress { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PlotArea { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal BuiltUpArea { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedCost { get; set; }
        
        public DateTime? ScheduledAppointmentDate { get; set; }
        
        [MaxLength(1000)]
        public string? AppointmentRemarks { get; set; }
        
        [Column(TypeName = "decimal(8,2)")]
        public decimal? FeeAmount { get; set; }
        
        public DateTime? PaymentDueDate { get; set; }
        
        [MaxLength(50)]
        public string? CertificateNumber { get; set; }
        
        public DateTime? CertificateIssuedDate { get; set; }
        
        [MaxLength(1000)]
        public string? Remarks { get; set; }
        
        // Foreign Keys
        [ForeignKey("ApplicantId")]
        public virtual User Applicant { get; set; } = null!;
        
        // Navigation properties
        public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
        public virtual ICollection<ApplicationStatus> StatusHistory { get; set; } = new List<ApplicationStatus>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<ApplicationComment> Comments { get; set; } = new List<ApplicationComment>();
    }
}