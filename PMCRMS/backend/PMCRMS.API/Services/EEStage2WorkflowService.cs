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
        private readonly IConfiguration _configuration;

        public EEStage2WorkflowService(
            PMCRMSDbContext context,
            ILogger<EEStage2WorkflowService> logger,
            IEmailService emailService,
            IDigitalSignatureService digitalSignatureService,
            IAutoAssignmentService autoAssignmentService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _digitalSignatureService = digitalSignatureService;
            _autoAssignmentService = autoAssignmentService;
            _configuration = configuration;
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

                // Get EE signature coordinates from configuration for license certificate
                var eeCoordinates = _configuration["HSM:LicenseCertificateSignatureCoordinates:ExecutiveEngineer"] 
                    ?? "100,50,250,110,1";
                
                _logger.LogInformation($"[EEStage2Workflow] Using EE signature coordinates for license certificate: {eeCoordinates}");
                
                var otpResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    eeUserId,
                    SignatureType.ExecutiveEngineer,
                    tempCertPath,
                    eeCoordinates,
                    null,
                    null
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
        /// Updates status from EXECUTIVE_ENGINEER_SIGN_PENDING (32) to CITY_ENGINEER_SIGN_PENDING (34)
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

                // Save certificate temporarily for signing
                var tempCertPath = Path.Combine(Path.GetTempPath(), $"LICENSE_CERT_{application.ApplicationNumber}_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempCertPath, licenseCertificate.FileContent);

                // Get EE signature coordinates from configuration for license certificate
                var eeCoordinates = _configuration["HSM:LicenseCertificateSignatureCoordinates:ExecutiveEngineer"] 
                    ?? "100,50,250,110,1";
                
                _logger.LogInformation($"[EEStage2Workflow] Using EE signature coordinates for license certificate: {eeCoordinates}");

                // Initiate signature on license certificate
                var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    eeUserId,
                    SignatureType.ExecutiveEngineer,
                    tempCertPath,
                    eeCoordinates,
                    null,
                    null
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
                    _logger.LogError($"[EEStage2Workflow] CompleteSignature failed: {completeResult.Message}");
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = $"Digital signature failed: {completeResult.Message}"
                    };
                }

                _logger.LogInformation($"[EEStage2Workflow] CompleteSignature SUCCESS. SignedDocumentPath: {completeResult.SignedDocumentPath}");

                // Update the license certificate in database with signed version
                if (string.IsNullOrEmpty(completeResult.SignedDocumentPath))
                {
                    _logger.LogError($"[EEStage2Workflow] SignedDocumentPath is null or empty for application {applicationId}");
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = "Signed document path is missing"
                    };
                }

                if (!File.Exists(completeResult.SignedDocumentPath))
                {
                    _logger.LogError($"[EEStage2Workflow] Signed document file does not exist at path: {completeResult.SignedDocumentPath}");
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = "Signed document file not found"
                    };
                }

                try
                {
                    var signedPdfBytes = await File.ReadAllBytesAsync(completeResult.SignedDocumentPath);
                    _logger.LogInformation($"[EEStage2Workflow] Read {signedPdfBytes.Length} bytes from signed PDF");
                    
                    licenseCertificate.FileContent = signedPdfBytes;
                    licenseCertificate.UpdatedDate = DateTime.UtcNow;
                    
                    _logger.LogInformation($"[EEStage2Workflow] Updated license certificate FileContent in memory ({signedPdfBytes.Length} bytes) for application {applicationId}");
                }
                catch (Exception certEx)
                {
                    _logger.LogError(certEx, $"[EEStage2Workflow] CRITICAL: Failed to read/update license certificate for application {applicationId}");
                    return new EEStage2SignResult
                    {
                        Success = false,
                        Message = $"Failed to update certificate: {certEx.Message}"
                    };
                }

                // Update application status to CITY_ENGINEER_SIGN_PENDING and mark EE Stage 2 signature complete
                application.Status = ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING;
                application.EEStage2DigitalSignatureApplied = true;
                application.EEStage2DigitalSignatureDate = DateTime.UtcNow;
                application.EEStage2ApprovalStatus = true;
                application.EEStage2ApprovalDate = DateTime.UtcNow;
                application.UpdatedDate = DateTime.UtcNow;

                _logger.LogInformation($"[EEStage2Workflow] BEFORE SaveChanges - Application {applicationId} Status: {application.Status} (Enum Value: {(int)application.Status}), Certificate bytes: {licenseCertificate.FileContent?.Length ?? 0}");

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[EEStage2Workflow] AFTER SaveChanges - Application {applicationId} Status: {application.Status} (Enum Value: {(int)application.Status}), Digital signature applied successfully");
                
                // Verify the status was actually saved by re-reading from database
                var savedApplication = await _context.PositionApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId);
                _logger.LogInformation($"[EEStage2Workflow] VERIFICATION - Re-read from DB - Application {applicationId} Status: {savedApplication?.Status} (Enum Value: {(int)(savedApplication?.Status ?? 0)}), AssignedCEStage2Id: {savedApplication?.AssignedCEStage2Id}");


                // Auto-assign to City Engineer for final signature
                try
                {
                    _logger.LogInformation($"[EEStage2Workflow] Attempting auto-assignment to City Engineer for application {applicationId}");
                    
                    var assignmentResult = await _autoAssignmentService.AutoAssignToNextWorkflowStageAsync(
                        applicationId,
                        ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING,
                        eeUserId
                    );

                    if (assignmentResult != null)
                    {
                        _logger.LogInformation(
                            "[EEStage2Workflow] ✅ Application {ApplicationId} auto-assigned to City Engineer (Officer ID: {OfficerId}) for final signature",
                            applicationId,
                            assignmentResult.AssignedToOfficerId
                        );
                        
                        // Re-verify the assignment was saved
                        var verifyAssignment = await _context.PositionApplications.AsNoTracking()
                            .Where(a => a.Id == applicationId)
                            .Select(a => new { a.Id, a.Status, a.AssignedCEStage2Id, a.AssignedToCEStage2Date })
                            .FirstOrDefaultAsync();
                        _logger.LogInformation(
                            "[EEStage2Workflow] Assignment verification - App {AppId}: Status={Status}, AssignedCEStage2Id={CEId}, AssignedDate={Date}",
                            verifyAssignment?.Id, verifyAssignment?.Status, verifyAssignment?.AssignedCEStage2Id, verifyAssignment?.AssignedToCEStage2Date
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[EEStage2Workflow] ⚠️ Application {ApplicationId} status updated to CITY_ENGINEER_SIGN_PENDING but no City Engineer available for auto-assignment. Application will appear as unassigned on CE dashboard.",
                            applicationId
                        );
                    }
                }
                catch (Exception assignEx)
                {
                    _logger.LogError(assignEx, "[EEStage2Workflow] ❌ Failed to auto-assign to City Engineer for application {ApplicationId}. Application will appear as unassigned on CE dashboard.", applicationId);
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
