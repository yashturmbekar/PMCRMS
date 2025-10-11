using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum AssignmentAction
    {
        AutoAssigned = 0,        // Automatically assigned by system
        ManuallyAssigned = 1,    // Manually assigned by admin
        Reassigned = 2,          // Reassigned to different officer
        Unassigned = 3,          // Removed from officer's workload
        Transferred = 4          // Transferred to different department/role
    }

    /// <summary>
    /// Tracks the history of application assignments to officers
    /// </summary>
    public class AssignmentHistory : BaseEntity
    {
        /// <summary>
        /// Reference to the PositionApplication
        /// </summary>
        [Required]
        public int ApplicationId { get; set; }

        /// <summary>
        /// Previous officer ID (null if first assignment)
        /// </summary>
        public int? PreviousOfficerId { get; set; }

        /// <summary>
        /// Newly assigned officer ID
        /// </summary>
        [Required]
        public int AssignedToOfficerId { get; set; }

        /// <summary>
        /// Type of assignment action
        /// </summary>
        public AssignmentAction Action { get; set; } = AssignmentAction.AutoAssigned;

        /// <summary>
        /// Date and time when assignment was made
        /// </summary>
        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Reason for assignment/reassignment
        /// </summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>
        /// Admin who performed manual assignment (null for auto-assignment)
        /// </summary>
        public string? AssignedByAdminId { get; set; }

        /// <summary>
        /// Auto-assignment rule that was applied (null for manual assignment)
        /// </summary>
        public int? AutoAssignmentRuleId { get; set; }

        /// <summary>
        /// Workload of the officer at time of assignment (number of pending applications)
        /// </summary>
        public int? OfficerWorkloadAtAssignment { get; set; }

        /// <summary>
        /// Strategy used for assignment
        /// </summary>
        public AssignmentStrategy? StrategyUsed { get; set; }

        /// <summary>
        /// Priority score calculated during assignment
        /// </summary>
        public decimal? PriorityScore { get; set; }

        /// <summary>
        /// Whether notification was sent to officer
        /// </summary>
        public bool NotificationSent { get; set; } = false;

        /// <summary>
        /// Notification sent date
        /// </summary>
        public DateTime? NotificationSentAt { get; set; }

        /// <summary>
        /// Whether officer accepted the assignment
        /// </summary>
        public bool? OfficerAccepted { get; set; }

        /// <summary>
        /// Date when officer accepted/acknowledged
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// IP address from which assignment was made
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent/browser details
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Application status at time of assignment
        /// </summary>
        public ApplicationCurrentStatus? ApplicationStatusAtAssignment { get; set; }

        /// <summary>
        /// Whether this is still the active assignment
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when assignment became inactive (reassigned/unassigned)
        /// </summary>
        public DateTime? InactivatedAt { get; set; }

        /// <summary>
        /// Duration officer held this assignment (in hours)
        /// </summary>
        public decimal? AssignmentDurationHours { get; set; }

        /// <summary>
        /// Comments from admin during manual assignment
        /// </summary>
        [MaxLength(2000)]
        public string? AdminComments { get; set; }

        /// <summary>
        /// Additional metadata in JSON format
        /// </summary>
        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        // Navigation Properties
        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("PreviousOfficerId")]
        public virtual Officer? PreviousOfficer { get; set; }

        [ForeignKey("AssignedToOfficerId")]
        public virtual Officer AssignedToOfficer { get; set; } = null!;

        [ForeignKey("AutoAssignmentRuleId")]
        public virtual AutoAssignmentRule? AutoAssignmentRule { get; set; }
    }
}
