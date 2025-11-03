using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Security.Cryptography;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for secure document downloads with OTP-based authentication
    /// Implements time-limited tokens and comprehensive audit logging
    /// </summary>
    public class DocumentDownloadService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<DocumentDownloadService> _logger;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public DocumentDownloadService(
            PMCRMSDbContext context,
            ILogger<DocumentDownloadService> logger,
            IEmailService emailService,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _env = env;
            _configuration = configuration;
        }

        /// <summary>
        /// Request access to download documents by generating and sending OTP
        /// </summary>
        /// <param name="applicationNumber">Application number to download documents for</param>
        /// <param name="email">Applicant's registered email address</param>
        /// <param name="ipAddress">IP address of the requester</param>
        /// <returns>Success status and message</returns>
        public async Task<(bool Success, string Message)> RequestAccessAsync(
            string applicationNumber,
            string email,
            string? ipAddress = null)
        {
            try
            {
                // Find application by number
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.ApplicationNumber == applicationNumber);

                if (application == null)
                {
                    _logger.LogWarning("Download access request for non-existent application: {ApplicationNumber}", applicationNumber);
                    return (false, "Application not found. Please check your application number.");
                }

                // Verify email matches applicant
                if (!string.Equals(application.Applicant.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Email mismatch for application {ApplicationNumber}. Provided: {ProvidedEmail}, Expected: {ExpectedEmail}",
                        applicationNumber, email, application.Applicant.Email);
                    return (false, "Email address does not match our records for this application.");
                }

                // Check if certificate has been issued (Status 22 or 23)
                if (application.CurrentStatus != ApplicationCurrentStatus.CertificateIssued &&
                    application.CurrentStatus != ApplicationCurrentStatus.Completed)
                {
                    _logger.LogWarning("Download attempt for application {ApplicationNumber} with status {Status}",
                        applicationNumber, application.CurrentStatus);
                    return (false, "Certificate has not been issued yet. Please wait for approval.");
                }

                // Rate limiting: Check daily OTP request limit
                var today = DateTime.UtcNow.Date;
                var todayRequests = await _context.DownloadTokens
                    .Where(t => t.ApplicationId == application.Id &&
                                t.CreatedAt >= today)
                    .CountAsync();

                var maxDailyRequests = int.Parse(_configuration["AppSettings:MaxDailyOtpRequests"] ?? "3");
                if (todayRequests >= maxDailyRequests)
                {
                    _logger.LogWarning("Daily OTP request limit exceeded for application {ApplicationNumber}", applicationNumber);
                    return (false, "Daily OTP request limit reached. Please try again tomorrow or contact support.");
                }

                // Generate 6-digit OTP
                string otp = GenerateOtp();

                // Generate cryptographically secure token (will be activated after OTP verification)
                string downloadToken = GenerateSecureToken();

                var tokenExpiryHours = int.Parse(_configuration["AppSettings:DocumentTokenExpiryHours"] ?? "48");
                var otpExpiryMinutes = int.Parse(_configuration["AppSettings:DocumentOtpExpiryMinutes"] ?? "30");

                // Create download token record
                var tokenRecord = new DownloadToken
                {
                    ApplicationId = application.Id,
                    Token = downloadToken,
                    Otp = otp,
                    Email = email,
                    ExpiresAt = DateTime.UtcNow.AddHours(tokenExpiryHours),
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(otpExpiryMinutes),
                    IsOtpVerified = false,
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow,
                    FailedAttempts = 0,
                    RequestIpAddress = ipAddress
                };

                _context.DownloadTokens.Add(tokenRecord);
                await _context.SaveChangesAsync();

                // Send OTP via email
                bool emailSent = await SendOtpEmailAsync(
                    email,
                    application.Applicant.Name,
                    applicationNumber,
                    otp);

                if (!emailSent)
                {
                    _logger.LogError("Failed to send OTP email for application {ApplicationNumber}", applicationNumber);
                    return (false, "Failed to send OTP email. Please try again or contact support.");
                }

                _logger.LogInformation("OTP sent successfully for application {ApplicationNumber} to {Email}",
                    applicationNumber, email);

                return (true, $"OTP has been sent to {email}. Please check your email and enter the code to proceed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting download access for application {ApplicationNumber}", applicationNumber);
                return (false, "An error occurred while processing your request. Please try again.");
            }
        }

        /// <summary>
        /// Verify OTP and activate download token
        /// </summary>
        /// <param name="applicationNumber">Application number</param>
        /// <param name="otp">6-digit OTP code</param>
        /// <returns>Success status, message, and download token if successful</returns>
        public async Task<(bool Success, string Message, string? DownloadToken, string? ApplicantName)> VerifyOtpAsync(
            string applicationNumber,
            string otp)
        {
            try
            {
                // Find application
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.ApplicationNumber == applicationNumber);

                if (application == null)
                {
                    return (false, "Application not found.", null, null);
                }

                // Find most recent unverified token for this application
                var tokenRecord = await _context.DownloadTokens
                    .Where(t => t.ApplicationId == application.Id &&
                                !t.IsOtpVerified &&
                                !t.IsRevoked)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (tokenRecord == null)
                {
                    _logger.LogWarning("No pending OTP found for application {ApplicationNumber}", applicationNumber);
                    return (false, "No pending OTP request found. Please request a new OTP.", null, null);
                }

                // Check if OTP has expired
                if (DateTime.UtcNow > tokenRecord.OtpExpiresAt)
                {
                    _logger.LogWarning("Expired OTP verification attempt for application {ApplicationNumber}", applicationNumber);
                    tokenRecord.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return (false, "OTP has expired. Please request a new one.", null, null);
                }

                // Check failed attempts (brute force protection)
                var maxFailedAttempts = int.Parse(_configuration["AppSettings:MaxFailedAttempts"] ?? "5");
                if (tokenRecord.FailedAttempts >= maxFailedAttempts)
                {
                    _logger.LogWarning("Maximum failed OTP attempts reached for application {ApplicationNumber}", applicationNumber);
                    tokenRecord.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return (false, "Too many failed attempts. Please request a new OTP.", null, null);
                }

                // Verify OTP
                if (tokenRecord.Otp != otp.Trim())
                {
                    tokenRecord.FailedAttempts++;
                    await _context.SaveChangesAsync();

                    maxFailedAttempts = int.Parse(_configuration["AppSettings:MaxFailedAttempts"] ?? "5");
                    int attemptsLeft = maxFailedAttempts - tokenRecord.FailedAttempts;
                    _logger.LogWarning("Invalid OTP for application {ApplicationNumber}. Attempts left: {AttemptsLeft}",
                        applicationNumber, attemptsLeft);

                    return (false, $"Invalid OTP. {attemptsLeft} attempt(s) remaining.", null, null);
                }

                // OTP is valid - activate token
                tokenRecord.IsOtpVerified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP verified successfully for application {ApplicationNumber}. Token activated.",
                    applicationNumber);

                return (true,
                    "OTP verified successfully! You can now download your documents.",
                    tokenRecord.Token,
                    application.Applicant.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for application {ApplicationNumber}", applicationNumber);
                return (false, "An error occurred while verifying OTP. Please try again.", null, null);
            }
        }

        /// <summary>
        /// Validate download token and return application details
        /// </summary>
        /// <param name="token">Download token</param>
        /// <returns>Application if token is valid, null otherwise</returns>
        public async Task<Application?> ValidateDownloadTokenAsync(string token)
        {
            try
            {
                var tokenRecord = await _context.DownloadTokens
                    .Include(t => t.Application)
                        .ThenInclude(a => a!.Applicant)
                    .FirstOrDefaultAsync(t => t.Token == token &&
                                              t.IsOtpVerified &&
                                              !t.IsRevoked);

                if (tokenRecord == null)
                {
                    _logger.LogWarning("Invalid or unverified download token");
                    return null;
                }

                // Check if token has expired
                if (DateTime.UtcNow > tokenRecord.ExpiresAt)
                {
                    _logger.LogWarning("Expired download token for application {ApplicationId}", tokenRecord.ApplicationId);
                    tokenRecord.IsRevoked = true;
                    await _context.SaveChangesAsync();
                    return null;
                }

                return tokenRecord.Application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating download token");
                return null;
            }
        }

        /// <summary>
        /// Get certificate PDF file
        /// </summary>
        /// <param name="token">Download token</param>
        /// <param name="ipAddress">IP address of requester</param>
        /// <param name="userAgent">User agent string</param>
        /// <returns>File bytes and filename if successful</returns>
        public async Task<(byte[]? FileBytes, string? FileName, string ErrorMessage)> GetCertificateAsync(
            string token,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var application = await ValidateDownloadTokenAsync(token);

            if (application == null)
            {
                return (null, null, "Invalid or expired download token.");
            }

            try
            {
                // Certificate should be in wwwroot/certificates/{ApplicationNumber}_certificate.pdf
                string fileName = $"{application.ApplicationNumber}_certificate.pdf";
                string filePath = Path.Combine(_env.WebRootPath, "certificates", fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogError("Certificate file not found for application {ApplicationNumber}: {FilePath}",
                        application.ApplicationNumber, filePath);
                    await LogDownloadAsync(application.Id, token, "Certificate", false, ipAddress, userAgent,
                        "Certificate file not found on server");
                    return (null, null, "Certificate file not found. Please contact support.");
                }

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                // Log successful download
                await LogDownloadAsync(application.Id, token, "Certificate", true, ipAddress, userAgent);

                _logger.LogInformation("Certificate downloaded for application {ApplicationNumber}",
                    application.ApplicationNumber);

                return (fileBytes, fileName, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for application {ApplicationId}", application.Id);
                await LogDownloadAsync(application.Id, token, "Certificate", false, ipAddress, userAgent, ex.Message);
                return (null, null, "Error downloading certificate. Please try again.");
            }
        }

        /// <summary>
        /// Get recommendation form PDF file
        /// </summary>
        public async Task<(byte[]? FileBytes, string? FileName, string ErrorMessage)> GetRecommendationFormAsync(
            string token,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var application = await ValidateDownloadTokenAsync(token);

            if (application == null)
            {
                return (null, null, "Invalid or expired download token.");
            }

            try
            {
                // Recommendation form should be in wwwroot/recommendation-forms/{ApplicationNumber}_recommendation.pdf
                string fileName = $"{application.ApplicationNumber}_recommendation.pdf";
                string filePath = Path.Combine(_env.WebRootPath, "recommendation-forms", fileName);

                if (!File.Exists(filePath))
                {
                    _logger.LogError("Recommendation form not found for application {ApplicationNumber}: {FilePath}",
                        application.ApplicationNumber, filePath);
                    await LogDownloadAsync(application.Id, token, "RecommendationForm", false, ipAddress, userAgent,
                        "Recommendation form not found on server");
                    return (null, null, "Recommendation form not available.");
                }

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                await LogDownloadAsync(application.Id, token, "RecommendationForm", true, ipAddress, userAgent);

                _logger.LogInformation("Recommendation form downloaded for application {ApplicationNumber}",
                    application.ApplicationNumber);

                return (fileBytes, fileName, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading recommendation form for application {ApplicationId}", application.Id);
                await LogDownloadAsync(application.Id, token, "RecommendationForm", false, ipAddress, userAgent, ex.Message);
                return (null, null, "Error downloading recommendation form. Please try again.");
            }
        }

        /// <summary>
        /// Get payment challan PDF file from database (no physical file access)
        /// </summary>
        public async Task<(byte[]? FileBytes, string? FileName, string ErrorMessage)> GetChallanAsync(
            string token,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var application = await ValidateDownloadTokenAsync(token);

            if (application == null)
            {
                return (null, null, "Invalid or expired download token.");
            }

            try
            {
                // Retrieve payment challan from database (SEDocuments table)
                var document = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == application.Id 
                                           && d.DocumentType == SEDocumentType.PaymentChallan);

                if (document?.FileContent == null)
                {
                    _logger.LogError("Payment challan not found in database for application {ApplicationNumber}",
                        application.ApplicationNumber);
                    await LogDownloadAsync(application.Id, token, "Challan", false, ipAddress, userAgent,
                        "Payment challan not found in database");
                    return (null, null, "Payment challan not available.");
                }

                string fileName = $"{application.ApplicationNumber}_challan.pdf";

                await LogDownloadAsync(application.Id, token, "Challan", true, ipAddress, userAgent);

                _logger.LogInformation("Payment challan retrieved from database for application {ApplicationNumber}",
                    application.ApplicationNumber);

                return (document.FileContent, fileName, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment challan from database for application {ApplicationId}", application.Id);
                await LogDownloadAsync(application.Id, token, "Challan", false, ipAddress, userAgent, ex.Message);
                return (null, null, "Error retrieving payment challan. Please try again.");
            }
        }

        /// <summary>
        /// Log download attempt to audit trail
        /// </summary>
        private async Task LogDownloadAsync(
            int applicationId,
            string token,
            string documentType,
            bool success,
            string? ipAddress = null,
            string? userAgent = null,
            string? errorMessage = null)
        {
            try
            {
                var auditLog = new DownloadAuditLog
                {
                    ApplicationId = applicationId,
                    Token = token,
                    DocumentType = documentType,
                    DownloadedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                _context.DownloadAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging download for application {ApplicationId}", applicationId);
                // Don't throw - logging failure shouldn't stop the download
            }
        }

        /// <summary>
        /// Generate cryptographically secure 6-digit OTP
        /// </summary>
        private string GenerateOtp()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int value = Math.Abs(BitConverter.ToInt32(randomBytes, 0));
                return (value % 1000000).ToString("D6");
            }
        }

        /// <summary>
        /// Generate cryptographically secure download token (GUID)
        /// </summary>
        private string GenerateSecureToken()
        {
            return Guid.NewGuid().ToString("N"); // 32 character hexadecimal string
        }

        /// <summary>
        /// Send OTP via email
        /// </summary>
        private async Task<bool> SendOtpEmailAsync(string email, string name, string applicationNumber, string otp)
        {
            string subject = $"OTP for Certificate Download - Application {applicationNumber}";

            var otpExpiryMinutes = int.Parse(_configuration["AppSettings:DocumentOtpExpiryMinutes"] ?? "30");
            
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #0066cc; text-align: center;'>Pune Municipal Corporation</h2>
                        <h3 style='color: #444;'>Certificate Download OTP</h3>
                        <p>Dear {name},</p>
                        <p>You have requested to download documents for application <strong>{applicationNumber}</strong>.</p>
                        <div style='background-color: #f0f8ff; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0;'>
                            <p style='margin: 0; font-size: 14px; color: #666;'>Your One-Time Password (OTP) is:</p>
                            <p style='font-size: 32px; font-weight: bold; color: #0066cc; margin: 10px 0; letter-spacing: 5px;'>{otp}</p>
                            <p style='margin: 0; font-size: 12px; color: #999;'>This OTP is valid for {otpExpiryMinutes} minutes</p>
                        </div>
                        <p style='color: #d9534f;'><strong>Important:</strong> Do not share this OTP with anyone. PMC officials will never ask you for your OTP.</p>
                        <p>If you did not request this OTP, please ignore this email or contact our support team immediately.</p>
                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666; text-align: center;'>
                            © 2024 Pune Municipal Corporation. All rights reserved.<br>
                            For assistance, contact: support@punecorporation.org
                        </p>
                    </div>
                </body>
                </html>";

            return await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
