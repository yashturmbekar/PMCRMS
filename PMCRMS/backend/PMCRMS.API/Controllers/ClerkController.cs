using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Controller for Clerk workflow operations
    /// Handles post-payment application processing
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Clerk,Admin")]
    public class ClerkController : ControllerBase
    {
        private readonly ClerkWorkflowService _clerkService;
        private readonly ILogger<ClerkController> _logger;

        public ClerkController(
            ClerkWorkflowService clerkService,
            ILogger<ClerkController> logger)
        {
            _clerkService = clerkService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for clerk review (PaymentCompleted status)
        /// GET /api/Clerk/Pending
        /// </summary>
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                _logger.LogInformation("[ClerkController] Get pending applications request");

                var applications = await _clerkService.GetPendingApplicationsAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} pending applications",
                    data = applications,
                    count = applications.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkController] Error getting pending applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving pending applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get completed applications processed by clerk
        /// GET /api/Clerk/Completed
        /// </summary>
        [HttpGet("Completed")]
        public async Task<IActionResult> GetCompletedApplications()
        {
            try
            {
                _logger.LogInformation("[ClerkController] Get completed applications request");

                var applications = await _clerkService.GetCompletedApplicationsAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} completed applications",
                    data = applications,
                    count = applications.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkController] Error getting completed applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving completed applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get application details for clerk review
        /// GET /api/Clerk/Application/{id}
        /// </summary>
        [HttpGet("Application/{id}")]
        public async Task<IActionResult> GetApplicationDetails(int id)
        {
            try
            {
                _logger.LogInformation($"[ClerkController] Get application details: {id}");

                var application = await _clerkService.GetApplicationDetailsAsync(id);

                if (application == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Application not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Application details retrieved",
                    data = application
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkController] Error getting application details: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving application details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Approve application and forward to EE Stage 2
        /// POST /api/Clerk/Approve/{id}
        /// </summary>
        [HttpPost("Approve/{id}")]
        public async Task<IActionResult> ApproveApplication(int id, [FromBody] ClerkApproveRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User ID not found in token"
                    });
                }

                _logger.LogInformation($"[ClerkController] Approve application {id} by user {userId}");

                var result = await _clerkService.ApproveApplicationAsync(id, request.Remarks ?? "", userId);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        applicationId = result.ApplicationId,
                        newStatus = result.NewStatus
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkController] Error approving application {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error approving application",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Reject application with reason
        /// POST /api/Clerk/Reject/{id}
        /// </summary>
        [HttpPost("Reject/{id}")]
        public async Task<IActionResult> RejectApplication(int id, [FromBody] ClerkRejectRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User ID not found in token"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Rejection reason is required"
                    });
                }

                _logger.LogInformation($"[ClerkController] Reject application {id} by user {userId}");

                var result = await _clerkService.RejectApplicationAsync(id, request.Reason, userId);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        applicationId = result.ApplicationId,
                        newStatus = result.NewStatus
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ClerkController] Error rejecting application {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error rejecting application",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get statistics for clerk dashboard
        /// GET /api/Clerk/Statistics
        /// </summary>
        [HttpGet("Statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                _logger.LogInformation("[ClerkController] Get statistics request");

                var pending = await _clerkService.GetPendingApplicationsAsync();
                var completed = await _clerkService.GetCompletedApplicationsAsync();

                var stats = new
                {
                    pendingCount = pending.Count,
                    completedCount = completed.Count,
                    totalProcessed = completed.Count,
                    todayProcessed = completed.Count(a => a.UpdatedAt.Date == DateTime.UtcNow.Date),
                    weekProcessed = completed.Count(a => a.UpdatedAt >= DateTime.UtcNow.AddDays(-7)),
                    monthProcessed = completed.Count(a => a.UpdatedAt >= DateTime.UtcNow.AddMonths(-1))
                };

                return Ok(new
                {
                    success = true,
                    message = "Statistics retrieved",
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ClerkController] Error getting statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving statistics",
                    error = ex.Message
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
