using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for City Engineer Stage 2 workflow for Position Applications (Licensing)
    /// Handles City Engineer final digital signature on license certificate after EE Stage 2
    /// Status progression: 
    ///   CITY_ENGINEER_SIGN_PENDING (34) → APPROVED (36) [New workflow]
    ///   CITY_ENGINEER_PENDING (33) → APPROVED (36) [Legacy workflow compatibility]
    /// </summary>
    public class CEStage2WorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<CEStage2WorkflowService> _logger;
        private readonly IEmailService _emailService;
        private readonly IHsmService _hsmService;
        private readonly IConfiguration _configuration;

        public CEStage2WorkflowService(
            PMCRMSDbContext context,
            ILogger<CEStage2WorkflowService> logger,
            IEmailService emailService,
            IHsmService hsmService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _hsmService = hsmService;
            _configuration = configuration;
        }

        /// <summary>
        /// Get pending applications for CE Stage 2 final signature
        /// </summary>
        public async Task<List<CEStage2ApplicationDto>> GetPendingApplicationsAsync(int? ceUserId = null)
        {
            try
            {
                _logger.LogInformation("[CEStage2Workflow] Fetching pending applications for CE final signature. CE User ID: {CeUserId}", ceUserId);
                _logger.LogInformation("[CEStage2Workflow] Querying for Status: CITY_ENGINEER_SIGN_PENDING (34) OR CITY_ENGINEER_PENDING (33)");

                var query = _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.Status == ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING 
                             || a.Status == ApplicationCurrentStatus.CITY_ENGINEER_PENDING);

                // Log ALL applications with these statuses before filtering by CE
                var allApplicationsWithStatus = await _context.PositionApplications
                    .Where(a => a.Status == ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING 
                             || a.Status == ApplicationCurrentStatus.CITY_ENGINEER_PENDING)
                    .Select(a => new { a.Id, a.ApplicationNumber, a.AssignedCEStage2Id, a.AssignedCityEngineerId, a.Status })
                    .ToListAsync();
                
                _logger.LogInformation("[CEStage2Workflow] Total applications with CITY_ENGINEER_SIGN_PENDING or CITY_ENGINEER_PENDING status: {Count}", allApplicationsWithStatus.Count);
                foreach (var app in allApplicationsWithStatus)
                {
                    _logger.LogInformation("[CEStage2Workflow] - App {Id} ({AppNumber}): Status={Status} (Value: {StatusValue}), AssignedCEStage2Id={CEId}, AssignedCityEngineerId={OldCEId}", 
                        app.Id, app.ApplicationNumber, app.Status, (int)app.Status, app.AssignedCEStage2Id, app.AssignedCityEngineerId);
                }

                // If ceUserId is provided, filter by assigned CE (check both new and old CE assignment fields)
                if (ceUserId.HasValue)
                {
                    _logger.LogInformation("[CEStage2Workflow] Filtering for CE User ID: {CeUserId} OR unassigned applications", ceUserId.Value);
                    query = query.Where(a => 
                        a.AssignedCEStage2Id == ceUserId.Value 
                        || a.AssignedCityEngineerId == ceUserId.Value 
                        || (a.AssignedCEStage2Id == null && a.AssignedCityEngineerId == null));
                }
                else
                {
                    _logger.LogInformation("[CEStage2Workflow] No CE filter applied (Admin view) - showing all applications");
                }

                var applications = await query
                    .OrderBy(a => a.AssignedToCEStage2Date ?? a.UpdatedDate)
                    .ToListAsync();

                _logger.LogInformation("[CEStage2Workflow] After CE filter: {Count} applications", applications.Count);

                // Get payment amounts from Challan table
                var applicationIds = applications.Select(a => a.Id).ToList();
                var challans = await _context.Challans
                    .Where(c => applicationIds.Contains(c.ApplicationId))
                    .ToDictionaryAsync(c => c.ApplicationId, c => c.Amount);

                var result = applications.Select(a =>
                {
                    var permanentAddress = a.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                    var addressText = permanentAddress != null
                        ? $"{permanentAddress.AddressLine1}, {permanentAddress.City}".TrimEnd(',', ' ')
                        : "";

                    return new CEStage2ApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        PositionType = a.PositionType.ToString(),
                        PropertyAddress = addressText,
                        PaymentAmount = challans.TryGetValue(a.Id, out var amount) ? amount : null,
                        EEStage2SignedDate = a.EEStage2DigitalSignatureDate,
                        AssignedDate = a.AssignedToCEStage2Date,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    };
                }).ToList();

                _logger.LogInformation($"[CEStage2Workflow] Found {result.Count} pending applications for CE final signature");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CEStage2Workflow] Error getting pending applications");
                throw;
            }
        }

        /// <summary>
        /// Get application details for CE Stage 2 review
        /// </summary>
        public async Task<CEStage2ApplicationDetailDto?> GetApplicationDetailsAsync(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[CEStage2Workflow] Getting application details: {applicationId}");

                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.AssignedEEStage2)
                    .Where(a => a.Id == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                // Get challan for payment amount
                var challan = await _context.Challans
                    .Where(c => c.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                var permanentAddress = application.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                var addressText = permanentAddress != null
                    ? $"{permanentAddress.AddressLine1}, {permanentAddress.AddressLine2}, {permanentAddress.City}".TrimEnd(',', ' ')
                    : "";

                var detail = new CEStage2ApplicationDetailDto
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber ?? "",
                    ApplicantName = $"{application.FirstName} {(string.IsNullOrEmpty(application.MiddleName) ? "" : application.MiddleName + " ")}{application.LastName}".Trim(),
                    ApplicantEmail = application.EmailAddress,
                    PositionType = application.PositionType.ToString(),
                    PropertyAddress = addressText,
                    CurrentStatus = application.Status.ToString(),
                    PaymentAmount = challan?.Amount,
                    EEStage2SignedDate = application.EEStage2DigitalSignatureDate,
                    EEStage2OfficerName = application.AssignedEEStage2?.Name,
                    AssignedDate = application.AssignedToCEStage2Date,
                    CreatedAt = application.CreatedDate,
                    UpdatedAt = application.UpdatedDate ?? application.CreatedDate
                };

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CEStage2Workflow] Error getting application details: {applicationId}");
                throw;
            }
        }

        /// <summary>
        /// Generate OTP for CE final digital signature
        /// </summary>
        public async Task<CEStage2OtpResult> GenerateOtpAsync(int applicationId, int ceUserId)
        {
            try
            {
                _logger.LogInformation($"[CEStage2Workflow] Generating OTP for application {applicationId} by CE {ceUserId}");

                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.Status != ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING 
                    && application.Status != ApplicationCurrentStatus.CITY_ENGINEER_PENDING)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Application is not in CITY_ENGINEER_SIGN_PENDING or CITY_ENGINEER_PENDING status. Current status: {application.Status}"
                    };
                }

                // Verify CE is assigned to this application (check both new and old assignment fields)
                if (application.AssignedCEStage2Id != ceUserId 
                    && application.AssignedCityEngineerId != ceUserId)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = "You are not authorized to sign this application"
                    };
                }

                // Fetch the license certificate from database
                var licenseCertificate = await _context.SEDocuments
                    .Where(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();

                if (licenseCertificate == null || licenseCertificate.FileContent == null)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = "License certificate not found. Please ensure the certificate has been generated and signed by Executive Engineer."
                    };
                }

                // Get CE officer and KeyLabel for OTP generation
                var ceOfficer = await _context.Officers.FindAsync(ceUserId);
                if (ceOfficer == null)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = "City Engineer not found"
                    };
                }

                var keyLabel = _configuration[$"HSM:KeyLabels:CityEngineer"];

                if (string.IsNullOrEmpty(keyLabel))
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = "KeyLabel not configured for CityEngineer role"
                    };
                }

                _logger.LogInformation(
                    "Generating OTP for CE Stage 2 using KeyLabel '{KeyLabel}' for officer {OfficerId}",
                    keyLabel, ceUserId);

                // Generate OTP using HSM
                var otpResult = await _hsmService.GenerateOtpAsync(
                    transactionId: applicationId.ToString(),
                    keyLabel: keyLabel,
                    otpType: "single"
                );
                
                if (!otpResult.Success)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Failed to generate OTP: {otpResult.ErrorMessage}"
                    };
                }

                _logger.LogInformation("✅ [CEStage2Workflow] OTP generated and sent by HSM for application {ApplicationId}: {Message}",
                    applicationId, otpResult.Message);

                return new CEStage2OtpResult
                {
                    Success = true,
                    Message = otpResult.Message ?? "OTP sent successfully. Please check your registered mobile/email.",
                    OtpReference = applicationId.ToString()
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
        /// Apply CE final digital signature to license certificate
        /// Updates status from CITY_ENGINEER_SIGN_PENDING (34) or CITY_ENGINEER_PENDING (33) to APPROVED (36)
        /// </summary>
        public async Task<CEStage2SignResult> ApplyFinalSignatureAsync(int applicationId, int ceUserId, string otpCode)
        {
            try
            {
                _logger.LogInformation($"[CEStage2Workflow] Applying CE final digital signature for application {applicationId} by CE {ceUserId}");

                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                if (application.Status != ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING
                    && application.Status != ApplicationCurrentStatus.CITY_ENGINEER_PENDING)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Application is not in CITY_ENGINEER_SIGN_PENDING or CITY_ENGINEER_PENDING status. Current status: {application.Status}"
                    };
                }

                // Verify CE is assigned (check both new and old assignment fields)
                if (application.AssignedCEStage2Id != ceUserId
                    && application.AssignedCityEngineerId != ceUserId)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "You are not authorized to sign this application"
                    };
                }

                // Get the officer
                var ceOfficer = await _context.Officers.FindAsync(ceUserId);
                if (ceOfficer == null)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "City Engineer not found."
                    };
                }

                // Fetch the license certificate from database
                var licenseCertificate = await _context.SEDocuments
                    .Where(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();

                if (licenseCertificate == null || licenseCertificate.FileContent == null)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "License certificate not found. Please ensure the certificate has been generated and signed by Executive Engineer."
                    };
                }

                // Get KeyLabel for CE from configuration
                var keyLabel = _configuration[$"HSM:KeyLabels:CityEngineer"];

                if (string.IsNullOrEmpty(keyLabel))
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "KeyLabel not configured for CityEngineer role"
                    };
                }

                _logger.LogInformation(
                    "Signing license certificate for CE Stage 2 using KeyLabel '{KeyLabel}' for officer {OfficerId}",
                    keyLabel, ceUserId);

                // Get CE signature coordinates from configuration for license certificate
                var ceCoordinates = _configuration["HSM:LicenseCertificateSignatureCoordinates:CityEngineer"] 
                    ?? "320,50,470,110,1";
                
                _logger.LogInformation(
                    "📍 Signature coordinates for {Role} on license certificate: {Coordinates}",
                    ceOfficer.Role, ceCoordinates);

                // Convert PDF to Base64
                var base64Pdf = Convert.ToBase64String(licenseCertificate.FileContent);

                // Sign PDF using HSM
                var signRequest = new HsmSignRequest
                {
                    TransactionId = applicationId.ToString(),
                    KeyLabel = keyLabel,
                    Base64Pdf = base64Pdf,
                    Otp = otpCode,
                    Coordinates = ceCoordinates,
                    PageLocation = "last",
                    OtpType = "single"
                };

                var signResult = await _hsmService.SignPdfAsync(signRequest);

                if (!signResult.Success)
                {
                    _logger.LogError("HSM signature failed for application {ApplicationId}: {Error}", 
                        applicationId, signResult.ErrorMessage);
                    
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Digital signature failed: {signResult.ErrorMessage}"
                    };
                }

                if (string.IsNullOrEmpty(signResult.SignedPdfBase64))
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "Signed PDF not returned by HSM"
                    };
                }

                // Convert Base64 back to bytes and update database
                var signedPdfBytes = Convert.FromBase64String(signResult.SignedPdfBase64);
                licenseCertificate.FileContent = signedPdfBytes;
                licenseCertificate.FileSize = (decimal)(signedPdfBytes.Length / 1024.0);
                licenseCertificate.UpdatedDate = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "[CEStage2Workflow] Updated license certificate FileContent in database ({Size} KB) for application {ApplicationId}",
                    licenseCertificate.FileSize, applicationId);

                // Update application - final approval
                application.Status = ApplicationCurrentStatus.APPROVED;
                application.CEStage2DigitalSignatureApplied = true;
                application.CEStage2DigitalSignatureDate = DateTime.UtcNow;
                application.CEStage2ApprovalStatus = true;
                application.CEStage2ApprovalDate = DateTime.UtcNow;
                application.ApprovedDate = DateTime.UtcNow;
                application.UpdatedDate = DateTime.UtcNow;

                _logger.LogInformation($"[CEStage2Workflow] BEFORE SaveChanges - Application Status: {application.Status}, Certificate bytes: {licenseCertificate.FileContent?.Length ?? 0}");

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[CEStage2Workflow] AFTER SaveChanges - CE final digital signature applied successfully for application {applicationId}. Status: APPROVED");

                // Send certificate ready notification (async, non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var downloadLink = $"{_configuration["AppSettings:FrontendUrl"]}/download-license/{application.ApplicationNumber}";
                        var licenseNumber = application.ApplicationNumber;
                        
                        await _emailService.SendCertificateReadyEmailAsync(
                            application.EmailAddress,
                            $"{application.FirstName} {application.LastName}",
                            application.ApplicationNumber ?? "N/A",
                            licenseNumber ?? "Not assigned",
                            downloadLink
                        );
                        
                        _logger.LogInformation($"[CEStage2Workflow] License ready email sent to {application.EmailAddress}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"[CEStage2Workflow] Failed to send license ready email");
                    }
                });

                return new CEStage2SignResult
                {
                    Success = true,
                    Message = "Final digital signature applied successfully. License certificate is now approved and ready for download.",
                    ApplicationId = applicationId,
                    NewStatus = ApplicationCurrentStatus.APPROVED.ToString(),
                    SignedCertificateUrl = $"/api/Download/certificate/{applicationId}"
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

        /// <summary>
        /// Get completed applications (signed by CE Stage 2)
        /// </summary>
        public async Task<List<CEStage2ApplicationDto>> GetCompletedApplicationsAsync(int? ceUserId = null)
        {
            try
            {
                _logger.LogInformation("[CEStage2Workflow] Getting completed applications");

                var query = _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.CEStage2DigitalSignatureApplied == true);

                if (ceUserId.HasValue)
                {
                    query = query.Where(a => a.AssignedCEStage2Id == ceUserId.Value);
                }

                var applications = await query
                    .OrderByDescending(a => a.CEStage2DigitalSignatureDate)
                    .ToListAsync();

                var applicationIds = applications.Select(a => a.Id).ToList();
                var challans = await _context.Challans
                    .Where(c => applicationIds.Contains(c.ApplicationId))
                    .ToDictionaryAsync(c => c.ApplicationId, c => c.Amount);

                var result = applications.Select(a =>
                {
                    var permanentAddress = a.Addresses.FirstOrDefault(addr => addr.AddressType == "Permanent");
                    var addressText = permanentAddress != null
                        ? $"{permanentAddress.AddressLine1}, {permanentAddress.City}".TrimEnd(',', ' ')
                        : "";

                    return new CEStage2ApplicationDto
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber ?? "",
                        ApplicantName = $"{a.FirstName} {(string.IsNullOrEmpty(a.MiddleName) ? "" : a.MiddleName + " ")}{a.LastName}".Trim(),
                        ApplicantEmail = a.EmailAddress,
                        PositionType = a.PositionType.ToString(),
                        PropertyAddress = addressText,
                        PaymentAmount = challans.TryGetValue(a.Id, out var amount) ? amount : null,
                        EEStage2SignedDate = a.EEStage2DigitalSignatureDate,
                        AssignedDate = a.AssignedToCEStage2Date,
                        CreatedAt = a.CreatedDate,
                        UpdatedAt = a.UpdatedDate ?? a.CreatedDate
                    };
                }).ToList();

                _logger.LogInformation($"[CEStage2Workflow] Found {result.Count} completed applications");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CEStage2Workflow] Error getting completed applications");
                throw;
            }
        }
    }

    #region DTOs

    public class CEStage2ApplicationDto
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string PositionType { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public decimal? PaymentAmount { get; set; }
        public DateTime? EEStage2SignedDate { get; set; }
        public DateTime? AssignedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CEStage2ApplicationDetailDto : CEStage2ApplicationDto
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public string? EEStage2OfficerName { get; set; }
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
        public int ApplicationId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? SignedCertificateUrl { get; set; }
    }

    public class CEStage2SignRequest
    {
        public string OtpCode { get; set; } = string.Empty;
    }

    #endregion
}

