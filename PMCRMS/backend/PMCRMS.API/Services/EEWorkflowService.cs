using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMCRMS.API.Configuration;
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
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHsmService _hsmService;
        private readonly IOptions<HsmConfiguration> _hsmConfig;
        private readonly IAutoAssignmentService _autoAssignmentService;

        public EEWorkflowService(
            PMCRMSDbContext context,
            ILogger<EEWorkflowService> logger,
            IDigitalSignatureService digitalSignatureService,
            INotificationService notificationService,
            IWorkflowNotificationService workflowNotificationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHsmService hsmService,
            IOptions<HsmConfiguration> hsmConfig,
            IAutoAssignmentService autoAssignmentService)
        {
            _context = context;
            _logger = logger;
            _digitalSignatureService = digitalSignatureService;
            _notificationService = notificationService;
            _workflowNotificationService = workflowNotificationService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _hsmService = hsmService;
            _hsmConfig = hsmConfig;
            _autoAssignmentService = autoAssignmentService;
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

                // Get application to determine position type
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    throw new Exception("Application not found");
                }

                // Validate officer has KeyLabel
                if (string.IsNullOrEmpty(officer.KeyLabel))
                {
                    throw new Exception($"Officer {officer.Name} does not have a KeyLabel configured");
                }

                _logger.LogInformation(
                    "Generating OTP from HSM for EE officer {OfficerId} ({OfficerName}) with KeyLabel {KeyLabel}",
                    officerId, officer.Name, officer.KeyLabel);

                // Call HSM OTP service with officer's KeyLabel
                var hsmResult = await _hsmService.GenerateOtpAsync(
                    transactionId: applicationId.ToString(),
                    keyLabel: officer.KeyLabel,
                    otpType: "single"
                );

                if (!hsmResult.Success)
                {
                    _logger.LogError("HSM OTP generation failed: {Error}", hsmResult.ErrorMessage);
                    throw new Exception($"Failed to generate OTP: {hsmResult.ErrorMessage}");
                }

                _logger.LogInformation("✅ OTP generated and sent by HSM to EE officer {OfficerId} - no database storage needed", officerId);

                // HSM sends OTP directly to officer's registered mobile/email
                // No need to store or return OTP - HSM validates it during signing
                return "OTP sent to registered mobile/email";
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

                // Get HSM key label for EE
                var keyLabel = _hsmConfig.Value.KeyLabels.GetKeyLabel("EE", application.PositionType.ToString());
                
                if (string.IsNullOrEmpty(keyLabel))
                {
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = "HSM key label not configured for EE officer" 
                    };
                }

                // Convert PDF to Base64 for HSM signing
                var base64Pdf = Convert.ToBase64String(recommendationForm.FileContent);

                // Sign PDF with HSM
                var signRequest = new HsmSignRequest
                {
                    TransactionId = applicationId.ToString(),
                    KeyLabel = keyLabel,
                    Base64Pdf = base64Pdf,
                    Otp = otp,
                    Coordinates = SignatureCoordinates.RecommendationForm
                };

                var signResult = await _hsmService.SignPdfAsync(signRequest);

                if (!signResult.Success)
                {
                    _logger.LogError("HSM PDF signing failed: {Error}", signResult.ErrorMessage);
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = $"Digital signature failed: {signResult.ErrorMessage}" 
                    };
                }

                if (string.IsNullOrEmpty(signResult.SignedPdfBase64))
                {
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = "Signed PDF content is empty" 
                    };
                }

                // Update recommendation form with signed PDF
                var signedPdfBytes = Convert.FromBase64String(signResult.SignedPdfBase64);
                recommendationForm.FileContent = signedPdfBytes;
                recommendationForm.FileSize = (decimal)(signedPdfBytes.Length / 1024.0);
                recommendationForm.UpdatedDate = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Updated recommendation form with EE digitally signed PDF via HSM for application {ApplicationId}", 
                    applicationId);

                // Update application
                var signatureDate = DateTime.UtcNow;
                application.ExecutiveEngineerApprovalStatus = true;
                application.ExecutiveEngineerApprovalComments = comments;
                application.ExecutiveEngineerApprovalDate = signatureDate;
                application.ExecutiveEngineerDigitalSignatureApplied = true;
                application.ExecutiveEngineerDigitalSignatureDate = signatureDate;

                // Use auto-assignment service for intelligent workload-based assignment to CE
                var assignment = await _autoAssignmentService.AutoAssignToNextWorkflowStageAsync(
                    applicationId: application.Id,
                    currentStatus: ApplicationCurrentStatus.CITY_ENGINEER_PENDING,
                    currentOfficerId: officerId
                );

                if (assignment != null)
                {
                    // Status already updated by auto-assignment service
                    application.Status = ApplicationCurrentStatus.CITY_ENGINEER_PENDING;

                    // Send email notification to applicant
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                        application.Id,
                        ApplicationCurrentStatus.CITY_ENGINEER_PENDING
                    );

                    _logger.LogInformation(
                        "Application {ApplicationId} auto-assigned to City Engineer {OfficerId} using workload-based strategy",
                        applicationId, assignment.AssignedToOfficerId);
                }
                else
                {
                    _logger.LogWarning(
                        "Auto-assignment failed for application {ApplicationId}. No available City Engineer found.",
                        application.Id);
                }

                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = assignment != null 
                        ? "Documents verified, recommendation form digitally signed via HSM, and application forwarded to City Engineer successfully"
                        : "Documents verified and digitally signed successfully. Manual assignment to City Engineer required.",
                    NewStatus = application.Status
                };
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

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.REJECTED,
                    rejectionComments
                );

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
