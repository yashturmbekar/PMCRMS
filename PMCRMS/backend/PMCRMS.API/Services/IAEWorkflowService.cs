using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for Assistant Engineer workflow operations
    /// </summary>
    public interface IAEWorkflowService
    {
        /// <summary>
        /// Get all applications pending for a specific Assistant Engineer
        /// </summary>
        Task<List<AEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId, PositionType positionType);

        /// <summary>
        /// Get all applications completed by a specific Assistant Engineer
        /// </summary>
        Task<List<AEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId, PositionType positionType);

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        Task<AEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId, PositionType positionType);

        /// <summary>
        /// Generate OTP for digital signature (calls HSM service)
        /// </summary>
        Task<string> GenerateOtpForSignatureAsync(int applicationId, int officerId);

        /// <summary>
        /// Verify documents, apply digital signature, and forward to Executive Engineer
        /// </summary>
        Task<WorkflowActionResultDto> VerifyAndSignDocumentsAsync(
            int applicationId, 
            int officerId, 
            PositionType positionType,
            string otp, 
            string? comments = null);

        /// <summary>
        /// Reject application with mandatory comments
        /// </summary>
        Task<WorkflowActionResultDto> RejectApplicationAsync(
            int applicationId, 
            int officerId, 
            PositionType positionType,
            string rejectionComments);
    }
}
