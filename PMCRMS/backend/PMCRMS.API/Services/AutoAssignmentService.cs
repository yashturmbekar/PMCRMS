using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Text.Json;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for auto-assignment of applications to Junior Engineers
    /// Implements RoundRobin, WorkloadBased, PriorityBased, and SkillBased strategies
    /// </summary>
    public class AutoAssignmentService : IAutoAssignmentService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AutoAssignmentService> _logger;
        private readonly INotificationService _notificationService;

        public AutoAssignmentService(
            PMCRMSDbContext context,
            ILogger<AutoAssignmentService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<AssignmentHistory?> AssignApplicationAsync(int applicationId, string? assignedByAdminId = null)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignmentHistories)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found for assignment", applicationId);
                    return null;
                }

                // Get active assignment rules for this position type
                var rules = await GetAssignmentRulesAsync(application.PositionType);
                if (!rules.Any())
                {
                    _logger.LogWarning("No active assignment rules found for position type {PositionType}", application.PositionType);
                    return null;
                }

                // Use the highest priority rule
                var rule = rules.First();

                // Find available officer based on strategy
                var officer = await GetAvailableOfficerAsync(application.PositionType, rule.Strategy);
                if (officer == null)
                {
                    _logger.LogWarning("No available officer found for application {ApplicationId} using strategy {Strategy}", 
                        applicationId, rule.Strategy);
                    return null;
                }

                // Validate the assignment
                if (!await ValidateAssignmentAsync(applicationId, officer.Id))
                {
                    _logger.LogWarning("Assignment validation failed for application {ApplicationId} to officer {OfficerId}", 
                        applicationId, officer.Id);
                    return null;
                }

                // Deactivate previous assignments
                var previousAssignments = application.AssignmentHistories
                    .Where(h => h.IsActive)
                    .ToList();

                foreach (var prev in previousAssignments)
                {
                    prev.IsActive = false;
                    prev.InactivatedAt = DateTime.UtcNow;
                    prev.AssignmentDurationHours = (decimal)(DateTime.UtcNow - prev.AssignedDate).TotalHours;
                }

                // Calculate workload
                var currentWorkload = await CalculateWorkloadAsync(officer.Id);

                // Create new assignment history
                var assignmentHistory = new AssignmentHistory
                {
                    ApplicationId = applicationId,
                    PreviousOfficerId = application.AssignedJuniorEngineerId,
                    AssignedToOfficerId = officer.Id,
                    Action = assignedByAdminId != null ? AssignmentAction.ManuallyAssigned : AssignmentAction.AutoAssigned,
                    AssignedDate = DateTime.UtcNow,
                    Reason = assignedByAdminId != null ? "Manual assignment by admin" : $"Auto-assigned using {rule.Strategy} strategy",
                    AssignedByAdminId = assignedByAdminId,
                    AutoAssignmentRuleId = assignedByAdminId == null ? rule.Id : null,
                    OfficerWorkloadAtAssignment = currentWorkload,
                    StrategyUsed = rule.Strategy,
                    NotificationSent = false,
                    IsActive = true,
                    ApplicationStatusAtAssignment = application.Status,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AssignmentHistories.Add(assignmentHistory);

                // Update application
                application.AssignedJuniorEngineerId = officer.Id;
                application.AssignedToJEDate = DateTime.UtcNow;
                application.Status = ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING;

                // Update rule statistics
                rule.TimesApplied++;
                rule.LastAppliedAt = DateTime.UtcNow;
                if (rule.Strategy == AssignmentStrategy.RoundRobin)
                {
                    rule.LastRoundRobinIndex = officer.Id;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} assigned to officer {OfficerId} ({OfficerName}) using {Strategy} strategy",
                    applicationId, officer.Id, officer.FullName, rule.Strategy);

                // Reload assignment history to include navigation properties
                await _context.Entry(assignmentHistory).Reference(h => h.Application).LoadAsync();

                // Send notification asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendAssignmentNotificationAsync(assignmentHistory.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send assignment notification for assignment {AssignmentId}", assignmentHistory.Id);
                    }
                });

                return assignmentHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning application {ApplicationId}", applicationId);
                throw;
            }
        }

        public async Task<Officer?> GetAvailableOfficerAsync(PositionType positionType, AssignmentStrategy strategy)
        {
            try
            {
                // Get target officer role based on position type
                var targetRole = GetTargetOfficerRole(positionType);

                // Get all eligible officers
                var eligibleOfficers = await _context.Officers
                    .Where(o => o.Role == targetRole && o.IsActive)
                    .ToListAsync();

                if (!eligibleOfficers.Any())
                {
                    _logger.LogWarning("No eligible officers found for role {Role}", targetRole);
                    return null;
                }

                Officer? selectedOfficer = null;

                switch (strategy)
                {
                    case AssignmentStrategy.RoundRobin:
                        selectedOfficer = await GetOfficerByRoundRobinAsync(positionType, eligibleOfficers);
                        break;

                    case AssignmentStrategy.WorkloadBased:
                        selectedOfficer = await GetOfficerByWorkloadAsync(eligibleOfficers);
                        break;

                    case AssignmentStrategy.PriorityBased:
                        selectedOfficer = await GetOfficerByPriorityAsync(eligibleOfficers);
                        break;

                    case AssignmentStrategy.SkillBased:
                        selectedOfficer = await GetOfficerBySkillAsync(eligibleOfficers);
                        break;

                    case AssignmentStrategy.Manual:
                        // Manual strategy should not be used in auto-assignment
                        _logger.LogWarning("Manual assignment strategy cannot be used for auto-assignment");
                        return null;

                    default:
                        // Default to workload-based
                        selectedOfficer = await GetOfficerByWorkloadAsync(eligibleOfficers);
                        break;
                }

                return selectedOfficer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available officer for position type {PositionType} with strategy {Strategy}",
                    positionType, strategy);
                throw;
            }
        }

        public async Task<int> CalculateWorkloadAsync(int officerId)
        {
            try
            {
                // Count active applications assigned to this officer
                var workload = await _context.PositionApplications
                    .CountAsync(a => a.AssignedJuniorEngineerId == officerId &&
                                    a.Status >= ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING &&
                                    a.Status < ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING);

                return workload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating workload for officer {OfficerId}", officerId);
                throw;
            }
        }

        public async Task<List<AutoAssignmentRule>> GetAssignmentRulesAsync(PositionType positionType)
        {
            try
            {
                var now = DateTime.UtcNow;

                var rules = await _context.AutoAssignmentRules
                    .Where(r => r.PositionType == positionType &&
                               r.IsActive &&
                               (r.EffectiveFrom == null || r.EffectiveFrom <= now) &&
                               (r.EffectiveTo == null || r.EffectiveTo >= now))
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment rules for position type {PositionType}", positionType);
                throw;
            }
        }

        public async Task<bool> ValidateAssignmentAsync(int applicationId, int officerId)
        {
            try
            {
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return false;
                }

                var officer = await _context.Officers.FindAsync(officerId);
                if (officer == null || !officer.IsActive)
                {
                    return false;
                }

                // Check if officer role matches position type
                var expectedRole = GetTargetOfficerRole(application.PositionType);
                if (officer.Role != expectedRole)
                {
                    _logger.LogWarning("Officer {OfficerId} role {OfficerRole} does not match expected role {ExpectedRole} for position type {PositionType}",
                        officerId, officer.Role, expectedRole, application.PositionType);
                    return false;
                }

                // Check workload limits
                var rules = await GetAssignmentRulesAsync(application.PositionType);
                if (rules.Any())
                {
                    var rule = rules.First();
                    var currentWorkload = await CalculateWorkloadAsync(officerId);
                    if (currentWorkload >= rule.MaxWorkloadPerOfficer)
                    {
                        _logger.LogWarning("Officer {OfficerId} has reached maximum workload limit ({Workload}/{MaxWorkload})",
                            officerId, currentWorkload, rule.MaxWorkloadPerOfficer);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating assignment for application {ApplicationId} to officer {OfficerId}",
                    applicationId, officerId);
                return false;
            }
        }

        public async Task<AssignmentHistory> ReassignApplicationAsync(int applicationId, int newOfficerId, string reason, string reassignedByAdminId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignmentHistories)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    throw new InvalidOperationException($"Application {applicationId} not found");
                }

                if (!await ValidateAssignmentAsync(applicationId, newOfficerId))
                {
                    throw new InvalidOperationException($"Cannot reassign application {applicationId} to officer {newOfficerId}");
                }

                // Deactivate previous assignments
                var previousAssignments = application.AssignmentHistories
                    .Where(h => h.IsActive)
                    .ToList();

                foreach (var prev in previousAssignments)
                {
                    prev.IsActive = false;
                    prev.InactivatedAt = DateTime.UtcNow;
                    prev.AssignmentDurationHours = (decimal)(DateTime.UtcNow - prev.AssignedDate).TotalHours;
                }

                var currentWorkload = await CalculateWorkloadAsync(newOfficerId);

                // Create reassignment history
                var assignmentHistory = new AssignmentHistory
                {
                    ApplicationId = applicationId,
                    PreviousOfficerId = application.AssignedJuniorEngineerId,
                    AssignedToOfficerId = newOfficerId,
                    Action = AssignmentAction.Reassigned,
                    AssignedDate = DateTime.UtcNow,
                    Reason = reason,
                    AssignedByAdminId = reassignedByAdminId,
                    OfficerWorkloadAtAssignment = currentWorkload,
                    NotificationSent = false,
                    IsActive = true,
                    ApplicationStatusAtAssignment = application.Status,
                    CreatedDate = DateTime.UtcNow
                };

                _context.AssignmentHistories.Add(assignmentHistory);

                // Update application
                application.AssignedJuniorEngineerId = newOfficerId;
                application.AssignedToJEDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} reassigned from officer {PreviousOfficerId} to officer {NewOfficerId}",
                    applicationId, assignmentHistory.PreviousOfficerId, newOfficerId);

                // Send notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendAssignmentNotificationAsync(assignmentHistory.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reassignment notification for assignment {AssignmentId}", assignmentHistory.Id);
                    }
                });

                return assignmentHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning application {ApplicationId} to officer {OfficerId}",
                    applicationId, newOfficerId);
                throw;
            }
        }

        public async Task<List<AssignmentHistory>> GetAssignmentHistoryAsync(int applicationId)
        {
            try
            {
                return await _context.AssignmentHistories
                    .Include(h => h.AssignedToOfficer)
                    .Include(h => h.PreviousOfficer)
                    .Include(h => h.AutoAssignmentRule)
                    .Where(h => h.ApplicationId == applicationId)
                    .OrderByDescending(h => h.AssignedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment history for application {ApplicationId}", applicationId);
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetWorkloadStatisticsAsync(OfficerRole role)
        {
            try
            {
                var officers = await _context.Officers
                    .Where(o => o.Role == role && o.IsActive)
                    .ToListAsync();

                var statistics = new Dictionary<int, int>();

                foreach (var officer in officers)
                {
                    var workload = await CalculateWorkloadAsync(officer.Id);
                    statistics[officer.Id] = workload;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workload statistics for role {Role}", role);
                throw;
            }
        }

        public async Task<List<int>> GetApplicationsNeedingEscalationAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var applicationsNeedingEscalation = new List<int>();

                // Get all active assignment rules with escalation configured
                var rulesWithEscalation = await _context.AutoAssignmentRules
                    .Where(r => r.IsActive && r.EscalationTimeHours.HasValue)
                    .ToListAsync();

                foreach (var rule in rulesWithEscalation)
                {
                    var escalationThreshold = now.AddHours(-rule.EscalationTimeHours.Value);

                    var applications = await _context.PositionApplications
                        .Include(a => a.AssignmentHistories)
                        .Where(a => a.PositionType == rule.PositionType &&
                                   a.Status >= ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING &&
                                   a.Status < ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING &&
                                   a.AssignedToJEDate <= escalationThreshold)
                        .Select(a => a.Id)
                        .ToListAsync();

                    applicationsNeedingEscalation.AddRange(applications);
                }

                return applicationsNeedingEscalation.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications needing escalation");
                throw;
            }
        }

        public async Task<AssignmentHistory?> EscalateApplicationAsync(int applicationId, string escalationReason)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.AssignmentHistories)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found for escalation", applicationId);
                    return null;
                }

                var rules = await GetAssignmentRulesAsync(application.PositionType);
                var ruleWithEscalation = rules.FirstOrDefault(r => r.EscalationRole.HasValue);

                if (ruleWithEscalation == null)
                {
                    _logger.LogWarning("No escalation rule found for position type {PositionType}", application.PositionType);
                    return null;
                }

                // Find officer with escalation role
                var escalationOfficer = await _context.Officers
                    .Where(o => o.Role == ruleWithEscalation.EscalationRole.Value && o.IsActive)
                    .OrderBy(o => _context.PositionApplications
                        .Count(a => a.AssignedJuniorEngineerId == o.Id &&
                                   a.Status >= ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING &&
                                   a.Status < ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING))
                    .FirstOrDefaultAsync();

                if (escalationOfficer == null)
                {
                    _logger.LogWarning("No available officer found for escalation role {Role}", ruleWithEscalation.EscalationRole);
                    return null;
                }

                // Deactivate previous assignments
                var previousAssignments = application.AssignmentHistories
                    .Where(h => h.IsActive)
                    .ToList();

                foreach (var prev in previousAssignments)
                {
                    prev.IsActive = false;
                    prev.InactivatedAt = DateTime.UtcNow;
                    prev.AssignmentDurationHours = (decimal)(DateTime.UtcNow - prev.AssignedDate).TotalHours;
                }

                var currentWorkload = await CalculateWorkloadAsync(escalationOfficer.Id);

                // Create escalation assignment
                var assignmentHistory = new AssignmentHistory
                {
                    ApplicationId = applicationId,
                    PreviousOfficerId = application.AssignedJuniorEngineerId,
                    AssignedToOfficerId = escalationOfficer.Id,
                    Action = AssignmentAction.Transferred,
                    AssignedDate = DateTime.UtcNow,
                    Reason = $"Escalated: {escalationReason}",
                    AutoAssignmentRuleId = ruleWithEscalation.Id,
                    OfficerWorkloadAtAssignment = currentWorkload,
                    NotificationSent = false,
                    IsActive = true,
                    ApplicationStatusAtAssignment = application.Status,
                    AdminComments = $"Auto-escalated after {ruleWithEscalation.EscalationTimeHours} hours",
                    CreatedDate = DateTime.UtcNow
                };

                _context.AssignmentHistories.Add(assignmentHistory);

                // Update application
                application.AssignedJuniorEngineerId = escalationOfficer.Id;
                application.AssignedToJEDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Application {ApplicationId} escalated to officer {OfficerId} ({OfficerRole})",
                    applicationId, escalationOfficer.Id, escalationOfficer.Role);

                return assignmentHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating application {ApplicationId}", applicationId);
                throw;
            }
        }

        #region Private Helper Methods

        private OfficerRole GetTargetOfficerRole(PositionType positionType)
        {
            // Map position types to junior engineer roles
            return positionType switch
            {
                PositionType.Architect => OfficerRole.JuniorArchitect,
                PositionType.LicenceEngineer => OfficerRole.JuniorLicenceEngineer,
                PositionType.StructuralEngineer => OfficerRole.JuniorStructuralEngineer,
                PositionType.Supervisor1 => OfficerRole.JuniorSupervisor1,
                PositionType.Supervisor2 => OfficerRole.JuniorSupervisor2,
                _ => throw new ArgumentException($"Unknown position type: {positionType}")
            };
        }

        private async Task<Officer?> GetOfficerByRoundRobinAsync(PositionType positionType, List<Officer> eligibleOfficers)
        {
            var rules = await GetAssignmentRulesAsync(positionType);
            var roundRobinRule = rules.FirstOrDefault(r => r.Strategy == AssignmentStrategy.RoundRobin);

            if (roundRobinRule?.LastRoundRobinIndex != null)
            {
                // Find next officer after last assigned
                var lastIndex = eligibleOfficers.FindIndex(o => o.Id == roundRobinRule.LastRoundRobinIndex);
                if (lastIndex >= 0)
                {
                    var nextIndex = (lastIndex + 1) % eligibleOfficers.Count;
                    return eligibleOfficers[nextIndex];
                }
            }

            // Return first officer if no previous assignment
            return eligibleOfficers.FirstOrDefault();
        }

        private async Task<Officer?> GetOfficerByWorkloadAsync(List<Officer> eligibleOfficers)
        {
            var workloads = new Dictionary<int, int>();

            foreach (var officer in eligibleOfficers)
            {
                workloads[officer.Id] = await CalculateWorkloadAsync(officer.Id);
            }

            // Return officer with minimum workload
            var minWorkloadOfficerId = workloads.OrderBy(w => w.Value).First().Key;
            return eligibleOfficers.FirstOrDefault(o => o.Id == minWorkloadOfficerId);
        }

        private async Task<Officer?> GetOfficerByPriorityAsync(List<Officer> eligibleOfficers)
        {
            // Priority-based: combine workload and seniority
            // Lower workload + higher seniority = higher priority
            var scores = new Dictionary<int, decimal>();

            foreach (var officer in eligibleOfficers)
            {
                var workload = await CalculateWorkloadAsync(officer.Id);
                // Simple scoring: lower workload is better, weight = 10
                var workloadScore = (100 - workload) * 10;
                
                // Seniority: experience months, weight = 1
                var seniorityScore = officer.ExperienceMonths ?? 0;

                scores[officer.Id] = workloadScore + seniorityScore;
            }

            var highestScoreOfficerId = scores.OrderByDescending(s => s.Value).First().Key;
            return eligibleOfficers.FirstOrDefault(o => o.Id == highestScoreOfficerId);
        }

        private async Task<Officer?> GetOfficerBySkillAsync(List<Officer> eligibleOfficers)
        {
            // For now, use workload-based as skill matching requires additional metadata
            // Can be enhanced with officer specialization/certification data
            return await GetOfficerByWorkloadAsync(eligibleOfficers);
        }

        private async Task SendAssignmentNotificationAsync(int assignmentHistoryId)
        {
            var assignment = await _context.AssignmentHistories
                .Include(h => h.AssignedToOfficer)
                .Include(h => h.Application)
                .FirstOrDefaultAsync(h => h.Id == assignmentHistoryId);

            if (assignment == null || assignment.NotificationSent)
            {
                return;
            }

            var officer = assignment.AssignedToOfficer;
            var application = assignment.Application;

            if (officer != null && application != null && application.ApplicationNumber != null)
            {
                var applicantName = $"{application.FirstName} {application.LastName}";
                
                // Use existing notification service method
                await _notificationService.NotifyOfficerAssignmentAsync(
                    officer.Id,
                    application.ApplicationNumber,
                    application.Id,
                    application.PositionType.ToString(),
                    applicantName,
                    assignment.AssignedByAdminId ?? "System");

                assignment.NotificationSent = true;
                assignment.NotificationSentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}
