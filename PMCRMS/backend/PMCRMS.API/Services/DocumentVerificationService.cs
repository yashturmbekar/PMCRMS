using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace PMCRMS.API.Services
{
    public class DocumentVerificationService : IDocumentVerificationService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<DocumentVerificationService> _logger;
        private readonly INotificationService _notificationService;

        public DocumentVerificationService(
            PMCRMSDbContext context,
            ILogger<DocumentVerificationService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<VerificationResult> StartVerificationAsync(
            int documentId,
            int applicationId,
            string documentType,
            int verifiedByOfficerId)
        {
            try
            {
                // Validate document exists
                var document = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Document not found",
                        Errors = new List<string> { "The specified document does not exist" }
                    };
                }

                // Validate application exists
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Application not found",
                        Errors = new List<string> { "The specified application does not exist" }
                    };
                }

                // Validate officer exists and is active
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Id == verifiedByOfficerId);

                if (officer == null || !officer.IsActive)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "The verifying officer does not exist or is inactive" }
                    };
                }

                // Check if verification already exists for this document
                var existingVerification = await _context.DocumentVerifications
                    .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.ApplicationId == applicationId);

                if (existingVerification != null &&
                    (existingVerification.Status == VerificationStatus.InProgress ||
                     existingVerification.Status == VerificationStatus.Approved))
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Verification already exists for this document",
                        Errors = new List<string> { "This document is already being verified or has been approved" }
                    };
                }

                // Calculate document hash
                var documentHash = await CalculateDocumentHashAsync(documentId);

                // Create new verification record
                var verification = new DocumentVerification
                {
                    DocumentId = documentId,
                    ApplicationId = applicationId,
                    DocumentType = documentType,
                    Status = VerificationStatus.InProgress,
                    VerifiedByOfficerId = verifiedByOfficerId,
                    VerificationStartedAt = DateTime.UtcNow,
                    DocumentHash = documentHash,
                    DocumentSizeBytes = document.FileSize.HasValue ? (long)(document.FileSize.Value * 1024) : null, // Convert KB to bytes
                    CreatedBy = officer.Name,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = officer.Name,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.DocumentVerifications.Add(verification);

                // Update application status if not already in verification
                if (application.Status != ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS)
                {
                    application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_IN_PROGRESS;
                    application.UpdatedBy = officer.Name;
                    application.UpdatedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send notification
                await SendVerificationNotificationAsync(verification, "started");

                _logger.LogInformation(
                    "Document verification started: VerificationId={VerificationId}, DocumentId={DocumentId}, ApplicationId={ApplicationId}, OfficerId={OfficerId}",
                    verification.Id, documentId, applicationId, verifiedByOfficerId);

                return new VerificationResult
                {
                    Success = true,
                    Message = "Document verification started successfully",
                    VerificationId = verification.Id,
                    Verification = verification
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting document verification");
                return new VerificationResult
                {
                    Success = false,
                    Message = "An error occurred while starting verification",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<VerificationResult> UpdateChecklistAsync(
            int verificationId,
            string? checklistItems,
            bool? isAuthentic,
            bool? isCompliant,
            bool? isComplete,
            string? comments,
            string updatedBy)
        {
            try
            {
                var verification = await _context.DocumentVerifications
                    .Include(v => v.Application)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Verification not found",
                        Errors = new List<string> { "The specified verification does not exist" }
                    };
                }

                // Cannot update completed or rejected verifications
                if (verification.Status == VerificationStatus.Approved ||
                    verification.Status == VerificationStatus.Rejected)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Cannot update completed verification",
                        Errors = new List<string> { "This verification has already been completed or rejected" }
                    };
                }

                // Update fields
                if (checklistItems != null)
                    verification.ChecklistItems = checklistItems;

                if (isAuthentic.HasValue)
                    verification.IsAuthentic = isAuthentic.Value;

                if (isCompliant.HasValue)
                    verification.IsCompliant = isCompliant.Value;

                if (isComplete.HasValue)
                    verification.IsComplete = isComplete.Value;

                if (!string.IsNullOrWhiteSpace(comments))
                    verification.VerificationComments = comments;

                verification.UpdatedBy = updatedBy;
                verification.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Verification checklist updated: VerificationId={VerificationId}, Authentic={Authentic}, Compliant={Compliant}, Complete={Complete}",
                    verificationId, isAuthentic, isCompliant, isComplete);

                return new VerificationResult
                {
                    Success = true,
                    Message = "Verification checklist updated successfully",
                    VerificationId = verification.Id,
                    Verification = verification
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating verification checklist");
                return new VerificationResult
                {
                    Success = false,
                    Message = "An error occurred while updating the checklist",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<VerificationResult> CompleteVerificationAsync(
            int verificationId,
            string? comments,
            string completedBy)
        {
            try
            {
                var verification = await _context.DocumentVerifications
                    .Include(v => v.Application)
                    .Include(v => v.Document)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Verification not found",
                        Errors = new List<string> { "The specified verification does not exist" }
                    };
                }

                // Validate current status
                if (verification.Status != VerificationStatus.InProgress)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Cannot complete verification in current status",
                        Errors = new List<string> { $"Verification is currently {verification.Status}" }
                    };
                }

                // Ensure required flags are set
                if (!verification.IsAuthentic.HasValue || !verification.IsCompliant.HasValue || !verification.IsComplete.HasValue)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Cannot complete verification without checking all flags",
                        Errors = new List<string> { "Please verify authenticity, compliance, and completeness before approving" }
                    };
                }

                if (!verification.IsAuthentic.Value || !verification.IsCompliant.Value || !verification.IsComplete.Value)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Document does not meet all verification criteria",
                        Errors = new List<string> { "Document must be authentic, compliant, and complete to be approved" }
                    };
                }

                // Calculate verification duration
                if (verification.VerificationStartedAt.HasValue)
                {
                    var duration = DateTime.UtcNow - verification.VerificationStartedAt.Value;
                    verification.VerificationDurationMinutes = (int)duration.TotalMinutes;
                }

                // Update verification status
                verification.Status = VerificationStatus.Approved;
                verification.VerifiedDate = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(comments))
                {
                    verification.VerificationComments = string.IsNullOrWhiteSpace(verification.VerificationComments)
                        ? comments
                        : $"{verification.VerificationComments}\n\nFinal Comments: {comments}";
                }

                verification.UpdatedBy = completedBy;
                verification.UpdatedDate = DateTime.UtcNow;

                // Update SEDocument status
                var document = verification.Document;
                document.IsVerified = true;
                document.VerifiedBy = verification.VerifiedByOfficerId;
                document.VerifiedDate = DateTime.UtcNow;
                document.VerificationRemarks = "Verified and Approved";

                // Check if all documents for the application are verified
                var allVerified = await AreAllDocumentsVerifiedAsync(verification.ApplicationId);

                // Update application status if all documents are verified
                if (allVerified)
                {
                    var application = verification.Application;
                    application.Status = ApplicationCurrentStatus.DOCUMENT_VERIFICATION_COMPLETED;
                    application.JEAllDocumentsVerified = true;
                    application.JEDocumentVerificationDate = DateTime.UtcNow;
                    application.UpdatedBy = completedBy;
                    application.UpdatedDate = DateTime.UtcNow;

                    _logger.LogInformation(
                        "All documents verified for application {ApplicationId}, status updated to DOCUMENT_VERIFICATION_COMPLETED",
                        verification.ApplicationId);
                }

                await _context.SaveChangesAsync();

                // Send notification
                await SendVerificationNotificationAsync(verification, "completed");

                _logger.LogInformation(
                    "Document verification completed: VerificationId={VerificationId}, Duration={Duration} minutes",
                    verificationId, verification.VerificationDurationMinutes);

                return new VerificationResult
                {
                    Success = true,
                    Message = "Document verification completed successfully",
                    VerificationId = verification.Id,
                    Verification = verification
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing document verification");
                return new VerificationResult
                {
                    Success = false,
                    Message = "An error occurred while completing verification",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<VerificationResult> RejectVerificationAsync(
            int verificationId,
            string rejectionReason,
            bool requiresResubmission,
            string rejectedBy)
        {
            try
            {
                var verification = await _context.DocumentVerifications
                    .Include(v => v.Application)
                    .Include(v => v.Document)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Verification not found",
                        Errors = new List<string> { "The specified verification does not exist" }
                    };
                }

                // Validate current status
                if (verification.Status == VerificationStatus.Approved)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Cannot reject an approved verification",
                        Errors = new List<string> { "This verification has already been approved" }
                    };
                }

                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = "Rejection reason is required",
                        Errors = new List<string> { "Please provide a reason for rejecting this document" }
                    };
                }

                // Calculate verification duration
                if (verification.VerificationStartedAt.HasValue)
                {
                    var duration = DateTime.UtcNow - verification.VerificationStartedAt.Value;
                    verification.VerificationDurationMinutes = (int)duration.TotalMinutes;
                }

                // Update verification status
                verification.Status = requiresResubmission
                    ? VerificationStatus.RequiresResubmission
                    : VerificationStatus.Rejected;
                verification.RejectionReason = rejectionReason;
                verification.VerifiedDate = DateTime.UtcNow;
                verification.UpdatedBy = rejectedBy;
                verification.UpdatedDate = DateTime.UtcNow;

                // Update SEDocument status
                var document = verification.Document;
                document.IsVerified = false;
                document.VerificationRemarks = $"Rejected: {rejectionReason}";

                // Update application status
                var application = verification.Application;
                if (requiresResubmission)
                {
                    application.Status = ApplicationCurrentStatus.RejectedByJE;
                }
                application.UpdatedBy = rejectedBy;
                application.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await SendVerificationNotificationAsync(verification, "rejected");

                _logger.LogInformation(
                    "Document verification rejected: VerificationId={VerificationId}, Reason={Reason}, RequiresResubmission={RequiresResubmission}",
                    verificationId, rejectionReason, requiresResubmission);

                return new VerificationResult
                {
                    Success = true,
                    Message = requiresResubmission
                        ? "Document rejected - resubmission required"
                        : "Document rejected",
                    VerificationId = verification.Id,
                    Verification = verification
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document verification");
                return new VerificationResult
                {
                    Success = false,
                    Message = "An error occurred while rejecting verification",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<DocumentVerification?> GetVerificationByIdAsync(int verificationId)
        {
            return await _context.DocumentVerifications
                .Include(v => v.Application)
                    .ThenInclude(a => a.User)
                .Include(v => v.Document)
                .Include(v => v.VerifiedByOfficer)
                .FirstOrDefaultAsync(v => v.Id == verificationId);
        }

        public async Task<List<DocumentVerification>> GetVerificationsForApplicationAsync(int applicationId)
        {
            return await _context.DocumentVerifications
                .Include(v => v.Document)
                .Include(v => v.VerifiedByOfficer)
                .Where(v => v.ApplicationId == applicationId)
                .OrderByDescending(v => v.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DocumentVerification>> GetVerificationsForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.DocumentVerifications
                .Include(v => v.Application)
                    .ThenInclude(a => a.User)
                .Include(v => v.Document)
                .Where(v => v.VerifiedByOfficerId == officerId);

            if (startDate.HasValue)
            {
                query = query.Where(v => v.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(v => v.CreatedDate <= endDate.Value);
            }

            return await query
                .OrderBy(v => v.VerifiedDate ?? v.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DocumentVerification>> GetPendingVerificationsAsync(int applicationId)
        {
            return await _context.DocumentVerifications
                .Include(v => v.Document)
                .Include(v => v.VerifiedByOfficer)
                .Where(v => v.ApplicationId == applicationId &&
                           (v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InProgress))
                .OrderBy(v => v.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> AreAllDocumentsVerifiedAsync(int applicationId)
        {
            // Get all documents for the application
            var allDocuments = await _context.SEDocuments
                .Where(d => d.ApplicationId == applicationId)
                .ToListAsync();

            if (!allDocuments.Any())
            {
                return false; // No documents to verify
            }

            // Get all approved verifications for the application
            var approvedVerifications = await _context.DocumentVerifications
                .Where(v => v.ApplicationId == applicationId && v.Status == VerificationStatus.Approved)
                .Select(v => v.DocumentId)
                .ToListAsync();

            // Check if all documents have approved verifications
            var allDocumentIds = allDocuments.Select(d => d.Id).ToList();
            return allDocumentIds.All(id => approvedVerifications.Contains(id));
        }

        public async Task<Dictionary<string, int>> GetVerificationStatisticsAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.DocumentVerifications
                .Where(v => v.VerifiedByOfficerId == officerId);

            if (startDate.HasValue)
            {
                query = query.Where(v => v.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(v => v.CreatedDate <= endDate.Value);
            }

            var verifications = await query.ToListAsync();

            return new Dictionary<string, int>
            {
                { "Total", verifications.Count },
                { "Pending", verifications.Count(v => v.Status == VerificationStatus.Pending) },
                { "InProgress", verifications.Count(v => v.Status == VerificationStatus.InProgress) },
                { "Approved", verifications.Count(v => v.Status == VerificationStatus.Approved) },
                { "Rejected", verifications.Count(v => v.Status == VerificationStatus.Rejected) },
                { "RequiresResubmission", verifications.Count(v => v.Status == VerificationStatus.RequiresResubmission) },
                { "AverageMinutes", verifications.Any(v => v.VerificationDurationMinutes.HasValue)
                    ? (int)verifications.Where(v => v.VerificationDurationMinutes.HasValue)
                        .Average(v => v.VerificationDurationMinutes!.Value)
                    : 0 }
            };
        }

        public async Task<string> CalculateDocumentHashAsync(int documentId)
        {
            try
            {
                var document = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return string.Empty;
                }

                // Create a hash based on document metadata (since we don't have file content access here)
                var hashInput = $"{document.Id}_{document.FileId}_{document.FileName}_{document.FileSize}_{document.CreatedDate:O}";

                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating document hash for DocumentId={DocumentId}", documentId);
                return string.Empty;
            }
        }

        private async Task SendVerificationNotificationAsync(DocumentVerification verification, string action)
        {
            try
            {
                // Ensure navigation properties are loaded
                if (verification.Application == null)
                {
                    await _context.Entry(verification)
                        .Reference(v => v.Application)
                        .Query()
                        .Include(a => a.User)
                        .LoadAsync();
                }
                else if (verification.Application.User == null)
                {
                    await _context.Entry(verification.Application)
                        .Reference(a => a.User)
                        .LoadAsync();
                }

                if (verification.VerifiedByOfficer == null)
                {
                    await _context.Entry(verification)
                        .Reference(v => v.VerifiedByOfficer)
                        .LoadAsync();
                }

                _logger.LogInformation(
                    "Notification: Document verification {Action} - VerificationId={VerificationId}, DocumentType={DocumentType}, Application={ApplicationNumber}, Officer={OfficerName}",
                    action,
                    verification.Id,
                    verification.DocumentType,
                    verification.Application?.ApplicationNumber ?? "Unknown",
                    verification.VerifiedByOfficer?.Name ?? "Unknown");

                // TODO: Implement actual email/SMS sending via NotificationService when needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification notification");
            }
        }
    }
}
