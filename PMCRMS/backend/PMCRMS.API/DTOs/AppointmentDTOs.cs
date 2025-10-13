using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    public class ScheduleAppointmentRequestDto
    {
        public int ApplicationId { get; set; }
        public DateTime ReviewDate { get; set; }
        public string ContactPerson { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class ConfirmAppointmentRequestDto
    {
        public int AppointmentId { get; set; }
    }

    public class CancelAppointmentRequestDto
    {
        public int AppointmentId { get; set; }
        public string CancelReason { get; set; } = string.Empty;
    }

    public class RescheduleAppointmentRequestDto
    {
        public int AppointmentId { get; set; }
        public DateTime NewReviewDate { get; set; }
        public string RescheduleReason { get; set; } = string.Empty;
        public string? Place { get; set; }
        public string? ContactPerson { get; set; }
        public string? RoomNumber { get; set; }
    }

    public class CompleteAppointmentRequestDto
    {
        public int AppointmentId { get; set; }
        public string? CompletionNotes { get; set; }
    }

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public int ScheduledByOfficerId { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public string ContactPerson { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public int? RescheduledFromAppointmentId { get; set; }
        public int? RescheduledToAppointmentId { get; set; }
        public bool EmailNotificationSent { get; set; }
        public bool SmsNotificationSent { get; set; }
        public bool ReminderSent { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AppointmentListDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public string Place { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public bool IsUpcoming { get; set; }
        public bool IsToday { get; set; }
    }

    public class CheckOfficerAvailabilityRequestDto
    {
        public int OfficerId { get; set; }
        public DateTime ReviewDate { get; set; }
        public int DurationMinutes { get; set; } = 60;
    }

    public class OfficerAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DateTime>? SuggestedTimes { get; set; }
    }

    public class AppointmentStatisticsDto
    {
        public int TotalAppointments { get; set; }
        public int ScheduledCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int RescheduledCount { get; set; }
        public int UpcomingCount { get; set; }
        public int TodayCount { get; set; }
        public int OverdueCount { get; set; }
    }
}
