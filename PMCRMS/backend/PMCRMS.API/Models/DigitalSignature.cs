using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum SignatureType
    {
        JuniorEngineer = 0,
        AssistantEngineer = 1,
        ExecutiveEngineer = 2,
        CityEngineer = 3,
        StructuralEngineer = 4,
        Other = 5
    }

    public enum SignatureStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Verified = 4,
        Revoked = 5
    }

    /// <summary>
    /// Represents a digital signature applied to an application using HSM
    /// </summary>
    public class DigitalSignature : BaseEntity
    {
        /// <summary>
        /// Reference to the PositionApplication
        /// </summary>
        [Required]
        public int ApplicationId { get; set; }

        /// <summary>
        /// Type/Level of signature (role-based)
        /// </summary>
        public SignatureType Type { get; set; } = SignatureType.JuniorEngineer;

        /// <summary>
        /// Current status of the signature
        /// </summary>
        public SignatureStatus Status { get; set; } = SignatureStatus.Pending;

        /// <summary>
        /// Officer who applied the digital signature
        /// </summary>
        [Required]
        public int SignedByOfficerId { get; set; }

        /// <summary>
        /// Date when document was signed
        /// </summary>
        public DateTime? SignedDate { get; set; }

        /// <summary>
        /// Path to the original document to be signed
        /// </summary>
        [MaxLength(500)]
        public string? OriginalDocumentPath { get; set; }

        /// <summary>
        /// Path to the digitally signed document
        /// </summary>
        [MaxLength(500)]
        public string? SignedDocumentPath { get; set; }

        /// <summary>
        /// Cryptographic hash of the signature for verification
        /// </summary>
        [MaxLength(256)]
        public string? SignatureHash { get; set; }

        /// <summary>
        /// Digital certificate thumbprint
        /// </summary>
        [MaxLength(100)]
        public string? CertificateThumbprint { get; set; }

        /// <summary>
        /// Certificate issuer information
        /// </summary>
        [MaxLength(500)]
        public string? CertificateIssuer { get; set; }

        /// <summary>
        /// Certificate subject (signer details)
        /// </summary>
        [MaxLength(500)]
        public string? CertificateSubject { get; set; }

        /// <summary>
        /// Certificate expiry date
        /// </summary>
        public DateTime? CertificateExpiryDate { get; set; }

        /// <summary>
        /// HSM provider name (e.g., "eMudhra", "Sify", "nCode")
        /// </summary>
        [MaxLength(100)]
        public string? HsmProvider { get; set; }

        /// <summary>
        /// Transaction ID from HSM provider
        /// </summary>
        [MaxLength(200)]
        public string? HsmTransactionId { get; set; }

        /// <summary>
        /// Key label used for signing (from officer profile)
        /// </summary>
        [MaxLength(200)]
        public string? KeyLabel { get; set; }

        /// <summary>
        /// Signature coordinates on PDF (e.g., "100,100,200,150,1")
        /// </summary>
        [MaxLength(100)]
        public string? SignatureCoordinates { get; set; }

        /// <summary>
        /// Whether the signature has been verified
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Last verification date
        /// </summary>
        public DateTime? LastVerifiedDate { get; set; }

        /// <summary>
        /// Verification result/details
        /// </summary>
        [MaxLength(1000)]
        public string? VerificationDetails { get; set; }

        /// <summary>
        /// IP address from which signature was applied (audit trail)
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent/browser details
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Error message if signature failed
        /// </summary>
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// OTP used for signing (for audit, stored securely)
        /// </summary>
        [MaxLength(10)]
        public string? OtpUsed { get; set; }

        /// <summary>
        /// Signature started timestamp
        /// </summary>
        public DateTime? SignatureStartedAt { get; set; }

        /// <summary>
        /// Signature completed timestamp
        /// </summary>
        public DateTime? SignatureCompletedAt { get; set; }

        /// <summary>
        /// Duration of signature process in seconds
        /// </summary>
        public int? SignatureDurationSeconds { get; set; }

        /// <summary>
        /// Raw HSM response (for debugging/audit)
        /// </summary>
        [Column(TypeName = "text")]
        public string? HsmResponse { get; set; }

        /// <summary>
        /// Additional metadata in JSON format
        /// </summary>
        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        // Navigation Properties
        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("SignedByOfficerId")]
        public virtual Officer SignedByOfficer { get; set; } = null!;
    }
}
