namespace PMCRMS.API.ViewModels
{
    /// <summary>
    /// Request model for initiating a payment
    /// </summary>
    public class InitiatePaymentRequestViewModel
    {
        /// <summary>
        /// Application ID (entity ID) for which payment is being made
        /// </summary>
        public string EntityId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for payment initiation
    /// </summary>
    public class PaymentResponseViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? TxnEntityId { get; set; }
        public string? BdOrderId { get; set; }
        public string? RData { get; set; }
        public string? PaymentGatewayUrl { get; set; }
        public string? MerchantId { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Request model for payment initialization (legacy)
    /// </summary>
    public class PaymentInitializationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BdOrderId { get; set; }
        public string? RData { get; set; }
        public string? PaymentGatewayUrl { get; set; }
        public string? MerchantId { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Request model for payment success callback
    /// </summary>
    public class PaymentSuccessRequest
    {
        public int ApplicationId { get; set; }
        public Guid? TxnEntityId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CardType { get; set; }
        public string? Mode { get; set; }
        public string? Amount { get; set; }
    }

    /// <summary>
    /// Response model for payment success processing
    /// </summary>
    public class PaymentSuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
    }

    /// <summary>
    /// Result of payment verification
    /// </summary>
    public class PaymentVerificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Status { get; set; }
        public decimal? Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? BdOrderId { get; set; }
    }

    /// <summary>
    /// Request model for payment callback from BillDesk
    /// </summary>
    public class PaymentCallbackRequest
    {
        public int ApplicationId { get; set; }
        public Guid? TxnEntityId { get; set; }
        public string? BdOrderId { get; set; }
        public string? TransactionId { get; set; }
        public string? Status { get; set; }
        public string? Amount { get; set; }
        public string? ResponseData { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of payment callback processing
    /// </summary>
    public class PaymentCallbackResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
        public string? ApplicationStatus { get; set; }
    }
}
