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
                    if (existingUser.Role != UserRole.User)
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
                
                // Log OTP only in development
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("OTP generated for {Identifier}: {OTP} (Development mode only)", request.Email, otpCode);
                }

                var isNewUser = existingUser == null;
                var message = isNewUser 
                    ? $"OTP sent to {request.Email}. A new account will be created upon verification."
                    : $"OTP sent successfully to {request.Email}";

                // Response data - include OTP only in development
                var responseData = new Dictionary<string, object>
                {
                    { "ExpiresIn", 300 }, // 5 minutes in seconds
                    { "ExpiresAt", otpVerification.ExpiryTime },
                    { "IsNewUser", isNewUser },
                    { "EmailSent", emailSent }
                };

                if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    responseData.Add("OtpCode", otpCode);
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = message,
                    Data = responseData
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, 
                    "Database error while sending OTP for {Email}. Inner exception: {InnerException}. Stack trace: {StackTrace}", 
                    request.Email, 
                    dbEx.InnerException?.Message ?? "None",
                    dbEx.StackTrace);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Database error occurred while sending OTP. Please try again later.",
                    Errors = new List<string> { "Database operation failed" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error sending OTP for {Email}. Exception type: {ExceptionType}, Message: {Message}, Inner exception: {InnerException}, Stack trace: {StackTrace}", 
                    request.Email,
                    ex.GetType().Name,
                    ex.Message,
                    ex.InnerException?.Message ?? "None",
                    ex.StackTrace);
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
                        PhoneNumber = !isEmail ? request.Identifier : null, // Null for email-only users
                        Name = userName,
                        Role = UserRole.User,
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
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Database error during OTP verification for {Identifier}. Purpose: {Purpose}, Inner exception: {InnerException}, Stack trace: {StackTrace}",
                    request.Identifier,
                    request.Purpose,
                    dbEx.InnerException?.Message ?? "None",
                    dbEx.StackTrace);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Database error occurred during OTP verification. Please try again.",
                    Errors = new List<string> { "Database operation failed" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error verifying OTP for {Identifier}. Purpose: {Purpose}, Exception type: {ExceptionType}, Message: {Message}, Inner exception: {InnerException}, Stack trace: {StackTrace}",
                    request.Identifier,
                    request.Purpose,
                    ex.GetType().Name,
                    ex.Message,
                    ex.InnerException?.Message ?? "None",
                    ex.StackTrace);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to verify OTP. Please try again.",
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
        /// Seed admin password manually (Development only)
        /// </summary>
        [HttpPost("seed-admin-password")]
        public async Task<ActionResult<ApiResponse>> SeedAdminPassword()
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "This endpoint is only available in development environment",
                        Errors = new List<string> { "Unauthorized" }
                    });
                }

                // Find admin user
                var admin = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "admin@gmail.com" && u.Role == UserRole.Admin);

                if (admin == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Errors = new List<string> { "User not found" }
                    });
                }

                // Set default password
                var defaultPassword = "admin@123";
                admin.PasswordHash = _passwordHasher.HashPassword(defaultPassword);
                admin.EmployeeId = "ADMIN001";
                admin.IsActive = true;
                admin.UpdatedBy = "PasswordSeeder";
                admin.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin password seeded successfully for {Email}", admin.Email);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Admin password seeded successfully",
                    Data = new
                    {
                        Email = admin.Email,
                        Password = defaultPassword,
                        Role = admin.Role.ToString(),
                        EmployeeId = admin.EmployeeId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding admin password");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to seed admin password",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Officer login with email and password - Uses Officers table
        /// </summary>
        [HttpPost("officer-login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> OfficerLogin([FromBody] OfficerLoginRequest request)
        {
            try
            {
                _logger.LogInformation("=== Officer Login Attempt Started ===");
                _logger.LogInformation("Officer login attempt for: {Email}", request.Email);

                // Find officer by email in Officers table
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Email == request.Email);

                if (officer == null)
                {
                    _logger.LogWarning("Login failed - officer not found: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                _logger.LogInformation("Officer found - ID: {OfficerId}, Name: {Name}, Role: {Role}, IsActive: {IsActive}", 
                    officer.Id, officer.Name, officer.Role, officer.IsActive);
                _logger.LogInformation("Officer PasswordHash exists: {HasPassword}, Length: {Length}", 
                    !string.IsNullOrEmpty(officer.PasswordHash), officer.PasswordHash?.Length ?? 0);
                _logger.LogInformation("Officer EmployeeId: {EmployeeId}, LoginAttempts: {LoginAttempts}", 
                    officer.EmployeeId, officer.LoginAttempts);

                // TEMPORARILY DISABLED: Account locking functionality
                // TODO: Re-enable when needed for production security
                /*
                // Check if account is locked
                if (officer.LockedUntil.HasValue && officer.LockedUntil > DateTime.UtcNow)
                {
                    var lockRemaining = (officer.LockedUntil.Value - DateTime.UtcNow).Minutes;
                    _logger.LogWarning("Login attempt on locked officer account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Account is locked. Please try again in {lockRemaining} minutes.",
                        Errors = new List<string> { "Account locked due to multiple failed login attempts" }
                    });
                }
                */

                // Check if account is active
                if (!officer.IsActive)
                {
                    _logger.LogWarning("Login attempt on inactive officer account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact administrator.",
                        Errors = new List<string> { "Account inactive" }
                    });
                }

                // Verify password hash exists
                if (string.IsNullOrEmpty(officer.PasswordHash))
                {
                    _logger.LogError("Officer account without password hash: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Password not set for this account. Please contact administrator.",
                        Errors = new List<string> { "Password configuration error" }
                    });
                }

                _logger.LogInformation("Attempting password verification for officer: {Email}", request.Email);
                _logger.LogInformation("Password provided length: {Length}", request.Password?.Length ?? 0);
                
                // Validate password is provided
                if (string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Empty password provided for officer login: {Email}", request.Email);
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Password is required",
                        Errors = new List<string> { "Invalid request" }
                    });
                }
                
                // Verify password
                var passwordVerificationResult = _passwordHasher.VerifyPassword(request.Password, officer.PasswordHash);
                _logger.LogInformation("Password verification result: {Result}", passwordVerificationResult);
                
                if (!passwordVerificationResult)
                {
                    // TEMPORARILY DISABLED: Login attempt tracking and account locking
                    // TODO: Re-enable when needed for production security
                    /*
                    // Increment login attempts
                    officer.LoginAttempts++;
                    
                    // Lock account after 5 failed attempts
                    if (officer.LoginAttempts >= 5)
                    {
                        officer.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                        officer.LoginAttempts = 0;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogWarning("Officer account locked after multiple failed attempts: {Email}", request.Email);
                        return Unauthorized(new ApiResponse
                        {
                            Success = false,
                            Message = "Account locked for 30 minutes due to multiple failed login attempts.",
                            Errors = new List<string> { "Too many failed login attempts" }
                        });
                    }

                    await _context.SaveChangesAsync();
                    var attemptsLeft = 5 - officer.LoginAttempts;
                    
                    _logger.LogWarning("Failed login attempt for {Email}. Attempts left: {Attempts}", request.Email, attemptsLeft);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Invalid email or password. {attemptsLeft} attempts remaining.",
                        Errors = new List<string> { "Authentication failed" }
                    });
                    */
                    
                    _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email or password.",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                _logger.LogInformation("Password verified successfully! Proceeding with login for officer: {Email}", request.Email);
                
                // Reset login attempts on successful login
                officer.LoginAttempts = 0;
                officer.LastLoginAt = DateTime.UtcNow;
                officer.UpdatedBy = officer.Email;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Officer login attempts reset and last login updated");

                // Generate JWT token for officer
                _logger.LogInformation("Generating JWT token for officer: {OfficerId}", officer.Id);
                var token = GenerateOfficerJwtToken(officer);
                var refreshToken = Guid.NewGuid().ToString();

                var loginResponse = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    User = new UserDto
                    {
                        Id = officer.Id,
                        Name = officer.Name,
                        Email = officer.Email,
                        PhoneNumber = officer.PhoneNumber,
                        Role = officer.Role.ToString(),
                        IsActive = officer.IsActive,
                        Address = null, // Officers don't have address field
                        EmployeeId = officer.EmployeeId,
                        LastLoginAt = officer.LastLoginAt,
                        MustChangePassword = officer.MustChangePassword,
                        Department = officer.Department
                    }
                };

                _logger.LogInformation("Officer {OfficerId} ({Role}) logged in successfully. MustChangePassword: {MustChangePassword}", 
                    officer.Id, officer.Role, officer.MustChangePassword);
                _logger.LogInformation("=== Officer Login Successful - Token Generated ===");

                var welcomeMessage = officer.MustChangePassword 
                    ? "Welcome! Please change your temporary password." 
                    : $"Welcome back, {officer.Name}!";

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = welcomeMessage,
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
        /// System Admin login with email and password - Uses SystemAdmins table
        /// </summary>
        [HttpPost("admin-login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> AdminLogin([FromBody] OfficerLoginRequest request)
        {
            try
            {
                _logger.LogInformation("=== System Admin Login Attempt Started ===");
                _logger.LogInformation("Admin login attempt for: {Email}", request.Email);

                // Find admin by email in SystemAdmins table
                var admin = await _context.SystemAdmins
                    .FirstOrDefaultAsync(a => a.Email == request.Email);

                if (admin == null)
                {
                    _logger.LogWarning("Login failed - admin not found: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                _logger.LogInformation("Admin found - ID: {AdminId}, Name: {Name}, IsActive: {IsActive}", 
                    admin.Id, admin.Name, admin.IsActive);
                _logger.LogInformation("Admin PasswordHash exists: {HasPassword}", 
                    !string.IsNullOrEmpty(admin.PasswordHash));

                // TEMPORARILY DISABLED: Account locking functionality for admin
                // TODO: Re-enable when needed for production security
                /*
                // Check if account is locked
                if (admin.LockedUntil.HasValue && admin.LockedUntil > DateTime.UtcNow)
                {
                    var lockRemaining = (admin.LockedUntil.Value - DateTime.UtcNow).Minutes;
                    _logger.LogWarning("Login attempt on locked admin account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Account is locked. Please try again in {lockRemaining} minutes.",
                        Errors = new List<string> { "Account locked due to multiple failed login attempts" }
                    });
                }
                */

                // Check if account is active
                if (!admin.IsActive)
                {
                    _logger.LogWarning("Login attempt on inactive admin account: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact system administrator.",
                        Errors = new List<string> { "Account inactive" }
                    });
                }

                // Verify password hash exists
                if (string.IsNullOrEmpty(admin.PasswordHash))
                {
                    _logger.LogError("Admin account without password hash: {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Password not set for this account. Please contact support.",
                        Errors = new List<string> { "Password configuration error" }
                    });
                }

                _logger.LogInformation("Attempting password verification for admin: {Email}", request.Email);
                
                // Verify password
                var passwordVerificationResult = _passwordHasher.VerifyPassword(request.Password, admin.PasswordHash);
                _logger.LogInformation("Password verification result: {Result}", passwordVerificationResult);
                
                if (!passwordVerificationResult)
                {
                    // TEMPORARILY DISABLED: Login attempt tracking and account locking for admin
                    // TODO: Re-enable when needed for production security
                    /*
                    // Increment login attempts
                    admin.LoginAttempts++;
                    
                    // Lock account after 5 failed attempts
                    if (admin.LoginAttempts >= 5)
                    {
                        admin.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                        admin.LoginAttempts = 0;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogWarning("Admin account locked after multiple failed attempts: {Email}", request.Email);
                        return Unauthorized(new ApiResponse
                        {
                            Success = false,
                            Message = "Account locked for 30 minutes due to multiple failed login attempts.",
                            Errors = new List<string> { "Too many failed login attempts" }
                        });
                    }

                    await _context.SaveChangesAsync();
                    var attemptsLeft = 5 - admin.LoginAttempts;
                    
                    _logger.LogWarning("Failed login attempt for admin {Email}. Attempts left: {Attempts}", request.Email, attemptsLeft);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = $"Invalid email or password. {attemptsLeft} attempts remaining.",
                        Errors = new List<string> { "Authentication failed" }
                    });
                    */
                    
                    _logger.LogWarning("Failed login attempt for admin {Email}", request.Email);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email or password.",
                        Errors = new List<string> { "Authentication failed" }
                    });
                }

                _logger.LogInformation("Password verified successfully! Proceeding with login for admin: {Email}", request.Email);
                
                // Reset login attempts on successful login
                admin.LoginAttempts = 0;
                admin.LastLoginAt = DateTime.UtcNow;
                admin.UpdatedBy = admin.Email;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin login attempts reset and last login updated");

                // Generate JWT token for admin
                _logger.LogInformation("Generating JWT token for admin: {AdminId}", admin.Id);
                var token = GenerateAdminJwtToken(admin);
                var refreshToken = Guid.NewGuid().ToString();

                var loginResponse = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    User = new UserDto
                    {
                        Id = admin.Id,
                        Name = admin.Name,
                        Email = admin.Email,
                        PhoneNumber = null,
                        Role = "Admin",
                        IsActive = admin.IsActive,
                        Address = null,
                        EmployeeId = admin.EmployeeId,
                        LastLoginAt = admin.LastLoginAt
                    }
                };

                _logger.LogInformation("Admin {AdminId} logged in successfully", admin.Id);
                _logger.LogInformation("=== Admin Login Successful - Token Generated ===");

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = $"Welcome back, {admin.Name}!",
                    Data = loginResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for {Email}", request.Email);
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

        /// <summary>
        /// Change password for first-time officer login (after invitation)
        /// </summary>
        [HttpPost("change-password-first-time")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePasswordFirstTime([FromBody] FirstTimePasswordChangeRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("officer_id")?.Value ?? User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var officerId))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid authentication token",
                        Errors = new List<string> { "Unauthorized" }
                    });
                }

                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null || !officer.IsActive)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "Officer not found" }
                    });
                }

                // Verify temporary password
                if (!_passwordHasher.VerifyPassword(request.TemporaryPassword, officer.PasswordHash))
                {
                    _logger.LogWarning("Failed first-time password change attempt for officer {OfficerId}", officerId);
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Temporary password is incorrect",
                        Errors = new List<string> { "Invalid temporary password" }
                    });
                }

                // Ensure this is indeed a first-time password change
                if (!officer.MustChangePassword)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Password has already been changed. Use regular password change endpoint.",
                        Errors = new List<string> { "Invalid operation" }
                    });
                }

                // Hash and update new password
                officer.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                officer.MustChangePassword = false;
                officer.PasswordChangedAt = DateTime.UtcNow;
                officer.UpdatedBy = officer.Email;
                officer.UpdatedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("First-time password changed successfully for officer {OfficerId}", officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password changed successfully! Please complete your profile."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing first-time password for officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to change password",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        /// <summary>
        /// Complete officer profile after first login
        /// </summary>
        [HttpPost("complete-profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CompleteProfile([FromBody] CompleteProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("officer_id")?.Value ?? User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var officerId))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid authentication token",
                        Errors = new List<string> { "Unauthorized" }
                    });
                }

                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null || !officer.IsActive)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "Officer not found" }
                    });
                }

                // Update profile
                officer.Name = request.Name;
                officer.PhoneNumber = request.PhoneNumber;
                officer.Department = request.Department;
                officer.UpdatedBy = officer.Email;
                officer.UpdatedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile completed successfully for officer {OfficerId}", officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Profile completed successfully! You can now access your dashboard."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing profile for officer");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to complete profile",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        #region Helper Methods

        private string GenerateJwtToken(User user)
        {
            // Read JWT settings from environment variables first, then fall back to appsettings.json
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? _configuration["JwtSettings:Issuer"] 
                ?? "PMCRMS.API";
            
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? _configuration["JwtSettings:Audience"] 
                ?? "PMCRMS.Client";
            
            var expiryHours = int.Parse(
                Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") 
                ?? _configuration["JwtSettings:ExpiryHours"] 
                ?? "24");

            // Validate JWT secret key length
            if (secretKey.Length < 32)
            {
                _logger.LogError("JWT SecretKey is too short. Current length: {Length} characters. Required: at least 32 characters (256 bits) for HS256 algorithm.", secretKey.Length);
                throw new InvalidOperationException($"JWT SecretKey must be at least 32 characters long. Current length: {secretKey.Length}");
            }

            _logger.LogInformation("JWT SecretKey validation passed. Length: {Length} characters", secretKey.Length);

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

        private string GenerateOfficerJwtToken(Officer officer)
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? _configuration["JwtSettings:Issuer"] 
                ?? "PMCRMS.API";
            
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? _configuration["JwtSettings:Audience"] 
                ?? "PMCRMS.Client";
            
            var expiryHours = int.Parse(
                Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") 
                ?? _configuration["JwtSettings:ExpiryHours"] 
                ?? "24");

            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException($"JWT SecretKey must be at least 32 characters long.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, officer.Id.ToString()),
                new Claim(ClaimTypes.Name, officer.Name),
                new Claim(ClaimTypes.Email, officer.Email),
                new Claim(ClaimTypes.Role, officer.Role.ToString()),
                new Claim("user_id", officer.Id.ToString()),
                new Claim("officer_id", officer.Id.ToString()),
                new Claim("role", officer.Role.ToString()),
                new Claim("employee_id", officer.EmployeeId),
                new Claim("user_type", "Officer")
            };

            if (!string.IsNullOrEmpty(officer.PhoneNumber))
                claims.Add(new Claim(ClaimTypes.MobilePhone, officer.PhoneNumber));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateAdminJwtToken(SystemAdmin admin)
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? _configuration["JwtSettings:Issuer"] 
                ?? "PMCRMS.API";
            
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? _configuration["JwtSettings:Audience"] 
                ?? "PMCRMS.Client";
            
            var expiryHours = int.Parse(
                Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") 
                ?? _configuration["JwtSettings:ExpiryHours"] 
                ?? "24");

            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException($"JWT SecretKey must be at least 32 characters long.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Name),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("user_id", admin.Id.ToString()),
                new Claim("admin_id", admin.Id.ToString()),
                new Claim("role", "Admin"),
                new Claim("employee_id", admin.EmployeeId ?? "ADMIN"),
                new Claim("is_super_admin", admin.IsSuperAdmin.ToString()),
                new Claim("user_type", "SystemAdmin")
            };

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

        /// <summary>
        /// DEVELOPMENT ONLY: Generate password hash
        /// This endpoint should be REMOVED or DISABLED in production
        /// </summary>
        [HttpPost("generate-hash")]
        public ActionResult<ApiResponse<object>> GeneratePasswordHash([FromBody] GenerateHashRequest request)
        {
            #if DEBUG
            try
            {
                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Password is required"
                    });
                }

                var hash = _passwordHasher.HashPassword(request.Password);
                
                var sqlScript = $@"
-- Update Officers with test@123 password and yopmail emails
UPDATE ""Officers""
SET 
    ""Email"" = LOWER(""EmployeeId"") || '@yopmail.com',
    ""PasswordHash"" = '{hash}',
    ""UpdatedDate"" = NOW()
WHERE ""Email"" NOT LIKE '%yopmail.com';

-- Verify the update
SELECT ""Id"", ""EmployeeId"", ""Email"", ""Name"", ""Role""
FROM ""Officers""
ORDER BY ""EmployeeId"";
";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password hash generated successfully",
                    Data = new 
                    {
                        Password = request.Password,
                        Hash = hash,
                        SqlScript = sqlScript
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password hash");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error generating hash"
                });
            }
            #else
            return NotFound(new ApiResponse
            {
                Success = false,
                Message = "This endpoint is only available in development mode"
            });
            #endif
        }

        #endregion

        #region Officer Invitation Endpoints

        /// <summary>
        /// Validate invitation token and retrieve invitation details
        /// </summary>
        [HttpGet("validate-invitation/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> ValidateInvitationToken(string token)
        {
            try
            {
                _logger.LogInformation("Validating invitation token: {Token}", token);

                var invitation = await _context.OfficerInvitations
                    .FirstOrDefaultAsync(i => i.InvitationToken == token);

                if (invitation == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { IsValid = false },
                        Message = "Invalid invitation token"
                    });
                }

                // Check if already accepted
                if (invitation.Status == InvitationStatus.Accepted)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { IsValid = false },
                        Message = "This invitation has already been accepted"
                    });
                }

                // Check if expired
                if (invitation.ExpiresAt <= DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _context.SaveChangesAsync();

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { IsValid = false },
                        Message = "This invitation has expired"
                    });
                }

                // Check if revoked
                if (invitation.Status == InvitationStatus.Revoked)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { IsValid = false },
                        Message = "This invitation has been revoked"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    { 
                        IsValid = true,
                        Name = invitation.Name,
                        Email = invitation.Email,
                        Role = invitation.Role.ToString()
                    },
                    Message = "Valid invitation"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error validating invitation"
                });
            }
        }

        /// <summary>
        /// Set password for invited officer and create their account
        /// </summary>
        [HttpPost("set-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> SetPassword([FromBody] SetPasswordRequest request)
        {
            try
            {
                _logger.LogInformation("Setting password for invitation token: {Token}", request.Token);

                // Validate password match
                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Passwords do not match"
                    });
                }

                // Validate password strength
                if (!IsPasswordStrong(request.Password))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Password must be at least 8 characters long and contain uppercase, lowercase, number and special character"
                    });
                }

                // Find invitation
                var invitation = await _context.OfficerInvitations
                    .FirstOrDefaultAsync(i => i.InvitationToken == request.Token);

                if (invitation == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid invitation token"
                    });
                }

                // Check if already accepted
                if (invitation.Status == InvitationStatus.Accepted)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "This invitation has already been accepted"
                    });
                }

                // Check if expired
                if (invitation.ExpiresAt <= DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _context.SaveChangesAsync();

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "This invitation has expired"
                    });
                }

                // Check if revoked
                if (invitation.Status == InvitationStatus.Revoked)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "This invitation has been revoked"
                    });
                }

                // Hash password
                var passwordHash = _passwordHasher.HashPassword(request.Password);

                // Create officer account
                var officer = new Officer
                {
                    Name = invitation.Name,
                    Email = invitation.Email,
                    PhoneNumber = invitation.PhoneNumber,
                    Role = invitation.Role,
                    EmployeeId = invitation.EmployeeId,
                    Department = invitation.Department ?? string.Empty,
                    PasswordHash = passwordHash,
                    MustChangePassword = false, // Password already set
                    PasswordChangedAt = DateTime.UtcNow,
                    IsActive = true,
                    InvitationId = invitation.Id,
                    CreatedBy = invitation.Email,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Officers.Add(officer);
                
                // Save officer first to get the ID
                await _context.SaveChangesAsync();

                // Now update invitation status with the officer ID
                invitation.Status = InvitationStatus.Accepted;
                invitation.AcceptedAt = DateTime.UtcNow;
                invitation.OfficerId = officer.Id;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Officer account created successfully for {Email} with ID {OfficerId}", 
                    officer.Email, officer.Id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    { 
                        UserId = officer.Id,
                        Email = officer.Email,
                        Message = "Password set successfully! You can now login."
                    },
                    Message = "Password set successfully! You can now login to PMCRMS."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting password");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error setting password"
                });
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c));

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        #endregion

        #region Officer Password Management

        /// <summary>
        /// Change password for authenticated officer
        /// </summary>
        [HttpPost("officer/change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> OfficerChangePassword([FromBody] OfficerChangePasswordRequest request)
        {
            try
            {
                var officerIdClaim = User.FindFirst("officer_id")?.Value;
                if (string.IsNullOrEmpty(officerIdClaim) || !int.TryParse(officerIdClaim, out var officerId))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid authentication token",
                        Errors = new List<string> { "Unauthorized" }
                    });
                }

                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null || !officer.IsActive)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "Officer not found" }
                    });
                }

                // Verify current password
                if (!_passwordHasher.VerifyPassword(request.CurrentPassword, officer.PasswordHash))
                {
                    _logger.LogWarning("Failed password change attempt for officer {OfficerId}", officerId);
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect",
                        Errors = new List<string> { "Invalid current password" }
                    });
                }

                // Hash and update new password
                officer.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                officer.PasswordChangedAt = DateTime.UtcNow;
                officer.UpdatedBy = officer.Email;
                officer.UpdatedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for officer {OfficerId}", officerId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing officer password");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to change password",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        /// <summary>
        /// Request password reset for officer (forgot password)
        /// </summary>
        [HttpPost("officer/forgot-password")]
        public async Task<ActionResult<ApiResponse>> OfficerForgotPassword([FromBody] OfficerForgotPasswordRequest request)
        {
            try
            {
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Email == request.Email && o.IsActive);

                // Always return success to prevent email enumeration
                if (officer == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "If an account exists with this email, a password reset link has been sent."
                    });
                }

                // Invalidate previous reset tokens
                var previousTokens = await _context.OfficerPasswordResets
                    .Where(r => r.OfficerId == officer.Id && !r.IsUsed && r.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in previousTokens)
                {
                    token.IsUsed = true;
                    token.UsedAt = DateTime.UtcNow;
                }

                // Generate reset token (32-character hex string)
                var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

                // Create password reset record
                var passwordReset = new OfficerPasswordReset
                {
                    OfficerId = officer.Id,
                    ResetToken = resetToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry
                    IsUsed = false,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                };

                _context.OfficerPasswordResets.Add(passwordReset);
                await _context.SaveChangesAsync();

                // Send reset email
                var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
                var resetLink = $"{frontendUrl}/officer/reset-password?token={resetToken}";

                await _emailService.SendOfficerPasswordResetEmailAsync(
                    officer.Email,
                    officer.Name,
                    resetToken,
                    resetLink
                );

                _logger.LogInformation("Password reset email sent to officer {OfficerId}", officer.Id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "If an account exists with this email, a password reset link has been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset request");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to process password reset request",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        /// <summary>
        /// Reset officer password using reset token
        /// </summary>
        [HttpPost("officer/reset-password")]
        public async Task<ActionResult<ApiResponse>> OfficerResetPassword([FromBody] OfficerResetPasswordRequest request)
        {
            try
            {
                // Find valid reset token
                var passwordReset = await _context.OfficerPasswordResets
                    .Include(r => r.Officer)
                    .FirstOrDefaultAsync(r => 
                        r.ResetToken == request.Token && 
                        !r.IsUsed && 
                        r.ExpiresAt > DateTime.UtcNow);

                if (passwordReset == null || passwordReset.Officer == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token",
                        Errors = new List<string> { "The password reset link is invalid or has expired. Please request a new one." }
                    });
                }

                var officer = passwordReset.Officer;

                if (!officer.IsActive)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Officer account is inactive",
                        Errors = new List<string> { "This account has been deactivated" }
                    });
                }

                // Update password
                officer.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                officer.PasswordChangedAt = DateTime.UtcNow;
                officer.UpdatedBy = officer.Email;
                officer.UpdatedDate = DateTime.UtcNow;

                // Mark token as used
                passwordReset.IsUsed = true;
                passwordReset.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for officer {OfficerId}", officer.Id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password has been reset successfully. You can now login with your new password."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting officer password");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to reset password",
                    Errors = new List<string> { "Internal server error" }
                });
            }
        }

        /// <summary>
        /// Validate password reset token (check if token is valid)
        /// </summary>
        [HttpGet("officer/validate-reset-token/{token}")]
        public async Task<ActionResult<ApiResponse>> ValidateResetToken(string token)
        {
            try
            {
                var passwordReset = await _context.OfficerPasswordResets
                    .Include(r => r.Officer)
                    .FirstOrDefaultAsync(r => 
                        r.ResetToken == token && 
                        !r.IsUsed && 
                        r.ExpiresAt > DateTime.UtcNow);

                if (passwordReset == null || passwordReset.Officer == null || !passwordReset.Officer.IsActive)
                {
                    return Ok(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Token is valid",
                    Data = new
                    {
                        OfficerName = passwordReset.Officer.Name,
                        Email = passwordReset.Officer.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to validate token"
                });
            }
        }

        #endregion
    }
}

public class GenerateHashRequest
{
    public string Password { get; set; } = string.Empty;
}

public class SetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
