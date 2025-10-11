using System.ComponentModel.DataAnnotations;
using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    // Officer Invitation DTOs
    public class InviteOfficerRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty; // Accepts string, converted to enum in controller

        // Optional - will be auto-generated if not provided
        [MaxLength(50)]
        public string? EmployeeId { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        public int ExpiryDays { get; set; } = 7;
    }

    public class OfficerInvitationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime InvitedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public string InvitedBy { get; set; } = string.Empty; // Alias for InvitedByName
        public bool IsExpired { get; set; }
        public int? OfficerId { get; set; } // Changed from UserId to OfficerId
        public string? TemporaryPassword { get; set; } // Only included when creating new invitation
    }

    public class ResendInvitationRequest
    {
        [Required]
        public int InvitationId { get; set; }
        
        public int ExpiryDays { get; set; } = 7;
    }

    public class UpdateOfficerRequest
    {
        [Required]
        public int OfficerId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public OfficerRole? Role { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        public bool? IsActive { get; set; }
    }

    // Officer Management DTOs
    public class OfficerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ApplicationsProcessed { get; set; }
    }

    public class OfficerListResponse
    {
        public List<OfficerDto> Officers { get; set; } = new List<OfficerDto>();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public class InvitationListResponse
    {
        public List<OfficerInvitationDto> Invitations { get; set; } = new List<OfficerInvitationDto>();
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int RevokedCount { get; set; }
    }

    public class OfficerDetailDto : OfficerDto
    {
        public string? Department { get; set; } // Changed from Address to Department
        public DateTime? UpdatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public List<ApplicationStatusSummaryDto> RecentStatusUpdates { get; set; } = new List<ApplicationStatusSummaryDto>();
    }

    public class ApplicationStatusSummaryDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? Remarks { get; set; }
    }

    // Form Configuration DTOs
    public class FormConfigurationDto
    {
        public int Id { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string FormType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BaseFee { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalFee { get; set; }
        public bool IsActive { get; set; }
        public bool AllowOnlineSubmission { get; set; }
        public int ProcessingDays { get; set; }
        public int? MaxFileSizeMB { get; set; }
        public int? MaxFilesAllowed { get; set; }
        public string? CustomFields { get; set; } // JSON string
        public string? RequiredDocuments { get; set; } // JSON string
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class FormConfigurationDetailDto : FormConfigurationDto
    {
        public List<FormFeeHistoryDto> FeeHistory { get; set; } = new List<FormFeeHistoryDto>();
    }

    public class FormFeeHistoryDto
    {
        public int Id { get; set; }
        public decimal OldBaseFee { get; set; }
        public decimal NewBaseFee { get; set; }
        public decimal OldProcessingFee { get; set; }
        public decimal NewProcessingFee { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? ChangeReason { get; set; }
        public DateTime ChangedDate { get; set; }
    }

    public class UpdateFormFeesRequest
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal BaseFee { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal ProcessingFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? LateFee { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }
    }

    public class UpdateFormCustomFieldsRequest
    {
        [Required]
        public string CustomFieldsJson { get; set; } = string.Empty; // JSON string of custom fields
    }

    public class CustomFieldDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty; // text, number, date, dropdown, file
        public bool IsRequired { get; set; }
        public string? Label { get; set; }
        public string? Placeholder { get; set; }
        public List<string>? Options { get; set; } // For dropdown fields
        public string? ValidationRule { get; set; }
    }

    public class UpdateFormConfigurationRequest
    {
        [MaxLength(100)]
        public string? FormName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public bool? AllowOnlineSubmission { get; set; }

        [Range(1, 365)]
        public int? ProcessingDays { get; set; }

        [Range(1, 100)]
        public int? MaxFileSizeMB { get; set; }

        [Range(1, 50)]
        public int? MaxFilesAllowed { get; set; }

        public string? RequiredDocuments { get; set; } // JSON string
    }

    public class CreateFormConfigurationRequest
    {
        [Required]
        [MaxLength(100)]
        public string FormName { get; set; } = string.Empty;

        [Required]
        public FormType FormType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BaseFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ProcessingFee { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal LateFee { get; set; } = 0;

        [Range(1, 365)]
        public int ProcessingDays { get; set; } = 30;

        [Range(1, 100)]
        public int MaxFileSizeMB { get; set; } = 5;

        [Range(1, 50)]
        public int MaxFilesAllowed { get; set; } = 10;

        public List<CustomFieldDto>? CustomFields { get; set; }

        public List<string>? RequiredDocuments { get; set; }
    }

    // Dashboard Statistics
    public class AdminDashboardStats
    {
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public int TotalOfficers { get; set; }
        public int ActiveOfficers { get; set; }
        public int PendingInvitations { get; set; }
        public decimal TotalRevenueCollected { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public List<ApplicationTrendDto> ApplicationTrends { get; set; } = new List<ApplicationTrendDto>();
        public List<RoleDistributionDto> RoleDistribution { get; set; } = new List<RoleDistributionDto>();
    }

    public class ApplicationTrendDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RoleDistributionDto
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
        public int ActiveCount { get; set; }
    }
}
