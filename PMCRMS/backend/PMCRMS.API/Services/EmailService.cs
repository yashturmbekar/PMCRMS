using System.Net;
using System.Net.Mail;

namespace PMCRMS.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        Task<bool> SendApplicationSubmissionEmailAsync(string toEmail, string applicantName, string applicationNumber, string applicationType, string applicationId, string viewUrl);
        Task<bool> SendApplicationStatusUpdateEmailAsync(string toEmail, string applicantName, string applicationNumber, string status, string assignedTo, string assignedRole, string remarks, string viewUrl);
        Task<bool> SendApplicationApprovalEmailAsync(string toEmail, string applicantName, string applicationNumber, string approvedBy, string approvedRole, string remarks, string viewUrl);
        Task<bool> SendApplicationRejectionEmailAsync(string toEmail, string applicantName, string applicationNumber, string rejectedBy, string rejectedRole, string remarks, string viewUrl);
        Task<bool> SendAssignmentNotificationEmailAsync(string toEmail, string officerName, string applicationNumber, string applicationType, string applicantName, string assignedBy, string viewUrl);
        Task<bool> SendOfficerInvitationEmailAsync(string toEmail, string officerName, string role, string employeeId, string temporaryPassword, string loginUrl);
        Task<bool> SendClerkApprovalEmailAsync(string toEmail, string applicantName, string applicationNumber, string remarks, string viewUrl);
        Task<bool> SendClerkRejectionEmailAsync(string toEmail, string applicantName, string applicationNumber, string rejectionReason, string viewUrl);
        
        // Post-payment workflow email notifications
        Task<bool> SendEE2SignatureRequestEmailAsync(string toEmail, string eeOfficerName, string applicationNumber, string applicantName, string viewUrl);
        Task<bool> SendEE2SignatureCompletedEmailAsync(string toEmail, string applicantName, string applicationNumber, string eeOfficerName, string viewUrl);
        Task<bool> SendCE2SignaturePendingEmailAsync(string toEmail, string ceOfficerName, string applicationNumber, string applicantName, string viewUrl);
        Task<bool> SendCertificateReadyEmailAsync(string toEmail, string applicantName, string applicationNumber, string certificateNumber, string downloadLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _useSsl;
        private readonly bool _requiresAuth;
        private readonly string? _brevoApiKey;
        private readonly bool _useBrevoApi;
        private readonly string _baseUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // Check for Brevo API key first (preferred method)
            _brevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY")
                ?? _configuration["EmailSettings:BrevoApiKey"];
            
            _useBrevoApi = !string.IsNullOrEmpty(_brevoApiKey);

            // Log environment variable availability
            var envSmtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
            var envSmtpPort = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
            var envUsername = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
            var envPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
            var envFrom = Environment.GetEnvironmentVariable("EMAIL_FROM");
            var envFromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME");
            var envUseSsl = Environment.GetEnvironmentVariable("EMAIL_USE_SSL");
            var envRequiresAuth = Environment.GetEnvironmentVariable("EMAIL_REQUIRES_AUTH");

            _logger.LogInformation("=== EMAIL CONFIGURATION DEBUG ===");
            _logger.LogInformation("ENV BREVO_API_KEY: {Value}", _brevoApiKey != null ? "***SET***" : "NOT SET");
            _logger.LogInformation("ENV EMAIL_SMTP_HOST: {Value}", envSmtpHost ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_SMTP_PORT: {Value}", envSmtpPort ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_USERNAME: {Value}", envUsername ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_PASSWORD: {Value}", string.IsNullOrEmpty(envPassword) ? "NOT SET" : "***SET***");
            _logger.LogInformation("ENV EMAIL_FROM: {Value}", envFrom ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_FROM_NAME: {Value}", envFromName ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_USE_SSL: {Value}", envUseSsl ?? "NOT SET");
            _logger.LogInformation("ENV EMAIL_REQUIRES_AUTH: {Value}", envRequiresAuth ?? "NOT SET");

            // Load email settings from environment variables first, then fallback to configuration
            _smtpHost = envSmtpHost 
                ?? _configuration["EmailSettings:SmtpHost"] 
                ?? "smtp.gmail.com";
            
            _smtpPort = int.Parse(envSmtpPort 
                ?? _configuration["EmailSettings:SmtpPort"] 
                ?? "587");
            
            _username = envUsername 
                ?? _configuration["EmailSettings:Username"] 
                ?? "";
            
            _password = envPassword 
                ?? _configuration["EmailSettings:Password"] 
                ?? "";
            
            _fromEmail = envFrom 
                ?? _configuration["EmailSettings:FromEmail"] 
                ?? "noreply@pmcrms.gov.in";
            
            _fromName = envFromName 
                ?? _configuration["EmailSettings:FromName"] 
                ?? "PMCRMS";
            
            _useSsl = bool.Parse(envUseSsl 
                ?? _configuration["EmailSettings:UseSsl"] 
                ?? "true");
            
            _requiresAuth = bool.Parse(envRequiresAuth 
                ?? _configuration["EmailSettings:RequiresAuthentication"] 
                ?? "true");

            // Get base URL for email images
            _baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL")
                ?? _configuration["AppSettings:BaseUrl"]
                ?? "http://localhost:5086";

            _logger.LogInformation("=== FINAL EMAIL CONFIGURATION ===");
            _logger.LogInformation("Primary Method: {Method}", _useBrevoApi ? "Brevo API" : "SMTP");
            _logger.LogInformation("Brevo API Key: {HasKey}", _brevoApiKey != null ? "SET" : "NOT SET");
            _logger.LogInformation("SMTP Host: {Host}", _smtpHost);
            _logger.LogInformation("SMTP Port: {Port}", _smtpPort);
            _logger.LogInformation("Username: {Username}", _username);
            _logger.LogInformation("Password: {Password}", string.IsNullOrEmpty(_password) ? "EMPTY" : $"SET (length: {_password.Length})");
            _logger.LogInformation("From Email: {FromEmail}", _fromEmail);
            _logger.LogInformation("From Name: {FromName}", _fromName);
            _logger.LogInformation("Use SSL: {UseSsl}", _useSsl);
            _logger.LogInformation("Requires Auth: {RequiresAuth}", _requiresAuth);
            _logger.LogInformation("=================================");
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
        {
            try
            {
                var subject = "Your PMCRMS Login OTP";
                var body = GenerateOtpEmailBody(otpCode, purpose);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendApplicationSubmissionEmailAsync(
            string toEmail, 
            string applicantName, 
            string applicationNumber, 
            string applicationType, 
            string applicationId, 
            string viewUrl)
        {
            try
            {
                var subject = $"Application Submitted Successfully - {applicationNumber}";
                var body = GenerateApplicationSubmissionEmailBody(applicantName, applicationNumber, applicationType, applicationId, viewUrl);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application submission email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendApplicationStatusUpdateEmailAsync(
            string toEmail,
            string applicantName,
            string applicationNumber,
            string status,
            string assignedTo,
            string assignedRole,
            string remarks,
            string viewUrl)
        {
            try
            {
                var subject = $"Application Status Updated - {applicationNumber}";
                var body = GenerateStatusUpdateEmailBody(applicantName, applicationNumber, status, assignedTo, assignedRole, remarks, viewUrl);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status update email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendApplicationApprovalEmailAsync(
            string toEmail,
            string applicantName,
            string applicationNumber,
            string approvedBy,
            string approvedRole,
            string remarks,
            string viewUrl)
        {
            try
            {
                var subject = $"Application Approved - {applicationNumber}";
                var body = GenerateApprovalEmailBody(applicantName, applicationNumber, approvedBy, approvedRole, remarks, viewUrl);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendApplicationRejectionEmailAsync(
            string toEmail,
            string applicantName,
            string applicationNumber,
            string rejectedBy,
            string rejectedRole,
            string remarks,
            string viewUrl)
        {
            try
            {
                var subject = $"Application Requires Attention - {applicationNumber}";
                var body = GenerateRejectionEmailBody(applicantName, applicationNumber, rejectedBy, rejectedRole, remarks, viewUrl);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending rejection email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendAssignmentNotificationEmailAsync(
            string toEmail,
            string officerName,
            string applicationNumber,
            string applicationType,
            string applicantName,
            string assignedBy,
            string viewUrl)
        {
            try
            {
                var subject = $"New Application Assigned - {applicationNumber}";
                var body = GenerateAssignmentEmailBody(officerName, applicationNumber, applicationType, applicantName, assignedBy, viewUrl);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending assignment notification email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendOfficerInvitationEmailAsync(
            string toEmail,
            string officerName,
            string role,
            string employeeId,
            string temporaryPassword,
            string loginUrl)
        {
            try
            {
                var subject = "Invitation to Join PMCRMS - Officer Account Created";
                var body = GenerateOfficerInvitationEmailBody(officerName, role, employeeId, temporaryPassword, loginUrl);
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending officer invitation email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            // Try Brevo API first if configured
            if (_useBrevoApi)
            {
                _logger.LogInformation("Attempting to send email via Brevo API...");
                var apiResult = await SendViaBrevoApiAsync(toEmail, subject, body);
                
                if (apiResult)
                {
                    _logger.LogInformation("✓ Email sent successfully via Brevo API");
                    return true;
                }
                
                _logger.LogWarning("Brevo API failed, falling back to SMTP...");
            }

            // Fallback to SMTP
            return await SendViaSmtpAsync(toEmail, subject, body);
        }

        private async Task<bool> SendViaBrevoApiAsync(string toEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation("=== SENDING EMAIL VIA BREVO API ===");
                _logger.LogInformation("To: {Email}", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("From: {FromEmail} ({FromName})", _fromEmail, _fromName);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri("https://api.brevo.com/v3/");
                httpClient.DefaultRequestHeaders.Add("api-key", _brevoApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    sender = new { name = _fromName, email = _fromEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = body
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Making API request to Brevo...");

                var response = await httpClient.PostAsync("smtp/email", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✓ Brevo API: Email sent successfully. Response: {Response}", responseBody);
                    return true;
                }
                else
                {
                    _logger.LogError("✗ Brevo API: Failed to send email. Status: {Status}, Response: {Response}", 
                        response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Brevo API: Error sending email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendViaSmtpAsync(string toEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation("=== SENDING EMAIL VIA SMTP ===");
                _logger.LogInformation("To: {Email}", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Using SMTP: {Host}:{Port}", _smtpHost, _smtpPort);
                _logger.LogInformation("From: {FromEmail} ({FromName})", _fromEmail, _fromName);
                _logger.LogInformation("SSL: {SSL}, Auth: {Auth}", _useSsl, _requiresAuth);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                _logger.LogInformation("Mail message created. Creating SMTP client...");

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _useSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 15000 // 15 seconds timeout instead of default 100 seconds
                };

                if (_requiresAuth)
                {
                    _logger.LogInformation("Setting credentials for authentication (Username: {Username})", _username);
                    smtpClient.Credentials = new NetworkCredential(_username, _password);
                }

                _logger.LogInformation("Attempting to connect and send email...");

                // Use Task.Run with timeout to avoid hanging indefinitely
                var sendTask = smtpClient.SendMailAsync(mailMessage);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
                var completedTask = await Task.WhenAny(sendTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogError("Email send operation timed out after 15 seconds for {Email}", toEmail);
                    _logger.LogError("Timeout details - Host: {Host}, Port: {Port}, SSL: {SSL}", _smtpHost, _smtpPort, _useSsl);
                    return false;
                }

                await sendTask; // This will throw if there was an error
                
                _logger.LogInformation("✓ Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "✗ SMTP error sending email to {Email}", toEmail);
                _logger.LogError("SMTP Error Code: {StatusCode}", smtpEx.StatusCode);
                _logger.LogError("SMTP Config - Host: {Host}, Port: {Port}, SSL: {SSL}, Auth: {Auth}", 
                    _smtpHost, _smtpPort, _useSsl, _requiresAuth);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ General error sending email to {Email}", toEmail);
                _logger.LogError("Error type: {ErrorType}", ex.GetType().Name);
                return false;
            }
        }

        private string GenerateOtpEmailBody(string otpCode, string purpose)
        {
            var actionText = purpose == "LOGIN" ? "login" : "registration";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .logo-container {{
            margin-bottom: 15px;
        }}
        .badge {{
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }}
        .header h1 {{
            margin: 10px 0 5px 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }}
        .otp-box {{
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
            border-radius: 8px;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: bold;
            color: #0c4a6e;
            letter-spacing: 10px;
            font-family: 'Courier New', monospace;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .warning {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{_baseUrl}/pmc-logo.png' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        <div class='content'>
            <h2>Your OTP Code</h2>
            <p>Dear User,</p>
            <p>You have requested to {actionText} to PMCRMS. Please use the following One-Time Password (OTP) to complete the process:</p>
            
            <div class='otp-box'>
                <div class='otp-code'>{otpCode}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px; color: #6b7280;'>Valid for 5 minutes</p>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>This OTP is valid for 5 minutes only</li>
                    <li>Do not share this OTP with anyone</li>
                    <li>PMC officials will never ask for your OTP</li>
                </ul>
            </div>
            
            <p>If you did not request this OTP, please ignore this email or contact our support team immediately.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
        }

        private string GenerateApplicationSubmissionEmailBody(
            string applicantName, 
            string applicationNumber, 
            string applicationType, 
            string applicationId, 
            string viewUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .logo-container {{
            margin-bottom: 15px;
        }}
        .badge {{
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }}
        .success-badge {{
            background-color: #10b981;
            color: white;
            display: inline-block;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
            margin: 15px 0;
        }}
        .header h1 {{
            margin: 10px 0 5px 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }}
        .info-box {{
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
        }}
        .info-row {{
            display: flex;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: bold;
            color: #0c4a6e;
            min-width: 180px;
        }}
        .info-value {{
            color: #333;
        }}
        .btn-primary {{
            display: inline-block;
            background-color: #0c4a6e;
            color: white;
            padding: 14px 28px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            margin: 20px 0;
            text-align: center;
        }}
        .btn-primary:hover {{
            background-color: #1e40af;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .info-notice {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin: 15px 0;
        }}
        .checkmark {{
            font-size: 48px;
            color: #10b981;
            text-align: center;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{_baseUrl}/pmc-logo.png' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        <div class='content'>
            <div class='checkmark'>✓</div>
            <div class='success-badge'>Application Submitted Successfully</div>
            
            <h2>Dear {applicantName},</h2>
            <p>Thank you for submitting your application to PMCRMS. Your application has been received and is now being processed.</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{applicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Application Type:</div>
                    <div class='info-value'>{applicationType}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Submission Date:</div>
                    <div class='info-value'>{DateTime.UtcNow:MMMM dd, yyyy}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Current Status:</div>
                    <div class='info-value'><span style='color: #f59e0b; font-weight: bold;'>Under Review</span></div>
                </div>
            </div>
            
            <div style='text-align: center;'>
                <a href='{viewUrl}' class='btn-primary'>View Application Details</a>
            </div>
            
            <div class='info-notice'>
                <strong>📋 Next Steps:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Your application will be reviewed by our team</li>
                    <li>You will receive updates via email at each stage</li>
                    <li>You can track your application status anytime using the link above</li>
                    <li>Please keep your application number for future reference</li>
                </ul>
            </div>
            
            <p><strong>Important:</strong> Please ensure all required documents are uploaded. If any documents are missing, you may be contacted by our team.</p>
            
            <p>If you have any questions or need assistance, please contact our support team.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>For support, please visit our website or contact us at support@pmcrms.gov.in</p>
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
        }

        private string GenerateStatusUpdateEmailBody(
            string applicantName,
            string applicationNumber,
            string status,
            string assignedTo,
            string assignedRole,
            string remarks,
            string viewUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        {GetCommonEmailStyles()}
    </style>
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        <div class='content'>
            <div class='status-icon' style='text-align: center; font-size: 48px; margin: 10px 0;'>🔄</div>
            <div class='status-badge' style='background-color: #3b82f6; color: white; display: inline-block; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: bold; margin: 15px 0;'>Status Updated</div>
            
            <h2>Dear {applicantName},</h2>
            <p>Your application status has been updated. Please review the details below:</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{applicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Current Status:</div>
                    <div class='info-value'><span style='color: #3b82f6; font-weight: bold;'>{status}</span></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Assigned To:</div>
                    <div class='info-value'>{assignedTo} ({assignedRole})</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Updated On:</div>
                    <div class='info-value'>{DateTime.UtcNow:MMMM dd, yyyy h:mm tt} UTC</div>
                </div>
                {(!string.IsNullOrEmpty(remarks) ? $@"
                <div class='info-row'>
                    <div class='info-label'>Remarks:</div>
                    <div class='info-value'>{remarks}</div>
                </div>" : "")}
            </div>
            
            <div style='text-align: center;'>
                <a href='{viewUrl}' class='btn-primary'>View Application Details</a>
            </div>
            
            <p>If you have any questions, please contact our support team.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateApprovalEmailBody(
            string applicantName,
            string applicationNumber,
            string approvedBy,
            string approvedRole,
            string remarks,
            string viewUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        {GetCommonEmailStyles()}
    </style>
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        <div class='content'>
            <div class='checkmark' style='font-size: 64px; color: #10b981; text-align: center; margin: 10px 0;'>✓</div>
            <div class='success-badge' style='background-color: #10b981; color: white; display: inline-block; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: bold; margin: 15px 0;'>Application Approved</div>
            
            <h2>Congratulations {applicantName}!</h2>
            <p>We are pleased to inform you that your application has been <strong>approved</strong>.</p>
            
            <div class='info-box' style='background-color: #f0fdf4; border-color: #10b981;'>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{applicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Status:</div>
                    <div class='info-value'><span style='color: #10b981; font-weight: bold;'>✓ APPROVED</span></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Approved By:</div>
                    <div class='info-value'>{approvedBy} ({approvedRole})</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Approval Date:</div>
                    <div class='info-value'>{DateTime.UtcNow:MMMM dd, yyyy h:mm tt} UTC</div>
                </div>
                {(!string.IsNullOrEmpty(remarks) ? $@"
                <div class='info-row'>
                    <div class='info-label'>Remarks:</div>
                    <div class='info-value'>{remarks}</div>
                </div>" : "")}
            </div>
            
            <div style='text-align: center;'>
                <a href='{viewUrl}' class='btn-primary' style='background-color: #10b981;'>View Approval Details</a>
            </div>
            
            <div class='info-notice'>
                <strong>📋 Next Steps:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Download your approval certificate from the application portal</li>
                    <li>Keep the application number for future reference</li>
                    <li>Follow any additional instructions provided in the approval</li>
                </ul>
            </div>
            
            <p>Thank you for using PMCRMS.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateRejectionEmailBody(
            string applicantName,
            string applicationNumber,
            string rejectedBy,
            string rejectedRole,
            string remarks,
            string viewUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        {GetCommonEmailStyles()}
    </style>
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        <div class='content'>
            <div class='status-icon' style='text-align: center; font-size: 48px; margin: 10px 0;'>⚠️</div>
            <div class='warning-badge' style='background-color: #ef4444; color: white; display: inline-block; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: bold; margin: 15px 0;'>Action Required</div>
            
            <h2>Dear {applicantName},</h2>
            <p>Your application requires attention and modifications. Please review the feedback provided below:</p>
            
            <div class='info-box' style='background-color: #fef2f2; border-color: #ef4444;'>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{applicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Status:</div>
                    <div class='info-value'><span style='color: #ef4444; font-weight: bold;'>⚠ Requires Revision</span></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Reviewed By:</div>
                    <div class='info-value'>{rejectedBy} ({rejectedRole})</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Review Date:</div>
                    <div class='info-value'>{DateTime.UtcNow:MMMM dd, yyyy h:mm tt} UTC</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Feedback/Remarks:</div>
                    <div class='info-value'><strong>{remarks}</strong></div>
                </div>
            </div>
            
            <div style='text-align: center;'>
                <a href='{viewUrl}' class='btn-primary' style='background-color: #ef4444;'>View Full Details & Resubmit</a>
            </div>
            
            <div class='warning' style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px; margin: 15px 0;'>
                <strong>📋 What to do next:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Review the feedback/remarks carefully</li>
                    <li>Make the necessary corrections to your application</li>
                    <li>Upload any additional documents if required</li>
                    <li>Resubmit your application for review</li>
                </ul>
            </div>
            
            <p>If you have any questions or need clarification, please contact our support team.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateAssignmentEmailBody(
            string officerName,
            string applicationNumber,
            string applicationType,
            string applicantName,
            string assignedBy,
            string viewUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        {GetCommonEmailStyles()}
    </style>
</head>
<body>
    <div class='container'>
        {GetEmailHeader()}
        <div class='content'>
            <div class='status-icon' style='text-align: center; font-size: 48px; margin: 10px 0;'>📋</div>
            <div class='assignment-badge' style='background-color: #8b5cf6; color: white; display: inline-block; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: bold; margin: 15px 0;'>New Assignment</div>
            
            <h2>Dear {officerName},</h2>
            <p>A new application has been assigned to you for review and processing.</p>
            
            <div class='info-box' style='background-color: #f5f3ff; border-color: #8b5cf6;'>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{applicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Application Type:</div>
                    <div class='info-value'>{applicationType}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Applicant Name:</div>
                    <div class='info-value'>{applicantName}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Assigned By:</div>
                    <div class='info-value'>{assignedBy}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Assigned On:</div>
                    <div class='info-value'>{DateTime.UtcNow:MMMM dd, yyyy h:mm tt} UTC</div>
                </div>
            </div>
            
            <div style='text-align: center;'>
                <a href='{viewUrl}' class='btn-primary' style='background-color: #8b5cf6;'>Review Application</a>
            </div>
            
            <div class='info-notice' style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px; margin: 15px 0;'>
                <strong>📋 Your Action Items:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Review all submitted documents and information</li>
                    <li>Verify compliance with regulations</li>
                    <li>Provide feedback or approve the application</li>
                    <li>Update the status accordingly</li>
                </ul>
            </div>
            
            <p>Please review the application at your earliest convenience.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS System</strong><br>
            Pune Municipal Corporation</p>
        </div>
        {GetEmailFooter()}
    </div>
</body>
</html>";
        }

        private string GenerateOfficerInvitationEmailBody(
            string officerName,
            string role,
            string employeeId,
            string temporaryPassword,
            string loginUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .logo-container {{
            margin-bottom: 15px;
        }}
        .badge {{
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }}
        .header h1 {{
            margin: 10px 0 5px 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }}
        .password-box {{
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            text-align: center;
            margin: 20px 0;
            border-radius: 8px;
        }}
        .password-code {{
            font-size: 32px;
            font-weight: bold;
            color: #0c4a6e;
            letter-spacing: 4px;
            font-family: 'Courier New', monospace;
            background-color: white;
            padding: 15px;
            border-radius: 6px;
            display: inline-block;
            margin: 10px 0;
        }}
        .info-row {{
            background-color: #f9fafb;
            padding: 12px;
            margin: 8px 0;
            border-radius: 6px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .info-label {{
            font-weight: 600;
            color: #6b7280;
        }}
        .info-value {{
            color: #0c4a6e;
            font-weight: bold;
        }}
        .btn-primary {{
            display: inline-block;
            background-color: #10b981;
            color: white;
            padding: 14px 32px;
            text-decoration: none;
            border-radius: 8px;
            font-weight: bold;
            margin: 20px 0;
            font-size: 16px;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .warning {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{_baseUrl}/pmc-logo.png' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        <div class='content'>
            <div style='text-align: center; font-size: 48px; margin: 10px 0;'>🎉</div>
            <h2 style='text-align: center; color: #10b981;'>Welcome to PMCRMS!</h2>
            
            <p>Dear <strong>{officerName}</strong>,</p>
            <p>Your officer account has been successfully created in the PMCRMS system. Below are your login credentials and account details:</p>
            
            <div class='info-row'>
                <span class='info-label'>Employee ID:</span>
                <span class='info-value'>{employeeId}</span>
            </div>
            
            <div class='info-row'>
                <span class='info-label'>Role:</span>
                <span class='info-value'>{role}</span>
            </div>
            
            <div class='password-box'>
                <p style='margin: 0 0 10px 0; font-size: 16px; font-weight: 600; color: #0c4a6e;'>Your Temporary Password</p>
                <div class='password-code'>{temporaryPassword}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px; color: #6b7280;'>Valid for 7 days</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='{loginUrl}' class='btn-primary'>Login to PMCRMS</a>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Important Security Instructions:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li><strong>Change your password immediately</strong> after first login</li>
                    <li>Use your <strong>Employee ID</strong> and the temporary password above to login</li>
                    <li>This password expires in 7 days</li>
                    <li>Never share your credentials with anyone</li>
                    <li>Contact IT support if you need assistance</li>
                </ul>
            </div>
            
            <p style='background-color: #f0f9ff; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                <strong>📋 Next Steps:</strong><br>
                1. Click the 'Login to PMCRMS' button above<br>
                2. Enter your Employee ID and temporary password<br>
                3. Set a new secure password<br>
                4. Complete your profile setup<br>
                5. Start managing applications
            </p>
            
            <p>If you have any questions, please contact the system administrator.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetCommonEmailStyles()
        {
            return @"
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }
        .header {
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }
        .logo-container {
            margin-bottom: 15px;
        }
        .badge {
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }
        .header h1 {
            margin: 10px 0 5px 0;
            font-size: 24px;
        }
        .header p {
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }
        .content {
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }
        .info-box {
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
        }
        .info-row {
            display: flex;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }
        .info-row:last-child {
            border-bottom: none;
        }
        .info-label {
            font-weight: bold;
            color: #0c4a6e;
            min-width: 180px;
        }
        .info-value {
            color: #333;
        }
        .btn-primary {
            display: inline-block;
            background-color: #0c4a6e;
            color: white;
            padding: 14px 28px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            margin: 20px 0;
            text-align: center;
        }
        .footer {
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }
        .info-notice {
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin: 15px 0;
        }
        .warning {
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin: 15px 0;
        }";
        }

        private string GetEmailHeader()
        {
            return $@"
        <div class='header'>
            <div class='logo-container'>
                <img src='{_baseUrl}/pmc-logo.png' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>";
        }

        private string GetEmailFooter()
        {
            return @"
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>For support, please visit our website or contact us at support@pmcrms.gov.in</p>
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
        </div>";
        }

        /// <summary>
        /// Send email notification when clerk approves application (forwards to EE Stage 2)
        /// </summary>
        public async Task<bool> SendClerkApprovalEmailAsync(
            string toEmail, 
            string applicantName, 
            string applicationNumber, 
            string remarks, 
            string viewUrl)
        {
            try
            {
                var subject = $"Application {applicationNumber} - Processed by Clerk";
                
                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {applicantName},</p>
                        
                        <p>Your application <strong>{applicationNumber}</strong> has been processed by the clerk and forwarded to the Executive Engineer for digital signature.</p>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Status:</strong> Forwarded to Executive Engineer (Stage 2)</p>
                            <p><strong>Processed Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                            {(!string.IsNullOrEmpty(remarks) ? $"<p><strong>Clerk Remarks:</strong> {remarks}</p>" : "")}
                        </div>
                        
                        <div class='info-notice'>
                            <p><strong>Next Steps:</strong></p>
                            <ul>
                                <li>Your application is now being reviewed by the Executive Engineer</li>
                                <li>Digital signature will be applied to your certificate</li>
                                <li>You will be notified once this step is completed</li>
                            </ul>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{viewUrl}' class='button' style='background-color: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
                                View Application Status
                            </a>
                        </p>
                        
                        <p>Thank you for your patience.</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending clerk approval email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }

        /// <summary>
        /// Send email notification when clerk rejects application
        /// </summary>
        public async Task<bool> SendClerkRejectionEmailAsync(
            string toEmail, 
            string applicantName, 
            string applicationNumber, 
            string rejectionReason, 
            string viewUrl)
        {
            try
            {
                var subject = $"Application {applicationNumber} - Rejected by Clerk";
                
                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {applicantName},</p>
                        
                        <p>We regret to inform you that your application <strong>{applicationNumber}</strong> has been rejected during clerk review.</p>
                        
                        <div class='warning'>
                            <p><strong>Rejection Reason:</strong></p>
                            <p>{rejectionReason}</p>
                        </div>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Status:</strong> Rejected by Clerk</p>
                            <p><strong>Rejection Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                        </div>
                        
                        <div class='info-notice'>
                            <p><strong>What You Can Do:</strong></p>
                            <ul>
                                <li>Review the rejection reason carefully</li>
                                <li>Address the issues mentioned in the rejection reason</li>
                                <li>Submit a new application with corrected information</li>
                                <li>Contact our office for clarification if needed</li>
                            </ul>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{viewUrl}' class='button' style='background-color: #ef4444; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
                                View Application Details
                            </a>
                        </p>
                        
                        <p>If you have any questions, please contact our support team.</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending clerk rejection email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }

        /// <summary>
        /// Send notification to Executive Engineer that digital signature is required (after clerk processing)
        /// </summary>
        public async Task<bool> SendEE2SignatureRequestEmailAsync(
            string toEmail, 
            string eeOfficerName, 
            string applicationNumber, 
            string applicantName, 
            string viewUrl)
        {
            try
            {
                string subject = $"Action Required: Digital Signature - Application {applicationNumber}";

                string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {eeOfficerName},</p>
                        
                        <p>A building permission certificate is ready for your digital signature.</p>
                        
                        <div class='highlight' style='background-color: #fff7ed; border-left: 4px solid #f97316; padding: 15px; margin: 20px 0; border-radius: 6px;'>
                            <p style='margin: 0; font-weight: bold; color: #ea580c;'>⚠️ Action Required: Digital Signature</p>
                            <p style='margin: 10px 0 0 0; color: #9a3412;'>This certificate requires your HSM-based digital signature before it can proceed to the City Engineer for final approval.</p>
                        </div>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Applicant Name:</strong> {applicantName}</p>
                            <p><strong>Current Status:</strong> Processed by Clerk</p>
                            <p><strong>Assigned To:</strong> {eeOfficerName} (Executive Engineer)</p>
                            <p><strong>Assigned Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                        </div>
                        
                        <div class='info-notice'>
                            <p><strong>Next Steps:</strong></p>
                            <ol>
                                <li>Log in to your dashboard</li>
                                <li>Review the application details and certificate</li>
                                <li>Request OTP for digital signature</li>
                                <li>Enter OTP to apply your digital signature using HSM</li>
                                <li>Certificate will be forwarded to City Engineer for final signature</li>
                            </ol>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{viewUrl}' class='button' style='background-color: #ea580c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
                                View Application & Apply Signature
                            </a>
                        </p>
                        
                        <p>Your prompt action ensures timely certificate issuance to the applicant.</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending EE2 signature request email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }

        /// <summary>
        /// Send notification to applicant that Executive Engineer has completed digital signature
        /// </summary>
        public async Task<bool> SendEE2SignatureCompletedEmailAsync(
            string toEmail, 
            string applicantName, 
            string applicationNumber, 
            string eeOfficerName, 
            string viewUrl)
        {
            try
            {
                string subject = $"Progress Update: Certificate Signed by Executive Engineer - {applicationNumber}";

                string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {applicantName},</p>
                        
                        <p>Good news! Your building permission certificate has been digitally signed by the Executive Engineer.</p>
                        
                        <div class='success' style='background-color: #f0fdf4; border-left: 4px solid #22c55e; padding: 15px; margin: 20px 0; border-radius: 6px;'>
                            <p style='margin: 0; font-weight: bold; color: #16a34a;'>✓ Executive Engineer Digital Signature Completed</p>
                            <p style='margin: 10px 0 0 0; color: #166534;'>Your certificate is now being forwarded to the City Engineer for final approval.</p>
                        </div>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Applicant Name:</strong> {applicantName}</p>
                            <p><strong>Current Status:</strong> Executive Engineer Signature Completed</p>
                            <p><strong>Signed By:</strong> {eeOfficerName} (Executive Engineer)</p>
                            <p><strong>Signature Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                        </div>
                        
                        <div class='info-notice'>
                            <p><strong>What Happens Next:</strong></p>
                            <ol>
                                <li>Certificate forwarded to City Engineer for review</li>
                                <li>City Engineer will apply final digital signature</li>
                                <li>Once complete, your certificate will be ready for download</li>
                                <li>You will receive download instructions via email</li>
                            </ol>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{viewUrl}' class='button' style='background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
                                Track Application Status
                            </a>
                        </p>
                        
                        <p>Thank you for your patience. Your certificate should be ready soon!</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending EE2 signature completed email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }

        /// <summary>
        /// Send notification to City Engineer that final signature is required
        /// </summary>
        public async Task<bool> SendCE2SignaturePendingEmailAsync(
            string toEmail, 
            string ceOfficerName, 
            string applicationNumber, 
            string applicantName, 
            string viewUrl)
        {
            try
            {
                string subject = $"Action Required: Final Certificate Signature - Application {applicationNumber}";

                string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {ceOfficerName},</p>
                        
                        <p>A building permission certificate has been signed by the Executive Engineer and is now ready for your final approval and signature.</p>
                        
                        <div class='highlight' style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 6px;'>
                            <p style='margin: 0; font-weight: bold; color: #d97706;'>⚠️ Action Required: Final Certificate Signature</p>
                            <p style='margin: 10px 0 0 0; color: #92400e;'>Your digital signature will complete the certificate issuance process and make it available for applicant download.</p>
                        </div>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Applicant Name:</strong> {applicantName}</p>
                            <p><strong>Current Status:</strong> Executive Engineer Signature Completed</p>
                            <p><strong>Assigned To:</strong> {ceOfficerName} (City Engineer)</p>
                            <p><strong>Assigned Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                        </div>
                        
                        <div class='info-notice'>
                            <p><strong>Next Steps:</strong></p>
                            <ol>
                                <li>Log in to your dashboard</li>
                                <li>Review the application details and certificate</li>
                                <li>Verify Executive Engineer's signature is present</li>
                                <li>Request OTP for your final digital signature</li>
                                <li>Enter OTP to issue the certificate using HSM</li>
                                <li>Certificate will be immediately available for applicant download</li>
                            </ol>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{viewUrl}' class='button' style='background-color: #d97706; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
                                View Application & Issue Certificate
                            </a>
                        </p>
                        
                        <p><strong>Important:</strong> Once you issue the certificate, it cannot be modified. Please review carefully before signing.</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending CE2 signature pending email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }

        /// <summary>
        /// Send notification to applicant that certificate is ready for download with OTP portal link
        /// </summary>
        public async Task<bool> SendCertificateReadyEmailAsync(
            string toEmail, 
            string applicantName, 
            string applicationNumber, 
            string certificateNumber, 
            string downloadLink)
        {
            try
            {
                string subject = $"🎉 Certificate Issued! Download Your Building Permission Certificate - {applicationNumber}";

                string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>{GetCommonEmailStyles()}</style>
                </head>
                <body>
                    {GetEmailHeader()}
                    
                    <div class='content'>
                        <p>Dear {applicantName},</p>
                        
                        <p>Congratulations! Your building permission certificate has been successfully issued by the Pune Municipal Corporation.</p>
                        
                        <div class='success' style='background-color: #f0fdf4; border-left: 4px solid #22c55e; padding: 20px; margin: 20px 0; border-radius: 6px; text-align: center;'>
                            <p style='margin: 0; font-size: 18px; font-weight: bold; color: #16a34a;'>✓ Certificate Issued Successfully!</p>
                            <p style='margin: 10px 0; font-size: 24px; font-weight: bold; color: #166534;'>{certificateNumber}</p>
                            <p style='margin: 0; color: #166534;'>Your certificate is now ready for download</p>
                        </div>
                        
                        <div class='info-box'>
                            <p><strong>Application Number:</strong> {applicationNumber}</p>
                            <p><strong>Certificate Number:</strong> {certificateNumber}</p>
                            <p><strong>Applicant Name:</strong> {applicantName}</p>
                            <p><strong>Issue Date:</strong> {DateTime.UtcNow:dd MMMM yyyy, hh:mm tt} UTC</p>
                            <p><strong>Digitally Signed By:</strong> Executive Engineer & City Engineer</p>
                        </div>
                        
                        <div class='info-notice' style='background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; border-radius: 6px;'>
                            <p style='margin: 0; font-weight: bold; color: #1e40af;'>📥 How to Download Your Certificate:</p>
                            <ol style='margin: 10px 0 0 0; color: #1e3a8a;'>
                                <li>Click the download button below</li>
                                <li>Enter your application number and registered email</li>
                                <li>You will receive a 6-digit OTP via email</li>
                                <li>Enter the OTP to verify your identity</li>
                                <li>Download your certificate, recommendation form, and payment challan</li>
                            </ol>
                        </div>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{downloadLink}' class='button' style='background-color: #16a34a; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold; font-size: 16px;'>
                                📥 Download Certificate Now
                            </a>
                        </p>
                        
                        <div class='warning' style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; border-radius: 6px;'>
                            <p style='margin: 0; font-weight: bold; color: #dc2626;'>⚠️ Important Notes:</p>
                            <ul style='margin: 10px 0 0 0; color: #991b1b;'>
                                <li>Download link is valid for 48 hours after OTP verification</li>
                                <li>Keep your certificate safe - you will need it for construction</li>
                                <li>Do not share the OTP with anyone</li>
                                <li>For any issues, contact our support team</li>
                            </ul>
                        </div>
                        
                        <p><strong>What's Included in Your Download:</strong></p>
                        <ul>
                            <li>✓ Building Permission Certificate (digitally signed)</li>
                            <li>✓ Recommendation Form</li>
                            <li>✓ Payment Challan</li>
                        </ul>
                        
                        <p>Thank you for using the Pune Municipal Corporation online services. We wish you success with your construction project!</p>
                        <p>Best regards,<br>Pune Municipal Corporation</p>
                    </div>
                    
                    {GetEmailFooter()}
                </body>
                </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Error sending certificate ready email to {Email} for application {ApplicationNumber}", 
                    toEmail, applicationNumber);
                return false;
            }
        }
    }
}
