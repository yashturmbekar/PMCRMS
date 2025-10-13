using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssistantEngineerController : ControllerBase
    {
        private readonly IAEWorkflowService _workflowService;
        private readonly ILogger<AssistantEngineerController> _logger;

        public AssistantEngineerController(
            IAEWorkflowService workflowService,
            ILogger<AssistantEngineerController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for the current Assistant Engineer based on their position specialty
        /// </summary>
        [HttpGet("pending/{positionType}")]
        public async Task<ActionResult<List<AEWorkflowStatusDto>>> GetPendingApplications(PositionType positionType)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetPendingApplicationsAsync(officerId, positionType);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending applications for position {PositionType}", positionType);
                return StatusCode(500, new { message = "Error retrieving pending applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get completed applications for the current Assistant Engineer based on their position specialty
        /// </summary>
        [HttpGet("completed/{positionType}")]
        public async Task<ActionResult<List<AEWorkflowStatusDto>>> GetCompletedApplications(PositionType positionType)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetCompletedApplicationsAsync(officerId, positionType);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed applications for position {PositionType}", positionType);
                return StatusCode(500, new { message = "Error retrieving completed applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        [HttpGet("application/{id}/status")]
        public async Task<ActionResult<AEWorkflowStatusDto>> GetApplicationStatus(int id, [FromQuery] PositionType positionType)
        {
            try
            {
                var status = await _workflowService.GetWorkflowStatusAsync(id, positionType);
                if (status == null)
                {
                    return NotFound(new { message = "Application not found" });
                }
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application status for ID {ApplicationId}", id);
                return StatusCode(500, new { message = "Error retrieving application status", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate OTP for digital signature
        /// </summary>
        [HttpPost("application/{id}/generate-otp")]
        public async Task<ActionResult<string>> GenerateOtp(int id)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var otp = await _workflowService.GenerateOtpForSignatureAsync(id, officerId);
                
                // In production, OTP should not be returned in response
                // It should only be sent to officer's registered email/phone
                return Ok(new { message = "OTP sent successfully", otp = otp }); // TODO: Remove OTP from response in production
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for application {ApplicationId}", id);
                return StatusCode(500, new { message = "Error generating OTP", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify documents, apply digital signature, and forward to Executive Engineer
        /// </summary>
        [HttpPost("verify-and-sign")]
        public async Task<ActionResult<WorkflowActionResultDto>> VerifyAndSign(
            [FromBody] VerifyAndSignRequestDto request)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workflowService.VerifyAndSignDocumentsAsync(
                    request.ApplicationId, 
                    officerId, 
                    request.PositionType, 
                    request.Otp, 
                    request.Comments);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying and signing application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new { message = "Error processing verification", error = ex.Message });
            }
        }

        /// <summary>
        /// Reject application with mandatory comments
        /// </summary>
        [HttpPost("reject")]
        public async Task<ActionResult<WorkflowActionResultDto>> RejectApplication(
            [FromBody] RejectApplicationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RejectionComments))
                {
                    return BadRequest(new { message = "Rejection comments are mandatory" });
                }

                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workflowService.RejectApplicationAsync(
                    request.ApplicationId, 
                    officerId, 
                    request.PositionType, 
                    request.RejectionComments);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new { message = "Error processing rejection", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request DTO for verify and sign operation
    /// </summary>
    public class VerifyAndSignRequestDto
    {
        public int ApplicationId { get; set; }
        public PositionType PositionType { get; set; }
        public string Otp { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request DTO for rejection operation
    /// </summary>
    public class RejectApplicationRequestDto
    {
        public int ApplicationId { get; set; }
        public PositionType PositionType { get; set; }
        public string RejectionComments { get; set; } = string.Empty;
    }
}
