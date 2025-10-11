using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfficersController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OfficersController> _logger;

        public OfficersController(
            PMCRMSDbContext context,
            IEmailService emailService,
            ILogger<OfficersController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Invite a new officer by admin
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OfficerInvitationDto>>> InviteOfficer([FromBody] InviteOfficerRequest request)
        {
            try
            {
                _logger.LogInformation("Admin inviting officer: {Email}, Role: {Role}", request.Email, request.Role);

                // Parse role string to enum
                if (!Enum.TryParse<UserRole>(request.Role, true, out var userRole))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"Invalid role: {request.Role}",
                        Errors = new List<string> { "Invalid role for officer invitation" }
                    });
                }

                // Validate role
                if (userRole == UserRole.Admin || userRole == UserRole.User)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot invite Admin or regular User through this endpoint",
                        Errors = new List<string> { "Invalid role for officer invitation" }
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

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A user with this email already exists",
                        Errors = new List<string> { "Email already registered" }
                    });
                }

                // Check if employee ID already exists
                var existingEmployeeId = await _context.OfficerInvitations
                    .FirstOrDefaultAsync(o => o.EmployeeId == request.EmployeeId);
                if (existingEmployeeId != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Employee ID already exists",
                        Errors = new List<string> { "Duplicate employee ID" }
                    });
                }

                // Check if there's a pending invitation for this email
                var pendingInvitation = await _context.OfficerInvitations
                    .FirstOrDefaultAsync(o => o.Email == request.Email && o.Status == InvitationStatus.Pending);
                
                if (pendingInvitation != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A pending invitation already exists for this email",
                        Errors = new List<string> { "Pending invitation exists" }
                    });
                }

                // Generate temporary password
                var temporaryPassword = GenerateTemporaryPassword();

                // Get current admin user ID
                var adminUserId = int.Parse(User.FindFirst("user_id")?.Value ?? "1");

                // Create invitation
                var invitation = new OfficerInvitation
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = userRole, // Use parsed enum value
                    EmployeeId = request.EmployeeId,
                    Department = request.Department ?? string.Empty,
                    TemporaryPassword = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                    InvitedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays > 0 ? request.ExpiryDays : 7),
                    InvitedByUserId = adminUserId,
                    Status = InvitationStatus.Pending,
                    CreatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin"
                };

                _context.OfficerInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Send invitation email
                var loginUrl = $"{Request.Scheme}://{Request.Host}/officer-login";
                var emailSent = await _emailService.SendOfficerInvitationEmailAsync(
                    request.Email,
                    request.Name,
                    userRole.ToString(), // Use parsed enum
                    request.EmployeeId ?? string.Empty,
                    temporaryPassword,
                    loginUrl
                );

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send invitation email to {Email}", request.Email);
                }

                _logger.LogInformation("Officer invited successfully: {Email}, Temporary Password: {Password}", 
                    request.Email, temporaryPassword);

                var invitationDto = new OfficerInvitationDto
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
                    InvitedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin"
                };

                return Ok(new ApiResponse<OfficerInvitationDto>
                {
                    Success = true,
                    Message = $"Officer invitation sent successfully. Temporary password: {temporaryPassword}",
                    Data = invitationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while inviting the officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all officer invitations
        /// </summary>
        [HttpGet("invitations")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<InvitationListResponse>>> GetInvitations(
            [FromQuery] InvitationStatus? status = null)
        {
            try
            {
                var query = _context.OfficerInvitations
                    .Include(o => o.InvitedByUser)
                    .Include(o => o.User)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                var invitations = await query
                    .OrderByDescending(o => o.InvitedAt)
                    .ToListAsync();

                var invitationDtos = invitations.Select(o => new OfficerInvitationDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = o.Email,
                    PhoneNumber = o.PhoneNumber,
                    Role = o.Role.ToString(),
                    EmployeeId = o.EmployeeId,
                    Department = o.Department,
                    Status = o.Status.ToString(),
                    InvitedAt = o.InvitedAt,
                    AcceptedAt = o.AcceptedAt,
                    ExpiresAt = o.ExpiresAt,
                    InvitedBy = o.InvitedByUser?.Name ?? "Unknown",
                    UserId = o.UserId
                }).ToList();

                var response = new InvitationListResponse
                {
                    Invitations = invitationDtos,
                    PendingCount = invitations.Count(o => o.Status == InvitationStatus.Pending),
                    AcceptedCount = invitations.Count(o => o.Status == InvitationStatus.Accepted),
                    ExpiredCount = invitations.Count(o => o.Status == InvitationStatus.Expired),
                    RevokedCount = invitations.Count(o => o.Status == InvitationStatus.Revoked)
                };

                return Ok(new ApiResponse<InvitationListResponse>
                {
                    Success = true,
                    Message = "Invitations retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invitations");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving invitations",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all officers
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OfficerListResponse>>> GetOfficers(
            [FromQuery] bool? isActive = null,
            [FromQuery] UserRole? role = null)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.Role != UserRole.User && u.Role != UserRole.Admin)
                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                if (role.HasValue)
                {
                    query = query.Where(u => u.Role == role.Value);
                }

                var officers = await query
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                var officerDtos = officers.Select(o => new OfficerDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = o.Email,
                    PhoneNumber = o.PhoneNumber,
                    Role = o.Role.ToString(),
                    EmployeeId = o.EmployeeId ?? "",
                    IsActive = o.IsActive,
                    LastLoginAt = o.LastLoginAt,
                    CreatedDate = o.CreatedDate
                }).ToList();

                var response = new OfficerListResponse
                {
                    Officers = officerDtos,
                    TotalCount = officerDtos.Count,
                    ActiveCount = officerDtos.Count(o => o.IsActive),
                    InactiveCount = officerDtos.Count(o => !o.IsActive)
                };

                return Ok(new ApiResponse<OfficerListResponse>
                {
                    Success = true,
                    Message = "Officers retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving officers");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving officers",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update officer status or details
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OfficerDto>>> UpdateOfficer(int id, [FromBody] UpdateOfficerRequest request)
        {
            try
            {
                var officer = await _context.Users.FindAsync(id);
                if (officer == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                if (officer.Role == UserRole.Admin)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot update admin user through this endpoint"
                    });
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Name))
                    officer.Name = request.Name;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    officer.PhoneNumber = request.PhoneNumber;

                if (request.Role.HasValue && request.Role.Value != UserRole.Admin && request.Role.Value != UserRole.User)
                    officer.Role = request.Role.Value;

                if (request.IsActive.HasValue)
                    officer.IsActive = request.IsActive.Value;

                officer.UpdatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";
                officer.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var officerDto = new OfficerDto
                {
                    Id = officer.Id,
                    Name = officer.Name,
                    Email = officer.Email,
                    PhoneNumber = officer.PhoneNumber,
                    Role = officer.Role.ToString(),
                    EmployeeId = officer.EmployeeId ?? "",
                    IsActive = officer.IsActive,
                    LastLoginAt = officer.LastLoginAt,
                    CreatedDate = officer.CreatedDate
                };

                return Ok(new ApiResponse<OfficerDto>
                {
                    Success = true,
                    Message = "Officer updated successfully",
                    Data = officerDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating the officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete an officer (soft delete by deactivating)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> DeleteOfficer(int id)
        {
            try
            {
                var officer = await _context.Users.FindAsync(id);
                if (officer == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                if (officer.Role == UserRole.Admin)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot delete admin user"
                    });
                }

                // Soft delete
                officer.IsActive = false;
                officer.UpdatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";
                officer.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Officer {OfficerId} deactivated by admin", id);

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
                    Message = "An error occurred while deleting the officer",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Resend invitation to an officer
        /// </summary>
        [HttpPost("invitations/{id}/resend")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> ResendInvitation(int id, [FromBody] ResendInvitationRequest request)
        {
            try
            {
                var invitation = await _context.OfficerInvitations.FindAsync(id);
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
                var temporaryPassword = GenerateTemporaryPassword();
                invitation.TemporaryPassword = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
                invitation.ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays);
                invitation.Status = InvitationStatus.Pending;

                await _context.SaveChangesAsync();

                // Send email
                var loginUrl = $"{Request.Scheme}://{Request.Host}/officer-login";
                await _emailService.SendOfficerInvitationEmailAsync(
                    invitation.Email,
                    invitation.Name,
                    invitation.Role.ToString(),
                    invitation.EmployeeId,
                    temporaryPassword,
                    loginUrl
                );

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Invitation resent successfully. New temporary password: {temporaryPassword}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invitation");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while resending the invitation",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Revoke an invitation
        /// </summary>
        [HttpPost("invitations/{id}/revoke")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> RevokeInvitation(int id)
        {
            try
            {
                var invitation = await _context.OfficerInvitations.FindAsync(id);
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
                        Message = "Cannot revoke an accepted invitation"
                    });
                }

                invitation.Status = InvitationStatus.Revoked;
                invitation.UpdatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Admin";
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
                _logger.LogError(ex, "Error revoking invitation");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while revoking the invitation",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var random = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }
            
            var result = new char[12];
            for (int i = 0; i < 12; i++)
            {
                result[i] = chars[random[i] % chars.Length];
            }
            
            return new string(result);
        }
    }
}
