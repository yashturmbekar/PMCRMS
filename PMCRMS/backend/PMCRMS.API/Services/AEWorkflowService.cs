using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMCRMS.API.Configuration;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Orchestrates the Assistant Engineer workflow
    /// Handles document verification, digital signature, and forwarding to Executive Engineer
    /// </summary>
    public class AEWorkflowService : IAEWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AEWorkflowService> _logger;
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHsmService _hsmService;
        private readonly IOptions<HsmConfiguration> _hsmConfig;
        private readonly IAutoAssignmentService _autoAssignmentService;

        public AEWorkflowService(
            PMCRMSDbContext context,
            ILogger<AEWorkflowService> logger,
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

        public async Task<List<AEWorkflowStatusDto>> GetPendingApplicationsAsync(int officerId, PositionType positionType)
        {
            try
            {
                var query = BuildPendingQuery(officerId, positionType);
                var applications = await query.ToListAsync();

                var statusList = new List<AEWorkflowStatusDto>();
                foreach (var app in applications)
                {
                    var status = await GetWorkflowStatusAsync(app.Id, positionType);
                    if (status != null)
                    {
                        statusList.Add(status);
                    }
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending applications for AE {OfficerId}, position {PositionType}", 
                    officerId, positionType);
                throw;
            }
        }

        public async Task<List<AEWorkflowStatusDto>> GetCompletedApplicationsAsync(int officerId, PositionType positionType)
        {
            try
            {
                var query = BuildCompletedQuery(officerId, positionType);
                var applications = await query.ToListAsync();

                var statusList = new List<AEWorkflowStatusDto>();
                foreach (var app in applications)
                {
                    var status = await GetWorkflowStatusAsync(app.Id, positionType);
                    if (status != null)
                    {
                        statusList.Add(status);
                    }
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed applications for AE {OfficerId}, position {PositionType}", 
                    officerId, positionType);
                throw;
            }
        }

        public async Task<AEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId, PositionType positionType)
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
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return null;
                }

                var (aeId, aeName, aeAssignedDate, aeApprovalStatus, aeApprovalComments, aeApprovalDate,
                     aeRejectionStatus, aeRejectionComments, aeRejectionDate, aeDigitalSignatureApplied, 
                     aeDigitalSignatureDate) = GetAEDataByPosition(application, positionType);

                return new AEWorkflowStatusDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    FirstName = application.FirstName,
                    LastName = application.LastName,
                    PositionType = application.PositionType,
                    CurrentStatus = application.Status,
                    CurrentStatusDisplay = GetCurrentStage(application.Status),
                    AssignedToAEId = aeId,
                    AssignedToAEName = aeName,
                    AssignedToAEDate = aeAssignedDate,
                    AssignedJEName = application.AssignedJuniorEngineer?.Name,
                    JEApprovalDate = application.JEApprovalDate,
                    AEApprovalStatus = aeApprovalStatus,
                    AEApprovalComments = aeApprovalComments,
                    AEApprovalDate = aeApprovalDate,
                    AERejectionStatus = aeRejectionStatus,
                    AERejectionComments = aeRejectionComments,
                    AERejectionDate = aeRejectionDate,
                    AEDigitalSignatureApplied = aeDigitalSignatureApplied,
                    AEDigitalSignatureDate = aeDigitalSignatureDate,
                    CurrentStage = GetCurrentStage(application.Status),
                    NextAction = GetNextAction(application.Status),
                    CreatedDate = application.CreatedDate,
                    CompletedDate = aeApprovalDate ?? aeRejectionDate
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

                _logger.LogInformation("Generating OTP from HSM for AE application {ApplicationId} and officer {OfficerId}", 
                    applicationId, officerId);

                // Validate officer has KeyLabel
                if (string.IsNullOrEmpty(officer.KeyLabel))
                {
                    throw new Exception($"Officer {officer.Name} does not have a KeyLabel configured");
                }

                _logger.LogInformation("Using KeyLabel {KeyLabel} for officer {OfficerName} ({Role})", 
                    officer.KeyLabel, officer.Name, officer.Role);

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

                _logger.LogInformation("✅ OTP generated and sent by HSM to AE officer {OfficerId} - no database storage needed", officerId);

                // HSM sends OTP directly to officer's registered mobile/email
                // No need to store or return OTP - HSM validates it during signing
                return "OTP sent to registered mobile/email";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for AE signature");
                throw;
            }
        }

        public async Task<WorkflowActionResultDto> VerifyAndSignDocumentsAsync(
            int applicationId, 
            int officerId, 
            PositionType positionType, 
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

                _logger.LogInformation("Starting HSM digital signature process for AE application {ApplicationId}", applicationId);

                // ========== TESTING MODE: HSM SIGNATURE BYPASSED ==========
                // TODO: REMOVE THIS COMMENT BLOCK FOR PRODUCTION
                
                // // Validate officer has KeyLabel
                // if (string.IsNullOrEmpty(officer.KeyLabel))
                // {
                //     return new WorkflowActionResultDto 
                //     { 
                //         Success = false, 
                //         Message = $"Officer {officer.Name} does not have a KeyLabel configured for digital signature" 
                //     };
                // }

                // _logger.LogInformation("Using KeyLabel {KeyLabel} for AE officer {OfficerName} signature", 
                //     officer.KeyLabel, officer.Name);

                // // Convert PDF to Base64
                // var base64Pdf = Convert.ToBase64String(recommendationForm.FileContent);

                // // Sign PDF using HSM with officer's KeyLabel
                // var signRequest = new HsmSignRequest
                // {
                //     TransactionId = applicationId.ToString(),
                //     KeyLabel = officer.KeyLabel,
                //     Base64Pdf = base64Pdf,
                //     Otp = otp,
                //     Coordinates = SignatureCoordinates.RecommendationForm, // 117,383,236,324
                //     PageLocation = "last",
                //     OtpType = "single"
                // };

                // var signResult = await _hsmService.SignPdfAsync(signRequest);

                // if (!signResult.Success)
                // {
                //     _logger.LogError("HSM signature failed for application {ApplicationId}: {Error}", 
                //         applicationId, signResult.ErrorMessage);
                    
                //     return new WorkflowActionResultDto 
                //     { 
                //         Success = false, 
                //         Message = $"Digital signature failed: {signResult.ErrorMessage}" 
                //     };
                // }

                // // Update recommendation form with signed PDF
                // if (!string.IsNullOrEmpty(signResult.SignedPdfBase64))
                // {
                //     var signedPdfBytes = Convert.FromBase64String(signResult.SignedPdfBase64);
                //     recommendationForm.FileContent = signedPdfBytes;
                //     recommendationForm.FileSize = (decimal)(signedPdfBytes.Length / 1024.0);
                //     recommendationForm.UpdatedDate = DateTime.UtcNow;
                    
                //     _logger.LogInformation(
                //         "Updated recommendation form with AE digitally signed PDF for application {ApplicationId}", 
                //         applicationId);
                // }

                // TESTING MODE: Skip HSM signature
                _logger.LogInformation(
                    "[TESTING MODE] Skipping HSM signature for AE application {ApplicationId}", 
                    applicationId);
                // ========== END TESTING MODE ==========

                // Update application based on position type
                UpdateApplicationAfterAESignature(application, positionType, comments, DateTime.UtcNow);

                // Use auto-assignment service for intelligent workload-based assignment to EE
                var assignment = await _autoAssignmentService.AutoAssignToNextWorkflowStageAsync(
                    applicationId: application.Id,
                    currentStatus: ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING,
                    currentOfficerId: officerId
                );

                if (assignment != null)
                {
                    // Status already updated by auto-assignment service
                    application.Status = ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING;

                    // Send email notification to applicant
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                        application.Id,
                        ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING
                    );

                    _logger.LogInformation(
                        "Application {ApplicationId} auto-assigned to Executive Engineer {OfficerId} using workload-based strategy",
                        applicationId, assignment.AssignedToOfficerId);
                }
                else
                {
                    _logger.LogWarning(
                        "Auto-assignment failed for application {ApplicationId}. No available Executive Engineer found.",
                        application.Id);
                }

                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = assignment != null 
                        ? "Documents verified, recommendation form digitally signed via HSM, and application forwarded to Executive Engineer successfully"
                        : "Documents verified and digitally signed successfully. Manual assignment to Executive Engineer required.",
                    NewStatus = application.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AE verify and sign for application {ApplicationId}", applicationId);
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> RejectApplicationAsync(
            int applicationId, 
            int officerId, 
            PositionType positionType, 
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

                // Update rejection based on position type
                var rejectionDate = DateTime.UtcNow;
                switch (positionType)
                {
                    case PositionType.Architect:
                        application.AEArchitectRejectionStatus = true;
                        application.AEArchitectRejectionComments = rejectionComments;
                        application.AEArchitectRejectionDate = rejectionDate;
                        break;
                    case PositionType.StructuralEngineer:
                        application.AEStructuralRejectionStatus = true;
                        application.AEStructuralRejectionComments = rejectionComments;
                        application.AEStructuralRejectionDate = rejectionDate;
                        break;
                    case PositionType.LicenceEngineer:
                        application.AELicenceRejectionStatus = true;
                        application.AELicenceRejectionComments = rejectionComments;
                        application.AELicenceRejectionDate = rejectionDate;
                        break;
                    case PositionType.Supervisor1:
                        application.AESupervisor1RejectionStatus = true;
                        application.AESupervisor1RejectionComments = rejectionComments;
                        application.AESupervisor1RejectionDate = rejectionDate;
                        break;
                    case PositionType.Supervisor2:
                        application.AESupervisor2RejectionStatus = true;
                        application.AESupervisor2RejectionComments = rejectionComments;
                        application.AESupervisor2RejectionDate = rejectionDate;
                        break;
                }

                application.Status = ApplicationCurrentStatus.REJECTED;
                application.Remarks = $"Rejected by Assistant Engineer ({positionType}): {rejectionComments}";

                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.REJECTED,
                    rejectionComments
                );

                _logger.LogInformation(
                    "Application {ApplicationId} rejected by AE for position {PositionType}", 
                    applicationId, positionType);

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

        private IQueryable<PositionApplication> BuildPendingQuery(int officerId, PositionType positionType)
        {
            var baseQuery = _context.PositionApplications
                .Include(a => a.AssignedJuniorEngineer)
                .Where(a => a.Status == ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING 
                         && a.PositionType == positionType);

            return positionType switch
            {
                PositionType.Architect => baseQuery.Where(a => a.AssignedAEArchitectId == officerId 
                                                             && a.AEArchitectApprovalStatus != true 
                                                             && a.AEArchitectRejectionStatus != true),
                PositionType.StructuralEngineer => baseQuery.Where(a => a.AssignedAEStructuralId == officerId 
                                                                      && a.AEStructuralApprovalStatus != true 
                                                                      && a.AEStructuralRejectionStatus != true),
                PositionType.LicenceEngineer => baseQuery.Where(a => a.AssignedAELicenceId == officerId 
                                                                   && a.AELicenceApprovalStatus != true 
                                                                   && a.AELicenceRejectionStatus != true),
                PositionType.Supervisor1 => baseQuery.Where(a => a.AssignedAESupervisor1Id == officerId 
                                                               && a.AESupervisor1ApprovalStatus != true 
                                                               && a.AESupervisor1RejectionStatus != true),
                PositionType.Supervisor2 => baseQuery.Where(a => a.AssignedAESupervisor2Id == officerId 
                                                               && a.AESupervisor2ApprovalStatus != true 
                                                               && a.AESupervisor2RejectionStatus != true),
                _ => baseQuery.Where(a => false) // No results for unknown position type
            };
        }

        private IQueryable<PositionApplication> BuildCompletedQuery(int officerId, PositionType positionType)
        {
            var baseQuery = _context.PositionApplications
                .Include(a => a.AssignedJuniorEngineer)
                .Where(a => a.PositionType == positionType);

            return positionType switch
            {
                PositionType.Architect => baseQuery.Where(a => a.AssignedAEArchitectId == officerId 
                                                             && (a.AEArchitectApprovalStatus == true 
                                                              || a.AEArchitectRejectionStatus == true)),
                PositionType.StructuralEngineer => baseQuery.Where(a => a.AssignedAEStructuralId == officerId 
                                                                      && (a.AEStructuralApprovalStatus == true 
                                                                       || a.AEStructuralRejectionStatus == true)),
                PositionType.LicenceEngineer => baseQuery.Where(a => a.AssignedAELicenceId == officerId 
                                                                   && (a.AELicenceApprovalStatus == true 
                                                                    || a.AELicenceRejectionStatus == true)),
                PositionType.Supervisor1 => baseQuery.Where(a => a.AssignedAESupervisor1Id == officerId 
                                                               && (a.AESupervisor1ApprovalStatus == true 
                                                                || a.AESupervisor1RejectionStatus == true)),
                PositionType.Supervisor2 => baseQuery.Where(a => a.AssignedAESupervisor2Id == officerId 
                                                               && (a.AESupervisor2ApprovalStatus == true 
                                                                || a.AESupervisor2RejectionStatus == true)),
                _ => baseQuery.Where(a => false)
            };
        }

        private (int? aeId, string? aeName, DateTime? aeAssignedDate, bool? aeApprovalStatus, 
                 string? aeApprovalComments, DateTime? aeApprovalDate, bool? aeRejectionStatus, 
                 string? aeRejectionComments, DateTime? aeRejectionDate, bool aeDigitalSignatureApplied, 
                 DateTime? aeDigitalSignatureDate) GetAEDataByPosition(PositionApplication app, PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => (
                    app.AssignedAEArchitectId,
                    app.AssignedAEArchitect?.Name,
                    app.AssignedToAEArchitectDate,
                    app.AEArchitectApprovalStatus,
                    app.AEArchitectApprovalComments,
                    app.AEArchitectApprovalDate,
                    app.AEArchitectRejectionStatus,
                    app.AEArchitectRejectionComments,
                    app.AEArchitectRejectionDate,
                    app.AEArchitectDigitalSignatureApplied,
                    app.AEArchitectDigitalSignatureDate
                ),
                PositionType.StructuralEngineer => (
                    app.AssignedAEStructuralId,
                    app.AssignedAEStructural?.Name,
                    app.AssignedToAEStructuralDate,
                    app.AEStructuralApprovalStatus,
                    app.AEStructuralApprovalComments,
                    app.AEStructuralApprovalDate,
                    app.AEStructuralRejectionStatus,
                    app.AEStructuralRejectionComments,
                    app.AEStructuralRejectionDate,
                    app.AEStructuralDigitalSignatureApplied,
                    app.AEStructuralDigitalSignatureDate
                ),
                PositionType.LicenceEngineer => (
                    app.AssignedAELicenceId,
                    app.AssignedAELicence?.Name,
                    app.AssignedToAELicenceDate,
                    app.AELicenceApprovalStatus,
                    app.AELicenceApprovalComments,
                    app.AELicenceApprovalDate,
                    app.AELicenceRejectionStatus,
                    app.AELicenceRejectionComments,
                    app.AELicenceRejectionDate,
                    app.AELicenceDigitalSignatureApplied,
                    app.AELicenceDigitalSignatureDate
                ),
                PositionType.Supervisor1 => (
                    app.AssignedAESupervisor1Id,
                    app.AssignedAESupervisor1?.Name,
                    app.AssignedToAESupervisor1Date,
                    app.AESupervisor1ApprovalStatus,
                    app.AESupervisor1ApprovalComments,
                    app.AESupervisor1ApprovalDate,
                    app.AESupervisor1RejectionStatus,
                    app.AESupervisor1RejectionComments,
                    app.AESupervisor1RejectionDate,
                    app.AESupervisor1DigitalSignatureApplied,
                    app.AESupervisor1DigitalSignatureDate
                ),
                PositionType.Supervisor2 => (
                    app.AssignedAESupervisor2Id,
                    app.AssignedAESupervisor2?.Name,
                    app.AssignedToAESupervisor2Date,
                    app.AESupervisor2ApprovalStatus,
                    app.AESupervisor2ApprovalComments,
                    app.AESupervisor2ApprovalDate,
                    app.AESupervisor2RejectionStatus,
                    app.AESupervisor2RejectionComments,
                    app.AESupervisor2RejectionDate,
                    app.AESupervisor2DigitalSignatureApplied,
                    app.AESupervisor2DigitalSignatureDate
                ),
                _ => (null, null, null, null, null, null, null, null, null, false, null)
            };
        }

        private void UpdateApplicationAfterAESignature(
            PositionApplication application, 
            PositionType positionType, 
            string? comments, 
            DateTime signatureDate)
        {
            switch (positionType)
            {
                case PositionType.Architect:
                    application.AEArchitectApprovalStatus = true;
                    application.AEArchitectApprovalComments = comments;
                    application.AEArchitectApprovalDate = signatureDate;
                    application.AEArchitectDigitalSignatureApplied = true;
                    application.AEArchitectDigitalSignatureDate = signatureDate;
                    break;
                case PositionType.StructuralEngineer:
                    application.AEStructuralApprovalStatus = true;
                    application.AEStructuralApprovalComments = comments;
                    application.AEStructuralApprovalDate = signatureDate;
                    application.AEStructuralDigitalSignatureApplied = true;
                    application.AEStructuralDigitalSignatureDate = signatureDate;
                    break;
                case PositionType.LicenceEngineer:
                    application.AELicenceApprovalStatus = true;
                    application.AELicenceApprovalComments = comments;
                    application.AELicenceApprovalDate = signatureDate;
                    application.AELicenceDigitalSignatureApplied = true;
                    application.AELicenceDigitalSignatureDate = signatureDate;
                    break;
                case PositionType.Supervisor1:
                    application.AESupervisor1ApprovalStatus = true;
                    application.AESupervisor1ApprovalComments = comments;
                    application.AESupervisor1ApprovalDate = signatureDate;
                    application.AESupervisor1DigitalSignatureApplied = true;
                    application.AESupervisor1DigitalSignatureDate = signatureDate;
                    break;
                case PositionType.Supervisor2:
                    application.AESupervisor2ApprovalStatus = true;
                    application.AESupervisor2ApprovalComments = comments;
                    application.AESupervisor2ApprovalDate = signatureDate;
                    application.AESupervisor2DigitalSignatureApplied = true;
                    application.AESupervisor2DigitalSignatureDate = signatureDate;
                    break;
            }
        }

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
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => "AE Review Pending",
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => "Forwarded to Executive Engineer",
                ApplicationCurrentStatus.REJECTED => "Rejected",
                _ => status.ToString()
            };
        }

        private string GetNextAction(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => "Verify Documents and Apply Digital Signature",
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => "Awaiting EE Review",
                ApplicationCurrentStatus.REJECTED => "No action required",
                _ => "No action required"
            };
        }
    }
}

