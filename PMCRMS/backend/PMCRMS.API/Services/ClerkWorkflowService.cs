using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for Clerk workflow operations
    /// Handles post-payment application processing
    /// Status progression: PaymentCompleted (16) → ProcessedByClerk (18) → forwards to EE Stage 2
    /// </summary>
    public class ClerkWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ClerkWorkflowService> _logger;
        private readonly IEmailService _emailService;

        public ClerkWorkflowService(
            PMCRMSDbContext context,
            ILogger<ClerkWorkflowService> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Get all pending applications for clerk review (PaymentCompleted status)
        /// </summary>
        public async Task<List<ClerkApplicationDto>> GetPendingApplicationsAsync()
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Getting pending applications (PaymentCompleted status)");

                var applications = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Transactions)
                    .Where(a => a.CurrentStatus == ApplicationCurrentStatus.PaymentCompleted)
                    .OrderBy(a => a.PaymentCompletedDate)
                    .Select(a => new ClerkApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber,
                        ApplicantName = a.Applicant.Name,
                        ApplicantEmail = a.Applicant.Email,
                        ApplicantMobile = a.Applicant.PhoneNumber ?? "",
                        ApplicationType = a.Type.ToString(),
                        PropertyAddress = a.SiteAddress ?? "",
                        PaymentCompletedDate = a.PaymentCompletedDate,
                        IsPaymentComplete = a.IsPaymentComplete,
                        PaymentAmount = a.Transactions
                            .Where(t => t.Status == "SUCCESS")
                            .OrderByDescending(t => t.CreatedAt)
                            .Select(t => t.Price)
                            .FirstOrDefault(),
                        SubmittedDate = a.CreatedDate,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    })
                    .ToListAsync();

                _logger.LogInformation($"[ClerkWorkflow] Found {applications.Count} pending applications");

                return applications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error getting pending applications");
                throw;
            }
        }

        /// <summary>
        /// Get application details for clerk review
        /// </summary>
        public async Task<ClerkApplicationDetailDto?> GetApplicationDetailsAsync(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[ClerkWorkflow] Getting application details: {applicationId}");

                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Transactions)
                    .Include(a => a.StatusHistory)
                    .Where(a => a.Id == applicationId)
                    .Select(a => new ClerkApplicationDetailDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber,
                        ApplicantName = a.Applicant.Name,
                        ApplicantEmail = a.Applicant.Email,
                        ApplicantMobile = a.Applicant.PhoneNumber ?? "",
                        ApplicationType = a.Type.ToString(),
                        PropertyAddress = a.SiteAddress ?? "",
                        CurrentStatus = a.CurrentStatus.ToString(),
                        PaymentCompletedDate = a.PaymentCompletedDate,
                        IsPaymentComplete = a.IsPaymentComplete,
                        PaymentAmount = a.Transactions
                            .Where(t => t.Status == "SUCCESS")
                            .OrderByDescending(t => t.CreatedAt)
                            .Select(t => t.Price)
                            .FirstOrDefault(),
                        TransactionId = a.Transactions
                            .Where(t => t.Status == "SUCCESS")
                            .OrderByDescending(t => t.CreatedAt)
                            .Select(t => t.TransactionId)
                            .FirstOrDefault(),
                        BdOrderId = a.Transactions
                            .Where(t => t.Status == "SUCCESS")
                            .OrderByDescending(t => t.CreatedAt)
                            .Select(t => t.BdOrderId)
                            .FirstOrDefault(),
                        StatusHistoryCount = a.StatusHistory.Count,
                        SubmittedDate = a.CreatedDate,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    })
                    .FirstOrDefaultAsync();

                return application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkWorkflow] Error getting application details: {applicationId}");
                throw;
            }
        }

        /// <summary>
        /// Approve application and forward to EE Stage 2
        /// Updates status from PaymentCompleted (16) to ProcessedByClerk (18)
        /// </summary>
        public async Task<ClerkActionResult> ApproveApplicationAsync(int applicationId, string remarks, int clerkUserId)
        {
            try
            {
                _logger.LogInformation($"[ClerkWorkflow] Approving application {applicationId} by user {clerkUserId}");

                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.CurrentStatus != ApplicationCurrentStatus.PaymentCompleted)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = $"Application is not in PaymentCompleted status. Current status: {application.CurrentStatus}"
                    };
                }

                // Update application status
                application.CurrentStatus = ApplicationCurrentStatus.ProcessedByClerk;
                application.UpdatedDate = DateTime.UtcNow;

                // Add status history entry
                var statusEntry = new ApplicationStatus
                {
                    ApplicationId = applicationId,
                    Status = ApplicationCurrentStatus.ProcessedByClerk,
                    UpdatedByUserId = clerkUserId,
                    UpdatedByOfficerId = 0, // Temporary: Set to 0 since we're using UserId
                    Remarks = remarks ?? "Processed by Clerk - Forwarded to Executive Engineer (Stage 2)",
                    StatusDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ApplicationStatuses.Add(statusEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[ClerkWorkflow] Application {applicationId} approved successfully");

                // Send email notification (non-blocking)
                try
                {
                    var viewUrl = $"{GetBaseUrl()}/view-application/{applicationId}";
                    await _emailService.SendClerkApprovalEmailAsync(
                        application.Applicant.Email,
                        application.Applicant.Name,
                        application.ApplicationNumber,
                        remarks ?? "",
                        viewUrl
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"[ClerkWorkflow] Failed to send approval email for application {applicationId}");
                }

                return new ClerkActionResult
                {
                    Success = true,
                    Message = "Application approved successfully and forwarded to Executive Engineer (Stage 2)",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.ProcessedByClerk.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkWorkflow] Error approving application {applicationId}");
                return new ClerkActionResult
                {
                    Success = false,
                    Message = $"Error approving application: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Reject application with reason
        /// </summary>
        public async Task<ClerkActionResult> RejectApplicationAsync(int applicationId, string rejectionReason, int clerkUserId)
        {
            try
            {
                _logger.LogInformation($"[ClerkWorkflow] Rejecting application {applicationId} by user {clerkUserId}");

                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = "Rejection reason is required"
                    };
                }

                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.CurrentStatus != ApplicationCurrentStatus.PaymentCompleted)
                {
                    return new ClerkActionResult
                    {
                        Success = false,
                        Message = $"Application is not in PaymentCompleted status. Current status: {application.CurrentStatus}"
                    };
                }

                // Update application status to rejected
                application.CurrentStatus = ApplicationCurrentStatus.RejectedByJE; // Reuse existing rejection status
                application.UpdatedDate = DateTime.UtcNow;

                // Add status history entry with rejection reason
                var statusEntry = new ApplicationStatus
                {
                    ApplicationId = applicationId,
                    Status = ApplicationCurrentStatus.RejectedByJE,
                    UpdatedByUserId = clerkUserId,
                    UpdatedByOfficerId = 0, // Temporary: Set to 0 since we're using UserId
                    Remarks = "RejectedByClerk",
                    RejectionReason = rejectionReason,
                    StatusDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ApplicationStatuses.Add(statusEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[ClerkWorkflow] Application {applicationId} rejected successfully");

                // Send rejection email (non-blocking)
                try
                {
                    var viewUrl = $"{GetBaseUrl()}/view-application/{applicationId}";
                    await _emailService.SendClerkRejectionEmailAsync(
                        application.Applicant.Email,
                        application.Applicant.Name,
                        application.ApplicationNumber,
                        rejectionReason,
                        viewUrl
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"[ClerkWorkflow] Failed to send rejection email for application {applicationId}");
                }

                return new ClerkActionResult
                {
                    Success = true,
                    Message = "Application rejected successfully",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.RejectedByJE.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkWorkflow] Error rejecting application {applicationId}");
                return new ClerkActionResult
                {
                    Success = false,
                    Message = $"Error rejecting application: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get completed applications processed by clerk
        /// </summary>
        public async Task<List<ClerkApplicationDto>> GetCompletedApplicationsAsync()
        {
            try
            {
                _logger.LogInformation("[ClerkWorkflow] Getting completed applications");

                var applications = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Transactions)
                    .Where(a =>
                        a.CurrentStatus == ApplicationCurrentStatus.ProcessedByClerk ||
                        a.CurrentStatus == ApplicationCurrentStatus.UnderDigitalSignatureByEE2 ||
                        a.CurrentStatus == ApplicationCurrentStatus.DigitalSignatureCompletedByEE2 ||
                        a.CurrentStatus == ApplicationCurrentStatus.UnderFinalApprovalByCE2 ||
                        a.CurrentStatus == ApplicationCurrentStatus.CertificateIssued)
                    .OrderByDescending(a => a.UpdatedDate)
                    .Select(a => new ClerkApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber,
                        ApplicantName = a.Applicant.Name,
                        ApplicantEmail = a.Applicant.Email,
                        ApplicantMobile = a.Applicant.PhoneNumber ?? "",
                        ApplicationType = a.Type.ToString(),
                        PropertyAddress = a.SiteAddress ?? "",
                        PaymentCompletedDate = a.PaymentCompletedDate,
                        IsPaymentComplete = a.IsPaymentComplete,
                        PaymentAmount = a.Transactions
                            .Where(t => t.Status == "SUCCESS")
                            .OrderByDescending(t => t.CreatedAt)
                            .Select(t => t.Price)
                            .FirstOrDefault(),
                        SubmittedDate = a.CreatedDate,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    })
                    .ToListAsync();

                _logger.LogInformation($"[ClerkWorkflow] Found {applications.Count} completed applications");

                return applications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkWorkflow] Error getting completed applications");
                throw;
            }
        }

        private string GetBaseUrl()
        {
            return Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
        }
    }

    // DTOs for Clerk Workflow
    public class ClerkApplicationDto
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantMobile { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public DateTime? PaymentCompletedDate { get; set; }
        public bool IsPaymentComplete { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ClerkApplicationDetailDto : ClerkApplicationDto
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? BdOrderId { get; set; }
        public int StatusHistoryCount { get; set; }
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
