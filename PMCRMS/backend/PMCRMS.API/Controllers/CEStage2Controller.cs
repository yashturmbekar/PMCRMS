using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CityEngineer,Admin")]
    public class CEStage2Controller : ControllerBase
    {
        private readonly CEStage2WorkflowService _workflowService;
        private readonly ILogger<CEStage2Controller> _logger;

        public CEStage2Controller(
            CEStage2WorkflowService workflowService,
            ILogger<CEStage2Controller> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for CE Stage 2 final signature (status 20)
        /// </summary>
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                var applications = await _workflowService.GetPendingApplicationsAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} pending applications for CE final signature",
                    data = applications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CEStage2Controller] Error retrieving pending applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving pending applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get completed applications by CE Stage 2
        /// </summary>
        [HttpGet("Completed")]
        public async Task<IActionResult> GetCompletedApplications()
        {
            try
            {
                var applications = await _workflowService.GetCompletedApplicationsAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} completed applications",
                    data = applications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CEStage2Controller] Error retrieving completed applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving completed applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get application details for CE review
        /// </summary>
        [HttpGet("Application/{id}")]
        public async Task<IActionResult> GetApplicationDetails(int id)
        {
            try
            {
                var application = await _workflowService.GetApplicationDetailsAsync(id);
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
                    message = "Application details retrieved successfully",
                    data = application
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Controller] Error retrieving application details for ID: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving application details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate OTP for CE final digital signature
        /// </summary>
        [HttpPost("GenerateOtp/{id}")]
        public async Task<IActionResult> GenerateOtp(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int ceUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user authentication"
                    });
                }

                var result = await _workflowService.GenerateOtpAsync(id, ceUserId);
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        data = result
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Controller] Error generating OTP for application ID: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating OTP",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Apply CE final digital signature with OTP verification
        /// </summary>
        [HttpPost("Sign/{id}")]
        public async Task<IActionResult> ApplyFinalSignature(int id, [FromBody] CEStage2SignRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int ceUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user authentication"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.OtpCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "OTP code is required"
                    });
                }

                var result = await _workflowService.ApplyFinalSignatureAsync(id, ceUserId, request.OtpCode);
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        data = result
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Controller] Error applying final signature for application ID: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error applying digital signature",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get statistics for CE Stage 2 dashboard
        /// </summary>
        [HttpGet("Statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var pendingApplications = await _workflowService.GetPendingApplicationsAsync();
                var completedApplications = await _workflowService.GetCompletedApplicationsAsync();

                var today = DateTime.UtcNow.Date;
                var weekAgo = today.AddDays(-7);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var todayProcessed = completedApplications.Count(a => a.EE2SignedDate.HasValue && a.EE2SignedDate.Value.Date == today);
                var weekProcessed = completedApplications.Count(a => a.EE2SignedDate.HasValue && a.EE2SignedDate.Value >= weekAgo);
                var monthProcessed = completedApplications.Count(a => a.EE2SignedDate.HasValue && a.EE2SignedDate.Value >= monthStart);

                var statistics = new
                {
                    pendingCount = pendingApplications.Count,
                    completedCount = completedApplications.Count,
                    todayProcessed,
                    weekProcessed,
                    monthProcessed
                };

                return Ok(new
                {
                    success = true,
                    message = "Statistics retrieved successfully",
                    data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CEStage2Controller] Error retrieving statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving statistics",
                    error = ex.Message
                });
            }
        }
    }
}
