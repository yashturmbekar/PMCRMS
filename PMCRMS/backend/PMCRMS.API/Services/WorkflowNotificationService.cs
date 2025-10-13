using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service to send email notifications to applicants at each workflow stage
    /// </summary>
    public interface IWorkflowNotificationService
    {
        Task NotifyApplicationWorkflowStageAsync(int applicationId, ApplicationCurrentStatus newStatus, string? remarks = null);
    }

    public class WorkflowNotificationService : IWorkflowNotificationService
    {
        private readonly PMCRMSDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<WorkflowNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public WorkflowNotificationService(
            PMCRMSDbContext context,
            IEmailService emailService,
            ILogger<WorkflowNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task NotifyApplicationWorkflowStageAsync(int applicationId, ApplicationCurrentStatus newStatus, string? remarks = null)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    _logger.LogWarning("[WorkflowNotification] Application {ApplicationId} not found", applicationId);
                    return;
                }

                if (string.IsNullOrEmpty(application.EmailAddress))
                {
                    _logger.LogWarning("[WorkflowNotification] No email found for application {ApplicationId}", applicationId);
                    return;
                }

                var (stageName, stageDescription) = GetStageInfo(newStatus, remarks);
                
                var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
                var viewUrl = $"{baseUrl}/applications/{applicationId}";

                var applicantName = $"{application.FirstName} {application.LastName}";

                _logger.LogInformation("[WorkflowNotification] Sending email to {Email} for application {ApplicationNumber} - Stage: {Stage}", 
                    application.EmailAddress, application.ApplicationNumber, stageName);

                // Send email notification asynchronously (fire and forget)
                _ = _emailService.SendWorkflowStageEmailAsync(
                    application.EmailAddress,
                    applicantName,
                    application.ApplicationNumber ?? "N/A",
                    stageName,
                    stageDescription,
                    viewUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WorkflowNotification] Error sending workflow notification for application {ApplicationId}", applicationId);
            }
        }

        private (string StageName, string Description) GetStageInfo(ApplicationCurrentStatus status, string? remarks)
        {
            return status switch
            {
                ApplicationCurrentStatus.JUNIOR_ENGINEER_PENDING => 
                    ("Junior Engineer Review", 
                     "Your application has been assigned to a Junior Engineer for initial review and verification. The officer will review your submitted documents and may schedule an appointment for site inspection."),

                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_PENDING => 
                    ("Document Verification", 
                     "Your documents are being verified by our Junior Engineer. This includes checking all submitted certificates, plans, and supporting documents for completeness and accuracy."),

                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => 
                    ("Assistant Engineer Review", 
                     "Your application has been forwarded to the Assistant Engineer for technical review. The AE will examine the architectural plans, structural designs, and compliance with building regulations."),

                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => 
                    ("Executive Engineer Review", 
                     "Your application is under review by the Executive Engineer. The EE will perform a comprehensive assessment of your application and technical drawings."),

                ApplicationCurrentStatus.CITY_ENGINEER_PENDING => 
                    ("City Engineer Review", 
                     "Your application has reached the City Engineer for final technical review and approval consideration."),

                ApplicationCurrentStatus.PaymentPending => 
                    ("Payment Required", 
                     $"Congratulations! Your application has been approved by the City Engineer (Stage 1). " +
                     $"To proceed with the certificate issuance process, please log in to the portal and complete the payment. " +
                     $"Once payment is confirmed, your application will be forwarded to our administrative clerk for final processing."),

                ApplicationCurrentStatus.CLERK_PENDING => 
                    ("Clerk Processing", 
                     "Payment received successfully! Your application is now being processed by our administrative clerk. The clerk will verify payment details and prepare your documents for final signature."),

                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_SIGN_PENDING => 
                    ("Digital Signature - Executive Engineer", 
                     "Your certificate is being prepared for digital signature by the Executive Engineer. This is a crucial step in validating your building permission certificate."),

                ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING => 
                    ("Digital Signature - City Engineer", 
                     "Your certificate is awaiting final digital signature from the City Engineer. This is the last step before your certificate is ready for download."),

                ApplicationCurrentStatus.ProcessedByClerk => 
                    ("Processing Complete", 
                     "Administrative processing is complete. Your application is moving forward to the digital signature stage."),

                ApplicationCurrentStatus.APPROVED => 
                    ("Application Approved! 🎉", 
                     "Congratulations! Your building permission application has been fully approved and your digitally signed certificate is ready for download. You will receive a separate email with download instructions."),

                ApplicationCurrentStatus.REJECTED => 
                    ("Application Requires Attention", 
                     $"Your application requires revisions. {(string.IsNullOrEmpty(remarks) ? "Please check the detailed remarks in your application dashboard." : $"Reason: {remarks}")} Please log in to view detailed feedback and resubmit your application after making necessary corrections."),

                ApplicationCurrentStatus.APPOINTMENT_SCHEDULED => 
                    ("Appointment Scheduled", 
                     "An appointment has been scheduled for document verification and site inspection. You will receive a detailed email with the appointment information including date, time, location, and contact details."),

                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS => 
                    ("Verification In Progress", 
                     "Document verification is currently in progress. Our Junior Engineer is reviewing all submitted documents. You will be notified once verification is complete."),

                ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED => 
                    ("Verification Completed", 
                     "Great news! All your documents have been successfully verified by the Junior Engineer. Your application will now proceed to the next stage of review."),

                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => 
                    ("Awaiting Junior Engineer Signature", 
                     "Document verification is complete. Your application is awaiting digital signature from the Junior Engineer before proceeding to the Assistant Engineer."),

                ApplicationCurrentStatus.PaymentCompleted => 
                    ("Payment Confirmed", 
                     "Thank you! Your payment has been confirmed and recorded in our system. Your application is now queued for administrative processing."),

                ApplicationCurrentStatus.UnderProcessingByClerk => 
                    ("Under Clerk Review", 
                     "Your application is currently being reviewed by the administrative clerk. They are verifying all payment records and preparing documents for signature."),

                ApplicationCurrentStatus.UnderDigitalSignatureByEE2 => 
                    ("Executive Engineer Signing", 
                     "Your certificate is currently being digitally signed by the Executive Engineer using secure HSM technology."),

                ApplicationCurrentStatus.DigitalSignatureCompletedByEE2 => 
                    ("EE Signature Complete", 
                     "The Executive Engineer has successfully applied their digital signature to your certificate. It will now proceed to the City Engineer for final signature."),

                ApplicationCurrentStatus.UnderFinalApprovalByCE2 => 
                    ("Final Approval Stage", 
                     "Your certificate is in the final approval stage with the City Engineer. Once signed, your certificate will be ready for download."),

                _ => 
                    ("Status Update", 
                     $"Your application status has been updated. Current status: {status}. Please log in to your dashboard for more details.")
            };
        }
    }
}
