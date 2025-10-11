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
    [Route("api/workflow")]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<WorkflowController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowRoutingService _workflowRoutingService;

        public WorkflowController(
            PMCRMSDbContext context,
            ILogger<WorkflowController> logger,
            INotificationService notificationService,
            IWorkflowRoutingService workflowRoutingService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _workflowRoutingService = workflowRoutingService;
        }

        /// <summary>
        /// Get applications by current officer role and status
        /// </summary>
        [HttpGet("applications")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetApplicationsByRole(
            [FromQuery] ApplicationCurrentStatus? status = null)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var query = _context.PositionApplications
                    .Include(a => a.User)
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
                        mobileNumber = a.MobileNumber,
                        assignedOfficerName = a.AssignedOfficerName
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
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Executive Engineer Stage 1 - Approve and forward to City Engineer
        /// </summary>
        [HttpPost("executive-engineer/applications/{id}/approve")]
        public async Task<ActionResult<ApiResponse>> ExecutiveEngineerApprove(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.ApprovedByEE1);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Invalid status transition" });
                }

                application.Status = ApplicationCurrentStatus.ApprovedByEE1;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Assign to City Engineer
                var cityEngineer = await _workflowRoutingService.GetCityEngineer();
                if (cityEngineer != null)
                {
                    application.AssignedOfficerId = cityEngineer.Id;
                    application.AssignedOfficerName = cityEngineer.Name;
                    application.AssignedOfficerRole = cityEngineer.Role.ToString();
                    application.AssignedDate = DateTime.UtcNow;
                    application.Status = ApplicationCurrentStatus.UnderReviewByCE1;
                }

                await _context.SaveChangesAsync();

                await _notificationService.NotifyApplicationApprovalAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Executive Engineer",
                    "Executive Engineer",
                    request.Remarks ?? "Approved and forwarded to City Engineer"
                );

                return Ok(new ApiResponse { Success = true, Message = "Application approved and forwarded to City Engineer" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Executive Engineer approval");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to approve application", Errors = new List<string> { ex.Message } });
            }
        }

        /// <summary>
        /// City Engineer Stage 1 - Approve and set to Payment Pending
        /// </summary>
        [HttpPost("city-engineer/applications/{id}/approve")]
        public async Task<ActionResult<ApiResponse>> CityEngineerApprove(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.ApprovedByCE1);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Invalid status transition" });
                }

                application.Status = ApplicationCurrentStatus.ApprovedByCE1;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Set to payment pending
                application.Status = ApplicationCurrentStatus.PaymentPending;

                await _context.SaveChangesAsync();

                await _notificationService.NotifyApplicationApprovalAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "City Engineer",
                    "City Engineer",
                    request.Remarks ?? "Application approved. Please proceed with payment."
                );

                return Ok(new ApiResponse { Success = true, Message = "Application approved. Payment is now pending." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in City Engineer approval");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to approve application", Errors = new List<string> { ex.Message } });
            }
        }

        /// <summary>
        /// Clerk - Process payment and forward to Executive Engineer Stage 2
        /// </summary>
        [HttpPost("clerk/applications/{id}/process")]
        public async Task<ActionResult<ApiResponse>> ClerkProcess(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                application.Status = ApplicationCurrentStatus.ProcessedByClerk;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Assign to Executive Engineer for digital signature
                var executiveEngineer = await _workflowRoutingService.GetExecutiveEngineer();
                if (executiveEngineer != null)
                {
                    application.AssignedOfficerId = executiveEngineer.Id;
                    application.AssignedOfficerName = executiveEngineer.Name;
                    application.AssignedOfficerRole = executiveEngineer.Role.ToString();
                    application.AssignedDate = DateTime.UtcNow;
                    application.Status = ApplicationCurrentStatus.UnderDigitalSignatureByEE2;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Application processed and forwarded for digital signature" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Clerk processing");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to process application", Errors = new List<string> { ex.Message } });
            }
        }

        /// <summary>
        /// Executive Engineer Stage 2 - Apply digital signature
        /// </summary>
        [HttpPost("executive-engineer/applications/{id}/sign")]
        public async Task<ActionResult<ApiResponse>> ExecutiveEngineerSign(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                application.Status = ApplicationCurrentStatus.DigitalSignatureCompletedByEE2;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Assign to City Engineer for final signature
                var cityEngineer = await _workflowRoutingService.GetCityEngineer();
                if (cityEngineer != null)
                {
                    application.AssignedOfficerId = cityEngineer.Id;
                    application.AssignedOfficerName = cityEngineer.Name;
                    application.AssignedOfficerRole = cityEngineer.Role.ToString();
                    application.AssignedDate = DateTime.UtcNow;
                    application.Status = ApplicationCurrentStatus.UnderFinalApprovalByCE2;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Digital signature applied. Forwarded to City Engineer for final approval." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying digital signature");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to apply digital signature", Errors = new List<string> { ex.Message } });
            }
        }

        /// <summary>
        /// City Engineer Stage 2 - Final approval and issue certificate
        /// </summary>
        [HttpPost("city-engineer/applications/{id}/finalize")]
        public async Task<ActionResult<ApiResponse>> CityEngineerFinalize(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                application.Status = ApplicationCurrentStatus.CertificateIssued;
                application.ApprovedDate = DateTime.UtcNow;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                await _notificationService.NotifyApplicationApprovalAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "City Engineer",
                    "City Engineer",
                    "Your certificate has been issued and is ready for download."
                );

                return Ok(new ApiResponse { Success = true, Message = "Certificate issued successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing application");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to finalize application", Errors = new List<string> { ex.Message } });
            }
        }

        /// <summary>
        /// Reject application at any stage
        /// </summary>
        [HttpPost("applications/{id}/reject")]
        public async Task<ActionResult<ApiResponse>> RejectApplication(
            int id,
            [FromBody] RejectApplicationRequest request)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Application not found" });
                }

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Rejection reason is required" });
                }

                // Determine rejection status based on current status
                var rejectionStatus = application.Status switch
                {
                    ApplicationCurrentStatus.UnderReviewByEE1 => ApplicationCurrentStatus.RejectedByEE1,
                    ApplicationCurrentStatus.UnderReviewByCE1 => ApplicationCurrentStatus.RejectedByCE1,
                    _ => ApplicationCurrentStatus.RejectedByJE
                };

                application.Status = rejectionStatus;
                application.Remarks = request.RejectionReason;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                await _notificationService.NotifyApplicationRejectionAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Officer",
                    GetCurrentOfficerRole(),
                    request.RejectionReason
                );

                return Ok(new ApiResponse { Success = true, Message = "Application rejected. Applicant has been notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application");
                return StatusCode(500, new ApiResponse { Success = false, Message = "Failed to reject application", Errors = new List<string> { ex.Message } });
            }
        }

        private int GetCurrentOfficerId()
        {
            var officerIdClaim = HttpContext.User.FindFirst("officer_id") ?? 
                                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(officerIdClaim?.Value ?? "0");
        }

        private string GetCurrentOfficerRole()
        {
            return HttpContext.User.FindFirst("role")?.Value ?? "Officer";
        }
    }
}
