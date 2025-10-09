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
    public class ApplicationsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(PMCRMSDbContext context, ILogger<ApplicationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApplicationDto>>>> GetApplications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var query = _context.Applications.AsQueryable();

                // Filter by user role
                if (userRole == UserRole.Applicant)
                {
                    query = query.Where(a => a.ApplicantId == userId);
                }

                var totalCount = await query.CountAsync();
                var applications = await query
                    .Include(a => a.Applicant)
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var applicationDtos = applications.Select(app => new ApplicationDto
                {
                    Id = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    ApplicantId = app.ApplicantId,
                    ApplicantName = app.Applicant?.Name ?? "Unknown",
                    ApplicationType = app.Type.ToString(),
                    ProjectTitle = app.ProjectTitle,
                    ProjectDescription = app.ProjectDescription,
                    SiteAddress = app.SiteAddress,
                    PlotArea = app.PlotArea,
                    BuiltUpArea = app.BuiltUpArea,
                    EstimatedCost = app.EstimatedCost,
                    CurrentStatus = app.CurrentStatus.ToString(),
                    CreatedAt = app.CreatedDate,
                    UpdatedAt = app.UpdatedDate ?? app.CreatedDate,
                    CertificateIssuedDate = app.CertificateIssuedDate,
                    CertificateNumber = app.CertificateNumber
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<ApplicationDto>>
                {
                    Success = true,
                    Message = $"Applications retrieved successfully. Total: {totalCount}, Page: {page}/{(int)Math.Ceiling((double)totalCount / pageSize)}",
                    Data = applicationDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve applications",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ApplicationDto>>> GetApplication(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .Include(a => a.Documents)
                    .Include(a => a.StatusHistory)
                    .Include(a => a.Comments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                // Check permissions
                if (userRole == UserRole.Applicant && application.ApplicantId != userId)
                {
                    return Forbid();
                }

                var applicationDto = new ApplicationDto
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    ApplicantId = application.ApplicantId,
                    ApplicantName = application.Applicant?.Name ?? "Unknown",
                    ApplicationType = application.Type.ToString(),
                    ProjectTitle = application.ProjectTitle,
                    ProjectDescription = application.ProjectDescription,
                    SiteAddress = application.SiteAddress,
                    PlotArea = application.PlotArea,
                    BuiltUpArea = application.BuiltUpArea,
                    EstimatedCost = application.EstimatedCost,
                    CurrentStatus = application.CurrentStatus.ToString(),
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate,
                    CertificateIssuedDate = application.CertificateIssuedDate,
                    CertificateNumber = application.CertificateNumber,
                    Documents = application.Documents.Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.Type.ToString(),
                        FileSize = d.FileSize,
                        UploadedAt = d.CreatedDate
                    }).ToList(),
                    StatusHistory = application.StatusHistory.Select(s => new StatusHistoryDto
                    {
                        Status = s.Status.ToString(),
                        Comments = s.Remarks ?? "",
                        CreatedAt = s.StatusDate,
                        CreatedBy = s.UpdatedByUserId.ToString()
                    }).OrderByDescending(s => s.CreatedAt).ToList()
                };

                return Ok(new ApiResponse<ApplicationDto>
                {
                    Success = true,
                    Message = "Application retrieved successfully",
                    Data = applicationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve application",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ApplicationDto>>> CreateApplication([FromBody] CreateApplicationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Generate application number
                var count = await _context.Applications.CountAsync() + 1;
                var applicationNumber = $"PMCRMS{DateTime.Now.Year}{count:D6}";

                var application = new Application
                {
                    ApplicationNumber = applicationNumber,
                    Type = Enum.Parse<ApplicationType>(request.ApplicationType),
                    ApplicantId = userId,
                    ProjectTitle = request.ProjectTitle,
                    ProjectDescription = request.ProjectDescription,
                    SiteAddress = request.SiteAddress,
                    PlotArea = request.PlotArea,
                    BuiltUpArea = request.BuiltUpArea,
                    EstimatedCost = request.EstimatedCost,
                    CurrentStatus = ApplicationCurrentStatus.Draft,
                    CreatedBy = userId.ToString()
                };

                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                // Add initial status
                var initialStatus = new ApplicationStatus
                {
                    ApplicationId = application.Id,
                    Status = ApplicationCurrentStatus.Draft,
                    UpdatedByUserId = userId,
                    Remarks = "Application created",
                    StatusDate = DateTime.UtcNow,
                    CreatedBy = userId.ToString()
                };

                _context.ApplicationStatuses.Add(initialStatus);
                await _context.SaveChangesAsync();

                // Reload with includes
                application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == application.Id);

                var applicationDto = new ApplicationDto
                {
                    Id = application!.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    ApplicantId = application.ApplicantId,
                    ApplicantName = application.Applicant?.Name ?? "Unknown",
                    ApplicationType = application.Type.ToString(),
                    ProjectTitle = application.ProjectTitle,
                    ProjectDescription = application.ProjectDescription,
                    SiteAddress = application.SiteAddress,
                    PlotArea = application.PlotArea,
                    BuiltUpArea = application.BuiltUpArea,
                    EstimatedCost = application.EstimatedCost,
                    CurrentStatus = application.CurrentStatus.ToString(),
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate
                };

                _logger.LogInformation("Application {ApplicationId} created by user {UserId}", application.Id, userId);

                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, new ApiResponse<ApplicationDto>
                {
                    Success = true,
                    Message = "Application created successfully",
                    Data = applicationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to create application",
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
    }
}