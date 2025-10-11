using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(
            IAppointmentService appointmentService,
            ILogger<AppointmentController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        [HttpPost("schedule")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> ScheduleAppointment([FromBody] ScheduleAppointmentRequestDto request)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var result = await _appointmentService.ScheduleAppointmentAsync(
                    request.ApplicationId,
                    officerId,
                    request.ReviewDate,
                    request.ContactPerson,
                    request.Place,
                    request.RoomNumber,
                    request.Comments);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToAppointmentResponseDto(result.Appointment!);

                return Ok(new { message = result.Message, appointment = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment");
                return StatusCode(500, new { message = "An error occurred while scheduling the appointment" });
            }
        }

        [HttpPut("{appointmentId}/confirm")]
        public async Task<IActionResult> ConfirmAppointment(int appointmentId)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _appointmentService.ConfirmAppointmentAsync(appointmentId, userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToAppointmentResponseDto(result.Appointment!);

                return Ok(new { message = result.Message, appointment = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "An error occurred while confirming the appointment" });
            }
        }

        [HttpPut("{appointmentId}/cancel")]
        public async Task<IActionResult> CancelAppointment(int appointmentId, [FromBody] CancelAppointmentRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _appointmentService.CancelAppointmentAsync(
                    appointmentId,
                    request.CancelReason,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToAppointmentResponseDto(result.Appointment!);

                return Ok(new { message = result.Message, appointment = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "An error occurred while cancelling the appointment" });
            }
        }

        [HttpPost("{appointmentId}/reschedule")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleAppointmentRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _appointmentService.RescheduleAppointmentAsync(
                    appointmentId,
                    request.NewReviewDate,
                    request.RescheduleReason,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToAppointmentResponseDto(result.Appointment!);

                return Ok(new { message = result.Message, appointment = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "An error occurred while rescheduling the appointment" });
            }
        }

        [HttpPut("{appointmentId}/complete")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> CompleteAppointment(int appointmentId, [FromBody] CompleteAppointmentRequestDto request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                
                var result = await _appointmentService.CompleteAppointmentAsync(
                    appointmentId,
                    request.CompletionNotes,
                    userName);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var response = MapToAppointmentResponseDto(result.Appointment!);

                return Ok(new { message = result.Message, appointment = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "An error occurred while completing the appointment" });
            }
        }

        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetAppointmentsByApplication(int applicationId)
        {
            try
            {
                var appointments = await _appointmentService.GetAppointmentsForApplicationAsync(applicationId);

                var response = appointments.Select(MapToAppointmentResponseDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointments for application {ApplicationId}", applicationId);
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("my-appointments")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> GetMyAppointments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var appointments = await _appointmentService.GetAppointmentsForOfficerAsync(
                    officerId,
                    startDate,
                    endDate);

                var response = appointments.Select(MapToAppointmentListDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointments for officer");
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        [HttpGet("upcoming")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> GetUpcomingAppointments([FromQuery] int daysAhead = 7)
        {
            try
            {
                var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (officerId == 0)
                {
                    return Unauthorized("Officer ID not found in token");
                }

                var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(officerId, daysAhead);

                var response = appointments.Select(MapToAppointmentListDto).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming appointments");
                return StatusCode(500, new { message = "An error occurred while retrieving upcoming appointments" });
            }
        }

        [HttpGet("{appointmentId}")]
        public async Task<IActionResult> GetAppointmentById(int appointmentId)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);

                if (appointment == null)
                {
                    return NotFound(new { message = "Appointment not found" });
                }

                var response = MapToAppointmentResponseDto(appointment);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "An error occurred while retrieving the appointment" });
            }
        }

        [HttpPost("check-availability")]
        [Authorize(Roles = "JuniorArchitect,JuniorStructuralEngineer,JuniorLicenceEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckOfficerAvailabilityRequestDto request)
        {
            try
            {
                var isAvailable = await _appointmentService.IsOfficerAvailableAsync(
                    request.OfficerId,
                    request.ReviewDate,
                    request.DurationMinutes);

                return Ok(new OfficerAvailabilityDto
                {
                    IsAvailable = isAvailable,
                    Message = isAvailable
                        ? "Officer is available at this time"
                        : "Officer is not available at this time. Please select a different time slot."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking officer availability");
                return StatusCode(500, new { message = "An error occurred while checking availability" });
            }
        }

        [HttpPost("send-reminders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendReminders([FromQuery] int hoursBeforeAppointment = 24)
        {
            try
            {
                var remindersSent = await _appointmentService.SendAppointmentRemindersAsync(hoursBeforeAppointment);

                return Ok(new
                {
                    message = $"Successfully sent {remindersSent} appointment reminders",
                    remindersSent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment reminders");
                return StatusCode(500, new { message = "An error occurred while sending reminders" });
            }
        }

        private AppointmentResponseDto MapToAppointmentResponseDto(Appointment appointment)
        {
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                ApplicationId = appointment.ApplicationId,
                ApplicationNumber = appointment.Application?.ApplicationNumber ?? $"APP_{appointment.ApplicationId}",
                ApplicantName = appointment.Application != null
                    ? $"{appointment.Application.FirstName} {appointment.Application.LastName}"
                    : "Unknown",
                ScheduledByOfficerId = appointment.ScheduledByOfficerId,
                OfficerName = appointment.ScheduledByOfficer?.Name ?? "Unknown",
                ReviewDate = appointment.ReviewDate,
                ContactPerson = appointment.ContactPerson,
                Place = appointment.Place,
                RoomNumber = appointment.RoomNumber,
                Comments = appointment.Comments,
                Status = appointment.Status,
                StatusDisplay = appointment.Status.ToString(),
                ConfirmedAt = appointment.ConfirmedAt,
                CompletedAt = appointment.CompletedAt,
                CancelledAt = appointment.CancelledAt,
                CancellationReason = appointment.CancellationReason,
                RescheduledFromAppointmentId = appointment.RescheduledFromAppointmentId,
                RescheduledToAppointmentId = appointment.RescheduledToAppointmentId,
                EmailNotificationSent = appointment.EmailNotificationSent,
                SmsNotificationSent = appointment.SmsNotificationSent,
                ReminderSent = appointment.ReminderSent,
                ReminderSentAt = appointment.ReminderSentAt,
                CreatedDate = appointment.CreatedDate
            };
        }

        private AppointmentListDto MapToAppointmentListDto(Appointment appointment)
        {
            var today = DateTime.UtcNow.Date;
            var appointmentDate = appointment.ReviewDate.Date;

            return new AppointmentListDto
            {
                Id = appointment.Id,
                ApplicationId = appointment.ApplicationId,
                ApplicationNumber = appointment.Application?.ApplicationNumber ?? $"APP_{appointment.ApplicationId}",
                ApplicantName = appointment.Application != null
                    ? $"{appointment.Application.FirstName} {appointment.Application.LastName}"
                    : "Unknown",
                ReviewDate = appointment.ReviewDate,
                Place = appointment.Place,
                ContactPerson = appointment.ContactPerson,
                Status = appointment.Status,
                StatusDisplay = appointment.Status.ToString(),
                IsUpcoming = appointment.ReviewDate > DateTime.UtcNow &&
                            (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.Confirmed),
                IsToday = appointmentDate == today
            };
        }
    }
}
