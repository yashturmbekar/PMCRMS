using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly INotificationService _notificationService;

        public AppointmentService(
            PMCRMSDbContext context,
            ILogger<AppointmentService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<AppointmentResult> ScheduleAppointmentAsync(
            int applicationId,
            int scheduledByOfficerId,
            DateTime reviewDate,
            string contactPerson,
            string place,
            string roomNumber,
            string? comments = null)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Application not found",
                        Errors = new List<string> { "Application not found" }
                    };
                }

                var officer = await _context.Officers.FindAsync(scheduledByOfficerId);
                if (officer == null || !officer.IsActive)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "Invalid officer" }
                    };
                }

                if (reviewDate <= DateTime.UtcNow)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Review date must be in the future",
                        Errors = new List<string> { "Invalid review date" }
                    };
                }

                var isAvailable = await IsOfficerAvailableAsync(scheduledByOfficerId, reviewDate);
                if (!isAvailable)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Officer is not available at this time",
                        Errors = new List<string> { "Time slot conflict" }
                    };
                }

                var appointment = new Appointment
                {
                    ApplicationId = applicationId,
                    ScheduledByOfficerId = scheduledByOfficerId,
                    ReviewDate = reviewDate,
                    ContactPerson = contactPerson,
                    Place = place,
                    RoomNumber = roomNumber,
                    Comments = comments,
                    Status = AppointmentStatus.Scheduled,
                    CreatedBy = scheduledByOfficerId.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);

                if (application.Status == ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING)
                {
                    application.Status = ApplicationCurrentStatus.APPOINTMENT_SCHEDULED;
                    application.AppointmentScheduled = true;
                    application.AppointmentScheduledDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await SendAppointmentNotificationAsync(appointment, "scheduled");

                _logger.LogInformation(
                    "Appointment {AppointmentId} scheduled for application {ApplicationId} on {Date}",
                    appointment.Id, applicationId, reviewDate);

                return new AppointmentResult
                {
                    Success = true,
                    Message = "Appointment scheduled successfully",
                    AppointmentId = appointment.Id,
                    Appointment = appointment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment for application {ApplicationId}", applicationId);
                return new AppointmentResult
                {
                    Success = false,
                    Message = "Failed to schedule appointment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AppointmentResult> ConfirmAppointmentAsync(int appointmentId, string confirmedBy)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    return new AppointmentResult { Success = false, Message = "Appointment not found" };
                }

                if (appointment.Status != AppointmentStatus.Scheduled)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = $"Cannot confirm appointment with status {appointment.Status}"
                    };
                }

                appointment.Status = AppointmentStatus.Confirmed;
                appointment.ConfirmedAt = DateTime.UtcNow;
                appointment.UpdatedBy = confirmedBy;
                appointment.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await SendAppointmentNotificationAsync(appointment, "confirmed");

                _logger.LogInformation("Appointment {AppointmentId} confirmed by {User}", appointmentId, confirmedBy);

                return new AppointmentResult
                {
                    Success = true,
                    Message = "Appointment confirmed",
                    AppointmentId = appointment.Id,
                    Appointment = appointment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming appointment {AppointmentId}", appointmentId);
                return new AppointmentResult
                {
                    Success = false,
                    Message = "Failed to confirm appointment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AppointmentResult> CancelAppointmentAsync(
            int appointmentId,
            string cancelReason,
            string cancelledBy)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    return new AppointmentResult { Success = false, Message = "Appointment not found" };
                }

                if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = $"Cannot cancel appointment with status {appointment.Status}"
                    };
                }

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.CancellationReason = cancelReason;
                appointment.CancelledAt = DateTime.UtcNow;
                appointment.UpdatedBy = cancelledBy;
                appointment.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await SendAppointmentNotificationAsync(appointment, "cancelled");

                _logger.LogInformation(
                    "Appointment {AppointmentId} cancelled by {User}. Reason: {Reason}",
                    appointmentId, cancelledBy, cancelReason);

                return new AppointmentResult
                {
                    Success = true,
                    Message = "Appointment cancelled",
                    AppointmentId = appointment.Id,
                    Appointment = appointment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
                return new AppointmentResult
                {
                    Success = false,
                    Message = "Failed to cancel appointment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AppointmentResult> RescheduleAppointmentAsync(
            int appointmentId,
            DateTime newReviewDate,
            string rescheduleReason,
            string rescheduledBy)
        {
            try
            {
                var originalAppointment = await _context.Appointments
                    .Include(a => a.Application)
                    .Include(a => a.ScheduledByOfficer)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (originalAppointment == null)
                {
                    return new AppointmentResult { Success = false, Message = "Appointment not found" };
                }

                if (originalAppointment.Status == AppointmentStatus.Completed || 
                    originalAppointment.Status == AppointmentStatus.Cancelled)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = $"Cannot reschedule appointment with status {originalAppointment.Status}"
                    };
                }

                if (newReviewDate <= DateTime.UtcNow)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "New review date must be in the future"
                    };
                }

                var isAvailable = await IsOfficerAvailableInternalAsync(
                    originalAppointment.ScheduledByOfficerId,
                    newReviewDate,
                    excludeAppointmentId: appointmentId);

                if (!isAvailable)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Officer is not available at this time"
                    };
                }

                var newAppointment = new Appointment
                {
                    ApplicationId = originalAppointment.ApplicationId,
                    ScheduledByOfficerId = originalAppointment.ScheduledByOfficerId,
                    ReviewDate = newReviewDate,
                    ContactPerson = originalAppointment.ContactPerson,
                    Place = originalAppointment.Place,
                    RoomNumber = originalAppointment.RoomNumber,
                    Comments = $"Rescheduled from {originalAppointment.ReviewDate:yyyy-MM-dd HH:mm}. Reason: {rescheduleReason}",
                    Status = AppointmentStatus.Scheduled,
                    RescheduledFromAppointmentId = appointmentId,
                    CreatedBy = rescheduledBy,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(newAppointment);

                originalAppointment.Status = AppointmentStatus.Rescheduled;
                originalAppointment.RescheduledToAppointmentId = newAppointment.Id;
                originalAppointment.UpdatedBy = rescheduledBy;
                originalAppointment.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                originalAppointment.RescheduledToAppointmentId = newAppointment.Id;
                await _context.SaveChangesAsync();

                await SendAppointmentNotificationAsync(newAppointment, "rescheduled");

                _logger.LogInformation(
                    "Appointment {OriginalId} rescheduled to {NewId} for {NewDate}. Reason: {Reason}",
                    appointmentId, newAppointment.Id, newReviewDate, rescheduleReason);

                return new AppointmentResult
                {
                    Success = true,
                    Message = "Appointment rescheduled successfully",
                    AppointmentId = newAppointment.Id,
                    Appointment = newAppointment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
                return new AppointmentResult
                {
                    Success = false,
                    Message = "Failed to reschedule appointment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<AppointmentResult> CompleteAppointmentAsync(
            int appointmentId,
            string? completionNotes,
            string completedBy)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    return new AppointmentResult { Success = false, Message = "Appointment not found" };
                }

                if (appointment.Status != AppointmentStatus.Confirmed && 
                    appointment.Status != AppointmentStatus.Scheduled)
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = $"Cannot complete appointment with status {appointment.Status}"
                    };
                }

                appointment.Status = AppointmentStatus.Completed;
                appointment.CompletedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(completionNotes))
                {
                    appointment.Comments = appointment.Comments + "\n\nCompletion Notes: " + completionNotes;
                }

                appointment.UpdatedBy = completedBy;
                appointment.UpdatedDate = DateTime.UtcNow;

                if (appointment.Application != null)
                {
                    appointment.Application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} marked as completed", appointmentId);

                return new AppointmentResult
                {
                    Success = true,
                    Message = "Appointment completed",
                    AppointmentId = appointment.Id,
                    Appointment = appointment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
                return new AppointmentResult
                {
                    Success = false,
                    Message = "Failed to complete appointment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<Appointment>> GetAppointmentsForApplicationAsync(int applicationId)
        {
            return await _context.Appointments
                .Include(a => a.ScheduledByOfficer)
                .Include(a => a.RescheduledFromAppointment)
                .Include(a => a.RescheduledToAppointment)
                .Where(a => a.ApplicationId == applicationId)
                .OrderByDescending(a => a.ReviewDate)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAppointmentsForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.Application)
                .ThenInclude(app => app.User)
                .Where(a => a.ScheduledByOfficerId == officerId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.ReviewDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.ReviewDate <= endDate.Value);
            }

            return await query.OrderBy(a => a.ReviewDate).ToListAsync();
        }

        public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(int officerId, int daysAhead = 7)
        {
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(daysAhead);

            return await _context.Appointments
                .Include(a => a.Application)
                .ThenInclude(app => app.User)
                .Where(a => a.ScheduledByOfficerId == officerId &&
                           a.ReviewDate >= startDate &&
                           a.ReviewDate <= endDate &&
                           (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
                .OrderBy(a => a.ReviewDate)
                .ToListAsync();
        }

        public async Task<bool> IsOfficerAvailableAsync(
            int officerId,
            DateTime reviewDate,
            int durationMinutes = 60)
        {
            return await IsOfficerAvailableInternalAsync(officerId, reviewDate, durationMinutes, null);
        }

        private async Task<bool> IsOfficerAvailableInternalAsync(
            int officerId,
            DateTime reviewDate,
            int durationMinutes = 60,
            int? excludeAppointmentId = null)
        {
            var proposedStart = reviewDate;
            var proposedEnd = reviewDate.AddMinutes(durationMinutes);

            var conflictingAppointments = await _context.Appointments
                .Where(a => a.ScheduledByOfficerId == officerId &&
                           (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                           a.ReviewDate < proposedEnd &&
                           a.ReviewDate.AddMinutes(60) > proposedStart)
                .ToListAsync();

            if (excludeAppointmentId.HasValue)
            {
                conflictingAppointments = conflictingAppointments
                    .Where(a => a.Id != excludeAppointmentId.Value)
                    .ToList();
            }

            return !conflictingAppointments.Any();
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Application)
                .ThenInclude(app => app.User)
                .Include(a => a.ScheduledByOfficer)
                .Include(a => a.RescheduledFromAppointment)
                .Include(a => a.RescheduledToAppointment)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<int> SendAppointmentRemindersAsync(int hoursBeforeAppointment = 24)
        {
            var reminderCutoff = DateTime.UtcNow.AddHours(hoursBeforeAppointment);
            var now = DateTime.UtcNow;

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Application)
                .ThenInclude(app => app.User)
                .Include(a => a.ScheduledByOfficer)
                .Where(a => (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                           a.ReviewDate > now &&
                           a.ReviewDate <= reminderCutoff &&
                           !a.ReminderSent)
                .ToListAsync();

            int remindersSent = 0;

            foreach (var appointment in upcomingAppointments)
            {
                try
                {
                    await SendAppointmentNotificationAsync(appointment, "reminder");
                    
                    appointment.ReminderSent = true;
                    appointment.ReminderSentAt = DateTime.UtcNow;
                    
                    remindersSent++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}", appointment.Id);
                }
            }

            if (remindersSent > 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Sent {Count} appointment reminders", remindersSent);
            return remindersSent;
        }

        private async Task SendAppointmentNotificationAsync(Appointment appointment, string action)
        {
            try
            {
                if (appointment.Application == null)
                {
                    await _context.Entry(appointment)
                        .Reference(a => a.Application)
                        .Query()
                        .Include(app => app.User)
                        .LoadAsync();
                }

                if (appointment.ScheduledByOfficer == null)
                {
                    await _context.Entry(appointment)
                        .Reference(a => a.ScheduledByOfficer)
                        .LoadAsync();
                }

                var application = appointment.Application;
                var officer = appointment.ScheduledByOfficer;

                if (application?.User == null || officer == null)
                {
                    _logger.LogWarning("Cannot send notification for appointment {AppointmentId} - missing data", appointment.Id);
                    return;
                }

                _logger.LogInformation(
                    "Appointment {Action}: ID={AppointmentId}, Date={Date}, Officer={Officer}, Applicant={Applicant}",
                    action, appointment.Id, appointment.ReviewDate, officer.Name,
                    $"{application.FirstName} {application.LastName}");

                if (action == "scheduled")
                {
                    appointment.EmailNotificationSent = true;
                    appointment.SmsNotificationSent = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment notification for {AppointmentId}", appointment.Id);
            }
        }
    }
}
