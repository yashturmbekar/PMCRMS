using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for Clerk workflow operations using PositionApplication table
    /// Handles post-payment application processing
    /// Status progression: PaymentCompleted → CLERK_PENDING → EXECUTIVE_ENGINEER_SIGN_PENDING
    /// Note: Clerk does NOT have digital signature functionality
    /// </summary>
    public class ClerkWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ClerkWorkflowService> _logger;
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IWorkflowProgressionService _workflowProgressionService;

        public ClerkWorkflowService(
            PMCRMSDbContext context,
            ILogger<ClerkWorkflowService> logger,
            IWorkflowNotificationService workflowNotificationService,
            IWorkflowProgressionService workflowProgressionService)
        {
            _context = context;
            _logger = logger;
            _workflowNotificationService = workflowNotificationService;
            _workflowProgressionService = workflowProgressionService;
        }

        public async Task<List<ClerkApplicationDto>> GetPendingApplicationsAsync(int clerkId)
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Getting pending applications for Clerk {ClerkId}", clerkId);

                var applications = await _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.AssignedClerk)
                    .Include(a => a.AssignedCityEngineer)
                    .Where(a => a.Status == ApplicationCurrentStatus.CLERK_PENDING && a.AssignedClerkId == clerkId)
                    .OrderBy(a => a.AssignedToClerkDate)
                    .ToListAsync();

                var result = applications.Select(a =>
                {
                    return new ClerkApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        ApplicantMobile = a.MobileNumber,
                        PositionType = a.PositionType.ToString(),
                        AssignedAEName = a.AssignedCityEngineer?.Name, // City Engineer who approved before payment
                        AssignedToClerkDate = a.AssignedToClerkDate,
                        SubmittedDate = a.SubmittedDate ?? DateTime.UtcNow,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    };
                }).ToList();

                _logger.LogInformation("[ClerkWorkflow] Found {Count} pending applications for Clerk {ClerkId}", result.Count, clerkId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error getting pending applications for Clerk {ClerkId}", clerkId);
                throw;
            }
        }

        public async Task<ClerkApplicationDetailDto?> GetApplicationDetailsAsync(int applicationId, int clerkId)
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Getting application details: {ApplicationId} for Clerk {ClerkId}", applicationId, clerkId);

                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.AssignedClerk)
                    .Include(a => a.AssignedJuniorEngineer)
                    .Include(a => a.AssignedCityEngineer)
                    .Where(a => a.Id == applicationId && a.AssignedClerkId == clerkId)
                    .Select(a => new ClerkApplicationDetailDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        ApplicantMobile = a.MobileNumber,
                        PositionType = a.PositionType.ToString(),
                        CurrentStatus = a.Status.ToString(),
                        AssignedToClerkDate = a.AssignedToClerkDate,
                        JuniorEngineerName = a.AssignedJuniorEngineer != null ? a.AssignedJuniorEngineer.Name : null,
                        CityEngineerName = a.AssignedCityEngineer != null ? a.AssignedCityEngineer.Name : null,
                        CityEngineerApprovalDate = a.CityEngineerApprovalDate,
                        SubmittedDate = a.SubmittedDate ?? DateTime.UtcNow,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    })
                    .FirstOrDefaultAsync();

                return application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error getting application details: {ApplicationId}", applicationId);
                throw;
            }
        }

        public async Task<ClerkActionResult> ApproveApplicationAsync(int applicationId, string? remarks, int clerkId)
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Approving application {ApplicationId} by Clerk {ClerkId}", applicationId, clerkId);

                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId && a.AssignedClerkId == clerkId);

                if (application == null)
                {
                    return new ClerkActionResult { Success = false, Message = "Application not found or not assigned to you" };
                }

                if (application.Status != ApplicationCurrentStatus.CLERK_PENDING)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = $"Application is not in CLERK_PENDING status. Current status: {application.Status}"
                    };
                }

                application.ClerkApprovalStatus = true;
                application.ClerkApprovalComments = remarks;
                application.ClerkApprovalDate = DateTime.UtcNow;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = $"Clerk_{clerkId}";

                await _context.SaveChangesAsync();

                _logger.LogInformation("[ClerkWorkflow] Application {ApplicationId} approved successfully by Clerk {ClerkId}", applicationId, clerkId);

                // Progress to Executive Engineer Stage 2 first
                var progressSuccess = await _workflowProgressionService.ProgressToExecutiveEngineerSignatureAsync(applicationId);

                if (!progressSuccess)
                {
                    _logger.LogWarning("[ClerkWorkflow] Application {ApplicationId} approved but failed to progress to EE Stage 2", applicationId);
                }

                // Send notification with the NEW status (EXECUTIVE_ENGINEER_SIGN_PENDING)
                try
                {
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(applicationId, ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "[ClerkWorkflow] Failed to send approval email for application {ApplicationId}", applicationId);
                }

                return new ClerkActionResult
                {
                    Success = true,
                    Message = "Application approved successfully and forwarded to Executive Engineer (Stage 2) for certificate signature",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error approving application {ApplicationId}", applicationId);
                return new ClerkActionResult { Success = false, Message = $"Error approving application: {ex.Message}" };
            }
        }

        public async Task<ClerkActionResult> RejectApplicationAsync(int applicationId, string rejectionReason, int clerkId)
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Rejecting application {ApplicationId} by Clerk {ClerkId}", applicationId, clerkId);

                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    return new ClerkActionResult { Success = false, Message = "Rejection reason is required" };
                }

                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.Appointments)
                    .FirstOrDefaultAsync(a => a.Id == applicationId && a.AssignedClerkId == clerkId);

                if (application == null)
                {
                    return new ClerkActionResult { Success = false, Message = "Application not found or not assigned to you" };
                }

                if (application.Status != ApplicationCurrentStatus.CLERK_PENDING)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = $"Application is not in CLERK_PENDING status. Current status: {application.Status}"
                    };
                }

                // Store rejection information
                var rejectionInfo = $"Rejected by Clerk on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {rejectionReason}";
                application.Remarks = rejectionInfo;

                // Set status to REJECTED
                application.Status = ApplicationCurrentStatus.REJECTED;
                
                // Clear all officer assignments
                application.AssignedJuniorEngineerId = null;
                application.AssignedToJEDate = null;
                application.AssignedExecutiveEngineerId = null;
                application.AssignedToExecutiveEngineerDate = null;
                application.AssignedCityEngineerId = null;
                application.AssignedToCityEngineerDate = null;
                application.AssignedAEArchitectId = null;
                application.AssignedToAEArchitectDate = null;
                application.AssignedAEStructuralId = null;
                application.AssignedToAEStructuralDate = null;
                application.AssignedAELicenceId = null;
                application.AssignedToAELicenceDate = null;
                application.AssignedAESupervisor1Id = null;
                application.AssignedToAESupervisor1Date = null;
                application.AssignedAESupervisor2Id = null;
                application.AssignedToAESupervisor2Date = null;
                application.AssignedEEStage2Id = null;
                application.AssignedToEEStage2Date = null;
                application.AssignedCEStage2Id = null;
                application.AssignedToCEStage2Date = null;
                application.AssignedClerkId = null;
                application.AssignedToClerkDate = null;

                // Clear JE workflow fields
                application.JEDigitalSignatureApplied = false;
                application.JEDigitalSignatureDate = null;
                application.JERejectionStatus = null;
                application.JERejectionComments = null;
                application.JERejectionDate = null;

                // Clear recommendation form
                application.IsRecommendationFormGenerated = false;
                application.RecommendationFormGeneratedDate = null;
                application.RecommendationFormGenerationAttempts = 0;
                application.RecommendationFormGenerationError = null;

                // Clear all AE workflow fields
                application.AEArchitectApprovalStatus = null;
                application.AEArchitectRejectionStatus = null;
                application.AEArchitectRejectionComments = null;
                application.AEArchitectRejectionDate = null;
                application.AEStructuralApprovalStatus = null;
                application.AEStructuralRejectionStatus = null;
                application.AEStructuralRejectionComments = null;
                application.AEStructuralRejectionDate = null;
                application.AELicenceApprovalStatus = null;
                application.AELicenceRejectionStatus = null;
                application.AELicenceRejectionComments = null;
                application.AELicenceRejectionDate = null;
                application.AELicenceDigitalSignatureApplied = false;
                application.AELicenceDigitalSignatureDate = null;
                application.AESupervisor1ApprovalStatus = null;
                application.AESupervisor1RejectionStatus = null;
                application.AESupervisor1RejectionComments = null;
                application.AESupervisor1RejectionDate = null;
                application.AESupervisor2ApprovalStatus = null;
                application.AESupervisor2RejectionStatus = null;
                application.AESupervisor2RejectionComments = null;
                application.AESupervisor2RejectionDate = null;

                // Clear EE workflow fields
                application.ExecutiveEngineerRejectionStatus = null;
                application.ExecutiveEngineerRejectionComments = null;
                application.ExecutiveEngineerRejectionDate = null;

                // Clear CE workflow fields
                application.CityEngineerRejectionStatus = null;
                application.CityEngineerRejectionComments = null;
                application.CityEngineerRejectionDate = null;

                // Clear clerk workflow fields
                application.ClerkApprovalStatus = null;
                application.ClerkApprovalComments = null;
                application.ClerkApprovalDate = null;
                application.ClerkRejectionStatus = null;
                application.ClerkRejectionComments = null;
                application.ClerkRejectionDate = null;

                // Delete all appointments
                if (application.Appointments != null && application.Appointments.Any())
                {
                    _context.Appointments.RemoveRange(application.Appointments);
                }

                // Delete recommendation form document
                var recommendationDoc = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                            d.DocumentType == SEDocumentType.RecommendedForm);
                if (recommendationDoc != null)
                {
                    _context.SEDocuments.Remove(recommendationDoc);
                }

                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = $"Clerk_{clerkId}";

                await _context.SaveChangesAsync();

                _logger.LogInformation("[ClerkWorkflow] Application {ApplicationId} rejected by Clerk {ClerkId} and reset to initial state", applicationId, clerkId);

                try
                {
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(applicationId, ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING, rejectionInfo);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "[ClerkWorkflow] Failed to send rejection email for application {ApplicationId}", applicationId);
                }

                return new ClerkActionResult
                {
                    Success = true,
                    Message = "Application rejected. The applicant can edit and resubmit the application.",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.REJECTED.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error rejecting application {ApplicationId}", applicationId);
                return new ClerkActionResult { Success = false, Message = $"Error rejecting application: {ex.Message}" };
            }
        }

        public async Task<List<ClerkApplicationDto>> GetCompletedApplicationsAsync(int clerkId)
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Getting completed applications for Clerk {ClerkId}", clerkId);

                var applications = await _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.AssignedClerk)
                    .Where(a => a.AssignedClerkId == clerkId && (a.ClerkApprovalStatus == true || a.ClerkRejectionStatus == true))
                    .OrderByDescending(a => a.UpdatedDate)
                    .Select(a => new ClerkApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        ApplicantMobile = a.MobileNumber,
                        PositionType = a.PositionType.ToString(),
                        AssignedToClerkDate = a.AssignedToClerkDate,
                        SubmittedDate = a.SubmittedDate ?? DateTime.UtcNow,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    })
                    .ToListAsync();

                _logger.LogInformation("[ClerkWorkflow] Found {Count} completed applications for Clerk {ClerkId}", applications.Count, clerkId);

                return applications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error getting completed applications for Clerk {ClerkId}", clerkId);
                throw;
            }
        }
    }

    public class ClerkApplicationDto
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantMobile { get; set; } = string.Empty;
        public string PositionType { get; set; } = string.Empty;
        public string? AssignedAEName { get; set; }
        public DateTime? AssignedToClerkDate { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ClerkApplicationDetailDto : ClerkApplicationDto
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public string? JuniorEngineerName { get; set; }
        public string? CityEngineerName { get; set; }
        public DateTime? CityEngineerApprovalDate { get; set; }
    }

    public class ClerkActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }

    public class ClerkApproveRequest
    {
        public string? Remarks { get; set; }
    }

    public class ClerkRejectRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
