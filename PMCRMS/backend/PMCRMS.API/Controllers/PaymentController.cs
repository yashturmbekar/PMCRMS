using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PaymentController(
            PaymentService paymentService,
            IBillDeskPaymentService billDeskPaymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _billDeskPaymentService = billDeskPaymentService;
            _logger = logger;
        }

        /// <summary>
        /// Initiate payment for an application
        /// POST /api/Payment/Initiate
        /// </summary>
        [HttpPost("Initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"[PaymentController] Initiate payment request for application: {request.ApplicationId}");

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? 
                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

                var result = await _paymentService.InitializePaymentAsync(
                    request.ApplicationId, 
                    clientIp, 
                    userAgent);

                if (!result.Success)
                {
                    _logger.LogError($"[PaymentController] Payment initiation failed: {result.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        error = result.ErrorDetails
                    });
                }

                _logger.LogInformation($"[PaymentController] Payment initiated successfully - BdOrderId: {result.BdOrderId}");

                return Ok(new
                {
                    success = true,
                    message = "Payment initiated successfully",
                    data = new
                    {
                        bdOrderId = result.BdOrderId,
                        rData = result.RData,
                        paymentGatewayUrl = "https://pay.billdesk.com/web/v1_2/embeddedsdk"
                    }
                });
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
        /// Payment callback endpoint from BillDesk
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
                _logger.LogInformation($"[PaymentController] Payment callback for application: {applicationId}");
                _logger.LogInformation($"[PaymentController] Callback params - Status: {status}, Amount: {amount}, BdOrderId: {bdOrderId}");

                var callbackRequest = new PaymentCallbackRequest
                {
                    ApplicationId = applicationId,
                    TxnEntityId = txnEntityId,
                    BdOrderId = bdOrderId,
                    Status = status,
                    Amount = amount
                };

                var result = await _billDeskPaymentService.ProcessPaymentCallbackAsync(callbackRequest);

                if (result.Success)
                {
                    _logger.LogInformation($"[PaymentController] Callback processed successfully");
                    
                    // Redirect to frontend success/failure page
                    var redirectUrl = status?.ToUpper() == "SUCCESS" 
                        ? $"{Request.Scheme}://{Request.Host}/#/payment/success?applicationId={applicationId}"
                        : $"{Request.Scheme}://{Request.Host}/#/payment/failure?applicationId={applicationId}";
                    
                    return Redirect(redirectUrl);
                }
                else
                {
                    _logger.LogError($"[PaymentController] Callback processing failed: {result.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentController] Error in PaymentCallback");
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
}
