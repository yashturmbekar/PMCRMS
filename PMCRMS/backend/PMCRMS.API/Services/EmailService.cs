using System.Net;
using System.Net.Mail;

namespace PMCRMS.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string purpose);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _useSsl;
        private readonly bool _requiresAuth;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load email settings from configuration
            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _username = _configuration["EmailSettings:Username"] ?? "";
            _password = _configuration["EmailSettings:Password"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@pmcrms.gov.in";
            _fromName = _configuration["EmailSettings:FromName"] ?? "PMCRMS";
            _useSsl = bool.Parse(_configuration["EmailSettings:UseSsl"] ?? "true");
            _requiresAuth = bool.Parse(_configuration["EmailSettings:RequiresAuthentication"] ?? "true");
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

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {Email}", toEmail);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _useSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (_requiresAuth)
                {
                    smtpClient.Credentials = new NetworkCredential(_username, _password);
                }

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {Email}: {Message}", toEmail, smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
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
        .logo {{
            width: 80px;
            height: 80px;
            background-color: white;
            border-radius: 50%;
            display: inline-block;
            padding: 10px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);
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
                <div class='logo'>
                    <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' width='60' height='60'>
                        <circle cx='50' cy='50' r='45' fill='#0c4a6e'/>
                        <text x='50' y='60' font-family='Arial, sans-serif' font-size='36' font-weight='bold' fill='white' text-anchor='middle'>PMC</text>
                    </svg>
                </div>
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
    }
}
