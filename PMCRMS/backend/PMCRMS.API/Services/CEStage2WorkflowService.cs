using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Workflow service for CE Stage 2 - Final Certificate Signature
    /// Handles City Engineer final digital signature after EE Stage 2
    /// Status flow: 20 (DigitalSignatureCompletedByEE2) → 22 (CertificateIssued)
    /// </summary>
    public class CEStage2WorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<CEStage2WorkflowService> _logger;
        private readonly IEmailService _emailService;
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly IConfiguration _configuration;

        public CEStage2WorkflowService(
            PMCRMSDbContext context,
            ILogger<CEStage2WorkflowService> logger,
            IEmailService emailService,
            IDigitalSignatureService digitalSignatureService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _digitalSignatureService = digitalSignatureService;
            _configuration = configuration;
        }

        /// <summary>
        /// Get pending applications for CE Stage 2 final signature (status 20)
        /// </summary>
        public async Task<List<CEStage2ApplicationDto>> GetPendingApplicationsAsync()
        {
            _logger.LogInformation("[CEStage2Workflow] Fetching pending applications for CE final signature");

            var applications = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Transactions)
                .Where(a => a.CurrentStatus == ApplicationCurrentStatus.DigitalSignatureCompletedByEE2)
                .OrderBy(a => a.UpdatedDate)
                .Select(a => new CEStage2ApplicationDto
                {
                    ApplicationId = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    ApplicantName = a.Applicant.Name,
                    BuildingType = a.Type.ToString(),
                    PlotArea = 0, // Not available in main application model
                    District = "", // Not available in main application model
                    EE2SignedDate = a.UpdatedDate,
                    PaymentAmount = a.Transactions
                        .Where(t => t.Status == "SUCCESS")
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.Price)
                        .FirstOrDefault(),
                    PaymentDate = a.Transactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.CreatedAt)
                        .FirstOrDefault(),
                    PaymentReference = a.Transactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.TransactionId)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();

            _logger.LogInformation($"[CEStage2Workflow] Found {applications.Count} pending applications for CE final signature");
            return applications;
        }

        /// <summary>
        /// Get completed applications by CE Stage 2
        /// </summary>
        public async Task<List<CEStage2ApplicationDto>> GetCompletedApplicationsAsync()
        {
            _logger.LogInformation("[CEStage2Workflow] Fetching completed applications by CE Stage 2");

            var applications = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Transactions)
                .Where(a => a.CurrentStatus == ApplicationCurrentStatus.CertificateIssued || 
                           a.CurrentStatus == ApplicationCurrentStatus.Completed)
                .OrderByDescending(a => a.UpdatedDate)
                .Select(a => new CEStage2ApplicationDto
                {
                    ApplicationId = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    ApplicantName = a.Applicant.Name,
                    BuildingType = a.Type.ToString(),
                    PlotArea = 0,
                    District = "",
                    EE2SignedDate = a.UpdatedDate,
                    PaymentAmount = a.Transactions
                        .Where(t => t.Status == "SUCCESS")
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.Price)
                        .FirstOrDefault(),
                    PaymentDate = a.Transactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.CreatedAt)
                        .FirstOrDefault(),
                    PaymentReference = a.Transactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => t.TransactionId)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();

            _logger.LogInformation($"[CEStage2Workflow] Found {applications.Count} completed applications by CE Stage 2");
            return applications;
        }

        /// <summary>
        /// Get detailed application information for CE review
        /// </summary>
        public async Task<CEStage2ApplicationDetailDto?> GetApplicationDetailsAsync(int applicationId)
        {
            _logger.LogInformation($"[CEStage2Workflow] Fetching application details for ID: {applicationId}");

            var application = await _context.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Transactions)
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                _logger.LogWarning($"[CEStage2Workflow] Application not found: {applicationId}");
                return null;
            }

            var details = new CEStage2ApplicationDetailDto
            {
                ApplicationId = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                ApplicantName = application.Applicant.Name,
                ApplicantEmail = application.Applicant.Email,
                ApplicantContact = application.Applicant.Email ?? "", // Use email instead of phone
                BuildingType = application.Type.ToString(),
                PlotArea = 0,
                BuildingArea = 0,
                Floors = 0,
                District = "",
                Taluka = "",
                Village = "",
                EE2SignedDate = application.UpdatedDate,
                PaymentAmount = application.Transactions
                    .Where(t => t.Status == "SUCCESS")
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.Price)
                    .FirstOrDefault(),
                PaymentDate = application.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.CreatedAt)
                    .FirstOrDefault(),
                PaymentReference = application.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.TransactionId)
                    .FirstOrDefault() ?? "",
                CertificatePath = null, // Certificate path not stored in main model
                CurrentStatus = application.CurrentStatus.ToString(),
                JEApprovedDate = null,
                JEOfficerName = null,
                AEApprovedDate = null,
                AEOfficerName = null,
                EE1ApprovedDate = null,
                EE1OfficerName = null,
                CE1ApprovedDate = null,
                CE1OfficerName = null
            };

            return details;
        }

        /// <summary>
        /// Generate OTP for CE final digital signature via HSM
        /// </summary>
        public async Task<CEStage2OtpResult> GenerateOtpAsync(int applicationId, int ceUserId)
        {
            _logger.LogInformation($"[CEStage2Workflow] Generating OTP for CE final signature - Application: {applicationId}, CE: {ceUserId}");

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return new CEStage2OtpResult
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            if (application.CurrentStatus != ApplicationCurrentStatus.DigitalSignatureCompletedByEE2)
            {
                return new CEStage2OtpResult
                {
                    Success = false,
                    Message = "Application is not ready for CE final signature."
                };
            }

            try
            {
                // Generate OTP via HSM - Initiate signature process
                var certificatePath = $"certificates/{application.ApplicationNumber}_certificate.pdf";
                
                var otpResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    ceUserId,
                    SignatureType.CityEngineer,
                    certificatePath,
                    "100,650,200,50,1", // Signature coordinates on certificate PDF for CE (below EE signature)
                    null, // IP address (optional)
                    null  // User agent (optional)
                );
                
                if (!otpResult.Success)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Failed to generate OTP: {otpResult.Message}"
                    };
                }

                _logger.LogInformation($"[CEStage2Workflow] OTP generated successfully for application {applicationId}, SignatureId: {otpResult.SignatureId}");

                return new CEStage2OtpResult
                {
                    Success = true,
                    Message = "OTP generated successfully. Please check your registered mobile/email for the OTP code.",
                    OtpReference = otpResult.SignatureId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Workflow] Error generating OTP for application {applicationId}");
                return new CEStage2OtpResult
                {
                    Success = false,
                    Message = $"Error generating OTP: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Apply CE final digital signature with OTP verification
        /// Status: 20 → 21 → 22
        /// </summary>
        public async Task<CEStage2SignResult> ApplyFinalSignatureAsync(int applicationId, int ceUserId, string otpCode)
        {
            _logger.LogInformation($"[CEStage2Workflow] Applying CE final digital signature - Application: {applicationId}, CE: {ceUserId}");

            var application = await _context.Applications
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return new CEStage2SignResult
                {
                    Success = false,
                    Message = "Application not found."
                };
            }

            if (application.CurrentStatus != ApplicationCurrentStatus.DigitalSignatureCompletedByEE2)
            {
                return new CEStage2SignResult
                {
                    Success = false,
                    Message = "Application is not ready for CE final signature."
                };
            }

            try
            {
                // Get the officer's email for audit
                var ceOfficer = await _context.Officers.FindAsync(ceUserId);
                if (ceOfficer == null)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "City Engineer not found."
                    };
                }

                // Initiate the signature to get a signature ID
                var certificatePath = $"certificates/{application.ApplicationNumber}_certificate.pdf";
                
                var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    ceUserId,
                    SignatureType.CityEngineer,
                    certificatePath,
                    "100,650,200,50,1", // Signature coordinates
                    null, // IP address
                    null  // User agent
                );

                if (!initiateResult.Success || !initiateResult.SignatureId.HasValue)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Failed to initiate signature: {initiateResult.Message}"
                    };
                }

                // Complete the signature with OTP verification
                var completeResult = await _digitalSignatureService.CompleteSignatureAsync(
                    initiateResult.SignatureId.Value,
                    otpCode,
                    ceOfficer.Email // Use Email as the identifier for completion
                );

                if (!completeResult.Success)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Digital signature failed: {completeResult.Message}"
                    };
                }

                // Update application status: 20 → 21 → 22
                application.CurrentStatus = ApplicationCurrentStatus.UnderFinalApprovalByCE2;
                application.UpdatedDate = DateTime.UtcNow;

                // Immediately update to CertificateIssued status
                application.CurrentStatus = ApplicationCurrentStatus.CertificateIssued;
                application.UpdatedDate = DateTime.UtcNow;
                application.IsPaymentComplete = true;
                application.PaymentCompletedDate = DateTime.UtcNow;
                application.CertificateNumber = $"CERT-{application.ApplicationNumber}";
                application.CertificateIssuedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[CEStage2Workflow] CE final digital signature applied successfully for application {applicationId}. Status: CertificateIssued");

                // Send certificate ready notification email (async, non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var downloadLink = $"{_configuration["AppSettings:FrontendUrl"]}/download-certificate/{application.ApplicationNumber}";
                        var certificateNumber = application.CertificateNumber ?? "Not assigned";
                        
                        await _emailService.SendCertificateReadyEmailAsync(
                            application.Applicant?.Email ?? "",
                            application.Applicant?.Name ?? "",
                            application.ApplicationNumber,
                            certificateNumber,
                            downloadLink
                        );
                        
                        _logger.LogInformation($"[CEStage2Workflow] Certificate ready email sent to {application.Applicant?.Email} for application {applicationId}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"[CEStage2Workflow] Failed to send certificate ready email for application {applicationId}");
                    }
                });

                return new CEStage2SignResult
                {
                    Success = true,
                    Message = "Final digital signature applied successfully. Certificate is now issued and ready for download.",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.CertificateIssued.ToString(),
                    SignedCertificateUrl = completeResult.SignedDocumentPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Workflow] Error applying CE final signature for application {applicationId}");
                return new CEStage2SignResult
                {
                    Success = false,
                    Message = $"Error applying digital signature: {ex.Message}"
                };
            }
        }
    }

    #region DTOs

    public class CEStage2ApplicationDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string BuildingType { get; set; } = string.Empty;
        public double PlotArea { get; set; }
        public string District { get; set; } = string.Empty;
        public DateTime? EE2SignedDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
    }

    public class CEStage2ApplicationDetailDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantContact { get; set; } = string.Empty;
        public string BuildingType { get; set; } = string.Empty;
        public double PlotArea { get; set; }
        public double BuildingArea { get; set; }
        public int Floors { get; set; }
        public string District { get; set; } = string.Empty;
        public string Taluka { get; set; } = string.Empty;
        public string Village { get; set; } = string.Empty;
        public DateTime? EE2SignedDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public string? CertificatePath { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime? JEApprovedDate { get; set; }
        public string? JEOfficerName { get; set; }
        public DateTime? AEApprovedDate { get; set; }
        public string? AEOfficerName { get; set; }
        public DateTime? EE1ApprovedDate { get; set; }
        public string? EE1OfficerName { get; set; }
        public DateTime? CE1ApprovedDate { get; set; }
        public string? CE1OfficerName { get; set; }
    }

    public class CEStage2OtpResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OtpReference { get; set; }
    }

    public class CEStage2SignResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ApplicationId { get; set; }
        public string? NewStatus { get; set; }
        public string? SignedCertificateUrl { get; set; }
    }

    public class CEStage2SignRequest
    {
        public string OtpCode { get; set; } = string.Empty;
    }

    #endregion
}
