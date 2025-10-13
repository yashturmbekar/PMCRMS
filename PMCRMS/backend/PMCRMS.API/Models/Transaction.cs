using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    /// <summary>
    /// Transaction entity for tracking payment transactions
    /// </summary>
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Unique transaction ID generated internally
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// BillDesk order ID
        /// </summary>
        [StringLength(100)]
        public string? BdOrderId { get; set; }

        /// <summary>
        /// Transaction status (PENDING, SUCCESS, FAILED, CANCELLED)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// Transaction amount/price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Amount actually paid (may differ from Price in case of partial payments)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountPaid { get; set; }

        /// <summary>
        /// Associated application ID
        /// </summary>
        [Required]
        public int ApplicationId { get; set; }

        /// <summary>
        /// User's first name
        /// </summary>
        [StringLength(100)]
        public string? FirstName { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        [StringLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        [StringLength(255)]
        public string? Email { get; set; }

        /// <summary>
        /// User's phone number
        /// </summary>
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// BillDesk/EaseBuzz specific status
        /// </summary>
        [StringLength(50)]
        public string? EaseBuzzStatus { get; set; }

        /// <summary>
        /// Error message if transaction failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Card type used for payment (if applicable)
        /// </summary>
        [StringLength(50)]
        public string? CardType { get; set; }

        /// <summary>
        /// Payment mode (Credit Card, Debit Card, Net Banking, UPI, etc.)
        /// </summary>
        [StringLength(50)]
        public string? Mode { get; set; }

        /// <summary>
        /// Payment gateway response data (JSON)
        /// </summary>
        public string? PaymentGatewayResponse { get; set; }

        /// <summary>
        /// RData from BillDesk
        /// </summary>
        public string? RData { get; set; }

        /// <summary>
        /// IP address of the client initiating payment
        /// </summary>
        [StringLength(50)]
        public string? ClientIpAddress { get; set; }

        /// <summary>
        /// User agent of the client
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to PositionApplication
        /// </summary>
        [ForeignKey(nameof(ApplicationId))]
        public virtual PositionApplication? Application { get; set; }
    }
}
