using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum AssignmentStrategy
    {
        RoundRobin = 0,          // Assign to officers in rotation
        WorkloadBased = 1,       // Assign to officer with least workload
        PriorityBased = 2,       // Based on officer priority/seniority
        SkillBased = 3,          // Based on officer expertise/skills
        Manual = 4               // Manual assignment by admin
    }

    /// <summary>
    /// Represents auto-assignment rules for routing applications to appropriate officers
    /// </summary>
    public class AutoAssignmentRule : BaseEntity
    {
        /// <summary>
        /// Position type this rule applies to (Architect, Structural Engineer, etc.)
        /// </summary>
        [Required]
        public PositionType PositionType { get; set; }

        /// <summary>
        /// Target officer role to assign to (JuniorArchitect, JuniorStructural, etc.)
        /// </summary>
        [Required]
        public OfficerRole TargetOfficerRole { get; set; }

        /// <summary>
        /// Assignment strategy to use
        /// </summary>
        public AssignmentStrategy Strategy { get; set; } = AssignmentStrategy.WorkloadBased;

        /// <summary>
        /// Priority order for this rule (lower number = higher priority)
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Maximum workload (number of pending applications) per officer
        /// </summary>
        public int MaxWorkloadPerOfficer { get; set; } = 50;

        /// <summary>
        /// Minimum experience required (in months) for officers to receive assignments
        /// </summary>
        public int? MinimumExperienceMonths { get; set; }

        /// <summary>
        /// Whether this rule is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Effective start date for this rule
        /// </summary>
        public DateTime? EffectiveFrom { get; set; }

        /// <summary>
        /// Effective end date for this rule
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// Additional conditions in JSON format for complex routing logic
        /// Format: {"region": "North", "applicationType": "New Construction"}
        /// </summary>
        [Column(TypeName = "text")]
        public string? Conditions { get; set; }

        /// <summary>
        /// Description of the rule
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Rule created by admin ID
        /// </summary>
        public string? CreatedByAdminId { get; set; }

        /// <summary>
        /// Rule last modified by admin ID
        /// </summary>
        public string? ModifiedByAdminId { get; set; }

        /// <summary>
        /// Auto-assign immediately on application submission
        /// </summary>
        public bool AutoAssignOnSubmission { get; set; } = true;

        /// <summary>
        /// Send notification to assigned officer
        /// </summary>
        public bool SendNotification { get; set; } = true;

        /// <summary>
        /// Notification template to use
        /// </summary>
        [MaxLength(200)]
        public string? NotificationTemplate { get; set; }

        /// <summary>
        /// Escalation time in hours if officer doesn't respond
        /// </summary>
        public int? EscalationTimeHours { get; set; }

        /// <summary>
        /// Escalate to role if no response
        /// </summary>
        public OfficerRole? EscalationRole { get; set; }

        /// <summary>
        /// Number of times this rule has been applied
        /// </summary>
        public int TimesApplied { get; set; } = 0;

        /// <summary>
        /// Last time this rule was applied
        /// </summary>
        public DateTime? LastAppliedAt { get; set; }

        /// <summary>
        /// Last round-robin officer index (for round-robin strategy)
        /// </summary>
        public int? LastRoundRobinIndex { get; set; }

        /// <summary>
        /// Additional metadata in JSON format
        /// </summary>
        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        // Navigation Properties
        public virtual ICollection<AssignmentHistory> AssignmentHistories { get; set; } = new List<AssignmentHistory>();
    }
}
