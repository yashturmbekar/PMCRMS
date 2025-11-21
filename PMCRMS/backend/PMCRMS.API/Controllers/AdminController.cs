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

                // Application statistics - Use PositionApplications table
                var allApplications = await _context.PositionApplications.ToListAsync();
                stats.TotalApplications = allApplications.Count;
                stats.PendingApplications = allApplications.Count(a => 
                    a.Status == ApplicationCurrentStatus.Draft || 
                    a.Status == ApplicationCurrentStatus.Submitted ||
                    a.Status.ToString().Contains("UnderReview") ||
                    a.Status.ToString().Contains("UnderProcessing") ||
                    a.Status == ApplicationCurrentStatus.PaymentPending);
                stats.ApprovedApplications = allApplications.Count(a => 
                    a.Status == ApplicationCurrentStatus.Completed ||
                    a.Status == ApplicationCurrentStatus.CertificateIssued);
                stats.RejectedApplications = allApplications.Count(a => 
                    a.Status == ApplicationCurrentStatus.REJECTED ||
                    a.Status == ApplicationCurrentStatus.RejectedByJE ||
                    a.Status == ApplicationCurrentStatus.RejectedByAE ||
                    a.Status == ApplicationCurrentStatus.RejectedByEE1 ||
                    a.Status == ApplicationCurrentStatus.RejectedByCE1);

                // Officer statistics
                var allOfficers = await _context.Officers.ToListAsync();
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
                _logger.LogInformation("Admin {AdminId} inviting officer: {Email}, Role: {Role}", adminId, request.Email, request.Role);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer name is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer email is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Role))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer role is required"
                    });
                }

                // Parse role string to enum
                if (!Enum.TryParse<OfficerRole>(request.Role, true, out var officerRole))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Invalid role: {request.Role}. Valid roles are: JuniorArchitect, AssistantArchitect, JuniorLicenceEngineer, AssistantLicenceEngineer, JuniorStructuralEngineer, AssistantStructuralEngineer, JuniorSupervisor1, AssistantSupervisor1, JuniorSupervisor2, AssistantSupervisor2, ExecutiveEngineer, CityEngineer, Clerk"
                    });
                }

                // Auto-generate Employee ID if not provided
                if (string.IsNullOrWhiteSpace(request.EmployeeId))
                {
                    var rolePrefix = string.Concat(request.Role.Where(char.IsUpper));
                    var timestamp = DateTime.UtcNow.Ticks.ToString().Substring(DateTime.UtcNow.Ticks.ToString().Length - 6);
                    request.EmployeeId = $"{rolePrefix}-{timestamp}";
                    _logger.LogInformation("Auto-generated Employee ID: {EmployeeId}", request.EmployeeId);
                }

                // Check if email already exists in Officers or SystemAdmins
                var existingOfficer = await _context.Officers.FirstOrDefaultAsync(o => o.Email == request.Email);
                if (existingOfficer != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "An officer with this email already exists"
                    });
                }

                var existingAdmin = await _context.SystemAdmins.FirstOrDefaultAsync(a => a.Email == request.Email);
                if (existingAdmin != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "An admin with this email already exists"
                    });
                }

                // Check if employee ID already exists
                var existingEmployeeId = await _context.Officers.FirstOrDefaultAsync(o => o.EmployeeId == request.EmployeeId);
                if (existingEmployeeId != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "An officer with this employee ID already exists"
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

                // Create Officer account immediately
                var officer = new Officer
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = officerRole,
                    EmployeeId = request.EmployeeId,
                    Department = request.Department ?? string.Empty,
                    PasswordHash = hashedPassword,
                    MustChangePassword = true, // Force password change on first login
                    IsActive = true,
                    CreatedBy = User.FindFirst("email")?.Value ?? "Admin",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Officers.Add(officer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Officer account created: {Email}, ID: {OfficerId}, Employee ID: {EmployeeId}", 
                    request.Email, officer.Id, officer.EmployeeId);

                // Create invitation record for tracking
                var invitation = new OfficerInvitation
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = officerRole, // Use the parsed OfficerRole enum value
                    EmployeeId = request.EmployeeId,
                    Department = request.Department ?? string.Empty,
                    TemporaryPassword = hashedPassword,
                    InvitedByAdminId = adminId, // Changed from InvitedByUserId
                    InvitedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays > 0 ? request.ExpiryDays : 7),
                    Status = InvitationStatus.Accepted, // Mark as accepted since officer is created
                    AcceptedAt = DateTime.UtcNow,
                    OfficerId = officer.Id,
                    CreatedBy = User.FindFirst("email")?.Value ?? "Admin"
                };

                _context.OfficerInvitations.Add(invitation);

                // Update officer with invitation reference
                officer.InvitationId = invitation.Id;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Officer invitation record created with ID: {InvitationId}", invitation.Id);

                // Send invitation email
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
                var loginUrl = $"{baseUrl}/officer-login";
                
                var emailSent = await _emailService.SendOfficerInvitationEmailAsync(
                    request.Email,
                    request.Name,
                    officerRole.ToString(), // Use the parsed OfficerRole enum
                    request.EmployeeId,
                    tempPassword,
                    loginUrl
                );

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send invitation email to {Email}, but invitation was created", request.Email);
                }

                var invitedByAdmin = await _context.SystemAdmins.FindAsync(adminId);

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
                    InvitedByName = invitedByAdmin?.Name ?? "Admin",
                    IsExpired = invitation.ExpiresAt <= DateTime.UtcNow,
                    TemporaryPassword = tempPassword // Include password in response
                };

                return Ok(new ApiResponse<OfficerInvitationDto>
                {
                    Success = true,
                    Data = responseDto,
                    Message = $"Officer invitation sent successfully! Temporary Password: {tempPassword}"
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
                    .Include(i => i.InvitedByAdmin)
                    .Include(i => i.Officer)
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
                    InvitedByName = i.InvitedByAdmin?.Name ?? "Admin",
                    IsExpired = i.ExpiresAt <= DateTime.UtcNow && i.Status == InvitationStatus.Pending,
                    OfficerId = i.OfficerId
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
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
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

                var query = _context.Officers.AsQueryable();

                if (!string.IsNullOrEmpty(role) && Enum.TryParse<OfficerRole>(role, out var roleEnum))
                {
                    query = query.Where(o => o.Role == roleEnum);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(o => o.IsActive == isActive.Value);
                }

                var officers = await query
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

                var officerDtos = new List<OfficerDto>();

                foreach (var officer in officers)
                {
                    var applicationsProcessed = await _context.ApplicationStatuses
                        .CountAsync(s => s.UpdatedByOfficerId == officer.Id);

                    officerDtos.Add(new OfficerDto
                    {
                        Id = officer.Id,
                        Name = officer.Name,
                        Email = officer.Email,
                        PhoneNumber = officer.PhoneNumber,
                        Role = officer.Role.ToString(),
                        EmployeeId = officer.EmployeeId,
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
                var officer = await _context.Officers.FindAsync(id);
                if (officer == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                var recentStatusUpdates = await _context.ApplicationStatuses
                    .Include(s => s.Application)
                    .Where(s => s.UpdatedByOfficerId == id)
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
                    .CountAsync(s => s.UpdatedByOfficerId == id);

                var officerDetail = new OfficerDetailDto
                {
                    Id = officer.Id,
                    Name = officer.Name,
                    Email = officer.Email,
                    PhoneNumber = officer.PhoneNumber,
                    Role = officer.Role.ToString(),
                    EmployeeId = officer.EmployeeId,
                    IsActive = officer.IsActive,
                    LastLoginAt = officer.LastLoginAt,
                    CreatedDate = officer.CreatedDate,
                    ApplicationsProcessed = applicationsProcessed,
                    Department = officer.Department, // Changed from Address to Department
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

                var officer = await _context.Officers.FindAsync(id);
                if (officer == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                bool emailChanged = false;
                string? newPassword = null;
                string oldEmail = officer.Email;

                // Check if email is being changed
                if (!string.IsNullOrEmpty(request.Email) && request.Email != officer.Email)
                {
                    // Validate that new email doesn't already exist
                    var existingOfficer = await _context.Officers
                        .FirstOrDefaultAsync(o => o.Email == request.Email && o.Id != id);
                    if (existingOfficer != null)
                    {
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "An officer with this email already exists",
                            Errors = new List<string> { "Email already in use" }
                        });
                    }

                    var existingAdmin = await _context.SystemAdmins
                        .FirstOrDefaultAsync(a => a.Email == request.Email);
                    if (existingAdmin != null)
                    {
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "An admin with this email already exists",
                            Errors = new List<string> { "Email already in use" }
                        });
                    }

                    // Generate new password when email changes
                    newPassword = GenerateTemporaryPassword();
                    officer.Email = request.Email;
                    officer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    officer.MustChangePassword = true;
                    officer.PasswordChangedAt = DateTime.UtcNow;
                    emailChanged = true;

                    _logger.LogInformation("Officer {OfficerId} email changed from {OldEmail} to {NewEmail}", 
                        id, oldEmail, request.Email);
                }

                // Update other fields
                if (!string.IsNullOrEmpty(request.Name))
                    officer.Name = request.Name;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    officer.PhoneNumber = request.PhoneNumber;

                if (request.Role.HasValue)
                    officer.Role = request.Role.Value;

                if (!string.IsNullOrEmpty(request.Department))
                    officer.Department = request.Department;

                if (request.IsActive.HasValue)
                    officer.IsActive = request.IsActive.Value;

                officer.UpdatedDate = DateTime.UtcNow;
                officer.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                // Send email with new password if email was changed
                if (emailChanged && newPassword != null)
                {
                    var loginUrl = $"{Request.Scheme}://{Request.Host}/officer-login";
                    var emailSent = await _emailService.SendOfficerInvitationEmailAsync(
                        officer.Email,
                        officer.Name,
                        officer.Role.ToString(),
                        officer.EmployeeId,
                        newPassword,
                        loginUrl
                    );

                    if (!emailSent)
                    {
                        _logger.LogWarning("Failed to send new password email to {Email}", officer.Email);
                    }

                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = $"Officer updated successfully. Email changed and new password sent to {officer.Email}. Temporary password: {newPassword}"
                    });
                }

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

                var officer = await _context.Officers.FindAsync(id);
                if (officer == null)
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

        #region Form Configuration Management

        [HttpGet("forms")]
        public async Task<ActionResult<ApiResponse<List<FormConfigurationDto>>>> GetAllForms()
        {
            try
            {
                var forms = await _context.FormConfigurations
                    .OrderBy(f => f.FormName)
                    .ToListAsync();

                var formDtos = forms.Select(f => new FormConfigurationDto
                {
                    Id = f.Id,
                    FormName = f.FormName,
                    FormType = f.FormType.ToString(),
                    Description = f.Description,
                    BaseFee = f.BaseFee,
                    ProcessingFee = f.ProcessingFee,
                    LateFee = f.LateFee,
                    IsActive = f.IsActive,
                    AllowOnlineSubmission = f.AllowOnlineSubmission,
                    ProcessingDays = f.ProcessingDays,
                    CustomFields = f.CustomFields,
                    RequiredDocuments = f.RequiredDocuments,
                    MaxFileSizeMB = f.MaxFileSizeMB,
                    MaxFilesAllowed = f.MaxFilesAllowed,
                    CreatedDate = f.CreatedDate,
                    UpdatedDate = f.UpdatedDate
                }).ToList();

                return Ok(new ApiResponse<List<FormConfigurationDto>>
                {
                    Success = true,
                    Data = formDtos,
                    Message = "Forms retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forms");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch forms",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("forms/{id}")]
        public async Task<ActionResult<ApiResponse<FormConfigurationDetailDto>>> GetFormDetail(int id)
        {
            try
            {
                var form = await _context.FormConfigurations
                    .Include(f => f.FeeHistory)
                        .ThenInclude(h => h.ChangedByAdmin)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form not found"
                    });
                }

                var feeHistoryDtos = form.FeeHistory
                    .OrderByDescending(h => h.CreatedDate)
                    .Select(h => new FormFeeHistoryDto
                    {
                        Id = h.Id,
                        OldBaseFee = h.OldBaseFee,
                        NewBaseFee = h.NewBaseFee,
                        OldProcessingFee = h.OldProcessingFee,
                        NewProcessingFee = h.NewProcessingFee,
                        EffectiveFrom = h.EffectiveFrom,
                        ChangedBy = h.ChangedByAdmin?.Name ?? "System",
                        ChangeReason = h.ChangeReason,
                        ChangedDate = h.CreatedDate
                    }).ToList();

                var formDetailDto = new FormConfigurationDetailDto
                {
                    Id = form.Id,
                    FormName = form.FormName,
                    FormType = form.FormType.ToString(),
                    Description = form.Description,
                    BaseFee = form.BaseFee,
                    ProcessingFee = form.ProcessingFee,
                    LateFee = form.LateFee,
                    IsActive = form.IsActive,
                    AllowOnlineSubmission = form.AllowOnlineSubmission,
                    ProcessingDays = form.ProcessingDays,
                    CustomFields = form.CustomFields,
                    RequiredDocuments = form.RequiredDocuments,
                    MaxFileSizeMB = form.MaxFileSizeMB,
                    MaxFilesAllowed = form.MaxFilesAllowed,
                    CreatedDate = form.CreatedDate,
                    UpdatedDate = form.UpdatedDate,
                    FeeHistory = feeHistoryDtos
                };

                return Ok(new ApiResponse<FormConfigurationDetailDto>
                {
                    Success = true,
                    Data = formDetailDto,
                    Message = "Form details retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching form details");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch form details",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("forms/{id}/fees")]
        public async Task<ActionResult<ApiResponse>> UpdateFormFees(int id, [FromBody] UpdateFormFeesRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} updating fees for form {FormId}", adminId, id);

                var form = await _context.FormConfigurations.FindAsync(id);
                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form not found"
                    });
                }

                // Create fee history record
                var feeHistory = new FormFeeHistory
                {
                    FormConfigurationId = id,
                    OldBaseFee = form.BaseFee,
                    NewBaseFee = request.BaseFee,
                    OldProcessingFee = form.ProcessingFee,
                    NewProcessingFee = request.ProcessingFee,
                    EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
                    ChangedByAdminId = adminId, // Changed from ChangedByUserId
                    ChangeReason = request.ChangeReason,
                    CreatedBy = User.FindFirst("email")?.Value ?? "Admin"
                };

                // Update form fees
                form.BaseFee = request.BaseFee;
                form.ProcessingFee = request.ProcessingFee;
                
                if (request.LateFee.HasValue)
                    form.LateFee = request.LateFee.Value;

                form.UpdatedDate = DateTime.UtcNow;
                form.UpdatedBy = User.FindFirst("email")?.Value;

                _context.FormFeeHistories.Add(feeHistory);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Form fees updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form fees");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update form fees",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("forms/{id}/custom-fields")]
        public async Task<ActionResult<ApiResponse>> UpdateFormCustomFields(int id, [FromBody] UpdateFormCustomFieldsRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} updating custom fields for form {FormId}", adminId, id);

                var form = await _context.FormConfigurations.FindAsync(id);
                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form not found"
                    });
                }

                // Update custom fields
                form.CustomFields = request.CustomFieldsJson;
                form.UpdatedDate = DateTime.UtcNow;
                form.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Custom fields updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating custom fields");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update custom fields",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("forms/{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateFormConfiguration(int id, [FromBody] UpdateFormConfigurationRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} updating form configuration {FormId}", adminId, id);

                var form = await _context.FormConfigurations.FindAsync(id);
                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form not found"
                    });
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.FormName))
                    form.FormName = request.FormName;

                if (!string.IsNullOrEmpty(request.Description))
                    form.Description = request.Description;

                if (request.IsActive.HasValue)
                    form.IsActive = request.IsActive.Value;

                if (request.AllowOnlineSubmission.HasValue)
                    form.AllowOnlineSubmission = request.AllowOnlineSubmission.Value;

                if (request.ProcessingDays.HasValue)
                    form.ProcessingDays = request.ProcessingDays.Value;

                if (request.MaxFileSizeMB.HasValue)
                    form.MaxFileSizeMB = request.MaxFileSizeMB.Value;

                if (request.MaxFilesAllowed.HasValue)
                    form.MaxFilesAllowed = request.MaxFilesAllowed.Value;

                if (!string.IsNullOrEmpty(request.RequiredDocuments))
                    form.RequiredDocuments = request.RequiredDocuments;

                form.UpdatedDate = DateTime.UtcNow;
                form.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Form configuration updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form configuration");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update form configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("forms/{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteForm(int id)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} deleting form {FormId}", adminId, id);

                var form = await _context.FormConfigurations.FindAsync(id);
                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form not found"
                    });
                }

                // Soft delete by deactivating
                form.IsActive = false;
                form.UpdatedDate = DateTime.UtcNow;
                form.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Form deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to delete form",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #endregion

        #region Application Management

        [HttpGet("applications")]
        public async Task<ActionResult<ApiResponse<List<ApplicationSummaryDto>>>> GetAllApplications(
            [FromQuery] string? status = null,
            [FromQuery] string? positionType = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} fetching all position applications", adminId);

                var query = _context.PositionApplications
                    .Include(a => a.User)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApplicationCurrentStatus>(status, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }

                // Filter by position type
                if (!string.IsNullOrEmpty(positionType) && Enum.TryParse<PositionType>(positionType, out var positionTypeEnum))
                {
                    query = query.Where(a => a.PositionType == positionTypeEnum);
                }

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a =>
                        (a.ApplicationNumber != null && a.ApplicationNumber.Contains(search)) ||
                        a.FirstName.Contains(search) ||
                        a.LastName.Contains(search) ||
                        a.EmailAddress.Contains(search) ||
                        a.MobileNumber.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var applications = await query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var applicationDtos = applications.Select(app => new ApplicationSummaryDto
                {
                    ApplicationId = app.Id,
                    ApplicationNumber = app.ApplicationNumber ?? "N/A",
                    ApplicantName = $"{app.FirstName} {app.MiddleName} {app.LastName}".Replace("  ", " ").Trim(),
                    ApplicationType = app.PositionType.ToString(),
                    Status = app.Status.ToString(),
                    SubmittedOn = app.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss")
                }).ToList();

                return Ok(new ApiResponse<List<ApplicationSummaryDto>>
                {
                    Success = true,
                    Data = applicationDtos,
                    Message = $"Retrieved {applicationDtos.Count} applications (Page {page} of {(int)Math.Ceiling((double)totalCount / pageSize)})"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching position applications for admin");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch applications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("applications/{id}")]
        public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> GetApplicationDetail(int id)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} fetching position application detail {ApplicationId}", adminId, id);

                var application = await _context.PositionApplications
                    .Include(a => a.User)
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

                var age = DateTime.Today.Year - application.DateOfBirth.Year;
                if (application.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

                var applicationDetail = new ApplicationDetailDto
                {
                    ApplicationId = application.Id,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    ApplicantId = application.UserId,
                    ApplicantName = $"{application.FirstName} {application.MiddleName} {application.LastName}".Replace("  ", " ").Trim(),
                    ApplicantEmail = application.EmailAddress,
                    ApplicantPhone = application.MobileNumber,
                    ApplicationType = application.PositionType.ToString(),
                    Status = application.Status.ToString(),
                    SubmittedOn = application.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastUpdated = (application.UpdatedDate ?? application.CreatedDate).ToString("yyyy-MM-ddTHH:mm:ss"),
                    FormData = new
                    {
                        // Personal Information
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
                        dateOfBirth = application.DateOfBirth.ToString("yyyy-MM-dd"),
                        age = age,
                        panCardNumber = application.PanCardNumber,
                        aadharCardNumber = application.AadharCardNumber,
                        coaCardNumber = application.CoaCardNumber,
                        
                        // Status Information
                        applicationNumber = application.ApplicationNumber,
                        status = application.Status.ToString(),
                        submittedDate = application.SubmittedDate?.ToString("yyyy-MM-dd"),
                        approvedDate = application.ApprovedDate?.ToString("yyyy-MM-dd"),
                        remarks = application.Remarks,
                        
                        // Addresses
                        addresses = application.Addresses.Select(addr => new
                        {
                            addressType = addr.AddressType,
                            addressLine1 = addr.AddressLine1,
                            addressLine2 = addr.AddressLine2,
                            addressLine3 = addr.AddressLine3,
                            city = addr.City,
                            state = addr.State,
                            country = addr.Country,
                            pinCode = addr.PinCode
                        }).ToList(),
                        
                        // Qualifications
                        qualifications = application.Qualifications.Select(qual => new
                        {
                            instituteName = qual.InstituteName,
                            universityName = qual.UniversityName,
                            specialization = qual.Specialization.ToString(),
                            degreeName = qual.DegreeName,
                            passingMonth = qual.PassingMonth,
                            yearOfPassing = qual.YearOfPassing.Year
                        }).ToList(),
                        
                        // Experiences
                        experiences = application.Experiences.Select(exp => new
                        {
                            companyName = exp.CompanyName,
                            position = exp.Position,
                            yearsOfExperience = exp.YearsOfExperience,
                            fromDate = exp.FromDate.ToString("yyyy-MM-dd"),
                            toDate = exp.ToDate.ToString("yyyy-MM-dd")
                        }).ToList(),
                        
                        // Documents
                        documents = application.Documents.Select(doc => new
                        {
                            documentType = doc.DocumentType.ToString(),
                            fileName = doc.FileName,
                            fileId = doc.FileId,
                            isVerified = doc.IsVerified,
                            verifiedDate = doc.VerifiedDate?.ToString("yyyy-MM-dd")
                        }).ToList()
                    },
                    ProjectTitle = null,
                    ProjectDescription = null,
                    SiteAddress = null,
                    PlotArea = null,
                    BuiltUpArea = null,
                    EstimatedCost = null,
                    AssignedOfficerId = null,
                    AssignedOfficerName = null,
                    AssignedOfficerDesignation = null,
                    AssignedDate = null,
                    FeeAmount = null,
                    PaymentDueDate = null,
                    CertificateNumber = null,
                    CertificateIssuedDate = null,
                    Remarks = application.Remarks,
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate,
                    Documents = application.Documents.Select(d => new ApplicationDocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        DocumentType = d.DocumentType.ToString(),
                        FileSize = d.FileSize.HasValue ? (long)d.FileSize.Value : null,
                        FilePath = d.FilePath,
                        IsVerified = d.IsVerified,
                        VerifiedBy = d.VerifiedBy,
                        VerifiedByName = d.VerifiedByOfficer?.Name,
                        VerifiedDate = d.VerifiedDate,
                        UploadedAt = d.CreatedDate
                    }).ToList(),
                    StatusHistory = new List<ApplicationStatusDto>(),
                    Comments = new List<ApplicationCommentDto>(),
                    Payments = new List<ApplicationPaymentDto>()
                };

                return Ok(new ApiResponse<ApplicationDetailDto>
                {
                    Success = true,
                    Data = applicationDetail,
                    Message = "Application details retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching position application detail");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch application details",
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
