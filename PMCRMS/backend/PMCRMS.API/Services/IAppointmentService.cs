using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for managing appointment scheduling between Junior Engineers and applicants
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// Schedule a new appointment for application review
        /// </summary>
        Task<AppointmentResult> ScheduleAppointmentAsync(
            int applicationId,
            int scheduledByOfficerId,
            DateTime reviewDate,
            string contactPerson,
            string place,
            string roomNumber,
            string? comments = null);

        /// <summary>
        /// Confirm an appointment
        /// </summary>
        Task<AppointmentResult> ConfirmAppointmentAsync(int appointmentId, string confirmedBy);

        /// <summary>
        /// Cancel an appointment
        /// </summary>
        Task<AppointmentResult> CancelAppointmentAsync(
            int appointmentId,
            string cancelReason,
            string cancelledBy);

        /// <summary>
        /// Reschedule an existing appointment
        /// </summary>
        Task<AppointmentResult> RescheduleAppointmentAsync(
            int appointmentId,
            DateTime newReviewDate,
            string rescheduleReason,
            string rescheduledBy,
            string? place = null,
            string? contactPerson = null,
            string? roomNumber = null);

        /// <summary>
        /// Mark appointment as completed
        /// </summary>
        Task<AppointmentResult> CompleteAppointmentAsync(
            int appointmentId,
            string? completionNotes,
            string completedBy);

        /// <summary>
        /// Get all appointments for an application
        /// </summary>
        Task<List<Appointment>> GetAppointmentsForApplicationAsync(int applicationId);

        /// <summary>
        /// Get all appointments for an officer
        /// </summary>
        Task<List<Appointment>> GetAppointmentsForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Get upcoming appointments for an officer
        /// </summary>
        Task<List<Appointment>> GetUpcomingAppointmentsAsync(int officerId, int daysAhead = 7);

        /// <summary>
        /// Check if an officer is available at a given time
        /// </summary>
        Task<bool> IsOfficerAvailableAsync(
            int officerId,
            DateTime reviewDate,
            int durationMinutes = 60);

        /// <summary>
        /// Get appointment by ID with full details
        /// </summary>
        Task<Appointment?> GetAppointmentByIdAsync(int appointmentId);

        /// <summary>
        /// Send appointment reminder notifications
        /// </summary>
        Task<int> SendAppointmentRemindersAsync(int hoursBeforeAppointment = 24);
    }

    /// <summary>
    /// Result of an appointment operation
    /// </summary>
    public class AppointmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
