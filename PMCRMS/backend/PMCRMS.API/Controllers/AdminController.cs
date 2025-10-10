using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Cryptography;
using System.Text;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AdminController(
            PMCRMSDbContext context,
            ILogger<AdminController> logger,
            IEmailService emailService,
            IPasswordHasher passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        #region Dashboard & Statistics

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AdminDashboardStats>>> GetDashboardStats()
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} fetching dashboard statistics", adminId);

                var stats = new AdminDashboardStats();

                // Application statistics
                var allApplications = await _context.Applications.ToListAsync();
                stats.TotalApplications = allApplications.Count;
                stats.PendingApplications = allApplications.Count(a => 
                    a.CurrentStatus == ApplicationCurrentStatus.Draft || 
                    a.CurrentStatus == ApplicationCurrentStatus.Submitted ||
                    a.CurrentStatus.ToString().Contains("UnderReview") ||
                    a.CurrentStatus.ToString().Contains("UnderProcessing") ||
                    a.CurrentStatus == ApplicationCurrentStatus.PaymentPending);
                stats.ApprovedApplications = allApplications.Count(a => 
                    a.CurrentStatus == ApplicationCurrentStatus.Completed ||
                    a.CurrentStatus == ApplicationCurrentStatus.CertificateIssued);
                stats.RejectedApplications = allApplications.Count(a => 
                    a.CurrentStatus.ToString().Contains("Rejected"));

                // Officer statistics
                var allOfficers = await _context.Users.Where(u => u.Role != UserRole.Applicant && u.Role != UserRole.User).ToListAsync();
                stats.TotalOfficers = allOfficers.Count;
                stats.ActiveOfficers = allOfficers.Count(o => o.IsActive);

                // Invitation statistics
                stats.PendingInvitations = await _context.OfficerInvitations
                    .CountAsync(i => i.Status == InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow);

                // Revenue statistics
                var allPayments = await _context.Payments.Where(p => p.Status == PaymentStatus.Success).ToListAsync();
                stats.TotalRevenueCollected = allPayments.Sum(p => p.Amount);
                
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                stats.RevenueThisMonth = allPayments
                    .Where(p => p.CreatedDate >= startOfMonth)
                    .Sum(p => p.Amount);

                // Application trends (last 7 days)
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var recentApplications = allApplications.Where(a => a.CreatedDate >= sevenDaysAgo).ToList();
                
                stats.ApplicationTrends = recentApplications
                    .GroupBy(a => a.CreatedDate.Date)
                    .Select(g => new ApplicationTrendDto
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count(),
                        Status = "All"
                    })
                    .OrderBy(t => t.Date)
                    .ToList();

                // Role distribution
                stats.RoleDistribution = allOfficers
                    .GroupBy(o => o.Role)
                    .Select(g => new RoleDistributionDto
                    {
                        Role = g.Key.ToString(),
                        Count = g.Count(),
                        ActiveCount = g.Count(o => o.IsActive)
                    })
                    .ToList();

                return Ok(new ApiResponse<AdminDashboardStats>
                {
                    Success = true,
                    Data = stats,
                    Message = "Dashboard statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin dashboard statistics");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch dashboard statistics",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Officer Invitation Management

        [HttpPost("invite-officer")]
        public async Task<ActionResult<ApiResponse<OfficerInvitationDto>>> InviteOfficer([FromBody] InviteOfficerRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} inviting officer: {Email}", adminId, request.Email);

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A user with this email already exists"
                    });
                }

                // Check if employee ID already exists
                var existingEmployee = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
                if (existingEmployee != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A user with this employee ID already exists"
                    });
                }

                // Check for pending invitations
                var pendingInvitation = await _context.OfficerInvitations
                    .FirstOrDefaultAsync(i => i.Email == request.Email && 
                                            i.Status == InvitationStatus.Pending &&
                                            i.ExpiresAt > DateTime.UtcNow);
                
                if (pendingInvitation != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A pending invitation already exists for this email"
                    });
                }

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();
                var hashedPassword = _passwordHasher.HashPassword(tempPassword);

                // Create invitation
                var invitation = new OfficerInvitation
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    EmployeeId = request.EmployeeId,
                    Department = request.Department,
                    TemporaryPassword = hashedPassword,
                    InvitedByUserId = adminId,
                    InvitedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays),
                    Status = InvitationStatus.Pending,
                    CreatedBy = User.FindFirst("email")?.Value ?? "Admin"
                };

                _context.OfficerInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Send invitation email
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
                var loginUrl = $"{baseUrl}/officer-login";
                
                var emailSent = await _emailService.SendOfficerInvitationEmailAsync(
                    request.Email,
                    request.Name,
                    request.Role.ToString(),
                    request.EmployeeId,
                    tempPassword,
                    loginUrl
                );

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send invitation email to {Email}, but invitation was created", request.Email);
                }

                var invitedBy = await _context.Users.FindAsync(adminId);

                var responseDto = new OfficerInvitationDto
                {
                    Id = invitation.Id,
                    Name = invitation.Name,
                    Email = invitation.Email,
                    PhoneNumber = invitation.PhoneNumber,
                    Role = invitation.Role.ToString(),
                    EmployeeId = invitation.EmployeeId,
                    Department = invitation.Department,
                    Status = invitation.Status.ToString(),
                    InvitedAt = invitation.InvitedAt,
                    ExpiresAt = invitation.ExpiresAt,
                    InvitedByName = invitedBy?.Name ?? "Admin",
                    IsExpired = invitation.ExpiresAt <= DateTime.UtcNow
                };

                return Ok(new ApiResponse<OfficerInvitationDto>
                {
                    Success = true,
                    Data = responseDto,
                    Message = "Officer invitation sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to send officer invitation",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("invitations")]
        public async Task<ActionResult<ApiResponse<List<OfficerInvitationDto>>>> GetInvitations(
            [FromQuery] string? status = null)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} fetching invitations", adminId);

                var query = _context.OfficerInvitations
                    .Include(i => i.InvitedByUser)
                    .Include(i => i.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvitationStatus>(status, out var statusEnum))
                {
                    query = query.Where(i => i.Status == statusEnum);
                }

                var invitations = await query
                    .OrderByDescending(i => i.InvitedAt)
                    .ToListAsync();

                var invitationDtos = invitations.Select(i => new OfficerInvitationDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Email = i.Email,
                    PhoneNumber = i.PhoneNumber,
                    Role = i.Role.ToString(),
                    EmployeeId = i.EmployeeId,
                    Department = i.Department,
                    Status = i.Status.ToString(),
                    InvitedAt = i.InvitedAt,
                    AcceptedAt = i.AcceptedAt,
                    ExpiresAt = i.ExpiresAt,
                    InvitedByName = i.InvitedByUser?.Name ?? "Admin",
                    IsExpired = i.ExpiresAt <= DateTime.UtcNow && i.Status == InvitationStatus.Pending,
                    UserId = i.UserId
                }).ToList();

                return Ok(new ApiResponse<List<OfficerInvitationDto>>
                {
                    Success = true,
                    Data = invitationDtos,
                    Message = "Invitations retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invitations");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch invitations",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("resend-invitation")]
        public async Task<ActionResult<ApiResponse>> ResendInvitation([FromBody] ResendInvitationRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} resending invitation {InvitationId}", adminId, request.InvitationId);

                var invitation = await _context.OfficerInvitations.FindAsync(request.InvitationId);
                if (invitation == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Invitation not found"
                    });
                }

                if (invitation.Status == InvitationStatus.Accepted)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot resend an accepted invitation"
                    });
                }

                // Generate new temporary password
                var tempPassword = GenerateTemporaryPassword();
                var hashedPassword = _passwordHasher.HashPassword(tempPassword);

                // Update invitation
                invitation.TemporaryPassword = hashedPassword;
                invitation.ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays);
                invitation.Status = InvitationStatus.Pending;
                invitation.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Resend email
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
                var loginUrl = $"{baseUrl}/officer-login";

                await _emailService.SendOfficerInvitationEmailAsync(
                    invitation.Email,
                    invitation.Name,
                    invitation.Role.ToString(),
                    invitation.EmployeeId,
                    tempPassword,
                    loginUrl
                );

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Invitation resent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invitation");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to resend invitation",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("invitations/{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteInvitation(int id)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} deleting invitation {InvitationId}", adminId, id);

                var invitation = await _context.OfficerInvitations.FindAsync(id);
                if (invitation == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Invitation not found"
                    });
                }

                invitation.Status = InvitationStatus.Revoked;
                invitation.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Invitation revoked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invitation");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to delete invitation",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Officer Management

        [HttpGet("officers")]
        public async Task<ActionResult<ApiResponse<List<OfficerDto>>>> GetOfficers(
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} fetching officers", adminId);

                var query = _context.Users
                    .Where(u => u.Role != UserRole.Applicant && u.Role != UserRole.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out var roleEnum))
                {
                    query = query.Where(u => u.Role == roleEnum);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                var officers = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                var officerDtos = new List<OfficerDto>();

                foreach (var officer in officers)
                {
                    var applicationsProcessed = await _context.ApplicationStatuses
                        .CountAsync(s => s.UpdatedByUserId == officer.Id);

                    officerDtos.Add(new OfficerDto
                    {
                        Id = officer.Id,
                        Name = officer.Name,
                        Email = officer.Email,
                        PhoneNumber = officer.PhoneNumber,
                        Role = officer.Role.ToString(),
                        EmployeeId = officer.EmployeeId ?? "",
                        IsActive = officer.IsActive,
                        LastLoginAt = officer.LastLoginAt,
                        CreatedDate = officer.CreatedDate,
                        ApplicationsProcessed = applicationsProcessed
                    });
                }

                return Ok(new ApiResponse<List<OfficerDto>>
                {
                    Success = true,
                    Data = officerDtos,
                    Message = "Officers retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching officers");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch officers",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("officers/{id}")]
        public async Task<ActionResult<ApiResponse<OfficerDetailDto>>> GetOfficerDetail(int id)
        {
            try
            {
                var officer = await _context.Users.FindAsync(id);
                if (officer == null || officer.Role == UserRole.Applicant || officer.Role == UserRole.User)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                var recentStatusUpdates = await _context.ApplicationStatuses
                    .Include(s => s.Application)
                    .Where(s => s.UpdatedByUserId == id)
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(10)
                    .Select(s => new ApplicationStatusSummaryDto
                    {
                        ApplicationId = s.ApplicationId,
                        ApplicationNumber = s.Application.ApplicationNumber,
                        Status = s.Status.ToString(),
                        UpdatedAt = s.CreatedDate,
                        Remarks = s.Remarks
                    })
                    .ToListAsync();

                var applicationsProcessed = await _context.ApplicationStatuses
                    .CountAsync(s => s.UpdatedByUserId == id);

                var officerDetail = new OfficerDetailDto
                {
                    Id = officer.Id,
                    Name = officer.Name,
                    Email = officer.Email,
                    PhoneNumber = officer.PhoneNumber,
                    Role = officer.Role.ToString(),
                    EmployeeId = officer.EmployeeId ?? "",
                    IsActive = officer.IsActive,
                    LastLoginAt = officer.LastLoginAt,
                    CreatedDate = officer.CreatedDate,
                    ApplicationsProcessed = applicationsProcessed,
                    Address = officer.Address,
                    UpdatedDate = officer.UpdatedDate,
                    CreatedBy = officer.CreatedBy,
                    RecentStatusUpdates = recentStatusUpdates
                };

                return Ok(new ApiResponse<OfficerDetailDto>
                {
                    Success = true,
                    Data = officerDetail,
                    Message = "Officer details retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching officer details");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch officer details",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("officers/{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateOfficer(int id, [FromBody] UpdateOfficerRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} updating officer {OfficerId}", adminId, id);

                var officer = await _context.Users.FindAsync(id);
                if (officer == null || officer.Role == UserRole.Applicant || officer.Role == UserRole.User)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.Name))
                    officer.Name = request.Name;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    officer.PhoneNumber = request.PhoneNumber;

                if (request.Role.HasValue)
                    officer.Role = request.Role.Value;

                if (request.IsActive.HasValue)
                    officer.IsActive = request.IsActive.Value;

                officer.UpdatedDate = DateTime.UtcNow;
                officer.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Officer updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("officers/{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteOfficer(int id)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} deleting officer {OfficerId}", adminId, id);

                var officer = await _context.Users.FindAsync(id);
                if (officer == null || officer.Role == UserRole.Applicant || officer.Role == UserRole.User || officer.Role == UserRole.Admin)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found or cannot be deleted"
                    });
                }

                // Instead of deleting, we deactivate
                officer.IsActive = false;
                officer.UpdatedDate = DateTime.UtcNow;
                officer.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Officer deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to delete officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateTemporaryPassword()
        {
            const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#$%&*";

            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one of each type
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(digits[random.Next(digits.Length)]);
            password.Append(special[random.Next(special.Length)]);

            // Fill the rest randomly (total 12 characters)
            const string allChars = upperCase + lowerCase + digits + special;
            for (int i = 4; i < 12; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }

        #endregion
    }
}
