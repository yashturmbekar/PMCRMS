using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    /// <summary>
    /// Stores temporary download tokens for document access
    /// Tokens are time-limited and single-use for security
    /// </summary>
    public class DownloadToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Associated application ID
        /// </summary>
        public int ApplicationId { get; set; }

        /// <summary>
        /// Cryptographically secure download token (GUID or JWT)
        /// Valid for 24-48 hours after OTP verification
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 6-digit OTP for initial verification
        /// Single-use, expires in 30 minutes
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Otp { get; set; } = string.Empty;

        /// <summary>
        /// Applicant email address (for OTP delivery and verification)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration timestamp (24-48 hours from creation)
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// OTP expiration timestamp (30 minutes from creation)
        /// </summary>
        [Required]
        public DateTime OtpExpiresAt { get; set; }

        /// <summary>
        /// Whether the OTP has been verified (token activated)
        /// </summary>
        public bool IsOtpVerified { get; set; }

        /// <summary>
        /// Whether the token has been revoked or invalidated
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Timestamp when token was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Number of failed OTP verification attempts
        /// Used for rate limiting and security
        /// </summary>
        public int FailedAttempts { get; set; }

        /// <summary>
        /// IP address from which the token was requested
        /// </summary>
        [MaxLength(50)]
        public string? RequestIpAddress { get; set; }

        // Navigation property
        public virtual Application? Application { get; set; }
    }

    /// <summary>
    /// Audit log for document downloads
    /// Tracks all download attempts for security and compliance
    /// </summary>
    public class DownloadAuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Associated application ID
        /// </summary>
        public int ApplicationId { get; set; }

        /// <summary>
        /// Download token used for this download
        /// </summary>
        [MaxLength(512)]
        public string? Token { get; set; }

        /// <summary>
        /// Type of document downloaded
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty; // Certificate, RecommendationForm, Challan

        /// <summary>
        /// Timestamp when document was downloaded
        /// </summary>
        [Required]
        public DateTime DownloadedAt { get; set; }

        /// <summary>
        /// IP address from which the download was made
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string (browser/device info)
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether the download was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if download failed
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        // Navigation property
        public virtual Application? Application { get; set; }
    }
}
