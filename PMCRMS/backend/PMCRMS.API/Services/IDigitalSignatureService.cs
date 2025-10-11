using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for managing digital signature workflow with HSM integration
    /// </summary>
    public interface IDigitalSignatureService
    {
        /// <summary>
        /// Initiate digital signature process for an application
        /// </summary>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <param name="signedByOfficerId">ID of the officer applying signature</param>
        /// <param name="signatureType">Type of signature (JE, AE, EE, etc.)</param>
        /// <param name="documentPath">Path to the document to be signed</param>
        /// <param name="coordinates">Signature coordinates on PDF (x,y,width,height,page)</param>
        /// <param name="ipAddress">IP address of the signer</param>
        /// <param name="userAgent">Browser/client user agent</param>
        /// <returns>SignatureResult with signature ID and status</returns>
        Task<SignatureResult> InitiateSignatureAsync(
            int applicationId,
            int signedByOfficerId,
            SignatureType signatureType,
            string documentPath,
            string coordinates,
            string? ipAddress,
            string? userAgent);

        /// <summary>
        /// Complete signature process with OTP verification and HSM signing
        /// </summary>
        /// <param name="signatureId">ID of the DigitalSignature</param>
        /// <param name="otp">OTP for HSM authentication</param>
        /// <param name="completedBy">Username completing the signature</param>
        /// <returns>SignatureResult with signed document details</returns>
        Task<SignatureResult> CompleteSignatureAsync(
            int signatureId,
            string otp,
            string completedBy);

        /// <summary>
        /// Verify a digital signature
        /// </summary>
        /// <param name="signatureId">ID of the DigitalSignature to verify</param>
        /// <returns>SignatureResult with verification details</returns>
        Task<SignatureResult> VerifySignatureAsync(int signatureId);

        /// <summary>
        /// Get signature by ID
        /// </summary>
        /// <param name="signatureId">ID of the DigitalSignature</param>
        /// <returns>DigitalSignature or null</returns>
        Task<DigitalSignature?> GetSignatureByIdAsync(int signatureId);

        /// <summary>
        /// Get all signatures for an application
        /// </summary>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <returns>List of DigitalSignatures</returns>
        Task<List<DigitalSignature>> GetSignaturesForApplicationAsync(int applicationId);

        /// <summary>
        /// Get all signatures by a specific officer
        /// </summary>
        /// <param name="officerId">ID of the Officer</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of DigitalSignatures</returns>
        Task<List<DigitalSignature>> GetSignaturesForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Retry failed signature
        /// </summary>
        /// <param name="signatureId">ID of the failed DigitalSignature</param>
        /// <param name="otp">New OTP for retry</param>
        /// <param name="retriedBy">Username retrying the signature</param>
        /// <returns>SignatureResult</returns>
        Task<SignatureResult> RetrySignatureAsync(
            int signatureId,
            string otp,
            string retriedBy);

        /// <summary>
        /// Revoke/cancel a signature
        /// </summary>
        /// <param name="signatureId">ID of the DigitalSignature</param>
        /// <param name="reason">Reason for revocation</param>
        /// <param name="revokedBy">Username revoking the signature</param>
        /// <returns>SignatureResult</returns>
        Task<SignatureResult> RevokeSignatureAsync(
            int signatureId,
            string reason,
            string revokedBy);

        /// <summary>
        /// Get signature statistics for an officer
        /// </summary>
        /// <param name="officerId">ID of the Officer</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>Dictionary of statistics</returns>
        Task<Dictionary<string, int>> GetSignatureStatisticsAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Check if officer's certificate is valid
        /// </summary>
        /// <param name="officerId">ID of the Officer</param>
        /// <returns>True if certificate is valid and not expired</returns>
        Task<bool> IsCertificateValidAsync(int officerId);

        /// <summary>
        /// Get HSM service health status
        /// </summary>
        /// <returns>True if HSM service is accessible</returns>
        Task<bool> CheckHsmHealthAsync();
    }

    /// <summary>
    /// Result object for digital signature operations
    /// </summary>
    public class SignatureResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? SignatureId { get; set; }
        public DigitalSignature? Signature { get; set; }
        public string? SignedDocumentPath { get; set; }
        public string? SignatureHash { get; set; }
        public bool? IsVerified { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
