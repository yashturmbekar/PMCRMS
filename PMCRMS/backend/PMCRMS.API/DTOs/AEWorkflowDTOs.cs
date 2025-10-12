using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Complete workflow status for an Assistant Engineer application
    /// </summary>
    public class AEWorkflowStatusDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public PositionType PositionType { get; set; }
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public string CurrentStatusDisplay { get; set; } = string.Empty;

        // Assignment
        public int? AssignedToAEId { get; set; }
        public string? AssignedToAEName { get; set; }
        public DateTime? AssignedToAEDate { get; set; }

        // JE Info
        public string? AssignedJEName { get; set; }
        public DateTime? JEApprovalDate { get; set; }

        // AE Actions
        public bool? AEApprovalStatus { get; set; }
        public string? AEApprovalComments { get; set; }
        public DateTime? AEApprovalDate { get; set; }
        public bool? AERejectionStatus { get; set; }
        public string? AERejectionComments { get; set; }
        public DateTime? AERejectionDate { get; set; }
        public bool AEDigitalSignatureApplied { get; set; }
        public DateTime? AEDigitalSignatureDate { get; set; }

        // Overall Progress
        public string CurrentStage { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
