using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using PMCRMS.API.Services;
using PMCRMS.API.ViewModels;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Payment controller for handling BillDesk payment integration
    /// Endpoints: initiate payment, process callbacks, verify status
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly IBillDeskPaymentService _billDeskPaymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly PMCRMSDbContext _context;
        private readonly IChallanService _challanService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ISECertificateGenerationService _certificateGenerationService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly string _baseUrl;

        public PaymentController(
            PaymentService paymentService,
            IBillDeskPaymentService billDeskPaymentService,
            ILogger<PaymentController> logger,
            PMCRMSDbContext context,
            IChallanService challanService,
            IEmailService emailService,
            IConfiguration configuration,
            ISECertificateGenerationService certificateGenerationService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _paymentService = paymentService;
            _billDeskPaymentService = billDeskPaymentService;
            _logger = logger;
            _context = context;
            _challanService = challanService;
            _emailService = emailService;
            _configuration = configuration;
            _certificateGenerationService = certificateGenerationService;
            _serviceScopeFactory = serviceScopeFactory;
            _baseUrl = _configuration["AppSettings:FrontendUrl"] ?? _configuration["AppSettings:BaseUrl"] ?? throw new InvalidOperationException("Frontend URL not configured");
        }

        /// <summary>
        /// Initiate payment for an application
        /// POST /api/Payment/Initiate
        /// 
        /// BillDesk payment gateway integration enabled
        /// </summary>
        [HttpPost("Initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT-CONTROLLER] ========================================");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] PAYMENT INITIATION REQUEST RECEIVED");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] ========================================");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Application ID: {request.ApplicationId}");
                
                // Get additional request context
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? 
                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                var userId = User?.Identity?.Name ?? "Anonymous";
                
                _logger.LogInformation($"[PAYMENT-CONTROLLER] === REQUEST CONTEXT ===");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Client IP: {clientIp}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] User Agent: {userAgent}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] User: {userId}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] === END OF CONTEXT ===");

                // ==================== BILLDESK INTEGRATION ENABLED ====================
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Using BillDesk Payment Gateway");

                var result = await _paymentService.InitializePaymentAsync(
                    request.ApplicationId, 
                    clientIp, 
                    userAgent);

                // **DETAILED RESULT LOGGING**
                _logger.LogInformation($"[PAYMENT-CONTROLLER] === PAYMENT SERVICE RESULT ===");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Success: {result.Success}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Message: {result.Message}");
                
                if (!result.Success)
                {
                    _logger.LogError($"[PAYMENT-CONTROLLER] Payment initiation failed");
                    _logger.LogError($"[PAYMENT-CONTROLLER] Error Details: {result.ErrorDetails}");
                    _logger.LogError($"[PAYMENT-CONTROLLER] ========================================");
                    
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        error = result.ErrorDetails
                    });
                }

                _logger.LogInformation($"[PAYMENT-CONTROLLER] BdOrderId: {result.BdOrderId}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] PaymentGatewayUrl: {result.PaymentGatewayUrl}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] RData Length: {result.RData?.Length ?? 0} characters");
                if (!string.IsNullOrEmpty(result.RData))
                {
                    _logger.LogInformation($"[PAYMENT-CONTROLLER] RData (First 100 chars): {result.RData.Substring(0, Math.Min(100, result.RData.Length))}...");
                }
                _logger.LogInformation($"[PAYMENT-CONTROLLER] === END OF RESULT ===");

                var responseData = new
                {
                    success = true,
                    message = "Payment initiated successfully",
                    data = new
                    {
                        bdOrderId = result.BdOrderId,
                        rData = result.RData,
                        paymentGatewayUrl = result.PaymentGatewayUrl, // Use URL from BillDesk response
                        merchantId = result.MerchantId
                    }
                };

                _logger.LogInformation($"[PAYMENT-CONTROLLER] === RESPONSE TO CLIENT ===");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Response: {System.Text.Json.JsonSerializer.Serialize(responseData)}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] === END OF RESPONSE ===");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] Payment initiated successfully - BdOrderId: {result.BdOrderId}");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] ========================================");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] PAYMENT INITIATION COMPLETED");
                _logger.LogInformation($"[PAYMENT-CONTROLLER] ========================================");

                return Ok(responseData);
                // ==================== END OF BILLDESK CODE ====================

                /* ==================== MOCK PAYMENT FOR TESTING (DISABLED) ====================
                // MOCK PAYMENT DISABLED - Using BillDesk payment gateway
                // To enable MOCK payment: Uncomment this section and comment out the BillDesk code above
                
                _logger.LogInformation($"[PaymentController] Using MOCK payment (BillDesk disabled)");

                // 1. Get application
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    return NotFound(new { success = false, message = "Application not found" });
                }

                // 2. Create mock transaction
                var mockTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = request.ApplicationId,
                    TransactionId = $"MOCK{DateTime.Now:yyyyMMddHHmmss}",
                    BdOrderId = $"MOCK_BD_{DateTime.Now:yyyyMMddHHmmss}",
                    Status = "SUCCESS",
                    Price = 3000.00m,
                    AmountPaid = 3000.00m,
                    Mode = "MOCK_PAYMENT",
                    CardType = "TEST",
                    FirstName = application.FirstName,
                    LastName = application.LastName,
                    Email = application.EmailAddress,
                    PhoneNumber = application.MobileNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Transactions.Add(mockTransaction);
                _logger.LogInformation($"[PaymentController] Mock transaction created: {mockTransaction.TransactionId}");

                // 3. Update application status to PaymentCompleted
                application.Status = ApplicationCurrentStatus.PaymentCompleted;

                // 4. Generate Challan (with debugging enabled)
                var challanRequest = new ChallanGenerationRequest
                {
                    ApplicationId = request.ApplicationId,
                    Name = $"{application.FirstName} {application.LastName}",
                    Position = application.PositionType.ToString(),
                    Amount = 3000m,
                    AmountInWords = "Three Thousand Only",
                    Date = DateTime.UtcNow
                };

                var challanResult = await _challanService.GenerateChallanAsync(challanRequest);
                
                if (!challanResult.Success)
                {
                    _logger.LogError($"[PaymentController] Challan generation failed: {challanResult.Message}");
                    return BadRequest(new { success = false, message = "Challan generation failed", error = challanResult.Message });
                }

                _logger.LogInformation($"[PaymentController] Challan generated: {challanResult.ChallanNumber}");

                // 5. Auto-assign to Clerk for processing
                _logger.LogInformation($"[PaymentController] Searching for active clerks...");
                
                var allClerks = await _context.Officers
                    .Where(o => o.Role == Models.OfficerRole.Clerk)
                    .ToListAsync();
                
                _logger.LogInformation($"[PaymentController] Total clerks found: {allClerks.Count}");
                foreach (var c in allClerks)
                {
                    _logger.LogInformation($"[PaymentController] Clerk: Id={c.Id}, Name={c.Name}, IsActive={c.IsActive}, Role={c.Role}");
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
                    _logger.LogInformation($"[PaymentController] Assigned to Clerk {clerk.Id} - {clerk.Name}, Status: CLERK_PENDING");
                }
                else
                {
                    application.Status = ApplicationCurrentStatus.PaymentCompleted;
                    _logger.LogWarning($"[PaymentController] No active clerk found for assignment, Status: PaymentCompleted");
                }

                // 6. Save changes (EF Core handles transaction automatically)
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[PaymentController] Mock payment completed successfully");

                // 7. Generate Licence Certificate (background task with retry logic)
                // Create new scope to avoid DbContext disposal issues
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation($"[PaymentController] Starting licence certificate generation for application: {request.ApplicationId}");
                        
                        // Create a new scope to get a fresh DbContext
                        using var scope = _serviceScopeFactory.CreateScope();
                        var certificateService = scope.ServiceProvider.GetRequiredService<ISECertificateGenerationService>();
                        
                        var certificateGenerated = await certificateService.GenerateAndSaveLicenceCertificateAsync(request.ApplicationId);
                        
                        if (certificateGenerated)
                        {
                            _logger.LogInformation($"[PaymentController] ✅ Licence certificate successfully generated for application: {request.ApplicationId}");
                        }
                        else
                        {
                            _logger.LogWarning($"[PaymentController] ⚠️ Licence certificate generation failed for application: {request.ApplicationId}");
                        }
                    }
                    catch (Exception certEx)
                    {
                        _logger.LogError(certEx, $"[PaymentController] ❌ Error during licence certificate generation for application: {request.ApplicationId}");
                        // Don't throw - certificate generation failure shouldn't affect payment success
                    }
                });

                // 8. Send email notification
                try
                {
                    var emailBody = $@"
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
        .success-icon {{
            text-align: center;
            font-size: 64px;
            color: #10b981;
            margin: 10px 0;
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
        .footer {{
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            font-size: 12px;
            color: #6b7280;
            text-align: center;
        }}
        .info-notice {{
            background-color: #dcfce7;
            border-left: 4px solid #10b981;
            padding: 12px;
            margin: 15px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo-container'>
                <img src='{_baseUrl}/pmc-logo.png' alt='PMC Logo' style='width: 100px; height: 100px; border-radius: 50%; background-color: white; padding: 10px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);' />
            </div>
            <div class='badge'>GOVERNMENT OF MAHARASHTRA</div>
            <h1>Pune Municipal Corporation</h1>
            <p>Permit Management & Certificate Recommendation System</p>
        </div>
        
        <div class='content'>
            <div class='success-icon'>✓</div>
            <div class='success-badge'>Payment Successful</div>
            
            <p>Dear {application.FirstName} {application.LastName},</p>
            
            <p>Your payment has been successfully processed. Your application is now being reviewed by our team.</p>
            
            <div class='info-box'>
                <h3 style='color: #0c4a6e; margin-top: 0;'>Payment Details</h3>
                <div class='info-row'>
                    <div class='info-label'>Transaction ID:</div>
                    <div class='info-value'>{mockTransaction.TransactionId}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Application Number:</div>
                    <div class='info-value'>{application.ApplicationNumber}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Amount Paid:</div>
                    <div class='info-value'>₹{mockTransaction.AmountPaid:F2}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Challan Number:</div>
                    <div class='info-value'>{challanResult.ChallanNumber}</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Payment Date:</div>
                    <div class='info-value'>{DateTime.Now:dd MMM yyyy, hh:mm tt}</div>
                </div>
            </div>
            
            <div class='info-notice'>
                <strong>📋 Next Steps:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Your application is now under processing by the clerk</li>
                    <li>You will receive updates via email at each stage</li>
                    <li>You can download your challan from the application portal</li>
                    <li>Keep your application number for future reference</li>
                </ul>
            </div>
            
            <p>Thank you for using PMCRMS.</p>
            
            <p>Best regards,<br>
            <strong>PMCRMS Team</strong><br>
            Pune Municipal Corporation</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>For support, please visit our website or contact us at support@pmcrms.gov.in</p>
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                    await _emailService.SendEmailAsync(
                        application.EmailAddress,
                        $"✅ Payment Successful - Application {application.ApplicationNumber}",
                        emailBody
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "[PaymentController] Error sending email");
                }

                // 9. Return response (same structure as BillDesk but payment already complete)
                return Ok(new
                {
                    success = true,
                    message = "Payment completed successfully (TEST MODE)",
                    data = new
                    {
                        bdOrderId = mockTransaction.BdOrderId,
                        rData = "MOCK_TEST_MODE",
                        paymentGatewayUrl = (string?)null,  // No redirect - payment complete
                        transactionId = mockTransaction.TransactionId,
                        challanNumber = challanResult.ChallanNumber,
                        mockMode = true
                    }
                });
                ==================== END OF MOCK PAYMENT ==================== */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in InitiatePayment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error initiating payment",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// BillDesk Payment Callback Endpoint (Primary - Handles encrypted response from BillDesk)
        /// GET /api/Payment/BillDeskCallback/{applicationId}
        /// BillDesk will POST the transaction response to this endpoint after payment
        /// </summary>
        [HttpGet("BillDeskCallback/{applicationId}")]
        [HttpPost("BillDeskCallback/{applicationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> BillDeskCallback(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[BILLDESK-CALLBACK] ========================================");
                _logger.LogInformation($"[BILLDESK-CALLBACK] BILLDESK PAYMENT CALLBACK RECEIVED");
                _logger.LogInformation($"[BILLDESK-CALLBACK] ========================================");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Application ID: {applicationId}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Method: {HttpContext.Request.Method}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Path: {HttpContext.Request.Path}");
                
                // Log all query parameters
                _logger.LogInformation($"[BILLDESK-CALLBACK] === ALL QUERY PARAMETERS ===");
                foreach (var param in HttpContext.Request.Query)
                {
                    _logger.LogInformation($"[BILLDESK-CALLBACK] {param.Key}: {param.Value}");
                }
                _logger.LogInformation($"[BILLDESK-CALLBACK] === END OF QUERY PARAMETERS ===");

                // Log all form parameters (BillDesk typically sends as form data)
                if (HttpContext.Request.HasFormContentType)
                {
                    _logger.LogInformation($"[BILLDESK-CALLBACK] === ALL FORM PARAMETERS ===");
                    foreach (var param in HttpContext.Request.Form)
                    {
                        _logger.LogInformation($"[BILLDESK-CALLBACK] {param.Key}: {param.Value}");
                    }
                    _logger.LogInformation($"[BILLDESK-CALLBACK] === END OF FORM PARAMETERS ===");
                }

                // Read body content
                string bodyContent = "";
                using (var reader = new StreamReader(HttpContext.Request.Body))
                {
                    bodyContent = await reader.ReadToEndAsync();
                }
                
                if (!string.IsNullOrEmpty(bodyContent))
                {
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Body Content Length: {bodyContent.Length}");
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Body Content (first 200 chars): {bodyContent.Substring(0, Math.Min(200, bodyContent.Length))}");
                }

                // Extract transaction ID from query parameters
                Guid? txnEntityId = null;
                if (HttpContext.Request.Query.TryGetValue("txnEntityId", out var txnEntityIdStr))
                {
                    if (Guid.TryParse(txnEntityIdStr, out var parsedGuid))
                    {
                        txnEntityId = parsedGuid;
                    }
                }

                // Get bd_order_id from query or form
                string? bdOrderId = HttpContext.Request.Query["bdorderid"].FirstOrDefault() 
                    ?? HttpContext.Request.Query["bd_order_id"].FirstOrDefault()
                    ?? HttpContext.Request.Form["bdorderid"].FirstOrDefault()
                    ?? HttpContext.Request.Form["bd_order_id"].FirstOrDefault();

                // Get encrypted response (msg parameter from BillDesk)
                // BillDesk UAT sends "transaction_response" parameter
                string? encryptedResponse = HttpContext.Request.Query["msg"].FirstOrDefault()
                    ?? HttpContext.Request.Form["msg"].FirstOrDefault()
                    ?? HttpContext.Request.Query["transaction_response"].FirstOrDefault()
                    ?? HttpContext.Request.Form["transaction_response"].FirstOrDefault();

                _logger.LogInformation($"[BILLDESK-CALLBACK] Extracted txnEntityId: {txnEntityId}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Extracted bdOrderId: {bdOrderId}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Encrypted Response Present: {!string.IsNullOrEmpty(encryptedResponse)}");
                if (!string.IsNullOrEmpty(encryptedResponse))
                {
                    _logger.LogInformation($"[BILLDESK-CALLBACK] ========== RAW ENCRYPTED RESPONSE ==========");
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Length: {encryptedResponse.Length} characters");
                    _logger.LogInformation($"[BILLDESK-CALLBACK] First 200 chars: {encryptedResponse.Substring(0, Math.Min(200, encryptedResponse.Length))}");
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Full Encrypted Response: {encryptedResponse}");
                    _logger.LogInformation($"[BILLDESK-CALLBACK] ========== END OF ENCRYPTED RESPONSE ==========");
                }

                if (string.IsNullOrEmpty(encryptedResponse))
                {
                    _logger.LogError($"[BILLDESK-CALLBACK] No encrypted response (msg or transaction_response parameter) found in callback");
                    return Redirect($"{_configuration["BillDesk:FrontendBaseUrl"]}/#/payment/failure?applicationId={applicationId}&error=no_response");
                }

                // Process the callback with encrypted response decryption
                var result = await _billDeskPaymentService.ProcessEncryptedCallbackAsync(
                    applicationId, 
                    encryptedResponse, 
                    txnEntityId, 
                    bdOrderId);

                _logger.LogInformation($"[BILLDESK-CALLBACK] === CALLBACK PROCESSING RESULT ===");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Success: {result.Success}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Message: {result.Message}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Payment Status: {result.PaymentStatus}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] Application Status: {result.ApplicationStatus}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] === END OF RESULT ===");

                // Redirect to frontend based on payment status
                var frontendUrl = _configuration["BillDesk:FrontendBaseUrl"];
                string redirectUrl;

                if (result.Success && result.PaymentStatus?.ToUpper() == "SUCCESS")
                {
                    redirectUrl = $"{frontendUrl}/#/payment/success?applicationId={applicationId}&txnId={result.TransactionId}&amount={result.Amount}";
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Payment SUCCESS - Redirecting to success page");
                }
                else
                {
                    redirectUrl = $"{frontendUrl}/#/payment/failure?applicationId={applicationId}&reason={Uri.EscapeDataString(result.Message ?? "Payment failed")}";
                    _logger.LogInformation($"[BILLDESK-CALLBACK] Payment FAILED - Redirecting to failure page");
                }

                _logger.LogInformation($"[BILLDESK-CALLBACK] Redirect URL: {redirectUrl}");
                _logger.LogInformation($"[BILLDESK-CALLBACK] ========================================");
                _logger.LogInformation($"[BILLDESK-CALLBACK] CALLBACK PROCESSING COMPLETED");
                _logger.LogInformation($"[BILLDESK-CALLBACK] ========================================");

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BILLDESK-CALLBACK] Error in BillDeskCallback");
                _logger.LogError($"[BILLDESK-CALLBACK] Exception Details: {ex.ToString()}");
                _logger.LogError($"[BILLDESK-CALLBACK] ========================================");

                var frontendUrl = _configuration["BillDesk:FrontendBaseUrl"];
                return Redirect($"{frontendUrl}/#/payment/failure?applicationId={applicationId}&error=exception");
            }
        }

        /// <summary>
        /// Payment callback endpoint from BillDesk (Legacy - for backward compatibility)
        /// GET /api/Payment/Callback/{applicationId}
        /// </summary>
        [HttpGet("Callback/{applicationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(
            int applicationId,
            [FromQuery] Guid? txnEntityId,
            [FromQuery] string? bdOrderId,
            [FromQuery] string? status,
            [FromQuery] string? amount)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT-CALLBACK] ========================================");
                _logger.LogInformation($"[PAYMENT-CALLBACK] PAYMENT CALLBACK RECEIVED (LEGACY)");
                _logger.LogInformation($"[PAYMENT-CALLBACK] ========================================");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Application ID: {applicationId}");
                
                // **DETAILED CALLBACK PARAMETERS LOGGING**
                _logger.LogInformation($"[PAYMENT-CALLBACK] === CALLBACK PARAMETERS ===");
                _logger.LogInformation($"[PAYMENT-CALLBACK] TxnEntityId: {txnEntityId}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] BdOrderId: {bdOrderId}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Status: {status}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Amount: {amount}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] === END OF PARAMETERS ===");
                
                // Log all query parameters
                _logger.LogInformation($"[PAYMENT-CALLBACK] === ALL QUERY PARAMETERS ===");
                foreach (var param in HttpContext.Request.Query)
                {
                    _logger.LogInformation($"[PAYMENT-CALLBACK] {param.Key}: {param.Value}");
                }
                _logger.LogInformation($"[PAYMENT-CALLBACK] === END OF QUERY PARAMETERS ===");

                var callbackRequest = new PaymentCallbackRequest
                {
                    ApplicationId = applicationId,
                    TxnEntityId = txnEntityId,
                    BdOrderId = bdOrderId,
                    Status = status,
                    Amount = amount
                };

                _logger.LogInformation($"[PAYMENT-CALLBACK] Processing callback through BillDesk payment service...");
                var result = await _billDeskPaymentService.ProcessPaymentCallbackAsync(callbackRequest);

                _logger.LogInformation($"[PAYMENT-CALLBACK] === CALLBACK PROCESSING RESULT ===");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Success: {result.Success}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Message: {result.Message}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Redirect URL: {result.RedirectUrl}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] Application Status: {result.ApplicationStatus}");
                _logger.LogInformation($"[PAYMENT-CALLBACK] === END OF RESULT ===");

                if (result.Success)
                {
                    _logger.LogInformation($"[PAYMENT-CALLBACK] Callback processed successfully");
                    
                    // Redirect to frontend success/failure page
                    var frontendUrl = _configuration["BillDesk:FrontendBaseUrl"];
                    var redirectUrl = status?.ToUpper() == "SUCCESS" 
                        ? $"{frontendUrl}/#/payment/success?applicationId={applicationId}"
                        : $"{frontendUrl}/#/payment/failure?applicationId={applicationId}";
                    
                    _logger.LogInformation($"[PAYMENT-CALLBACK] Redirecting to: {redirectUrl}");
                    _logger.LogInformation($"[PAYMENT-CALLBACK] ========================================");
                    _logger.LogInformation($"[PAYMENT-CALLBACK] CALLBACK PROCESSING COMPLETED");
                    _logger.LogInformation($"[PAYMENT-CALLBACK] ========================================");
                    
                    return Redirect(redirectUrl);
                }
                else
                {
                    _logger.LogError($"[PAYMENT-CALLBACK] Callback processing failed: {result.Message}");
                    _logger.LogError($"[PAYMENT-CALLBACK] ========================================");
                    
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT-CALLBACK] Error in PaymentCallback");
                _logger.LogError($"[PAYMENT-CALLBACK] Exception Details: {ex.ToString()}");
                _logger.LogError($"[PAYMENT-CALLBACK] ========================================");
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing payment callback",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Process payment success (alternative endpoint)
        /// POST /api/Payment/Success
        /// </summary>
        [HttpPost("Success")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess([FromBody] PaymentSuccessRequest request)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Payment success for application: {request.ApplicationId}");

                var result = await _paymentService.ProcessPaymentSuccessAsync(request);

                if (!result.Success)
                {
                    _logger.LogError($"[PaymentController] Payment success processing failed: {result.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                _logger.LogInformation($"[PaymentController] Payment success processed");

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    redirectUrl = result.RedirectUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in PaymentSuccess");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing payment success",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get payment status for an application
        /// GET /api/Payment/Status/{applicationId}
        /// </summary>
        [HttpGet("Status/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Get payment status for application: {applicationId}");

                var result = await _paymentService.GetPaymentStatusAsync(applicationId);

                if (!result.Success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        applicationId = result.ApplicationId,
                        isPaymentComplete = result.IsPaymentComplete,
                        paymentStatus = result.PaymentStatus,
                        amount = result.Amount,
                        amountPaid = result.AmountPaid,
                        transactionId = result.TransactionId,
                        bdOrderId = result.BdOrderId,
                        paymentDate = result.PaymentDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in GetPaymentStatus");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting payment status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get payment history for an application
        /// GET /api/Payment/History/{applicationId}
        /// </summary>
        [HttpGet("History/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentHistory(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Get payment history for application: {applicationId}");

                var transactions = await _paymentService.GetPaymentHistoryAsync(applicationId);

                return Ok(new
                {
                    success = true,
                    message = "Payment history retrieved",
                    data = transactions.Select(t => new
                    {
                        id = t.Id,
                        transactionId = t.TransactionId,
                        bdOrderId = t.BdOrderId,
                        status = t.Status,
                        amount = t.Price,
                        amountPaid = t.AmountPaid,
                        paymentMode = t.Mode,
                        cardType = t.CardType,
                        errorMessage = t.ErrorMessage,
                        createdAt = t.CreatedAt,
                        updatedAt = t.UpdatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in GetPaymentHistory");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting payment history",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get detailed transaction information by transaction ID
        /// GET /api/Payment/Transaction/{transactionId}
        /// </summary>
        [HttpGet("Transaction/{transactionId}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionDetails(Guid transactionId)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Get transaction details for: {transactionId}");

                var transaction = await _context.Transactions
                    .Include(t => t.Application)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (transaction == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transaction not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Transaction details retrieved",
                    data = new
                    {
                        id = transaction.Id,
                        transactionId = transaction.TransactionId,
                        bdOrderId = transaction.BdOrderId,
                        status = transaction.Status,
                        price = transaction.Price,
                        amountPaid = transaction.AmountPaid,
                        applicationId = transaction.ApplicationId,
                        firstName = transaction.FirstName,
                        lastName = transaction.LastName,
                        email = transaction.Email,
                        phoneNumber = transaction.PhoneNumber,
                        easeBuzzStatus = transaction.EaseBuzzStatus,
                        errorMessage = transaction.ErrorMessage,
                        cardType = transaction.CardType,
                        mode = transaction.Mode,
                        paymentGatewayResponse = transaction.PaymentGatewayResponse,
                        clientIpAddress = transaction.ClientIpAddress,
                        userAgent = transaction.UserAgent,
                        createdAt = transaction.CreatedAt,
                        updatedAt = transaction.UpdatedAt,
                        applicationDetails = transaction.Application != null ? new
                        {
                            id = transaction.Application.Id,
                            applicantName = $"{transaction.Application.FirstName} {transaction.Application.MiddleName} {transaction.Application.LastName}".Trim(),
                            positionType = transaction.Application.PositionType.ToString(),
                            status = transaction.Application.Status.ToString()
                        } : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in GetTransactionDetails");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting transaction details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Verify payment with BillDesk
        /// POST /api/Payment/Verify
        /// </summary>
        [HttpPost("Verify")]
        [Authorize]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Verify payment - TransactionId: {request.TransactionId}, BdOrderId: {request.BdOrderId}");

                var result = await _billDeskPaymentService.VerifyPaymentAsync(
                    request.TransactionId, 
                    request.BdOrderId);

                if (!result.Success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        status = result.Status,
                        amount = result.Amount,
                        transactionId = result.TransactionId,
                        bdOrderId = result.BdOrderId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in VerifyPayment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error verifying payment",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate and download payment challan (receipt)
        /// GET /api/Payment/DownloadChallan/{applicationId}/{transactionId}
        /// </summary>
        [HttpGet("DownloadChallan/{applicationId}/{transactionId}")]
        [Authorize]
        public async Task<IActionResult> DownloadChallan(int applicationId, Guid transactionId)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Download challan - ApplicationId: {applicationId}, TransactionId: {transactionId}");

                var pdfBytes = await _paymentService.GenerateChallanPdfAsync(applicationId, transactionId);

                var fileName = $"PMC_Challan_APP{applicationId}_{DateTime.Now:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error generating challan");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating payment challan",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate and download preliminary certificate
        /// GET /api/Payment/DownloadCertificate/{applicationId}
        /// </summary>
        [HttpGet("DownloadCertificate/{applicationId}")]
        [Authorize]
        public async Task<IActionResult> DownloadCertificate(int applicationId)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Download certificate - ApplicationId: {applicationId}");

                var pdfBytes = await _paymentService.GenerateCertificatePdfAsync(applicationId);

                var fileName = $"PMC_Certificate_APP{applicationId}_{DateTime.Now:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error generating certificate");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating certificate",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test endpoint to decrypt BillDesk response (for debugging)
        /// POST /api/Payment/DecryptTest
        /// </summary>
        [HttpPost("DecryptTest")]
        [AllowAnonymous]
        public async Task<IActionResult> DecryptTest([FromBody] DecryptTestRequest request)
        {
            try
            {
                _logger.LogInformation($"[DECRYPT-TEST] Decrypting BillDesk response");
                _logger.LogInformation($"[DECRYPT-TEST] Encrypted Response Length: {request.EncryptedResponse?.Length ?? 0}");

                if (string.IsNullOrEmpty(request.EncryptedResponse))
                {
                    return BadRequest(new { success = false, message = "Encrypted response is required" });
                }

                // Use the BillDesk service to decrypt
                var decryptResult = await _billDeskPaymentService.DecryptPaymentResponseAsync(request.EncryptedResponse);

                if (!decryptResult.Success)
                {
                    _logger.LogError($"[DECRYPT-TEST] Decryption failed: {decryptResult.Message}");
                    return Ok(new
                    {
                        success = false,
                        message = decryptResult.Message,
                        error = decryptResult.Message
                    });
                }

                _logger.LogInformation($"[DECRYPT-TEST] ========== DECRYPTED RESPONSE ==========");
                _logger.LogInformation($"[DECRYPT-TEST] {decryptResult.DecryptedData}");
                _logger.LogInformation($"[DECRYPT-TEST] ========== END OF DECRYPTED RESPONSE ==========");

                // Parse JSON to make it readable
                var jsonDocument = System.Text.Json.JsonDocument.Parse(decryptResult.DecryptedData);
                var formattedJson = System.Text.Json.JsonSerializer.Serialize(
                    jsonDocument, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                return Ok(new
                {
                    success = true,
                    message = "Decryption successful",
                    decryptedData = decryptResult.DecryptedData,
                    formattedJson = formattedJson,
                    parsedData = jsonDocument.RootElement
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DECRYPT-TEST] Error in DecryptTest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error decrypting response",
                    error = ex.Message,
                    stackTrace = ex.ToString()
                });
            }
        }
    }

    /// <summary>
    /// Request model for initiating payment
    /// </summary>
    public class InitiatePaymentRequest
    {
        public int ApplicationId { get; set; }
    }

    /// <summary>
    /// Request model for verifying payment
    /// </summary>
    public class VerifyPaymentRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string BdOrderId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for decrypt test
    /// </summary>
    public class DecryptTestRequest
    {
        public string EncryptedResponse { get; set; } = string.Empty;
    }
}

