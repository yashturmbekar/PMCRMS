using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
    public class DigitalSignatureController : ControllerBase
    {
        private readonly IDigitalSignatureService _signatureService;
        private readonly ILogger<DigitalSignatureController> _logger;

        public DigitalSignatureController(
            IDigitalSignatureService signatureService,
            ILogger<DigitalSignatureController> logger)
        {
            _signatureService = signatureService;
            _logger = logger;
        }

        /// <summary>
        /// Initiate digital signature process for a document
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateSignature([FromBody] InitiateSignatureRequestDto request)
        {
            try
            {
                var currentUser = User.Identity?.Name;
                if (string.IsNullOrEmpty(currentUser))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Get officer ID from authenticated user
                var officerIdClaim = User.FindFirst("OfficerId");
                if (officerIdClaim == null || !int.TryParse(officerIdClaim.Value, out int officerId))
                {
                    return BadRequest(new { message = "Officer ID not found in token" });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _signatureService.InitiateSignatureAsync(
                    request.ApplicationId,
                    officerId,
                    request.SignatureType,
                    request.DocumentPath,
                    request.Coordinates ?? string.Empty,
                    ipAddress,
                    userAgent
                );

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToSignatureResponseDto(result.Signature!);
                return Ok(new
                {
                    message = result.Message,
                    signature = response,
                    metadata = result.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating digital signature");
                return StatusCode(500, new { message = "An error occurred while initiating signature" });
            }
        }

        /// <summary>
        /// Complete digital signature with OTP
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteSignature(int id, [FromBody] CompleteSignatureRequestDto request)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "System";

                var result = await _signatureService.CompleteSignatureAsync(id, request.Otp, currentUser);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToSignatureResponseDto(result.Signature!);
                return Ok(new
                {
                    message = result.Message,
                    signature = response,
                    signedDocumentPath = result.SignedDocumentPath,
                    signatureHash = result.SignatureHash
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing digital signature");
                return StatusCode(500, new { message = "An error occurred while completing signature" });
            }
        }

        /// <summary>
        /// Verify a digital signature
        /// </summary>
        [HttpPost("{id}/verify")]
        public async Task<IActionResult> VerifySignature(int id)
        {
            try
            {
                var result = await _signatureService.VerifySignatureAsync(id);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = new SignatureVerificationDto
                {
                    SignatureId = id,
                    IsValid = result.IsVerified ?? false,
                    IsVerified = result.IsVerified ?? false,
                    VerificationMessage = result.Message,
                    VerifiedAt = DateTime.UtcNow,
                    CertificateStatus = result.Signature?.Status.ToString() ?? "Unknown",
                    IsCertificateValid = result.Signature?.CertificateExpiryDate > DateTime.UtcNow,
                    IsDocumentTampered = !(result.IsVerified ?? false),
                    VerificationDetails = new Dictionary<string, object>
                    {
                        { "SignatureHash", result.SignatureHash ?? "N/A" },
                        { "Status", result.Signature?.Status.ToString() ?? "Unknown" }
                    }
                };

                return Ok(new
                {
                    message = result.Message,
                    verification = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                return StatusCode(500, new { message = "An error occurred while verifying signature" });
            }
        }

        /// <summary>
        /// Retry a failed signature
        /// </summary>
        [HttpPost("{id}/retry")]
        public async Task<IActionResult> RetrySignature(int id, [FromBody] RetrySignatureRequestDto request)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "System";

                var result = await _signatureService.RetrySignatureAsync(id, request.Otp, currentUser);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToSignatureResponseDto(result.Signature!);
                return Ok(new
                {
                    message = result.Message,
                    signature = response,
                    reason = request.Reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying signature");
                return StatusCode(500, new { message = "An error occurred while retrying signature" });
            }
        }

        /// <summary>
        /// Revoke a digital signature
        /// </summary>
        [HttpPut("{id}/revoke")]
        public async Task<IActionResult> RevokeSignature(int id, [FromBody] RevokeSignatureRequestDto request)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "System";

                var result = await _signatureService.RevokeSignatureAsync(id, request.Reason, currentUser);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToSignatureResponseDto(result.Signature!);
                return Ok(new
                {
                    message = result.Message,
                    signature = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking signature");
                return StatusCode(500, new { message = "An error occurred while revoking signature" });
            }
        }

        /// <summary>
        /// Get signature details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSignature(int id)
        {
            try
            {
                var signature = await _signatureService.GetSignatureByIdAsync(id);

                if (signature == null)
                {
                    return NotFound(new { message = "Signature not found" });
                }

                var response = MapToSignatureResponseDto(signature);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving signature");
                return StatusCode(500, new { message = "An error occurred while retrieving signature" });
            }
        }

        /// <summary>
        /// Get all signatures for an application
        /// </summary>
        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetSignaturesForApplication(int applicationId)
        {
            try
            {
                var signatures = await _signatureService.GetSignaturesForApplicationAsync(applicationId);

                var response = signatures.Select(MapToSignatureListDto).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application signatures");
                return StatusCode(500, new { message = "An error occurred while retrieving signatures" });
            }
        }

        /// <summary>
        /// Get signature summary for an application
        /// </summary>
        [HttpGet("application/{applicationId}/summary")]
        public async Task<IActionResult> GetApplicationSignatureSummary(int applicationId)
        {
            try
            {
                var signatures = await _signatureService.GetSignaturesForApplicationAsync(applicationId);

                var summary = new ApplicationSignatureSummaryDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = signatures.FirstOrDefault()?.Application?.ApplicationNumber ?? "N/A",
                    TotalSignatures = signatures.Count,
                    CompletedSignatures = signatures.Count(s => s.Status == SignatureStatus.Completed || s.Status == SignatureStatus.Verified),
                    PendingSignatures = signatures.Count(s => s.Status == SignatureStatus.Pending || s.Status == SignatureStatus.InProgress),
                    FailedSignatures = signatures.Count(s => s.Status == SignatureStatus.Failed),
                    AllSignaturesCompleted = signatures.Any() && signatures.All(s => 
                        s.Status == SignatureStatus.Completed || 
                        s.Status == SignatureStatus.Verified),
                    SignatureProgress = signatures.Any()
                        ? (signatures.Count(s => s.Status == SignatureStatus.Completed || s.Status == SignatureStatus.Verified) * 100.0 / signatures.Count)
                        : 0,
                    Signatures = signatures.Select(MapToSignatureListDto).ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving signature summary");
                return StatusCode(500, new { message = "An error occurred while retrieving signature summary" });
            }
        }

        /// <summary>
        /// Get signatures for current officer
        /// </summary>
        [HttpGet("my-signatures")]
        public async Task<IActionResult> GetMySignatures([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // TODO: Get actual officer ID from authenticated user
                var officerIdClaim = User.FindFirst("OfficerId");
                if (officerIdClaim == null || !int.TryParse(officerIdClaim.Value, out int officerId))
                {
                    return BadRequest(new { message = "Officer ID not found in token" });
                }

                var signatures = await _signatureService.GetSignaturesForOfficerAsync(officerId, startDate, endDate);

                var response = signatures.Select(MapToSignatureListDto).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving officer signatures");
                return StatusCode(500, new { message = "An error occurred while retrieving signatures" });
            }
        }

        /// <summary>
        /// Get signature statistics for current officer
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var officerIdClaim = User.FindFirst("OfficerId");
                if (officerIdClaim == null || !int.TryParse(officerIdClaim.Value, out int officerId))
                {
                    return BadRequest(new { message = "Officer ID not found in token" });
                }

                var stats = await _signatureService.GetSignatureStatisticsAsync(officerId, startDate, endDate);

                var response = new SignatureStatisticsDto
                {
                    TotalSignatures = stats.GetValueOrDefault("Total", 0),
                    PendingCount = stats.GetValueOrDefault("Pending", 0),
                    InProgressCount = stats.GetValueOrDefault("InProgress", 0),
                    CompletedCount = stats.GetValueOrDefault("Completed", 0),
                    FailedCount = stats.GetValueOrDefault("Failed", 0),
                    VerifiedCount = stats.GetValueOrDefault("Verified", 0),
                    RevokedCount = stats.GetValueOrDefault("Revoked", 0),
                    AverageSignatureDurationSeconds = stats.GetValueOrDefault("AverageSeconds", 0),
                    SuccessRate = stats.GetValueOrDefault("Total", 0) > 0
                        ? (stats.GetValueOrDefault("Completed", 0) + stats.GetValueOrDefault("Verified", 0)) * 100.0 / stats.GetValueOrDefault("Total", 0)
                        : 0,
                    VerificationRate = stats.GetValueOrDefault("Completed", 0) > 0
                        ? stats.GetValueOrDefault("Verified", 0) * 100.0 / stats.GetValueOrDefault("Completed", 0)
                        : 0,
                    TotalRetries = stats.GetValueOrDefault("TotalRetries", 0)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving signature statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        /// <summary>
        /// Check if officer's certificate is valid
        /// </summary>
        [HttpGet("certificate/validate")]
        public async Task<IActionResult> ValidateCertificate()
        {
            try
            {
                var officerIdClaim = User.FindFirst("OfficerId");
                if (officerIdClaim == null || !int.TryParse(officerIdClaim.Value, out int officerId))
                {
                    return BadRequest(new { message = "Officer ID not found in token" });
                }

                var isValid = await _signatureService.IsCertificateValidAsync(officerId);

                return Ok(new
                {
                    isValid,
                    message = isValid ? "Certificate is valid" : "Certificate is invalid or expired"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate");
                return StatusCode(500, new { message = "An error occurred while validating certificate" });
            }
        }

        /// <summary>
        /// Check HSM service health
        /// </summary>
        [HttpGet("hsm/health")]
        public async Task<IActionResult> CheckHsmHealth()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var isHealthy = await _signatureService.CheckHsmHealthAsync();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var response = new HsmHealthDto
                {
                    IsHealthy = isHealthy,
                    ServiceUrl = "Configured", // Don't expose actual URL in response
                    Provider = "HSM", // Get from configuration
                    CheckedAt = DateTime.UtcNow,
                    ResponseTimeMs = (int)responseTime,
                    ErrorMessage = isHealthy ? null : "HSM service is not accessible",
                    ServiceInfo = new Dictionary<string, object>
                    {
                        { "Status", isHealthy ? "Online" : "Offline" },
                        { "CheckedBy", User.Identity?.Name ?? "System" }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking HSM health");
                return StatusCode(500, new { message = "An error occurred while checking HSM health" });
            }
        }

        // Helper methods for mapping

        private SignatureResponseDto MapToSignatureResponseDto(DigitalSignature signature)
        {
            return new SignatureResponseDto
            {
                Id = signature.Id,
                ApplicationId = signature.ApplicationId,
                ApplicationNumber = signature.Application?.ApplicationNumber ?? "N/A",
                Type = signature.Type,
                TypeDisplay = signature.Type.ToString(),
                Status = signature.Status,
                StatusDisplay = signature.Status.ToString(),
                SignedByOfficerId = signature.SignedByOfficerId,
                OfficerName = signature.SignedByOfficer?.Name ?? "Unknown",
                SignedDate = signature.SignedDate,
                SignedDocumentPath = signature.SignedDocumentPath,
                SignatureHash = signature.SignatureHash,
                CertificateThumbprint = signature.CertificateThumbprint,
                CertificateIssuer = signature.CertificateIssuer,
                CertificateSubject = signature.CertificateSubject,
                CertificateExpiryDate = signature.CertificateExpiryDate,
                HsmProvider = signature.HsmProvider,
                HsmTransactionId = signature.HsmTransactionId,
                SignatureCoordinates = signature.SignatureCoordinates,
                IsVerified = signature.IsVerified,
                LastVerifiedDate = signature.LastVerifiedDate,
                VerificationDetails = signature.VerificationDetails,
                ErrorMessage = signature.ErrorMessage,
                RetryCount = signature.RetryCount,
                SignatureStartedAt = signature.SignatureStartedAt,
                SignatureCompletedAt = signature.SignatureCompletedAt,
                SignatureDurationSeconds = signature.SignatureDurationSeconds,
                IsCertificateValid = signature.CertificateExpiryDate.HasValue && signature.CertificateExpiryDate.Value > DateTime.UtcNow,
                DaysUntilCertificateExpiry = signature.CertificateExpiryDate.HasValue
                    ? (int)(signature.CertificateExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : 0,
                CreatedDate = signature.CreatedDate
            };
        }

        private SignatureListDto MapToSignatureListDto(DigitalSignature signature)
        {
            return new SignatureListDto
            {
                Id = signature.Id,
                ApplicationId = signature.ApplicationId,
                ApplicationNumber = signature.Application?.ApplicationNumber ?? "N/A",
                Type = signature.Type,
                TypeDisplay = signature.Type.ToString(),
                Status = signature.Status,
                StatusDisplay = signature.Status.ToString(),
                SignedDate = signature.SignedDate,
                OfficerName = signature.SignedByOfficer?.Name ?? "Unknown",
                IsVerified = signature.IsVerified,
                IsPending = signature.Status == SignatureStatus.Pending || signature.Status == SignatureStatus.InProgress,
                IsCompleted = signature.Status == SignatureStatus.Completed || signature.Status == SignatureStatus.Verified,
                HasError = signature.Status == SignatureStatus.Failed
            };
        }
    }
}
