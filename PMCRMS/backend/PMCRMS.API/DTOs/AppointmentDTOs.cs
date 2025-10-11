using System.ComponentModel.DataAnnotations;
using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Request to schedule an appointment
    /// </summary>
    public class ScheduleAppointmentRequest
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
    }

    /// <summary>
    /// Request to update appointment status
    /// </summary>
    public class UpdateAppointmentRequest
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; }

        public DateTime? CompletedDateTime { get; set; }

        [MaxLength(1000)]
        public string? CompletionNotes { get; set; }
    }

    /// <summary>
    /// Appointment response DTO
    /// </summary>
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public DateTime ScheduledDateTime { get; set; }
        public string? Location { get; set; }
        public string? Purpose { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ScheduledByOfficerName { get; set; } = string.Empty;
        public DateTime? CompletedDateTime { get; set; }
        public string? CompletionNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
