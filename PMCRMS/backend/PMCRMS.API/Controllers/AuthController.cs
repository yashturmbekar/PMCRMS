using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        public AuthController(
            PMCRMSDbContext context, 
            IConfiguration configuration, 
            ILogger<AuthController> logger,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [HttpPost("send-otp")]
        public async Task<ActionResult<ApiResponse>> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                _logger.LogInformation("OTP request received for: {Identifier}", request.Email);

                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email format",
                        Errors = new List<string> { "Please provide a valid email address" }
                    });
                }

                // Check if user exists - if it's an officer account, deny OTP login
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
                
                if (existingUser != null)
                {
                    // Only allow OTP login for applicants
                    if (existingUser.Role != UserRole.Applicant)
                    {
                        _logger.LogWarning("OTP login attempted for officer account: {Email}", request.Email);
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "Officer accounts must use password-based login",
                            Errors = new List<string> { "Invalid login method for officer accounts" }
                        });
                    }
                }
                
                // For new users, OTP will serve as registration
                // User will be created during OTP verification

                // Invalidate previous OTPs for this identifier
                var previousOtps = await _context.OtpVerifications
                    .Where(o => o.Identifier == request.Email && o.IsActive && !o.IsUsed)
                    .ToListAsync();
                
                foreach (var otp in previousOtps)
                {
                    otp.IsActive = false;
                }

                // Generate OTP (6-digit)
                var otpCode = GenerateOtp();
                
                // Save OTP to database with 5-minute expiry
                var otpVerification = new OtpVerification
                {
                    Identifier = request.Email,
                    OtpCode = otpCode,
                    Purpose = request.Purpose,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5), // 5 minutes expiry
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP saved to database. ID: {OtpId}, Expires at: {ExpiryTime}", 
                    otpVerification.Id, otpVerification.ExpiryTime);

                // Send OTP via email
                var emailSent = await _emailService.SendOtpEmailAsync(request.Email, otpCode, request.Purpose);
                
                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send OTP email to {Email}, but OTP was generated and saved to database", request.Email);
                }
                else
                {
                    _logger.LogInformation("OTP email sent successfully to {Email}", request.Email);
                }
                
                // Log OTP for development/testing (Remove in production)
                _logger.LogInformation("OTP generated for {Identifier}: {OTP} (Remove this log in production)", request.Email, otpCode);

                var isNewUser = existingUser == null;
                var message = isNewUser 
                    ? $"OTP sent to {request.Email}. A new account will be created upon verification."
                    : $"OTP sent successfully to {request.Email}";

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = message,
                    Data = new { 
                        ExpiresIn = 300, // 5 minutes in seconds
                        ExpiresAt = otpVerification.ExpiryTime,
                        IsNewUser = isNewUser,
                        EmailSent = emailSent,
                        // Remove in production - for testing only
                        OtpCode = otpCode
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP for {Email}", request.Email);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to send OTP. Please try again later.",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                _logger.LogInformation("OTP verification request for: {Identifier}, Purpose: {Purpose}", 
                    request.Identifier, request.Purpose);

                // Find valid OTP with detailed logging
                var allOtps = await _context.OtpVerifications
                    .Where(o => o.Identifier == request.Identifier && o.Purpose == request.Purpose)
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} OTP records for {Identifier}", allOtps.Count, request.Identifier);

                var otpVerification = allOtps
                    .Where(o => o.IsActive && !o.IsUsed && o.ExpiryTime > DateTime.UtcNow)
                    .FirstOrDefault();

                if (otpVerification == null)
                {
                    var latestOtp = allOtps.FirstOrDefault();
                    if (latestOtp != null)
                    {
                        _logger.LogWarning(
                            "OTP validation failed for {Identifier}. Latest OTP: IsActive={IsActive}, IsUsed={IsUsed}, ExpiryTime={ExpiryTime}, Now={Now}", 
                            request.Identifier, latestOtp.IsActive, latestOtp.IsUsed, latestOtp.ExpiryTime, DateTime.UtcNow);
                    }
                    else
                    {
                        _logger.LogWarning("No OTP found for {Identifier}", request.Identifier);
                    }

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired OTP. Please request a new OTP.",
                        Errors = new List<string> { "OTP verification failed" }
                    });
                }

                _logger.LogInformation("OTP found. Verifying code for {Identifier}", request.Identifier);

                if (otpVerification.OtpCode != request.OtpCode)
                {
                    otpVerification.AttemptCount++;
                    otpVerification.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Invalid OTP attempt {Attempt}/3 for {Identifier}", 
                        otpVerification.AttemptCount, request.Identifier);

                    if (otpVerification.AttemptCount >= 3)
                    {
                        otpVerification.IsActive = false;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogWarning("OTP deactivated for {Identifier} due to max attempts", request.Identifier);

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
                        Message = $"Invalid OTP code. {3 - otpVerification.AttemptCount} attempts remaining.",
                        Errors = new List<string> { "OTP verification failed" }
                    });
                }

                _logger.LogInformation("OTP code verified successfully for {Identifier}", request.Identifier);

                // Mark OTP as used
                otpVerification.IsUsed = true;
                otpVerification.VerifiedAt = DateTime.UtcNow;
                otpVerification.UpdatedDate = DateTime.UtcNow;

                // Find or create user (automatic registration via OTP)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);

                var isNewRegistration = false;

                if (user == null)
                {
                    // Automatically create new user account via OTP verification
                    _logger.LogInformation("Creating new user account for {Identifier}", request.Identifier);
                    
                    var isEmail = request.Identifier.Contains("@");
                    var userName = isEmail 
                        ? request.Identifier.Split('@')[0] 
                        : $"User_{request.Identifier.Substring(request.Identifier.Length - 4)}";
                    
                    user = new User
                    {
                        Email = isEmail ? request.Identifier : $"{Guid.NewGuid().ToString().Substring(0, 8)}@temp.pmcrms.gov.in",
                        PhoneNumber = !isEmail ? request.Identifier : "0000000000", // Temporary phone number
                        Name = userName,
                        Role = UserRole.Applicant,
                        IsActive = true,
                        CreatedBy = "OTP_Registration",
                        CreatedDate = DateTime.UtcNow
                    };
                    
                    _context.Users.Add(user);
                    isNewRegistration = true;
                    _logger.LogInformation("New user account created via OTP for: {Identifier}, Name: {Name}", 
                        request.Identifier, userName);
                }
                else
                {
                    _logger.LogInformation("Existing user found for {Identifier}. User ID: {UserId}", 
                        request.Identifier, user.Id);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP marked as used and user saved to database");

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedBy = user.Email ?? user.PhoneNumber ?? "System";
                user.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User last login updated for {UserId}", user.Id);

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
                        Address = user.Address,
                        EmployeeId = user.EmployeeId,
                        LastLoginAt = user.LastLoginAt
                    }
                };

                _logger.LogInformation("User {UserId} logged in successfully via OTP. New registration: {IsNewRegistration}", 
                    user.Id, isNewRegistration);

                var successMessage = isNewRegistration 
                    ? "Account created and logged in successfully! Welcome to PMCRMS."
                    : "Login successful";

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = successMessage,
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

        /// <summary>
        /// Officer login with email and password
        /// </summary>
        [HttpPost("officer-login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> OfficerLogin([FromBody] OfficerLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Officer login attempt for: {Email}", request.Email);

                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                // Check if account is locked
                if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                {
                    var lockRemaining = (user.LockedUntil.Value - DateTime.UtcNow).Minutes;
                    _logger.LogWarning("Login attempt on locked account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Account is locked. Please try again in {lockRemaining} minutes.",
                        Errors = new List<string> { "Account locked due to multiple failed login attempts" }
                    });
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt on inactive account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact administrator.",
                        Errors = new List<string> { "Account inactive" }
                    });
                }

                // Verify this is an officer account (not Applicant)
                if (user.Role == UserRole.Applicant)
                {
                    _logger.LogWarning("Officer login attempted on applicant account: {Email}", request.Email);
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "This is an applicant account. Please use OTP-based login.",
                        Errors = new List<string> { "Invalid login method" }
                    });
                }

                // Verify password hash exists
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    _logger.LogError("Officer account without password hash: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Password not set for this account. Please contact administrator.",
                        Errors = new List<string> { "Password configuration error" }
                    });
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                {
                    // Increment login attempts
                    user.LoginAttempts++;
                    
                    // Lock account after 5 failed attempts
                    if (user.LoginAttempts >= 5)
                    {
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                        user.LoginAttempts = 0;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogWarning("Account locked after multiple failed attempts: {Email}", request.Email);
                        return Unauthorized(new ApiResponse
                        {
                            Success = false,
                            Message = "Account locked for 30 minutes due to multiple failed login attempts.",
                            Errors = new List<string> { "Too many failed login attempts" }
                        });
                    }

                    await _context.SaveChangesAsync();
                    var attemptsLeft = 5 - user.LoginAttempts;
                    
                    _logger.LogWarning("Failed login attempt for {Email}. Attempts left: {Attempts}", request.Email, attemptsLeft);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Invalid email or password. {attemptsLeft} attempts remaining.",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                // Reset login attempts on successful login
                user.LoginAttempts = 0;
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedBy = user.Email;
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
                        Address = user.Address,
                        EmployeeId = user.EmployeeId,
                        LastLoginAt = user.LastLoginAt
                    }
                };

                _logger.LogInformation("Officer {UserId} ({Role}) logged in successfully", user.Id, user.Role);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = $"Welcome back, {user.Name}!",
                    Data = loginResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during officer login for {Email}", request.Email);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later.",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid authentication token",
                        Errors = new List<string> { "Unauthorized" }
                    });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "User not found or inactive",
                        Errors = new List<string> { "User not found" }
                    });
                }

                // Verify current password
                if (string.IsNullOrEmpty(user.PasswordHash) || 
                    !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Failed password change attempt for user {UserId}", userId);
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect",
                        Errors = new List<string> { "Invalid current password" }
                    });
                }

                // Hash and update new password
                user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                user.UpdatedBy = user.Email;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to change password",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        #region Helper Methods

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

            // Add employee ID for officers
            if (!string.IsNullOrEmpty(user.EmployeeId))
                claims.Add(new Claim("employee_id", user.EmployeeId));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateOtp()
        {
            // Generate cryptographically secure 6-digit OTP
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomNumber = BitConverter.ToUInt32(bytes, 0);
            return (randomNumber % 900000 + 100000).ToString();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}