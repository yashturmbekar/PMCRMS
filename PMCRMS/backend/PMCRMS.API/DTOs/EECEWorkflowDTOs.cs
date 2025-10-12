using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Complete workflow status for an Executive Engineer application
    /// </summary>
    public class EEWorkflowStatusDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public PositionType PositionType { get; set; }
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public string CurrentStatusDisplay { get; set; } = string.Empty;

        // Assignment
        public int? AssignedToEEId { get; set; }
        public string? AssignedToEEName { get; set; }
        public DateTime? AssignedToEEDate { get; set; }

        // Previous officers
        public string? AssignedJEName { get; set; }
        public string? AssignedAEName { get; set; }

        // EE Actions
        public bool? EEApprovalStatus { get; set; }
        public string? EEApprovalComments { get; set; }
        public DateTime? EEApprovalDate { get; set; }
        public bool? EERejectionStatus { get; set; }
        public string? EERejectionComments { get; set; }
        public DateTime? EERejectionDate { get; set; }
        public bool EEDigitalSignatureApplied { get; set; }
        public DateTime? EEDigitalSignatureDate { get; set; }

        // Overall Progress
        public string CurrentStage { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }

    /// <summary>
    /// Complete workflow status for a City Engineer application
    /// </summary>
    public class CEWorkflowStatusDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public PositionType PositionType { get; set; }
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public string CurrentStatusDisplay { get; set; } = string.Empty;

        // Assignment
        public int? AssignedToCEId { get; set; }
        public string? AssignedToCEName { get; set; }
        public DateTime? AssignedToCEDate { get; set; }

        // Previous officers
        public string? AssignedJEName { get; set; }
        public string? AssignedAEName { get; set; }
        public string? AssignedEEName { get; set; }

        // CE Actions
        public bool? CEApprovalStatus { get; set; }
        public string? CEApprovalComments { get; set; }
        public DateTime? CEApprovalDate { get; set; }
        public bool? CERejectionStatus { get; set; }
        public string? CERejectionComments { get; set; }
        public DateTime? CERejectionDate { get; set; }
        public bool CEDigitalSignatureApplied { get; set; }
        public DateTime? CEDigitalSignatureDate { get; set; }

        // Overall Progress
        public string CurrentStage { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
