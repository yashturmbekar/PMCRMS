using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Orchestrates the Executive Engineer workflow
    /// Handles document verification, digital signature, and forwarding to City Engineer
    /// </summary>
    public class EEWorkflowService : IEEWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<EEWorkflowService> _logger;
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly INotificationService _notificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public EEWorkflowService(
            PMCRMSDbContext context,
            ILogger<EEWorkflowService> logger,
            IDigitalSignatureService digitalSignatureService,
            INotificationService notificationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _digitalSignatureService = digitalSignatureService;
            _notificationService = notificationService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<List<EEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId)
        {
            try
            {
                var applications = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .Include(a => a.AssignedAEArchitect)
                    .Include(a => a.AssignedAEStructural)
                    .Include(a => a.AssignedAELicence)
                    .Include(a => a.AssignedAESupervisor1)
                    .Include(a => a.AssignedAESupervisor2)
                    .Include(a => a.AssignedExecutiveEngineer)
                    .Where(a => a.Status == ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING 
                             && a.AssignedExecutiveEngineerId == officerId
                             && a.ExecutiveEngineerApprovalStatus != true
                             && a.ExecutiveEngineerRejectionStatus != true)
                    .ToListAsync();

                var statusList = new List<EEWorkflowStatusDto>();
                foreach (var app in applications)
                {
                    var status = await GetWorkflowStatusAsync(app.Id);
                    if (status != null)
                    {
                        statusList.Add(status);
                    }
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending applications for EE {OfficerId}", officerId);
                throw;
            }
        }

        public async Task<List<EEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId)
        {
            try
            {
                var applications = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .Include(a => a.AssignedAEArchitect)
                    .Include(a => a.AssignedAEStructural)
                    .Include(a => a.AssignedAELicence)
                    .Include(a => a.AssignedAESupervisor1)
                    .Include(a => a.AssignedAESupervisor2)
                    .Include(a => a.AssignedExecutiveEngineer)
                    .Where(a => a.AssignedExecutiveEngineerId == officerId
                             && (a.ExecutiveEngineerApprovalStatus == true
                              || a.ExecutiveEngineerRejectionStatus == true))
                    .ToListAsync();

                var statusList = new List<EEWorkflowStatusDto>();
                foreach (var app in applications)
                {
                    var status = await GetWorkflowStatusAsync(app.Id);
                    if (status != null)
                    {
                        statusList.Add(status);
                    }
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed applications for EE {OfficerId}", officerId);
                throw;
            }
        }

        public async Task<EEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .Include(a => a.AssignedAEArchitect)
                    .Include(a => a.AssignedAEStructural)
                    .Include(a => a.AssignedAELicence)
                    .Include(a => a.AssignedAESupervisor1)
                    .Include(a => a.AssignedAESupervisor2)
                    .Include(a => a.AssignedExecutiveEngineer)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return null;
                }

                // Get AE name based on position type
                var aeName = application.PositionType switch
                {
                    PositionType.Architect => application.AssignedAEArchitect?.Name,
                    PositionType.StructuralEngineer => application.AssignedAEStructural?.Name,
                    PositionType.LicenceEngineer => application.AssignedAELicence?.Name,
                    PositionType.Supervisor1 => application.AssignedAESupervisor1?.Name,
                    PositionType.Supervisor2 => application.AssignedAESupervisor2?.Name,
                    _ => null
                };

                return new EEWorkflowStatusDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    FirstName = application.FirstName,
                    LastName = application.LastName,
                    PositionType = application.PositionType,
                    CurrentStatus = application.Status,
                    CurrentStatusDisplay = GetCurrentStage(application.Status),
                    AssignedToEEId = application.AssignedExecutiveEngineerId,
                    AssignedToEEName = application.AssignedExecutiveEngineer?.Name,
                    AssignedToEEDate = application.AssignedToExecutiveEngineerDate,
                    AssignedJEName = application.AssignedJuniorEngineer?.Name,
                    AssignedAEName = aeName,
                    EEApprovalStatus = application.ExecutiveEngineerApprovalStatus,
                    EEApprovalComments = application.ExecutiveEngineerApprovalComments,
                    EEApprovalDate = application.ExecutiveEngineerApprovalDate,
                    EERejectionStatus = application.ExecutiveEngineerRejectionStatus,
                    EERejectionComments = application.ExecutiveEngineerRejectionComments,
                    EERejectionDate = application.ExecutiveEngineerRejectionDate,
                    EEDigitalSignatureApplied = application.ExecutiveEngineerDigitalSignatureApplied,
                    EEDigitalSignatureDate = application.ExecutiveEngineerDigitalSignatureDate,
                    CurrentStage = GetCurrentStage(application.Status),
                    NextAction = GetNextAction(application.Status),
                    CreatedDate = application.CreatedDate,
                    CompletedDate = application.ExecutiveEngineerApprovalDate ?? application.ExecutiveEngineerRejectionDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow status for application {ApplicationId}", applicationId);
                return null;
            }
        }

        public async Task<string> GenerateOtpForSignatureAsync(int applicationId, int officerId)
        {
            try
            {
                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null)
                {
                    throw new Exception("Officer not found");
                }

                _logger.LogInformation("Generating OTP from HSM for EE application {ApplicationId} and officer {OfficerId}", 
                    applicationId, officerId);

                var otp = await CallHsmOtpServiceAsync(officer.Email, officer.PhoneNumber);
                _logger.LogInformation("OTP generated successfully from HSM for EE officer {OfficerId}", officerId);

                // Store OTP in database for validation
                var otpVerification = new OtpVerification
                {
                    Identifier = officer.Email,
                    OtpCode = otp,
                    Purpose = "DIGITAL_SIGNATURE_EE",
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    IsActive = true,
                    CreatedBy = officerId.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                return otp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for EE signature");
                throw;
            }
        }

        public async Task<WorkflowActionResultDto> VerifyAndSignDocumentsAsync(
            int applicationId, 
            int officerId, 
            string otp, 
            string? comments = null)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Officer not found" };
                }

                // Validate OTP
                var otpVerification = await _context.OtpVerifications
                    .Where(o => o.Identifier == officer.Email 
                             && o.OtpCode == otp 
                             && (o.Purpose == "DIGITAL_SIGNATURE_EE" || o.Purpose == "DIGITAL_SIGNATURE")
                             && !o.IsUsed
                             && o.IsActive
                             && o.ExpiryTime > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedDate)
                    .FirstOrDefaultAsync();

                if (otpVerification == null)
                {
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = "Invalid or expired OTP. Please generate a new OTP." 
                    };
                }

                // Mark OTP as used
                otpVerification.IsUsed = true;
                otpVerification.VerifiedAt = DateTime.UtcNow;

                // Get recommendation form PDF
                var recommendationForm = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == applicationId 
                                            && d.DocumentType == SEDocumentType.RecommendedForm);

                if (recommendationForm == null || recommendationForm.FileContent == null)
                {
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = "Recommendation form not found." 
                    };
                }

                // Save PDF temporarily for HSM signing
                var tempPdfPath = Path.Combine(Path.GetTempPath(), $"recommendation_ee_{applicationId}.pdf");
                await File.WriteAllBytesAsync(tempPdfPath, recommendationForm.FileContent);

                try
                {
                    // Initiate digital signature with HSM
                    var coordinates = $"{100},{600},{200},{150},{1}"; // Different position than JE and AE
                    var ipAddress = "";
                    var userAgent = "PMCRMS_API_EE";

                    var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                        applicationId: applicationId,
                        signedByOfficerId: officerId,
                        signatureType: SignatureType.ExecutiveEngineer,
                        documentPath: tempPdfPath,
                        coordinates: coordinates,
                        ipAddress: ipAddress,
                        userAgent: userAgent
                    );

                    if (!initiateResult.Success || initiateResult.SignatureId == null)
                    {
                        return new WorkflowActionResultDto 
                        { 
                            Success = false, 
                            Message = $"Failed to initiate digital signature: {initiateResult.Message}" 
                        };
                    }

                    // Complete signature with OTP
                    var completeResult = await _digitalSignatureService.CompleteSignatureAsync(
                        signatureId: initiateResult.SignatureId.Value,
                        otp: otp,
                        completedBy: officer.Email
                    );

                    if (!completeResult.Success)
                    {
                        return new WorkflowActionResultDto 
                        { 
                            Success = false, 
                            Message = $"Digital signature failed: {completeResult.Message}" 
                        };
                    }

                    // Update recommendation form with signed PDF
                    if (!string.IsNullOrEmpty(completeResult.SignedDocumentPath) && 
                        File.Exists(completeResult.SignedDocumentPath))
                    {
                        var signedPdfBytes = await File.ReadAllBytesAsync(completeResult.SignedDocumentPath);
                        recommendationForm.FileContent = signedPdfBytes;
                        recommendationForm.FileSize = (decimal)(signedPdfBytes.Length / 1024.0);
                        recommendationForm.UpdatedDate = DateTime.UtcNow;
                        
                        _logger.LogInformation(
                            "Updated recommendation form with EE digitally signed PDF for application {ApplicationId}", 
                            applicationId);
                    }

                    // Update application
                    var signatureDate = DateTime.UtcNow;
                    application.ExecutiveEngineerApprovalStatus = true;
                    application.ExecutiveEngineerApprovalComments = comments;
                    application.ExecutiveEngineerApprovalDate = signatureDate;
                    application.ExecutiveEngineerDigitalSignatureApplied = true;
                    application.ExecutiveEngineerDigitalSignatureDate = signatureDate;

                    // Forward to City Engineer
                    var cityEngineer = await _context.Officers
                        .Where(o => o.Role == OfficerRole.CityEngineer && o.IsActive)
                        .OrderBy(o => o.Id)
                        .FirstOrDefaultAsync();

                    if (cityEngineer != null)
                    {
                        application.AssignedCityEngineerId = cityEngineer.Id;
                        application.AssignedToCityEngineerDate = DateTime.UtcNow;
                        application.Status = ApplicationCurrentStatus.CITY_ENGINEER_PENDING;

                        // Send notification to CE
                        await _notificationService.NotifyOfficerAssignmentAsync(
                            cityEngineer.Id,
                            application.ApplicationNumber ?? "N/A",
                            application.Id,
                            application.PositionType.ToString(),
                            $"{application.FirstName} {application.LastName}",
                            officerId.ToString());

                        _logger.LogInformation(
                            "Application {ApplicationId} forwarded to City Engineer {OfficerId}", 
                            applicationId, cityEngineer.Id);
                    }

                    await _context.SaveChangesAsync();

                    return new WorkflowActionResultDto
                    {
                        Success = true,
                        Message = "Documents verified, recommendation form digitally signed, and application forwarded to City Engineer successfully",
                        NewStatus = application.Status
                    };
                }
                finally
                {
                    if (File.Exists(tempPdfPath))
                    {
                        File.Delete(tempPdfPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EE verify and sign for application {ApplicationId}", applicationId);
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> RejectApplicationAsync(
            int applicationId, 
            int officerId, 
            string rejectionComments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionComments))
                {
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = "Rejection comments are mandatory" 
                    };
                }

                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                var rejectionDate = DateTime.UtcNow;
                application.ExecutiveEngineerRejectionStatus = true;
                application.ExecutiveEngineerRejectionComments = rejectionComments;
                application.ExecutiveEngineerRejectionDate = rejectionDate;
                application.Status = ApplicationCurrentStatus.REJECTED;
                application.Remarks = $"Rejected by Executive Engineer: {rejectionComments}";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} rejected by EE", applicationId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Application rejected successfully",
                    NewStatus = application.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", applicationId);
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        // Helper Methods

        private async Task<string> CallHsmOtpServiceAsync(string email, string? phoneNumber)
        {
            var httpClient = _httpClientFactory.CreateClient("HSM_OTP");
            var otpBaseUrl = _configuration["HSM:OtpBaseUrl"];

            _logger.LogInformation("Calling HSM OTP service at {OtpBaseUrl} for email: {Email}", otpBaseUrl, email);

            try
            {
                var requestData = new
                {
                    email = email,
                    mobile = phoneNumber ?? "",
                    purpose = "DIGITAL_SIGNATURE",
                    otpLength = 6,
                    validity = 5
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("HSM OTP Request: {Request}", jsonContent);

                var response = await httpClient.PostAsync("", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("HSM OTP Response Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HSM OTP service failed with status {response.StatusCode}: {responseContent}");
                }

                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                string otp;
                if (root.TryGetProperty("otp", out var otpElement))
                {
                    otp = otpElement.GetString() ?? throw new Exception("OTP is null in response");
                }
                else if (root.TryGetProperty("code", out var codeElement))
                {
                    otp = codeElement.GetString() ?? throw new Exception("Code is null in response");
                }
                else if (root.TryGetProperty("otpCode", out var otpCodeElement))
                {
                    otp = otpCodeElement.GetString() ?? throw new Exception("OtpCode is null in response");
                }
                else
                {
                    _logger.LogError("Unexpected HSM OTP response format: {Response}", responseContent);
                    throw new Exception("Could not extract OTP from HSM response");
                }

                _logger.LogInformation("Successfully extracted OTP from HSM response");
                return otp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling HSM OTP service");
                throw;
            }
        }

        private string GetCurrentStage(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => "EE Review Pending",
                ApplicationCurrentStatus.CITY_ENGINEER_PENDING => "Forwarded to City Engineer",
                ApplicationCurrentStatus.REJECTED => "Rejected",
                _ => status.ToString()
            };
        }

        private string GetNextAction(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => "Verify Documents and Apply Digital Signature",
                ApplicationCurrentStatus.CITY_ENGINEER_PENDING => "Awaiting CE Review",
                ApplicationCurrentStatus.REJECTED => "No action required",
                _ => "No action required"
            };
        }
    }
}
