using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/assistant-engineer")]
    [Authorize]
    public class AssistantEngineerController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AssistantEngineerController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowRoutingService _workflowRoutingService;

        public AssistantEngineerController(
            PMCRMSDbContext context,
            ILogger<AssistantEngineerController> logger,
            INotificationService notificationService,
            IWorkflowRoutingService workflowRoutingService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _workflowRoutingService = workflowRoutingService;
        }

        /// <summary>
        /// Get applications assigned to current Assistant Engineer
        /// </summary>
        [HttpGet("applications")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetAssignedApplications(
            [FromQuery] ApplicationCurrentStatus? status = null)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var query = _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.Documents)
                    .Where(a => a.AssignedOfficerId == officerId);

                if (status.HasValue)
                {
                    query = query.Where(a => a.Status == status.Value);
                }

                var applications = await query
                    .OrderByDescending(a => a.SubmittedDate)
                    .Select(a => new
                    {
                        id = a.Id,
                        applicationNumber = a.ApplicationNumber,
                        applicantName = $"{a.FirstName} {a.LastName}",
                        positionType = a.PositionType.ToString(),
                        submittedDate = a.SubmittedDate,
                        status = a.Status.ToString(),
                        email = a.EmailAddress,
                        mobileNumber = a.MobileNumber
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Applications retrieved successfully",
                    Data = applications.Cast<object>().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications for Assistant Engineer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Approve application and forward to Executive Engineer
        /// </summary>
        [HttpPost("applications/{id}/approve")]
        public async Task<ActionResult<ApiResponse>> ApproveApplication(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.ApprovedByAE);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status transition"
                    });
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.ApprovedByAE;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Get and assign Executive Engineer
                var executiveEngineer = await _workflowRoutingService.GetExecutiveEngineer();
                if (executiveEngineer != null)
                {
                    application.AssignedOfficerId = executiveEngineer.Id;
                    application.AssignedOfficerName = executiveEngineer.Name;
                    application.AssignedOfficerRole = executiveEngineer.Role.ToString();
                    application.AssignedDate = DateTime.UtcNow;

                    // Change status to under review by EE
                    application.Status = ApplicationCurrentStatus.UnderReviewByEE1;
                }

                await _context.SaveChangesAsync();

                // Send notification to applicant
                await _notificationService.NotifyApplicationApprovalAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Assistant Engineer",
                    "Assistant Engineer",
                    request.Remarks ?? "Your application has been approved and forwarded to Executive Engineer"
                );

                _logger.LogInformation("Application {ApplicationNumber} approved by Assistant Engineer {OfficerId}",
                    application.ApplicationNumber, officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application approved successfully and forwarded to Executive Engineer"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to approve application",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Reject application and send back to applicant
        /// </summary>
        [HttpPost("applications/{id}/reject")]
        public async Task<ActionResult<ApiResponse>> RejectApplication(
            int id,
            [FromBody] RejectApplicationRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Rejection reason is required"
                    });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.RejectedByAE);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status transition"
                    });
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.RejectedByAE;
                application.Remarks = request.RejectionReason;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                // Send notification to applicant
                await _notificationService.NotifyApplicationRejectionAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Assistant Engineer",
                    "Assistant Engineer",
                    request.RejectionReason
                );

                _logger.LogInformation("Application {ApplicationNumber} rejected by Assistant Engineer {OfficerId}",
                    application.ApplicationNumber, officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application rejected. Applicant has been notified."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to reject application",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private int GetCurrentOfficerId()
        {
            var officerIdClaim = HttpContext.User.FindFirst("officer_id") ?? 
                                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(officerIdClaim?.Value ?? "0");
        }
    }
}
