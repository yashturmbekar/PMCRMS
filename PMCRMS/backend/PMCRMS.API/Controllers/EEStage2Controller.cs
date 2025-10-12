using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Controller for EE Stage 2 workflow (Digital Signature on Certificate)
    /// Handles certificate signing after clerk processing
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ExecutiveEngineer,Admin")]
    public class EEStage2Controller : ControllerBase
    {
        private readonly EEStage2WorkflowService _eeStage2Service;
        private readonly ILogger<EEStage2Controller> _logger;

        public EEStage2Controller(
            EEStage2WorkflowService eeStage2Service,
            ILogger<EEStage2Controller> logger)
        {
            _eeStage2Service = eeStage2Service;
            _logger = logger;
        }

        /// <summary>
        /// Get pending applications for EE Stage 2 signature (ProcessedByClerk status)
        /// GET /api/EEStage2/Pending
        /// </summary>
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                _logger.LogInformation("[EEStage2Controller] Get pending applications request");

                var applications = await _eeStage2Service.GetPendingApplicationsAsync();

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
                _logger.LogError(ex, "[EEStage2Controller] Error getting pending applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving pending applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get completed applications (signed by EE Stage 2)
        /// GET /api/EEStage2/Completed
        /// </summary>
        [HttpGet("Completed")]
        public async Task<IActionResult> GetCompletedApplications()
        {
            try
            {
                _logger.LogInformation("[EEStage2Controller] Get completed applications request");

                var applications = await _eeStage2Service.GetCompletedApplicationsAsync();

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
                _logger.LogError(ex, "[EEStage2Controller] Error getting completed applications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving completed applications",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get application details for EE Stage 2 review
        /// GET /api/EEStage2/Application/{id}
        /// </summary>
        [HttpGet("Application/{id}")]
        public async Task<IActionResult> GetApplicationDetails(int id)
        {
            try
            {
                _logger.LogInformation($"[EEStage2Controller] Get application details: {id}");

                var application = await _eeStage2Service.GetApplicationDetailsAsync(id);

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
                _logger.LogError(ex, $"[EEStage2Controller] Error getting application details: {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving application details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate OTP for digital signature
        /// POST /api/EEStage2/GenerateOtp/{id}
        /// </summary>
        [HttpPost("GenerateOtp/{id}")]
        public async Task<IActionResult> GenerateOtp(int id)
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

                _logger.LogInformation($"[EEStage2Controller] Generate OTP for application {id} by user {userId}");

                var result = await _eeStage2Service.GenerateOtpAsync(id, userId);

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
                        otpReference = result.OtpReference
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EEStage2Controller] Error generating OTP for application {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating OTP",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Apply digital signature to certificate
        /// POST /api/EEStage2/Sign/{id}
        /// </summary>
        [HttpPost("Sign/{id}")]
        public async Task<IActionResult> ApplyDigitalSignature(int id, [FromBody] EEStage2SignRequest request)
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

                if (string.IsNullOrWhiteSpace(request.OtpCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "OTP code is required"
                    });
                }

                _logger.LogInformation($"[EEStage2Controller] Apply digital signature for application {id} by user {userId}");

                var result = await _eeStage2Service.ApplyDigitalSignatureAsync(id, userId, request.OtpCode);

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
                        newStatus = result.NewStatus,
                        signedCertificateUrl = result.SignedCertificateUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EEStage2Controller] Error applying digital signature for application {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error applying digital signature",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get statistics for EE Stage 2 dashboard
        /// GET /api/EEStage2/Statistics
        /// </summary>
        [HttpGet("Statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                _logger.LogInformation("[EEStage2Controller] Get statistics request");

                var pending = await _eeStage2Service.GetPendingApplicationsAsync();
                var completed = await _eeStage2Service.GetCompletedApplicationsAsync();

                var stats = new
                {
                    pendingCount = pending.Count,
                    completedCount = completed.Count,
                    totalSigned = completed.Count,
                    todaySigned = completed.Count(a => a.UpdatedAt.Date == DateTime.UtcNow.Date),
                    weekSigned = completed.Count(a => a.UpdatedAt >= DateTime.UtcNow.AddDays(-7)),
                    monthSigned = completed.Count(a => a.UpdatedAt >= DateTime.UtcNow.AddMonths(-1))
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
                _logger.LogError(ex, "[EEStage2Controller] Error getting statistics");
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
