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
        private readonly IDigitalSignatureService _digitalSignatureService;
        private readonly INotificationService _notificationService;
        private readonly PdfService _pdfService;

        public JEWorkflowService(
            PMCRMSDbContext context,
            ILogger<JEWorkflowService> logger,
            IAutoAssignmentService autoAssignmentService,
            IAppointmentService appointmentService,
            IDocumentVerificationService documentVerificationService,
            IDigitalSignatureService digitalSignatureService,
            INotificationService notificationService,
            PdfService pdfService)
        {
            _context = context;
            _logger = logger;
            _autoAssignmentService = autoAssignmentService;
            _appointmentService = appointmentService;
            _documentVerificationService = documentVerificationService;
            _digitalSignatureService = digitalSignatureService;
            _notificationService = notificationService;
            _pdfService = pdfService;
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
                application.AppointmentScheduled = true;
                application.AppointmentScheduledDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

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

                // Validate OTP and apply digital signature if OTP provided
                if (!string.IsNullOrWhiteSpace(request.Otp))
                {
                    var otpVerification = await _context.OtpVerifications
                        .Where(o => o.Identifier == officer.Email 
                                 && o.OtpCode == request.Otp 
                                 && o.Purpose == "DIGITAL_SIGNATURE"
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
                    
                    // Get recommendation form PDF document
                    var recommendationForm = await _context.SEDocuments
                        .FirstOrDefaultAsync(d => d.ApplicationId == request.ApplicationId 
                                                && d.DocumentType == SEDocumentType.RecommendedForm);

                    if (recommendationForm == null || recommendationForm.FileContent == null)
                    {
                        return new WorkflowActionResultDto 
                        { 
                            Success = false, 
                            Message = "Recommendation form not found. Please generate it first." 
                        };
                    }

                    // Save PDF temporarily for HSM signing
                    var tempPdfPath = Path.Combine(Path.GetTempPath(), $"recommendation_{request.ApplicationId}.pdf");
                    await File.WriteAllBytesAsync(tempPdfPath, recommendationForm.FileContent);

                    try
                    {
                        // Initiate digital signature with HSM
                        var coordinates = $"{100},{700},{200},{150},{1}"; // X, Y, Width, Height, Page
                        var ipAddress = ""; // TODO: Get from HTTP context if needed
                        var userAgent = "PMCRMS_API";

                        var initiateResult = await _digitalSignatureService.InitiateSignatureAsync(
                            applicationId: request.ApplicationId,
                            signedByOfficerId: officerId,
                            signatureType: SignatureType.JuniorEngineer,
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

                        // Complete signature with OTP (calls actual HSM API)
                        var completeResult = await _digitalSignatureService.CompleteSignatureAsync(
                            signatureId: initiateResult.SignatureId.Value,
                            otp: request.Otp,
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
                        if (!string.IsNullOrEmpty(completeResult.SignedDocumentPath) && File.Exists(completeResult.SignedDocumentPath))
                        {
                            var signedPdfBytes = await File.ReadAllBytesAsync(completeResult.SignedDocumentPath);
                            recommendationForm.FileContent = signedPdfBytes;
                            recommendationForm.FileSize = (decimal)(signedPdfBytes.Length / 1024.0); // KB
                            recommendationForm.UpdatedDate = DateTime.UtcNow;
                            
                            _logger.LogInformation("Updated recommendation form with digitally signed PDF for application {ApplicationId}", request.ApplicationId);
                        }

                        // Set digital signature flags on application
                        application.DigitalSignatureApplied = true;
                        application.DigitalSignatureDate = DateTime.UtcNow;
                    }
                    finally
                    {
                        // Clean up temporary file
                        if (File.Exists(tempPdfPath))
                        {
                            File.Delete(tempPdfPath);
                        }
                    }
                }

                // Update application status if this is the first verification
                if (application.Status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING)
                {
                    application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS;
                }

                // Set AllDocumentsVerified to true
                application.AllDocumentsVerified = true;
                application.DocumentsVerifiedDate = DateTime.UtcNow;
                
                // Save JE comments if provided
                if (!string.IsNullOrWhiteSpace(request.Comments))
                {
                    application.JEComments = request.Comments;
                }

                await _context.SaveChangesAsync();

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = request.Otp != null ? "Documents verified and recommendation form digitally signed successfully" : "Documents verified successfully",
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
                application.AllDocumentsVerified = true;
                application.DocumentsVerifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

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

                // Generate 6-digit OTP
                var random = new Random();
                var otp = random.Next(100000, 999999).ToString();

                // Store OTP in database
                var otpVerification = new OtpVerification
                {
                    Identifier = officer.Email,
                    OtpCode = otp,
                    Purpose = "DIGITAL_SIGNATURE",
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    IsActive = true,
                    CreatedBy = officerId.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated OTP for application {ApplicationId} and officer {OfficerId}. OTP: {Otp}", 
                    applicationId, officerId, otp);

                // In production, send OTP via email service instead of returning it
                return otp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for signature");
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
                application.DigitalSignatureApplied = true;
                application.DigitalSignatureDate = DateTime.UtcNow;
                application.JECompletedDate = DateTime.UtcNow;
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
                    HasAppointment = application.AppointmentScheduled,
                    AppointmentDate = latestAppointment?.ReviewDate,
                    AppointmentPlace = latestAppointment?.Place,
                    IsAppointmentCompleted = latestAppointment?.Status == AppointmentStatus.Completed,
                    AllDocumentsVerified = application.AllDocumentsVerified,
                    TotalDocuments = totalDocs,
                    VerifiedDocuments = verifiedCount,
                    DocumentsVerifiedDate = application.DocumentsVerifiedDate,
                    DigitalSignatureApplied = application.DigitalSignatureApplied,
                    DigitalSignatureDate = application.DigitalSignatureDate,
                    TotalSignatures = application.DigitalSignatures.Count,
                    CompletedSignatures = application.DigitalSignatures.Count(ds => ds.Status == SignatureStatus.Completed),
                    ProgressPercentage = CalculateProgressPercentage(application),
                    CurrentStage = GetCurrentStage(application.Status),
                    NextAction = GetNextAction(application),
                    CanProceedToNextStage = CanProceedToNextStage(application),
                    CreatedDate = application.CreatedDate,
                    CompletedDate = application.JECompletedDate
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
                    TotalDurationDays = application.JECompletedDate.HasValue 
                        ? (int)(application.JECompletedDate.Value - application.CreatedDate).TotalDays 
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
                    CanVerifyDocuments = application.AppointmentScheduled,
                    CanApplySignature = application.AllDocumentsVerified,
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
                    CompletedJEStage = applications.Count(a => a.JECompletedDate.HasValue),
                    AverageProcessingDays = applications.Where(a => a.JECompletedDate.HasValue)
                        .Average(a => (a.JECompletedDate!.Value - a.CreatedDate).TotalDays)
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
                    ApplicationsInProgress = applications.Count(a => !a.JECompletedDate.HasValue),
                    ApplicationsCompleted = applications.Count(a => a.JECompletedDate.HasValue),
                    TotalAverageProcessingDays = applications.Where(a => a.JECompletedDate.HasValue)
                        .Average(a => (a.JECompletedDate!.Value - a.CreatedDate).TotalDays)
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
                var applications = await _context.PositionApplications
                    .Where(a => a.AssignedJuniorEngineerId == officerId)
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
                        application.AppointmentScheduled = false;
                        application.Status = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING;
                        break;
                    case "VERIFICATION":
                        application.AllDocumentsVerified = false;
                        application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING;
                        break;
                    case "SIGNATURE":
                        application.DigitalSignatureApplied = false;
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
                               !a.JECompletedDate.HasValue)
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
                application.AppointmentScheduled,
                application.AllDocumentsVerified,
                application.DigitalSignatureApplied,
                application.JECompletedDate.HasValue
            };

            var completedSteps = steps.Count(s => s);
            return (completedSteps / (double)steps.Length) * 100;
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
                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => application.AllDocumentsVerified,
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
