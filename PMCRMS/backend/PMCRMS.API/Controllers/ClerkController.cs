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
        /// GET /api/Clerk/pending
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                var clerkId = GetCurrentOfficerId();
                if (clerkId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Officer ID not found in token"
                    });
                }

                _logger.LogInformation("[ClerkController] Get pending applications request for Clerk {ClerkId}", clerkId);

                var applications = await _clerkService.GetPendingApplicationsAsync(clerkId);

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
        /// GET /api/Clerk/completed
        /// </summary>
        [HttpGet("completed")]
        public async Task<IActionResult> GetCompletedApplications()
        {
            try
            {
                var clerkId = GetCurrentOfficerId();
                if (clerkId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Officer ID not found in token"
                    });
                }

                _logger.LogInformation("[ClerkController] Get completed applications request for Clerk {ClerkId}", clerkId);

                var applications = await _clerkService.GetCompletedApplicationsAsync(clerkId);

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
        /// GET /api/Clerk/application/{id}
        /// </summary>
        [HttpGet("application/{id}")]
        public async Task<IActionResult> GetApplicationDetails(int id)
        {
            try
            {
                var clerkId = GetCurrentOfficerId();
                if (clerkId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Officer ID not found in token"
                    });
                }

                _logger.LogInformation("[ClerkController] Get application details: {ApplicationId} for Clerk {ClerkId}", id, clerkId);

                var application = await _clerkService.GetApplicationDetailsAsync(id, clerkId);

                if (application == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Application not found or not assigned to you"
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
                _logger.LogError(ex, "[ClerkController] Error getting application details: {ApplicationId}", id);
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
        /// POST /api/Clerk/approve/{id}
        /// </summary>
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveApplication(int id, [FromBody] ClerkApproveRequest request)
        {
            try
            {
                var clerkId = GetCurrentOfficerId();
                if (clerkId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Officer ID not found in token"
                    });
                }

                _logger.LogInformation("[ClerkController] Approve application {ApplicationId} by Clerk {ClerkId}", id, clerkId);

                var result = await _clerkService.ApproveApplicationAsync(id, request.Remarks ?? "", clerkId);

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
                _logger.LogError(ex, "[ClerkController] Error approving application {ApplicationId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error approving application",
                    error = ex.Message
                });
            }
        }

        // REMOVED: Clerks should NOT be able to reject applications (Stage 1 officers only)
        // /// <summary>
        // /// Reject application with reason
        // /// POST /api/Clerk/reject/{id}
        // /// </summary>
        // [HttpPost("reject/{id}")]
        // public async Task<IActionResult> RejectApplication(int id, [FromBody] ClerkRejectRequest request)
        // {
        //     try
        //     {
        //         var clerkId = GetCurrentOfficerId();
        //         if (clerkId == 0)
        //         {
        //             return Unauthorized(new
        //             {
        //                 success = false,
        //                 message = "Officer ID not found in token"
        //             });
        //         }
        //
        //         if (string.IsNullOrWhiteSpace(request.Reason))
        //         {
        //             return BadRequest(new
        //             {
        //                 success = false,
        //                 message = "Rejection reason is required"
        //             });
        //         }
        //
        //         _logger.LogInformation("[ClerkController] Reject application {ApplicationId} by Clerk {ClerkId}", id, clerkId);
        //
        //         var result = await _clerkService.RejectApplicationAsync(id, request.Reason, clerkId);
        //
        //         if (!result.Success)
        //         {
        //             return BadRequest(new
        //             {
        //                 success = false,
        //                 message = result.Message
        //             });
        //         }
        //
        //         return Ok(new
        //         {
        //             success = true,
        //             message = result.Message,
        //             data = new
        //             {
        //                 applicationId = result.ApplicationId,
        //                 newStatus = result.NewStatus
        //             }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "[ClerkController] Error rejecting application {ApplicationId}", id);
        //         return StatusCode(500, new
        //         {
        //             success = false,
        //             message = "Error rejecting application",
        //             error = ex.Message
        //         });
        //     }
        // }

        /// <summary>
        /// Get statistics for clerk dashboard
        /// GET /api/Clerk/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var clerkId = GetCurrentOfficerId();
                if (clerkId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Officer ID not found in token"
                    });
                }

                _logger.LogInformation("[ClerkController] Get statistics request for Clerk {ClerkId}", clerkId);

                var pending = await _clerkService.GetPendingApplicationsAsync(clerkId);
                var completed = await _clerkService.GetCompletedApplicationsAsync(clerkId);

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

        private int GetCurrentOfficerId()
        {
            var officerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(officerIdClaim, out var officerId) ? officerId : 0;
        }
    }
}
