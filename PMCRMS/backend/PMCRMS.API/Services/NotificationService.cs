using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            int userId,
            string type,
            string title,
            string message,
            int? applicationId = null,
            string? applicationNumber = null,
            string? actionUrl = null,
            string? actorName = null,
            string? actorRole = null,
            NotificationPriority priority = NotificationPriority.Normal);

        Task NotifyApplicationStatusChangeAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string status,
            string assignedTo,
            string assignedRole,
            string remarks,
            string updatedByName,
            string updatedByRole);

        Task NotifyApplicationApprovalAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string approvedByName,
            string approvedByRole,
            string remarks);

        Task NotifyApplicationRejectionAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string rejectedByName,
            string rejectedByRole,
            string remarks);

        Task NotifyOfficerAssignmentAsync(
            int officerId,
            string applicationNumber,
            int applicationId,
            string applicationType,
            string applicantName,
            string assignedByName);
    }

    public class NotificationService : INotificationService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public NotificationService(
            PMCRMSDbContext context,
            ILogger<NotificationService> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task CreateNotificationAsync(
            int userId,
            string type,
            string title,
            string message,
            int? applicationId = null,
            string? applicationNumber = null,
            string? actionUrl = null,
            string? actorName = null,
            string? actorRole = null,
            NotificationPriority priority = NotificationPriority.Normal)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ApplicationId = applicationId,
                    ApplicationNumber = applicationNumber,
                    ActionUrl = actionUrl,
                    ActorName = actorName,
                    ActorRole = actorRole,
                    Priority = priority,
                    CreatedBy = actorName ?? "System"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created notification for user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            }
        }

        public async Task NotifyApplicationStatusChangeAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string status,
            string assignedTo,
            string assignedRole,
            string remarks,
            string updatedByName,
            string updatedByRole)
        {
            try
            {
                var user = await _context.Users.FindAsync(applicantId);
                if (user == null) return;

                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
                var viewUrl = $"{baseUrl}/applications/{applicationId}";

                // Create in-app notification
                await CreateNotificationAsync(
                    userId: applicantId,
                    type: NotificationType.StatusChange,
                    title: $"Application Status Updated - {applicationNumber}",
                    message: $"Your application has been updated to: {status}. Assigned to: {assignedTo} ({assignedRole}). {remarks}",
                    applicationId: applicationId,
                    applicationNumber: applicationNumber,
                    actionUrl: $"/applications/{applicationId}",
                    actorName: updatedByName,
                    actorRole: updatedByRole,
                    priority: NotificationPriority.Normal
                );

                // Send email notification
                if (!string.IsNullOrEmpty(user.Email))
                {
                    _ = _emailService.SendApplicationStatusUpdateEmailAsync(
                        user.Email,
                        user.Name,
                        applicationNumber,
                        status,
                        assignedTo,
                        assignedRole,
                        remarks,
                        viewUrl
                    );
                }

                _logger.LogInformation("Notified applicant {ApplicantId} about status change for application {ApplicationNumber}",
                    applicantId, applicationNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying application status change for {ApplicationNumber}", applicationNumber);
            }
        }

        public async Task NotifyApplicationApprovalAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string approvedByName,
            string approvedByRole,
            string remarks)
        {
            try
            {
                var user = await _context.Users.FindAsync(applicantId);
                if (user == null) return;

                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
                var viewUrl = $"{baseUrl}/applications/{applicationId}";

                // Create in-app notification
                await CreateNotificationAsync(
                    userId: applicantId,
                    type: NotificationType.Approval,
                    title: $"Application Approved - {applicationNumber}",
                    message: $"Congratulations! Your application has been approved by {approvedByName} ({approvedByRole}). {remarks}",
                    applicationId: applicationId,
                    applicationNumber: applicationNumber,
                    actionUrl: $"/applications/{applicationId}",
                    actorName: approvedByName,
                    actorRole: approvedByRole,
                    priority: NotificationPriority.High
                );

                // Send email notification
                if (!string.IsNullOrEmpty(user.Email))
                {
                    _ = _emailService.SendApplicationApprovalEmailAsync(
                        user.Email,
                        user.Name,
                        applicationNumber,
                        approvedByName,
                        approvedByRole,
                        remarks,
                        viewUrl
                    );
                }

                _logger.LogInformation("Notified applicant {ApplicantId} about approval for application {ApplicationNumber}",
                    applicantId, applicationNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying application approval for {ApplicationNumber}", applicationNumber);
            }
        }

        public async Task NotifyApplicationRejectionAsync(
            int applicantId,
            string applicationNumber,
            int applicationId,
            string rejectedByName,
            string rejectedByRole,
            string remarks)
        {
            try
            {
                var user = await _context.Users.FindAsync(applicantId);
                if (user == null) return;

                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
                var viewUrl = $"{baseUrl}/applications/{applicationId}";

                // Create in-app notification
                await CreateNotificationAsync(
                    userId: applicantId,
                    type: NotificationType.Rejection,
                    title: $"Application Requires Attention - {applicationNumber}",
                    message: $"Your application requires revisions. Feedback from {rejectedByName} ({rejectedByRole}): {remarks}",
                    applicationId: applicationId,
                    applicationNumber: applicationNumber,
                    actionUrl: $"/applications/{applicationId}",
                    actorName: rejectedByName,
                    actorRole: rejectedByRole,
                    priority: NotificationPriority.High
                );

                // Send email notification
                if (!string.IsNullOrEmpty(user.Email))
                {
                    _ = _emailService.SendApplicationRejectionEmailAsync(
                        user.Email,
                        user.Name,
                        applicationNumber,
                        rejectedByName,
                        rejectedByRole,
                        remarks,
                        viewUrl
                    );
                }

                _logger.LogInformation("Notified applicant {ApplicantId} about rejection for application {ApplicationNumber}",
                    applicantId, applicationNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying application rejection for {ApplicationNumber}", applicationNumber);
            }
        }

        public async Task NotifyOfficerAssignmentAsync(
            int officerId,
            string applicationNumber,
            int applicationId,
            string applicationType,
            string applicantName,
            string assignedByName)
        {
            try
            {
                var officer = await _context.Users.FindAsync(officerId);
                if (officer == null) return;

                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
                var viewUrl = $"{baseUrl}/applications/{applicationId}";

                // Create in-app notification
                await CreateNotificationAsync(
                    userId: officerId,
                    type: NotificationType.Assignment,
                    title: $"New Assignment - {applicationNumber}",
                    message: $"A new {applicationType} application from {applicantName} has been assigned to you for review.",
                    applicationId: applicationId,
                    applicationNumber: applicationNumber,
                    actionUrl: $"/applications/{applicationId}",
                    actorName: assignedByName,
                    actorRole: "System",
                    priority: NotificationPriority.Normal
                );

                // Send email notification
                if (!string.IsNullOrEmpty(officer.Email))
                {
                    _ = _emailService.SendAssignmentNotificationEmailAsync(
                        officer.Email,
                        officer.Name,
                        applicationNumber,
                        applicationType,
                        applicantName,
                        assignedByName,
                        viewUrl
                    );
                }

                _logger.LogInformation("Notified officer {OfficerId} about assignment for application {ApplicationNumber}",
                    officerId, applicationNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying officer assignment for {ApplicationNumber}", applicationNumber);
            }
        }
    }
}
