using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMCRMS.API.Configuration;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Orchestrates the City Engineer workflow (Final Approval)
    /// Handles document verification, digital signature, and final approval/rejection
    /// </summary>
    public class CEWorkflowService : ICEWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<CEWorkflowService> _logger;
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHsmService _hsmService;
        private readonly ISignatureWorkflowService _signatureWorkflowService;
        private readonly IAutoAssignmentService _autoAssignmentService;

        public CEWorkflowService(
            PMCRMSDbContext context,
            ILogger<CEWorkflowService> logger,
            IDigitalSignatureService digitalSignatureService,
            INotificationService notificationService,
            IWorkflowNotificationService workflowNotificationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHsmService hsmService,
            ISignatureWorkflowService signatureWorkflowService,
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
            _signatureWorkflowService = signatureWorkflowService;
            _autoAssignmentService = autoAssignmentService;
        }

        public async Task<List<CEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId)
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
                    .Include(a => a.AssignedCityEngineer)
                    .Where(a => a.Status == ApplicationCurrentStatus.CITY_ENGINEER_PENDING 
                             && a.AssignedCityEngineerId == officerId
                             && a.CityEngineerApprovalStatus != true
                             && a.CityEngineerRejectionStatus != true)
                    .ToListAsync();

                var statusList = new List<CEWorkflowStatusDto>();
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
                _logger.LogError(ex, "Error getting pending applications for CE {OfficerId}", officerId);
                throw;
            }
        }

        public async Task<List<CEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId)
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
                    .Include(a => a.AssignedCityEngineer)
                    .Where(a => a.AssignedCityEngineerId == officerId
                             && (a.CityEngineerApprovalStatus == true
                              || a.CityEngineerRejectionStatus == true))
                    .ToListAsync();

                var statusList = new List<CEWorkflowStatusDto>();
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
                _logger.LogError(ex, "Error getting completed applications for CE {OfficerId}", officerId);
                throw;
            }
        }

        public async Task<CEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId)
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
                    .Include(a => a.AssignedCityEngineer)
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

                return new CEWorkflowStatusDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    FirstName = application.FirstName,
                    LastName = application.LastName,
                    PositionType = application.PositionType,
                    CurrentStatus = application.Status,
                    CurrentStatusDisplay = GetCurrentStage(application.Status),
                    AssignedToCEId = application.AssignedCityEngineerId,
                    AssignedToCEName = application.AssignedCityEngineer?.Name,
                    AssignedToCEDate = application.AssignedToCityEngineerDate,
                    AssignedJEName = application.AssignedJuniorEngineer?.Name,
                    AssignedAEName = aeName,
                    AssignedEEName = application.AssignedExecutiveEngineer?.Name,
                    CEApprovalStatus = application.CityEngineerApprovalStatus,
                    CEApprovalComments = application.CityEngineerApprovalComments,
                    CEApprovalDate = application.CityEngineerApprovalDate,
                    CERejectionStatus = application.CityEngineerRejectionStatus,
                    CERejectionComments = application.CityEngineerRejectionComments,
                    CERejectionDate = application.CityEngineerRejectionDate,
                    CEDigitalSignatureApplied = application.CityEngineerDigitalSignatureApplied,
                    CEDigitalSignatureDate = application.CityEngineerDigitalSignatureDate,
                    CurrentStage = GetCurrentStage(application.Status),
                    NextAction = GetNextAction(application.Status),
                    CreatedDate = application.CreatedDate,
                    CompletedDate = application.CityEngineerApprovalDate ?? application.CityEngineerRejectionDate
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

                // Get KeyLabel from configuration based on officer role
                var keyLabel = _configuration[$"HSM:KeyLabels:CityEngineer"];

                if (string.IsNullOrEmpty(keyLabel))
                {
                    throw new Exception($"KeyLabel not configured for CityEngineer role");
                }

                _logger.LogInformation(
                    "Generating OTP from HSM for CE officer {OfficerId} ({OfficerName}) with KeyLabel {KeyLabel}",
                    officerId, officer.Name, keyLabel);

                // Call HSM OTP service with KeyLabel
                var hsmResult = await _hsmService.GenerateOtpAsync(
                    transactionId: applicationId.ToString(),
                    keyLabel: keyLabel,
                    otpType: "single"
                );

                if (!hsmResult.Success)
                {
                    _logger.LogError("HSM OTP generation failed: {Error}", hsmResult.ErrorMessage);
                    throw new Exception($"Failed to generate OTP: {hsmResult.ErrorMessage}");
                }

                _logger.LogInformation("✅ OTP generated and sent by HSM to CE officer {OfficerId} - no database storage needed", officerId);

                // Return the actual HSM success message (e.g., "Message successfully sent to XXXXXX4115")
                // HSM sends OTP directly to officer's registered mobile/email
                // No need to store or return OTP - HSM validates it during signing
                return hsmResult.Message ?? "OTP sent to registered mobile number";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for CE signature");
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

                // Use unified signature workflow service
                _logger.LogInformation("Starting digital signature process for CE application {ApplicationId}", applicationId);
                
                var signatureResult = await _signatureWorkflowService.SignAsCityEngineerAsync(
                    applicationId,
                    officerId,
                    otp
                );

                if (!signatureResult.Success)
                {
                    _logger.LogError("Digital signature failed for application {ApplicationId}: {Error}", 
                        applicationId, signatureResult.ErrorMessage);
                    
                    return new WorkflowActionResultDto 
                    { 
                        Success = false, 
                        Message = signatureResult.ErrorMessage ?? "Digital signature failed" 
                    };
                }

                _logger.LogInformation(
                    "CE digitally signed PDF for application {ApplicationId}", 
                    applicationId);

                // Update application - CE Stage 1 Approval
                var signatureDate = DateTime.UtcNow;
                application.CityEngineerApprovalStatus = true;
                application.CityEngineerApprovalComments = comments;
                application.CityEngineerApprovalDate = signatureDate;
                application.CityEngineerDigitalSignatureApplied = true;
                application.CityEngineerDigitalSignatureDate = signatureDate;
                
                // Check if position is Architect - NO payment required for Architects
                if (application.PositionType == PositionType.Architect)
                {
                    _logger.LogInformation(
                        "Application {ApplicationId} is for Architect position - Skipping payment, directly assigning to Clerk", 
                        applicationId);
                    
                    // Set status to CLERK_PENDING and auto-assign to clerk
                    application.Status = ApplicationCurrentStatus.CLERK_PENDING;
                    application.Remarks = $"Approved by City Engineer on {signatureDate:yyyy-MM-dd HH:mm:ss}. No payment required for Architect position. Assigned to Clerk for certificate processing.";
                    
                    // Auto-assign to an active clerk
                    var clerk = await _context.Officers
                        .Where(o => o.Role == OfficerRole.Clerk && o.IsActive)
                        .OrderBy(o => Guid.NewGuid()) // Random assignment
                        .FirstOrDefaultAsync();
                    
                    if (clerk != null)
                    {
                        application.AssignedClerkId = clerk.Id;
                        application.AssignedToClerkDate = DateTime.UtcNow;
                        _logger.LogInformation(
                            "Application {ApplicationId} (Architect) assigned to Clerk {ClerkId} - {ClerkName}", 
                            applicationId, clerk.Id, clerk.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No active clerk found for Architect application {ApplicationId} - Status set to CLERK_PENDING but not assigned", 
                            applicationId);
                    }
                    
                    await _context.SaveChangesAsync();

                    // Send email notification to applicant about clerk assignment (no payment needed)
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                        application.Id,
                        ApplicationCurrentStatus.CLERK_PENDING
                    );

                    _logger.LogInformation(
                        "Application {ApplicationId} (Architect) approved by City Engineer - Direct to Clerk (no payment) for officer {OfficerId}", 
                        applicationId, officerId);

                    return new WorkflowActionResultDto
                    {
                        Success = true,
                        Message = "Documents verified and approved by City Engineer. No payment required for Architect position. Application forwarded to Clerk.",
                        NewStatus = application.Status
                    };
                }
                else
                {
                    // For non-Architect positions, route to Payment
                    application.Status = ApplicationCurrentStatus.PaymentPending;
                    application.Remarks = $"Approved by City Engineer on {signatureDate:yyyy-MM-dd HH:mm:ss}. Payment required to proceed to Clerk for certificate generation.";
                    
                    await _context.SaveChangesAsync();

                    // Send email notification to applicant about payment requirement
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                        application.Id,
                        ApplicationCurrentStatus.PaymentPending
                    );

                    _logger.LogInformation(
                        "Application {ApplicationId} approved by City Engineer - Routed to Payment for officer {OfficerId}. User must complete payment before Clerk assignment.", 
                        applicationId, officerId);

                    return new WorkflowActionResultDto
                    {
                        Success = true,
                        Message = "Documents verified and approved by City Engineer. Please complete payment to proceed.",
                        NewStatus = application.Status
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CE verify and sign for application {ApplicationId}", applicationId);
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
                    .Include(a => a.Appointments)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Store rejection information
                var rejectionInfo = $"Rejected by City Engineer on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {rejectionComments}";
                application.Remarks = rejectionInfo;

                // Set status to REJECTED
                application.Status = ApplicationCurrentStatus.REJECTED;
                
                // Clear all officer assignments
                application.AssignedJuniorEngineerId = null;
                application.AssignedToJEDate = null;
                application.AssignedExecutiveEngineerId = null;
                application.AssignedToExecutiveEngineerDate = null;
                application.AssignedCityEngineerId = null;
                application.AssignedToCityEngineerDate = null;
                application.AssignedAEArchitectId = null;
                application.AssignedToAEArchitectDate = null;
                application.AssignedAEStructuralId = null;
                application.AssignedToAEStructuralDate = null;
                application.AssignedAELicenceId = null;
                application.AssignedToAELicenceDate = null;
                application.AssignedAESupervisor1Id = null;
                application.AssignedToAESupervisor1Date = null;
                application.AssignedAESupervisor2Id = null;
                application.AssignedToAESupervisor2Date = null;
                application.AssignedEEStage2Id = null;
                application.AssignedToEEStage2Date = null;
                application.AssignedCEStage2Id = null;
                application.AssignedToCEStage2Date = null;
                application.AssignedClerkId = null;
                application.AssignedToClerkDate = null;

                // Clear JE workflow fields
                application.JEDigitalSignatureApplied = false;
                application.JEDigitalSignatureDate = null;
                application.JERejectionStatus = null;
                application.JERejectionComments = null;
                application.JERejectionDate = null;

                // Clear recommendation form
                application.IsRecommendationFormGenerated = false;
                application.RecommendationFormGeneratedDate = null;
                application.RecommendationFormGenerationAttempts = 0;
                application.RecommendationFormGenerationError = null;

                // Clear all AE workflow fields
                application.AEArchitectApprovalStatus = null;
                application.AEArchitectRejectionStatus = null;
                application.AEArchitectRejectionComments = null;
                application.AEArchitectRejectionDate = null;
                application.AEStructuralApprovalStatus = null;
                application.AEStructuralRejectionStatus = null;
                application.AEStructuralRejectionComments = null;
                application.AEStructuralRejectionDate = null;
                application.AELicenceApprovalStatus = null;
                application.AELicenceRejectionStatus = null;
                application.AELicenceRejectionComments = null;
                application.AELicenceRejectionDate = null;
                application.AELicenceDigitalSignatureApplied = false;
                application.AELicenceDigitalSignatureDate = null;
                application.AESupervisor1ApprovalStatus = null;
                application.AESupervisor1RejectionStatus = null;
                application.AESupervisor1RejectionComments = null;
                application.AESupervisor1RejectionDate = null;
                application.AESupervisor2ApprovalStatus = null;
                application.AESupervisor2RejectionStatus = null;
                application.AESupervisor2RejectionComments = null;
                application.AESupervisor2RejectionDate = null;

                // Clear EE workflow fields
                application.ExecutiveEngineerRejectionStatus = null;
                application.ExecutiveEngineerRejectionComments = null;
                application.ExecutiveEngineerRejectionDate = null;

                // Clear CE workflow fields
                application.CityEngineerRejectionStatus = null;
                application.CityEngineerRejectionComments = null;
                application.CityEngineerRejectionDate = null;

                // Clear clerk approval
                application.ClerkApprovalStatus = null;
                application.ClerkApprovalComments = null;
                application.ClerkApprovalDate = null;

                // Delete all appointments
                if (application.Appointments != null && application.Appointments.Any())
                {
                    _context.Appointments.RemoveRange(application.Appointments);
                }

                // Delete recommendation form document
                var recommendationDoc = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                            d.DocumentType == SEDocumentType.RecommendedForm);
                if (recommendationDoc != null)
                {
                    _context.SEDocuments.Remove(recommendationDoc);
                }

                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING,
                    rejectionInfo
                );

                _logger.LogInformation("Application {ApplicationId} rejected by CE", applicationId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Application rejected. The applicant can edit and resubmit the application.",
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
                ApplicationCurrentStatus.CITY_ENGINEER_PENDING => "CE Final Review Pending",
                ApplicationCurrentStatus.PaymentPending => "PAYMENT_PENDING",
                ApplicationCurrentStatus.PaymentCompleted => "PAYMENT_COMPLETED",
                ApplicationCurrentStatus.APPROVED => "APPROVED",
                ApplicationCurrentStatus.REJECTED => "REJECTED",
                _ => status.ToString()
            };
        }

        private string GetNextAction(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.CITY_ENGINEER_PENDING => "Verify Documents and Apply Digital Signature (Final Approval)",
                ApplicationCurrentStatus.APPROVED => "Certificate can be issued",
                ApplicationCurrentStatus.REJECTED => "No action required",
                _ => "No action required"
            };
        }
    }
}
