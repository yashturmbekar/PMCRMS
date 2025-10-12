using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(PMCRMSDbContext context, ILogger<DiagnosticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Diagnostic endpoint for testing Officer ID 7 (Assistant Structural Engineer)
        /// </summary>
        [HttpGet("test-officer-7")]
        public async Task<ActionResult> TestOfficer7()
        {
            var officerId = 7;
            var positionType = PositionType.StructuralEngineer;
            
            var result = new
            {
                Test = "Assistant Structural Engineer - Officer ID 7",
                Timestamp = DateTime.UtcNow,
                
                // 1. Officer Details
                Officer = await _context.Officers
                    .Where(o => o.Id == officerId)
                    .Select(o => new
                    {
                        o.Id,
                        o.Name,
                        o.Email,
                        Role = o.Role.ToString(),
                        o.EmployeeId,
                        o.IsActive,
                        o.CreatedDate
                    })
                    .FirstOrDefaultAsync(),
                
                // 2. All Applications Assigned to Officer 7
                AllAssignedApplications = await _context.PositionApplications
                    .Where(pa => pa.AssignedAEStructuralId == officerId)
                    .Select(pa => new
                    {
                        pa.Id,
                        pa.ApplicationNumber,
                        ApplicantName = pa.FirstName + " " + pa.LastName,
                        PositionType = pa.PositionType.ToString(),
                        Status = pa.Status.ToString(),
                        pa.AssignedAEStructuralId,
                        pa.AssignedToAEStructuralDate,
                        pa.AEStructuralApprovalStatus,
                        pa.AEStructuralRejectionStatus,
                        pa.CreatedDate
                    })
                    .OrderByDescending(pa => pa.CreatedDate)
                    .ToListAsync(),
                
                // 3. Applications that SHOULD be visible in Pending tab
                PendingTabApplications = await _context.PositionApplications
                    .Where(pa => pa.Status == ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                              && pa.PositionType == positionType
                              && pa.AssignedAEStructuralId == officerId
                              && pa.AEStructuralApprovalStatus != true
                              && pa.AEStructuralRejectionStatus != true)
                    .Select(pa => new
                    {
                        pa.Id,
                        pa.ApplicationNumber,
                        ApplicantName = pa.FirstName + " " + pa.LastName,
                        PositionType = pa.PositionType.ToString(),
                        Status = pa.Status.ToString(),
                        pa.CreatedDate,
                        Verdict = "✅ SHOULD BE VISIBLE IN PENDING TAB"
                    })
                    .OrderByDescending(pa => pa.CreatedDate)
                    .ToListAsync(),
                
                // 4. Applications that SHOULD be visible in Completed tab
                CompletedTabApplications = await _context.PositionApplications
                    .Where(pa => pa.PositionType == positionType
                              && pa.AssignedAEStructuralId == officerId
                              && (pa.AEStructuralApprovalStatus == true || pa.AEStructuralRejectionStatus == true))
                    .Select(pa => new
                    {
                        pa.Id,
                        pa.ApplicationNumber,
                        ApplicantName = pa.FirstName + " " + pa.LastName,
                        PositionType = pa.PositionType.ToString(),
                        Status = pa.Status.ToString(),
                        ProcessingStatus = pa.AEStructuralApprovalStatus == true ? "Approved" : "Rejected",
                        ProcessingDate = pa.AEStructuralApprovalStatus == true 
                            ? pa.AEStructuralApprovalDate 
                            : pa.AEStructuralRejectionDate,
                        Verdict = "📋 SHOULD BE IN COMPLETED TAB"
                    })
                    .OrderByDescending(pa => pa.ProcessingDate)
                    .ToListAsync(),
                
                // 5. Issues - Wrong Position Type
                IssuesWrongPositionType = await _context.PositionApplications
                    .Where(pa => pa.AssignedAEStructuralId == officerId
                              && pa.PositionType != positionType)
                    .Select(pa => new
                    {
                        Issue = "❌ Wrong Position Type",
                        pa.Id,
                        pa.ApplicationNumber,
                        ApplicationPositionType = pa.PositionType.ToString(),
                        Expected = "StructuralEngineer",
                        Status = pa.Status.ToString()
                    })
                    .ToListAsync(),
                
                // 6. Issues - Wrong Status
                IssuesWrongStatus = await _context.PositionApplications
                    .Where(pa => pa.AssignedAEStructuralId == officerId
                              && pa.Status != ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                              && pa.AEStructuralApprovalStatus != true
                              && pa.AEStructuralRejectionStatus != true)
                    .Select(pa => new
                    {
                        Issue = "❌ Wrong Status",
                        pa.Id,
                        pa.ApplicationNumber,
                        CurrentStatus = pa.Status.ToString(),
                        Expected = "ASSISTANT_ENGINEER_PENDING",
                        PositionType = pa.PositionType.ToString()
                    })
                    .ToListAsync(),
                
                // 7. Summary
                Summary = new
                {
                    TotalAssignedToOfficer7 = await _context.PositionApplications
                        .CountAsync(pa => pa.AssignedAEStructuralId == officerId),
                    
                    ShouldBeVisibleInPending = await _context.PositionApplications
                        .CountAsync(pa => pa.Status == ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                                       && pa.PositionType == positionType
                                       && pa.AssignedAEStructuralId == officerId
                                       && pa.AEStructuralApprovalStatus != true
                                       && pa.AEStructuralRejectionStatus != true),
                    
                    ShouldBeVisibleInCompleted = await _context.PositionApplications
                        .CountAsync(pa => pa.PositionType == positionType
                                       && pa.AssignedAEStructuralId == officerId
                                       && (pa.AEStructuralApprovalStatus == true || pa.AEStructuralRejectionStatus == true)),
                    
                    WrongPositionTypeCount = await _context.PositionApplications
                        .CountAsync(pa => pa.AssignedAEStructuralId == officerId
                                       && pa.PositionType != positionType),
                    
                    WrongStatusCount = await _context.PositionApplications
                        .CountAsync(pa => pa.AssignedAEStructuralId == officerId
                                       && pa.Status != ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                                       && pa.AEStructuralApprovalStatus != true
                                       && pa.AEStructuralRejectionStatus != true)
                },
                
                // 8. Instructions
                Instructions = new
                {
                    Step1 = "Login as Officer ID 7",
                    Step2 = "Go to AE Dashboard",
                    Step3 = "Select 'Structural Engineer' from dropdown",
                    Step4 = "Applications shown should match 'PendingTabApplications' array above",
                    Step5 = "Switch to 'Completed' tab - should match 'CompletedTabApplications' array above",
                    API_Endpoint_Pending = "GET /api/AssistantEngineer/pending/StructuralEngineer",
                    API_Endpoint_Completed = "GET /api/AssistantEngineer/completed/StructuralEngineer"
                }
            };
            
            _logger.LogInformation("Diagnostic test run for Officer ID {OfficerId}", officerId);
            
            return Ok(result);
        }
    }
}
