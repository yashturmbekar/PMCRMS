namespace PMCRMS.API.DTOs
{
    public class UpdateStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class UpdateApplicationStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int? AssignedOfficerId { get; set; }
    }

    public class WorkflowDto
    {
        public string ApplicationType { get; set; } = string.Empty;
        public List<WorkflowStepDto> Steps { get; set; } = new List<WorkflowStepDto>();
    }

    public class WorkflowStepDto
    {
        public int Step { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}