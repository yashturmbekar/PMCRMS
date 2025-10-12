using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum AppointmentStatus
    {
        Scheduled = 0,
        Confirmed = 1,
        Completed = 2,
        Cancelled = 3,
        Rescheduled = 4
    }

    /// <summary>
    /// Represents an appointment scheduled by Junior Engineer for an applicant
    /// </summary>
    public class Appointment : BaseEntity
    {
        /// <summary>
        /// Reference to the PositionApplication
        /// </summary>
        [Required]
        public int ApplicationId { get; set; }

        /// <summary>
        /// Scheduled date and time for the appointment
        /// </summary>
        [Required]
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// Contact person for the appointment
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ContactPerson { get; set; } = string.Empty;

        /// <summary>
        /// Place/Location of the appointment
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Place { get; set; } = string.Empty;

        /// <summary>
        /// Room number for the appointment
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>
        /// Additional comments or instructions
        /// </summary>
        [MaxLength(2000)]
        public string? Comments { get; set; }

        /// <summary>
        /// Current status of the appointment
        /// </summary>
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        /// <summary>
        /// Officer who scheduled the appointment (Junior Engineer)
        /// </summary>
        [Required]
        public int ScheduledByOfficerId { get; set; }

        /// <summary>
        /// Confirmation date when applicant confirmed the appointment
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// Completion date when the appointment was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Cancellation reason if appointment was cancelled
        /// </summary>
        [MaxLength(1000)]
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Date when appointment was cancelled
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// If rescheduled, reference to the new appointment
        /// </summary>
        public int? RescheduledToAppointmentId { get; set; }

        /// <summary>
        /// If this is a rescheduled appointment, reference to original
        /// </summary>
        public int? RescheduledFromAppointmentId { get; set; }

        /// <summary>
        /// Email notification sent flag
        /// </summary>
        public bool EmailNotificationSent { get; set; } = false;

        /// <summary>
        /// SMS notification sent flag
        /// </summary>
        public bool SmsNotificationSent { get; set; } = false;

        /// <summary>
        /// Reminder notification sent flag
        /// </summary>
        public bool ReminderSent { get; set; } = false;

        /// <summary>
        /// Date when reminder was sent
        /// </summary>
        public DateTime? ReminderSentAt { get; set; }

        // Navigation Properties
        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("ScheduledByOfficerId")]
        public virtual Officer ScheduledByOfficer { get; set; } = null!;

        // Self-referencing relationships configured in DbContext via Fluent API
        // Do not use [ForeignKey] here to avoid conflicts with Fluent API configuration
        public virtual Appointment? RescheduledToAppointment { get; set; }
        public virtual Appointment? RescheduledFromAppointment { get; set; }
    }
}
