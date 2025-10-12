using PMCRMS.API.DTOs;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for Executive Engineer workflow operations
    /// </summary>
    public interface IEEWorkflowService
    {
        /// <summary>
        /// Get all applications pending for Executive Engineer (all position types)
        /// </summary>
        Task<List<EEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId);

        /// <summary>
        /// Get all applications completed by Executive Engineer
        /// </summary>
        Task<List<EEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId);

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        Task<EEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId);

        /// <summary>
        /// Generate OTP for digital signature (calls HSM service)
        /// </summary>
        Task<string> GenerateOtpForSignatureAsync(int applicationId, int officerId);

        /// <summary>
        /// Verify documents, apply digital signature, and forward to City Engineer
        /// </summary>
        Task<WorkflowActionResultDto> VerifyAndSignDocumentsAsync(
            int applicationId, 
            int officerId, 
            string otp, 
            string? comments = null);

        /// <summary>
        /// Reject application with mandatory comments
        /// </summary>
        Task<WorkflowActionResultDto> RejectApplicationAsync(
            int applicationId, 
            int officerId, 
            string rejectionComments);
    }
}
