using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(PMCRMSDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("send-otp")]
        public async Task<ActionResult<ApiResponse>> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                _logger.LogInformation("OTP request received for: {Identifier}", request.Email);

                // Generate OTP (6-digit)
                var otpCode = new Random().Next(100000, 999999).ToString();
                
                // Save OTP to database
                var otpVerification = new OtpVerification
                {
                    Identifier = request.Email,
                    OtpCode = otpCode,
                    Purpose = request.Purpose,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10), // 10 minutes expiry
                    IsActive = true
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                // TODO: Send OTP via email/SMS service
                _logger.LogInformation("OTP generated for {Identifier}: {OTP}", request.Email, otpCode);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "OTP sent successfully to your email/phone",
                    Data = new { ExpiresIn = 600 } // 10 minutes in seconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to send OTP",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                _logger.LogInformation("OTP verification request for: {Identifier}", request.Identifier);

                // Find valid OTP
                var otpVerification = await _context.OtpVerifications
                    .Where(o => o.Identifier == request.Identifier 
                               && o.Purpose == request.Purpose 
                               && o.IsActive 
                               && !o.IsUsed 
                               && o.ExpiryTime > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedDate)
                    .FirstOrDefaultAsync();

                if (otpVerification == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired OTP",
                        Errors = new List<string> { "OTP verification failed" }
                    });
                }

                if (otpVerification.OtpCode != request.OtpCode)
                {
                    otpVerification.AttemptCount++;
                    await _context.SaveChangesAsync();

                    if (otpVerification.AttemptCount >= 3)
                    {
                        otpVerification.IsActive = false;
                        await _context.SaveChangesAsync();
                        
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "Maximum OTP attempts exceeded. Please request a new OTP.",
                            Errors = new List<string> { "OTP verification failed" }
                        });
                    }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid OTP code",
                        Errors = new List<string> { "OTP verification failed" }
                    });
                }

                // Mark OTP as used
                otpVerification.IsUsed = true;
                otpVerification.VerifiedAt = DateTime.UtcNow;

                // Find or create user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);

                if (user == null && request.Purpose == "REGISTRATION")
                {
                    // Create new user for registration
                    user = new User
                    {
                        Email = request.Identifier.Contains("@") ? request.Identifier : "",
                        PhoneNumber = !request.Identifier.Contains("@") ? request.Identifier : "",
                        Name = "New User", // Will be updated during profile completion
                        Role = UserRole.Applicant,
                        IsActive = true,
                        CreatedBy = "System"
                    };
                    
                    _context.Users.Add(user);
                }

                if (user == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found. Please register first.",
                        Errors = new List<string> { "User not found" }
                    });
                }

                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = GenerateJwtToken(user);
                var refreshToken = Guid.NewGuid().ToString();

                var loginResponse = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Role = user.Role.ToString(),
                        IsActive = user.IsActive,
                        Address = user.Address
                    }
                };

                _logger.LogInformation("User {UserId} logged in successfully", user.Id);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = loginResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to verify OTP",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpGet("test")]
        public ActionResult<ApiResponse> Test()
        {
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "PMCRMS API is running successfully!",
                Data = new 
                { 
                    Version = "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    Timestamp = DateTime.UtcNow
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            var issuer = jwtSettings["Issuer"] ?? "PMCRMS.API";
            var audience = jwtSettings["Audience"] ?? "PMCRMS.Client";
            var expiryHours = int.Parse(jwtSettings["ExpiryHours"] ?? "24");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.PhoneNumber ?? user.Email ?? "Unknown"),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim("role", user.Role.ToString())
            };

            // Add email claim if available
            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            // Add phone claim if available
            if (!string.IsNullOrEmpty(user.PhoneNumber))
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}