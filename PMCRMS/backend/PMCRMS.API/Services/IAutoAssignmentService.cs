using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service interface for auto-assignment of applications to Junior Engineers
    /// Supports multiple assignment strategies: RoundRobin, WorkloadBased, PriorityBased, SkillBased
    /// </summary>
    public interface IAutoAssignmentService
    {
        /// <summary>
        /// Assigns an application to an available Junior Engineer based on configured rules
        /// </summary>
        /// <param name="applicationId">ID of the application to assign</param>
        /// <param name="assignedByAdminId">Optional admin ID if manually triggered</param>
        /// <returns>Assignment history record if successful, null if no officer available</returns>
        Task<AssignmentHistory?> AssignApplicationAsync(int applicationId, string? assignedByAdminId = null);

        /// <summary>
        /// Finds the most suitable officer for an application based on position type and strategy
        /// </summary>
        /// <param name="positionType">Type of position (Architect, StructuralEngineer, etc.)</param>
        /// <param name="strategy">Assignment strategy to use</param>
        /// <returns>Selected officer if available, null otherwise</returns>
        Task<Officer?> GetAvailableOfficerAsync(PositionType positionType, AssignmentStrategy strategy);

        /// <summary>
        /// Calculates current workload for an officer (active assigned applications)
        /// </summary>
        /// <param name="officerId">ID of the officer</param>
        /// <returns>Number of active applications assigned to the officer</returns>
        Task<int> CalculateWorkloadAsync(int officerId);

        /// <summary>
        /// Gets all active assignment rules for a specific position type
        /// </summary>
        /// <param name="positionType">Type of position</param>
        /// <returns>List of active rules ordered by priority</returns>
        Task<List<AutoAssignmentRule>> GetAssignmentRulesAsync(PositionType positionType);

        /// <summary>
        /// Validates if an officer can be assigned to an application
        /// </summary>
        /// <param name="applicationId">ID of the application</param>
        /// <param name="officerId">ID of the officer</param>
        /// <returns>True if assignment is valid, false otherwise</returns>
        Task<bool> ValidateAssignmentAsync(int applicationId, int officerId);

        /// <summary>
        /// Reassigns an application from one officer to another
        /// </summary>
        /// <param name="applicationId">ID of the application</param>
        /// <param name="newOfficerId">ID of the new officer</param>
        /// <param name="reason">Reason for reassignment</param>
        /// <param name="reassignedByAdminId">Admin ID who performed the reassignment</param>
        /// <returns>New assignment history record</returns>
        Task<AssignmentHistory> ReassignApplicationAsync(int applicationId, int newOfficerId, string reason, string reassignedByAdminId);

        /// <summary>
        /// Gets assignment history for an application
        /// </summary>
        /// <param name="applicationId">ID of the application</param>
        /// <returns>List of all assignment history records</returns>
        Task<List<AssignmentHistory>> GetAssignmentHistoryAsync(int applicationId);

        /// <summary>
        /// Gets workload statistics for all officers by role
        /// </summary>
        /// <param name="role">Officer role to filter by</param>
        /// <returns>Dictionary of officer ID to workload count</returns>
        Task<Dictionary<int, int>> GetWorkloadStatisticsAsync(OfficerRole role);

        /// <summary>
        /// Checks if any applications need escalation (exceeded escalation time)
        /// </summary>
        /// <returns>List of application IDs that need escalation</returns>
        Task<List<int>> GetApplicationsNeedingEscalationAsync();

        /// <summary>
        /// Escalates an application to a higher role based on escalation rules
        /// </summary>
        /// <param name="applicationId">ID of the application</param>
        /// <param name="escalationReason">Reason for escalation</param>
        /// <returns>New assignment history record after escalation</returns>
        Task<AssignmentHistory?> EscalateApplicationAsync(int applicationId, string escalationReason);
    }
}
