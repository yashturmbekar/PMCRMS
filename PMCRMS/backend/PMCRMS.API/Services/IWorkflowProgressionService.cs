using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for managing automatic workflow progression through all approval stages
    /// Handles auto-assignment at each stage transition
    /// </summary>
    public interface IWorkflowProgressionService
    {
        /// <summary>
        /// STAGE 1→2: Progress from JE Digital Signature to Assistant Engineer
        /// Triggered when JE completes digital signature
        /// </summary>
        Task<bool> ProgressToAssistantEngineerAsync(int applicationId);

        /// <summary>
        /// STAGE 2→3: Progress from AE Approval to Executive Engineer (Stage 1)
        /// Triggered when AE approves and completes digital signature
        /// </summary>
        Task<bool> ProgressToExecutiveEngineerStage1Async(int applicationId);

        /// <summary>
        /// STAGE 3→4: Progress from EE Stage 1 to City Engineer
        /// Triggered when EE approves and completes digital signature
        /// Routes to payment after CE approval
        /// </summary>
        Task<bool> ProgressToCityEngineerAsync(int applicationId);

        /// <summary>
        /// STAGE 4→5: Progress from CE Approval to Payment
        /// User receives payment notification
        /// </summary>
        Task<bool> ProgressToPaymentAsync(int applicationId);

        /// <summary>
        /// STAGE 5→6: Progress from Payment to Clerk
        /// Triggered when user completes payment
        /// </summary>
        Task<bool> ProgressToClerkAsync(int applicationId);

        /// <summary>
        /// STAGE 6→7: Progress from Clerk to Executive Engineer (Digital Signature)
        /// Triggered when clerk completes processing
        /// </summary>
        Task<bool> ProgressToExecutiveEngineerSignatureAsync(int applicationId);

        /// <summary>
        /// STAGE 7→COMPLETE: Progress from EE Signature to City Engineer Final Signature
        /// Triggered when EE completes digital signature
        /// Final stage before certificate issuance
        /// </summary>
        Task<bool> ProgressToCityEngineerFinalSignatureAsync(int applicationId);

        /// <summary>
        /// FINAL: Complete workflow and issue certificate
        /// Triggered when CE completes final digital signature
        /// </summary>
        Task<bool> CompleteWorkflowAsync(int applicationId);

        /// <summary>
        /// Get current workflow stage information
        /// </summary>
        Task<WorkflowStageInfo?> GetWorkflowStageAsync(int applicationId);

        /// <summary>
        /// Get complete workflow history for an application
        /// </summary>
        Task<List<WorkflowProgressionHistory>> GetWorkflowHistoryAsync(int applicationId);
    }

    /// <summary>
    /// Workflow stage information
    /// </summary>
    public class WorkflowStageInfo
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public string CurrentStageName { get; set; } = string.Empty;
        public string NextStageName { get; set; } = string.Empty;
        public int? CurrentlyAssignedOfficerId { get; set; }
        public string? CurrentlyAssignedOfficerName { get; set; }
        public OfficerRole? CurrentlyAssignedOfficerRole { get; set; }
        public DateTime? CurrentStageStartDate { get; set; }
        public int CurrentStageNumber { get; set; }
        public int TotalStages { get; set; } = 7;
        public decimal ProgressPercentage { get; set; }
        public bool CanProgress { get; set; }
        public string? ProgressBlockedReason { get; set; }
        public List<string> RequiredActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Workflow progression history record
    /// </summary>
    public class WorkflowProgressionHistory
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationCurrentStatus FromStatus { get; set; }
        public ApplicationCurrentStatus ToStatus { get; set; }
        public string FromStageName { get; set; } = string.Empty;
        public string ToStageName { get; set; } = string.Empty;
        public int? FromOfficerId { get; set; }
        public string? FromOfficerName { get; set; }
        public int? ToOfficerId { get; set; }
        public string? ToOfficerName { get; set; }
        public DateTime ProgressionDate { get; set; }
        public string? Comments { get; set; }
        public bool IsAutoProgression { get; set; }
        public string ProgressionTriggeredBy { get; set; } = string.Empty;
    }
}
