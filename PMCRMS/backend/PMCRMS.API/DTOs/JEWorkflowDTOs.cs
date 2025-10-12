using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Complete workflow status for a Junior Engineer application
    /// </summary>
    public class JEWorkflowStatusDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public string CurrentStatusDisplay { get; set; } = string.Empty;

        // Assignment
        public bool IsAssigned { get; set; }
        public int? AssignedToOfficerId { get; set; }
        public string? AssignedToOfficerName { get; set; }
        public DateTime? AssignedDate { get; set; }

        // Appointment
        public bool HasAppointment { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? AppointmentPlace { get; set; }
        public bool IsAppointmentCompleted { get; set; }

        // Document Verification
        public bool AllDocumentsVerified { get; set; }
        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public DateTime? DocumentsVerifiedDate { get; set; }

        // Digital Signature
        public bool DigitalSignatureApplied { get; set; }
        public DateTime? DigitalSignatureDate { get; set; }
        public int TotalSignatures { get; set; }
        public int CompletedSignatures { get; set; }

        // Overall Progress
        public double ProgressPercentage { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
        public bool CanProceedToNextStage { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    /// <summary>
    /// Request to start Junior Engineer workflow
    /// </summary>
    public class StartJEWorkflowRequestDto
    {
        public int ApplicationId { get; set; }
        public AssignmentStrategy Strategy { get; set; } = AssignmentStrategy.WorkloadBased;
    }

    /// <summary>
    /// Request to verify a document
    /// </summary>
    public class VerifyDocumentRequestDto
    {
        public int ApplicationId { get; set; }
        public string? Comments { get; set; }
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to generate OTP for digital signature
    /// </summary>
    public class GenerateOtpForSignatureDto
    {
        public int ApplicationId { get; set; }
    }

    /// <summary>
    /// Request to apply digital signature
    /// </summary>
    public class ApplySignatureRequestDto
    {
        public int ApplicationId { get; set; }
        public SignatureType SignatureType { get; set; }
        public string DocumentPath { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complete workflow summary for reporting
    /// </summary>
    public class WorkflowSummaryDto
    {
        public int TotalApplications { get; set; }
        public int PendingAssignment { get; set; }
        public int AppointmentScheduled { get; set; }
        public int UnderVerification { get; set; }
        public int AwaitingSignature { get; set; }
        public int CompletedJEStage { get; set; }

        public double AverageProcessingDays { get; set; }
        public double AssignmentSuccessRate { get; set; }
        public double AppointmentCompletionRate { get; set; }
        public double VerificationSuccessRate { get; set; }
        public double SignatureSuccessRate { get; set; }

        public List<OfficerWorkloadDto> OfficerWorkloads { get; set; } = new List<OfficerWorkloadDto>();
    }

    /// <summary>
    /// Workflow action result
    /// </summary>
    public class WorkflowActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApplicationCurrentStatus? NewStatus { get; set; }
        public string? NextAction { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Workflow timeline event
    /// </summary>
    public class WorkflowTimelineEventDto
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PerformedBy { get; set; }
        public ApplicationCurrentStatus? StatusBefore { get; set; }
        public ApplicationCurrentStatus? StatusAfter { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Complete workflow history for an application
    /// </summary>
    public class WorkflowHistoryDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public List<WorkflowTimelineEventDto> Timeline { get; set; } = new List<WorkflowTimelineEventDto>();
        public Dictionary<string, int> DurationByStage { get; set; } = new Dictionary<string, int>();
        public int TotalDurationDays { get; set; }
    }

    /// <summary>
    /// Bulk workflow operations request
    /// </summary>
    public class BulkWorkflowActionRequestDto
    {
        public List<int> ApplicationIds { get; set; } = new List<int>();
        public string ActionType { get; set; } = string.Empty; // "ASSIGN", "SCHEDULE", "VERIFY", "SIGN"
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Workflow validation result
    /// </summary>
    public class WorkflowValidationResultDto
    {
        public bool IsValid { get; set; }
        public bool CanAssign { get; set; }
        public bool CanScheduleAppointment { get; set; }
        public bool CanVerifyDocuments { get; set; }
        public bool CanApplySignature { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, bool> StageReadiness { get; set; } = new Dictionary<string, bool>();
    }

    /// <summary>
    /// Workflow metrics for dashboard
    /// </summary>
    public class WorkflowMetricsDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Volume metrics
        public int TotalApplicationsReceived { get; set; }
        public int ApplicationsInProgress { get; set; }
        public int ApplicationsCompleted { get; set; }

        // Performance metrics
        public double AverageAssignmentTimeHours { get; set; }
        public double AverageAppointmentSchedulingDays { get; set; }
        public double AverageVerificationDays { get; set; }
        public double AverageSignatureDays { get; set; }
        public double TotalAverageProcessingDays { get; set; }

        // Success rates
        public double OnTimeAppointmentRate { get; set; }
        public double FirstTimeVerificationPassRate { get; set; }
        public double SignatureSuccessRate { get; set; }

        // Bottlenecks
        public string? CurrentBottleneck { get; set; }
        public List<string> DelayedApplications { get; set; } = new List<string>();

        // Officer performance
        public List<OfficerWorkloadDto> TopPerformers { get; set; } = new List<OfficerWorkloadDto>();
    }

    /// <summary>
    /// Request to transition workflow to next stage
    /// </summary>
    public class TransitionWorkflowRequestDto
    {
        public int ApplicationId { get; set; }
        public ApplicationCurrentStatus TargetStatus { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Workflow notification settings
    /// </summary>
    public class WorkflowNotificationSettingsDto
    {
        public bool NotifyOnAssignment { get; set; } = true;
        public bool NotifyOnAppointmentScheduled { get; set; } = true;
        public bool NotifyOnAppointmentReminder { get; set; } = true;
        public bool NotifyOnVerificationComplete { get; set; } = true;
        public bool NotifyOnSignatureRequired { get; set; } = true;
        public bool NotifyOnStageCompletion { get; set; } = true;
        public int ReminderDaysBeforeAppointment { get; set; } = 1;
        public List<string> NotificationChannels { get; set; } = new List<string> { "Email", "SMS" };
    }
}
