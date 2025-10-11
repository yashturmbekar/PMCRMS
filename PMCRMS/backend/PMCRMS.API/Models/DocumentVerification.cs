using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum VerificationStatus
    {
        Pending = 0,
        InProgress = 1,
        Approved = 2,
        Rejected = 3,
        RequiresResubmission = 4
    }

    /// <summary>
    /// Represents verification of a submitted document by Junior Engineer
    /// </summary>
    public class DocumentVerification : BaseEntity
    {
        /// <summary>
        /// Reference to the SEDocument being verified
        /// </summary>
        [Required]
        public int DocumentId { get; set; }

        /// <summary>
        /// Reference to the PositionApplication
        /// </summary>
        [Required]
        public int ApplicationId { get; set; }

        /// <summary>
        /// Type of document being verified (e.g., "Architectural Drawing", "NOC", "Structural Certificate")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Current verification status
        /// </summary>
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        /// <summary>
        /// Officer who performed the verification (Junior Engineer)
        /// </summary>
        public int? VerifiedByOfficerId { get; set; }

        /// <summary>
        /// Date and time when verification was completed
        /// </summary>
        public DateTime? VerifiedDate { get; set; }

        /// <summary>
        /// Verification started timestamp
        /// </summary>
        public DateTime? VerificationStartedAt { get; set; }

        /// <summary>
        /// Comments from the verifying officer
        /// </summary>
        [MaxLength(2000)]
        public string? VerificationComments { get; set; }

        /// <summary>
        /// Reason for rejection if status is Rejected
        /// </summary>
        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Whether the document is authentic (not tampered, proper seal/signature)
        /// </summary>
        public bool? IsAuthentic { get; set; }

        /// <summary>
        /// Whether the document is compliant with regulations
        /// </summary>
        public bool? IsCompliant { get; set; }

        /// <summary>
        /// Whether all required information is present in the document
        /// </summary>
        public bool? IsComplete { get; set; }

        /// <summary>
        /// JSON string containing checklist items and their verification status
        /// Format: [{"item": "Proper seal", "checked": true}, {"item": "Valid date", "checked": true}]
        /// </summary>
        [Column(TypeName = "text")]
        public string? ChecklistItems { get; set; }

        /// <summary>
        /// Time spent on verification in minutes
        /// </summary>
        public int? VerificationDurationMinutes { get; set; }

        /// <summary>
        /// IP address from which verification was performed (audit trail)
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// File size of the document at time of verification
        /// </summary>
        public long? DocumentSizeBytes { get; set; }

        /// <summary>
        /// File hash for tamper detection
        /// </summary>
        [MaxLength(100)]
        public string? DocumentHash { get; set; }

        /// <summary>
        /// Number of pages in the document
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// Additional metadata in JSON format
        /// </summary>
        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        // Navigation Properties
        [ForeignKey("DocumentId")]
        public virtual SEDocument Document { get; set; } = null!;

        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("VerifiedByOfficerId")]
        public virtual Officer? VerifiedByOfficer { get; set; }
    }
}
