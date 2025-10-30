using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JEWorkflowController : ControllerBase
    {
        private readonly IJEWorkflowService _workflowService;
        private readonly ILogger<JEWorkflowController> _logger;

        // Role constants for authorization - includes ALL junior-level officer roles
        private const string JuniorRoles = "JuniorArchitect,JuniorLicenceEngineer,JuniorStructuralEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin";
        private const string AllOfficerRoles = "JuniorArchitect,JuniorLicenceEngineer,JuniorStructuralEngineer,JuniorSupervisor1,JuniorSupervisor2,AssistantArchitect,AssistantLicenceEngineer,AssistantStructuralEngineer,AssistantSupervisor1,AssistantSupervisor2,ExecutiveEngineer,CityEngineer,Clerk,Admin";

        public JEWorkflowController(
            IJEWorkflowService workflowService,
            ILogger<JEWorkflowController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Start JE workflow for an application - auto-assigns to available JE
        /// </summary>
        [HttpPost("start")]
        [Authorize(Roles = "Admin,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> StartWorkflow([FromBody] StartJEWorkflowRequestDto request)
        {
            try
            {
                var result = await _workflowService.StartWorkflowAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow for application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Schedule appointment for site visit/document review
        /// </summary>
        [HttpPost("schedule-appointment")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> ScheduleAppointment([FromBody] ScheduleAppointmentRequestDto request)
        {
            try
            {
                var result = await _workflowService.ScheduleAppointmentAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Reschedule an existing appointment to a new date/time
        /// </summary>
        [HttpPost("reschedule-appointment")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> RescheduleAppointment([FromBody] RescheduleAppointmentRequestDto request)
        {
            try
            {
                var result = await _workflowService.RescheduleAppointmentAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Complete appointment and transition to document verification
        /// </summary>
        [HttpPost("complete-appointment")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> CompleteAppointment([FromBody] CompleteAppointmentRequestDto request)
        {
            try
            {
                var result = await _workflowService.CompleteAppointmentAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Verify a document as part of the verification stage
        /// </summary>
        [HttpPost("verify-document")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> VerifyDocument([FromBody] VerifyDocumentRequestDto request)
        {
            try
            {
                var result = await _workflowService.VerifyDocumentAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Generate OTP for digital signature on recommendation form
        /// </summary>
        [HttpPost("application/{id}/generate-otp")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GenerateOtpForSignature(int id)
        {
            try
            {
                var result = await _workflowService.GenerateOtpForSignatureAsync(id, GetCurrentUserId());
                return Ok(new
                {
                    success = true,
                    message = "OTP sent to your registered email address",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for signature");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Complete all document verifications
        /// </summary>
        [HttpPost("complete-verification/{applicationId}")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> CompleteDocumentVerification(int applicationId)
        {
            try
            {
                var result = await _workflowService.CompleteDocumentVerificationAsync(applicationId, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing verification");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Initiate digital signature process
        /// </summary>
        [HttpPost("initiate-signature/{applicationId}")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> InitiateDigitalSignature(int applicationId, [FromBody] string documentPath)
        {
            try
            {
                var result = await _workflowService.InitiateDigitalSignatureAsync(applicationId, GetCurrentUserId(), documentPath);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating signature");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Complete digital signature with OTP
        /// </summary>
        [HttpPost("complete-signature")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> CompleteDigitalSignature([FromBody] ApplySignatureRequestDto request)
        {
            try
            {
                var result = await _workflowService.CompleteDigitalSignatureAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing signature");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get complete workflow status for an application
        /// </summary>
        [HttpGet("status/{applicationId}")]
        [ProducesResponseType(typeof(JEWorkflowStatusDto), 200)]
        public async Task<IActionResult> GetWorkflowStatus(int applicationId)
        {
            try
            {
                var status = await _workflowService.GetWorkflowStatusAsync(applicationId);
                return status != null ? Ok(status) : NotFound(new { Message = "Application not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow status");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get complete workflow history with timeline
        /// </summary>
        [HttpGet("history/{applicationId}")]
        [ProducesResponseType(typeof(WorkflowHistoryDto), 200)]
        public async Task<IActionResult> GetWorkflowHistory(int applicationId)
        {
            try
            {
                var history = await _workflowService.GetWorkflowHistoryAsync(applicationId);
                return history != null ? Ok(history) : NotFound(new { Message = "Application not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow history");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow timeline events
        /// </summary>
        [HttpGet("timeline/{applicationId}")]
        [ProducesResponseType(typeof(List<WorkflowTimelineEventDto>), 200)]
        public async Task<IActionResult> GetWorkflowTimeline(int applicationId)
        {
            try
            {
                var timeline = await _workflowService.GetWorkflowTimelineAsync(applicationId);
                return Ok(timeline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Transition workflow to specific status (admin override)
        /// </summary>
        [HttpPost("transition")]
        [Authorize(Roles = "Admin,AssistantEngineer,ExecutiveEngineer")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> TransitionToStatus([FromBody] TransitionWorkflowRequestDto request)
        {
            try
            {
                var result = await _workflowService.TransitionToStatusAsync(request, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning status");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Validate if workflow can proceed to next stage
        /// </summary>
        [HttpGet("validate/{applicationId}")]
        [ProducesResponseType(typeof(WorkflowValidationResultDto), 200)]
        public async Task<IActionResult> ValidateWorkflowProgress(int applicationId)
        {
            try
            {
                var validation = await _workflowService.ValidateWorkflowProgressAsync(applicationId);
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating workflow");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow summary for all applications
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Roles = "Admin,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        [ProducesResponseType(typeof(WorkflowSummaryDto), 200)]
        public async Task<IActionResult> GetWorkflowSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var summary = await _workflowService.GetWorkflowSummaryAsync(fromDate, toDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow summary");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get workflow metrics for dashboard
        /// </summary>
        [HttpGet("metrics")]
        [Authorize(Roles = "Admin,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        [ProducesResponseType(typeof(WorkflowMetricsDto), 200)]
        public async Task<IActionResult> GetWorkflowMetrics([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var metrics = await _workflowService.GetWorkflowMetricsAsync(fromDate, toDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get all applications for a specific JE officer
        /// </summary>
        [HttpGet("officer/{officerId}/applications")]
        [Authorize(Roles = JuniorRoles + ",AssistantEngineer")]
        [ProducesResponseType(typeof(List<JEWorkflowStatusDto>), 200)]
        public async Task<IActionResult> GetOfficerApplications(int officerId)
        {
            try
            {
                var applications = await _workflowService.GetOfficerApplicationsAsync(officerId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting officer applications");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get all applications pending at a specific stage
        /// </summary>
        [HttpGet("stage/{status}")]
        [Authorize(Roles = "Admin,AssistantEngineer,ExecutiveEngineer")]
        [ProducesResponseType(typeof(List<JEWorkflowStatusDto>), 200)]
        public async Task<IActionResult> GetApplicationsByStage(ApplicationCurrentStatus status)
        {
            try
            {
                var applications = await _workflowService.GetApplicationsByStageAsync(status);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications by stage");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Perform bulk workflow actions
        /// </summary>
        [HttpPost("bulk-action")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<WorkflowActionResultDto>), 200)]
        public async Task<IActionResult> PerformBulkAction([FromBody] BulkWorkflowActionRequestDto request)
        {
            try
            {
                var results = await _workflowService.PerformBulkActionAsync(request, GetCurrentUserId());
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Retry failed workflow step
        /// </summary>
        [HttpPost("retry/{applicationId}/{stepName}")]
        [Authorize(Roles = "Admin,AssistantEngineer")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> RetryWorkflowStep(int applicationId, string stepName)
        {
            try
            {
                var result = await _workflowService.RetryWorkflowStepAsync(applicationId, stepName, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying workflow step");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Cancel workflow for an application
        /// </summary>
        [HttpPost("cancel/{applicationId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> CancelWorkflow(int applicationId, [FromBody] string reason)
        {
            try
            {
                var result = await _workflowService.CancelWorkflowAsync(applicationId, reason, GetCurrentUserId());
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling workflow");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Send reminders for delayed applications
        /// </summary>
        [HttpPost("send-reminders")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> SendDelayedApplicationReminders()
        {
            try
            {
                var count = await _workflowService.SendDelayedApplicationRemindersAsync();
                return Ok(new { Message = $"Sent {count} reminders", Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminders");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Reject application with comments
        /// </summary>
        [HttpPost("reject")]
        [Authorize(Roles = JuniorRoles)]
        [ProducesResponseType(typeof(WorkflowActionResultDto), 200)]
        public async Task<IActionResult> RejectApplication([FromBody] RejectApplicationRequestDto request)
        {
            try
            {
                var result = await _workflowService.RejectApplicationAsync(
                    request.ApplicationId, 
                    GetCurrentUserId(), 
                    request.RejectionComments);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Retry recommendation form generation - for cases where automatic generation failed
        /// </summary>
        [HttpPost("retry-recommendation-form/{applicationId}")]
        [Authorize(Roles = JuniorRoles + ",Admin")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> RetryRecommendationFormGeneration(int applicationId)
        {
            try
            {
                var result = await _workflowService.RetryRecommendationFormGenerationAsync(applicationId);
                
                if (result.Success)
                {
                    return Ok(new 
                    { 
                        Success = true,
                        Message = result.Message,
                        Data = new
                        {
                            ApplicationId = applicationId,
                            IsRecommendationFormGenerated = result.Data?.GetType().GetProperty("IsRecommendationFormGenerated")?.GetValue(result.Data),
                            GeneratedDate = result.Data?.GetType().GetProperty("RecommendationFormGeneratedDate")?.GetValue(result.Data),
                            Attempts = result.Data?.GetType().GetProperty("RecommendationFormGenerationAttempts")?.GetValue(result.Data)
                        }
                    });
                }
                
                return BadRequest(new 
                { 
                    Success = false,
                    Message = result.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying recommendation form generation for application {ApplicationId}", applicationId);
                return StatusCode(500, new 
                { 
                    Success = false,
                    Message = "Internal server error", 
                    Error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Check recommendation form generation status for an application
        /// </summary>
        [HttpGet("recommendation-form-status/{applicationId}")]
        [Authorize(Roles = AllOfficerRoles)]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetRecommendationFormStatus(int applicationId)
        {
            try
            {
                var status = await _workflowService.GetRecommendationFormStatusAsync(applicationId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendation form status for application {ApplicationId}", applicationId);
                return StatusCode(500, new 
                { 
                    Success = false,
                    Message = "Internal server error", 
                    Error = ex.Message 
                });
            }
        }
    }
}
