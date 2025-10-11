using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using System.Globalization;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StructuralEngineerController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<StructuralEngineerController> _logger;

        public StructuralEngineerController(
            PMCRMSDbContext context,
            ILogger<StructuralEngineerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create a new Structural Engineer Application
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<StructuralEngineerApplicationResponse>>> CreateApplication(
            [FromBody] CreateStructuralEngineerApplicationRequest request)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning("Unauthorized access attempt to create SE application");
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Unauthorized access"
                    });
                }

                _logger.LogInformation("Creating structural engineer application for user {UserId}", userIdInt);

                // Generate application number
                var applicationNumber = await GenerateApplicationNumber();

                // Create the main application
                var application = new PositionApplication
                {
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    MotherName = request.MotherName,
                    MobileNumber = request.MobileNumber,
                    EmailAddress = request.EmailAddress,
                    PositionType = (PositionType)request.PositionType,
                    BloodGroup = request.BloodGroup,
                    Height = request.Height,
                    Gender = (Gender)request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    PanCardNumber = request.PanCardNumber.ToUpper(),
                    AadharCardNumber = request.AadharCardNumber,
                    CoaCardNumber = request.CoaCardNumber,
                    UserId = userIdInt,
                    ApplicationNumber = applicationNumber,
                    Status = ApplicationCurrentStatus.Draft,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userIdInt.ToString()
                };

                _context.PositionApplications.Add(application);
                await _context.SaveChangesAsync();

                // Add Addresses
                var addresses = new List<SEAddress>
                {
                    new SEAddress
                    {
                        ApplicationId = application.Id,
                        AddressType = "Current",
                        AddressLine1 = request.CurrentAddress.AddressLine1,
                        AddressLine2 = request.CurrentAddress.AddressLine2,
                        AddressLine3 = request.CurrentAddress.AddressLine3,
                        City = request.CurrentAddress.City,
                        State = request.CurrentAddress.State,
                        Country = request.CurrentAddress.Country,
                        PinCode = request.CurrentAddress.PinCode,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = userIdInt.ToString()
                    },
                    new SEAddress
                    {
                        ApplicationId = application.Id,
                        AddressType = "Permanent",
                        AddressLine1 = request.PermanentAddress.AddressLine1,
                        AddressLine2 = request.PermanentAddress.AddressLine2,
                        AddressLine3 = request.PermanentAddress.AddressLine3,
                        City = request.PermanentAddress.City,
                        State = request.PermanentAddress.State,
                        Country = request.PermanentAddress.Country,
                        PinCode = request.PermanentAddress.PinCode,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = userIdInt.ToString()
                    }
                };
                _context.SEAddresses.AddRange(addresses);

                // Add Qualifications
                var qualifications = request.Qualifications.Select(q => new SEQualification
                {
                    ApplicationId = application.Id,
                    FileId = q.FileId,
                    InstituteName = q.InstituteName,
                    UniversityName = q.UniversityName,
                    Specialization = (Specialization)q.Specialization,
                    DegreeName = q.DegreeName,
                    PassingMonth = q.PassingMonth,
                    YearOfPassing = q.YearOfPassing,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userIdInt.ToString()
                }).ToList();
                _context.SEQualifications.AddRange(qualifications);

                // Add Experiences
                var experiences = request.Experiences.Select(e => new SEExperience
                {
                    ApplicationId = application.Id,
                    FileId = e.FileId,
                    CompanyName = e.CompanyName,
                    Position = e.Position,
                    YearsOfExperience = e.YearsOfExperience,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userIdInt.ToString()
                }).ToList();
                _context.SEExperiences.AddRange(experiences);

                // Add Documents
                var documents = request.Documents.Select(d => new SEDocument
                {
                    ApplicationId = application.Id,
                    DocumentType = (SEDocumentType)d.DocumentType,
                    FilePath = d.FilePath,
                    FileName = d.FileName,
                    FileId = d.FileId,
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userIdInt.ToString()
                }).ToList();
                _context.SEDocuments.AddRange(documents);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Structural engineer application created successfully. ApplicationNumber: {ApplicationNumber}",
                    applicationNumber);

                // Fetch and return the complete application
                var response = await GetApplicationResponse(application.Id);

                return Ok(new ApiResponse<StructuralEngineerApplicationResponse>
                {
                    Success = true,
                    Message = "Application created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating structural engineer application");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all Structural Engineer Applications
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<StructuralEngineerApplicationResponse>>>> GetApplications()
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Unauthorized access"
                    });
                }

                var role = User.FindFirst("role")?.Value;
                
                IQueryable<PositionApplication> query = _context.PositionApplications;

                // Filter based on role
                if (role == "Applicant" || role == "JuniorArchitect")
                {
                    query = query.Where(a => a.UserId == userIdInt);
                }

                var applications = await query
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

                var responses = applications.Select(a => MapToResponse(a)).ToList();

                return Ok(new ApiResponse<List<StructuralEngineerApplicationResponse>>
                {
                    Success = true,
                    Message = "Applications retrieved successfully",
                    Data = responses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching structural engineer applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get a specific Structural Engineer Application by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<StructuralEngineerApplicationResponse>>> GetApplication(int id)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Unauthorized access"
                    });
                }

                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Check authorization
                var role = User.FindFirst("role")?.Value;
                if ((role == "Applicant" || role == "JuniorArchitect") && application.UserId != userIdInt)
                {
                    return Forbid();
                }

                var response = MapToResponse(application);

                return Ok(new ApiResponse<StructuralEngineerApplicationResponse>
                {
                    Success = true,
                    Message = "Application retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching structural engineer application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update Application Status
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<ApiResponse>> UpdateStatus(
            int id,
            [FromBody] UpdatePositionApplicationStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Unauthorized access"
                    });
                }

                var application = await _context.PositionApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                application.Status = (ApplicationCurrentStatus)request.Status;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = userIdInt.ToString();

                if (request.Status == (int)ApplicationCurrentStatus.Submitted)
                {
                    application.SubmittedDate = DateTime.UtcNow;
                }
                else if (request.Status == (int)ApplicationCurrentStatus.Completed)
                {
                    application.ApprovedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Application {ApplicationId} status updated to {Status} by user {UserId}",
                    id, request.Status, userIdInt);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application status updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status for {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #region Private Helper Methods

        private async Task<string> GenerateApplicationNumber()
        {
            var prefix = "SE";
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month.ToString("D2");
            
            // Get the last application number for this month
            var lastNumber = await _context.PositionApplications
                .Where(a => a.ApplicationNumber!.StartsWith($"{prefix}{year}{month}"))
                .OrderByDescending(a => a.ApplicationNumber)
                .Select(a => a.ApplicationNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (!string.IsNullOrEmpty(lastNumber) && lastNumber.Length >= 10)
            {
                var lastSequence = lastNumber.Substring(10);
                if (int.TryParse(lastSequence, out int parsedSequence))
                {
                    sequence = parsedSequence + 1;
                }
            }

            return $"{prefix}{year}{month}{sequence:D4}";
        }

        private async Task<StructuralEngineerApplicationResponse> GetApplicationResponse(int applicationId)
        {
            var application = await _context.PositionApplications
                .Include(a => a.Addresses)
                .Include(a => a.Qualifications)
                .Include(a => a.Experiences)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                throw new Exception("Application not found");

            return MapToResponse(application);
        }

        private StructuralEngineerApplicationResponse MapToResponse(PositionApplication app)
        {
            var monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;

            return new StructuralEngineerApplicationResponse
            {
                Id = app.Id,
                ApplicationNumber = app.ApplicationNumber ?? "",
                FirstName = app.FirstName,
                MiddleName = app.MiddleName,
                LastName = app.LastName,
                MotherName = app.MotherName,
                MobileNumber = app.MobileNumber,
                EmailAddress = app.EmailAddress,
                PositionType = app.PositionType.ToString(),
                BloodGroup = app.BloodGroup,
                Height = app.Height,
                Gender = app.Gender.ToString(),
                DateOfBirth = app.DateOfBirth,
                PanCardNumber = app.PanCardNumber,
                AadharCardNumber = app.AadharCardNumber,
                CoaCardNumber = app.CoaCardNumber,
                Status = app.Status.ToString(),
                SubmittedDate = app.SubmittedDate,
                ApprovedDate = app.ApprovedDate,
                Remarks = app.Remarks,
                CreatedDate = app.CreatedDate,
                Addresses = app.Addresses.Select(a => new AddressResponseDto
                {
                    Id = a.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    AddressLine3 = a.AddressLine3,
                    City = a.City,
                    State = a.State,
                    Country = a.Country,
                    PinCode = a.PinCode
                }).ToList(),
                Qualifications = app.Qualifications.Select(q => new QualificationResponseDto
                {
                    Id = q.Id,
                    FileId = q.FileId,
                    InstituteName = q.InstituteName,
                    UniversityName = q.UniversityName,
                    Specialization = q.Specialization.ToString(),
                    DegreeName = q.DegreeName,
                    PassingMonth = q.PassingMonth,
                    PassingMonthName = q.PassingMonth > 0 && q.PassingMonth <= 12 
                        ? monthNames[q.PassingMonth - 1] 
                        : "",
                    YearOfPassing = q.YearOfPassing,
                    PassingYear = q.YearOfPassing.Year
                }).ToList(),
                Experiences = app.Experiences.Select(e => new ExperienceResponseDto
                {
                    Id = e.Id,
                    FileId = e.FileId,
                    CompanyName = e.CompanyName,
                    Position = e.Position,
                    YearsOfExperience = e.YearsOfExperience,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate
                }).ToList(),
                Documents = app.Documents.Select(d => new DocumentResponseDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType.ToString(),
                    FilePath = d.FilePath,
                    FileName = d.FileName,
                    FileId = d.FileId,
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    IsVerified = d.IsVerified,
                    VerifiedDate = d.VerifiedDate,
                    VerificationRemarks = d.VerificationRemarks
                }).ToList(),
                TotalExperience = app.Experiences.Sum(e => e.YearsOfExperience)
            };
        }

        /// <summary>
        /// Download SE document by ID (serves PDF from database)
        /// </summary>
        [HttpGet("documents/{documentId}/download")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                _logger.LogInformation("Downloading SE document {DocumentId}", documentId);

                var document = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("SE document {DocumentId} not found", documentId);
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Document not found"
                    });
                }

                // Try to serve from database first (for system-generated PDFs like RecommendedForm)
                if (document.FileContent != null && document.FileContent.Length > 0)
                {
                    _logger.LogInformation("Serving SE document {DocumentId} from database ({Size} bytes)", 
                        documentId, document.FileContent.Length);
                    
                    var contentType = document.ContentType ?? "application/pdf";
                    var fileName = document.FileName ?? $"document_{documentId}.pdf";
                    
                    // Set headers to allow inline viewing in browser
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
                    Response.Headers["Content-Length"] = document.FileContent.Length.ToString();
                    
                    _logger.LogInformation("Returning PDF from database: {FileName}, {Size} bytes, ContentType: {ContentType}", 
                        fileName, document.FileContent.Length, contentType);
                    
                    return File(document.FileContent, contentType);
                }

                // Fallback to physical file (for user-uploaded documents)
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath);
                    
                    if (System.IO.File.Exists(physicalPath))
                    {
                        _logger.LogInformation("Serving SE document {DocumentId} from physical path {Path}", 
                            documentId, physicalPath);
                        
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
                        var contentType = document.ContentType ?? "application/pdf";
                        var fileName = document.FileName ?? Path.GetFileName(physicalPath);
                        
                        // Return file with inline disposition to allow viewing in browser
                        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                        return File(fileBytes, contentType);
                    }
                }

                _logger.LogWarning("SE document {DocumentId} has no content - neither database nor physical file", documentId);
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Document content not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading SE document {DocumentId}", documentId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to download document",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion
    }
}
