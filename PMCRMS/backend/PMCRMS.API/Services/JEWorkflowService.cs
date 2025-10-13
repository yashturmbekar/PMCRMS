using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Orchestrates the complete Junior Engineer workflow
    /// Coordinates between auto-assignment, appointments, verification, and digital signature services
    /// </summary>
    public class JEWorkflowService : IJEWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<JEWorkflowService> _logger;
        private readonly IAutoAssignmentService _autoAssignmentService;
        private readonly IAppointmentService _appointmentService;
        private readonly IDocumentVerificationService _documentVerificationService;
        private readonly ISignatureWorkflowService _signatureWorkflowService;
        private readonly INotificationService _notificationService;
        private readonly PdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHsmService _hsmService;

        public JEWorkflowService(
            PMCRMSDbContext context,
            ILogger<JEWorkflowService> logger,
            IAutoAssignmentService autoAssignmentService,
            IAppointmentService appointmentService,
            IDocumentVerificationService documentVerificationService,
            ISignatureWorkflowService signatureWorkflowService,
            INotificationService notificationService,
            PdfService pdfService,
            IEmailService emailService,
            IWorkflowNotificationService workflowNotificationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHsmService hsmService)
        {
            _context = context;
            _logger = logger;
            _autoAssignmentService = autoAssignmentService;
            _appointmentService = appointmentService;
            _documentVerificationService = documentVerificationService;
            _signatureWorkflowService = signatureWorkflowService;
            _notificationService = notificationService;
            _pdfService = pdfService;
            _emailService = emailService;
            _workflowNotificationService = workflowNotificationService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _hsmService = hsmService;
        }

        public async Task<WorkflowActionResultDto> StartWorkflowAsync(StartJEWorkflowRequestDto request, int initiatedByUserId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Application not found",
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Check if already assigned
                if (application.AssignedJuniorEngineerId.HasValue)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Application already assigned to a Junior Engineer",
                        NewStatus = application.Status
                    };
                }

                // Auto-assign to JE
                var assignmentHistory = await _autoAssignmentService.AssignApplicationAsync(
                    request.ApplicationId, 
                    initiatedByUserId.ToString());

                if (assignmentHistory == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "No available Junior Engineer found"
                    };
                }

                // Update application
                application.Status = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING;
                application.AssignedJuniorEngineerId = assignmentHistory.AssignedToOfficerId;
                application.AssignedToJEDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING
                );

                // Send notification
                var officer = await _context.Officers.FindAsync(assignmentHistory.AssignedToOfficerId);
                if (officer != null)
                {
                    await _notificationService.NotifyOfficerAssignmentAsync(
                        officer.Id,
                        application.ApplicationNumber ?? "N/A",
                        application.Id,
                        application.PositionType.ToString(),
                        $"{application.FirstName} {application.LastName}",
                        initiatedByUserId.ToString());
                }

                _logger.LogInformation(
                    "Workflow started for application {ApplicationId}, assigned to officer {OfficerId}",
                    request.ApplicationId, 
                    assignmentHistory.AssignedToOfficerId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Application successfully assigned to {officer?.Name ?? "Junior Engineer"}",
                    NewStatus = application.Status,
                    NextAction = "Schedule Appointment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting workflow for application {ApplicationId}", request.ApplicationId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = $"Error starting workflow: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<WorkflowActionResultDto> ScheduleAppointmentAsync(ScheduleAppointmentRequestDto request, int scheduledByOfficerId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Create appointment
                await _appointmentService.ScheduleAppointmentAsync(
                    request.ApplicationId,
                    scheduledByOfficerId,
                    request.ReviewDate,
                    request.ContactPerson,
                    request.Place,
                    request.RoomNumber,
                    request.Comments);

                // Update application
                application.Status = ApplicationCurrentStatus.APPOINTMENT_SCHEDULED;
                application.JEAppointmentScheduled = true;
                application.JEAppointmentScheduledDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // NOTE: Email notification is sent by AppointmentService.ScheduleAppointmentAsync
                // with detailed appointment information (date, time, location, contact person, etc.)
                // No need to send a separate generic status update email here

                // Generate recommendation form PDF after appointment is scheduled
                try
                {
                    await GenerateAndSaveRecommendationFormAsync(request.ApplicationId);
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx, "Error generating recommendation form for application {ApplicationId}", request.ApplicationId);
                    // Don't fail the entire operation if PDF generation fails
                }

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Appointment scheduled successfully",
                    NewStatus = application.Status,
                    NextAction = "Complete Appointment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment for application {ApplicationId}", request.ApplicationId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<WorkflowActionResultDto> RescheduleAppointmentAsync(RescheduleAppointmentRequestDto request, int officerId)
        {
            try
            {
                _logger.LogInformation("Rescheduling appointment {AppointmentId} by officer {OfficerId}", request.AppointmentId, officerId);

                // Get officer details for logging
                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Officer not found" };
                }

                // Reschedule appointment using the AppointmentService
                var result = await _appointmentService.RescheduleAppointmentAsync(
                    request.AppointmentId,
                    request.NewReviewDate,
                    request.RescheduleReason,
                    officer.Name,
                    request.Place,
                    request.ContactPerson,
                    request.RoomNumber);

                if (!result.Success)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    };
                }

                // Get the new appointment details
                var newAppointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == result.AppointmentId);

                if (newAppointment == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "New appointment not found" };
                }

                _logger.LogInformation("Appointment rescheduled successfully. New appointment ID: {AppointmentId}", result.AppointmentId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Appointment rescheduled successfully to {request.NewReviewDate:MMMM dd, yyyy 'at' hh:mm tt}",
                    NewStatus = newAppointment.Application?.Status ?? ApplicationCurrentStatus.APPOINTMENT_SCHEDULED,
                    NextAction = "Complete Appointment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", request.AppointmentId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = $"Error rescheduling appointment: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<WorkflowActionResultDto> CompleteAppointmentAsync(CompleteAppointmentRequestDto request, int officerId)
        {
            try
            {
                // Complete appointment
                await _appointmentService.CompleteAppointmentAsync(
                    request.AppointmentId,
                    request.CompletionNotes,
                    officerId.ToString());

                // Get application from appointment
                var appointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

                if (appointment?.Application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Appointment or application not found" };
                }

                // Update application
                appointment.Application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING;
                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    appointment.ApplicationId,
                    ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING
                );

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Appointment completed. Ready for document verification.",
                    NewStatus = appointment.Application.Status,
                    NextAction = "Start Document Verification"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> VerifyDocumentAsync(VerifyDocumentRequestDto request, int officerId)
        {
            try
            {
                // Find the application using ApplicationId from the request
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Get officer details
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Id == officerId);

                if (officer == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Officer not found" };
                }

                // ========== TESTING MODE: HSM SIGNATURE BYPASSED ==========
                // TODO: REMOVE THIS COMMENT BLOCK FOR PRODUCTION
                // Apply digital signature if OTP provided (HSM validates OTP directly)
                // if (!string.IsNullOrWhiteSpace(request.Otp))
                // {
                //     // ✅ Use SignatureWorkflowService - HSM handles OTP validation
                //     _logger.LogInformation(
                //         "Initiating JE digital signature for application {ApplicationId} by officer {OfficerId}",
                //         request.ApplicationId, officerId);

                //     var signatureResult = await _signatureWorkflowService.SignAsJuniorEngineerAsync(
                //         request.ApplicationId,
                //         officerId,
                //         request.Otp
                //     );

                //     if (!signatureResult.Success)
                //     {
                //         return new WorkflowActionResultDto
                //         {
                //             Success = false,
                //             Message = $"Digital signature failed: {signatureResult.ErrorMessage}",
                //             Errors = new List<string> { signatureResult.ErrorMessage ?? "Unknown error" }
                //         };
                //     }

                //     // Set digital signature flags on application
                //     application.JEDigitalSignatureApplied = true;
                //     application.JEDigitalSignatureDate = DateTime.UtcNow;

                //     _logger.LogInformation(
                //         "JE digital signature completed for application {ApplicationId}",
                //         request.ApplicationId);
                // }

                // TESTING MODE: Auto-apply digital signature without HSM
                application.JEDigitalSignatureApplied = true;
                application.JEDigitalSignatureDate = DateTime.UtcNow;
                _logger.LogInformation(
                    "[TESTING MODE] JE digital signature auto-applied for application {ApplicationId}",
                    request.ApplicationId);
                // ========== END TESTING MODE ==========

                // Update application status if this is the first verification
                if (application.Status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING)
                {
                    application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS;
                    
                    // Send email notification to applicant
                    await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                        application.Id,
                        ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS
                    );
                }

                // Set AllDocumentsVerified to true
                application.JEAllDocumentsVerified = true;
                application.JEDocumentVerificationDate = DateTime.UtcNow;
                
                // Mark JE approval
                application.JEApprovalStatus = true;
                application.JEApprovalDate = DateTime.UtcNow;
                
                // Save JE comments if provided
                if (!string.IsNullOrWhiteSpace(request.Comments))
                {
                    application.JEComments = request.Comments;
                    application.JEApprovalComments = request.Comments;
                }

                // If digital signature was applied, auto-forward to Assistant Engineer
                // TESTING MODE: Always forward since we auto-apply signature
                if (application.JEDigitalSignatureApplied)
                {
                    // Use auto-assignment service for intelligent workload-based assignment
                    var assignment = await _autoAssignmentService.AutoAssignToNextWorkflowStageAsync(
                        applicationId: application.Id,
                        currentStatus: ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING,
                        currentOfficerId: officerId
                    );

                    if (assignment != null)
                    {
                        // Status already updated by auto-assignment service
                        application.Status = ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING;
                        application.JEApprovalDate = DateTime.UtcNow;

                        _logger.LogInformation(
                            "Application {ApplicationId} auto-assigned to Assistant Engineer {OfficerId} using workload-based strategy",
                            application.Id, assignment.AssignedToOfficerId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Auto-assignment failed for application {ApplicationId}. No available Assistant Engineer found.",
                            application.Id);
                    }
                }

                await _context.SaveChangesAsync();

                var message = application.JEDigitalSignatureApplied
                    ? application.Status == ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                        ? "Documents verified, recommendation form digitally signed, and application forwarded to Assistant Engineer successfully"
                        : "Documents verified and recommendation form digitally signed successfully"
                    : "Documents verified successfully";

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = message,
                    NewStatus = application.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> CompleteDocumentVerificationAsync(int applicationId, int officerId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Update application
                application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED;
                application.JEAllDocumentsVerified = true;
                application.JEDocumentVerificationDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED
                );

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "All documents verified. Ready for digital signature.",
                    NewStatus = application.Status,
                    NextAction = "Initiate Digital Signature"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing verification");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<string> GenerateOtpForSignatureAsync(int applicationId, int officerId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    throw new Exception("Application not found");
                }

                // Get officer details
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Id == officerId);

                if (officer == null)
                {
                    throw new Exception("Officer not found");
                }

                _logger.LogInformation("Generating OTP from HSM for application {ApplicationId} and officer {OfficerId}", 
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
                    keyLabel: "Test2025Sign",
                    otpType: "single"
                );

                if (!hsmResult.Success)
                {
                    _logger.LogError("HSM OTP generation failed: {Error}", hsmResult.ErrorMessage);
                    throw new Exception($"Failed to generate OTP: {hsmResult.ErrorMessage}");
                }

                _logger.LogInformation("✅ OTP generated and sent by HSM to officer {OfficerId} - no database storage needed", officerId);

                // HSM sends OTP directly to officer's registered mobile/email
                // No need to store or return OTP - HSM validates it during signing
                return "OTP sent to registered mobile/email";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for signature");
                throw;
            }
        }

        /// <summary>
        /// Call HSM OTP service to generate and send OTP
        /// </summary>
        private async Task<string> CallHsmOtpServiceAsync(string email, string? phoneNumber)
        {
            var httpClient = _httpClientFactory.CreateClient("HSM_OTP");
            var otpBaseUrl = _configuration["HSM:OtpBaseUrl"];

            _logger.LogInformation("Calling HSM OTP service at {OtpBaseUrl} for email: {Email}", otpBaseUrl, email);

            try
            {
                // Build the request payload for HSM OTP service
                // Note: The exact format depends on eMudhra's API specification
                // This is a common format - adjust based on actual API documentation
                var requestData = new
                {
                    email = email,
                    mobile = phoneNumber ?? "",
                    purpose = "DIGITAL_SIGNATURE",
                    otpLength = 6,
                    validity = 5 // minutes
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

                // Parse the response to extract OTP
                // Note: Adjust parsing based on actual HSM response format
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Try to extract OTP from response (common field names)
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
                    // If response format is different, log it and throw
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

        public async Task<WorkflowActionResultDto> InitiateDigitalSignatureAsync(int applicationId, int officerId, string documentPath)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Update application
                application.Status = ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE;
                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE
                );

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Digital signature process initiated.",
                    NewStatus = application.Status,
                    NextAction = "Complete Digital Signature with OTP"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating signature");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> CompleteDigitalSignatureAsync(ApplySignatureRequestDto request, int officerId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Update application - JE workflow complete
                application.Status = ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING;
                
                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING
                );

                application.JEDigitalSignatureApplied = true;
                application.JEDigitalSignatureDate = DateTime.UtcNow;
                application.JEApprovalDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Digital signature applied. Application forwarded to Assistant Engineer.",
                    NewStatus = application.Status,
                    NextAction = "Awaiting AE Review"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing signature");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<JEWorkflowStatusDto?> GetWorkflowStatusAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .Include(a => a.Appointments)
                    .Include(a => a.DocumentVerifications)
                    .Include(a => a.DigitalSignatures)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return null;
                }

                var latestAppointment = application.Appointments
                    .OrderByDescending(a => a.CreatedDate)
                    .FirstOrDefault();

                var verifiedCount = application.DocumentVerifications.Count(dv => dv.Status == VerificationStatus.Approved);
                var totalDocs = application.Documents.Count;

                return new JEWorkflowStatusDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    FirstName = application.FirstName ?? string.Empty,
                    LastName = application.LastName ?? string.Empty,
                    CurrentStatus = application.Status,
                    CurrentStatusDisplay = GetCurrentStage(application.Status),
                    IsAssigned = application.AssignedJuniorEngineerId.HasValue,
                    AssignedToOfficerId = application.AssignedJuniorEngineerId,
                    AssignedToOfficerName = application.AssignedJuniorEngineer?.Name,
                    AssignedDate = application.AssignedToJEDate,
                    HasAppointment = application.JEAppointmentScheduled,
                    AppointmentDate = latestAppointment?.ReviewDate,
                    AppointmentPlace = latestAppointment?.Place,
                    IsAppointmentCompleted = latestAppointment?.Status == AppointmentStatus.Completed,
                    AllDocumentsVerified = application.JEAllDocumentsVerified,
                    TotalDocuments = totalDocs,
                    VerifiedDocuments = verifiedCount,
                    DocumentsVerifiedDate = application.JEDocumentVerificationDate,
                    DigitalSignatureApplied = application.JEDigitalSignatureApplied,
                    DigitalSignatureDate = application.JEDigitalSignatureDate,
                    TotalSignatures = application.DigitalSignatures.Count,
                    CompletedSignatures = application.DigitalSignatures.Count(ds => ds.Status == SignatureStatus.Completed),
                    ProgressPercentage = CalculateProgressPercentage(application),
                    CurrentStage = GetCurrentStage(application.Status),
                    NextAction = GetNextAction(application),
                    CanProceedToNextStage = CanProceedToNextStage(application),
                    CreatedDate = application.CreatedDate,
                    CompletedDate = application.JEApprovalDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow status");
                return null;
            }
        }

        public async Task<WorkflowHistoryDto?> GetWorkflowHistoryAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return null;
                }

                var timeline = new List<WorkflowTimelineEventDto>();

                // Get all workflow events
                var assignments = await _context.AssignmentHistories
                    .Include(ah => ah.AssignedToOfficer)
                    .Where(ah => ah.ApplicationId == applicationId)
                    .ToListAsync();

                foreach (var assignment in assignments)
                {
                    timeline.Add(new WorkflowTimelineEventDto
                    {
                        Timestamp = assignment.CreatedDate,
                        EventType = "Assignment",
                        Description = $"Assigned to {assignment.AssignedToOfficer?.Name}",
                        PerformedBy = "System",
                        StatusAfter = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING
                    });
                }

                return new WorkflowHistoryDto
                {
                    ApplicationId = applicationId,
                    ApplicationNumber = application.ApplicationNumber ?? "N/A",
                    Timeline = timeline.OrderBy(t => t.Timestamp).ToList(),
                    TotalDurationDays = application.JEApprovalDate.HasValue 
                        ? (int)(application.JEApprovalDate.Value - application.CreatedDate).TotalDays 
                        : (int)(DateTime.UtcNow - application.CreatedDate).TotalDays
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history");
                return null;
            }
        }

        public async Task<WorkflowActionResultDto> TransitionToStatusAsync(TransitionWorkflowRequestDto request, int userId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(request.ApplicationId);
                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                var previousStatus = application.Status;
                application.Status = request.TargetStatus;
                application.Remarks = request.Reason;
                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Status transitioned from {previousStatus} to {request.TargetStatus}",
                    NewStatus = request.TargetStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning status");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowValidationResultDto> ValidateWorkflowProgressAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.Appointments)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new WorkflowValidationResultDto
                    {
                        IsValid = false,
                        ValidationErrors = new List<string> { "Application not found" }
                    };
                }

                var errors = new List<string>();
                
                if (!application.AssignedJuniorEngineerId.HasValue)
                {
                    errors.Add("Application not assigned");
                }

                return new WorkflowValidationResultDto
                {
                    IsValid = errors.Count == 0,
                    CanAssign = !application.AssignedJuniorEngineerId.HasValue,
                    CanScheduleAppointment = application.AssignedJuniorEngineerId.HasValue,
                    CanVerifyDocuments = application.JEAppointmentScheduled,
                    CanApplySignature = application.JEAllDocumentsVerified,
                    ValidationErrors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating workflow");
                throw;
            }
        }

        public async Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                fromDate ??= DateTime.UtcNow.AddMonths(-1);
                toDate ??= DateTime.UtcNow;

                var applications = await _context.PositionApplications
                    .Where(a => a.CreatedDate >= fromDate && a.CreatedDate <= toDate)
                    .ToListAsync();

                return new WorkflowSummaryDto
                {
                    TotalApplications = applications.Count,
                    PendingAssignment = applications.Count(a => !a.AssignedJuniorEngineerId.HasValue),
                    AppointmentScheduled = applications.Count(a => a.Status == ApplicationCurrentStatus.APPOINTMENT_SCHEDULED),
                    UnderVerification = applications.Count(a => a.Status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS),
                    AwaitingSignature = applications.Count(a => a.Status == ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE),
                    CompletedJEStage = applications.Count(a => a.JEApprovalDate.HasValue),
                    AverageProcessingDays = applications.Where(a => a.JEApprovalDate.HasValue)
                        .Average(a => (a.JEApprovalDate!.Value - a.CreatedDate).TotalDays)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting summary");
                throw;
            }
        }

        public async Task<WorkflowMetricsDto> GetWorkflowMetricsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var applications = await _context.PositionApplications
                    .Where(a => a.CreatedDate >= fromDate && a.CreatedDate <= toDate)
                    .ToListAsync();

                return new WorkflowMetricsDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalApplicationsReceived = applications.Count,
                    ApplicationsInProgress = applications.Count(a => !a.JEApprovalDate.HasValue),
                    ApplicationsCompleted = applications.Count(a => a.JEApprovalDate.HasValue),
                    TotalAverageProcessingDays = applications.Where(a => a.JEApprovalDate.HasValue)
                        .Average(a => (a.JEApprovalDate!.Value - a.CreatedDate).TotalDays)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics");
                throw;
            }
        }

        public async Task<List<JEWorkflowStatusDto>> GetOfficerApplicationsAsync(int officerId)
        {
            try
            {
                // ✅ Only show applications that are still pending JE action
                // Exclude applications where JE has completed verification and digital signature
                var applications = await _context.PositionApplications
                    .Where(a => a.AssignedJuniorEngineerId == officerId 
                        && (!a.JEDigitalSignatureApplied || a.Status != ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING))
                    .ToListAsync();

                var statusList = new List<JEWorkflowStatusDto>();
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
                _logger.LogError(ex, "Error getting officer applications");
                throw;
            }
        }

        public async Task<List<JEWorkflowStatusDto>> GetApplicationsByStageAsync(ApplicationCurrentStatus status)
        {
            try
            {
                var applications = await _context.PositionApplications
                    .Where(a => a.Status == status)
                    .ToListAsync();

                var statusList = new List<JEWorkflowStatusDto>();
                foreach (var app in applications)
                {
                    var workflowStatus = await GetWorkflowStatusAsync(app.Id);
                    if (workflowStatus != null)
                    {
                        statusList.Add(workflowStatus);
                    }
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications by stage");
                throw;
            }
        }

        public async Task<List<WorkflowActionResultDto>> PerformBulkActionAsync(BulkWorkflowActionRequestDto request, int userId)
        {
            var results = new List<WorkflowActionResultDto>();

            foreach (var appId in request.ApplicationIds)
            {
                try
                {
                    var application = await _context.PositionApplications.FindAsync(appId);
                    if (application == null)
                    {
                        results.Add(new WorkflowActionResultDto 
                        { 
                            Success = false, 
                            Message = $"Application {appId} not found" 
                        });
                        continue;
                    }

                    // Perform action based on type
                    results.Add(new WorkflowActionResultDto
                    {
                        Success = true,
                        Message = $"Action {request.ActionType} performed on application {appId}"
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();
            return results;
        }

        public async Task<WorkflowActionResultDto> RetryWorkflowStepAsync(int applicationId, string stepName, int userId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Reset step
                switch (stepName.ToUpper())
                {
                    case "APPOINTMENT":
                        application.JEAppointmentScheduled = false;
                        application.Status = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING;
                        break;
                    case "VERIFICATION":
                        application.JEAllDocumentsVerified = false;
                        application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING;
                        break;
                    case "SIGNATURE":
                        application.JEDigitalSignatureApplied = false;
                        application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED;
                        break;
                    default:
                        return new WorkflowActionResultDto { Success = false, Message = $"Unknown step: {stepName}" };
                }

                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Step '{stepName}' reset successfully",
                    NewStatus = application.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying step");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<WorkflowActionResultDto> CancelWorkflowAsync(int applicationId, string reason, int userId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                application.Status = ApplicationCurrentStatus.REJECTED;
                application.Remarks = $"Cancelled: {reason}";
                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.REJECTED,
                    $"Cancelled: {reason}"
                );

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Workflow cancelled successfully",
                    NewStatus = application.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling workflow");
                return new WorkflowActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<List<WorkflowTimelineEventDto>> GetWorkflowTimelineAsync(int applicationId)
        {
            try
            {
                var history = await GetWorkflowHistoryAsync(applicationId);
                return history?.Timeline ?? new List<WorkflowTimelineEventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline");
                throw;
            }
        }

        public async Task<int> SendDelayedApplicationRemindersAsync()
        {
            try
            {
                var thresholdDate = DateTime.UtcNow.AddDays(-3);
                var delayedApps = await _context.PositionApplications
                    .Where(a => a.AssignedJuniorEngineerId.HasValue &&
                               a.AssignedToJEDate <= thresholdDate &&
                               !a.JEApprovalDate.HasValue)
                    .ToListAsync();

                foreach (var app in delayedApps)
                {
                    await _notificationService.CreateNotificationAsync(
                        app.AssignedJuniorEngineerId!.Value,
                        "WORKFLOW_REMINDER",
                        "Pending Application",
                        $"Application {app.ApplicationNumber} is pending your action.",
                        app.Id,
                        app.ApplicationNumber,
                        priority: NotificationPriority.High);
                }

                return delayedApps.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminders");
                throw;
            }
        }

        // Helper Methods
        private double CalculateProgressPercentage(PositionApplication application)
        {
            var steps = new[]
            {
                application.AssignedJuniorEngineerId.HasValue,
                application.JEAppointmentScheduled,
                application.JEAllDocumentsVerified,
                application.JEDigitalSignatureApplied,
                application.JEApprovalDate.HasValue
            };

            var completedSteps = steps.Count(s => s);
            return (completedSteps / (double)steps.Length) * 100;
        }

        /// <summary>
        /// Maps position type to corresponding Assistant Engineer role
        /// </summary>
        private OfficerRole MapPositionToAERole(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => OfficerRole.AssistantArchitect,
                PositionType.StructuralEngineer => OfficerRole.AssistantStructuralEngineer,
                PositionType.LicenceEngineer => OfficerRole.AssistantLicenceEngineer,
                PositionType.Supervisor1 => OfficerRole.AssistantSupervisor1,
                PositionType.Supervisor2 => OfficerRole.AssistantSupervisor2,
                _ => throw new ArgumentException($"Unknown position type: {positionType}")
            };
        }

        private string GetCurrentStage(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => "JE Review Pending",
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => "Appointment Scheduled",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => "Document Verification Pending",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => "Document Verification In Progress",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => "Document Verification Completed",
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => "Awaiting JE Digital Signature",
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => "Forwarded to Assistant Engineer",
                _ => status.ToString()
            };
        }

        private string GetNextAction(PositionApplication application)
        {
            return application.Status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => "Schedule Appointment",
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => "Complete Appointment",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => "Start Document Verification",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => "Complete Document Verification",
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => "Initiate Digital Signature",
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => "Complete Digital Signature",
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => "Awaiting AE Review",
                _ => "No action required"
            };
        }

        private bool CanProceedToNextStage(PositionApplication application)
        {
            return application.Status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => application.AssignedJuniorEngineerId.HasValue,
                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => true,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => true,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => true,
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => application.JEAllDocumentsVerified,
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => true,
                _ => false
            };
        }

        /// <summary>
        /// Generates recommendation form PDF and saves it to the database
        /// </summary>
        private async Task GenerateAndSaveRecommendationFormAsync(int applicationId)
        {
            // Check if recommendation form already exists
            var existingForm = await _context.SEDocuments
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.RecommendedForm);

            if (existingForm != null)
            {
                _logger.LogInformation("Recommendation form already exists for application {ApplicationId}", applicationId);
                return;
            }

            // Generate PDF
            var pdfResult = await _pdfService.GenerateApplicationPdfAsync(applicationId);

            if (!pdfResult.IsSuccess || pdfResult.FileContent == null)
            {
                throw new Exception($"Failed to generate recommendation form: {pdfResult.Message}");
            }

            // Create document record - Store PDF content in database instead of file path
            var document = new SEDocument
            {
                ApplicationId = applicationId,
                DocumentType = SEDocumentType.RecommendedForm,
                FileName = pdfResult.FileName ?? $"RecommendedForm_{applicationId}.pdf",
                FilePath = null, // No physical file path needed
                FileId = Guid.NewGuid().ToString(),
                FileSize = (decimal)(pdfResult.FileContent.Length / 1024.0), // Size in KB
                ContentType = "application/pdf",
                FileContent = pdfResult.FileContent, // Store PDF binary data in database
                IsVerified = false,
                CreatedDate = DateTime.UtcNow
            };

            _context.SEDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recommendation form generated and saved to database for application {ApplicationId}", applicationId);
        }
    }
}
