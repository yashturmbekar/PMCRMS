using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/junior-engineer")]
    [Authorize]
    public class JuniorEngineerController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<JuniorEngineerController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowRoutingService _workflowRoutingService;

        public JuniorEngineerController(
            PMCRMSDbContext context,
            ILogger<JuniorEngineerController> logger,
            INotificationService notificationService,
            IWorkflowRoutingService workflowRoutingService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _workflowRoutingService = workflowRoutingService;
        }

        /// <summary>
        /// Get applications assigned to current Junior Engineer
        /// </summary>
        [HttpGet("applications")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetAssignedApplications(
            [FromQuery] ApplicationCurrentStatus? status = null)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var query = _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.Documents)
                    .Where(a => a.AssignedOfficerId == officerId);

                if (status.HasValue)
                {
                    query = query.Where(a => a.Status == status.Value);
                }

                var applications = await query
                    .OrderByDescending(a => a.SubmittedDate)
                    .Select(a => new
                    {
                        id = a.Id,
                        applicationNumber = a.ApplicationNumber,
                        applicantName = $"{a.FirstName} {a.LastName}",
                        positionType = a.PositionType.ToString(),
                        submittedDate = a.SubmittedDate,
                        status = a.Status.ToString(),
                        email = a.EmailAddress,
                        mobileNumber = a.MobileNumber,
                        documentsCount = a.Documents.Count,
                        verifiedDocumentsCount = a.Documents.Count(d => d.IsVerified)
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Applications retrieved successfully",
                    Data = applications.Cast<object>().ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications for Junior Engineer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get application details for verification
        /// </summary>
        [HttpGet("applications/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetApplicationDetails(int id)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .Include(a => a.Addresses)
                    .Include(a => a.Qualifications)
                    .Include(a => a.Experiences)
                    .Include(a => a.Documents)
                        .ThenInclude(d => d.VerifiedByOfficer)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                var result = new
                {
                    id = application.Id,
                    applicationNumber = application.ApplicationNumber,
                    firstName = application.FirstName,
                    middleName = application.MiddleName,
                    lastName = application.LastName,
                    motherName = application.MotherName,
                    mobileNumber = application.MobileNumber,
                    emailAddress = application.EmailAddress,
                    positionType = application.PositionType.ToString(),
                    bloodGroup = application.BloodGroup,
                    height = application.Height,
                    gender = application.Gender.ToString(),
                    dateOfBirth = application.DateOfBirth,
                    panCardNumber = application.PanCardNumber,
                    aadharCardNumber = application.AadharCardNumber,
                    coaCardNumber = application.CoaCardNumber,
                    status = application.Status.ToString(),
                    submittedDate = application.SubmittedDate,
                    addresses = application.Addresses.Select(addr => new
                    {
                        id = addr.Id,
                        addressType = addr.AddressType,
                        addressLine1 = addr.AddressLine1,
                        addressLine2 = addr.AddressLine2,
                        addressLine3 = addr.AddressLine3,
                        city = addr.City,
                        state = addr.State,
                        country = addr.Country,
                        pinCode = addr.PinCode
                    }),
                    qualifications = application.Qualifications.Select(q => new
                    {
                        id = q.Id,
                        fileId = q.FileId,
                        instituteName = q.InstituteName,
                        universityName = q.UniversityName,
                        specialization = q.Specialization.ToString(),
                        degreeName = q.DegreeName,
                        passingMonth = q.PassingMonth,
                        yearOfPassing = q.YearOfPassing.Year
                    }),
                    experiences = application.Experiences.Select(e => new
                    {
                        id = e.Id,
                        fileId = e.FileId,
                        companyName = e.CompanyName,
                        position = e.Position,
                        yearsOfExperience = e.YearsOfExperience,
                        fromDate = e.FromDate,
                        toDate = e.ToDate
                    }),
                    documents = application.Documents.Select(d => new
                    {
                        id = d.Id,
                        fileId = d.FileId,
                        documentType = d.DocumentType.ToString(),
                        fileName = d.FileName,
                        filePath = d.FilePath,
                        fileSize = d.FileSize,
                        contentType = d.ContentType,
                        isVerified = d.IsVerified,
                        verifiedBy = d.VerifiedByOfficer?.Name,
                        verifiedDate = d.VerifiedDate,
                        verificationRemarks = d.VerificationRemarks
                    })
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Application details retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application details {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve application details",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Verify or reject a document
        /// </summary>
        [HttpPut("documents/{documentId}/verify")]
        public async Task<ActionResult<ApiResponse>> VerifyDocument(
            int documentId,
            [FromBody] VerifyDocumentRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var document = await _context.SEDocuments.FindAsync(documentId);
                if (document == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Document not found"
                    });
                }

                document.IsVerified = request.IsVerified;
                document.VerifiedBy = officerId;
                document.VerifiedDate = DateTime.UtcNow;
                document.VerificationRemarks = request.Remarks;
                document.UpdatedDate = DateTime.UtcNow;
                document.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} verification status updated to {Status} by officer {OfficerId}",
                    documentId, request.IsVerified, officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Document verification status updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document {DocumentId}", documentId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to verify document",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Approve application and forward to Assistant Engineer
        /// </summary>
        [HttpPost("applications/{id}/approve")]
        public async Task<ActionResult<ApiResponse>> ApproveApplication(
            int id,
            [FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var application = await _context.PositionApplications
                    .Include(a => a.User)
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

                // Validate all documents are verified
                var allDocumentsVerified = application.Documents.All(d => d.IsVerified);
                if (!allDocumentsVerified)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "All documents must be verified before approval"
                    });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.ApprovedByJE);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status transition"
                    });
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.ApprovedByJE;
                application.Remarks = request.Remarks;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                // Get and assign Assistant Engineer
                var assistantEngineer = await _workflowRoutingService.GetAssistantEngineerForPosition(application.PositionType);
                if (assistantEngineer != null)
                {
                    application.AssignedOfficerId = assistantEngineer.Id;
                    application.AssignedOfficerName = assistantEngineer.Name;
                    application.AssignedOfficerRole = assistantEngineer.Role.ToString();
                    application.AssignedDate = DateTime.UtcNow;

                    // Change status to under review by AE
                    application.Status = ApplicationCurrentStatus.UnderReviewByAE;
                }

                await _context.SaveChangesAsync();

                // Send notification to applicant
                await _notificationService.NotifyApplicationApprovalAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Junior Engineer",
                    "Junior Engineer",
                    request.Remarks ?? "Your application has been approved and forwarded to Assistant Engineer"
                );

                _logger.LogInformation("Application {ApplicationNumber} approved by Junior Engineer {OfficerId}",
                    application.ApplicationNumber, officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application approved successfully and forwarded to Assistant Engineer"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to approve application",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Reject application and send back to applicant
        /// </summary>
        [HttpPost("applications/{id}/reject")]
        public async Task<ActionResult<ApiResponse>> RejectApplication(
            int id,
            [FromBody] RejectApplicationRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Rejection reason is required"
                    });
                }

                // Validate status transition
                var isValid = await _workflowRoutingService.ValidateStatusTransition(
                    application.Status, ApplicationCurrentStatus.RejectedByJE);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid status transition"
                    });
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.RejectedByJE;
                application.Remarks = request.RejectionReason;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                // Send notification to applicant
                await _notificationService.NotifyApplicationRejectionAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    User.FindFirst("name")?.Value ?? "Junior Engineer",
                    "Junior Engineer",
                    request.RejectionReason
                );

                _logger.LogInformation("Application {ApplicationNumber} rejected by Junior Engineer {OfficerId}",
                    application.ApplicationNumber, officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Application rejected. Applicant has been notified."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to reject application",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private int GetCurrentOfficerId()
        {
            var officerIdClaim = HttpContext.User.FindFirst("officer_id") ?? 
                                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(officerIdClaim?.Value ?? "0");
        }
    }

    // DTOs for Junior Engineer operations
    public class VerifyDocumentRequest
    {
        public bool IsVerified { get; set; }
        public string? Remarks { get; set; }
    }

    public class ApproveApplicationRequest
    {
        public string? Remarks { get; set; }
    }

    public class RejectApplicationRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
