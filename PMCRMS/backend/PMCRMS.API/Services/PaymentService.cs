using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.ViewModels;
using System.Text;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Main payment service orchestrating payment operations
    /// </summary>
    public partial class PaymentService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBillDeskPaymentService _billDeskPaymentService;
        private readonly IWorkflowNotificationService _workflowNotificationService;
        private readonly IWorkflowProgressionService _workflowProgressionService;
        private readonly ISECertificateGenerationService _certificateGenerationService;

        public PaymentService(
            PMCRMSDbContext context,
            ILogger<PaymentService> logger,
            IHttpClientFactory httpClientFactory,
            IBillDeskPaymentService billDeskPaymentService,
            IWorkflowNotificationService workflowNotificationService,
            IWorkflowProgressionService workflowProgressionService,
            ISECertificateGenerationService certificateGenerationService)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _billDeskPaymentService = billDeskPaymentService;
            _workflowNotificationService = workflowNotificationService;
            _workflowProgressionService = workflowProgressionService;
            _certificateGenerationService = certificateGenerationService;
        }

        /// <summary>
        /// Initialize payment for an application
        /// </summary>
        public async Task<PaymentInitializationResponse> InitializePaymentAsync(
            int applicationId, 
            string clientIp, 
            string userAgent)
        {
            try
            {
                _logger.LogInformation($"[PaymentService] Initializing payment for application: {applicationId}");

                // Validate application exists and is in correct stage
                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogError($"[PaymentService] Application not found: {applicationId}");
                    return new PaymentInitializationResponse
                    {
                        Success = false,
                        Message = "Application not found",
                        ErrorDetails = "Application not found"
                    };
                }

                // Validate application is in PaymentPending stage
                if (application.Status != ApplicationCurrentStatus.PaymentPending)
                {
                    _logger.LogWarning($"[PaymentService] Application {applicationId} not in payment pending stage. Current status: {application.Status}");
                    return new PaymentInitializationResponse
                    {
                        Success = false,
                        Message = "Application must be in payment pending status",
                        ErrorDetails = $"Current status: {application.Status}"
                    };
                }

                // Check if payment already completed
                if (application.Status == ApplicationCurrentStatus.PaymentCompleted)
                {
                    _logger.LogWarning($"[PaymentService] Payment already completed for application: {applicationId}");
                    return new PaymentInitializationResponse
                    {
                        Success = false,
                        Message = "Payment already completed for this application",
                        ErrorDetails = "Duplicate payment attempt"
                    };
                }

                // Use the BillDesk payment service
                var result = await _billDeskPaymentService.InitiatePaymentLegacyAsync(
                    applicationId, 
                    clientIp, 
                    userAgent);

                if (result.Success)
                {
                    _logger.LogInformation($"[PaymentService] Payment initialized successfully - BdOrderId: {result.BdOrderId}");
                }
                else
                {
                    _logger.LogError($"[PaymentService] Payment initialization failed: {result.Message}");
                }

                return new PaymentInitializationResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    BdOrderId = result.BdOrderId,
                    RData = result.RData,
                    ErrorDetails = result.Success ? null : result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentService] Error initializing payment");
                return new PaymentInitializationResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorDetails = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Process successful payment callback
        /// </summary>
        public async Task<PaymentSuccessResponse> ProcessPaymentSuccessAsync(PaymentSuccessRequest request)
        {
            try
            {
                _logger.LogInformation($"[PaymentService] Processing payment success for application: {request.ApplicationId}");

                var application = await _context.PositionApplications.FindAsync(request.ApplicationId);
                if (application == null)
                {
                    _logger.LogError($"[PaymentService] Application not found: {request.ApplicationId}");
                    return new PaymentSuccessResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Update transaction if provided
                if (request.TxnEntityId.HasValue)
                {
                    var transaction = await _context.Transactions.FindAsync(request.TxnEntityId.Value);
                    if (transaction != null)
                    {
                        transaction.Status = "SUCCESS";
                        transaction.EaseBuzzStatus = request.Status;
                        transaction.ErrorMessage = request.ErrorMessage;
                        transaction.CardType = request.CardType;
                        transaction.Mode = request.Mode;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        
                        if (decimal.TryParse(request.Amount, out var amount))
                        {
                            transaction.AmountPaid = amount;
                        }

                        _logger.LogInformation($"[PaymentService] Updated transaction {transaction.Id}");
                    }
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.PaymentCompleted;
                application.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send email notification to applicant
                await _workflowNotificationService.NotifyApplicationWorkflowStageAsync(
                    application.Id,
                    ApplicationCurrentStatus.PaymentCompleted
                );

                _logger.LogInformation($"[PaymentService] Payment processed successfully for application: {request.ApplicationId}");
                _logger.LogInformation($"[PaymentService] Application moved to PaymentCompleted status");

                // Auto-assign to Clerk after successful payment
                _logger.LogInformation($"[PaymentService] Initiating auto-assignment to Clerk for application: {request.ApplicationId}");
                var clerkAssigned = await _workflowProgressionService.ProgressToClerkAsync(application.Id);
                
                if (clerkAssigned)
                {
                    _logger.LogInformation($"[PaymentService] Application {request.ApplicationId} successfully assigned to Clerk");
                }
                else
                {
                    _logger.LogWarning($"[PaymentService] Application {request.ApplicationId} payment complete but Clerk assignment failed - may need manual assignment");
                }

                // Generate Licence Certificate after successful payment (async, non-blocking)
                // This runs in background with retry logic - failure won't block payment success flow
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation($"[PaymentService] Starting licence certificate generation for application: {request.ApplicationId}");
                        
                        var certificateGenerated = await _certificateGenerationService.GenerateAndSaveLicenceCertificateAsync(request.ApplicationId);
                        
                        if (certificateGenerated)
                        {
                            _logger.LogInformation($"[PaymentService] ✅ Licence certificate successfully generated for application: {request.ApplicationId}");
                        }
                        else
                        {
                            _logger.LogWarning($"[PaymentService] ⚠️ Licence certificate generation failed for application: {request.ApplicationId}");
                        }
                    }
                    catch (Exception certEx)
                    {
                        _logger.LogError(certEx, $"[PaymentService] ❌ Error during licence certificate generation for application: {request.ApplicationId}");
                        // Don't throw - certificate generation failure shouldn't affect payment success
                    }
                });

                return new PaymentSuccessResponse
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    RedirectUrl = "/payment/success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentService] Error processing payment success");
                return new PaymentSuccessResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Process failed payment callback
        /// </summary>
        public async Task<PaymentSuccessResponse> ProcessPaymentFailureAsync(PaymentSuccessRequest request)
        {
            try
            {
                _logger.LogWarning($"[PaymentService] Processing payment failure for application: {request.ApplicationId}");
                _logger.LogWarning($"[PaymentService] Failure reason: {request.ErrorMessage}");

                var application = await _context.PositionApplications.FindAsync(request.ApplicationId);
                if (application == null)
                {
                    return new PaymentSuccessResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Update transaction if provided
                if (request.TxnEntityId.HasValue)
                {
                    var transaction = await _context.Transactions.FindAsync(request.TxnEntityId.Value);
                    if (transaction != null)
                    {
                        transaction.Status = "FAILED";
                        transaction.EaseBuzzStatus = request.Status;
                        transaction.ErrorMessage = request.ErrorMessage;
                        transaction.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation($"[PaymentService] Updated failed transaction {transaction.Id}");
                    }
                }

                // Update application status
                application.Status = ApplicationCurrentStatus.PaymentPending;
                application.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send email notification to applicant about payment failure (optional)
                // Could use PaymentPending or create a specific failure notification
                _logger.LogInformation($"[PaymentService] Payment failure recorded for application: {request.ApplicationId}");

                return new PaymentSuccessResponse
                {
                    Success = true,
                    Message = "Payment failure recorded",
                    RedirectUrl = "/payment/failure"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentService] Error processing payment failure");
                return new PaymentSuccessResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get payment status for an application
        /// </summary>
        public async Task<PaymentStatusResponse> GetPaymentStatusAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new PaymentStatusResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Get latest transaction for this application
                var latestTransaction = await _context.Transactions
                    .Where(t => t.ApplicationId == applicationId)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                return new PaymentStatusResponse
                {
                    Success = true,
                    Message = "Payment status retrieved",
                    ApplicationId = application.Id,
                    IsPaymentComplete = application.Status == ApplicationCurrentStatus.PaymentCompleted,
                    PaymentStatus = latestTransaction?.Status ?? "NOT_INITIATED",
                    Amount = latestTransaction?.Price,
                    AmountPaid = latestTransaction?.AmountPaid,
                    TransactionId = latestTransaction?.TransactionId,
                    BdOrderId = latestTransaction?.BdOrderId,
                    PaymentDate = latestTransaction?.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentService] Error getting payment status");
                return new PaymentStatusResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get payment history for an application
        /// </summary>
        public async Task<List<Transaction>> GetPaymentHistoryAsync(int applicationId)
        {
            try
            {
                var transactions = await _context.Transactions
                    .Where(t => t.ApplicationId == applicationId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentService] Error getting payment history");
                return new List<Transaction>();
            }
        }

        private string GenerateOrderId()
        {
            string prefix = "PMC";
            string timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            Random random = new Random();
            int randomSuffix = random.Next(100, 999);
            return $"{prefix}{timestamp}{randomSuffix}".Substring(0, 13);
        }

        private string GenerateRandomNumber(int length)
        {
            Random random = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(random.Next(0, 10));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Payment status response model
    /// </summary>
    public class PaymentStatusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ApplicationId { get; set; }
        public bool IsPaymentComplete { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? TransactionId { get; set; }
        public string? BdOrderId { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    /// <summary>
    /// Payment service extension for PDF generation
    /// </summary>
    public partial class PaymentService
    {
        /// <summary>
        /// Generate payment challan PDF
        /// </summary>
        public async Task<byte[]> GenerateChallanPdfAsync(int applicationId, Guid transactionId)
        {
            // Create PdfService instance directly
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
            var pdfLogger = loggerFactory.CreateLogger<PdfService>();
            
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var environment = new WebHostEnvironmentWrapper(webRootPath);
            
            var pdfService = new PdfService(_context, environment, pdfLogger);
            return await pdfService.GenerateChallanAsync(applicationId, transactionId);
        }

        /// <summary>
        /// Generate preliminary certificate PDF
        /// </summary>
        public async Task<byte[]> GenerateCertificatePdfAsync(int applicationId)
        {
            // Create PdfService instance directly
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
            var pdfLogger = loggerFactory.CreateLogger<PdfService>();
            
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var environment = new WebHostEnvironmentWrapper(webRootPath);
            
            var pdfService = new PdfService(_context, environment, pdfLogger);
            return await pdfService.GeneratePreliminaryCertificateAsync(applicationId);
        }
    }

    /// <summary>
    /// Simple wrapper for IWebHostEnvironment
    /// </summary>
    internal class WebHostEnvironmentWrapper : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public string ApplicationName { get; set; } = "PMCRMS.API";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;

        public WebHostEnvironmentWrapper(string webRootPath)
        {
            WebRootPath = webRootPath;
            ContentRootPath = Directory.GetCurrentDirectory();
        }
    }
}
