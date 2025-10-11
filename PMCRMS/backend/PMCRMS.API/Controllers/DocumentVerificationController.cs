using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVerificationController : ControllerBase
    {
        private readonly IDocumentVerificationService _verificationService;
        private readonly ILogger<DocumentVerificationController> _logger;

        public DocumentVerificationController(
            IDocumentVerificationService verificationService,
            ILogger<DocumentVerificationController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        [HttpPost("start")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> StartVerification([FromBody] StartVerificationRequestDto request)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var result = await _verificationService.StartVerificationAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    request.DocumentType,
                    officerId);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToVerificationResponseDto(result.Verification!);

                return Ok(new { message = result.Message, verification = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting verification");
                return StatusCode(500, new { message = "An error occurred while starting verification" });
            }
        }

        [HttpPut("{verificationId}/update-checklist")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> UpdateChecklist(int verificationId, [FromBody] UpdateChecklistRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _verificationService.UpdateChecklistAsync(
                    verificationId,
                    request.ChecklistItems,
                    request.IsAuthentic,
                    request.IsCompliant,
                    request.IsComplete,
                    request.Comments,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToVerificationResponseDto(result.Verification!);

                return Ok(new { message = result.Message, verification = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating verification checklist for VerificationId={VerificationId}", verificationId);
                return StatusCode(500, new { message = "An error occurred while updating the checklist" });
            }
        }

        [HttpPut("{verificationId}/complete")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> CompleteVerification(int verificationId, [FromBody] CompleteVerificationRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _verificationService.CompleteVerificationAsync(
                    verificationId,
                    request.Comments,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToVerificationResponseDto(result.Verification!);

                return Ok(new { message = result.Message, verification = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing verification for VerificationId={VerificationId}", verificationId);
                return StatusCode(500, new { message = "An error occurred while completing verification" });
            }
        }

        [HttpPut("{verificationId}/reject")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> RejectVerification(int verificationId, [FromBody] RejectVerificationRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _verificationService.RejectVerificationAsync(
                    verificationId,
                    request.RejectionReason,
                    request.RequiresResubmission,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToVerificationResponseDto(result.Verification!);

                return Ok(new { message = result.Message, verification = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verification for VerificationId={VerificationId}", verificationId);
                return StatusCode(500, new { message = "An error occurred while rejecting verification" });
            }
        }

        [HttpGet("{verificationId}")]
        public async Task<IActionResult> GetVerificationById(int verificationId)
        {
            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(verificationId);

                if (verification == null)
                {
                    return NotFound(new { message = "Verification not found" });
                }

                var response = MapToVerificationResponseDto(verification);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification {VerificationId}", verificationId);
                return StatusCode(500, new { message = "An error occurred while retrieving verification" });
            }
        }

        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetVerificationsByApplication(int applicationId)
        {
            try
            {
                var verifications = await _verificationService.GetVerificationsForApplicationAsync(applicationId);

                var response = verifications.Select(MapToVerificationListDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verifications for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while retrieving verifications" });
            }
        }

        [HttpGet("application/{applicationId}/summary")]
        public async Task<IActionResult> GetApplicationVerificationSummary(int applicationId)
        {
            try
            {
                var verifications = await _verificationService.GetVerificationsForApplicationAsync(applicationId);
                var allVerified = await _verificationService.AreAllDocumentsVerifiedAsync(applicationId);

                var verificationList = verifications.Select(MapToVerificationListDto).ToList();

                var summary = new ApplicationVerificationSummaryDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = verifications.FirstOrDefault()?.Application?.ApplicationNumber ?? $"APP_{applicationId}",
                    TotalDocuments = verifications.Count,
                    VerifiedDocuments = verifications.Count(v => v.Status == VerificationStatus.Approved),
                    PendingDocuments = verifications.Count(v => v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InProgress),
                    RejectedDocuments = verifications.Count(v => v.Status == VerificationStatus.Rejected || v.Status == VerificationStatus.RequiresResubmission),
                    AllDocumentsVerified = allVerified,
                    VerificationProgress = verifications.Any() 
                        ? (double)verifications.Count(v => v.Status == VerificationStatus.Approved) / verifications.Count * 100 
                        : 0,
                    Verifications = verificationList
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification summary for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while retrieving verification summary" });
            }
        }

        [HttpGet("my-verifications")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> GetMyVerifications(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var verifications = await _verificationService.GetVerificationsForOfficerAsync(
                    officerId,
                    startDate,
                    endDate);

                var response = verifications.Select(MapToVerificationListDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verifications for officer");
                return StatusCode(500, new { message = "An error occurred while retrieving verifications" });
            }
        }

        [HttpGet("pending/application/{applicationId}")]
        public async Task<IActionResult> GetPendingVerifications(int applicationId)
        {
            try
            {
                var verifications = await _verificationService.GetPendingVerificationsAsync(applicationId);

                var response = verifications.Select(MapToVerificationListDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verifications for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while retrieving pending verifications" });
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> GetVerificationStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var stats = await _verificationService.GetVerificationStatisticsAsync(
                    officerId,
                    startDate,
                    endDate);

                var total = stats.GetValueOrDefault("Total", 0);
                var approved = stats.GetValueOrDefault("Approved", 0);
                var rejected = stats.GetValueOrDefault("Rejected", 0) + stats.GetValueOrDefault("RequiresResubmission", 0);

                var response = new VerificationStatisticsDto
                {
                    TotalVerifications = total,
                    PendingCount = stats.GetValueOrDefault("Pending", 0),
                    InProgressCount = stats.GetValueOrDefault("InProgress", 0),
                    ApprovedCount = approved,
                    RejectedCount = stats.GetValueOrDefault("Rejected", 0),
                    RequiresResubmissionCount = stats.GetValueOrDefault("RequiresResubmission", 0),
                    AverageVerificationMinutes = stats.GetValueOrDefault("AverageMinutes", 0),
                    ApprovalRate = total > 0 ? (double)approved / total * 100 : 0,
                    RejectionRate = total > 0 ? (double)rejected / total * 100 : 0
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        private VerificationResponseDto MapToVerificationResponseDto(DocumentVerification verification)
        {
            return new VerificationResponseDto
            {
                Id = verification.Id,
                DocumentId = verification.DocumentId,
                ApplicationId = verification.ApplicationId,
                ApplicationNumber = verification.Application?.ApplicationNumber ?? $"APP_{verification.ApplicationId}",
                ApplicantName = verification.Application != null
                    ? $"{verification.Application.FirstName} {verification.Application.LastName}"
                    : "Unknown",
                DocumentType = verification.DocumentType,
                Status = verification.Status,
                StatusDisplay = verification.Status.ToString(),
                VerifiedByOfficerId = verification.VerifiedByOfficerId,
                OfficerName = verification.VerifiedByOfficer?.Name,
                VerifiedDate = verification.VerifiedDate,
                VerificationStartedAt = verification.VerificationStartedAt,
                VerificationComments = verification.VerificationComments,
                RejectionReason = verification.RejectionReason,
                IsAuthentic = verification.IsAuthentic,
                IsCompliant = verification.IsCompliant,
                IsComplete = verification.IsComplete,
                ChecklistItems = verification.ChecklistItems,
                VerificationDurationMinutes = verification.VerificationDurationMinutes,
                DocumentHash = verification.DocumentHash,
                DocumentSizeBytes = verification.DocumentSizeBytes,
                PageCount = verification.PageCount,
                CreatedDate = verification.CreatedDate
            };
        }

        private VerificationListDto MapToVerificationListDto(DocumentVerification verification)
        {
            return new VerificationListDto
            {
                Id = verification.Id,
                DocumentId = verification.DocumentId,
                ApplicationId = verification.ApplicationId,
                ApplicationNumber = verification.Application?.ApplicationNumber ?? $"APP_{verification.ApplicationId}",
                DocumentType = verification.DocumentType,
                Status = verification.Status,
                StatusDisplay = verification.Status.ToString(),
                VerifiedDate = verification.VerifiedDate,
                OfficerName = verification.VerifiedByOfficer?.Name,
                IsPending = verification.Status == VerificationStatus.Pending || verification.Status == VerificationStatus.InProgress,
                IsCompleted = verification.Status == VerificationStatus.Approved
            };
        }
    }
}
