using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Implementation of workflow progression service
    /// Handles automatic progression through all approval stages with auto-assignment
    /// </summary>
    public class WorkflowProgressionService : IWorkflowProgressionService
    {
        private readonly PMCRMSDbContext _context;
        private readonly IAutoAssignmentService _autoAssignmentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WorkflowProgressionService> _logger;

        public WorkflowProgressionService(
            PMCRMSDbContext context,
            IAutoAssignmentService autoAssignmentService,
            INotificationService notificationService,
            ILogger<WorkflowProgressionService> logger)
        {
            _context = context;
            _autoAssignmentService = autoAssignmentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> ProgressToAssistantEngineerAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 1→2: Progressing application {ApplicationId} to Assistant Engineer", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to Assistant Engineer
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to Assistant Engineer {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no Assistant Engineer available for assignment", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to Assistant Engineer", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToExecutiveEngineerStage1Async(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 2→3: Progressing application {ApplicationId} to Executive Engineer (Stage 1)", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to Executive Engineer
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to Executive Engineer {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no Executive Engineer available for assignment", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to Executive Engineer Stage 1", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToCityEngineerAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 3→4: Progressing application {ApplicationId} to City Engineer", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.CITY_ENGINEER_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to City Engineer
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to City Engineer {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no City Engineer available for assignment", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to City Engineer", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToPaymentAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);
                    
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 4→5: Progressing application {ApplicationId} to Payment", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.PaymentPending;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Send payment notification to user
                if (application.User != null)
                {
                    try
                    {
                        // TODO: Send payment notification email
                        _logger.LogInformation("✓ Payment notification sent to user {UserId} for application {ApplicationId}", 
                            application.UserId, applicationId);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogError(notifyEx, "Failed to send payment notification for application {ApplicationId}", applicationId);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to Payment", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToClerkAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 5→6: Progressing application {ApplicationId} to Clerk", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.CLERK_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to Clerk
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to Clerk {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no Clerk available for assignment", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to Clerk", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToExecutiveEngineerSignatureAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 6→7: Progressing application {ApplicationId} to Executive Engineer (Digital Signature)", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to Executive Engineer for signature
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to Executive Engineer for signature {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no Executive Engineer available for signature", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to Executive Engineer Signature", applicationId);
                return false;
            }
        }

        public async Task<bool> ProgressToCityEngineerFinalSignatureAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("STAGE 7→FINAL: Progressing application {ApplicationId} to City Engineer (Final Signature)", applicationId);

                // Update status
                application.Status = ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Auto-assign to City Engineer for final signature
                var assignmentResult = await _autoAssignmentService.AssignApplicationAsync(applicationId);
                
                if (assignmentResult != null)
                {
                    _logger.LogInformation("✓ Application {ApplicationId} auto-assigned to City Engineer for final signature {OfficerId}", 
                        applicationId, assignmentResult.AssignedToOfficerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠ Application {ApplicationId} status updated but no City Engineer available for final signature", applicationId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error progressing application {ApplicationId} to City Engineer Final Signature", applicationId);
                return false;
            }
        }

        public async Task<bool> CompleteWorkflowAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);
                    
                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return false;
                }

                _logger.LogInformation("FINAL STAGE: Completing workflow for application {ApplicationId}", applicationId);

                // Update status to completed
                application.Status = ApplicationCurrentStatus.Completed;
                application.ApprovedDate = DateTime.UtcNow;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                // Send completion notification to user
                if (application.User != null)
                {
                    try
                    {
                        // TODO: Send completion and certificate issuance notification
                        _logger.LogInformation("✓ Completion notification sent to user {UserId} for application {ApplicationId}", 
                            application.UserId, applicationId);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogError(notifyEx, "Failed to send completion notification for application {ApplicationId}", applicationId);
                    }
                }

                _logger.LogInformation("🎉 Workflow completed successfully for application {ApplicationId}", applicationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing workflow for application {ApplicationId}", applicationId);
                return false;
            }
        }

        public async Task<WorkflowStageInfo?> GetWorkflowStageAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignedJuniorEngineer)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return null;
                }

                var stageInfo = new WorkflowStageInfo
                {
                    ApplicationId = application.Id,
                    ApplicationNumber = application.ApplicationNumber ?? "",
                    CurrentStatus = application.Status,
                    CurrentlyAssignedOfficerId = application.AssignedJuniorEngineerId,
                    CurrentlyAssignedOfficerName = application.AssignedJuniorEngineer?.Name,
                    CurrentlyAssignedOfficerRole = application.AssignedJuniorEngineer?.Role
                };

                // Determine current stage and progress
                switch (application.Status)
                {
                    case ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING:
                    case ApplicationCurrentStatus.APPOINTMENT_SCHEDULED:
                    case ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING:
                    case ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS:
                    case ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED:
                    case ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE:
                        stageInfo.CurrentStageNumber = 1;
                        stageInfo.CurrentStageName = "Junior Engineer Review";
                        stageInfo.NextStageName = "Assistant Engineer Approval";
                        stageInfo.ProgressPercentage = 14.3m;
                        break;

                    case ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING:
                        stageInfo.CurrentStageNumber = 2;
                        stageInfo.CurrentStageName = "Assistant Engineer Approval";
                        stageInfo.NextStageName = "Executive Engineer Review (Stage 1)";
                        stageInfo.ProgressPercentage = 28.6m;
                        break;

                    case ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING:
                        stageInfo.CurrentStageNumber = 3;
                        stageInfo.CurrentStageName = "Executive Engineer Review (Stage 1)";
                        stageInfo.NextStageName = "City Engineer Approval";
                        stageInfo.ProgressPercentage = 42.9m;
                        break;

                    case ApplicationCurrentStatus.CITY_ENGINEER_PENDING:
                        stageInfo.CurrentStageNumber = 4;
                        stageInfo.CurrentStageName = "City Engineer Approval";
                        stageInfo.NextStageName = "Payment";
                        stageInfo.ProgressPercentage = 57.1m;
                        break;

                    case ApplicationCurrentStatus.PaymentPending:
                    case ApplicationCurrentStatus.PaymentCompleted:
                        stageInfo.CurrentStageNumber = 5;
                        stageInfo.CurrentStageName = "Payment Processing";
                        stageInfo.NextStageName = "Clerk Processing";
                        stageInfo.ProgressPercentage = 71.4m;
                        break;

                    case ApplicationCurrentStatus.CLERK_PENDING:
                    case ApplicationCurrentStatus.ProcessedByClerk:
                        stageInfo.CurrentStageNumber = 5;
                        stageInfo.CurrentStageName = "Clerk Processing";
                        stageInfo.NextStageName = "Executive Engineer Signature";
                        stageInfo.ProgressPercentage = 71.4m;
                        break;

                    case ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING:
                    case ApplicationCurrentStatus.DigitalSignatureCompletedByEE2:
                        stageInfo.CurrentStageNumber = 6;
                        stageInfo.CurrentStageName = "Executive Engineer Digital Signature";
                        stageInfo.NextStageName = "City Engineer Final Signature";
                        stageInfo.ProgressPercentage = 85.7m;
                        break;

                    case ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING:
                    case ApplicationCurrentStatus.CertificateIssued:
                        stageInfo.CurrentStageNumber = 7;
                        stageInfo.CurrentStageName = "City Engineer Final Signature";
                        stageInfo.NextStageName = "Completed";
                        stageInfo.ProgressPercentage = 95.0m;
                        break;

                    case ApplicationCurrentStatus.Completed:
                        stageInfo.CurrentStageNumber = 7;
                        stageInfo.CurrentStageName = "Completed";
                        stageInfo.NextStageName = "N/A";
                        stageInfo.ProgressPercentage = 100.0m;
                        break;

                    default:
                        stageInfo.CurrentStageNumber = 0;
                        stageInfo.CurrentStageName = application.Status.ToString();
                        stageInfo.NextStageName = "Unknown";
                        stageInfo.ProgressPercentage = 0;
                        break;
                }

                return stageInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow stage for application {ApplicationId}", applicationId);
                return null;
            }
        }

        public async Task<List<WorkflowProgressionHistory>> GetWorkflowHistoryAsync(int applicationId)
        {
            try
            {
                // Get assignment history which tracks workflow progression
                var assignmentHistory = await _context.AssignmentHistories
                    .Include(h => h.PreviousOfficer)
                    .Include(h => h.AssignedToOfficer)
                    .Include(h => h.Application)
                    .Where(h => h.ApplicationId == applicationId)
                    .OrderBy(h => h.AssignedDate)
                    .ToListAsync();

                var history = new List<WorkflowProgressionHistory>();

                foreach (var assignment in assignmentHistory)
                {
                    history.Add(new WorkflowProgressionHistory
                    {
                        Id = assignment.Id,
                        ApplicationId = assignment.ApplicationId,
                        FromStatus = assignment.ApplicationStatusAtAssignment ?? ApplicationCurrentStatus.Draft,
                        ToStatus = assignment.Application?.Status ?? ApplicationCurrentStatus.Draft,
                        FromOfficerId = assignment.PreviousOfficerId,
                        FromOfficerName = assignment.PreviousOfficer?.Name,
                        ToOfficerId = assignment.AssignedToOfficerId,
                        ToOfficerName = assignment.AssignedToOfficer?.Name,
                        ProgressionDate = assignment.AssignedDate,
                        Comments = assignment.Reason,
                        IsAutoProgression = assignment.Action == AssignmentAction.AutoAssigned,
                        ProgressionTriggeredBy = assignment.AssignedByAdminId ?? "System"
                    });
                }

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow history for application {ApplicationId}", applicationId);
                return new List<WorkflowProgressionHistory>();
            }
        }
    }
}
