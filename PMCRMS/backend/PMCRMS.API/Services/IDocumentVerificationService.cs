using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for managing document verification workflow
    /// </summary>
    public interface IDocumentVerificationService
    {
        /// <summary>
        /// Start verification process for a document
        /// </summary>
        /// <param name="documentId">ID of the SEDocument to verify</param>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <param name="documentType">Type of document being verified</param>
        /// <param name="verifiedByOfficerId">ID of the officer performing verification</param>
        /// <returns>VerificationResult containing the created DocumentVerification</returns>
        Task<VerificationResult> StartVerificationAsync(
            int documentId,
            int applicationId,
            string documentType,
            int verifiedByOfficerId);

        /// <summary>
        /// Update the verification checklist and flags for a verification
        /// </summary>
        /// <param name="verificationId">ID of the DocumentVerification</param>
        /// <param name="checklistItems">JSON string of checklist items</param>
        /// <param name="isAuthentic">Whether document is authentic</param>
        /// <param name="isCompliant">Whether document is compliant</param>
        /// <param name="isComplete">Whether document is complete</param>
        /// <param name="comments">Verification comments</param>
        /// <param name="updatedBy">Username of person updating</param>
        /// <returns>VerificationResult</returns>
        Task<VerificationResult> UpdateChecklistAsync(
            int verificationId,
            string? checklistItems,
            bool? isAuthentic,
            bool? isCompliant,
            bool? isComplete,
            string? comments,
            string updatedBy);

        /// <summary>
        /// Complete verification process (approve the document)
        /// </summary>
        /// <param name="verificationId">ID of the DocumentVerification</param>
        /// <param name="comments">Final verification comments</param>
        /// <param name="completedBy">Username of person completing</param>
        /// <returns>VerificationResult</returns>
        Task<VerificationResult> CompleteVerificationAsync(
            int verificationId,
            string? comments,
            string completedBy);

        /// <summary>
        /// Reject a document verification
        /// </summary>
        /// <param name="verificationId">ID of the DocumentVerification</param>
        /// <param name="rejectionReason">Reason for rejection</param>
        /// <param name="requiresResubmission">Whether document needs to be resubmitted</param>
        /// <param name="rejectedBy">Username of person rejecting</param>
        /// <returns>VerificationResult</returns>
        Task<VerificationResult> RejectVerificationAsync(
            int verificationId,
            string rejectionReason,
            bool requiresResubmission,
            string rejectedBy);

        /// <summary>
        /// Get a specific verification by ID
        /// </summary>
        /// <param name="verificationId">ID of the DocumentVerification</param>
        /// <returns>DocumentVerification or null</returns>
        Task<DocumentVerification?> GetVerificationByIdAsync(int verificationId);

        /// <summary>
        /// Get all verifications for an application
        /// </summary>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <returns>List of DocumentVerifications</returns>
        Task<List<DocumentVerification>> GetVerificationsForApplicationAsync(int applicationId);

        /// <summary>
        /// Get all verifications performed by a specific officer
        /// </summary>
        /// <param name="officerId">ID of the Officer</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of DocumentVerifications</returns>
        Task<List<DocumentVerification>> GetVerificationsForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Get pending verifications for an application
        /// </summary>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <returns>List of pending DocumentVerifications</returns>
        Task<List<DocumentVerification>> GetPendingVerificationsAsync(int applicationId);

        /// <summary>
        /// Check if all required documents for an application are verified
        /// </summary>
        /// <param name="applicationId">ID of the PositionApplication</param>
        /// <returns>True if all documents are approved</returns>
        Task<bool> AreAllDocumentsVerifiedAsync(int applicationId);

        /// <summary>
        /// Get verification statistics for an officer
        /// </summary>
        /// <param name="officerId">ID of the Officer</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>Dictionary of statistics</returns>
        Task<Dictionary<string, int>> GetVerificationStatisticsAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Calculate document hash for tamper detection
        /// </summary>
        /// <param name="documentId">ID of the SEDocument</param>
        /// <returns>SHA256 hash of the document</returns>
        Task<string> CalculateDocumentHashAsync(int documentId);
    }

    /// <summary>
    /// Result object for verification operations
    /// </summary>
    public class VerificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? VerificationId { get; set; }
        public DocumentVerification? Verification { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
