using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatusController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<StatusController> _logger;
        private readonly Services.IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public StatusController(
            PMCRMSDbContext context, 
            ILogger<StatusController> logger,
            Services.IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("update/{applicationId}")]
        [Authorize(Roles = "JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer,Clerk,Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateApplicationStatus(
            int applicationId, 
            [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Parse and validate new status
                if (!Enum.TryParse<ApplicationCurrentStatus>(request.NewStatus, true, out var newStatus))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status",
                        Errors = new List<string> { $"Valid statuses: {string.Join(", ", Enum.GetNames<ApplicationCurrentStatus>())}" }
                    });
                }

                // Validate status transition
                var validTransition = IsValidStatusTransition(application.CurrentStatus, newStatus, userRole);
                if (!validTransition.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = validTransition.ErrorMessage,
                        Errors = new List<string> { "Invalid status transition" }
                    });
                }

                // Update application status
                application.CurrentStatus = newStatus;
                application.UpdatedBy = userId.ToString();
                application.UpdatedDate = DateTime.UtcNow;

                // Add status history
                var statusHistory = new ApplicationStatus
                {
                    ApplicationId = applicationId,
                    Status = newStatus,
                    UpdatedByUserId = userId,
                    Remarks = request.Remarks,
                    RejectionReason = request.RejectionReason,
                    StatusDate = DateTime.UtcNow,
                    CreatedBy = userId.ToString()
                };

                _context.ApplicationStatuses.Add(statusHistory);

                // Handle special status changes
                await HandleSpecialStatusChange(application, newStatus, request);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} status updated from {OldStatus} to {NewStatus} by user {UserId}", 
                    applicationId, application.CurrentStatus, newStatus, userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Application status updated to {newStatus}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for application {ApplicationId}", applicationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update application status",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("workflow/{applicationType}")]
        public ActionResult<ApiResponse<WorkflowDto>> GetWorkflow(string applicationType)
        {
            try
            {
                if (!Enum.TryParse<ApplicationType>(applicationType, true, out var appType))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid application type"
                    });
                }

                var workflow = GetWorkflowSteps(appType);

                return Ok(new ApiResponse<WorkflowDto>
                {
                    Success = true,
                    Message = "Workflow retrieved successfully",
                    Data = workflow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow for application type {ApplicationType}", applicationType);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve workflow",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("history/{applicationId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StatusHistoryDto>>>> GetStatusHistory(int applicationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Check permissions
                if (userRole == UserRole.User && application.ApplicantId != userId)
                {
                    return Forbid();
                }

                var statusHistory = await _context.ApplicationStatuses
                    .Where(s => s.ApplicationId == applicationId)
                    .Include(s => s.UpdatedByUser)
                    .OrderByDescending(s => s.StatusDate)
                    .Select(s => new StatusHistoryDto
                    {
                        Status = s.Status.ToString(),
                        Comments = s.Remarks ?? "",
                        CreatedAt = s.StatusDate,
                        CreatedBy = s.UpdatedByUser.Name ?? s.UpdatedByUserId.ToString()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<StatusHistoryDto>>
                {
                    Success = true,
                    Message = "Status history retrieved successfully",
                    Data = statusHistory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status history for application {ApplicationId}", applicationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve status history",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer,Clerk,Admin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApplicationDto>>>> GetPendingApplications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();

                // Get applications pending for current user role
                var pendingStatuses = GetPendingStatusesForRole(userRole);

                var query = _context.Applications
                    .Where(a => pendingStatuses.Contains(a.CurrentStatus));

                var totalCount = await query.CountAsync();
                var applications = await query
                    .Include(a => a.Applicant)
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var applicationDtos = applications.Select(app => new ApplicationDto
                {
                    Id = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    ApplicantId = app.ApplicantId,
                    ApplicantName = app.Applicant?.Name ?? "Unknown",
                    ApplicationType = app.Type.ToString(),
                    ProjectTitle = app.ProjectTitle,
                    ProjectDescription = app.ProjectDescription,
                    SiteAddress = app.SiteAddress,
                    PlotArea = app.PlotArea,
                    BuiltUpArea = app.BuiltUpArea,
                    EstimatedCost = app.EstimatedCost,
                    CurrentStatus = app.CurrentStatus.ToString(),
                    CreatedAt = app.CreatedDate,
                    UpdatedAt = app.UpdatedDate ?? app.CreatedDate
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<ApplicationDto>>
                {
                    Success = true,
                    Message = $"Pending applications retrieved successfully. Total: {totalCount}",
                    Data = applicationDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve pending applications",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst("user_id") ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = HttpContext.User.FindFirst("role") ?? HttpContext.User.FindFirst(ClaimTypes.Role);
            return Enum.Parse<UserRole>(roleClaim?.Value ?? "Applicant");
        }

        private (bool IsValid, string ErrorMessage) IsValidStatusTransition(
            ApplicationCurrentStatus currentStatus, 
            ApplicationCurrentStatus newStatus, 
            UserRole userRole)
        {
            // Define valid transitions based on current status and user role
            // Simplified for now - Admin, Executive Engineer and City Engineer can perform any transition
            var validTransitions = new Dictionary<ApplicationCurrentStatus, Dictionary<UserRole, ApplicationCurrentStatus[]>>
            {
                [ApplicationCurrentStatus.Submitted] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.CityEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                },
                [ApplicationCurrentStatus.UnderReviewByJE] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                },
                [ApplicationCurrentStatus.ApprovedByJE] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                },
                [ApplicationCurrentStatus.UnderReviewByAE] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                },
                [ApplicationCurrentStatus.ApprovedByAE] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = Enum.GetValues<ApplicationCurrentStatus>(),
                },
                [ApplicationCurrentStatus.UnderReviewByEE1] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = new[] { ApplicationCurrentStatus.ApprovedByEE1, ApplicationCurrentStatus.RejectedByEE1 }
                },
                [ApplicationCurrentStatus.ApprovedByEE1] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.CityEngineer] = new[] { ApplicationCurrentStatus.UnderReviewByCE1 }
                },
                [ApplicationCurrentStatus.UnderReviewByCE1] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.CityEngineer] = new[] { ApplicationCurrentStatus.ApprovedByCE1, ApplicationCurrentStatus.RejectedByCE1 }
                },
                [ApplicationCurrentStatus.ApprovedByCE1] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.Clerk] = new[] { ApplicationCurrentStatus.PaymentPending }
                },
                [ApplicationCurrentStatus.PaymentCompleted] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.Clerk] = new[] { ApplicationCurrentStatus.UnderProcessingByClerk }
                },
                [ApplicationCurrentStatus.UnderProcessingByClerk] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.Clerk] = new[] { ApplicationCurrentStatus.ProcessedByClerk }
                },
                [ApplicationCurrentStatus.ProcessedByClerk] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = new[] { ApplicationCurrentStatus.UnderDigitalSignatureByEE2 }
                },
                [ApplicationCurrentStatus.UnderDigitalSignatureByEE2] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.ExecutiveEngineer] = new[] { ApplicationCurrentStatus.DigitalSignatureCompletedByEE2 }
                },
                [ApplicationCurrentStatus.DigitalSignatureCompletedByEE2] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.CityEngineer] = new[] { ApplicationCurrentStatus.UnderFinalApprovalByCE2 }
                },
                [ApplicationCurrentStatus.UnderFinalApprovalByCE2] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.CityEngineer] = new[] { ApplicationCurrentStatus.CertificateIssued }
                },
                [ApplicationCurrentStatus.CertificateIssued] = new()
                {
                    [UserRole.Admin] = Enum.GetValues<ApplicationCurrentStatus>(),
                    [UserRole.Clerk] = new[] { ApplicationCurrentStatus.Completed }
                }
            };

            // Admins can make any transition
            if (userRole == UserRole.Admin)
            {
                return (true, "");
            }

            if (!validTransitions.ContainsKey(currentStatus))
            {
                return (false, "No valid transitions available from current status");
            }

            if (!validTransitions[currentStatus].ContainsKey(userRole))
            {
                return (false, "You don't have permission to update this application status");
            }

            if (!validTransitions[currentStatus][userRole].Contains(newStatus))
            {
                return (false, $"Invalid transition from {currentStatus} to {newStatus}");
            }

            return (true, "");
        }

        private async Task HandleSpecialStatusChange(Application application, ApplicationCurrentStatus newStatus, UpdateStatusRequest request)
        {
            switch (newStatus)
            {
                case ApplicationCurrentStatus.Submitted:
                    // Send confirmation email when application is submitted
                    try
                    {
                        var applicant = await _context.Users.FindAsync(application.ApplicantId);
                        if (applicant != null)
                        {
                            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? _configuration["CorsSettings:AllowedOrigins:0"] ?? throw new InvalidOperationException("Frontend URL not configured");
                            var viewUrl = $"{frontendUrl}/applications/{application.Id}";

                            await _emailService.SendApplicationSubmissionEmailAsync(
                                applicant.Email,
                                applicant.Name,
                                application.ApplicationNumber,
                                application.Type.ToString(),
                                application.Id.ToString(),
                                viewUrl
                            );

                            _logger.LogInformation("Submission email sent to {Email} for application {ApplicationNumber}",
                                applicant.Email, application.ApplicationNumber);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send submission email for application {ApplicationNumber}",
                            application.ApplicationNumber);
                        // Don't fail the request if email fails
                    }
                    break;

                case ApplicationCurrentStatus.PaymentPending:
                    // Calculate fee amount based on application type and area
                    application.FeeAmount = CalculateFeeAmount(application.Type, application.BuiltUpArea);
                    application.PaymentDueDate = DateTime.UtcNow.AddDays(30); // 30 days to pay
                    break;

                case ApplicationCurrentStatus.CertificateIssued:
                    // Generate certificate number
                    application.CertificateNumber = GenerateCertificateNumber(application.Type, application.Id);
                    application.CertificateIssuedDate = DateTime.UtcNow;
                    break;

                case ApplicationCurrentStatus.RejectedByJE:
                case ApplicationCurrentStatus.RejectedByAE:
                case ApplicationCurrentStatus.RejectedByEE1:
                case ApplicationCurrentStatus.RejectedByCE1:
                    // Ensure rejection reason is provided
                    if (string.IsNullOrEmpty(request.RejectionReason))
                    {
                        throw new ArgumentException("Rejection reason is required for rejected status");
                    }
                    break;
            }
        }

        private decimal CalculateFeeAmount(ApplicationType applicationType, decimal builtUpArea)
        {
            // Fee calculation logic based on application type and area
            var baseRate = applicationType switch
            {
                ApplicationType.BuildingPermit => 50m, // ₹50 per sq ft
                ApplicationType.OccupancyCertificate => 30m, // ₹30 per sq ft
                ApplicationType.CompletionCertificate => 25m, // ₹25 per sq ft
                ApplicationType.DemolitionPermit => 20m, // ₹20 per sq ft
                _ => 25m
            };

            return baseRate * builtUpArea;
        }

        private string GenerateCertificateNumber(ApplicationType applicationType, int applicationId)
        {
            var prefix = applicationType switch
            {
                ApplicationType.BuildingPermit => "BP",
                ApplicationType.OccupancyCertificate => "OC",
                ApplicationType.CompletionCertificate => "CC",
                ApplicationType.DemolitionPermit => "DP",
                _ => "GC"
            };

            return $"{prefix}{DateTime.Now.Year}{applicationId:D6}";
        }

        private WorkflowDto GetWorkflowSteps(ApplicationType applicationType)
        {
            // Standard workflow steps for all application types
            var steps = new List<WorkflowStepDto>
            {
                new() { Step = 1, Status = "Draft", Description = "Application being prepared", Role = "Applicant" },
                new() { Step = 2, Status = "Submitted", Description = "Application submitted for review", Role = "Applicant" },
                new() { Step = 3, Status = "UnderReviewByJE", Description = "Under review by Junior Engineer", Role = "JuniorEngineer" },
                new() { Step = 4, Status = "ApprovedByJE", Description = "Approved by Junior Engineer", Role = "JuniorEngineer" },
                new() { Step = 5, Status = "UnderReviewByAE", Description = "Under review by Assistant Engineer", Role = "AssistantEngineer" },
                new() { Step = 6, Status = "ApprovedByAE", Description = "Approved by Assistant Engineer", Role = "AssistantEngineer" },
                new() { Step = 7, Status = "UnderReviewByEE1", Description = "Under review by Executive Engineer", Role = "ExecutiveEngineer" },
                new() { Step = 8, Status = "ApprovedByEE1", Description = "Approved by Executive Engineer", Role = "ExecutiveEngineer" },
                new() { Step = 9, Status = "UnderReviewByCE1", Description = "Under review by City Engineer", Role = "CityEngineer" },
                new() { Step = 10, Status = "ApprovedByCE1", Description = "Approved by City Engineer", Role = "CityEngineer" },
                new() { Step = 11, Status = "PaymentPending", Description = "Payment pending", Role = "Applicant" },
                new() { Step = 12, Status = "PaymentCompleted", Description = "Payment completed", Role = "System" },
                new() { Step = 13, Status = "UnderProcessingByClerk", Description = "Under processing by Clerk", Role = "Clerk" },
                new() { Step = 14, Status = "ProcessedByClerk", Description = "Processed by Clerk", Role = "Clerk" },
                new() { Step = 15, Status = "UnderDigitalSignatureByEE2", Description = "Under digital signature", Role = "ExecutiveEngineer" },
                new() { Step = 16, Status = "DigitalSignatureCompletedByEE2", Description = "Digital signature completed", Role = "ExecutiveEngineer" },
                new() { Step = 17, Status = "UnderFinalApprovalByCE2", Description = "Under final approval", Role = "CityEngineer" },
                new() { Step = 18, Status = "CertificateIssued", Description = "Certificate issued", Role = "CityEngineer" },
                new() { Step = 19, Status = "Completed", Description = "Process completed", Role = "System" }
            };

            return new WorkflowDto
            {
                ApplicationType = applicationType.ToString(),
                Steps = steps
            };
        }

        private ApplicationCurrentStatus[] GetPendingStatusesForRole(UserRole userRole)
        {
            return userRole switch
            {
                UserRole.ExecutiveEngineer => new[]
                {
                    ApplicationCurrentStatus.UnderReviewByEE1,
                    ApplicationCurrentStatus.UnderDigitalSignatureByEE2
                },
                UserRole.CityEngineer => new[]
                {
                    ApplicationCurrentStatus.UnderReviewByCE1,
                    ApplicationCurrentStatus.UnderFinalApprovalByCE2
                },
                UserRole.Clerk => new[]
                {
                    ApplicationCurrentStatus.UnderProcessingByClerk
                },
                UserRole.Admin => Enum.GetValues<ApplicationCurrentStatus>()
                    .Where(s => s != ApplicationCurrentStatus.Draft && s != ApplicationCurrentStatus.Completed)
                    .ToArray(),
                _ => new ApplicationCurrentStatus[0]
            };
        }
    }
}
