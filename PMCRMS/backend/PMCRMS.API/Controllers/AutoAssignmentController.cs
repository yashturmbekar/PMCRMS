using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AutoAssignmentController : ControllerBase
    {
        private readonly IAutoAssignmentService _assignmentService;
        private readonly ILogger<AutoAssignmentController> _logger;

        public AutoAssignmentController(
            IAutoAssignmentService assignmentService,
            ILogger<AutoAssignmentController> logger)
        {
            _assignmentService = assignmentService;
            _logger = logger;
        }

        /// <summary>
        /// Auto-assign an application to an available Junior Engineer
        /// </summary>
        [HttpPost("assign/{applicationId}")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<AssignmentResponseDto>> AssignApplication(int applicationId)
        {
            try
            {
                var adminId = User.FindFirst("UserId")?.Value;
                
                var assignment = await _assignmentService.AssignApplicationAsync(applicationId, adminId);
                
                if (assignment == null)
                {
                    return NotFound(new { message = "No available officer found or application not found" });
                }

                return Ok(new AssignmentResponseDto
                {
                    AssignmentId = assignment.Id,
                    ApplicationId = assignment.ApplicationId,
                    AssignedToOfficerId = assignment.AssignedToOfficerId,
                    PreviousOfficerId = assignment.PreviousOfficerId,
                    Action = assignment.Action.ToString(),
                    AssignedDate = assignment.AssignedDate,
                    Reason = assignment.Reason,
                    OfficerWorkload = assignment.OfficerWorkloadAtAssignment,
                    StrategyUsed = assignment.StrategyUsed?.ToString(),
                    Success = true,
                    Message = "Application assigned successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while assigning the application" });
            }
        }

        /// <summary>
        /// Reassign an application to a different Junior Engineer
        /// </summary>
        [HttpPost("reassign")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<AssignmentResponseDto>> ReassignApplication([FromBody] ReassignApplicationRequestDto request)
        {
            try
            {
                var adminId = User.FindFirst("UserId")?.Value;
                
                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { message = "Admin ID not found" });
                }

                var assignment = await _assignmentService.ReassignApplicationAsync(
                    request.ApplicationId,
                    request.NewOfficerId,
                    request.Reason,
                    adminId);

                return Ok(new AssignmentResponseDto
                {
                    AssignmentId = assignment.Id,
                    ApplicationId = assignment.ApplicationId,
                    AssignedToOfficerId = assignment.AssignedToOfficerId,
                    PreviousOfficerId = assignment.PreviousOfficerId,
                    Action = assignment.Action.ToString(),
                    AssignedDate = assignment.AssignedDate,
                    Reason = assignment.Reason,
                    OfficerWorkload = assignment.OfficerWorkloadAtAssignment,
                    Success = true,
                    Message = "Application reassigned successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new { message = "An error occurred while reassigning the application" });
            }
        }

        /// <summary>
        /// Get assignment history for an application
        /// </summary>
        [HttpGet("history/{applicationId}")]
        public async Task<ActionResult<List<AssignmentHistoryDto>>> GetAssignmentHistory(int applicationId)
        {
            try
            {
                var history = await _assignmentService.GetAssignmentHistoryAsync(applicationId);
                
                var historyDtos = history.Select(h => new AssignmentHistoryDto
                {
                    Id = h.Id,
                    ApplicationId = h.ApplicationId,
                    PreviousOfficerId = h.PreviousOfficerId,
                    PreviousOfficerName = h.PreviousOfficer?.FullName,
                    AssignedToOfficerId = h.AssignedToOfficerId,
                    AssignedOfficerName = h.AssignedToOfficer?.FullName,
                    Action = h.Action.ToString(),
                    AssignedDate = h.AssignedDate,
                    Reason = h.Reason,
                    OfficerWorkloadAtAssignment = h.OfficerWorkloadAtAssignment,
                    StrategyUsed = h.StrategyUsed?.ToString(),
                    NotificationSent = h.NotificationSent,
                    OfficerAccepted = h.OfficerAccepted,
                    AcceptedAt = h.AcceptedAt,
                    IsActive = h.IsActive,
                    AssignmentDurationHours = h.AssignmentDurationHours,
                    AdminComments = h.AdminComments
                }).ToList();

                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment history for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while retrieving assignment history" });
            }
        }

        /// <summary>
        /// Get workload statistics for officers by role
        /// </summary>
        [HttpGet("workload/statistics")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<WorkloadStatisticsDto>> GetWorkloadStatistics([FromQuery] OfficerRole? role)
        {
            try
            {
                if (!role.HasValue)
                {
                    return BadRequest(new { message = "Officer role is required" });
                }

                var statistics = await _assignmentService.GetWorkloadStatisticsAsync(role.Value);
                
                return Ok(new WorkloadStatisticsDto
                {
                    Role = role.Value.ToString(),
                    OfficerWorkloads = statistics.Select(s => new OfficerWorkloadDto
                    {
                        OfficerId = s.Key,
                        CurrentWorkload = s.Value
                    }).ToList(),
                    TotalOfficers = statistics.Count,
                    TotalWorkload = statistics.Values.Sum(),
                    AverageWorkload = statistics.Any() ? (decimal)statistics.Values.Average() : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workload statistics for role {Role}", role);
                return StatusCode(500, new { message = "An error occurred while retrieving workload statistics" });
            }
        }

        /// <summary>
        /// Get officer's current workload
        /// </summary>
        [HttpGet("workload/officer/{officerId}")]
        public async Task<ActionResult<OfficerWorkloadDto>> GetOfficerWorkload(int officerId)
        {
            try
            {
                var workload = await _assignmentService.CalculateWorkloadAsync(officerId);
                
                return Ok(new OfficerWorkloadDto
                {
                    OfficerId = officerId,
                    CurrentWorkload = workload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workload for officer {OfficerId}", officerId);
                return StatusCode(500, new { message = "An error occurred while retrieving officer workload" });
            }
        }

        /// <summary>
        /// Get applications needing escalation
        /// </summary>
        [HttpGet("escalation/pending")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<List<int>>> GetApplicationsNeedingEscalation()
        {
            try
            {
                var applicationIds = await _assignmentService.GetApplicationsNeedingEscalationAsync();
                
                return Ok(new
                {
                    applicationIds,
                    count = applicationIds.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications needing escalation");
                return StatusCode(500, new { message = "An error occurred while retrieving applications for escalation" });
            }
        }

        /// <summary>
        /// Escalate an application to a higher role
        /// </summary>
        [HttpPost("escalation/escalate/{applicationId}")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<AssignmentResponseDto>> EscalateApplication(int applicationId, [FromBody] EscalateApplicationRequestDto request)
        {
            try
            {
                var assignment = await _assignmentService.EscalateApplicationAsync(applicationId, request.EscalationReason);
                
                if (assignment == null)
                {
                    return NotFound(new { message = "Application not found or no escalation path configured" });
                }

                return Ok(new AssignmentResponseDto
                {
                    AssignmentId = assignment.Id,
                    ApplicationId = assignment.ApplicationId,
                    AssignedToOfficerId = assignment.AssignedToOfficerId,
                    PreviousOfficerId = assignment.PreviousOfficerId,
                    Action = assignment.Action.ToString(),
                    AssignedDate = assignment.AssignedDate,
                    Reason = assignment.Reason,
                    OfficerWorkload = assignment.OfficerWorkloadAtAssignment,
                    Success = true,
                    Message = "Application escalated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while escalating the application" });
            }
        }

        /// <summary>
        /// Validate if an officer can be assigned to an application
        /// </summary>
        [HttpPost("validate")]
        [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<AssignmentValidationDto>> ValidateAssignment([FromBody] ValidateAssignmentRequestDto request)
        {
            try
            {
                var isValid = await _assignmentService.ValidateAssignmentAsync(request.ApplicationId, request.OfficerId);
                
                return Ok(new AssignmentValidationDto
                {
                    ApplicationId = request.ApplicationId,
                    OfficerId = request.OfficerId,
                    IsValid = isValid,
                    Message = isValid ? "Assignment is valid" : "Assignment validation failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating assignment for application {ApplicationId} to officer {OfficerId}",
                    request.ApplicationId, request.OfficerId);
                return StatusCode(500, new { message = "An error occurred while validating the assignment" });
            }
        }
    }
}
