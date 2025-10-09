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
    public class UserController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(PMCRMSDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive,
                    Address = user.Address
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve profile",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Update user fields
                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;

                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Address))
                    user.Address = request.Address;

                user.UpdatedBy = userId.ToString();
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive,
                    Address = user.Address
                };

                _logger.LogInformation("Profile updated for user {UserId}", userId);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update profile",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Filter by role if provided
                if (role.HasValue)
                {
                    query = query.Where(u => u.Role == role.Value);
                }

                // Filter by active status if provided
                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = u.Name ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Role = u.Role.ToString(),
                        IsActive = u.IsActive,
                        Address = u.Address
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = true,
                    Message = $"Users retrieved successfully. Total: {totalCount}, Page: {page}/{(int)Math.Ceiling((double)totalCount / pageSize)}",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve users",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,JuniorEngineer,AssistantEngineer,ExecutiveEngineer,CityEngineer")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive,
                    Address = user.Address
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve user",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Parse and validate role
                if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid role",
                        Errors = new List<string> { $"Valid roles: {string.Join(", ", Enum.GetNames<UserRole>())}" }
                    });
                }

                // Only Admin can assign Admin role
                if (newRole == UserRole.Admin && currentUserRole != UserRole.Admin)
                {
                    return Forbid();
                }

                // Cannot change own role
                if (id == currentUserId)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot change your own role"
                    });
                }

                user.Role = newRole;
                user.UpdatedBy = currentUserId.ToString();
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} role updated to {Role} by user {CurrentUserId}", id, newRole, currentUserId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "User role updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update user role",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Cannot deactivate own account
                if (id == currentUserId)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Cannot change your own status"
                    });
                }

                user.IsActive = request.IsActive;
                user.UpdatedBy = currentUserId.ToString();
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} status updated to {Status} by user {CurrentUserId}", 
                    id, request.IsActive ? "Active" : "Inactive", currentUserId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"User {(request.IsActive ? "activated" : "deactivated")} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update user status",
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