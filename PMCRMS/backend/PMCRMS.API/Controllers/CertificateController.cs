using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ISECertificateGenerationService _certificateService;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(
            PMCRMSDbContext context,
            ISECertificateGenerationService certificateService,
            ILogger<CertificateController> logger)
        {
            _context = context;
            _certificateService = certificateService;
            _logger = logger;
        }

        /// <summary>
        /// Download license certificate PDF from database (binary storage only)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>PDF file stream from database</returns>
        [HttpGet("download/{applicationId}")]
        public async Task<IActionResult> DownloadCertificate(int applicationId)
        {
            try
            {
                // Get user ID from claims (try multiple claim names for compatibility)
                var userIdClaim = User.FindFirst("userId") 
                    ?? User.FindFirst("user_id") 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier);
                    
                if (userIdClaim == null)
                {
                    _logger.LogWarning("No user ID claim found in token for download");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                int userId = int.Parse(userIdClaim.Value);

                // Check if user is authorized to download (Admin, Officer roles, or application owner)
                var isAuthorizedOfficer = User.IsInRole("Admin") 
                    || User.IsInRole("Clerk") 
                    || User.IsInRole("ExecutiveEngineer") 
                    || User.IsInRole("CityEngineer");
                
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new { message = "Application not found" });
                }

                // Authorization: User must own the application or be an authorized officer
                if (!isAuthorizedOfficer && application.UserId != userId)
                {
                    return Forbid();
                }

                // Retrieve certificate PDF from database
                var certificatePdf = await _certificateService.GetCertificatePdfAsync(applicationId);

                if (certificatePdf == null)
                {
                    return NotFound(new { message = "Certificate not found. It may not have been generated yet." });
                }

                _logger.LogInformation("Certificate downloaded for application {ApplicationId} by user {UserId}", applicationId, userId);

                // Return PDF file stream from binary data (no physical file)
                return File(certificatePdf, "application/pdf", $"LicenceCertificate_{applicationId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "Error downloading certificate", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if certificate exists for an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Certificate status</returns>
        [HttpGet("status/{applicationId}")]
        public async Task<IActionResult> GetCertificateStatus(int applicationId)
        {
            try
            {
                // Get user ID from claims (try multiple claim names for compatibility)
                var userIdClaim = User.FindFirst("userId") 
                    ?? User.FindFirst("user_id") 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier);
                    
                if (userIdClaim == null)
                {
                    _logger.LogWarning("No user ID claim found in token. Available claims: {Claims}", 
                        string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                    return Unauthorized(new { message = "User not authenticated" });
                }

                int userId = int.Parse(userIdClaim.Value);

                // Check if user is authorized to view status (Admin, Officer roles, or application owner)
                var isAuthorizedOfficer = User.IsInRole("Admin") 
                    || User.IsInRole("Clerk") 
                    || User.IsInRole("ExecutiveEngineer") 
                    || User.IsInRole("CityEngineer");
                
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new { message = "Application not found" });
                }

                // Authorization: User must own the application or be an authorized officer
                if (!isAuthorizedOfficer && application.UserId != userId)
                {
                    return Forbid();
                }

                // Check if certificate exists in database
                var certificate = await _context.SEDocuments
                    .Where(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();

                if (certificate == null)
                {
                    return Ok(new
                    {
                        exists = false,
                        message = "Certificate not generated yet",
                        applicationId = applicationId
                    });
                }

                return Ok(new
                {
                    exists = true,
                    message = "Certificate available",
                    applicationId = applicationId,
                    certificateId = certificate.Id,
                    generatedDate = certificate.CreatedDate,
                    fileName = certificate.FileName,
                    fileSize = certificate.FileSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking certificate status for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "Error checking certificate status", error = ex.Message });
            }
        }

        /// <summary>
        /// Regenerate certificate (Admin only)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Success/failure status</returns>
        [Authorize(Roles = "Admin,Officer")]
        [HttpPost("regenerate/{applicationId}")]
        public async Task<IActionResult> RegenerateCertificate(int applicationId)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                int userId = int.Parse(userIdClaim.Value);

                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new { message = "Application not found" });
                }

                // Check if payment is completed
                if (application.Status != ApplicationCurrentStatus.PaymentCompleted)
                {
                    return BadRequest(new { message = "Certificate can only be generated for applications with completed payment" });
                }

                _logger.LogInformation("Regenerating certificate for application {ApplicationId} by user {UserId}", applicationId, userId);

                // Generate certificate
                var success = await _certificateService.GenerateAndSaveLicenceCertificateAsync(applicationId);

                if (success)
                {
                    return Ok(new
                    {
                        message = "Certificate regenerated successfully",
                        applicationId = applicationId
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to regenerate certificate" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating certificate for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "Error regenerating certificate", error = ex.Message });
            }
        }
    }
}
