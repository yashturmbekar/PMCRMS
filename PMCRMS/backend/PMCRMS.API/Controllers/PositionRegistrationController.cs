using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using System.Globalization;

namespace PMCRMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionRegistrationController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<PositionRegistrationController> _logger;
        private readonly Services.IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly Services.IAutoAssignmentService _autoAssignmentService;
        private readonly Services.IJEWorkflowService _jeWorkflowService;

        public PositionRegistrationController(
            PMCRMSDbContext context,
            ILogger<PositionRegistrationController> logger,
            Services.IEmailService emailService,
            IConfiguration configuration,
            Services.IAutoAssignmentService autoAssignmentService,
            Services.IJEWorkflowService jeWorkflowService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
            _autoAssignmentService = autoAssignmentService;
            _jeWorkflowService = jeWorkflowService;
        }

        // Helper method to format enum names - just remove underscores
        private string FormatEnumName(string enumValue)
        {
            if (string.IsNullOrEmpty(enumValue))
                return enumValue;

            // Only remove underscores and replace with spaces
            return enumValue.Replace("_", " ");
        }

        // POST: api/PositionRegistration/draft
        /// <summary>
        /// Save application as draft - NO VALIDATION applied
        /// This endpoint allows users to save incomplete applications
        /// </summary>
        [HttpPost("draft")]
        public async Task<ActionResult<PositionRegistrationResponseDTO>> SaveDraft(
            [FromBody] System.Text.Json.JsonElement requestJson)
        {
            try
            {
                _logger.LogInformation("[DRAFT] SaveDraft called - Raw JSON received");
                
                // Deserialize with lenient options - ignore validation attributes
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                PositionRegistrationRequestDTO? request;
                try
                {
                    request = System.Text.Json.JsonSerializer.Deserialize<PositionRegistrationRequestDTO>(requestJson.GetRawText(), options);
                    if (request == null)
                    {
                        return BadRequest(new { error = "Invalid request data" });
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "[DRAFT] Failed to deserialize request - accepting partial data");
                    // Create a minimal request object for drafts
                    request = new PositionRegistrationRequestDTO
                    {
                        PositionType = requestJson.TryGetProperty("positionType", out var pt) ? (PositionType)pt.GetInt32() : PositionType.Architect,
                        FirstName = requestJson.TryGetProperty("firstName", out var fn) ? fn.GetString() ?? "" : "",
                        LastName = requestJson.TryGetProperty("lastName", out var ln) ? ln.GetString() ?? "" : "",
                        MotherName = requestJson.TryGetProperty("motherName", out var mn) ? mn.GetString() ?? "" : "",
                        MobileNumber = requestJson.TryGetProperty("mobileNumber", out var mob) ? mob.GetString() ?? "" : "",
                        EmailAddress = requestJson.TryGetProperty("emailAddress", out var email) ? email.GetString() ?? "" : "",
                        PanCardNumber = requestJson.TryGetProperty("panCardNumber", out var pan) ? pan.GetString() ?? "" : "",
                        AadharCardNumber = requestJson.TryGetProperty("aadharCardNumber", out var aadhar) ? aadhar.GetString() ?? "" : "",
                        Qualifications = new List<QualificationDTO>(),
                        Experiences = new List<ExperienceDTO>(),
                        Documents = new List<DocumentUploadDTO>(),
                        LocalAddress = new AddressDTO(),
                        PermanentAddress = new AddressDTO()
                    };
                }
                
                _logger.LogInformation("[DRAFT] SaveDraft processing - PositionType: {PositionType}", request.PositionType);
                
                // Force status to Draft (1)
                request.Status = ApplicationCurrentStatus.Draft;
                
                // NO VALIDATION for draft - users can save incomplete forms
                // This allows them to come back later and complete the application
                
                // Get user ID from authentication context
                var userId = GetCurrentUserId();

                // Create application entity with minimal required data
                var application = new PositionApplication
                {
                    PositionType = request.PositionType,
                    FirstName = request.FirstName?.Trim() ?? "",
                    MiddleName = request.MiddleName?.Trim(),
                    LastName = request.LastName?.Trim() ?? "",
                    MotherName = request.MotherName?.Trim() ?? "",
                    MobileNumber = request.MobileNumber ?? "",
                    EmailAddress = request.EmailAddress?.ToLower().Trim() ?? "",
                    BloodGroup = request.BloodGroup?.ToUpper().Trim(),
                    Height = request.Height ?? 0,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth != default ? request.DateOfBirth.Date : DateTime.MinValue,
                    PanCardNumber = request.PanCardNumber?.ToUpper().Trim() ?? "",
                    AadharCardNumber = request.AadharCardNumber ?? "",
                    CoaCardNumber = request.CoaCardNumber?.Trim(),
                    UserId = userId,
                    Status = ApplicationCurrentStatus.Draft,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                };

                // No application number for drafts
                application.ApplicationNumber = null;

                // Add addresses if provided
                if (request.LocalAddress != null && !string.IsNullOrEmpty(request.LocalAddress.AddressLine1))
                {
                    application.Addresses.Add(new SEAddress
                    {
                        AddressType = "Local",
                        AddressLine1 = request.LocalAddress.AddressLine1?.Trim() ?? "",
                        AddressLine2 = request.LocalAddress.AddressLine2?.Trim(),
                        AddressLine3 = request.LocalAddress.AddressLine3?.Trim(),
                        City = request.LocalAddress.City?.Trim() ?? "",
                        State = request.LocalAddress.State?.Trim() ?? "",
                        Country = "India", // Always India - ignore user input
                        PinCode = request.LocalAddress.PinCode ?? "",
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                if (request.PermanentAddress != null && !string.IsNullOrEmpty(request.PermanentAddress.AddressLine1))
                {
                    application.Addresses.Add(new SEAddress
                    {
                        AddressType = "Permanent",
                        AddressLine1 = request.PermanentAddress.AddressLine1?.Trim() ?? "",
                        AddressLine2 = request.PermanentAddress.AddressLine2?.Trim(),
                        AddressLine3 = request.PermanentAddress.AddressLine3?.Trim(),
                        City = request.PermanentAddress.City?.Trim() ?? "",
                        State = request.PermanentAddress.State?.Trim() ?? "",
                        Country = "India", // Always India - ignore user input
                        PinCode = request.PermanentAddress.PinCode ?? "",
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Add qualifications if provided
                if (request.Qualifications != null)
                {
                    foreach (var qual in request.Qualifications.Where(q => !string.IsNullOrEmpty(q.InstituteName)))
                    {
                        application.Qualifications.Add(new SEQualification
                        {
                            FileId = qual.FileId,
                            InstituteName = qual.InstituteName?.Trim() ?? "",
                            UniversityName = qual.UniversityName?.Trim() ?? "",
                            Specialization = qual.Specialization,
                            DegreeName = qual.DegreeName?.Trim() ?? "",
                            PassingMonth = qual.PassingMonth,
                            YearOfPassing = qual.YearOfPassing > 0 
                                ? new DateTime(qual.YearOfPassing, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                // Add experiences if provided
                if (request.Experiences != null)
                {
                    foreach (var exp in request.Experiences.Where(e => !string.IsNullOrEmpty(e.CompanyName)))
                    {
                        var yearsOfExperience = 0m;
                        if (exp.FromDate != default && exp.ToDate != default)
                        {
                            yearsOfExperience = CalculateYearsOfExperience(exp.FromDate, exp.ToDate);
                        }
                        
                        application.Experiences.Add(new SEExperience
                        {
                            FileId = exp.FileId,
                            CompanyName = exp.CompanyName?.Trim() ?? "",
                            Position = exp.Position?.Trim() ?? "",
                            FromDate = exp.FromDate != default 
                                ? DateTime.SpecifyKind(exp.FromDate.Date, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            ToDate = exp.ToDate != default 
                                ? DateTime.SpecifyKind(exp.ToDate.Date, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            YearsOfExperience = yearsOfExperience,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                // Add documents if provided - convert base64 to binary and store in database
                if (request.Documents != null)
                {
                    foreach (var doc in request.Documents)
                    {
                        byte[]? fileContent = null;
                        
                        // Convert base64 string to binary data
                        if (!string.IsNullOrEmpty(doc.FileBase64))
                        {
                            try
                            {
                                fileContent = Convert.FromBase64String(doc.FileBase64);
                                _logger.LogInformation("[DRAFT] Converted document {FileId} from base64 to binary ({Size} bytes)", 
                                    doc.FileId, fileContent.Length);
                            }
                            catch (FormatException ex)
                            {
                                _logger.LogWarning(ex, "[DRAFT] Failed to convert base64 data for document {FileId} - skipping", doc.FileId);
                                // Don't fail the draft save if a document has invalid data
                                continue;
                            }
                        }
                        
                        application.Documents.Add(new SEDocument
                        {
                            FileId = doc.FileId,
                            DocumentType = doc.DocumentType,
                            FileName = doc.FileName ?? "",
                            FilePath = null, // No physical file path - storing in database
                            FileContent = fileContent, // Binary data stored in database
                            FileSize = fileContent != null ? (decimal)(fileContent.Length / 1024.0) : doc.FileSize,
                            ContentType = doc.ContentType,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                _context.PositionApplications.Add(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[DRAFT] Position registration draft saved successfully. ID: {Id}", application.Id);

                var response = await GetApplicationResponse(application.Id);
                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DRAFT] Error saving position registration draft");
                return StatusCode(500, new { error = "An error occurred while saving your draft" });
            }
        }

        // POST: api/PositionRegistration
        [HttpPost]
        public async Task<ActionResult<PositionRegistrationResponseDTO>> CreateApplication(
            [FromBody] PositionRegistrationRequestDTO request)
        {
            try
            {
                // Debug logging for position type tracking
                _logger.LogInformation("[DEBUG] CreateApplication called - PositionType: {PositionType}", request.PositionType);
                _logger.LogInformation("[DEBUG] Request JSON: {@Request}", request);
                
                // Only validate if status is Submitted (2), skip validation for Draft (1)
                if (request.Status == ApplicationCurrentStatus.Submitted)
                {
                    var validationErrors = ValidateRequest(request);
                    if (validationErrors.Any())
                    {
                        return BadRequest(new { errors = validationErrors });
                    }
                }

                // Note: Removed duplicate checks for PAN, Aadhar, Email, and Mobile
                // Users can submit multiple applications with the same details

                // Get user ID from authentication context
                var userId = GetCurrentUserId();

                // Create application entity
                var application = new PositionApplication
                {
                    PositionType = request.PositionType,
                    FirstName = request.FirstName.Trim(),
                    MiddleName = request.MiddleName?.Trim(),
                    LastName = request.LastName.Trim(),
                    MotherName = request.MotherName.Trim(),
                    MobileNumber = request.MobileNumber,
                    EmailAddress = request.EmailAddress.ToLower().Trim(),
                    BloodGroup = request.BloodGroup?.ToUpper().Trim(),
                    Height = request.Height ?? 0,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth.Date,
                    PanCardNumber = request.PanCardNumber.ToUpper().Trim(),
                    AadharCardNumber = request.AadharCardNumber,
                    CoaCardNumber = request.CoaCardNumber?.Trim(),
                    UserId = userId,
                    Status = request.Status,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                };

                // Generate application number if submitted
                if (request.Status == ApplicationCurrentStatus.Submitted)
                {
                    application.ApplicationNumber = await GenerateApplicationNumber(request.PositionType);
                    application.SubmittedDate = DateTime.UtcNow;
                }

                // Add addresses
                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Local",
                    AddressLine1 = request.LocalAddress.AddressLine1.Trim(),
                    AddressLine2 = request.LocalAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.LocalAddress.AddressLine3?.Trim(),
                    City = request.LocalAddress.City.Trim(),
                    State = request.LocalAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.LocalAddress.PinCode,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                });

                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Permanent",
                    AddressLine1 = request.PermanentAddress.AddressLine1.Trim(),
                    AddressLine2 = request.PermanentAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.PermanentAddress.AddressLine3?.Trim(),
                    City = request.PermanentAddress.City.Trim(),
                    State = request.PermanentAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.PermanentAddress.PinCode,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                });

                // Add qualifications
                foreach (var qual in request.Qualifications)
                {
                    application.Qualifications.Add(new SEQualification
                    {
                        FileId = qual.FileId,
                        InstituteName = qual.InstituteName.Trim(),
                        UniversityName = qual.UniversityName.Trim(),
                        Specialization = qual.Specialization,
                        DegreeName = qual.DegreeName.Trim(),
                        PassingMonth = qual.PassingMonth,
                        YearOfPassing = new DateTime(qual.YearOfPassing, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Add experiences
                foreach (var exp in request.Experiences)
                {
                    var yearsOfExperience = CalculateYearsOfExperience(exp.FromDate, exp.ToDate);
                    
                    application.Experiences.Add(new SEExperience
                    {
                        FileId = exp.FileId,
                        CompanyName = exp.CompanyName.Trim(),
                        Position = exp.Position.Trim(),
                        FromDate = DateTime.SpecifyKind(exp.FromDate.Date, DateTimeKind.Utc),
                        ToDate = DateTime.SpecifyKind(exp.ToDate.Date, DateTimeKind.Utc),
                        YearsOfExperience = yearsOfExperience,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Add documents - convert base64 to binary and store in database
                foreach (var doc in request.Documents)
                {
                    byte[]? fileContent = null;
                    
                    // Convert base64 string to binary data
                    if (!string.IsNullOrEmpty(doc.FileBase64))
                    {
                        try
                        {
                            fileContent = Convert.FromBase64String(doc.FileBase64);
                            _logger.LogInformation("Converted document {FileId} from base64 to binary ({Size} bytes)", 
                                doc.FileId, fileContent.Length);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogError(ex, "Failed to convert base64 data for document {FileId}", doc.FileId);
                            throw new InvalidOperationException($"Invalid base64 data for document {doc.FileId}");
                        }
                    }
                    
                    application.Documents.Add(new SEDocument
                    {
                        FileId = doc.FileId,
                        DocumentType = doc.DocumentType,
                        FileName = doc.FileName,
                        FilePath = null, // No physical file path - storing in database
                        FileContent = fileContent, // Binary data stored in database
                        FileSize = fileContent != null ? (decimal)(fileContent.Length / 1024.0) : doc.FileSize,
                        ContentType = doc.ContentType,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                _context.PositionApplications.Add(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Position registration application created successfully. ID: {Id}", application.Id);

                // Trigger auto-assignment if application is submitted
                if (request.Status == ApplicationCurrentStatus.Submitted)
                {
                    try
                    {
                        _logger.LogInformation("Triggering auto-assignment for application {ApplicationId}", application.Id);
                        var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(application.Id);
                        
                        if (assignmentResult != null)
                        {
                            _logger.LogInformation("Application {ApplicationId} auto-assigned to officer {OfficerId}. AssignedJuniorEngineerId: {JEId}, AssignedToJEDate: {JEDate}", 
                                application.Id, assignmentResult.AssignedToOfficerId, 
                                assignmentResult.Application?.AssignedJuniorEngineerId, 
                                assignmentResult.Application?.AssignedToJEDate);
                            
                            // Reload application to get updated assignment info
                            await _context.Entry(application).ReloadAsync();
                        }
                        else
                        {
                            _logger.LogWarning("Auto-assignment failed for application {ApplicationId} - no available officer or no assignment rules configured", 
                                application.Id);
                        }
                    }
                    catch (Exception assignEx)
                    {
                        _logger.LogError(assignEx, "Error during auto-assignment for application {ApplicationId}", application.Id);
                        // Don't fail the request if auto-assignment fails
                    }
                }

                // Send email if application is submitted
                if (request.Status == ApplicationCurrentStatus.Submitted && !string.IsNullOrEmpty(application.ApplicationNumber))
                {
                    try
                    {
                        var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? _configuration["CorsSettings:AllowedOrigins:0"] ?? throw new InvalidOperationException("Frontend URL not configured");
                        var viewUrl = $"{frontendUrl}/applications/{application.Id}";
                        var applicantName = $"{application.FirstName} {application.LastName}";
                        var positionTypeName = application.PositionType.ToString();

                        await _emailService.SendApplicationSubmissionEmailAsync(
                            application.EmailAddress,
                            applicantName,
                            application.ApplicationNumber,
                            $"Position Registration - {positionTypeName}",
                            application.Id.ToString(),
                            viewUrl
                        );

                        _logger.LogInformation("Submission email sent to {Email} for application {ApplicationNumber}", 
                            application.EmailAddress, application.ApplicationNumber);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send submission email for application {ApplicationNumber}", 
                            application.ApplicationNumber);
                        // Don't fail the request if email fails
                    }
                }

                var response = await GetApplicationResponse(application.Id);
                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position registration application");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // GET: api/PositionRegistration/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PositionRegistrationResponseDTO>> GetApplication(int id)
        {
            try
            {
                var response = await GetApplicationResponse(id);
                if (response == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // PUT: api/PositionRegistration/draft/{id}
        /// <summary>
        /// Update existing draft application - NO VALIDATION applied
        /// This endpoint allows users to update their saved drafts
        /// </summary>
        [HttpPut("draft/{id}")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] System.Text.Json.JsonElement requestJson)
        {
            try
            {
                _logger.LogInformation("[DRAFT] UpdateDraft called - ID: {Id}", id);
                
                // Deserialize with lenient options - ignore validation attributes
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                PositionRegistrationRequestDTO? request;
                try
                {
                    request = System.Text.Json.JsonSerializer.Deserialize<PositionRegistrationRequestDTO>(requestJson.GetRawText(), options);
                    if (request == null)
                    {
                        return BadRequest(new { error = "Invalid request data" });
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "[DRAFT] Failed to deserialize request - this shouldn't happen for updates");
                    return BadRequest(new { error = "Invalid request data", details = ex.Message });
                }
                
                _logger.LogInformation("[DRAFT] UpdateDraft processing - PositionType: {PositionType}", request.PositionType);
                
                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Only allow updating drafts
                if (application.Status != ApplicationCurrentStatus.Draft)
                {
                    return BadRequest(new { error = "Only draft applications can be updated using this endpoint" });
                }

                // Force status to remain Draft
                request.Status = ApplicationCurrentStatus.Draft;

                // NO VALIDATION for draft updates

                // Update basic fields
                application.PositionType = request.PositionType;
                application.FirstName = request.FirstName?.Trim() ?? "";
                application.MiddleName = request.MiddleName?.Trim();
                application.LastName = request.LastName?.Trim() ?? "";
                application.MotherName = request.MotherName?.Trim() ?? "";
                application.MobileNumber = request.MobileNumber ?? "";
                application.EmailAddress = request.EmailAddress?.ToLower().Trim() ?? "";
                application.BloodGroup = request.BloodGroup?.ToUpper().Trim();
                application.Height = request.Height ?? 0;
                application.Gender = request.Gender;
                application.DateOfBirth = request.DateOfBirth != default ? request.DateOfBirth.Date : DateTime.MinValue;
                application.PanCardNumber = request.PanCardNumber?.ToUpper().Trim() ?? "";
                application.AadharCardNumber = request.AadharCardNumber ?? "";
                application.CoaCardNumber = request.CoaCardNumber?.Trim();
                application.Status = ApplicationCurrentStatus.Draft;
                application.UpdatedBy = "User";
                application.UpdatedDate = DateTime.UtcNow;

                // Application number remains null for drafts

                // Update addresses
                _context.SEAddresses.RemoveRange(application.Addresses);
                
                if (request.LocalAddress != null && !string.IsNullOrEmpty(request.LocalAddress.AddressLine1))
                {
                    application.Addresses.Add(new SEAddress
                    {
                        AddressType = "Local",
                        AddressLine1 = request.LocalAddress.AddressLine1?.Trim() ?? "",
                        AddressLine2 = request.LocalAddress.AddressLine2?.Trim(),
                        AddressLine3 = request.LocalAddress.AddressLine3?.Trim(),
                        City = request.LocalAddress.City?.Trim() ?? "",
                        State = request.LocalAddress.State?.Trim() ?? "",
                        Country = "India", // Always India - ignore user input
                        PinCode = request.LocalAddress.PinCode ?? "",
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                if (request.PermanentAddress != null && !string.IsNullOrEmpty(request.PermanentAddress.AddressLine1))
                {
                    application.Addresses.Add(new SEAddress
                    {
                        AddressType = "Permanent",
                        AddressLine1 = request.PermanentAddress.AddressLine1?.Trim() ?? "",
                        AddressLine2 = request.PermanentAddress.AddressLine2?.Trim(),
                        AddressLine3 = request.PermanentAddress.AddressLine3?.Trim(),
                        City = request.PermanentAddress.City?.Trim() ?? "",
                        State = request.PermanentAddress.State?.Trim() ?? "",
                        Country = "India", // Always India - ignore user input
                        PinCode = request.PermanentAddress.PinCode ?? "",
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update qualifications
                _context.SEQualifications.RemoveRange(application.Qualifications);
                
                if (request.Qualifications != null)
                {
                    foreach (var qual in request.Qualifications.Where(q => !string.IsNullOrEmpty(q.InstituteName)))
                    {
                        application.Qualifications.Add(new SEQualification
                        {
                            FileId = qual.FileId,
                            InstituteName = qual.InstituteName?.Trim() ?? "",
                            UniversityName = qual.UniversityName?.Trim() ?? "",
                            Specialization = qual.Specialization,
                            DegreeName = qual.DegreeName?.Trim() ?? "",
                            PassingMonth = qual.PassingMonth,
                            YearOfPassing = qual.YearOfPassing > 0 
                                ? new DateTime(qual.YearOfPassing, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                // Update experiences
                _context.SEExperiences.RemoveRange(application.Experiences);
                
                if (request.Experiences != null)
                {
                    foreach (var exp in request.Experiences.Where(e => !string.IsNullOrEmpty(e.CompanyName)))
                    {
                        var yearsOfExperience = 0m;
                        if (exp.FromDate != default && exp.ToDate != default)
                        {
                            yearsOfExperience = CalculateYearsOfExperience(exp.FromDate, exp.ToDate);
                        }
                        
                        application.Experiences.Add(new SEExperience
                        {
                            FileId = exp.FileId,
                            CompanyName = exp.CompanyName?.Trim() ?? "",
                            Position = exp.Position?.Trim() ?? "",
                            FromDate = exp.FromDate != default 
                                ? DateTime.SpecifyKind(exp.FromDate.Date, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            ToDate = exp.ToDate != default 
                                ? DateTime.SpecifyKind(exp.ToDate.Date, DateTimeKind.Utc) 
                                : DateTime.MinValue,
                            YearsOfExperience = yearsOfExperience,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                // Update documents - convert base64 to binary and store in database
                _context.SEDocuments.RemoveRange(application.Documents);
                
                if (request.Documents != null)
                {
                    foreach (var doc in request.Documents)
                    {
                        byte[]? fileContent = null;
                        
                        // Convert base64 string to binary data
                        if (!string.IsNullOrEmpty(doc.FileBase64))
                        {
                            try
                            {
                                fileContent = Convert.FromBase64String(doc.FileBase64);
                                _logger.LogInformation("[DRAFT] Converted document {FileId} from base64 to binary ({Size} bytes)", 
                                    doc.FileId, fileContent.Length);
                            }
                            catch (FormatException ex)
                            {
                                _logger.LogWarning(ex, "[DRAFT] Failed to convert base64 data for document {FileId} - skipping", doc.FileId);
                                // Don't fail the draft save if a document has invalid data
                                continue;
                            }
                        }
                        
                        application.Documents.Add(new SEDocument
                        {
                            FileId = doc.FileId,
                            DocumentType = doc.DocumentType,
                            FileName = doc.FileName ?? "",
                            FilePath = null, // No physical file path - storing in database
                            FileContent = fileContent, // Binary data stored in database
                            FileSize = fileContent != null ? (decimal)(fileContent.Length / 1024.0) : doc.FileSize,
                            ContentType = doc.ContentType,
                            CreatedBy = "User",
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("[DRAFT] Position registration draft updated successfully. ID: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DRAFT] Error updating draft application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while updating your draft" });
            }
        }

        // PUT: api/PositionRegistration/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromBody] PositionRegistrationRequestDTO request)
        {
            try
            {
                // Debug logging for position type tracking
                _logger.LogInformation("[DEBUG] UpdateApplication called - ID: {Id}, PositionType: {PositionType}", id, request.PositionType);
                
                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Check if application can be updated
                if (application.Status == ApplicationCurrentStatus.Completed ||
                    application.Status == ApplicationCurrentStatus.CertificateIssued)
                {
                    return BadRequest(new { error = "Cannot update completed or approved applications" });
                }

                // Only validate if status is Submitted (2), skip validation for Draft (1)
                if (request.Status == ApplicationCurrentStatus.Submitted)
                {
                    var validationErrors = ValidateRequest(request);
                    if (validationErrors.Any())
                    {
                        return BadRequest(new { errors = validationErrors });
                    }
                }

                // Note: Removed duplicate checks for PAN and Aadhar
                // Users can have multiple applications with the same details

                // Update basic fields
                application.PositionType = request.PositionType;
                application.FirstName = request.FirstName.Trim();
                application.MiddleName = request.MiddleName?.Trim();
                application.LastName = request.LastName.Trim();
                application.MotherName = request.MotherName.Trim();
                application.MobileNumber = request.MobileNumber;
                application.EmailAddress = request.EmailAddress.ToLower().Trim();
                application.BloodGroup = request.BloodGroup?.ToUpper().Trim();
                application.Height = request.Height ?? 0;
                application.Gender = request.Gender;
                application.DateOfBirth = request.DateOfBirth.Date;
                application.PanCardNumber = request.PanCardNumber.ToUpper().Trim();
                application.AadharCardNumber = request.AadharCardNumber;
                application.CoaCardNumber = request.CoaCardNumber?.Trim();
                application.Status = request.Status;
                application.UpdatedBy = "User";
                application.UpdatedDate = DateTime.UtcNow;

                // Generate application number if newly submitted
                if (request.Status == ApplicationCurrentStatus.Submitted && string.IsNullOrEmpty(application.ApplicationNumber))
                {
                    application.ApplicationNumber = await GenerateApplicationNumber(request.PositionType);
                    application.SubmittedDate = DateTime.UtcNow;
                }

                // Update addresses
                _context.SEAddresses.RemoveRange(application.Addresses);
                
                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Local",
                    AddressLine1 = request.LocalAddress.AddressLine1.Trim(),
                    AddressLine2 = request.LocalAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.LocalAddress.AddressLine3?.Trim(),
                    City = request.LocalAddress.City.Trim(),
                    State = request.LocalAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.LocalAddress.PinCode,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                });

                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Permanent",
                    AddressLine1 = request.PermanentAddress.AddressLine1.Trim(),
                    AddressLine2 = request.PermanentAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.PermanentAddress.AddressLine3?.Trim(),
                    City = request.PermanentAddress.City.Trim(),
                    State = request.PermanentAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.PermanentAddress.PinCode,
                    CreatedBy = "User",
                    CreatedDate = DateTime.UtcNow
                });

                // Update qualifications
                _context.SEQualifications.RemoveRange(application.Qualifications);
                
                foreach (var qual in request.Qualifications)
                {
                    application.Qualifications.Add(new SEQualification
                    {
                        FileId = qual.FileId,
                        InstituteName = qual.InstituteName.Trim(),
                        UniversityName = qual.UniversityName.Trim(),
                        Specialization = qual.Specialization,
                        DegreeName = qual.DegreeName.Trim(),
                        PassingMonth = qual.PassingMonth,
                        YearOfPassing = new DateTime(qual.YearOfPassing, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update experiences
                _context.SEExperiences.RemoveRange(application.Experiences);
                
                foreach (var exp in request.Experiences)
                {
                    var yearsOfExperience = CalculateYearsOfExperience(exp.FromDate, exp.ToDate);
                    
                    application.Experiences.Add(new SEExperience
                    {
                        FileId = exp.FileId,
                        CompanyName = exp.CompanyName.Trim(),
                        Position = exp.Position.Trim(),
                        FromDate = DateTime.SpecifyKind(exp.FromDate.Date, DateTimeKind.Utc),
                        ToDate = DateTime.SpecifyKind(exp.ToDate.Date, DateTimeKind.Utc),
                        YearsOfExperience = yearsOfExperience,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update documents - convert base64 to binary and store in database
                _context.SEDocuments.RemoveRange(application.Documents);
                
                foreach (var doc in request.Documents)
                {
                    byte[]? fileContent = null;
                    
                    // Convert base64 string to binary data
                    if (!string.IsNullOrEmpty(doc.FileBase64))
                    {
                        try
                        {
                            fileContent = Convert.FromBase64String(doc.FileBase64);
                            _logger.LogInformation("Converted document {FileId} from base64 to binary ({Size} bytes)", 
                                doc.FileId, fileContent.Length);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogError(ex, "Failed to convert base64 data for document {FileId}", doc.FileId);
                            throw new InvalidOperationException($"Invalid base64 data for document {doc.FileId}");
                        }
                    }
                    
                    application.Documents.Add(new SEDocument
                    {
                        FileId = doc.FileId,
                        DocumentType = doc.DocumentType,
                        FileName = doc.FileName,
                        FilePath = null, // No physical file path - storing in database
                        FileContent = fileContent, // Binary data stored in database
                        FileSize = fileContent != null ? (decimal)(fileContent.Length / 1024.0) : doc.FileSize,
                        ContentType = doc.ContentType,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Position registration application updated successfully. ID: {Id}", id);

                // Trigger auto-assignment if application status changed to submitted
                var wasJustSubmitted = request.Status == ApplicationCurrentStatus.Submitted && 
                                      !string.IsNullOrEmpty(application.ApplicationNumber) &&
                                      application.SubmittedDate.HasValue &&
                                      (DateTime.UtcNow - application.SubmittedDate.Value).TotalSeconds < 10;

                if (wasJustSubmitted)
                {
                    try
                    {
                        _logger.LogInformation("Triggering auto-assignment for updated application {ApplicationId}", application.Id);
                        var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(application.Id);
                        
                        if (assignmentResult != null)
                        {
                            _logger.LogInformation("Application {ApplicationId} auto-assigned to officer {OfficerId}. AssignedJuniorEngineerId: {JEId}, AssignedToJEDate: {JEDate}", 
                                application.Id, assignmentResult.AssignedToOfficerId,
                                assignmentResult.Application?.AssignedJuniorEngineerId,
                                assignmentResult.Application?.AssignedToJEDate);
                            
                            // Reload application to get updated assignment info
                            await _context.Entry(application).ReloadAsync();
                        }
                        else
                        {
                            _logger.LogWarning("Auto-assignment failed for application {ApplicationId} - no available officer or no assignment rules configured", 
                                application.Id);
                        }
                    }
                    catch (Exception assignEx)
                    {
                        _logger.LogError(assignEx, "Error during auto-assignment for application {ApplicationId}", application.Id);
                        // Don't fail the request if auto-assignment fails
                    }
                }

                // Send email if application status changed to submitted
                if (wasJustSubmitted && !string.IsNullOrEmpty(application.ApplicationNumber))
                {
                    try
                    {
                        var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? _configuration["CorsSettings:AllowedOrigins:0"] ?? throw new InvalidOperationException("Frontend URL not configured");
                        var viewUrl = $"{frontendUrl}/applications/{application.Id}";
                        var applicantName = $"{application.FirstName} {application.LastName}";
                        var positionTypeName = application.PositionType.ToString();

                        await _emailService.SendApplicationSubmissionEmailAsync(
                            application.EmailAddress,
                            applicantName,
                            application.ApplicationNumber,
                            $"Position Registration - {positionTypeName}",
                            application.Id.ToString(),
                            viewUrl
                        );

                        _logger.LogInformation("Submission email sent to {Email} for application {ApplicationNumber}", 
                            application.EmailAddress, application.ApplicationNumber);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send submission email for application {ApplicationNumber}", 
                            application.ApplicationNumber);
                        // Don't fail the request if email fails
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // DELETE: api/PositionRegistration/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Only allow deletion of draft applications
                if (application.Status != ApplicationCurrentStatus.Draft)
                {
                    return BadRequest(new { error = "Only draft applications can be deleted" });
                }

                _context.PositionApplications.Remove(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Position registration application deleted successfully. ID: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // POST: api/PositionRegistration/5/resubmit
        /// <summary>
        /// Resubmit a rejected application with updated data
        /// This endpoint allows users to edit and resubmit their rejected applications
        /// The application will re-enter the workflow at the Junior Engineer stage
        /// </summary>
        [HttpPost("{id}/resubmit")]
        public async Task<ActionResult<PositionRegistrationResponseDTO>> ResubmitApplication(
            int id, 
            [FromBody] PositionRegistrationRequestDTO request)
        {
            try
            {
                _logger.LogInformation("[Resubmit] Starting resubmission for application {Id}", id);
                
                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Only allow resubmission of rejected applications
                if (application.Status != ApplicationCurrentStatus.REJECTED)
                {
                    return BadRequest(new { 
                        error = "Only rejected applications can be resubmitted", 
                        currentStatus = application.Status.ToString() 
                    });
                }

                // Validate the updated request
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Store rejection history in Remarks for audit trail
                var previousRemarks = application.Remarks ?? "";
                var rejectionHistory = $"[PREVIOUS REJECTION - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {previousRemarks}";

                // Update basic fields with new data from user
                application.PositionType = request.PositionType;
                application.FirstName = request.FirstName.Trim();
                application.MiddleName = request.MiddleName?.Trim();
                application.LastName = request.LastName.Trim();
                application.MotherName = request.MotherName.Trim();
                application.MobileNumber = request.MobileNumber;
                application.EmailAddress = request.EmailAddress.ToLower().Trim();
                application.BloodGroup = request.BloodGroup?.ToUpper().Trim();
                application.Height = request.Height ?? 0;
                application.Gender = request.Gender;
                application.DateOfBirth = request.DateOfBirth.Date;
                application.PanCardNumber = request.PanCardNumber.ToUpper().Trim();
                application.AadharCardNumber = request.AadharCardNumber;
                application.CoaCardNumber = request.CoaCardNumber?.Trim();
                
                // Reset status to Junior Engineer Pending to restart workflow
                application.Status = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING;
                application.Remarks = $"Resubmitted on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. {rejectionHistory}";
                application.UpdatedBy = "User_Resubmission";
                application.UpdatedDate = DateTime.UtcNow;
                application.SubmittedDate = DateTime.UtcNow; // Update submission date
                
                // Clear all rejection flags to allow fresh review
                application.JERejectionStatus = null;
                application.JERejectionComments = null;
                application.JERejectionDate = null;
                
                application.AEArchitectRejectionStatus = null;
                application.AEArchitectRejectionComments = null;
                application.AEArchitectRejectionDate = null;
                
                application.AEStructuralRejectionStatus = null;
                application.AEStructuralRejectionComments = null;
                application.AEStructuralRejectionDate = null;
                
                application.AELicenceRejectionStatus = null;
                application.AELicenceRejectionComments = null;
                application.AELicenceRejectionDate = null;
                
                application.AESupervisor1RejectionStatus = null;
                application.AESupervisor1RejectionComments = null;
                application.AESupervisor1RejectionDate = null;
                
                application.AESupervisor2RejectionStatus = null;
                application.AESupervisor2RejectionComments = null;
                application.AESupervisor2RejectionDate = null;
                
                application.ExecutiveEngineerRejectionStatus = null;
                application.ExecutiveEngineerRejectionComments = null;
                application.ExecutiveEngineerRejectionDate = null;
                
                application.CityEngineerRejectionStatus = null;
                application.CityEngineerRejectionComments = null;
                application.CityEngineerRejectionDate = null;
                
                application.ClerkRejectionStatus = null;
                application.ClerkRejectionComments = null;
                application.ClerkRejectionDate = null;
                
                // Reset recommendation form status - it will need to be regenerated after review
                application.IsRecommendationFormGenerated = false;
                application.RecommendationFormGeneratedDate = null;
                application.RecommendationFormGenerationAttempts = 0;
                application.RecommendationFormGenerationError = null;

                // Update addresses
                _context.SEAddresses.RemoveRange(application.Addresses);
                
                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Local",
                    AddressLine1 = request.LocalAddress.AddressLine1.Trim(),
                    AddressLine2 = request.LocalAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.LocalAddress.AddressLine3?.Trim(),
                    City = request.LocalAddress.City.Trim(),
                    State = request.LocalAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.LocalAddress.PinCode,
                    CreatedBy = "User_Resubmission",
                    CreatedDate = DateTime.UtcNow
                });

                application.Addresses.Add(new SEAddress
                {
                    AddressType = "Permanent",
                    AddressLine1 = request.PermanentAddress.AddressLine1.Trim(),
                    AddressLine2 = request.PermanentAddress.AddressLine2?.Trim(),
                    AddressLine3 = request.PermanentAddress.AddressLine3?.Trim(),
                    City = request.PermanentAddress.City.Trim(),
                    State = request.PermanentAddress.State.Trim(),
                    Country = "India", // Always India - ignore user input
                    PinCode = request.PermanentAddress.PinCode,
                    CreatedBy = "User_Resubmission",
                    CreatedDate = DateTime.UtcNow
                });

                // Update qualifications
                _context.SEQualifications.RemoveRange(application.Qualifications);
                
                foreach (var qual in request.Qualifications)
                {
                    application.Qualifications.Add(new SEQualification
                    {
                        FileId = qual.FileId,
                        InstituteName = qual.InstituteName.Trim(),
                        UniversityName = qual.UniversityName.Trim(),
                        Specialization = qual.Specialization,
                        DegreeName = qual.DegreeName.Trim(),
                        PassingMonth = qual.PassingMonth,
                        YearOfPassing = new DateTime(qual.YearOfPassing, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        CreatedBy = "User_Resubmission",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update experiences
                _context.SEExperiences.RemoveRange(application.Experiences);
                
                foreach (var exp in request.Experiences)
                {
                    var yearsOfExperience = CalculateYearsOfExperience(exp.FromDate, exp.ToDate);
                    
                    application.Experiences.Add(new SEExperience
                    {
                        FileId = exp.FileId,
                        CompanyName = exp.CompanyName.Trim(),
                        Position = exp.Position.Trim(),
                        FromDate = DateTime.SpecifyKind(exp.FromDate.Date, DateTimeKind.Utc),
                        ToDate = DateTime.SpecifyKind(exp.ToDate.Date, DateTimeKind.Utc),
                        YearsOfExperience = yearsOfExperience,
                        CreatedBy = "User_Resubmission",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update documents - convert base64 to binary and store in database
                // IMPORTANT: Only remove user-submitted documents, preserve system-generated ones
                var systemGeneratedDocTypes = new[] 
                { 
                    SEDocumentType.RecommendedForm,      // Generated by officers
                    SEDocumentType.PaymentChallan,       // Generated by payment system
                    SEDocumentType.LicenceCertificate    // Generated after approval
                };
                
                var userDocuments = application.Documents
                    .Where(d => !systemGeneratedDocTypes.Contains(d.DocumentType))
                    .ToList();
                    
                _context.SEDocuments.RemoveRange(userDocuments);
                
                _logger.LogInformation("[Resubmit] Removed {Count} user documents, preserved system-generated documents", userDocuments.Count);
                
                foreach (var doc in request.Documents)
                {
                    byte[]? fileContent = null;
                    if (!string.IsNullOrEmpty(doc.FileBase64))
                    {
                        try
                        {
                            fileContent = Convert.FromBase64String(doc.FileBase64);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error converting base64 to binary for document {FileId}", doc.FileId);
                            return BadRequest(new { error = $"Invalid file data for document {doc.FileName}" });
                        }
                    }

                    application.Documents.Add(new SEDocument
                    {
                        FileId = doc.FileId,
                        DocumentType = doc.DocumentType,
                        FileName = doc.FileName,
                        FileContent = fileContent,
                        FileSize = doc.FileSize,
                        ContentType = doc.ContentType,
                        FilePath = null, // Deprecated
                        IsVerified = false, // Reset verification status
                        CreatedBy = "User_Resubmission",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("[Resubmit] Application {Id} resubmitted successfully, status reset to JUNIOR_ENGINEER_PENDING", id);

                // Trigger auto-assignment to JE
                try
                {
                    await _autoAssignmentService.AssignApplicationAsync(application.Id);
                    _logger.LogInformation("[Resubmit] Auto-assignment triggered for application {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Resubmit] Error during auto-assignment for application {Id}", id);
                    // Continue even if auto-assignment fails - it can be done manually
                }

                var response = MapToResponse(application);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Resubmit] Error resubmitting application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // GET: api/PositionRegistration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PositionRegistrationResponseDTO>>> GetAllApplications(
            [FromQuery] PositionType? positionType = null,
            [FromQuery] ApplicationCurrentStatus? status = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                var query = _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                    .Include(a => a.AssignedJuniorEngineer)
                    .AsQueryable();

                if (positionType.HasValue)
                {
                    query = query.Where(a => a.PositionType == positionType.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(a => a.Status == status.Value);
                }

                if (userId.HasValue)
                {
                    query = query.Where(a => a.UserId == userId.Value);
                }

                var applications = await query.OrderByDescending(a => a.CreatedDate).ToListAsync();

                var responses = applications.Select(MapToResponse).ToList();

                return Ok(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // GET: api/PositionRegistration/5/workflow
        /// <summary>
        /// Get detailed JE workflow status for an application
        /// </summary>
        [HttpGet("{id}/workflow")]
        public async Task<ActionResult<JEWorkflowStatusDto>> GetApplicationWorkflow(int id)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Check if application is in JE workflow stage
                if (!IsInJEWorkflowStage(application.Status))
                {
                    return BadRequest(new { error = "Application is not in JE workflow stage" });
                }

                var workflowStatus = await _jeWorkflowService.GetWorkflowStatusAsync(id);
                
                if (workflowStatus == null)
                {
                    return NotFound(new { error = "Workflow status not found" });
                }

                return Ok(workflowStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow for application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // GET: api/PositionRegistration/5/workflow/history
        /// <summary>
        /// Get JE workflow history for an application
        /// </summary>
        [HttpGet("{id}/workflow/history")]
        public async Task<ActionResult<WorkflowHistoryDto>> GetApplicationWorkflowHistory(int id)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                var workflowHistory = await _jeWorkflowService.GetWorkflowHistoryAsync(id);
                
                if (workflowHistory == null)
                {
                    return NotFound(new { error = "Workflow history not found" });
                }

                return Ok(workflowHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow history for application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        // GET: api/PositionRegistration/5/workflow/timeline
        /// <summary>
        /// Get JE workflow timeline for an application
        /// </summary>
        [HttpGet("{id}/workflow/timeline")]
        public async Task<ActionResult<List<WorkflowTimelineEventDto>>> GetApplicationWorkflowTimeline(int id)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                var timeline = await _jeWorkflowService.GetWorkflowTimelineAsync(id);
                
                if (timeline == null)
                {
                    return NotFound(new { error = "Workflow timeline not found" });
                }

                return Ok(timeline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow timeline for application {Id}", id);
                return StatusCode(500, new { error = "An error occurred while processing your request" });
            }
        }

        #region Private Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst("user_id");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        private async Task<PositionRegistrationResponseDTO?> GetApplicationResponse(int id)
        {
            var application = await _context.PositionApplications
                .Include(a => a.Addresses)
                .Include(a => a.Qualifications)
                .Include(a => a.Experiences)
                .Include(a => a.Documents)
                .Include(a => a.AssignedJuniorEngineer) // For workflow info
                .Include(a => a.AssignmentHistories) // For workflow timeline
                .Include(a => a.Appointments) // For workflow info
                .Include(a => a.DocumentVerifications) // For workflow info
                .Include(a => a.DigitalSignatures) // For workflow info
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return null;
            }

            return MapToResponse(application);
        }

        private PositionRegistrationResponseDTO MapToResponse(PositionApplication application)
        {
            var age = DateTime.Today.Year - application.DateOfBirth.Year;
            if (application.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            var response = new PositionRegistrationResponseDTO
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber ?? "",
                PositionType = application.PositionType,
                PositionTypeName = FormatEnumName(application.PositionType.ToString()),
                FirstName = application.FirstName,
                MiddleName = application.MiddleName,
                LastName = application.LastName,
                FullName = $"{application.FirstName} {application.MiddleName} {application.LastName}".Replace("  ", " ").Trim(),
                MotherName = application.MotherName,
                MobileNumber = application.MobileNumber,
                EmailAddress = application.EmailAddress,
                BloodGroup = application.BloodGroup,
                Height = application.Height > 0 ? application.Height : null,
                Gender = application.Gender,
                GenderName = application.Gender.ToString(),
                DateOfBirth = application.DateOfBirth,
                Age = age,
                PanCardNumber = application.PanCardNumber,
                AadharCardNumber = application.AadharCardNumber,
                CoaCardNumber = application.CoaCardNumber,
                Status = application.Status,
                StatusName = FormatEnumName(application.Status.ToString()),
                SubmittedDate = application.SubmittedDate,
                ApprovedDate = application.ApprovedDate,
                Remarks = application.Remarks,
                CreatedDate = application.CreatedDate,
                UpdatedDate = application.UpdatedDate,
                
                // Payment Information - Derive from status and transaction data
                IsPaymentComplete = application.Status == ApplicationCurrentStatus.PaymentCompleted || 
                                   application.Status > ApplicationCurrentStatus.PaymentCompleted,
                PaymentCompletedDate = GetPaymentCompletedDate(application),
                
                AssignedJuniorEngineerId = application.AssignedJuniorEngineerId,
                AssignedJuniorEngineerName = application.AssignedJuniorEngineer?.Name,
                Addresses = application.Addresses.Select(a => new AddressResponseDTO
                {
                    Id = a.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    AddressLine3 = a.AddressLine3,
                    City = a.City,
                    State = a.State,
                    Country = a.Country,
                    PinCode = a.PinCode,
                    FullAddress = $"{a.AddressLine1}, {a.AddressLine2}, {a.AddressLine3}, {a.City}, {a.State}, {a.Country} - {a.PinCode}"
                        .Replace(", ,", ",").Replace(",,", ",").Trim()
                }).ToList(),
                Qualifications = application.Qualifications.Select(q => new QualificationResponseDTO
                {
                    Id = q.Id,
                    FileId = q.FileId,
                    InstituteName = q.InstituteName,
                    UniversityName = q.UniversityName,
                    Specialization = q.Specialization,
                    SpecializationName = q.Specialization.ToString(),
                    DegreeName = q.DegreeName,
                    PassingMonth = q.PassingMonth,
                    PassingMonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(q.PassingMonth),
                    YearOfPassing = q.YearOfPassing.Year
                }).ToList(),
                Experiences = application.Experiences.Select(e => new ExperienceResponseDTO
                {
                    Id = e.Id,
                    FileId = e.FileId,
                    CompanyName = e.CompanyName,
                    Position = e.Position,
                    YearsOfExperience = e.YearsOfExperience,
                    FromDate = e.FromDate,
                    ToDate = e.ToDate
                }).ToList(),
                Documents = application.Documents.Select(d => new DocumentResponseDTO
                {
                    Id = d.Id,
                    FileId = d.FileId,
                    DocumentType = d.DocumentType,
                    DocumentTypeName = d.DocumentType.ToString(),
                    FileName = d.FileName,
                    FilePath = d.FilePath, // Deprecated - may be null
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    IsVerified = d.IsVerified,
                    VerifiedDate = d.VerifiedDate,
                    VerificationRemarks = d.VerificationRemarks,
                    FileBase64 = d.FileContent != null && d.FileContent.Length > 0 
                        ? Convert.ToBase64String(d.FileContent) 
                        : null
                }).ToList()
            };

            // Add recommendation form PDF if it exists (separate from documents)
            var recommendationForm = application.Documents
                .FirstOrDefault(d => d.DocumentType == SEDocumentType.RecommendedForm);
            
            if (recommendationForm != null && recommendationForm.FileContent != null && recommendationForm.FileContent.Length > 0)
            {
                response.RecommendationForm = new RecommendationFormDTO
                {
                    DocumentId = recommendationForm.Id,
                    FileName = recommendationForm.FileName,
                    FileId = recommendationForm.FileId,
                    FileSize = recommendationForm.FileSize ?? 0,
                    ContentType = recommendationForm.ContentType ?? "application/pdf",
                    PdfBase64 = Convert.ToBase64String(recommendationForm.FileContent),
                    CreatedDate = recommendationForm.CreatedDate
                };
            }

            // Add JE workflow information if application is in JE stage
            if (IsInJEWorkflowStage(application.Status))
            {
                response.WorkflowInfo = BuildWorkflowInfo(application);
            }

            return response;
        }

        private List<string> ValidateRequest(PositionRegistrationRequestDTO request)
        {
            var errors = new List<string>();

            // Age validation
            var age = DateTime.Today.Year - request.DateOfBirth.Year;
            if (request.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            if (age < 18)
            {
                errors.Add("Applicant must be at least 18 years old");
            }

            if (age > 70)
            {
                errors.Add("Applicant must be below 70 years old");
            }

            // Date of birth not in future
            if (request.DateOfBirth.Date > DateTime.Today)
            {
                errors.Add("Date of birth cannot be in the future");
            }

            // Experience date validations
            foreach (var exp in request.Experiences)
            {
                if (exp.FromDate >= exp.ToDate)
                {
                    errors.Add($"Experience 'From Date' must be before 'To Date' for {exp.CompanyName}");
                }

                if (exp.FromDate.Date > DateTime.Today)
                {
                    errors.Add($"Experience 'From Date' cannot be in the future for {exp.CompanyName}");
                }

                if (exp.ToDate.Date > DateTime.Today)
                {
                    errors.Add($"Experience 'To Date' cannot be in the future for {exp.CompanyName}");
                }
            }

            // Qualification year validation
            foreach (var qual in request.Qualifications)
            {
                if (qual.YearOfPassing < 1950 || qual.YearOfPassing > DateTime.Today.Year)
                {
                    errors.Add($"Invalid year of passing for {qual.DegreeName}");
                }
            }

            return errors;
        }

        private async Task<string> GenerateApplicationNumber(PositionType positionType)
        {
            var prefix = positionType switch
            {
                PositionType.Architect => "ARC",
                PositionType.LicenceEngineer => "LIC",
                PositionType.StructuralEngineer => "SE",
                PositionType.Supervisor1 => "SUP1",
                PositionType.Supervisor2 => "SUP2",
                _ => "APP"
            };

            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month.ToString("D2");

            // Retry logic to handle concurrent application submissions
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var count = await _context.PositionApplications
                    .Where(a => a.PositionType == positionType &&
                               a.ApplicationNumber != null &&
                               a.ApplicationNumber.StartsWith($"{prefix}{year}{month}"))
                    .CountAsync();

                var sequence = (count + 1 + attempt).ToString("D4");
                var applicationNumber = $"{prefix}{year}{month}{sequence}";

                // Check if this number is already taken
                var exists = await _context.PositionApplications
                    .AnyAsync(a => a.ApplicationNumber == applicationNumber);

                if (!exists)
                {
                    return applicationNumber;
                }

                // If exists, retry with next sequence
                _logger.LogWarning("Application number {ApplicationNumber} already exists, retrying with next sequence (attempt {Attempt})",
                    applicationNumber, attempt + 1);
            }

            // Fallback: use timestamp-based suffix if all retries fail
            var timestamp = DateTime.Now.ToString("HHmmss");
            return $"{prefix}{year}{month}{timestamp}";
        }

        private decimal CalculateYearsOfExperience(DateTime fromDate, DateTime toDate)
        {
            var totalDays = (toDate - fromDate).TotalDays;
            var years = (decimal)(totalDays / 365.25);
            return Math.Round(years, 2);
        }

        /// <summary>
        /// Check if application status is in JE workflow stage
        /// </summary>
        private bool IsInJEWorkflowStage(ApplicationCurrentStatus status)
        {
            return status == ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING ||
                   status == ApplicationCurrentStatus.APPOINTMENT_SCHEDULED ||
                   status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING ||
                   status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS ||
                   status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED ||
                   status == ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE;
        }

        /// <summary>
        /// Build workflow information for application
        /// </summary>
        private JEWorkflowInfo BuildWorkflowInfo(PositionApplication application)
        {
            var workflowInfo = new JEWorkflowInfo
            {
                AssignedJuniorEngineerId = application.AssignedJuniorEngineerId,
                AssignedJuniorEngineerName = application.AssignedJuniorEngineer?.Name,
                AssignedJuniorEngineerEmail = application.AssignedJuniorEngineer?.Email,
                CurrentStage = GetWorkflowStage(application.Status),
                NextAction = GetNextWorkflowAction(application.Status)
            };

            // Get assignment date from assignment history
            var assignment = application.AssignmentHistories
                .Where(ah => ah.AssignedToOfficerId == application.AssignedJuniorEngineerId)
                .OrderByDescending(ah => ah.AssignedDate)
                .FirstOrDefault();
            
            workflowInfo.AssignedDate = assignment?.AssignedDate;

            // Calculate progress percentage
            workflowInfo.ProgressPercentage = CalculateJEWorkflowProgress(application.Status);

            // Get appointment information - get the latest scheduled appointment
            var appointment = application.Appointments
                .Where(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                .OrderByDescending(a => a.ReviewDate)
                .FirstOrDefault();
            
            if (appointment != null)
            {
                workflowInfo.HasAppointment = true;
                workflowInfo.AppointmentId = appointment.Id;
                workflowInfo.AppointmentDate = appointment.ReviewDate;
                workflowInfo.AppointmentPlace = appointment.Place;
                workflowInfo.AppointmentRoomNumber = appointment.RoomNumber;
                workflowInfo.AppointmentContactPerson = appointment.ContactPerson;
                workflowInfo.AppointmentComments = appointment.Comments;
            }

            // Get document verification information
            var documentVerifications = application.DocumentVerifications.ToList();
            workflowInfo.TotalDocumentsCount = application.Documents.Count;
            workflowInfo.VerifiedDocumentsCount = documentVerifications
                .Count(dv => dv.Status == Models.VerificationStatus.Approved);
            workflowInfo.AllDocumentsVerified = application.JEAllDocumentsVerified;

            // Get digital signature information
            var digitalSignature = application.DigitalSignatures
                .OrderByDescending(ds => ds.CreatedDate)
                .FirstOrDefault();
            
            if (digitalSignature != null)
            {
                workflowInfo.HasDigitalSignature = true;
                workflowInfo.SignatureCompletedDate = digitalSignature.SignedDate;
            }

            // Build timeline
            workflowInfo.Timeline = BuildWorkflowTimeline(application);

            return workflowInfo;
        }

        /// <summary>
        /// Get human-readable workflow stage
        /// </summary>
        private string GetWorkflowStage(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => "Assignment Pending",
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => "Appointment Scheduled",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => "Awaiting Document Verification",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => "Documents Being Verified",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => "Documents Verified",
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => "Awaiting JE Signature",
                _ => status.ToString()
            };
        }

        /// <summary>
        /// Get next recommended workflow action
        /// </summary>
        private string GetNextWorkflowAction(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => "Assign to Junior Engineer",
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => "Complete Appointment",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => "Start Document Verification",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => "Complete Document Verification",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => "Initiate Digital Signature",
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => "Complete Digital Signature",
                _ => "No action required"
            };
        }

        /// <summary>
        /// Calculate JE workflow progress percentage (0-100)
        /// </summary>
        private int CalculateJEWorkflowProgress(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => 0,
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => 20,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => 40,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => 60,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => 80,
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => 90,
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => 100,
                _ => 0
            };
        }

        /// <summary>
        /// Build workflow timeline from application history
        /// </summary>
        private List<WorkflowTimelineEvent> BuildWorkflowTimeline(PositionApplication application)
        {
            var timeline = new List<WorkflowTimelineEvent>();

            // Application submission
            if (application.SubmittedDate.HasValue)
            {
                timeline.Add(new WorkflowTimelineEvent
                {
                    EventType = "Submission",
                    Description = "Application submitted",
                    Timestamp = application.SubmittedDate.Value,
                    PerformedBy = $"{application.FirstName} {application.LastName}"
                });
            }

            // JE Assignment
            var jeAssignment = application.AssignmentHistories
                .Where(ah => ah.AssignedToOfficerId == application.AssignedJuniorEngineerId)
                .OrderBy(ah => ah.AssignedDate)
                .FirstOrDefault();
            
            if (jeAssignment != null)
            {
                timeline.Add(new WorkflowTimelineEvent
                {
                    EventType = "Assignment",
                    Description = $"Assigned to {application.AssignedJuniorEngineer?.Name ?? "Junior Engineer"}",
                    Timestamp = jeAssignment.AssignedDate,
                    PerformedBy = "Auto-Assignment System"
                });
            }

            // Appointments
            foreach (var appointment in application.Appointments.OrderBy(a => a.ReviewDate))
            {
                timeline.Add(new WorkflowTimelineEvent
                {
                    EventType = "Appointment",
                    Description = $"Appointment scheduled at {appointment.Place}",
                    Timestamp = appointment.CreatedDate,
                    PerformedBy = application.AssignedJuniorEngineer?.Name
                });

                if (appointment.Status == Models.AppointmentStatus.Completed)
                {
                    timeline.Add(new WorkflowTimelineEvent
                    {
                        EventType = "AppointmentCompleted",
                        Description = "Appointment completed",
                        Timestamp = appointment.UpdatedDate ?? appointment.CreatedDate,
                        PerformedBy = application.AssignedJuniorEngineer?.Name
                    });
                }
            }

            // Document Verifications
            var verifications = application.DocumentVerifications
                .Where(dv => dv.Status == Models.VerificationStatus.Approved || 
                            dv.Status == Models.VerificationStatus.Rejected)
                .OrderBy(dv => dv.VerifiedDate);
            
            foreach (var verification in verifications)
            {
                var document = application.Documents.FirstOrDefault(d => d.Id == verification.DocumentId);
                timeline.Add(new WorkflowTimelineEvent
                {
                    EventType = verification.Status == Models.VerificationStatus.Approved ? "DocumentVerified" : "DocumentRejected",
                    Description = $"Document {document?.DocumentType.ToString() ?? "Unknown"} {(verification.Status == Models.VerificationStatus.Approved ? "verified" : "rejected")}",
                    Timestamp = verification.VerifiedDate ?? verification.CreatedDate,
                    PerformedBy = application.AssignedJuniorEngineer?.Name
                });
            }

            // Digital Signatures
            foreach (var signature in application.DigitalSignatures.OrderBy(ds => ds.CreatedDate))
            {
                timeline.Add(new WorkflowTimelineEvent
                {
                    EventType = "SignatureInitiated",
                    Description = "Digital signature process initiated",
                    Timestamp = signature.CreatedDate,
                    PerformedBy = application.AssignedJuniorEngineer?.Name
                });

                if (signature.SignedDate.HasValue)
                {
                    timeline.Add(new WorkflowTimelineEvent
                    {
                        EventType = "SignatureCompleted",
                        Description = "Digital signature completed",
                        Timestamp = signature.SignedDate.Value,
                        PerformedBy = application.AssignedJuniorEngineer?.Name
                    });
                }
            }

            return timeline.OrderBy(t => t.Timestamp).ToList();
        }

        /// <summary>
        /// Get payment completed date from Transaction table for the application
        /// </summary>
        private DateTime? GetPaymentCompletedDate(PositionApplication application)
        {
            var successfulTransaction = _context.Transactions
                .Where(t => t.ApplicationId == application.Id && t.Status == "SUCCESS")
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefault();

            return successfulTransaction?.UpdatedAt;
        }

        #endregion
    }
}

