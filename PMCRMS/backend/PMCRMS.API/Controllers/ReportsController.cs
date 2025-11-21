using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(PMCRMSDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get position-level summary with application counts
        /// </summary>
        [HttpGet("positions")]
        public async Task<ActionResult> GetPositionSummaries()
        {
            try
            {
                _logger.LogInformation("Fetching position summaries for reports");

                // Get all applications
                var allApplications = await _context.PositionApplications.ToListAsync();

                // Get all position types from enum
                var allPositionTypes = Enum.GetValues<PositionType>();

                // Create position summaries for all position types, including those with zero applications
                var positions = allPositionTypes.Select(positionType =>
                {
                    var applicationsForPosition = allApplications.Where(a => a.PositionType == positionType).ToList();
                    
                    return new
                    {
                        positionType = positionType.ToString(),
                        positionName = FormatPositionName(positionType.ToString()),
                        totalApplications = applicationsForPosition.Count,
                        pendingCount = applicationsForPosition.Count(a => 
                            a.Status == ApplicationCurrentStatus.Draft || 
                            a.Status == ApplicationCurrentStatus.Submitted ||
                            a.Status == ApplicationCurrentStatus.PaymentPending),
                        approvedCount = applicationsForPosition.Count(a => 
                            a.Status == ApplicationCurrentStatus.Completed ||
                            a.Status == ApplicationCurrentStatus.CertificateIssued),
                        rejectedCount = applicationsForPosition.Count(a => 
                            a.Status == ApplicationCurrentStatus.REJECTED ||
                            a.Status == ApplicationCurrentStatus.RejectedByJE ||
                            a.Status == ApplicationCurrentStatus.RejectedByAE ||
                            a.Status == ApplicationCurrentStatus.RejectedByEE1 ||
                            a.Status == ApplicationCurrentStatus.RejectedByCE1),
                        underReviewCount = applicationsForPosition.Count(a => 
                            a.Status.ToString().Contains("UnderReview")),
                        inProgressCount = applicationsForPosition.Count(a => 
                            a.Status.ToString().Contains("UnderProcessing") ||
                            a.Status.ToString().Contains("Stage"))
                    };
                }).ToList();

                var data = new
                {
                    positions = positions.OrderBy(p => p.positionType).ToList(),
                    totalPositions = positions.Count,
                    totalApplications = positions.Sum(p => p.totalApplications)
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Position summaries retrieved successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching position summaries");
                return StatusCode(500, new { success = false, message = "Failed to fetch position summaries" });
            }
        }

        /// <summary>
        /// Get stage-level summary for a specific position
        /// </summary>
        [HttpGet("positions/{positionType}/stages")]
        public async Task<ActionResult> GetStageSummaries(string positionType)
        {
            try
            {
                _logger.LogInformation("Fetching stage summaries for position: {PositionType}", positionType);

                if (!Enum.TryParse<PositionType>(positionType, true, out var positionEnum))
                {
                    return BadRequest(new { success = false, message = $"Invalid position type: {positionType}" });
                }

                var applications = await _context.PositionApplications
                    .Where(a => a.PositionType == positionEnum)
                    .ToListAsync();

                // Group by status as "stages" - kept simple to show all current statuses
                var stages = applications
                    .GroupBy(a => a.Status)
                    .Select(g => new
                    {
                        stageName = g.Key.ToString(),
                        stageDisplayName = FormatStatusDisplayName(g.Key.ToString()),
                        applicationCount = g.Count(),
                        avgProcessingDays = g.Where(a => a.UpdatedDate.HasValue && a.UpdatedDate.HasValue)
                                            .Select(a => (a.UpdatedDate!.Value - a.CreatedDate).TotalDays)
                                            .DefaultIfEmpty(0)
                                            .Average(),
                        oldestApplicationDate = g.Min(a => a.CreatedDate),
                        newestApplicationDate = g.Max(a => a.CreatedDate)
                    })
                    .OrderByDescending(s => s.applicationCount)
                    .ToList();

                var data = new
                {
                    positionType = positionType,
                    positionName = FormatPositionName(positionType),
                    stages = stages,
                    totalStages = stages.Count,
                    totalApplications = applications.Count
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Stage summaries retrieved successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stage summaries for position {PositionType}", positionType);
                return StatusCode(500, new { success = false, message = "Failed to fetch stage summaries" });
            }
        }

        /// <summary>
        /// Get detailed applications list for a specific position and stage
        /// </summary>
        [HttpGet("positions/{positionType}/stages/{stageName}/applications")]
        public async Task<ActionResult> GetApplicationsByStage(
            string positionType,
            string stageName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = "createdDate",
            [FromQuery] string? sortDirection = "desc")
        {
            try
            {
                _logger.LogInformation("Fetching applications for position: {PositionType}, stage: {StageName}", positionType, stageName);

                if (!Enum.TryParse<PositionType>(positionType, true, out var positionEnum))
                {
                    return BadRequest(new { success = false, message = $"Invalid position type: {positionType}" });
                }

                if (!Enum.TryParse<ApplicationCurrentStatus>(stageName, true, out var statusEnum))
                {
                    return BadRequest(new { success = false, message = $"Invalid stage name: {stageName}" });
                }

                var query = _context.PositionApplications
                    .Where(a => a.PositionType == positionEnum && a.Status == statusEnum)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(a =>
                        (a.ApplicationNumber != null && a.ApplicationNumber.Contains(searchTerm)) ||
                        a.FirstName.Contains(searchTerm) ||
                        a.LastName.Contains(searchTerm) ||
                        a.EmailAddress.Contains(searchTerm) ||
                        a.MobileNumber.Contains(searchTerm));
                }

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "applicationnumber" => sortDirection == "asc" 
                        ? query.OrderBy(a => a.ApplicationNumber)
                        : query.OrderByDescending(a => a.ApplicationNumber),
                    "applicantname" => sortDirection == "asc"
                        ? query.OrderBy(a => a.FirstName).ThenBy(a => a.LastName)
                        : query.OrderByDescending(a => a.FirstName).ThenByDescending(a => a.LastName),
                    "createddate" or _ => sortDirection == "asc"
                        ? query.OrderBy(a => a.CreatedDate)
                        : query.OrderByDescending(a => a.CreatedDate)
                };

                var totalCount = await query.CountAsync();
                var applications = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var applicationDtos = applications.Select(a => new
                {
                    applicationId = a.Id,
                    applicationNumber = a.ApplicationNumber ?? "N/A",
                    firstName = a.FirstName,
                    lastName = a.LastName,
                    positionType = a.PositionType.ToString(),
                    currentStage = a.Status.ToString(),
                    submittedDate = a.CreatedDate.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    createdDate = a.CreatedDate.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                }).ToList();

                var data = new
                {
                    positionType = positionType,
                    positionName = FormatPositionName(positionType),
                    stageName = stageName,
                    stageDisplayName = FormatStatusDisplayName(stageName),
                    applications = applicationDtos,
                    pagination = new
                    {
                        currentPage = pageNumber,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Applications retrieved successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching applications for position {PositionType}, stage {StageName}", positionType, stageName);
                return StatusCode(500, new { success = false, message = "Failed to fetch applications" });
            }
        }

        /// <summary>
        /// Format position type to display name
        /// </summary>
        private string FormatPositionName(string positionType)
        {
            return positionType switch
            {
                "Architect" => "Architect Applications",
                "LicenceEngineer" => "Licence Engineer Applications",
                "StructuralEngineer" => "Structural Engineer Applications",
                "Supervisor1" => "Supervisor 1 Applications",
                "Supervisor2" => "Supervisor 2 Applications",
                _ => positionType + " Applications"
            };
        }

        /// <summary>
        /// Format status to display name
        /// </summary>
        private string FormatStatusDisplayName(string status)
        {
            // Convert PascalCase or camelCase to Title Case with spaces
            return System.Text.RegularExpressions.Regex.Replace(status, "([a-z])([A-Z])", "$1 $2");
        }
    }
}
