using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using System.Security.Claims;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;
        private readonly INotificationService _notificationService;

        public AppointmentsController(
            PMCRMSDbContext context,
            ILogger<AppointmentsController> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all appointments for the current officer
        /// </summary>
        [HttpGet("my-appointments")]
        public async Task<ActionResult<ApiResponse<List<AppointmentDto>>>> GetMyAppointments()
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                var appointments = await _context.Appointments
                    .Include(a => a.Application)
                        .ThenInclude(app => app.User)
                    .Include(a => a.ScheduledByOfficer)
                    .Where(a => a.ScheduledByOfficerId == officerId)
                    .OrderByDescending(a => a.ScheduledDateTime)
                    .Select(a => new AppointmentDto
                    {
                        Id = a.Id,
                        ApplicationId = a.ApplicationId,
                        ApplicationNumber = a.Application.ApplicationNumber ?? "N/A",
                        ApplicantName = $"{a.Application.FirstName} {a.Application.LastName}",
                        ScheduledDateTime = a.ScheduledDateTime,
                        Location = a.Location,
                        Purpose = a.Purpose,
                        Remarks = a.Remarks,
                        Status = a.Status.ToString(),
                        ScheduledByOfficerName = a.ScheduledByOfficer.Name,
                        CompletedDateTime = a.CompletedDateTime,
                        CompletionNotes = a.CompletionNotes,
                        CreatedAt = a.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<AppointmentDto>>
                {
                    Success = true,
                    Message = "Appointments retrieved successfully",
                    Data = appointments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve appointments",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get appointments for a specific application
        /// </summary>
        [HttpGet("application/{applicationId}")]
        public async Task<ActionResult<ApiResponse<List<AppointmentDto>>>> GetAppointmentsByApplication(int applicationId)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Application)
                        .ThenInclude(app => app.User)
                    .Include(a => a.ScheduledByOfficer)
                    .Where(a => a.ApplicationId == applicationId)
                    .OrderByDescending(a => a.ScheduledDateTime)
                    .Select(a => new AppointmentDto
                    {
                        Id = a.Id,
                        ApplicationId = a.ApplicationId,
                        ApplicationNumber = a.Application.ApplicationNumber ?? "N/A",
                        ApplicantName = $"{a.Application.FirstName} {a.Application.LastName}",
                        ScheduledDateTime = a.ScheduledDateTime,
                        Location = a.Location,
                        Purpose = a.Purpose,
                        Remarks = a.Remarks,
                        Status = a.Status.ToString(),
                        ScheduledByOfficerName = a.ScheduledByOfficer.Name,
                        CompletedDateTime = a.CompletedDateTime,
                        CompletionNotes = a.CompletionNotes,
                        CreatedAt = a.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<AppointmentDto>>
                {
                    Success = true,
                    Message = "Appointments retrieved successfully",
                    Data = appointments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for application {ApplicationId}", applicationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to retrieve appointments",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Schedule a new appointment (Junior Engineer only)
        /// </summary>
        [HttpPost("schedule")]
        public async Task<ActionResult<ApiResponse<AppointmentDto>>> ScheduleAppointment(
            [FromBody] ScheduleAppointmentRequest request)
        {
            try
            {
                var officerId = GetCurrentOfficerId();
                
                // Verify the application exists
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    });
                }

                var appointment = new Appointment
                {
                    ApplicationId = request.ApplicationId,
                    ScheduledDateTime = request.ScheduledDateTime,
                    Location = request.Location,
                    Purpose = request.Purpose,
                    Remarks = request.Remarks,
                    ScheduledByOfficerId = officerId,
                    Status = AppointmentStatus.Scheduled,
                    CreatedBy = User.FindFirst("name")?.Value ?? "System",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Reload with includes
                appointment = await _context.Appointments
                    .Include(a => a.Application)
                        .ThenInclude(app => app.User)
                    .Include(a => a.ScheduledByOfficer)
                    .FirstOrDefaultAsync(a => a.Id == appointment.Id);

                // Send notification to applicant
                await _notificationService.NotifyAppointmentScheduledAsync(
                    application.UserId,
                    application.ApplicationNumber ?? "N/A",
                    application.Id,
                    request.ScheduledDateTime,
                    request.Location ?? "To be confirmed",
                    request.Purpose ?? "Document verification"
                );

                var appointmentDto = new AppointmentDto
                {
                    Id = appointment!.Id,
                    ApplicationId = appointment.ApplicationId,
                    ApplicationNumber = appointment.Application.ApplicationNumber ?? "N/A",
                    ApplicantName = $"{appointment.Application.FirstName} {appointment.Application.LastName}",
                    ScheduledDateTime = appointment.ScheduledDateTime,
                    Location = appointment.Location,
                    Purpose = appointment.Purpose,
                    Remarks = appointment.Remarks,
                    Status = appointment.Status.ToString(),
                    ScheduledByOfficerName = appointment.ScheduledByOfficer.Name,
                    CompletedDateTime = appointment.CompletedDateTime,
                    CompletionNotes = appointment.CompletionNotes,
                    CreatedAt = appointment.CreatedDate
                };

                _logger.LogInformation("Appointment scheduled for application {AppNumber} by officer {OfficerId}", 
                    application.ApplicationNumber, officerId);

                return Ok(new ApiResponse<AppointmentDto>
                {
                    Success = true,
                    Message = "Appointment scheduled successfully",
                    Data = appointmentDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to schedule appointment",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update appointment status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateAppointmentStatus(
            int id,
            [FromBody] UpdateAppointmentRequest request)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Application)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Appointment not found"
                    });
                }

                appointment.Status = request.Status;
                appointment.CompletedDateTime = request.CompletedDateTime;
                appointment.CompletionNotes = request.CompletionNotes;
                appointment.UpdatedDate = DateTime.UtcNow;
                appointment.UpdatedBy = User.FindFirst("name")?.Value ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} status updated to {Status}", id, request.Status);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Appointment status updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status for appointment {Id}", id);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update appointment status",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private int GetCurrentOfficerId()
        {
            var officerIdClaim = HttpContext.User.FindFirst("officer_id") ?? 
                                HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(officerIdClaim?.Value ?? "0");
        }
    }
}
