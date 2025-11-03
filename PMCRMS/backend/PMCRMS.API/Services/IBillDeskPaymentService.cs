using PMCRMS.API.ViewModels;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for BillDesk payment gateway integration
    /// </summary>
    public interface IBillDeskPaymentService
    {
        /// <summary>
        /// Initiate a payment transaction with BillDesk
        /// </summary>
        Task<PaymentResponseViewModel> InitiatePaymentAsync(
            InitiatePaymentRequestViewModel model, 
            string userId, 
            HttpContext httpContext);

        /// <summary>
        /// Legacy method for initiating payment (backward compatibility)
        /// </summary>
        Task<PaymentResponseViewModel> InitiatePaymentLegacyAsync(
            int applicationId, 
            string clientIp, 
            string userAgent);

        /// <summary>
        /// Verify payment status from BillDesk
        /// </summary>
        Task<PaymentVerificationResult> VerifyPaymentAsync(string transactionId, string bdOrderId);

        /// <summary>
        /// Process payment callback from BillDesk
        /// </summary>
        Task<PaymentCallbackResult> ProcessPaymentCallbackAsync(PaymentCallbackRequest request);

        /// <summary>
        /// Process encrypted payment callback from BillDesk (handles decryption)
        /// </summary>
        Task<PaymentCallbackResult> ProcessEncryptedCallbackAsync(
            int applicationId,
            string encryptedResponse,
            Guid? txnEntityId,
            string? bdOrderId);

        /// <summary>
        /// Decrypt BillDesk encrypted response (for testing/debugging)
        /// </summary>
        Task<DecryptionResult> DecryptPaymentResponseAsync(string encryptedResponse);
    }
}
