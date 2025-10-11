using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service to handle workflow routing and automatic assignment of applications to officers
    /// </summary>
    public interface IWorkflowRoutingService
    {
        Task<Officer?> GetJuniorEngineerForPosition(PositionType positionType);
        Task<Officer?> GetAssistantEngineerForPosition(PositionType positionType);
        Task<Officer?> GetExecutiveEngineer();
        Task<Officer?> GetCityEngineer();
        Task<Officer?> GetClerk();
        Task<bool> AssignApplicationToOfficer(int applicationId, int officerId, ApplicationCurrentStatus status);
        Task<bool> ValidateStatusTransition(ApplicationCurrentStatus currentStatus, ApplicationCurrentStatus newStatus);
    }

    public class WorkflowRoutingService : IWorkflowRoutingService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<WorkflowRoutingService> _logger;

        public WorkflowRoutingService(PMCRMSDbContext context, ILogger<WorkflowRoutingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get Junior Engineer for specific position type
        /// </summary>
        public async Task<Officer?> GetJuniorEngineerForPosition(PositionType positionType)
        {
            var role = positionType switch
            {
                PositionType.Architect => OfficerRole.JuniorArchitect,
                PositionType.StructuralEngineer => OfficerRole.JuniorStructuralEngineer,
                PositionType.LicenceEngineer => OfficerRole.JuniorLicenceEngineer,
                PositionType.Supervisor1 => OfficerRole.JuniorSupervisor1,
                PositionType.Supervisor2 => OfficerRole.JuniorSupervisor2,
                _ => OfficerRole.JuniorArchitect
            };

            var officer = await _context.Officers
                .Where(o => o.Role == role && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment if multiple officers
                .FirstOrDefaultAsync();

            if (officer == null)
            {
                _logger.LogWarning("No active Junior Engineer found for position type: {PositionType}", positionType);
            }

            return officer;
        }

        /// <summary>
        /// Get Assistant Engineer for specific position type
        /// </summary>
        public async Task<Officer?> GetAssistantEngineerForPosition(PositionType positionType)
        {
            var role = positionType switch
            {
                PositionType.Architect => OfficerRole.AssistantArchitect,
                PositionType.StructuralEngineer => OfficerRole.AssistantStructuralEngineer,
                PositionType.LicenceEngineer => OfficerRole.AssistantLicenceEngineer,
                PositionType.Supervisor1 => OfficerRole.AssistantSupervisor1,
                PositionType.Supervisor2 => OfficerRole.AssistantSupervisor2,
                _ => OfficerRole.AssistantArchitect
            };

            var officer = await _context.Officers
                .Where(o => o.Role == role && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment if multiple officers
                .FirstOrDefaultAsync();

            if (officer == null)
            {
                _logger.LogWarning("No active Assistant Engineer found for position type: {PositionType}", positionType);
            }

            return officer;
        }

        /// <summary>
        /// Get Executive Engineer (shared across all position types)
        /// </summary>
        public async Task<Officer?> GetExecutiveEngineer()
        {
            var officer = await _context.Officers
                .Where(o => o.Role == OfficerRole.ExecutiveEngineer && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment if multiple officers
                .FirstOrDefaultAsync();

            if (officer == null)
            {
                _logger.LogWarning("No active Executive Engineer found");
            }

            return officer;
        }

        /// <summary>
        /// Get City Engineer (shared across all position types)
        /// </summary>
        public async Task<Officer?> GetCityEngineer()
        {
            var officer = await _context.Officers
                .Where(o => o.Role == OfficerRole.CityEngineer && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment if multiple officers
                .FirstOrDefaultAsync();

            if (officer == null)
            {
                _logger.LogWarning("No active City Engineer found");
            }

            return officer;
        }

        /// <summary>
        /// Get Clerk (shared across all position types)
        /// </summary>
        public async Task<Officer?> GetClerk()
        {
            var officer = await _context.Officers
                .Where(o => o.Role == OfficerRole.Clerk && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment if multiple officers
                .FirstOrDefaultAsync();

            if (officer == null)
            {
                _logger.LogWarning("No active Clerk found");
            }

            return officer;
        }

        /// <summary>
        /// Assign application to officer and update status
        /// </summary>
        public async Task<bool> AssignApplicationToOfficer(int applicationId, int officerId, ApplicationCurrentStatus status)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                var officer = await _context.Officers.FindAsync(officerId);

                if (application == null || officer == null)
                {
                    _logger.LogError("Application or Officer not found. ApplicationId: {AppId}, OfficerId: {OfficerId}", 
                        applicationId, officerId);
                    return false;
                }

                application.Status = status;
                application.UpdatedDate = DateTime.UtcNow;
                application.UpdatedBy = officer.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {AppNumber} assigned to {OfficerName} with status {Status}", 
                    application.ApplicationNumber, officer.Name, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning application {AppId} to officer {OfficerId}", 
                    applicationId, officerId);
                return false;
            }
        }

        /// <summary>
        /// Validate if status transition is allowed
        /// </summary>
        public Task<bool> ValidateStatusTransition(ApplicationCurrentStatus currentStatus, ApplicationCurrentStatus newStatus)
        {
            // Define valid status transitions based on workflow
            var validTransitions = new Dictionary<ApplicationCurrentStatus, List<ApplicationCurrentStatus>>
            {
                [ApplicationCurrentStatus.Draft] = new() { ApplicationCurrentStatus.Submitted },
                [ApplicationCurrentStatus.Submitted] = new() { ApplicationCurrentStatus.UnderReviewByJE, ApplicationCurrentStatus.RejectedByJE },
                [ApplicationCurrentStatus.UnderReviewByJE] = new() { ApplicationCurrentStatus.ApprovedByJE, ApplicationCurrentStatus.RejectedByJE },
                [ApplicationCurrentStatus.ApprovedByJE] = new() { ApplicationCurrentStatus.UnderReviewByAE },
                [ApplicationCurrentStatus.RejectedByJE] = new() { ApplicationCurrentStatus.Submitted },
                [ApplicationCurrentStatus.UnderReviewByAE] = new() { ApplicationCurrentStatus.ApprovedByAE, ApplicationCurrentStatus.RejectedByAE },
                [ApplicationCurrentStatus.ApprovedByAE] = new() { ApplicationCurrentStatus.UnderReviewByEE1 },
                [ApplicationCurrentStatus.RejectedByAE] = new() { ApplicationCurrentStatus.Submitted },
                [ApplicationCurrentStatus.UnderReviewByEE1] = new() { ApplicationCurrentStatus.ApprovedByEE1, ApplicationCurrentStatus.RejectedByEE1 },
                [ApplicationCurrentStatus.ApprovedByEE1] = new() { ApplicationCurrentStatus.UnderReviewByCE1 },
                [ApplicationCurrentStatus.RejectedByEE1] = new() { ApplicationCurrentStatus.Submitted },
                [ApplicationCurrentStatus.UnderReviewByCE1] = new() { ApplicationCurrentStatus.ApprovedByCE1, ApplicationCurrentStatus.RejectedByCE1 },
                [ApplicationCurrentStatus.ApprovedByCE1] = new() { ApplicationCurrentStatus.PaymentPending },
                [ApplicationCurrentStatus.RejectedByCE1] = new() { ApplicationCurrentStatus.Submitted },
                [ApplicationCurrentStatus.PaymentPending] = new() { ApplicationCurrentStatus.PaymentCompleted },
                [ApplicationCurrentStatus.PaymentCompleted] = new() { ApplicationCurrentStatus.UnderProcessingByClerk },
                [ApplicationCurrentStatus.UnderProcessingByClerk] = new() { ApplicationCurrentStatus.ProcessedByClerk },
                [ApplicationCurrentStatus.ProcessedByClerk] = new() { ApplicationCurrentStatus.UnderDigitalSignatureByEE2 },
                [ApplicationCurrentStatus.UnderDigitalSignatureByEE2] = new() { ApplicationCurrentStatus.DigitalSignatureCompletedByEE2 },
                [ApplicationCurrentStatus.DigitalSignatureCompletedByEE2] = new() { ApplicationCurrentStatus.UnderFinalApprovalByCE2 },
                [ApplicationCurrentStatus.UnderFinalApprovalByCE2] = new() { ApplicationCurrentStatus.CertificateIssued },
                [ApplicationCurrentStatus.CertificateIssued] = new() { ApplicationCurrentStatus.Completed }
            };

            var isValid = validTransitions.ContainsKey(currentStatus) && 
                          validTransitions[currentStatus].Contains(newStatus);

            if (!isValid)
            {
                _logger.LogWarning("Invalid status transition from {CurrentStatus} to {NewStatus}", 
                    currentStatus, newStatus);
            }

            return Task.FromResult(isValid);
        }
    }
}
