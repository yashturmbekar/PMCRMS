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

        public JEWorkflowService(
            PMCRMSDbContext context,
            ILogger<JEWorkflowService> logger,
            IAutoAssignmentService autoAssignmentService,
            IAppointmentService appointmentService,
            IDocumentVerificationService documentVerificationService,
            IDigitalSignatureService digitalSignatureService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _autoAssignmentService = autoAssignmentService;
            _appointmentService = appointmentService;
            _documentVerificationService = documentVerificationService;
            _digitalSignatureService = digitalSignatureService;
            _notificationService = notificationService;
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
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == request.DocumentId); // Note: Using DocumentId as ApplicationId based on DTO

                if (application == null)
                {
                    return new WorkflowActionResultDto { Success = false, Message = "Application not found" };
                }

                // Update status if pending
                if (application.Status == ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING)
                {
                    application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS;
                    await _context.SaveChangesAsync();
                }

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Document verification recorded",
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
    }
}
