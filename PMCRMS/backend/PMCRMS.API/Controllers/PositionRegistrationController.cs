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

        public PositionRegistrationController(
            PMCRMSDbContext context,
            ILogger<PositionRegistrationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/PositionRegistration
        [HttpPost]
        public async Task<ActionResult<PositionRegistrationResponseDTO>> CreateApplication(
            [FromBody] PositionRegistrationRequestDTO request)
        {
            try
            {
                // Additional custom validations
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Check for duplicate PAN
                var existingPan = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.PanCardNumber == request.PanCardNumber.ToUpper());
                if (existingPan)
                {
                    return BadRequest(new { error = "An application with this PAN card number already exists" });
                }

                // Check for duplicate Aadhar
                var existingAadhar = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.AadharCardNumber == request.AadharCardNumber);
                if (existingAadhar)
                {
                    return BadRequest(new { error = "An application with this Aadhar card number already exists" });
                }

                // Check for duplicate Email
                var existingEmail = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.EmailAddress.ToLower() == request.EmailAddress.ToLower());
                if (existingEmail)
                {
                    return BadRequest(new { error = "An application with this email address already exists" });
                }

                // Check for duplicate Mobile
                var existingMobile = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.MobileNumber == request.MobileNumber);
                if (existingMobile)
                {
                    return BadRequest(new { error = "An application with this mobile number already exists" });
                }

                // Get or create user (for now, we'll create a temporary user ID = 1)
                // In production, this should come from authentication context
                var userId = 1;

                // Create application entity
                var application = new StructuralEngineerApplication
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
                    Country = request.LocalAddress.Country.Trim(),
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
                    Country = request.PermanentAddress.Country.Trim(),
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
                        YearOfPassing = new DateTime(qual.YearOfPassing, 1, 1),
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
                        FromDate = exp.FromDate.Date,
                        ToDate = exp.ToDate.Date,
                        YearsOfExperience = yearsOfExperience,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Add documents
                foreach (var doc in request.Documents)
                {
                    application.Documents.Add(new SEDocument
                    {
                        FileId = doc.FileId,
                        DocumentType = doc.DocumentType,
                        FileName = doc.FileName,
                        FilePath = doc.FilePath,
                        FileSize = doc.FileSize,
                        ContentType = doc.ContentType,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                _context.StructuralEngineerApplications.Add(application);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Position registration application created successfully. ID: {Id}", application.Id);

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

        // PUT: api/PositionRegistration/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromBody] PositionRegistrationRequestDTO request)
        {
            try
            {
                var application = await _context.StructuralEngineerApplications
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

                // Additional custom validations
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Check for duplicate PAN (excluding current application)
                var existingPan = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.Id != id && a.PanCardNumber == request.PanCardNumber.ToUpper());
                if (existingPan)
                {
                    return BadRequest(new { error = "An application with this PAN card number already exists" });
                }

                // Check for duplicate Aadhar (excluding current application)
                var existingAadhar = await _context.StructuralEngineerApplications
                    .AnyAsync(a => a.Id != id && a.AadharCardNumber == request.AadharCardNumber);
                if (existingAadhar)
                {
                    return BadRequest(new { error = "An application with this Aadhar card number already exists" });
                }

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
                    Country = request.LocalAddress.Country.Trim(),
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
                    Country = request.PermanentAddress.Country.Trim(),
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
                        YearOfPassing = new DateTime(qual.YearOfPassing, 1, 1),
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
                        FromDate = exp.FromDate.Date,
                        ToDate = exp.ToDate.Date,
                        YearsOfExperience = yearsOfExperience,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // Update documents
                _context.SEDocuments.RemoveRange(application.Documents);
                
                foreach (var doc in request.Documents)
                {
                    application.Documents.Add(new SEDocument
                    {
                        FileId = doc.FileId,
                        DocumentType = doc.DocumentType,
                        FileName = doc.FileName,
                        FilePath = doc.FilePath,
                        FileSize = doc.FileSize,
                        ContentType = doc.ContentType,
                        CreatedBy = "User",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Position registration application updated successfully. ID: {Id}", id);

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
                var application = await _context.StructuralEngineerApplications.FindAsync(id);
                if (application == null)
                {
                    return NotFound(new { error = "Application not found" });
                }

                // Only allow deletion of draft applications
                if (application.Status != ApplicationCurrentStatus.Draft)
                {
                    return BadRequest(new { error = "Only draft applications can be deleted" });
                }

                _context.StructuralEngineerApplications.Remove(application);
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

        // GET: api/PositionRegistration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PositionRegistrationResponseDTO>>> GetAllApplications(
            [FromQuery] PositionType? positionType = null,
            [FromQuery] ApplicationCurrentStatus? status = null,
            [FromQuery] int? userId = null)
        {
            try
            {
                var query = _context.StructuralEngineerApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
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

        #region Private Helper Methods

        private async Task<PositionRegistrationResponseDTO?> GetApplicationResponse(int id)
        {
            var application = await _context.StructuralEngineerApplications
                .Include(a => a.Addresses)
                .Include(a => a.Qualifications)
                .Include(a => a.Experiences)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return null;
            }

            return MapToResponse(application);
        }

        private PositionRegistrationResponseDTO MapToResponse(StructuralEngineerApplication application)
        {
            var age = DateTime.Today.Year - application.DateOfBirth.Year;
            if (application.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            return new PositionRegistrationResponseDTO
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber ?? "",
                PositionType = application.PositionType,
                PositionTypeName = application.PositionType.ToString(),
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
                StatusName = application.Status.ToString(),
                SubmittedDate = application.SubmittedDate,
                ApprovedDate = application.ApprovedDate,
                Remarks = application.Remarks,
                CreatedDate = application.CreatedDate,
                UpdatedDate = application.UpdatedDate,
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
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    IsVerified = d.IsVerified,
                    VerifiedDate = d.VerifiedDate,
                    VerificationRemarks = d.VerificationRemarks
                }).ToList()
            };
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

            var count = await _context.StructuralEngineerApplications
                .Where(a => a.PositionType == positionType &&
                           a.ApplicationNumber != null &&
                           a.ApplicationNumber.StartsWith($"{prefix}{year}{month}"))
                .CountAsync();

            var sequence = (count + 1).ToString("D4");

            return $"{prefix}{year}{month}{sequence}";
        }

        private decimal CalculateYearsOfExperience(DateTime fromDate, DateTime toDate)
        {
            var totalDays = (toDate - fromDate).TotalDays;
            var years = (decimal)(totalDays / 365.25);
            return Math.Round(years, 2);
        }

        #endregion
    }
}
