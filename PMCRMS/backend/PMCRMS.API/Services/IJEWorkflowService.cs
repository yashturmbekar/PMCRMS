using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for orchestrating the complete Junior Engineer workflow
    /// Coordinates auto-assignment, appointments, document verification, and digital signatures
    /// </summary>
    public interface IJEWorkflowService
    {
        /// <summary>
        /// Start the complete JE workflow for an application
        /// This auto-assigns the application to a JE based on the selected strategy
        /// </summary>
        Task<WorkflowActionResultDto> StartWorkflowAsync(StartJEWorkflowRequestDto request, int initiatedByUserId);

        /// <summary>
        /// Schedule appointment for site visit/document review
        /// Automatically transitions application to APPOINTMENT_SCHEDULED status
        /// </summary>
        Task<WorkflowActionResultDto> ScheduleAppointmentAsync(ScheduleAppointmentRequestDto request, int scheduledByOfficerId);

        /// <summary>
        /// Mark appointment as completed and transition to verification stage
        /// Creates document verification records for all required documents
        /// </summary>
        Task<WorkflowActionResultDto> CompleteAppointmentAsync(CompleteAppointmentRequestDto request, int officerId);

        /// <summary>
        /// Verify a document as part of the verification stage
        /// Automatically checks if all documents are verified and transitions status if complete
        /// </summary>
        Task<WorkflowActionResultDto> VerifyDocumentAsync(VerifyDocumentRequestDto request, int officerId);

        /// <summary>
        /// Complete all document verifications
        /// Transitions application to DOCUMENT_VERIFICATION_COMPLETED status
        /// </summary>
        Task<WorkflowActionResultDto> CompleteDocumentVerificationAsync(int applicationId, int officerId);

        /// <summary>
        /// Initiate digital signature process
        /// Transitions application to AWAITING_JE_DIGITAL_SIGNATURE status
        /// </summary>
        Task<WorkflowActionResultDto> InitiateDigitalSignatureAsync(int applicationId, int officerId, string documentPath);

        /// <summary>
        /// Complete digital signature with OTP
        /// Applies HSM signature and transitions to READY_FOR_AE_REVIEW status
        /// </summary>
        Task<WorkflowActionResultDto> CompleteDigitalSignatureAsync(ApplySignatureRequestDto request, int officerId);

        /// <summary>
        /// Get complete workflow status for an application
        /// </summary>
        Task<JEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId);

        /// <summary>
        /// Get complete workflow history with timeline
        /// </summary>
        Task<WorkflowHistoryDto?> GetWorkflowHistoryAsync(int applicationId);

        /// <summary>
        /// Transition workflow to a specific status (admin override)
        /// </summary>
        Task<WorkflowActionResultDto> TransitionToStatusAsync(TransitionWorkflowRequestDto request, int userId);

        /// <summary>
        /// Validate if workflow can proceed to next stage
        /// </summary>
        Task<WorkflowValidationResultDto> ValidateWorkflowProgressAsync(int applicationId);

        /// <summary>
        /// Get workflow summary for all applications
        /// </summary>
        Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get workflow metrics for dashboard
        /// </summary>
        Task<WorkflowMetricsDto> GetWorkflowMetricsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get all applications for a specific JE officer
        /// </summary>
        Task<List<JEWorkflowStatusDto>> GetOfficerApplicationsAsync(int officerId);

        /// <summary>
        /// Get all applications pending at a specific stage
        /// </summary>
        Task<List<JEWorkflowStatusDto>> GetApplicationsByStageAsync(ApplicationCurrentStatus status);

        /// <summary>
        /// Perform bulk workflow actions
        /// </summary>
        Task<List<WorkflowActionResultDto>> PerformBulkActionAsync(BulkWorkflowActionRequestDto request, int userId);

        /// <summary>
        /// Retry failed workflow step
        /// </summary>
        Task<WorkflowActionResultDto> RetryWorkflowStepAsync(int applicationId, string stepName, int userId);

        /// <summary>
        /// Cancel workflow for an application
        /// </summary>
        Task<WorkflowActionResultDto> CancelWorkflowAsync(int applicationId, string reason, int userId);

        /// <summary>
        /// Get workflow timeline events for reporting
        /// </summary>
        Task<List<WorkflowTimelineEventDto>> GetWorkflowTimelineAsync(int applicationId);

        /// <summary>
        /// Check for delayed applications and send reminders
        /// </summary>
        Task<int> SendDelayedApplicationRemindersAsync();
    }
}
