using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    /// <summary>
    /// Represents an appointment scheduled by Junior Engineer for an applicant
    /// </summary>
    public class Appointment : BaseEntity
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? Purpose { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        public int ScheduledByOfficerId { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        public DateTime? CompletedDateTime { get; set; }

        [MaxLength(1000)]
        public string? CompletionNotes { get; set; }

        // Foreign Keys
        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("ScheduledByOfficerId")]
        public virtual Officer ScheduledByOfficer { get; set; } = null!;
    }

    public enum AppointmentStatus
    {
        Scheduled = 0,
        Completed = 1,
        Cancelled = 2,
        Rescheduled = 3
    }
}
