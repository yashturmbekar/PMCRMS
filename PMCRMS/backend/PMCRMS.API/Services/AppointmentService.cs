using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AppointmentService(
            PMCRMSDbContext context,
            ILogger<AppointmentService> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<AppointmentResult> ScheduleAppointmentAsync(
            int applicationId,
            int scheduledByOfficerId,
            DateTime reviewDate,
            string? contactPerson,
            string? place,
            string? roomNumber,
            string? comments = null)
        {
            try
            {
                _logger.LogInformation(
                    "ScheduleAppointmentAsync called - ApplicationId: {ApplicationId}, OfficerId: {OfficerId}, ReviewDate: {ReviewDate}, ContactPerson: {ContactPerson}, Place: {Place}, RoomNumber: {RoomNumber}",
                    applicationId, scheduledByOfficerId, reviewDate, contactPerson, place, roomNumber);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(contactPerson))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Contact person is required",
                        Errors = new List<string> { "Contact person cannot be empty" }
                    };
                }

                if (string.IsNullOrWhiteSpace(place))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Place is required",
                        Errors = new List<string> { "Place cannot be empty" }
                    };
                }

                if (string.IsNullOrWhiteSpace(roomNumber))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Room number is required",
                        Errors = new List<string> { "Room number cannot be empty" }
                    };
                }

                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found", applicationId);
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Application not found",
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Validate that the review date is not in the past
                // Frontend sends datetime in format: YYYY-MM-DDTHH:mm:ss (without timezone)
                // ASP.NET Core deserializes this as UTC by default
                // We should use the datetime as-is without any conversion
                var now = DateTime.UtcNow;
                
                // Treat the incoming datetime as UTC (which is how ASP.NET Core deserializes it)
                var reviewDateUtc = DateTime.SpecifyKind(reviewDate, DateTimeKind.Utc);

                if (reviewDateUtc < now)
                {
                    _logger.LogWarning("Attempted to schedule appointment in the past. ReviewDate: {ReviewDate}, Current: {Now}", 
                        reviewDateUtc, now);
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Cannot schedule an appointment in the past. Please select a future date and time.",
                        Errors = new List<string> { "Review date cannot be in the past" }
                    };
                }

                _logger.LogInformation("Creating appointment entity for application {ApplicationId}", applicationId);

                // Use the datetime as-is - frontend sends local time but ASP.NET deserializes it as UTC
                // We just need to mark it as UTC for consistency
                var utcReviewDate = DateTime.SpecifyKind(reviewDate, DateTimeKind.Utc);

                var appointment = new Appointment
                {
                    ApplicationId = applicationId,
                    ScheduledByOfficerId = scheduledByOfficerId,
                    ReviewDate = utcReviewDate,
                    ContactPerson = contactPerson,
                    Place = place,
                    RoomNumber = roomNumber,
                    Comments = comments,
                    Status = AppointmentStatus.Scheduled,
                    CreatedBy = scheduledByOfficerId.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                _logger.LogInformation("Appointment entity added to context. Now updating application status...");

                if (application.Status == ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING)
                {
                    application.Status = ApplicationCurrentStatus.APPOINTMENT_SCHEDULED;
                    application.JEAppointmentScheduled = true;
                    application.JEAppointmentScheduledDate = DateTime.UtcNow;
                    _logger.LogInformation("Application {ApplicationId} status updated to APPOINTMENT_SCHEDULED", applicationId);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync completed. Appointment {AppointmentId} saved to database", appointment.Id);

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
            string rescheduledBy,
            string? place = null,
            string? contactPerson = null,
            string? roomNumber = null)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(rescheduleReason))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Reschedule reason is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(place))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Location is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(contactPerson))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Contact person is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(roomNumber))
                {
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Room number is required"
                    };
                }

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

                // Validate that the new review date is not in the past
                // Frontend sends datetime in format: YYYY-MM-DDTHH:mm:ss (without timezone)
                // ASP.NET Core deserializes this as UTC by default
                // We should use the datetime as-is without any conversion
                var now = DateTime.UtcNow;
                
                // Treat the incoming datetime as UTC (which is how ASP.NET Core deserializes it)
                var newReviewDateUtc = DateTime.SpecifyKind(newReviewDate, DateTimeKind.Utc);

                if (newReviewDateUtc < now)
                {
                    _logger.LogWarning("Attempted to reschedule appointment to a past date. NewReviewDate: {NewReviewDate}, Current: {Now}", 
                        newReviewDateUtc, now);
                    return new AppointmentResult
                    {
                        Success = false,
                        Message = "Cannot reschedule an appointment to a past date. Please select a future date and time.",
                        Errors = new List<string> { "New review date cannot be in the past" }
                    };
                }

                // Use the datetime as-is - frontend sends local time but ASP.NET deserializes it as UTC
                // We just need to mark it as UTC for consistency
                var utcNewReviewDate = DateTime.SpecifyKind(newReviewDate, DateTimeKind.Utc);

                var newAppointment = new Appointment
                {
                    ApplicationId = originalAppointment.ApplicationId,
                    ScheduledByOfficerId = originalAppointment.ScheduledByOfficerId,
                    ReviewDate = utcNewReviewDate,
                    ContactPerson = contactPerson ?? originalAppointment.ContactPerson,
                    Place = place ?? originalAppointment.Place,
                    RoomNumber = roomNumber ?? originalAppointment.RoomNumber,
                    Comments = rescheduleReason, // Use user's reschedule reason as comments
                    Status = AppointmentStatus.Scheduled,
                    RescheduledFromAppointmentId = appointmentId,
                    CreatedBy = rescheduledBy,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(newAppointment);
                
                // Save the new appointment first to get its ID
                await _context.SaveChangesAsync();

                // Now update the original appointment with the new appointment's ID
                originalAppointment.Status = AppointmentStatus.Rescheduled;
                originalAppointment.RescheduledToAppointmentId = newAppointment.Id;
                originalAppointment.UpdatedBy = rescheduledBy;
                originalAppointment.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload the new appointment with all required navigation properties for email
                var newAppointmentWithDetails = await _context.Appointments
                    .Include(a => a.Application)
                        .ThenInclude(app => app.User)
                    .Include(a => a.ScheduledByOfficer)
                    .FirstOrDefaultAsync(a => a.Id == newAppointment.Id);

                if (newAppointmentWithDetails != null)
                {
                    await SendAppointmentNotificationAsync(newAppointmentWithDetails, "rescheduled");
                }

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

                // Send email with appointment details
                var subject = action switch
                {
                    "scheduled" => "Appointment Scheduled for Document Verification",
                    "confirmed" => "Appointment Confirmed",
                    "cancelled" => "Appointment Cancelled",
                    "rescheduled" => "Appointment Rescheduled",
                    "reminder" => "Reminder: Upcoming Appointment",
                    _ => "Appointment Update"
                };

                var emailBody = action switch
                {
                    "scheduled" => BuildScheduledEmailBody(application, appointment, officer),
                    "confirmed" => BuildConfirmedEmailBody(application, appointment, officer),
                    "cancelled" => BuildCancelledEmailBody(application, appointment, officer),
                    "rescheduled" => BuildRescheduledEmailBody(application, appointment, officer),
                    "reminder" => BuildReminderEmailBody(application, appointment, officer),
                    _ => BuildScheduledEmailBody(application, appointment, officer)
                };

                await _emailService.SendEmailAsync(
                    application.EmailAddress,
                    subject,
                    emailBody
                );

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

        private string BuildScheduledEmailBody(PositionApplication application, Appointment appointment, Officer officer)
        {
            var appointmentDate = appointment.ReviewDate.ToLocalTime();
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://pmcrms.punemunicipal.gov.in";
            
            // Get logo as base64
            var logoPath = Path.Combine("wwwroot", "Images", "Certificate", "pmc-logo.png");
            var logoDataUri = "";
            if (File.Exists(logoPath))
            {
                var imageBytes = File.ReadAllBytes(logoPath);
                logoDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
            }
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .logo-container {{
            margin-bottom: 15px;
        }}
        .badge {{
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }}
        .success-badge {{
            background-color: #10b981;
            color: white;
            display: inline-block;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
            margin: 15px 0;
        }}
        .header h1 {{
            margin: 10px 0 5px 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }}
        .info-box {{
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
        }}
        .info-row {{
            display: flex;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: bold;
            color: #0c4a6e;
            min-width: 180px;
        }}
        .info-value {{
            color: #333;
        }}
        .highlight-box {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .instructions-box {{
            background-color: #f0fdf4;
            border: 1px solid #86efac;
            padding: 15px;
            margin: 20px 0;
            border-radius: 6px;
        }}
        .instructions-box h3 {{
            color: #166534;
            margin-top: 0;
        }}
        .instructions-box ul {{
            margin: 10px 0;
            padding-left: 20px;
        }}
        .instructions-box li {{
            margin: 8px 0;
            color: #333;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .calendar-icon {{
            font-size: 48px;
            color: #0c4a6e;
            text-align: center;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{logoDataUri}' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        <div class='content'>
            <div class='calendar-icon'>📅</div>
            <div class='success-badge'>Appointment Scheduled for Document Verification</div>
            
            <h2>Dear {application.FirstName} {application.LastName},</h2>
            <p>An appointment has been scheduled for document verification and site inspection for your building permit application.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #0c4a6e;'>📋 Appointment Details</h3>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{application.ApplicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Date & Time:</div>
                    <div class='info-value'><strong>{appointmentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Location:</div>
                    <div class='info-value'>{appointment.Place ?? "PMC Office"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Room Number:</div>
                    <div class='info-value'>{appointment.RoomNumber ?? "TBD"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Contact Person:</div>
                    <div class='info-value'>{appointment.ContactPerson ?? "PMC Officer"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Scheduled By:</div>
                    <div class='info-value'>{officer?.Name ?? "PMC Officer"} (Junior Engineer)</div>
                </div>
            </div>

            {(string.IsNullOrEmpty(appointment.Comments) ? "" : $@"
            <div class='highlight-box'>
                <strong style='color: #92400e;'>📌 Additional Instructions:</strong>
                <p style='margin: 10px 0 0 0;'>{appointment.Comments}</p>
            </div>")}

            <div class='instructions-box'>
                <h3>📄 What to Bring:</h3>
                <ul>
                    <li><strong>All original documents</strong> as per your application</li>
                    <li><strong>Valid government-issued photo ID</strong> (Aadhar Card/Passport/Driving License)</li>
                    <li><strong>A copy of your application form</strong></li>
                    <li>Any <strong>additional documents</strong> requested by the officer</li>
                </ul>
            </div>

            <div class='highlight-box'>
                <strong style='color: #92400e;'>⏰ Important Note:</strong>
                <p style='margin: 10px 0 0 0;'>Please arrive <strong>10 minutes before</strong> your scheduled time. If you need to reschedule, please contact us at least <strong>24 hours in advance</strong>.</p>
            </div>

            <p style='margin-top: 25px;'>For any queries, please contact the PMC office or reply to this email.</p>

            <div class='footer'>
                <p><strong>PMCRMS Team</strong></p>
                <p>Pune Municipal Corporation</p>
                <p style='margin-top: 10px; font-size: 11px;'>This is an automated email. Please do not reply directly to this message.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildConfirmedEmailBody(PositionApplication application, Appointment appointment, Officer officer)
        {
            var appointmentDate = appointment.ReviewDate.ToLocalTime();
            
            var logoPath = Path.Combine("wwwroot", "Images", "Certificate", "pmc-logo.png");
            var logoDataUri = "";
            if (File.Exists(logoPath))
            {
                var imageBytes = File.ReadAllBytes(logoPath);
                logoDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
            }
            
            return $@"
Dear {application.FirstName} {application.LastName},

Your appointment has been confirmed.

<strong>Confirmed Appointment Details:</strong>

Application Number: {application.ApplicationNumber}
Date & Time: {appointmentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}
Location: {appointment.Place}
Room Number: {appointment.RoomNumber}
Contact Person: {appointment.ContactPerson}

Please arrive on time with all required documents.

Best regards,
PMCRMS Team
Pune Municipal Corporation";
        }

        private string BuildCancelledEmailBody(PositionApplication application, Appointment appointment, Officer officer)
        {
            var logoPath = Path.Combine("wwwroot", "Images", "Certificate", "pmc-logo.png");
            var logoDataUri = "";
            if (File.Exists(logoPath))
            {
                var imageBytes = File.ReadAllBytes(logoPath);
                logoDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
            }
            
            return $@"
Dear {application.FirstName} {application.LastName},

Your appointment scheduled for {appointment.ReviewDate.ToLocalTime():MMMM dd, yyyy 'at' hh:mm tt} has been cancelled.

Application Number: {application.ApplicationNumber}
{(string.IsNullOrEmpty(appointment.CancellationReason) ? "" : $"Reason: {appointment.CancellationReason}")}

A new appointment will be scheduled shortly. You will receive a separate notification.

Best regards,
PMCRMS Team
Pune Municipal Corporation";
        }

        private string BuildRescheduledEmailBody(PositionApplication application, Appointment appointment, Officer officer)
        {
            var appointmentDate = appointment.ReviewDate.ToLocalTime();
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://pmcrms.punemunicipal.gov.in";
            
            var logoPath = Path.Combine("wwwroot", "Images", "Certificate", "pmc-logo.png");
            var logoDataUri = "";
            if (File.Exists(logoPath))
            {
                var imageBytes = File.ReadAllBytes(logoPath);
                logoDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
            }
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #0c4a6e;
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .logo-container {{
            margin-bottom: 15px;
        }}
        .badge {{
            background-color: #f59e0b;
            color: white;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            margin-top: 8px;
            letter-spacing: 0.5px;
        }}
        .success-badge {{
            background-color: #10b981;
            color: white;
            display: inline-block;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
            margin: 15px 0;
        }}
        .header h1 {{
            margin: 10px 0 5px 0;
            font-size: 24px;
        }}
        .header p {{
            margin: 5px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 8px 8px;
        }}
        .info-box {{
            background-color: #f0f9ff;
            border: 2px solid #0c4a6e;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
        }}
        .info-row {{
            display: flex;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: bold;
            color: #0c4a6e;
            min-width: 180px;
        }}
        .info-value {{
            color: #333;
        }}
        .highlight-box {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .instructions-box {{
            background-color: #f0fdf4;
            border: 1px solid #86efac;
            padding: 15px;
            margin: 20px 0;
            border-radius: 6px;
        }}
        .instructions-box h3 {{
            color: #166534;
            margin-top: 0;
        }}
        .instructions-box ul {{
            margin: 10px 0;
            padding-left: 20px;
        }}
        .instructions-box li {{
            margin: 8px 0;
            color: #333;
        }}
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .calendar-icon {{
            font-size: 48px;
            color: #0c4a6e;
            text-align: center;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{logoDataUri}' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        <div class='content'>
            <div class='calendar-icon'>📅</div>
            <div class='success-badge'>Appointment Rescheduled</div>
            
            <h2>Dear {application.FirstName} {application.LastName},</h2>
            <p>Your appointment has been rescheduled to a new date and time. <strong>New Appointment Details:</strong> Application Number: <strong>{application.ApplicationNumber}</strong> Date & Time: <strong>{appointmentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</strong> Location: {appointment.Place ?? "PMC Office"} Room Number: {appointment.RoomNumber ?? "TBD"} Contact Person: {appointment.ContactPerson ?? "PMC Officer"}</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #0c4a6e;'>📋 Appointment Details</h3>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'><strong>{application.ApplicationNumber}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Date & Time:</div>
                    <div class='info-value'><strong>{appointmentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</strong></div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Location:</div>
                    <div class='info-value'>{appointment.Place ?? "PMC Office"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Room Number:</div>
                    <div class='info-value'>{appointment.RoomNumber ?? "TBD"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Contact Person:</div>
                    <div class='info-value'>{appointment.ContactPerson ?? "PMC Officer"}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Rescheduled By:</div>
                    <div class='info-value'>{officer?.Name ?? "PMC Officer"} (Junior Engineer)</div>
                </div>
            </div>

            {(string.IsNullOrEmpty(appointment.Comments) ? "" : $@"
            <div class='highlight-box'>
                <strong style='color: #92400e;'>📌 Additional Instructions:</strong>
                <p style='margin: 10px 0 0 0;'>{appointment.Comments}</p>
            </div>")}

            <div class='instructions-box'>
                <h3>📄 What to Bring:</h3>
                <ul>
                    <li><strong>All original documents</strong> as per your application</li>
                    <li><strong>Valid government-issued photo ID</strong> (Aadhar Card/Passport/Driving License)</li>
                    <li><strong>A copy of your application form</strong></li>
                    <li>Any <strong>additional documents</strong> requested by the officer</li>
                </ul>
            </div>

            <div class='highlight-box'>
                <strong style='color: #92400e;'>⏰ Important Note:</strong>
                <p style='margin: 10px 0 0 0;'>Please arrive <strong>10 minutes before</strong> your scheduled time. If you need to reschedule again, please contact us at least <strong>24 hours in advance</strong>.</p>
            </div>

            <p style='margin-top: 25px;'>For any queries, please contact the PMC office or reply to this email.</p>

            <div class='footer'>
                <p><strong>PMCRMS Team</strong></p>
                <p>Pune Municipal Corporation</p>
                <p style='margin-top: 10px; font-size: 11px;'>This is an automated email. Please do not reply directly to this message.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildReminderEmailBody(PositionApplication application, Appointment appointment, Officer officer)
        {
            var appointmentDate = appointment.ReviewDate.ToLocalTime();
            var hoursUntil = (appointment.ReviewDate - DateTime.UtcNow).TotalHours;
            
            var logoPath = Path.Combine("wwwroot", "Images", "Certificate", "pmc-logo.png");
            var logoDataUri = "";
            if (File.Exists(logoPath))
            {
                var imageBytes = File.ReadAllBytes(logoPath);
                logoDataUri = $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
            }
            
            return $@"
Dear {application.FirstName} {application.LastName},

This is a reminder that you have an upcoming appointment in approximately {(int)hoursUntil} hours.

<strong>Appointment Details:</strong>

Application Number: {application.ApplicationNumber}
Date & Time: {appointmentDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}
Location: {appointment.Place}
Room Number: {appointment.RoomNumber}
Contact Person: {appointment.ContactPerson}

Please ensure you arrive on time with all required original documents.

Best regards,
PMCRMS Team
Pune Municipal Corporation";
        }
    }
}

