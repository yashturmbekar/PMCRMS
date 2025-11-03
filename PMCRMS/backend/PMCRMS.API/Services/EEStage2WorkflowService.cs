using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for EE Stage 2 workflow (Digital Signature on Certificate)
    /// Handles applications after clerk processing
    /// Status progression: ProcessedByClerk (18) → UnderDigitalSignatureByEE2 (19) → DigitalSignatureCompletedByEE2 (20) → forwards to CE Stage 2 (21)
    /// </summary>
    public class EEStage2WorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<EEStage2WorkflowService> _logger;
        private readonly IEmailService _emailService;
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly IAutoAssignmentService _autoAssignmentService;

        public EEStage2WorkflowService(
            PMCRMSDbContext context,
            ILogger<EEStage2WorkflowService> logger,
            IEmailService emailService,
            IDigitalSignatureService digitalSignatureService,
            IAutoAssignmentService autoAssignmentService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _digitalSignatureService = digitalSignatureService;
            _autoAssignmentService = autoAssignmentService;
        }

        /// <summary>
        /// Get all pending applications for EE Stage 2 signature (EXECUTIVE_ENGINEER_SIGN_PENDING status)
        /// </summary>
        public async Task<List<EEStage2ApplicationDto>> GetPendingApplicationsAsync()
        {
            try
            {
                _logger.LogInformation("[EEStage2Workflow] Getting pending applications (EXECUTIVE_ENGINEER_SIGN_PENDING status)");

                var applications = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.Status == ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING)
                    .OrderBy(a => a.ClerkApprovalDate ?? a.UpdatedDate)
                    .ToListAsync();

                // Get payment amounts from Challan table
                var applicationIds = applications.Select(a => a.Id).ToList();
                var challans = await _context.Challans
                    .Where(c => applicationIds.Contains(c.ApplicationId))
                    .ToDictionaryAsync(c => c.ApplicationId, c => c.Amount);

                var result = applications.Select(a =>
                {
                    // Get permanent address
                    var permanentAddress = a.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                    var addressText = permanentAddress != null
                        ? $"{permanentAddress.AddressLine1}, {permanentAddress.AddressLine2}, {permanentAddress.AddressLine3}".TrimEnd(',', ' ')
                        : "";

                    return new EEStage2ApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        PositionType = a.PositionType.ToString(),
                        PropertyAddress = addressText,
                        PaymentAmount = challans.TryGetValue(a.Id, out var amount) ? amount : null,
                        ProcessedByClerkDate = a.ClerkApprovalDate ?? a.UpdatedDate ?? a.CreatedDate,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    };
                }).ToList();

                _logger.LogInformation($"[EEStage2Workflow] Found {result.Count} pending applications");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EEStage2Workflow] Error getting pending applications");
                throw;
            }
        }

        /// <summary>
        /// Get application details for EE Stage 2 review
        /// </summary>
        public async Task<EEStage2ApplicationDetailDto?> GetApplicationDetailsAsync(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[EEStage2Workflow] Getting application details: {applicationId}");

                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.Id == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                // Get challan for payment amount
                var challan = await _context.Challans
                    .Where(c => c.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                // Get permanent address
                var permanentAddress = application.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                var addressText = permanentAddress != null
                    ? $"{permanentAddress.AddressLine1}, {permanentAddress.AddressLine2}, {permanentAddress.AddressLine3}".TrimEnd(',', ' ')
                    : "";

                var detail = new EEStage2ApplicationDetailDto
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber ?? "",
                    ApplicantName = $"{application.FirstName} {(string.IsNullOrEmpty(application.MiddleName) ? "" : application.MiddleName + " ")}{application.LastName}".Trim(),
                    ApplicantEmail = application.EmailAddress,
                    PositionType = application.PositionType.ToString(),
                    PropertyAddress = addressText,
                    CurrentStatus = application.Status.ToString(),
                    PaymentAmount = challan?.Amount,
                    ProcessedByClerkDate = application.ClerkApprovalDate ?? application.UpdatedDate ?? application.CreatedDate,
                    CertificateNumber = null, // Will be generated after signature
                    StatusHistoryCount = 0, // Can be implemented later if needed
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate
                };

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EEStage2Workflow] Error getting application details: {applicationId}");
                throw;
            }
        }

        /// <summary>
        /// Generate OTP for digital signature
        /// </summary>
        public async Task<EEStage2OtpResult> GenerateOtpAsync(int applicationId, int eeUserId)
        {
            try
            {
                _logger.LogInformation($"[EEStage2Workflow] Generating OTP for application {applicationId} by EE {eeUserId}");

                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return new EEStage2OtpResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.Status != ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING)
                {
                    return new EEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Application is not in EXECUTIVE_ENGINEER_SIGN_PENDING status. Current status: {application.Status}"
                    };
                }

                // Fetch the license certificate from database
                var licenseCertificate = await _context.SEDocuments
                    .Where(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();

                if (licenseCertificate == null || licenseCertificate.FileContent == null)
                {
                    return new EEStage2OtpResult
                    {
                        Success = false,
                        Message = "License certificate not found. Please ensure the certificate has been generated."
                    };
                }

                // Save certificate temporarily for signing
                var tempCertPath = Path.Combine(Path.GetTempPath(), $"LICENSE_CERT_{application.ApplicationNumber}_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempCertPath, licenseCertificate.FileContent);
                
                var otpResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    eeUserId,
                    SignatureType.ExecutiveEngineer,
                    tempCertPath,
                    "50,50,180,60,1", // EE signature coordinates on LEFT side of license certificate (x,y,width,height,page)
                    null, // IP address (optional)
                    null  // User agent (optional)
                );
                
                if (!otpResult.Success)
                {
                    return new EEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Failed to generate OTP: {otpResult.Message}"
                    };
                }

                _logger.LogInformation($"[EEStage2Workflow] OTP generated successfully for application {applicationId}, SignatureId: {otpResult.SignatureId}");

                return new EEStage2OtpResult
                {
                    Success = true,
                    Message = "OTP generated successfully. Please check your registered mobile/email for the OTP code.",
                    OtpReference = otpResult.SignatureId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EEStage2Workflow] Error generating OTP for application {applicationId}");
                return new EEStage2OtpResult
                {
                    Success = false,
                    Message = $"Error generating OTP: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Apply digital signature to certificate
        /// Updates status from EXECUTIVE_ENGINEER_SIGN_PENDING (32) to CITY_ENGINEER_SIGN_PENDING (33)
        /// </summary>
        public async Task<EEStage2SignResult> ApplyDigitalSignatureAsync(int applicationId, int eeUserId, string otpCode)
        {
            try
            {
                _logger.LogInformation($"[EEStage2Workflow] Applying digital signature for application {applicationId} by EE {eeUserId}");

                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.Status != ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = $"Application is not in EXECUTIVE_ENGINEER_SIGN_PENDING status. Current status: {application.Status}"
                    };
                }

                // Get the officer's username for audit
                var eeOfficer = await _context.Officers.FindAsync(eeUserId);
                if (eeOfficer == null)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = "Executive Engineer not found."
                    };
                }

                // First, get the license certificate document from database
                var licenseCertificate = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                            d.DocumentType == SEDocumentType.LicenceCertificate);

                if (licenseCertificate == null || licenseCertificate.FileContent == null)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = "License certificate not found. Certificate must be generated before signing."
                    };
                }

                // Initiate signature on license certificate
                // Coordinates: Executive Engineer signature (left side at bottom)
                // Format: x,y,width,height,page
                // Position: Left side, 50 points from left, 50 points from bottom
                var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    eeUserId,
                    SignatureType.ExecutiveEngineer,
                    $"LICENSE_CERT_{application.ApplicationNumber}",
                    "50,50,180,60,1", // Left signature box (x=50, y=50 from bottom, width=180, height=60, page=1)
                    null, // IP address
                    null  // User agent
                );

                if (!initiateResult.Success || !initiateResult.SignatureId.HasValue)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = $"Failed to initiate signature: {initiateResult.Message}"
                    };
                }

                // Complete the signature with OTP verification
                var completeResult = await _digitalSignatureService.CompleteSignatureAsync(
                    initiateResult.SignatureId.Value,
                    otpCode,
                    eeOfficer.Email // Use Email as the identifier for completion
                );

                if (!completeResult.Success)
                {
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = $"Digital signature failed: {completeResult.Message}"
                    };
                }

                // Update the license certificate in database with signed version
                try
                {
                    if (!string.IsNullOrEmpty(completeResult.SignedDocumentPath) && File.Exists(completeResult.SignedDocumentPath))
                    {
                        var signedPdfBytes = await File.ReadAllBytesAsync(completeResult.SignedDocumentPath);
                        licenseCertificate.FileContent = signedPdfBytes;
                        licenseCertificate.UpdatedDate = DateTime.UtcNow;
                        
                        _logger.LogInformation($"[EEStage2Workflow] Updated license certificate in database with EE signature for application {applicationId}");
                    }
                }
                catch (Exception certEx)
                {
                    _logger.LogError(certEx, $"[EEStage2Workflow] Failed to update license certificate in database for application {applicationId}");
                    // Don't fail the entire operation if this fails
                }

                // Update application status to CITY_ENGINEER_SIGN_PENDING and mark EE Stage 2 signature complete
                application.Status = ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING;
                application.EEStage2DigitalSignatureApplied = true;
                application.EEStage2DigitalSignatureDate = DateTime.UtcNow;
                application.EEStage2ApprovalStatus = true;
                application.EEStage2ApprovalDate = DateTime.UtcNow;
                application.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[EEStage2Workflow] Digital signature applied successfully for application {applicationId}");

                // Auto-assign to City Engineer for final signature
                try
                {
                    var assignmentResult = await _autoAssignmentService.AutoAssignToNextWorkflowStageAsync(
                        applicationId,
                        ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING,
                        eeUserId
                    );

                    if (assignmentResult != null)
                    {
                        _logger.LogInformation(
                            "[EEStage2Workflow] Application {ApplicationId} auto-assigned to City Engineer {OfficerId} for final signature",
                            applicationId,
                            assignmentResult.AssignedToOfficerId
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[EEStage2Workflow] Application {ApplicationId} status updated to CITY_ENGINEER_SIGN_PENDING but no City Engineer available for auto-assignment",
                            applicationId
                        );
                    }
                }
                catch (Exception assignEx)
                {
                    _logger.LogError(assignEx, "[EEStage2Workflow] Failed to auto-assign to City Engineer for application {ApplicationId}", applicationId);
                    // Don't fail the signature operation if assignment fails
                }

                // Send email notification (non-blocking)
                try
                {
                    var viewUrl = $"{GetBaseUrl()}/view-application/{applicationId}";
                    // TODO: Implement SendEE2SignatureCompletedEmailAsync in EmailService
                    // await _emailService.SendEE2SignatureCompletedEmailAsync(
                    //     application.EmailAddress,
                    //     $"{application.FirstName} {application.LastName}",
                    //     application.ApplicationNumber,
                    //     viewUrl
                    // );
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"[EEStage2Workflow] Failed to send signature completion email for application {applicationId}");
                }

                return new EEStage2SignResult
                {
                    Success = true,
                    Message = "Digital signature applied successfully. Application forwarded to City Engineer for final approval.",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING.ToString(),
                    SignedCertificateUrl = completeResult.SignedDocumentPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EEStage2Workflow] Error applying digital signature for application {applicationId}");
                return new EEStage2SignResult
                {
                    Success = false,
                    Message = $"Error applying digital signature: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get completed applications (signed by EE Stage 2)
        /// </summary>
        public async Task<List<EEStage2ApplicationDto>> GetCompletedApplicationsAsync()
        {
            try
            {
                _logger.LogInformation("[EEStage2Workflow] Getting completed applications");

                var applications = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.EEStage2DigitalSignatureApplied == true)
                    .OrderByDescending(a => a.EEStage2DigitalSignatureDate)
                    .ToListAsync();

                // Get payment amounts from Challan table
                var applicationIds = applications.Select(a => a.Id).ToList();
                var challans = await _context.Challans
                    .Where(c => applicationIds.Contains(c.ApplicationId))
                    .ToDictionaryAsync(c => c.ApplicationId, c => c.Amount);

                var result = applications.Select(a =>
                {
                    // Get permanent address
                    var permanentAddress = a.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                    var addressText = permanentAddress != null
                        ? $"{permanentAddress.AddressLine1}, {permanentAddress.AddressLine2}, {permanentAddress.AddressLine3}".TrimEnd(',', ' ')
                        : "";

                    return new EEStage2ApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        PositionType = a.PositionType.ToString(),
                        PropertyAddress = addressText,
                        PaymentAmount = challans.TryGetValue(a.Id, out var amount) ? amount : null,
                        ProcessedByClerkDate = a.ClerkApprovalDate ?? a.UpdatedDate ?? a.CreatedDate,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    };
                }).ToList();

                _logger.LogInformation($"[EEStage2Workflow] Found {result.Count} completed applications");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EEStage2Workflow] Error getting completed applications");
                throw;
            }
        }

        private string GetBaseUrl()
        {
            return Environment.GetEnvironmentVariable("FRONTEND_URL") ?? throw new InvalidOperationException("FRONTEND_URL environment variable not configured");
        }
    }

    // DTOs for EE Stage 2 Workflow
    public class EEStage2ApplicationDto
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string PositionType { get; set; } = string.Empty; // Changed from ApplicationType to match frontend
        public string PropertyAddress { get; set; } = string.Empty;
        public decimal? PaymentAmount { get; set; }
        public DateTime ProcessedByClerkDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EEStage2ApplicationDetailDto : EEStage2ApplicationDto
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public string? CertificateNumber { get; set; }
        public int StatusHistoryCount { get; set; }
    }

    public class EEStage2OtpResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OtpReference { get; set; }
    }

    public class EEStage2SignResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? SignedCertificateUrl { get; set; }
    }

    public class EEStage2SignRequest
    {
        public string OtpCode { get; set; } = string.Empty;
    }
}
