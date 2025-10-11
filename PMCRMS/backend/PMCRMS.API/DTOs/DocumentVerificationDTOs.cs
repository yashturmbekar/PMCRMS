using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Request to start document verification
    /// </summary>
    public class StartVerificationRequestDto
    {
        public int DocumentId { get; set; }
        public int ApplicationId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to update verification checklist
    /// </summary>
    public class UpdateChecklistRequestDto
    {
        public string? ChecklistItems { get; set; }
        public bool? IsAuthentic { get; set; }
        public bool? IsCompliant { get; set; }
        public bool? IsComplete { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to complete verification
    /// </summary>
    public class CompleteVerificationRequestDto
    {
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to reject verification
    /// </summary>
    public class RejectVerificationRequestDto
    {
        public string RejectionReason { get; set; } = string.Empty;
        public bool RequiresResubmission { get; set; }
    }

    /// <summary>
    /// Full verification details response
    /// </summary>
    public class VerificationResponseDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public VerificationStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public int? VerifiedByOfficerId { get; set; }
        public string? OfficerName { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public DateTime? VerificationStartedAt { get; set; }
        public string? VerificationComments { get; set; }
        public string? RejectionReason { get; set; }
        public bool? IsAuthentic { get; set; }
        public bool? IsCompliant { get; set; }
        public bool? IsComplete { get; set; }
        public string? ChecklistItems { get; set; }
        public int? VerificationDurationMinutes { get; set; }
        public string? DocumentHash { get; set; }
        public long? DocumentSizeBytes { get; set; }
        public int? PageCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Simplified verification list item
    /// </summary>
    public class VerificationListDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public VerificationStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime? VerifiedDate { get; set; }
        public string? OfficerName { get; set; }
        public bool IsPending { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// Checklist item structure
    /// </summary>
    public class ChecklistItemDto
    {
        public string Item { get; set; } = string.Empty;
        public bool Checked { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Verification statistics
    /// </summary>
    public class VerificationStatisticsDto
    {
        public int TotalVerifications { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int RequiresResubmissionCount { get; set; }
        public int AverageVerificationMinutes { get; set; }
        public double ApprovalRate { get; set; }
        public double RejectionRate { get; set; }
    }

    /// <summary>
    /// Application verification summary
    /// </summary>
    public class ApplicationVerificationSummaryDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public int PendingDocuments { get; set; }
        public int RejectedDocuments { get; set; }
        public bool AllDocumentsVerified { get; set; }
        public double VerificationProgress { get; set; }
        public List<VerificationListDto> Verifications { get; set; } = new List<VerificationListDto>();
    }
}
