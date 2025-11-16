using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for City Engineer Stage 2 workflow for Position Applications (Licensing)
    /// Handles City Engineer final digital signature on license certificate after EE Stage 2
    /// Status progression: CITY_ENGINEER_SIGN_PENDING (34) → APPROVED (36)
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
        /// Get pending applications for CE Stage 2 final signature
        /// </summary>
        public async Task<List<CEStage2ApplicationDto>> GetPendingApplicationsAsync(int? ceUserId = null)
        {
            try
            {
                _logger.LogInformation("[CEStage2Workflow] Fetching pending applications for CE final signature. CE User ID: {CeUserId}", ceUserId);
                _logger.LogInformation("[CEStage2Workflow] Querying for Status: CITY_ENGINEER_SIGN_PENDING (Enum Value: {StatusValue})", (int)ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING);

                var query = _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Where(a => a.Status == ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING);

                // Log ALL applications with this status before filtering by CE
                var allApplicationsWithStatus = await _context.PositionApplications
                    .Where(a => a.Status == ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING)
                    .Select(a => new { a.Id, a.ApplicationNumber, a.AssignedCEStage2Id, a.Status })
                    .ToListAsync();
                
                _logger.LogInformation("[CEStage2Workflow] Total applications with CITY_ENGINEER_SIGN_PENDING status: {Count}", allApplicationsWithStatus.Count);
                foreach (var app in allApplicationsWithStatus)
                {
                    _logger.LogInformation("[CEStage2Workflow] - App {Id} ({AppNumber}): Status={Status} (Value: {StatusValue}), AssignedCEStage2Id={CEId}", 
                        app.Id, app.ApplicationNumber, app.Status, (int)app.Status, app.AssignedCEStage2Id);
                }

                // If ceUserId is provided, filter by assigned CE (or unassigned applications)
                if (ceUserId.HasValue)
                {
                    _logger.LogInformation("[CEStage2Workflow] Filtering for CE User ID: {CeUserId} OR unassigned applications", ceUserId.Value);
                    query = query.Where(a => a.AssignedCEStage2Id == ceUserId.Value || a.AssignedCEStage2Id == null);
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

                if (application.Status != ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Application is not in CITY_ENGINEER_SIGN_PENDING status. Current status: {application.Status}"
                    };
                }

                // Verify CE is assigned to this application
                if (application.AssignedCEStage2Id != ceUserId)
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

                // Save certificate temporarily for signing
                var tempCertPath = Path.Combine(Path.GetTempPath(), $"LICENSE_CERT_{application.ApplicationNumber}_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempCertPath, licenseCertificate.FileContent);

                // Get CE signature coordinates from configuration for license certificate
                var ceCoordinates = _configuration["HSM:LicenseCertificateSignatureCoordinates:CityEngineer"] 
                    ?? "320,50,470,110,1";
                
                _logger.LogInformation($"[CEStage2Workflow] Using CE signature coordinates for license certificate: {ceCoordinates}");
                
                var otpResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    ceUserId,
                    SignatureType.CityEngineer,
                    tempCertPath,
                    ceCoordinates,
                    null,
                    null
                );
                
                if (!otpResult.Success)
                {
                    return new CEStage2OtpResult
                    {
                        Success = false,
                        Message = $"Failed to generate OTP: {otpResult.Message}"
                    };
                }

                _logger.LogInformation($"[CEStage2Workflow] OTP generated successfully for application {applicationId}");

                return new CEStage2OtpResult
                {
                    Success = true,
                    Message = "OTP generated successfully. Please check your registered mobile/email.",
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
        /// Apply CE final digital signature to license certificate
        /// Updates status from CITY_ENGINEER_SIGN_PENDING (34) to APPROVED (36)
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

                if (application.Status != ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Application is not in CITY_ENGINEER_SIGN_PENDING status. Current status: {application.Status}"
                    };
                }

                // Verify CE is assigned
                if (application.AssignedCEStage2Id != ceUserId)
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

                // Save certificate temporarily for signing
                var tempCertPath = Path.Combine(Path.GetTempPath(), $"LICENSE_CERT_{application.ApplicationNumber}_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempCertPath, licenseCertificate.FileContent);

                // Get CE signature coordinates from configuration for license certificate
                var ceCoordinates = _configuration["HSM:LicenseCertificateSignatureCoordinates:CityEngineer"] 
                    ?? "320,50,470,110,1";
                
                _logger.LogInformation($"[CEStage2Workflow] Using CE signature coordinates for license certificate: {ceCoordinates}");
                
                var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                    applicationId,
                    ceUserId,
                    SignatureType.CityEngineer,
                    tempCertPath,
                    ceCoordinates,
                    null,
                    null
                );

                if (!initiateResult.Success || !initiateResult.SignatureId.HasValue)
                {
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Failed to initiate signature: {initiateResult.Message}"
                    };
                }

                // Complete signature with OTP
                var completeResult = await _digitalSignatureService.CompleteSignatureAsync(
                    initiateResult.SignatureId.Value,
                    otpCode,
                    ceOfficer.Email
                );

                if (!completeResult.Success)
                {
                    _logger.LogError($"[CEStage2Workflow] CompleteSignature failed: {completeResult.Message}");
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Digital signature failed: {completeResult.Message}"
                    };
                }

                _logger.LogInformation($"[CEStage2Workflow] CompleteSignature SUCCESS. SignedDocumentPath: {completeResult.SignedDocumentPath}");

                // Update the license certificate in database with final signed version
                if (string.IsNullOrEmpty(completeResult.SignedDocumentPath))
                {
                    _logger.LogError($"[CEStage2Workflow] SignedDocumentPath is null or empty for application {applicationId}");
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "Signed document path is missing"
                    };
                }

                if (!File.Exists(completeResult.SignedDocumentPath))
                {
                    _logger.LogError($"[CEStage2Workflow] Signed document file does not exist at path: {completeResult.SignedDocumentPath}");
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = "Signed document file not found"
                    };
                }

                try
                {
                    var signedPdfBytes = await File.ReadAllBytesAsync(completeResult.SignedDocumentPath);
                    _logger.LogInformation($"[CEStage2Workflow] Read {signedPdfBytes.Length} bytes from signed PDF");
                    
                    licenseCertificate.FileContent = signedPdfBytes;
                    licenseCertificate.UpdatedDate = DateTime.UtcNow;
                    
                    _logger.LogInformation($"[CEStage2Workflow] Updated license certificate FileContent in memory ({signedPdfBytes.Length} bytes) for application {applicationId}");
                }
                catch (Exception certEx)
                {
                    _logger.LogError(certEx, $"[CEStage2Workflow] CRITICAL: Failed to read/update license certificate for application {applicationId}");
                    return new CEStage2SignResult
                    {
                        Success = false,
                        Message = $"Failed to update certificate: {certEx.Message}"
                    };
                }

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

