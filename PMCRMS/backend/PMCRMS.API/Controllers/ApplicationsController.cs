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
    public class ApplicationsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ApplicationsController> _logger;
        private readonly Services.IEmailService _emailService;
        private readonly Services.INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public ApplicationsController(
            PMCRMSDbContext context, 
            ILogger<ApplicationsController> logger,
            Services.IEmailService emailService,
            Services.INotificationService notificationService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetApplications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var query = _context.Applications.AsQueryable();

                // Filter by user role
                if (userRole == UserRole.User)
                {
                    query = query.Where(a => a.ApplicantId == userId);
                }

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
                    AssignedOfficerId = app.AssignedOfficerId,
                    AssignedOfficerName = app.AssignedOfficerName,
                    AssignedOfficerDesignation = app.AssignedOfficerDesignation,
                    AssignedDate = app.AssignedDate,
                    CreatedAt = app.CreatedDate,
                    UpdatedAt = app.UpdatedDate ?? app.CreatedDate,
                    CertificateIssuedDate = app.CertificateIssuedDate,
                    CertificateNumber = app.CertificateNumber
                }).ToList();

                // Return paginated response format expected by frontend
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Applications retrieved successfully. Total: {totalCount}, Page: {page}/{(int)Math.Ceiling((double)totalCount / pageSize)}",
                    Data = new
                    {
                        data = applicationDtos,
                        page = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ApplicationDto>>> GetApplication(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Documents)
                    .Include(a => a.StatusHistory)
                    .Include(a => a.Comments)
                    .FirstOrDefaultAsync(a => a.Id == id);

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

                var applicationDto = new ApplicationDto
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    ApplicantId = application.ApplicantId,
                    ApplicantName = application.Applicant?.Name ?? "Unknown",
                    ApplicationType = application.Type.ToString(),
                    ProjectTitle = application.ProjectTitle,
                    ProjectDescription = application.ProjectDescription,
                    SiteAddress = application.SiteAddress,
                    PlotArea = application.PlotArea,
                    BuiltUpArea = application.BuiltUpArea,
                    EstimatedCost = application.EstimatedCost,
                    CurrentStatus = application.CurrentStatus.ToString(),
                    AssignedOfficerId = application.AssignedOfficerId,
                    AssignedOfficerName = application.AssignedOfficerName,
                    AssignedOfficerDesignation = application.AssignedOfficerDesignation,
                    AssignedDate = application.AssignedDate,
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate,
                    CertificateIssuedDate = application.CertificateIssuedDate,
                    CertificateNumber = application.CertificateNumber,
                    Documents = application.Documents.Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.Type.ToString(),
                        FileSize = d.FileSize,
                        UploadedAt = d.CreatedDate
                    }).ToList(),
                    StatusHistory = application.StatusHistory.Select(s => new StatusHistoryDto
                    {
                        Status = s.Status.ToString(),
                        Comments = s.Remarks ?? "",
                        CreatedAt = s.StatusDate,
                        CreatedBy = s.UpdatedByUserId.ToString()
                    }).OrderByDescending(s => s.CreatedAt).ToList()
                };

                return Ok(new ApiResponse<ApplicationDto>
                {
                    Success = true,
                    Message = "Application retrieved successfully",
                    Data = applicationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve application",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ApplicationDto>>> CreateApplication([FromBody] CreateApplicationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Generate application number
                var count = await _context.Applications.CountAsync() + 1;
                var applicationNumber = $"PMCRMS{DateTime.Now.Year}{count:D6}";

                var application = new Application
                {
                    ApplicationNumber = applicationNumber,
                    Type = Enum.Parse<ApplicationType>(request.ApplicationType),
                    ApplicantId = userId,
                    ProjectTitle = request.ProjectTitle,
                    ProjectDescription = request.ProjectDescription,
                    SiteAddress = request.SiteAddress,
                    PlotArea = request.PlotArea,
                    BuiltUpArea = request.BuiltUpArea,
                    EstimatedCost = request.EstimatedCost,
                    CurrentStatus = ApplicationCurrentStatus.Draft,
                    CreatedBy = userId.ToString()
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                // Add initial status
                var initialStatus = new ApplicationStatus
                {
                    ApplicationId = application.Id,
                    Status = ApplicationCurrentStatus.Draft,
                    UpdatedByUserId = userId,
                    Remarks = "Application created",
                    StatusDate = DateTime.UtcNow,
                    CreatedBy = userId.ToString()
                };

                _context.ApplicationStatuses.Add(initialStatus);
                await _context.SaveChangesAsync();

                // Reload with includes
                application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == application.Id);

                var applicationDto = new ApplicationDto
                {
                    Id = application!.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    ApplicantId = application.ApplicantId,
                    ApplicantName = application.Applicant?.Name ?? "Unknown",
                    ApplicationType = application.Type.ToString(),
                    ProjectTitle = application.ProjectTitle,
                    ProjectDescription = application.ProjectDescription,
                    SiteAddress = application.SiteAddress,
                    PlotArea = application.PlotArea,
                    BuiltUpArea = application.BuiltUpArea,
                    EstimatedCost = application.EstimatedCost,
                    CurrentStatus = application.CurrentStatus.ToString(),
                    AssignedOfficerId = application.AssignedOfficerId,
                    AssignedOfficerName = application.AssignedOfficerName,
                    AssignedOfficerDesignation = application.AssignedOfficerDesignation,
                    AssignedDate = application.AssignedDate,
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate
                };

                _logger.LogInformation("Application {ApplicationId} created by user {UserId}", application.Id, userId);

                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, new ApiResponse<ApplicationDto>
                {
                    Success = true,
                    Message = "Application created successfully",
                    Data = applicationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to create application",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("my-applications")]
        public async Task<ActionResult<ApiResponse<object>>> GetMyApplications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var userId = GetCurrentUserId();

                var query = _context.Applications
                    .Where(a => a.ApplicantId == userId);

                var totalCount = await query.CountAsync();
                var applications = await query
                    .Include(a => a.Applicant)
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var applicationDtos = applications.Select(app => new
                {
                    id = app.Id,
                    applicationNumber = app.ApplicationNumber,
                    positionType = 0, // Default to Architect, you can map this based on your application type
                    applicantName = app.Applicant?.Name ?? "Unknown",
                    submissionDate = app.CreatedDate.ToString("yyyy-MM-dd"),
                    stage = MapStatusToStage(app.CurrentStatus),
                    status = app.CurrentStatus.ToString()
                }).ToList();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "My applications retrieved successfully",
                    Data = new
                    {
                        items = applicationDtos,
                        totalCount = totalCount,
                        page = page,
                        pageSize = pageSize,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving my applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update application status and assign officer
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<ApiResponse>> UpdateApplicationStatus(
            int id,
            [FromBody] UpdateApplicationStatusRequest request)
        {
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                var userId = GetCurrentUserId();
                var currentUserName = User.FindFirst("name")?.Value ?? "System";
                var currentUserRole = GetCurrentUserRole().ToString();

                // Parse the new status
                if (!Enum.TryParse<ApplicationCurrentStatus>(request.Status, out var newStatus))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status value"
                    });
                }

                // Update application status
                var oldStatus = application.CurrentStatus;
                application.CurrentStatus = newStatus;
                application.Remarks = request.Remarks;

                // Update assigned officer if provided
                if (request.AssignedOfficerId.HasValue)
                {
                    var assignedOfficer = await _context.Users.FindAsync(request.AssignedOfficerId.Value);
                    if (assignedOfficer != null)
                    {
                        application.AssignedOfficerId = request.AssignedOfficerId;
                        application.AssignedOfficerName = assignedOfficer.Name;
                        application.AssignedOfficerDesignation = assignedOfficer.Role.ToString();
                        application.AssignedDate = DateTime.UtcNow;

                        // Notify the assigned officer
                        await _notificationService.NotifyOfficerAssignmentAsync(
                            assignedOfficer.Id,
                            application.ApplicationNumber,
                            application.Id,
                            application.Type.ToString(),
                            application.Applicant.Name,
                            currentUserName
                        );
                    }
                }

                // Add status history record
                var statusHistory = new ApplicationStatus
                {
                    ApplicationId = application.Id,
                    Status = newStatus,
                    UpdatedByUserId = userId,
                    Remarks = request.Remarks ?? string.Empty,
                    StatusDate = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };

                _context.ApplicationStatuses.Add(statusHistory);
                await _context.SaveChangesAsync();

                // Send appropriate notifications based on status
                var assignedTo = application.AssignedOfficerName ?? "Pending Assignment";
                var assignedRole = application.AssignedOfficerDesignation ?? "";

                if (newStatus.ToString().Contains("Approved"))
                {
                    await _notificationService.NotifyApplicationApprovalAsync(
                        application.ApplicantId,
                        application.ApplicationNumber,
                        application.Id,
                        currentUserName,
                        currentUserRole,
                        request.Remarks ?? "Your application has been approved"
                    );
                }
                else if (newStatus.ToString().Contains("Rejected"))
                {
                    await _notificationService.NotifyApplicationRejectionAsync(
                        application.ApplicantId,
                        application.ApplicationNumber,
                        application.Id,
                        currentUserName,
                        currentUserRole,
                        request.Remarks ?? "Your application requires revision"
                    );
                }
                else
                {
                    await _notificationService.NotifyApplicationStatusChangeAsync(
                        application.ApplicantId,
                        application.ApplicationNumber,
                        application.Id,
                        newStatus.ToString(),
                        assignedTo,
                        assignedRole,
                        request.Remarks ?? "",
                        currentUserName,
                        currentUserRole
                    );
                }

                _logger.LogInformation(
                    "Application {ApplicationNumber} status updated from {OldStatus} to {NewStatus} by {User}",
                    application.ApplicationNumber, oldStatus, newStatus, currentUserName);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application status updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status for application {Id}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error updating application status",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Assign application to an officer
        /// </summary>
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<ApiResponse>> AssignOfficer(
            int id,
            [FromBody] AssignOfficerRequest request)
        {
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                var assignedOfficer = await _context.Users.FindAsync(request.OfficerId);
                if (assignedOfficer == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                var currentUserName = User.FindFirst("name")?.Value ?? "System";

                application.AssignedOfficerId = request.OfficerId;
                application.AssignedOfficerName = assignedOfficer.Name;
                application.AssignedOfficerDesignation = assignedOfficer.Role.ToString();
                application.AssignedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notify the assigned officer
                await _notificationService.NotifyOfficerAssignmentAsync(
                    assignedOfficer.Id,
                    application.ApplicationNumber,
                    application.Id,
                    application.Type.ToString(),
                    application.Applicant.Name,
                    currentUserName
                );

                // Notify the applicant
                await _notificationService.NotifyApplicationStatusChangeAsync(
                    application.ApplicantId,
                    application.ApplicationNumber,
                    application.Id,
                    application.CurrentStatus.ToString(),
                    assignedOfficer.Name,
                    assignedOfficer.Role.ToString(),
                    $"Your application has been assigned to {assignedOfficer.Name} for review.",
                    currentUserName,
                    GetCurrentUserRole().ToString()
                );

                _logger.LogInformation(
                    "Application {ApplicationNumber} assigned to officer {OfficerName} by {AssignedBy}",
                    application.ApplicationNumber, assignedOfficer.Name, currentUserName);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Application assigned to {assignedOfficer.Name} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning officer for application {Id}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error assigning officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboardAnalytics()
        {
            try
            {
                var userId = GetCurrentUserId();

                var applications = await _context.Applications
                    .Where(a => a.ApplicantId == userId)
                    .ToListAsync();

                var totalApplications = applications.Count;
                var approvedApplications = applications.Count(a => 
                    a.CurrentStatus == ApplicationCurrentStatus.CertificateIssued ||
                    a.CurrentStatus == ApplicationCurrentStatus.Completed);
                var rejectedApplications = applications.Count(a => 
                    a.CurrentStatus == ApplicationCurrentStatus.RejectedByJE ||
                    a.CurrentStatus == ApplicationCurrentStatus.RejectedByAE ||
                    a.CurrentStatus == ApplicationCurrentStatus.RejectedByEE1 ||
                    a.CurrentStatus == ApplicationCurrentStatus.RejectedByCE1);
                var pendingApplications = totalApplications - approvedApplications - rejectedApplications;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Analytics retrieved successfully",
                    Data = new
                    {
                        totalApplications,
                        approvedApplications,
                        pendingApplications,
                        rejectedApplications
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve analytics",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private int MapStatusToStage(ApplicationCurrentStatus status)
        {
            // Map ApplicationCurrentStatus to ApplicationStage enum (0-10)
            return status switch
            {
                ApplicationCurrentStatus.Draft => 0,
                ApplicationCurrentStatus.Submitted => 0, // JUNIOR_ENGINEER_PENDING
                ApplicationCurrentStatus.UnderReviewByJE => 1, // DOCUMENT_VERIFICATION_PENDING
                ApplicationCurrentStatus.ApprovedByJE => 2, // ASSISTANT_ENGINEER_PENDING
                ApplicationCurrentStatus.RejectedByJE => 10, // REJECTED
                ApplicationCurrentStatus.UnderReviewByAE => 2, // ASSISTANT_ENGINEER_PENDING
                ApplicationCurrentStatus.ApprovedByAE => 3, // EXECUTIVE_ENGINEER_PENDING
                ApplicationCurrentStatus.RejectedByAE => 10, // REJECTED
                ApplicationCurrentStatus.UnderReviewByEE1 => 3, // EXECUTIVE_ENGINEER_PENDING
                ApplicationCurrentStatus.ApprovedByEE1 => 4, // CITY_ENGINEER_PENDING
                ApplicationCurrentStatus.RejectedByEE1 => 10, // REJECTED
                ApplicationCurrentStatus.UnderReviewByCE1 => 4, // CITY_ENGINEER_PENDING
                ApplicationCurrentStatus.ApprovedByCE1 => 5, // PAYMENT_PENDING
                ApplicationCurrentStatus.RejectedByCE1 => 10, // REJECTED
                ApplicationCurrentStatus.PaymentPending => 5, // PAYMENT_PENDING
                ApplicationCurrentStatus.PaymentCompleted => 6, // CLERK_PENDING
                ApplicationCurrentStatus.UnderProcessingByClerk => 6, // CLERK_PENDING
                ApplicationCurrentStatus.ProcessedByClerk => 7, // EXECUTIVE_ENGINEER_SIGN_PENDING
                ApplicationCurrentStatus.UnderDigitalSignatureByEE2 => 7, // EXECUTIVE_ENGINEER_SIGN_PENDING
                ApplicationCurrentStatus.DigitalSignatureCompletedByEE2 => 8, // CITY_ENGINEER_SIGN_PENDING
                ApplicationCurrentStatus.UnderFinalApprovalByCE2 => 8, // CITY_ENGINEER_SIGN_PENDING
                ApplicationCurrentStatus.CertificateIssued => 9, // APPROVED
                ApplicationCurrentStatus.Completed => 9, // APPROVED
                _ => 0
            };
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
    }
}
