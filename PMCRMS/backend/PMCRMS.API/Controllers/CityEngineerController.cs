using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CityEngineerController : ControllerBase
    {
        private readonly ICEWorkflowService _workflowService;
        private readonly ILogger<CityEngineerController> _logger;

        public CityEngineerController(
            ICEWorkflowService workflowService,
            ILogger<CityEngineerController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for the current City Engineer (all position types - Final Approval)
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<List<CEWorkflowStatusDto>>> GetPendingApplications()
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetPendingApplicationsAsync(officerId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending applications for CE");
                return StatusCode(500, new { message = "Error retrieving pending applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get completed applications for the current City Engineer
        /// </summary>
        [HttpGet("completed")]
        public async Task<ActionResult<List<CEWorkflowStatusDto>>> GetCompletedApplications()
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetCompletedApplicationsAsync(officerId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed applications for CE");
                return StatusCode(500, new { message = "Error retrieving completed applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        [HttpGet("application/{id}/status")]
        public async Task<ActionResult<CEWorkflowStatusDto>> GetApplicationStatus(int id)
        {
            try
            {
                var status = await _workflowService.GetWorkflowStatusAsync(id);
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
        /// Generate OTP for digital signature (Final Approval)
        /// </summary>
        [HttpPost("application/{id}/generate-otp")]
        public async Task<ActionResult<string>> GenerateOtp(int id)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var message = await _workflowService.GenerateOtpForSignatureAsync(id, officerId);
                
                // Return the HSM success message to frontend
                return Ok(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for application {ApplicationId}", id);
                return StatusCode(500, new { message = "Error generating OTP", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify documents, apply digital signature, and set FINAL APPROVAL
        /// </summary>
        [HttpPost("verify-and-sign")]
        public async Task<ActionResult<WorkflowActionResultDto>> VerifyAndSign(
            [FromBody] CEVerifyAndSignRequestDto request)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workflowService.VerifyAndSignDocumentsAsync(
                    request.ApplicationId, 
                    officerId, 
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
        /// Reject application with mandatory comments (FINAL REJECTION)
        /// </summary>
        [HttpPost("reject")]
        public async Task<ActionResult<WorkflowActionResultDto>> RejectApplication(
            [FromBody] CERejectApplicationRequestDto request)
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

    public class CEVerifyAndSignRequestDto
    {
        public int ApplicationId { get; set; }
        public string Otp { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class CERejectApplicationRequestDto
    {
        public int ApplicationId { get; set; }
        public string RejectionComments { get; set; } = string.Empty;
    }
}
