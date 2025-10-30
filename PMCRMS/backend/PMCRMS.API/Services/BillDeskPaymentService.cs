using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.ViewModels;
using System.Globalization;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Professional BillDesk payment service implementation
    /// Handles complete payment lifecycle: initiation, encryption, callback processing
    /// </summary>
    public class BillDeskPaymentService : IBillDeskPaymentService
    {
        private readonly IBillDeskConfigService _configService;
        private readonly IPluginContextService _pluginContextService;
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<BillDeskPaymentService> _logger;

        public BillDeskPaymentService(
            IBillDeskConfigService configService,
            IPluginContextService pluginContextService,
            PMCRMSDbContext context,
            ILogger<BillDeskPaymentService> logger)
        {
            _configService = configService;
            _pluginContextService = pluginContextService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initiate payment transaction with BillDesk gateway
        /// </summary>
        public async Task<PaymentResponseViewModel> InitiatePaymentAsync(
            InitiatePaymentRequestViewModel model,
            string userId,
            HttpContext httpContext)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT] Initiating payment for Application: {model.EntityId}, User: {userId}");

                // Step 1: Get application details
                var fields = await _pluginContextService.GetEntityFieldsById(model.EntityId);
                var firstName = fields.FirstName?.ToString();
                var lastName = fields.LastName?.ToString();
                var email = fields.EmailAddress?.ToString();
                var mobileNumber = fields.MobileNumber?.ToString();
                var finalFee = fields.Price?.ToString();

                if (string.IsNullOrEmpty(finalFee))
                {
                    _logger.LogError($"[PAYMENT] Price not found for application {model.EntityId}");
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = "Price not found in application"
                    };
                }

                _logger.LogInformation($"[PAYMENT] Application details - Name: {firstName} {lastName}, Amount: ₹{finalFee}");

                // Step 2: Generate unique transaction ID
                string transactionId = _pluginContextService.RandomNumber(12);
                _logger.LogInformation($"[PAYMENT] Generated TransactionId: {transactionId}");

                // Step 3: Create Transaction Entity
                dynamic txnEntity = new System.Dynamic.ExpandoObject();
                txnEntity.TransactionId = transactionId;
                txnEntity.Status = "PENDING";
                txnEntity.Price = finalFee;
                txnEntity.ApplicationId = model.EntityId;
                txnEntity.FirstName = firstName;
                txnEntity.LastName = lastName;
                txnEntity.Email = email;
                txnEntity.PhoneNumber = mobileNumber;

                var txnEntityId = await _pluginContextService.CreateEntity("Transaction", "", txnEntity);
                _logger.LogInformation($"[PAYMENT] Created Transaction Entity: {txnEntityId}");

                // Step 4: Generate Order IDs and timestamps
                string orderId = GenerateOrderId();
                string traceId = orderId.ToLower();
                string bdTimestamp = GetIstTimestamp();

                _logger.LogInformation($"[PAYMENT] OrderId: {orderId}, TraceId: {traceId}, Timestamp: {bdTimestamp}");

                // Step 5: Get client information
                string ipAddress = GetClientIpAddress(httpContext);
                string userAgent = httpContext.Request.Headers["User-Agent"].ToString() ??
                                  "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

                _logger.LogInformation($"[PAYMENT] Client IP: {ipAddress}");

                // Step 6: Encrypt payment data
                var encryptionResult = await EncryptPaymentDataAsync(
                    orderId, finalFee, model.EntityId, txnEntityId, ipAddress, userAgent);

                if (!encryptionResult.Success)
                {
                    _logger.LogError($"[PAYMENT] Encryption failed: {encryptionResult.Message}");
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Encryption failed: {encryptionResult.Message}",
                        ErrorDetails = encryptionResult.Message
                    };
                }

                _logger.LogInformation("[PAYMENT] Payment data encrypted successfully");

                // Step 7: Call BillDesk payment API
                var paymentResult = await CallBillDeskPaymentApiAsync(
                    traceId, bdTimestamp, encryptionResult.EncryptedBody);

                if (!paymentResult.Success)
                {
                    _logger.LogError($"[PAYMENT] BillDesk API call failed: {paymentResult.Message}");
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Payment API failed: {paymentResult.Message}",
                        ErrorDetails = paymentResult.Message
                    };
                }

                _logger.LogInformation("[PAYMENT] BillDesk API call successful");

                // Step 8: Decrypt payment response
                var decryptResult = await DecryptPaymentResponseAsync(paymentResult.EncryptedResponse);

                if (!decryptResult.Success)
                {
                    _logger.LogError($"[PAYMENT] Decryption failed: {decryptResult.Message}");
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Decryption failed: {decryptResult.Message}",
                        ErrorDetails = decryptResult.Message
                    };
                }

                _logger.LogInformation("[PAYMENT] Response decrypted successfully");

                // Step 9: Extract BdOrderId, RData, and Payment Gateway URL
                var paymentDetails = ExtractPaymentDetails(decryptResult.DecryptedData);
                string bdOrderId = paymentDetails.Item1;
                string rData = paymentDetails.Item2;
                string paymentGatewayUrl = paymentDetails.Item3;

                // Step 10: Update transaction with BillDesk order ID
                await UpdateTransactionWithBdOrderId(txnEntityId, bdOrderId, rData, ipAddress, userAgent);

                _logger.LogInformation($"[PAYMENT] Payment initiated successfully - BdOrderId: {bdOrderId}");

                return new PaymentResponseViewModel
                {
                    Success = true,
                    Message = "Payment initiated successfully",
                    TransactionId = transactionId,
                    TxnEntityId = txnEntityId,
                    BdOrderId = bdOrderId,
                    RData = rData,
                    PaymentGatewayUrl = paymentGatewayUrl, // Use URL from BillDesk response
                    MerchantId = _configService.MerchantId // Include merchant ID from configuration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error initiating payment");
                return new PaymentResponseViewModel
                {
                    Success = false,
                    Message = $"Error initiating payment: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Legacy payment initiation method (backward compatibility)
        /// </summary>
        public async Task<PaymentResponseViewModel> InitiatePaymentLegacyAsync(
            int applicationId, 
            string clientIp, 
            string userAgent)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT] Legacy payment initiation for application: {applicationId}");

                var application = await _context.PositionApplications.FindAsync(applicationId);
                if (application == null)
                {
                    _logger.LogError($"[PAYMENT] Application not found: {applicationId}");
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Create HttpContext for legacy call
                var httpContext = new DefaultHttpContext();
                httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(clientIp);
                httpContext.Request.Headers["User-Agent"] = userAgent;

                var request = new InitiatePaymentRequestViewModel
                {
                    EntityId = applicationId.ToString()
                };

                var result = await InitiatePaymentAsync(request, "", httpContext);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error in legacy payment initialization");
                return new PaymentResponseViewModel
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorDetails = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Verify payment status with BillDesk
        /// </summary>
        public async Task<PaymentVerificationResult> VerifyPaymentAsync(string transactionId, string bdOrderId)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT] Verifying payment - TransactionId: {transactionId}, BdOrderId: {bdOrderId}");

                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.BdOrderId == bdOrderId);

                if (transaction == null)
                {
                    _logger.LogWarning($"[PAYMENT] Transaction not found for verification");
                    return new PaymentVerificationResult
                    {
                        Success = false,
                        Message = "Transaction not found"
                    };
                }

                _logger.LogInformation($"[PAYMENT] Transaction verified - Status: {transaction.Status}, Amount: ₹{transaction.Price}");

                return new PaymentVerificationResult
                {
                    Success = true,
                    Message = "Transaction verified",
                    Status = transaction.Status,
                    Amount = transaction.Price,
                    TransactionId = transaction.TransactionId,
                    BdOrderId = transaction.BdOrderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error verifying payment");
                return new PaymentVerificationResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Process payment callback from BillDesk
        /// </summary>
        public async Task<PaymentCallbackResult> ProcessPaymentCallbackAsync(PaymentCallbackRequest request)
        {
            try
            {
                _logger.LogInformation($"[PAYMENT] Processing callback for application: {request.ApplicationId}");
                _logger.LogInformation($"[PAYMENT] Callback data - Status: {request.Status}, Amount: {request.Amount}, BdOrderId: {request.BdOrderId}");

                var application = await _context.PositionApplications.FindAsync(request.ApplicationId);
                if (application == null)
                {
                    _logger.LogError($"[PAYMENT] Application not found: {request.ApplicationId}");
                    return new PaymentCallbackResult
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
                        transaction.Status = request.Status?.ToUpper() == "SUCCESS" ? "SUCCESS" : "FAILED";
                        transaction.EaseBuzzStatus = request.Status;
                        transaction.ErrorMessage = request.ErrorMessage;
                        transaction.PaymentGatewayResponse = request.ResponseData;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        
                        if (decimal.TryParse(request.Amount, out var amount))
                        {
                            transaction.AmountPaid = amount;
                        }

                        _logger.LogInformation($"[PAYMENT] Updated transaction {transaction.Id} - Status: {transaction.Status}");
                    }
                }

                // Update application status based on payment result
                if (request.Status?.ToUpper() == "SUCCESS")
                {
                    // Auto-assign to Clerk for processing
                    var clerk = await _context.Officers
                        .Where(o => o.Role == Models.OfficerRole.Clerk && o.IsActive)
                        .OrderBy(o => Guid.NewGuid()) // Random assignment for now
                        .FirstOrDefaultAsync();

                    if (clerk != null)
                    {
                        application.AssignedClerkId = clerk.Id;
                        application.AssignedToClerkDate = DateTime.UtcNow;
                        application.Status = ApplicationCurrentStatus.CLERK_PENDING;
                        application.Remarks = $"Payment completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Amount: ₹{request.Amount}. Transaction ID: {request.TransactionId}. Assigned to Clerk for processing.";
                        _logger.LogInformation($"[PAYMENT] Payment successful - Application {request.ApplicationId} assigned to Clerk {clerk.Id} - {clerk.Name}");
                    }
                    else
                    {
                        application.Status = ApplicationCurrentStatus.PaymentCompleted;
                        application.Remarks = $"Payment completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Amount: ₹{request.Amount}. Transaction ID: {request.TransactionId}. Awaiting clerk assignment.";
                        _logger.LogWarning($"[PAYMENT] Payment successful but no active clerk found for assignment");
                    }
                    
                    application.UpdatedDate = DateTime.UtcNow;

                    // TODO: Generate Certificate and Challan PDFs here
                    // await GenerateCertificateAndChallan(application);
                }
                else
                {
                    application.Status = ApplicationCurrentStatus.PaymentPending;
                    application.UpdatedDate = DateTime.UtcNow;

                    _logger.LogWarning($"[PAYMENT] Payment failed for application {request.ApplicationId}");
                }

                await _context.SaveChangesAsync();

                var redirectUrl = request.Status?.ToUpper() == "SUCCESS"
                    ? "/payment/success"
                    : "/payment/failure";

                return new PaymentCallbackResult
                {
                    Success = true,
                    Message = "Payment callback processed successfully",
                    RedirectUrl = redirectUrl,
                    ApplicationStatus = application.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error processing payment callback");
                return new PaymentCallbackResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        #region Private Helper Methods

        private async Task<EncryptionResult> EncryptPaymentDataAsync(
            string orderId, string amount, string entityId, string txnEntityId,
            string ipAddress, string userAgent)
        {
            try
            {
                // Use InvariantCulture to force standard formatting (colons, not dots)
                // Format: 2025-10-24T18:31:30+05:30 (RFC3339/ISO 8601)
                var orderDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);
                
                _logger.LogInformation($"[BILLDESK-ENCRYPT] ===== ENCRYPTION REQUEST STARTED =====");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] Generated order_date: {orderDate}");

                dynamic input = new System.Dynamic.ExpandoObject();
                input.MerchantId = _configService.MerchantId;
                input.EncryptionKey = _configService.EncryptionKey;
                input.SigningKey = _configService.SigningKey;
                input.keyId = _configService.KeyId;
                input.clientId = _configService.ClientId;
                input.orderid = orderId;
                input.Action = "Encrypt";
                input.amount = amount;
                input.currency = "356"; // INR currency code
                input.ReturnUrl = $"{_configService.ReturnUrlBase}/{entityId}?txnEntityId={txnEntityId}";
                input.itemcode = "DIRECT";
                input.OrderDate = orderDate.ToString(CultureInfo.InvariantCulture);
                input.InitChannel = "internet";
                input.IpAddress = ipAddress;
                input.UserAgent = userAgent;
                input.AcceptHeader = "text/html";

                // **PAYLOAD LOGGING (Sensitive data masked)**
                _logger.LogInformation($"[BILLDESK-ENCRYPT] === ENCRYPTION INPUT PAYLOAD ===");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] OrderId: {orderId}");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] Amount: {amount}");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] Currency: 356 (INR)");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] ReturnUrl: {_configService.ReturnUrlBase}/{entityId}");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] ItemCode: DIRECT");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] OrderDate: {orderDate}");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] InitChannel: internet");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] IpAddress: {ipAddress}");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] === END OF INPUT PAYLOAD ===");

                dynamic response = await _pluginContextService.Invoke("BILLDESK", input);

                // **LOG ENCRYPTION RESPONSE**
                _logger.LogInformation($"[BILLDESK-ENCRYPT] === ENCRYPTION RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] Status: {response?.Status}");
                if (response?.Status == "SUCCESS")
                {
                    var encryptedToken = response.Message?.ToString() ?? "";
                    _logger.LogInformation($"[BILLDESK-ENCRYPT] Encrypted JWE Token Length: {encryptedToken.Length} characters");
                }
                else
                {
                    _logger.LogError($"[BILLDESK-ENCRYPT] Encryption Failed - Message: {response?.Message}");
                }
                _logger.LogInformation($"[BILLDESK-ENCRYPT] === END OF RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-ENCRYPT] ===== ENCRYPTION REQUEST COMPLETED =====");

                if (response?.Status != "SUCCESS")
                {
                    return new EncryptionResult 
                    { 
                        Success = false, 
                        Message = response?.Message ?? "Unknown encryption error" 
                    };
                }

                return new EncryptionResult
                {
                    Success = true,
                    EncryptedBody = response.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BILLDESK-ENCRYPT] Error encrypting payment data");
                return new EncryptionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<PaymentApiResult> CallBillDeskPaymentApiAsync(
            string traceId, string bdTimestamp, string encryptedBody)
        {
            try
            {
                _logger.LogInformation($"[BILLDESK-API] ===== API CALL STARTED =====");
                
                dynamic input = new System.Dynamic.ExpandoObject();
                input.Path = "payments/ve1_2/orders/create";
                input.Method = "POST";
                input.Headers = $"BD-Traceid: {traceId}\r\nBD-Timestamp: {bdTimestamp}\r\nclient_id: {_configService.ClientId}\r\nContent-Type: application/jose\r\nAccept: application/jose";
                input.Body = Encoding.UTF8.GetBytes(encryptedBody);

                // **DETAILED API REQUEST LOGGING**
                _logger.LogInformation($"[BILLDESK-API] === API REQUEST DETAILS ===");
                _logger.LogInformation($"[BILLDESK-API] Endpoint: {_configService.ApiBaseUrl}/payments/ve1_2/orders/create");
                _logger.LogInformation($"[BILLDESK-API] Method: POST");
                _logger.LogInformation($"[BILLDESK-API] Path: payments/ve1_2/orders/create");
                _logger.LogInformation($"[BILLDESK-API] === REQUEST HEADERS ===");
                _logger.LogInformation($"[BILLDESK-API] BD-Traceid: {traceId}");
                _logger.LogInformation($"[BILLDESK-API] BD-Timestamp: {bdTimestamp}");
                _logger.LogInformation($"[BILLDESK-API] Content-Type: application/jose");
                _logger.LogInformation($"[BILLDESK-API] Accept: application/jose");
                _logger.LogInformation($"[BILLDESK-API] === END OF HEADERS ===");
                _logger.LogInformation($"[BILLDESK-API] Request Body Size: {input.Body.Length} bytes");
                _logger.LogInformation($"[BILLDESK-API] === END OF REQUEST DETAILS ===");

                dynamic output = await _pluginContextService.Invoke("HTTPPayment", input);

                // **API RESPONSE LOGGING (Sensitive data masked)**
                string responseData = output?.Status == "SUCCESS" ? Encoding.UTF8.GetString(output.Content) : "";
                
                _logger.LogInformation($"[BILLDESK-API] === API RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-API] Response Status: {output?.Status}");
                
                if (output?.Status == "SUCCESS")
                {
                    _logger.LogInformation($"[BILLDESK-API] Response Length: {responseData.Length} characters");
                    _logger.LogInformation($"[BILLDESK-API] API call successful - proceeding to decrypt response");
                }
                else
                {
                    _logger.LogError($"[BILLDESK-API] API call failed - Status: {output?.Status}, Message: {output?.Message}");
                }
                _logger.LogInformation($"[BILLDESK-API] === END OF RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-API] ===== API CALL COMPLETED =====");

                if (output?.Status != "SUCCESS")
                {
                    return new PaymentApiResult 
                    { 
                        Success = false, 
                        Message = output?.Message ?? "Unknown API error" 
                    };
                }

                return new PaymentApiResult
                {
                    Success = true,
                    EncryptedResponse = responseData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BILLDESK-API] Error calling BillDesk payment API");
                return new PaymentApiResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<DecryptionResult> DecryptPaymentResponseAsync(string encryptedResponse)
        {
            try
            {
                _logger.LogInformation($"[BILLDESK-DECRYPT] ===== DECRYPTION REQUEST STARTED =====");
                _logger.LogInformation($"[BILLDESK-DECRYPT] Encrypted Response Length: {encryptedResponse.Length} characters");
                
                dynamic input = new System.Dynamic.ExpandoObject();
                input.EncryptionKey = _configService.EncryptionKey;
                input.SigningKey = _configService.SigningKey;
                input.Action = "Decrypt";
                input.responseBody = encryptedResponse;

                // **DECRYPTION REQUEST LOGGING (Sensitive data masked)**
                _logger.LogInformation($"[BILLDESK-DECRYPT] === DECRYPTION INPUT ===");
                _logger.LogInformation($"[BILLDESK-DECRYPT] Action: Decrypt");
                _logger.LogInformation($"[BILLDESK-DECRYPT] ResponseBody Length: {encryptedResponse.Length} characters");
                _logger.LogInformation($"[BILLDESK-DECRYPT] === END OF INPUT ===");

                dynamic response = await _pluginContextService.Invoke("BILLDESK", input);

                // **DECRYPTION RESPONSE LOGGING (Sensitive data masked)**
                _logger.LogInformation($"[BILLDESK-DECRYPT] === DECRYPTION RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-DECRYPT] Status: {response?.Status}");
                
                if (response?.Status == "SUCCESS")
                {
                    var decryptedData = response.Message?.ToString() ?? "";
                    _logger.LogInformation($"[BILLDESK-DECRYPT] Decryption successful");
                    _logger.LogInformation($"[BILLDESK-DECRYPT] Decrypted Data Length: {decryptedData.Length} characters");
                    
                    // Try to parse and log structured data (non-sensitive fields only)
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(decryptedData))
                        {
                            _logger.LogInformation($"[BILLDESK-DECRYPT] === PARSED RESPONSE DATA ===");
                            var root = doc.RootElement;
                            
                            if (root.TryGetProperty("orderid", out var orderId))
                                _logger.LogInformation($"[BILLDESK-DECRYPT] Order ID: {orderId.GetString()}");
                            
                            if (root.TryGetProperty("bdorderid", out var bdOrderId))
                                _logger.LogInformation($"[BILLDESK-DECRYPT] BillDesk Order ID: {bdOrderId.GetString()}");
                            
                            if (root.TryGetProperty("amount", out var amount))
                                _logger.LogInformation($"[BILLDESK-DECRYPT] Amount: {amount.GetString()}");
                            
                            if (root.TryGetProperty("order_status", out var orderStatus))
                                _logger.LogInformation($"[BILLDESK-DECRYPT] Order Status: {orderStatus.GetString()}");
                            
                            if (root.TryGetProperty("transaction_date", out var txnDate))
                                _logger.LogInformation($"[BILLDESK-DECRYPT] Transaction Date: {txnDate.GetString()}");
                            
                            if (root.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
                            {
                                _logger.LogInformation($"[BILLDESK-DECRYPT] Links Found: {links.GetArrayLength()}");
                                foreach (var link in links.EnumerateArray())
                                {
                                    if (link.TryGetProperty("rel", out var rel))
                                    {
                                        _logger.LogInformation($"[BILLDESK-DECRYPT] Link Rel: {rel.GetString()}");
                                        if (link.TryGetProperty("href", out var href))
                                            _logger.LogInformation($"[BILLDESK-DECRYPT] Link Href: {href.GetString()}");
                                        if (link.TryGetProperty("parameters", out var parameters))
                                            _logger.LogInformation($"[BILLDESK-DECRYPT] Link Parameters: {parameters.ToString()}");
                                    }
                                }
                            }
                            _logger.LogInformation($"[BILLDESK-DECRYPT] === END OF PARSED DATA ===");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning($"[BILLDESK-DECRYPT] Unable to parse decrypted JSON: {jsonEx.Message}");
                    }
                }
                else
                {
                    _logger.LogError($"[BILLDESK-DECRYPT] Decryption Failed - Message: {response?.Message}");
                }
                _logger.LogInformation($"[BILLDESK-DECRYPT] === END OF RESPONSE ===");
                _logger.LogInformation($"[BILLDESK-DECRYPT] ===== DECRYPTION REQUEST COMPLETED =====");

                if (response?.Status != "SUCCESS")
                {
                    return new DecryptionResult 
                    { 
                        Success = false, 
                        Message = response?.Message ?? "Unknown decryption error" 
                    };
                }

                return new DecryptionResult
                {
                    Success = true,
                    DecryptedData = response.Message.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BILLDESK-DECRYPT] Error decrypting payment response");
                return new DecryptionResult { Success = false, Message = ex.Message };
            }
        }

        private (string bdOrderId, string rData, string paymentGatewayUrl) ExtractPaymentDetails(string jsonString)
        {
            try
            {
                string bdOrderId = "";
                string rData = "";
                string paymentGatewayUrl = "";

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("links", out JsonElement links) &&
                        links.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement link in links.EnumerateArray())
                        {
                            if (link.TryGetProperty("rel", out JsonElement relProp) &&
                                relProp.GetString() == "redirect")
                            {
                                // Extract payment gateway URL from href
                                if (link.TryGetProperty("href", out JsonElement hrefProp))
                                    paymentGatewayUrl = hrefProp.GetString() ?? "";

                                if (link.TryGetProperty("parameters", out JsonElement parameters))
                                {
                                    if (parameters.TryGetProperty("bdorderid", out JsonElement bdorderidProp))
                                        bdOrderId = bdorderidProp.GetString() ?? "";

                                    if (parameters.TryGetProperty("rdata", out JsonElement rdataProp))
                                        rData = rdataProp.GetString() ?? "";

                                    break;
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"[PAYMENT] Extracted payment gateway URL from BillDesk response: {paymentGatewayUrl}");
                return (bdOrderId, rData, paymentGatewayUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error extracting payment details from JSON");
                throw;
            }
        }

        private async Task UpdateTransactionWithBdOrderId(string txnEntityId, string bdOrderId, string rData, string ipAddress, string userAgent)
        {
            try
            {
                if (Guid.TryParse(txnEntityId, out var transactionId))
                {
                    var transaction = await _context.Transactions.FindAsync(transactionId);
                    if (transaction != null)
                    {
                        transaction.BdOrderId = bdOrderId;
                        transaction.RData = rData;
                        transaction.ClientIpAddress = ipAddress;
                        transaction.UserAgent = userAgent;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"[PAYMENT] Updated transaction with BdOrderId: {bdOrderId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PAYMENT] Error updating transaction with BdOrderId");
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

        private string GetClientIpAddress(HttpContext httpContext)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress;

            if (remoteIp == null)
                return "127.0.0.1";

            if (remoteIp.IsIPv4MappedToIPv6)
                return remoteIp.MapToIPv4().ToString();

            return remoteIp.ToString();
        }

        private string GetIstTimestamp()
        {
            try
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                return istNow.ToString("yyyyMMddHHmmss");
            }
            catch
            {
                // Fallback if IST timezone is not available
                var istNow = DateTime.UtcNow.AddHours(5).AddMinutes(30);
                return istNow.ToString("yyyyMMddHHmmss");
            }
        }

        #endregion

        #region Inner Result Classes

        private class EncryptionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string EncryptedBody { get; set; } = string.Empty;
        }

        private class PaymentApiResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string EncryptedResponse { get; set; } = string.Empty;
        }

        private class DecryptionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string DecryptedData { get; set; } = string.Empty;
        }

        #endregion
    }
}
