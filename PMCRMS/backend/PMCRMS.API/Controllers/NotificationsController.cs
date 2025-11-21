using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<NotificationsController> _logger;
        private readonly IEmailService _emailService;

        public NotificationsController(
            PMCRMSDbContext context,
            ILogger<NotificationsController> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Get all notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
            [FromQuery] bool? unreadOnly = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                
                _logger.LogInformation("Fetching notifications for user {UserId}, unreadOnly: {UnreadOnly}, page: {Page}",
                    userId, unreadOnly, page);

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly == true)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        UserId = n.UserId,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        ApplicationId = n.ApplicationId,
                        ApplicationNumber = n.ApplicationNumber,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt,
                        ActionUrl = n.ActionUrl,
                        ActorName = n.ActorName,
                        ActorRole = n.ActorRole,
                        Priority = n.Priority,
                        CreatedDate = n.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notifications,
                    Message = $"Retrieved {notifications.Count} of {totalCount} notifications"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error fetching notifications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");

                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = unreadCount,
                    Message = "Unread count retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unread count");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error fetching unread count",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Mark notifications as read
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead([FromBody] MarkNotificationReadRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");

                var notifications = await _context.Notifications
                    .Where(n => request.NotificationIds.Contains(n.Id) && n.UserId == userId)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    if (!notification.IsRead)
                    {
                        notification.IsRead = true;
                        notification.ReadAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                    notifications.Count, userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Marked {notifications.Count} notification(s) as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notifications as read");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error marking notifications as read",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked all {Count} notifications as read for user {UserId}",
                    unreadNotifications.Count, userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Marked all {unreadNotifications.Count} notification(s) as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error marking all notifications as read",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create a notification (internal use or admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,CityEngineer,ExecutiveEngineer")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification(
            [FromBody] CreateNotificationRequest request)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                var currentUserName = User.FindFirst("name")?.Value ?? "System";

                var notification = new Notification
                {
                    UserId = request.UserId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    ApplicationId = request.ApplicationId,
                    ApplicationNumber = request.ApplicationNumber,
                    ActionUrl = request.ActionUrl,
                    ActorName = request.ActorName ?? currentUserName,
                    ActorRole = request.ActorRole,
                    Priority = request.Priority,
                    CreatedBy = currentUserName
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created notification {NotificationId} for user {UserId} by {Creator}",
                    notification.Id, request.UserId, currentUserName);

                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    ApplicationId = notification.ApplicationId,
                    ApplicationNumber = notification.ApplicationNumber,
                    IsRead = notification.IsRead,
                    ReadAt = notification.ReadAt,
                    ActionUrl = notification.ActionUrl,
                    ActorName = notification.ActorName,
                    ActorRole = notification.ActorRole,
                    Priority = notification.Priority,
                    CreatedDate = notification.CreatedDate
                };

                return Ok(new ApiResponse<NotificationDto>
                {
                    Success = true,
                    Data = notificationDto,
                    Message = "Notification created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error creating notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteNotification(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}",
                    id, userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Notification deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error deleting notification",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
