using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Mock Payment Controller for Testing - Bypasses BillDesk
    /// This controller simulates payment completion for testing purposes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MockPaymentController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly IChallanService _challanService;
        private readonly EmailService _emailService;
        private readonly ILogger<MockPaymentController> _logger;

        public MockPaymentController(
            PMCRMSDbContext context,
            IChallanService challanService,
            EmailService emailService,
            ILogger<MockPaymentController> logger)
        {
            _context = context;
            _challanService = challanService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Mock payment initiation - Immediately completes payment and processes workflow
        /// POST /api/MockPayment/Complete/{applicationId}
        /// </summary>
        [HttpPost("Complete/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> CompletePayment(int applicationId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation($"[MOCK PAYMENT] Processing mock payment for application: {applicationId}");

                // 1. Get application
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound(new { success = false, message = "Application not found" });
                }

                // 2. Create mock transaction
                var mockTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    TransactionId = $"MOCK{DateTime.Now:yyyyMMddHHmmss}",
                    BdOrderId = $"MOCK_BD_{DateTime.Now:yyyyMMddHHmmss}",
                    Status = "SUCCESS",
                    Price = 3000.00m,
                    AmountPaid = 3000.00m,
                    Mode = "MOCK_PAYMENT",
                    CardType = "TEST",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(mockTransaction);
                _logger.LogInformation($"[MOCK PAYMENT] Created mock transaction: {mockTransaction.TransactionId}");

                // 3. Update application status to PaymentCompleted
                application.Status = ApplicationCurrentStatus.PaymentCompleted;
                _logger.LogInformation($"[MOCK PAYMENT] Updated application status to PaymentCompleted");

                // 4. Generate Challan
                _logger.LogInformation($"[MOCK PAYMENT] Generating challan...");
                var challanRequest = new ChallanGenerationRequest
                {
                    ApplicationId = applicationId,
                    Name = $"{application.FirstName} {application.LastName}",
                    Position = application.PositionType.ToString(),
                    Amount = 3000m,
                    AmountInWords = "Three Thousand Only",
                    Date = DateTime.UtcNow
                };

                var challanResult = await _challanService.GenerateChallanAsync(challanRequest);
                
                if (!challanResult.Success)
                {
                    _logger.LogError($"[MOCK PAYMENT] Challan generation failed: {challanResult.Message}");
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = "Challan generation failed: " + challanResult.Message });
                }

                _logger.LogInformation($"[MOCK PAYMENT] Challan generated successfully: {challanResult.ChallanNumber}");

                // 5. Auto-assign to Clerk for processing
                _logger.LogInformation($"[MOCK PAYMENT] Searching for active clerks...");
                
                var allClerks = await _context.Officers
                    .Where(o => o.Role == Models.OfficerRole.Clerk)
                    .ToListAsync();
                
                _logger.LogInformation($"[MOCK PAYMENT] Total clerks found: {allClerks.Count}");
                foreach (var c in allClerks)
                {
                    _logger.LogInformation($"[MOCK PAYMENT] Clerk: Id={c.Id}, Name={c.Name}, IsActive={c.IsActive}, Role={c.Role}");
                }
                
                var clerk = await _context.Officers
                    .Where(o => o.Role == Models.OfficerRole.Clerk && o.IsActive)
                    .OrderBy(o => Guid.NewGuid()) // Random assignment for now
                    .FirstOrDefaultAsync();

                if (clerk != null)
                {
                    application.AssignedClerkId = clerk.Id;
                    application.AssignedToClerkDate = DateTime.UtcNow;
                    application.Status = ApplicationCurrentStatus.CLERK_PENDING;
                    application.Remarks = $"Payment completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Amount: ₹{mockTransaction.AmountPaid:F2}. Challan: {challanResult.ChallanNumber}. Assigned to Clerk for processing.";
                    _logger.LogInformation($"[MOCK PAYMENT] Assigned to Clerk {clerk.Id} - {clerk.Name}");
                }
                else
                {
                    application.Status = ApplicationCurrentStatus.PaymentCompleted;
                    application.Remarks = $"Payment completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Amount: ₹{mockTransaction.AmountPaid:F2}. Challan: {challanResult.ChallanNumber}. Awaiting clerk assignment.";
                    _logger.LogWarning($"[MOCK PAYMENT] No active clerk found for assignment");
                }

                // 6. Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"[MOCK PAYMENT] All database changes committed successfully");

                // 7. Send email notification to applicant
                try
                {
                    var emailBody = $@"
                        <h2>Payment Successful - Application #{application.ApplicationNumber}</h2>
                        <p>Dear {application.FirstName} {application.LastName},</p>
                        <p>Your payment of <strong>₹3000</strong> has been successfully processed.</p>
                        <p><strong>Transaction Details:</strong></p>
                        <ul>
                            <li>Transaction ID: {mockTransaction.TransactionId}</li>
                            <li>Application Number: {application.ApplicationNumber}</li>
                            <li>Amount: ₹{mockTransaction.AmountPaid:F2}</li>
                            <li>Date: {DateTime.Now:dd/MM/yyyy HH:mm}</li>
                            <li>Challan Number: {challanResult.ChallanNumber}</li>
                        </ul>
                        <p>Your application is now under review and has been assigned to our office for processing.</p>
                        <p>You can download your payment challan from the application portal.</p>
                        <p><strong>Pune Municipal Corporation</strong></p>
                    ";

                    await _emailService.SendEmailAsync(
                        application.EmailAddress,
                        "Payment Successful - PMC Application",
                        emailBody
                    );

                    _logger.LogInformation($"[MOCK PAYMENT] Email sent to: {application.EmailAddress}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "[MOCK PAYMENT] Error sending email notification");
                    // Don't fail the payment if email fails
                }

                // 8. Return success response
                return Ok(new
                {
                    success = true,
                    message = "Mock payment completed successfully",
                    data = new
                    {
                        applicationId = application.Id,
                        applicationNumber = application.ApplicationNumber,
                        transactionId = mockTransaction.TransactionId,
                        challanNumber = challanResult.ChallanNumber,
                        status = application.Status.ToString(),
                        amountPaid = mockTransaction.AmountPaid,
                        paymentDate = mockTransaction.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[MOCK PAYMENT] Error processing mock payment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing mock payment",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get challan PDF for download
        /// GET /api/MockPayment/DownloadChallan/{applicationId}
        /// </summary>
        [HttpGet("DownloadChallan/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> DownloadChallan(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[MOCK PAYMENT] Download challan for application: {applicationId}");

                var pdfBytes = await _challanService.GetChallanPdfAsync(applicationId);

                if (pdfBytes == null)
                {
                    return NotFound(new { success = false, message = "Challan not found" });
                }

                var fileName = $"PMC_Challan_APP{applicationId}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MOCK PAYMENT] Error downloading challan");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error downloading challan",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Check if challan is generated
        /// GET /api/MockPayment/ChallanStatus/{applicationId}
        /// </summary>
        [HttpGet("ChallanStatus/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> GetChallanStatus(int applicationId)
        {
            try
            {
                var isGenerated = await _challanService.IsChallanGeneratedAsync(applicationId);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        applicationId = applicationId,
                        isChallanGenerated = isGenerated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MOCK PAYMENT] Error checking challan status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking challan status",
                    error = ex.Message
                });
            }
        }
    }
}
