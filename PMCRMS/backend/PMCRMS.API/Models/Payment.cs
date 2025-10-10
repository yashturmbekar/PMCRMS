using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum PaymentStatus
    {
        Pending = 1,
        InProgress = 2,
        Success = 3,
        Failed = 4,
        Cancelled = 5,
        Refunded = 6
    }

    public enum PaymentMethod
    {
        EaseBuzz = 1,
        BankTransfer = 2,
        Cash = 3,
        Cheque = 4,
        DD = 5
    }

    public class Payment : BaseEntity
    {
        [Required]
        public int ApplicationId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        public PaymentMethod Method { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedDate { get; set; }
        
        [MaxLength(100)]
        public string? GatewayTransactionId { get; set; }
        
        [MaxLength(50)]
        public string? GatewayPaymentId { get; set; }
        
        [MaxLength(1000)]
        public string? GatewayResponse { get; set; }
        
        [MaxLength(500)]
        public string? FailureReason { get; set; }
        
        public int? ProcessedBy { get; set; }
        
        [MaxLength(500)]
        public string? ProcessingRemarks { get; set; }
        
        // Foreign Keys
        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; } = null!;
        
        [ForeignKey("ProcessedBy")]
        public virtual User? ProcessedByUser { get; set; }
    }
}
