namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Response DTO for assignment operations
    /// </summary>
    public class AssignmentResponseDto
    {
        public int AssignmentId { get; set; }
        public int ApplicationId { get; set; }
        public int AssignedToOfficerId { get; set; }
        public int? PreviousOfficerId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string? Reason { get; set; }
        public int? OfficerWorkload { get; set; }
        public string? StrategyUsed { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for reassigning an application
    /// </summary>
    public class ReassignApplicationRequestDto
    {
        public int ApplicationId { get; set; }
        public int NewOfficerId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for assignment history record
    /// </summary>
    public class AssignmentHistoryDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int? PreviousOfficerId { get; set; }
        public string? PreviousOfficerName { get; set; }
        public int AssignedToOfficerId { get; set; }
        public string? AssignedOfficerName { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string? Reason { get; set; }
        public int? OfficerWorkloadAtAssignment { get; set; }
        public string? StrategyUsed { get; set; }
        public bool NotificationSent { get; set; }
        public bool? OfficerAccepted { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal? AssignmentDurationHours { get; set; }
        public string? AdminComments { get; set; }
    }

    /// <summary>
    /// DTO for officer workload information
    /// </summary>
    public class OfficerWorkloadDto
    {
        public int OfficerId { get; set; }
        public string? OfficerName { get; set; }
        public int CurrentWorkload { get; set; }
    }

    /// <summary>
    /// DTO for workload statistics by role
    /// </summary>
    public class WorkloadStatisticsDto
    {
        public string Role { get; set; } = string.Empty;
        public List<OfficerWorkloadDto> OfficerWorkloads { get; set; } = new();
        public int TotalOfficers { get; set; }
        public int TotalWorkload { get; set; }
        public decimal AverageWorkload { get; set; }
    }

    /// <summary>
    /// Request DTO for escalating an application
    /// </summary>
    public class EscalateApplicationRequestDto
    {
        public string EscalationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for validating an assignment
    /// </summary>
    public class ValidateAssignmentRequestDto
    {
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }
    }

    /// <summary>
    /// Response DTO for assignment validation
    /// </summary>
    public class AssignmentValidationDto
    {
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
