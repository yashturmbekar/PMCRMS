using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Controller for City Engineer Stage 2 workflow for Position Applications (Licensing)
    /// Handles final digital signature on license certificates
    /// </summary>
    [ApiController]
    [Route("api/position/ce-stage2")]
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
        /// Get pending position applications for CE Stage 2 final signature
        /// </summary>
        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                int? ceUserId = null;
                
                // Filter by CE user ID if not admin
                if (roleClaim != "Admin" && int.TryParse(userIdClaim, out var userId))
                {
                    ceUserId = userId;
                }

                var applications = await _workflowService.GetPendingApplicationsAsync(ceUserId);
                
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} pending position applications for CE final signature",
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
        /// Get completed position applications by CE Stage 2
        /// </summary>
        [HttpGet("Completed")]
        public async Task<IActionResult> GetCompletedApplications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                int? ceUserId = null;
                
                if (roleClaim != "Admin" && int.TryParse(userIdClaim, out var userId))
                {
                    ceUserId = userId;
                }

                var applications = await _workflowService.GetCompletedApplicationsAsync(ceUserId);
                
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {applications.Count} completed position applications",
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
        /// Get detailed information for a specific position application
        /// </summary>
        [HttpGet("{id}")]
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
                        message = $"Position application with ID {id} not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Position application details retrieved successfully",
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
        /// Generate OTP for CE digital signature
        /// </summary>
        [HttpPost("{id}/GenerateOtp")]
        public async Task<IActionResult> GenerateOtp(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var ceUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user credentials"
                    });
                }

                var result = await _workflowService.GenerateOtpAsync(id, ceUserId);
                
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
                    data = new { otpReference = result.OtpReference }
                });
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
        /// Apply CE final digital signature to license certificate
        /// </summary>
        [HttpPost("{id}/ApplySignature")]
        public async Task<IActionResult> ApplyFinalSignature(int id, [FromBody] CEStage2SignRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.OtpCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "OTP code is required"
                    });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var ceUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user credentials"
                    });
                }

                var result = await _workflowService.ApplyFinalSignatureAsync(id, ceUserId, request.OtpCode, request.Comments);
                
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
        /// Get CE Stage 2 dashboard statistics
        /// </summary>
        [HttpGet("Statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                int? ceUserId = null;
                
                if (roleClaim != "Admin" && int.TryParse(userIdClaim, out var userId))
                {
                    ceUserId = userId;
                }

                var pending = await _workflowService.GetPendingApplicationsAsync(ceUserId);
                var completed = await _workflowService.GetCompletedApplicationsAsync(ceUserId);

                return Ok(new
                {
                    success = true,
                    message = "Statistics retrieved successfully",
                    data = new
                    {
                        pendingCount = pending.Count,
                        completedCount = completed.Count,
                        totalProcessed = completed.Count
                    }
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

