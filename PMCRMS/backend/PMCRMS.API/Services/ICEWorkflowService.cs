using PMCRMS.API.DTOs;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for City Engineer workflow operations (Final Approval)
    /// </summary>
    public interface ICEWorkflowService
    {
        /// <summary>
        /// Get all applications pending for City Engineer (all position types)
        /// </summary>
        Task<List<CEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId);

        /// <summary>
        /// Get all applications completed by City Engineer
        /// </summary>
        Task<List<CEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId);

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        Task<CEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId);

        /// <summary>
        /// Generate OTP for digital signature (calls HSM service)
        /// </summary>
        Task<string> GenerateOtpForSignatureAsync(int applicationId, int officerId);

        /// <summary>
        /// Verify documents, apply digital signature, and set final approval
        /// </summary>
        Task<WorkflowActionResultDto> VerifyAndSignDocumentsAsync(
            int applicationId, 
            int officerId, 
            string otp, 
            string? comments = null);

        /// <summary>
        /// Reject application with mandatory comments (Final rejection)
        /// </summary>
        Task<WorkflowActionResultDto> RejectApplicationAsync(
            int applicationId, 
            int officerId, 
            string rejectionComments);
    }
}
