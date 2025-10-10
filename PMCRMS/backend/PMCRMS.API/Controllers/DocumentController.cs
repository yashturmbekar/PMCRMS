using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<DocumentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _uploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public DocumentController(PMCRMSDbContext context, ILogger<DocumentController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            var fileSettings = _configuration.GetSection("FileUploadSettings");
            _uploadPath = fileSettings["UploadPath"] ?? "./uploads";
            _maxFileSize = (long.Parse(fileSettings["MaxFileSizeMB"] ?? "10")) * 1024 * 1024; // Convert MB to bytes
            _allowedExtensions = fileSettings.GetSection("AllowedExtensions").Get<string[]>() ?? 
                                new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        [HttpPost("upload/{applicationId}")]
        public async Task<ActionResult<ApiResponse<DocumentDto>>> UploadDocument(
            int applicationId, 
            IFormFile file, 
            [FromForm] string documentType,
            [FromForm] string? description = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "No file uploaded",
                        Errors = new List<string> { "File is required" }
                    });
                }

                // Check file size
                if (file.Length > _maxFileSize)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB",
                        Errors = new List<string> { "File too large" }
                    });
                }

                // Check file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "File type not allowed",
                        Errors = new List<string> { $"Allowed extensions: {string.Join(", ", _allowedExtensions)}" }
                    });
                }

                // Verify application exists and user has permission
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Check permissions
                if (userRole == UserRole.User && application.ApplicantId != userId)
                {
                    return Forbid();
                }

                // Parse document type
                if (!Enum.TryParse<DocumentType>(documentType, true, out var docType))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid document type",
                        Errors = new List<string> { $"Valid types: {string.Join(", ", Enum.GetNames<DocumentType>())}" }
                    });
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(_uploadPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save document record to database
                var document = new ApplicationDocument
                {
                    ApplicationId = applicationId,
                    Type = docType,
                    FileName = file.FileName,
                    FilePath = filePath,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    Description = description,
                    IsRequired = IsRequiredDocument(docType, application.Type),
                    CreatedBy = userId.ToString()
                };

                _context.ApplicationDocuments.Add(document);
                await _context.SaveChangesAsync();

                var documentDto = new DocumentDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    DocumentType = document.Type.ToString(),
                    FileSize = document.FileSize,
                    UploadedAt = document.CreatedDate
                };

                _logger.LogInformation("Document {DocumentId} uploaded for application {ApplicationId} by user {UserId}", 
                    document.Id, applicationId, userId);

                return Ok(new ApiResponse<DocumentDto>
                {
                    Success = true,
                    Message = "Document uploaded successfully",
                    Data = documentDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for application {ApplicationId}", applicationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to upload document",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("download/{documentId}")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var document = await _context.ApplicationDocuments
                    .Include(d => d.Application)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Document not found"
                    });
                }

                // Check permissions
                if (userRole == UserRole.User && document.Application.ApplicantId != userId)
                {
                    return Forbid();
                }

                if (!System.IO.File.Exists(document.FilePath))
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "File not found on server"
                    });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);
                var contentType = document.ContentType ?? "application/octet-stream";

                _logger.LogInformation("Document {DocumentId} downloaded by user {UserId}", documentId, userId);

                return File(fileBytes, contentType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to download document",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpDelete("{documentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteDocument(int documentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var document = await _context.ApplicationDocuments
                    .Include(d => d.Application)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Document not found"
                    });
                }

                // Check permissions - only applicant can delete their own documents, and only in draft status
                if (userRole == UserRole.User && 
                    (document.Application.ApplicantId != userId || 
                     document.Application.CurrentStatus != ApplicationCurrentStatus.Draft))
                {
                    return Forbid();
                }

                // Delete file from disk
                if (System.IO.File.Exists(document.FilePath))
                {
                    System.IO.File.Delete(document.FilePath);
                }

                // Delete database record
                _context.ApplicationDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} deleted by user {UserId}", documentId, userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Document deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to delete document",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("application/{applicationId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DocumentDto>>>> GetApplicationDocuments(int applicationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Check permissions
                if (userRole == UserRole.User && application.ApplicantId != userId)
                {
                    return Forbid();
                }

                var documents = await _context.ApplicationDocuments
                    .Where(d => d.ApplicationId == applicationId)
                    .OrderByDescending(d => d.CreatedDate)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.Type.ToString(),
                        FileSize = d.FileSize,
                        UploadedAt = d.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<DocumentDto>>
                {
                    Success = true,
                    Message = "Documents retrieved successfully",
                    Data = documents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for application {ApplicationId}", applicationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve documents",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst("user_id") ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = HttpContext.User.FindFirst("role") ?? HttpContext.User.FindFirst(ClaimTypes.Role);
            return Enum.Parse<UserRole>(roleClaim?.Value ?? "Applicant");
        }

        private bool IsRequiredDocument(DocumentType docType, ApplicationType appType)
        {
            // Define which documents are required for each application type
            var requiredDocs = appType switch
            {
                ApplicationType.BuildingPermit => new[] { DocumentType.SitePlan, DocumentType.FloorPlan, DocumentType.IdentityProof },
                ApplicationType.OccupancyCertificate => new[] { DocumentType.FloorPlan, DocumentType.IdentityProof },
                ApplicationType.CompletionCertificate => new[] { DocumentType.IdentityProof },
                ApplicationType.DemolitionPermit => new[] { DocumentType.StructuralPlan, DocumentType.IdentityProof },
                _ => new[] { DocumentType.IdentityProof }
            };

            return requiredDocs.Contains(docType);
        }
    }
}
