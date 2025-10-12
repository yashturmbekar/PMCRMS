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
    public class ExecutiveEngineerController : ControllerBase
    {
        private readonly IEEWorkflowService _workflowService;
        private readonly ILogger<ExecutiveEngineerController> _logger;

        public ExecutiveEngineerController(
            IEEWorkflowService workflowService,
            ILogger<ExecutiveEngineerController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for the current Executive Engineer (all position types)
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<List<EEWorkflowStatusDto>>> GetPendingApplications()
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetPendingApplicationsAsync(officerId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending applications for EE");
                return StatusCode(500, new { message = "Error retrieving pending applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get completed applications for the current Executive Engineer
        /// </summary>
        [HttpGet("completed")]
        public async Task<ActionResult<List<EEWorkflowStatusDto>>> GetCompletedApplications()
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _workflowService.GetCompletedApplicationsAsync(officerId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed applications for EE");
                return StatusCode(500, new { message = "Error retrieving completed applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow status for a specific application
        /// </summary>
        [HttpGet("application/{id}/status")]
        public async Task<ActionResult<EEWorkflowStatusDto>> GetApplicationStatus(int id)
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
        /// Generate OTP for digital signature
        /// </summary>
        [HttpPost("application/{id}/generate-otp")]
        public async Task<ActionResult<string>> GenerateOtp(int id)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var otp = await _workflowService.GenerateOtpForSignatureAsync(id, officerId);
                
                return Ok(new { message = "OTP sent successfully", otp = otp }); // TODO: Remove OTP from response in production
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for application {ApplicationId}", id);
                return StatusCode(500, new { message = "Error generating OTP", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify documents, apply digital signature, and forward to City Engineer
        /// </summary>
        [HttpPost("application/{id}/verify-and-sign")]
        public async Task<ActionResult<WorkflowActionResultDto>> VerifyAndSign(
            int id, 
            [FromBody] EEVerifyAndSignRequestDto request)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workflowService.VerifyAndSignDocumentsAsync(
                    id, 
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
                _logger.LogError(ex, "Error verifying and signing application {ApplicationId}", id);
                return StatusCode(500, new { message = "Error processing verification", error = ex.Message });
            }
        }

        /// <summary>
        /// Reject application with mandatory comments
        /// </summary>
        [HttpPost("application/{id}/reject")]
        public async Task<ActionResult<WorkflowActionResultDto>> RejectApplication(
            int id, 
            [FromBody] EERejectApplicationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RejectionComments))
                {
                    return BadRequest(new { message = "Rejection comments are mandatory" });
                }

                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workflowService.RejectApplicationAsync(
                    id, 
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
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", id);
                return StatusCode(500, new { message = "Error processing rejection", error = ex.Message });
            }
        }
    }

    public class EEVerifyAndSignRequestDto
    {
        public string Otp { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class EERejectApplicationRequestDto
    {
        public string RejectionComments { get; set; } = string.Empty;
    }
}
