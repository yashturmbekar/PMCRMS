using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Text;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Implementation of plugin context service for BillDesk integration
    /// Handles encryption, HTTP calls, and challan generation
    /// </summary>
    public class PluginContextService : IPluginContextService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<PluginContextService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBillDeskCryptoService _cryptoService;
        private readonly IBillDeskConfigService _configService;

        public PluginContextService(
            PMCRMSDbContext context,
            ILogger<PluginContextService> logger,
            IHttpClientFactory httpClientFactory,
            IBillDeskCryptoService cryptoService,
            IBillDeskConfigService configService)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cryptoService = cryptoService;
            _configService = configService;
        }

        public async Task<dynamic> GetEntityFieldsById(string entityId)
        {
            if (!int.TryParse(entityId, out var applicationId))
            {
                throw new ArgumentException("Invalid entity ID format");
            }

            var application = await _context.PositionApplications
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                throw new InvalidOperationException("Application not found");
            }

            // Calculate price based on position type
            string certificatePrice = GetPriceByPositionType(application.PositionType);

            var applicant = await _context.Users.FindAsync(application.UserId);

            return new
            {
                FirstName = application.FirstName ?? "",
                LastName = application.LastName ?? "",
                EmailAddress = application.EmailAddress ?? "",
                MobileNumber = application.MobileNumber ?? "",
                Price = certificatePrice
            };
        }

        /// <summary>
        /// Get fee amount based on position type
        /// Architect: 0 (No fee)
        /// LicenceEngineer: 3000
        /// StructuralEngineer: 1500
        /// Supervisor1: 1500
        /// Supervisor2: 900
        /// </summary>
        private string GetPriceByPositionType(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "0",
                PositionType.LicenceEngineer => "3000",
                PositionType.StructuralEngineer => "1500",
                PositionType.Supervisor1 => "1500",
                PositionType.Supervisor2 => "900",
                _ => "0" // Default to 0 for unknown types
            };
        }

        public string RandomNumber(int length)
        {
            Random random = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(random.Next(0, 10));
            }
            return sb.ToString();
        }

        public async Task<string> CreateEntity(string entityType, string parentId, dynamic entity)
        {
            if (entityType == "Transaction")
            {
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionId = entity.TransactionId,
                    Status = entity.Status,
                    Price = Convert.ToDecimal(entity.Price),
                    ApplicationId = int.Parse(entity.ApplicationId),
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Email = entity.Email,
                    PhoneNumber = entity.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created transaction {transaction.Id} for application {transaction.ApplicationId}");

                return transaction.Id.ToString();
            }

            throw new NotSupportedException($"Entity type '{entityType}' is not supported");
        }

        public async Task<dynamic> Invoke(string serviceName, dynamic input)
        {
            _logger.LogInformation($"Invoking service: {serviceName}");

            switch (serviceName)
            {
                case "BILLDESK":
                    return await HandleBillDeskService(input);
                case "HTTPPayment":
                    return await HandleHttpPaymentService(input);
                case "Challan":
                    return await HandleChallanService(input);
                default:
                    throw new NotSupportedException($"Service '{serviceName}' is not supported");
            }
        }

        private async Task<dynamic> HandleBillDeskService(dynamic input)
        {
            try
            {
                string action = input.Action;
                _logger.LogInformation($"[PLUGIN-BILLDESK] ===== BILLDESK PLUGIN INVOKED =====");
                _logger.LogInformation($"[PLUGIN-BILLDESK] Action: {action}");

                if (action == "Encrypt")
                {
                    // Real BillDesk JWE encryption
                    var payload = new
                    {
                        mercid = input.MerchantId?.ToString(),
                        orderid = input.orderid?.ToString(),
                        amount = input.amount?.ToString(),
                        order_date = input.OrderDate?.ToString(),
                        currency = input.currency?.ToString() ?? "356",
                        ru = input.ReturnUrl?.ToString(),
                        itemcode = input.itemcode?.ToString() ?? "DIRECT",
                        device = new
                        {
                            init_channel = input.InitChannel?.ToString() ?? "internet",
                            ip = input.IpAddress?.ToString(),
                            user_agent = input.UserAgent?.ToString(),
                            accept_header = input.AcceptHeader?.ToString() ?? "text/html"
                        }
                    };

                    // **DETAILED ENCRYPTION PAYLOAD LOGGING**
                    _logger.LogInformation($"[PLUGIN-BILLDESK] === JWE ENCRYPTION PAYLOAD ===");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] mercid: {payload.mercid}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] orderid: {payload.orderid}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] amount: {payload.amount}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] order_date: {payload.order_date}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] currency: {payload.currency}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] ru (return url): {payload.ru}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] itemcode: {payload.itemcode}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] device.init_channel: {payload.device.init_channel}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] device.ip: {payload.device.ip}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] device.user_agent: {payload.device.user_agent}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] device.accept_header: {payload.device.accept_header}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] === END OF PAYLOAD ===");
                    
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Merchant ID: {input.MerchantId?.ToString()}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Key ID: {input.keyId?.ToString()}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Creating JWE token...");
                    
                    string jweToken = _cryptoService.CreateJWE(
                        payload, 
                        input.MerchantId?.ToString() ?? "",
                        input.keyId?.ToString() ?? ""
                    );
                    
                    _logger.LogInformation($"[PLUGIN-BILLDESK] JWE token created successfully");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] JWE Token Length: {jweToken.Length} characters");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] JWE Token (First 100 chars): {jweToken.Substring(0, Math.Min(100, jweToken.Length))}...");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] ===== ENCRYPTION COMPLETED =====");
                    
                    return new { Status = "SUCCESS", Message = jweToken };
                }
                else if (action == "Decrypt")
                {
                    // Real BillDesk JWE decryption
                    string jweToken = input.responseBody?.ToString() ?? "";
                    
                    _logger.LogInformation($"[PLUGIN-BILLDESK] === JWE DECRYPTION ===");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] JWE Token Length: {jweToken.Length} characters");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] JWE Token (First 100 chars): {jweToken.Substring(0, Math.Min(100, jweToken.Length))}...");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Decrypting JWE response...");
                    
                    string decryptedJson = _cryptoService.ParseJWE(jweToken);
                    
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Decryption successful");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Decrypted JSON Length: {decryptedJson.Length} characters");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] Decrypted JSON: {decryptedJson}");
                    _logger.LogInformation($"[PLUGIN-BILLDESK] ===== DECRYPTION COMPLETED =====");
                    
                    return new { Status = "SUCCESS", Message = decryptedJson };
                }

                _logger.LogError($"[PLUGIN-BILLDESK] Unsupported action: {action}");
                return new { Status = "ERROR", Message = "Unsupported action" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PLUGIN-BILLDESK] Error in BillDesk service");
                return new { Status = "ERROR", Message = ex.Message };
            }
        }

        private async Task<dynamic> HandleHttpPaymentService(dynamic input)
        {
            try
            {
                _logger.LogInformation($"[PLUGIN-HTTP] ===== HTTP PAYMENT SERVICE INVOKED =====");

                // Real HTTP call to BillDesk API
                string path = input.Path?.ToString() ?? "";
                string method = input.Method?.ToString() ?? "POST";
                string headers = input.Headers?.ToString() ?? "";
                byte[] bodyBytes = input.Body as byte[] ?? Array.Empty<byte>();

                // **DETAILED HTTP REQUEST LOGGING**
                _logger.LogInformation($"[PLUGIN-HTTP] === HTTP REQUEST DETAILS ===");
                _logger.LogInformation($"[PLUGIN-HTTP] Method: {method}");
                _logger.LogInformation($"[PLUGIN-HTTP] Path: {path}");
                _logger.LogInformation($"[PLUGIN-HTTP] Base URL: {_configService.ApiBaseUrl}");
                
                // Parse headers
                var headerDict = new Dictionary<string, string>();
                foreach (var headerLine in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = headerLine.Split(new string[] { ": " }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        headerDict[parts[0]] = parts[1];
                    }
                }

                _logger.LogInformation($"[PLUGIN-HTTP] === REQUEST HEADERS ===");
                foreach (var header in headerDict)
                {
                    _logger.LogInformation($"[PLUGIN-HTTP] {header.Key}: {header.Value}");
                }
                _logger.LogInformation($"[PLUGIN-HTTP] === END OF HEADERS ===");

                // BillDesk UAT/Production API endpoint
                string baseUrl = _configService.ApiBaseUrl;
                string fullUrl = $"{baseUrl}/{path}";

                _logger.LogInformation($"[PLUGIN-HTTP] Full URL: {fullUrl}");
                _logger.LogInformation($"[PLUGIN-HTTP] Request Body Size: {bodyBytes.Length} bytes");
                if (bodyBytes.Length > 0)
                {
                    var bodyPreview = Encoding.UTF8.GetString(bodyBytes);
                    _logger.LogInformation($"[PLUGIN-HTTP] Request Body (First 200 chars): {bodyPreview.Substring(0, Math.Min(200, bodyPreview.Length))}...");
                }
                _logger.LogInformation($"[PLUGIN-HTTP] === END OF REQUEST DETAILS ===");

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage(new HttpMethod(method), fullUrl);
                
                // Add headers
                foreach (var header in headerDict)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add body for POST/PUT requests
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && bodyBytes.Length > 0)
                {
                    request.Content = new ByteArrayContent(bodyBytes);
                    if (headerDict.ContainsKey("Content-Type"))
                    {
                        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(headerDict["Content-Type"]);
                    }
                }

                _logger.LogInformation($"[PLUGIN-HTTP] Sending HTTP request to BillDesk...");
                
                // Make the HTTP call
                var response = await httpClient.SendAsync(request);
                
                // **DETAILED HTTP RESPONSE LOGGING**
                _logger.LogInformation($"[PLUGIN-HTTP] === HTTP RESPONSE ===");
                _logger.LogInformation($"[PLUGIN-HTTP] Status Code: {response.StatusCode} ({(int)response.StatusCode})");
                _logger.LogInformation($"[PLUGIN-HTTP] Is Success: {response.IsSuccessStatusCode}");
                
                _logger.LogInformation($"[PLUGIN-HTTP] === RESPONSE HEADERS ===");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation($"[PLUGIN-HTTP] {header.Key}: {string.Join(", ", header.Value)}");
                }
                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        _logger.LogInformation($"[PLUGIN-HTTP] {header.Key}: {string.Join(", ", header.Value)}");
                    }
                }
                _logger.LogInformation($"[PLUGIN-HTTP] === END OF RESPONSE HEADERS ===");

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[PLUGIN-HTTP] === ERROR RESPONSE ===");
                    _logger.LogError($"[PLUGIN-HTTP] Error Content Length: {errorContent.Length} characters");
                    _logger.LogError($"[PLUGIN-HTTP] Error Content (encrypted): {errorContent}");
                    
                    // Try to decrypt the error response if it's a JWE/JWS token
                    try
                    {
                        if (errorContent.Contains(".") && errorContent.Split('.').Length >= 3)
                        {
                            _logger.LogInformation("[PLUGIN-HTTP] Attempting to decrypt BillDesk error response...");
                            
                            var decryptInput = new
                            {
                                Action = "Decrypt",
                                responseBody = errorContent,  // lowercase 'r' to match line 167
                                EncryptionKey = _configService.EncryptionKey,
                                SigningKey = _configService.SigningKey
                            };
                            
                            dynamic decryptResult = await HandleBillDeskService(decryptInput);
                            if (decryptResult.Status == "SUCCESS")
                            {
                                string decryptedError = decryptResult.Message;
                                _logger.LogError($"[PLUGIN-HTTP] BillDesk API error (decrypted): {decryptedError}");
                                errorContent = decryptedError;
                            }
                        }
                    }
                    catch (Exception decryptEx)
                    {
                        _logger.LogWarning($"[PLUGIN-HTTP] Could not decrypt error response: {decryptEx.Message}");
                    }
                    
                    _logger.LogError($"[PLUGIN-HTTP] === END OF ERROR RESPONSE ===");
                    _logger.LogError($"[PLUGIN-HTTP] ===== HTTP REQUEST FAILED =====");
                    return new { Status = "ERROR", Message = $"API returned {response.StatusCode}: {errorContent}" };
                }

                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                string responseContent = System.Text.Encoding.UTF8.GetString(responseBytes);

                _logger.LogInformation($"[PLUGIN-HTTP] Response Content Length: {responseContent.Length} characters");
                _logger.LogInformation($"[PLUGIN-HTTP] Response Content (First 200 chars): {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
                _logger.LogInformation($"[PLUGIN-HTTP] Full Response Content: {responseContent}");
                _logger.LogInformation($"[PLUGIN-HTTP] === END OF RESPONSE ===");
                
                // Decrypt and log successful responses too
                try
                {
                    if (responseContent.Contains(".") && responseContent.Split('.').Length >= 3)
                    {
                        _logger.LogInformation("[PLUGIN-HTTP] Response appears to be encrypted (JWE/JWS token)");
                        _logger.LogInformation("[PLUGIN-HTTP] Attempting to decrypt BillDesk success response...");
                        
                        var decryptInput = new
                        {
                            Action = "Decrypt",
                            responseBody = responseContent,
                            EncryptionKey = _configService.EncryptionKey,
                            SigningKey = _configService.SigningKey
                        };
                        
                        dynamic decryptResult = await HandleBillDeskService(decryptInput);
                        if (decryptResult.Status == "SUCCESS")
                        {
                            string decryptedResponse = decryptResult.Message;
                            _logger.LogInformation($"[PLUGIN-HTTP] === DECRYPTED SUCCESS RESPONSE ===");
                            _logger.LogInformation($"[PLUGIN-HTTP] Decrypted Response: {decryptedResponse}");
                            _logger.LogInformation($"[PLUGIN-HTTP] === END OF DECRYPTED RESPONSE ===");
                        }
                    }
                }
                catch (Exception decryptEx)
                {
                    _logger.LogWarning($"[PLUGIN-HTTP] Could not decrypt success response: {decryptEx.Message}");
                }
                
                _logger.LogInformation($"[PLUGIN-HTTP] ===== HTTP REQUEST COMPLETED SUCCESSFULLY =====");
                return new { Status = "SUCCESS", Content = responseBytes };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PLUGIN-HTTP] Error in HTTP Payment service");
                _logger.LogError($"[PLUGIN-HTTP] ===== HTTP REQUEST FAILED WITH EXCEPTION =====");
                return new { Status = "ERROR", Message = ex.Message };
            }
        }

        private async Task<dynamic> HandleChallanService(dynamic input)
        {
            try
            {
                _logger.LogInformation("Processing challan generation request");

                // Map the dynamic input to ChallanModel structure
                var challanData = new
                {
                    ChallanNumber = input.ChallanNumber?.ToString() ?? GenerateChallanNumber(),
                    Name = input.Name?.ToString() ?? "",
                    Position = input.Position?.ToString() ?? "",
                    Amount = input.Amount?.ToString() ?? "0",
                    AmountInWords = input.AmountInWords?.ToString() ?? "",
                    Date = input.Date != null ? DateTime.Parse(input.Date.ToString()) : DateTime.Now,
                    Number = input.Number?.ToString() ?? "",
                    Address = input.Address?.ToString() ?? ""
                };

                // Generate PDF using QuestPDF
                var pdfBytes = GenerateChallanPdf(challanData);

                _logger.LogInformation($"Challan generated successfully: {challanData.ChallanNumber}");

                return new
                {
                    Result = pdfBytes,
                    Success = true,
                    Message = "Challan generated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Challan service");
                return new { Status = "ERROR", Message = ex.Message };
            }
        }

        private byte[] GenerateChallanPdf(dynamic model)
        {
            // QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Content().Row(column =>
                    {
                        // Left challan copy
                        column.RelativeItem()
                            .AlignCenter()
                            .MinimalBox()
                            .Border(1)
                            .Padding(3)
                            .Width(4 * 35)
                            .Height(22 * 17)
                            .ScaleToFit()
                            .Text(text => ComposeChallanContent(text, model));

                        // Right challan copy (duplicate)
                        column.RelativeItem()
                            .AlignMiddle()
                            .MinimalBox()
                            .Border(1)
                            .Padding(3)
                            .Width(4 * 35)
                            .Height(22 * 17)
                            .ScaleToFit()
                            .Text(text => ComposeChallanContent(text, model));
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeChallanContent(QuestPDF.Fluent.TextDescriptor text, dynamic model)
        {
            try
            {
                // Set default style - Arial for English, Nirmala UI for Devanagari
                text.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").LineHeight(2));

                text.Line(" PUNE MUNICIPAL CORPORATION").Underline().FontFamily("Arial").FontSize(8).Bold();

                // Use Nirmala UI for Marathi text
                text.Line("               चलन पावती")
                    .FontFamily("Nirmala UI")
                    .FontSize(8).Bold();

                text.Line("फाईल/संदर्भ")
                    .FontFamily("Nirmala UI");

                text.Span("अर्ज क्र :")
                    .FontFamily("Nirmala UI");
                text.Line(" LIC01").FontFamily("Arial");

                text.Span("चलन क्र : ")
                    .FontFamily("Nirmala UI");
                text.Line($" {model.ChallanNumber}").FontFamily("Arial");

                text.Span("खात्याचे नाव :")
                    .FontFamily("Nirmala UI");
                text.Line($" {model.Position}").FontFamily("Arial");

                text.Span("आर्किटेक्ट नाव :")
                    .FontFamily("Nirmala UI");
                text.Line(" ").FontFamily("Arial");

                text.Span("मालकाचे नाव :")
                    .FontFamily("Nirmala UI");
                text.Line($" {model.Name}").FontFamily("Arial");

                text.Span("मिळकत :")
                    .FontFamily("Nirmala UI");
                text.Line("NEW LICENSE ENGG").FontFamily("Arial");
                text.Line("General").FontFamily("Arial").LineHeight(0);
                text.Line("___________________________________________").FontFamily("Arial").LineHeight(1);

                text.Line("  अर्थशिर्षक           तपशील      रक्कमरुपये")
                    .FontFamily("Nirmala UI")
                    .LineHeight(0);

                text.Line("___________________________________________").FontFamily("Arial").LineHeight(0);
                text.Line($"LicensedEngineer(G)   R123A102          {model.Amount}.00").FontFamily("Arial");
                text.Line($"                                  {model.Amount}.00").FontFamily("Arial");
                text.Line("___________________________________________").FontFamily("Arial").LineHeight(0);

                text.Line("एकूण रक्कम रुपये (अक्षरी)")
                    .FontFamily("Nirmala UI")
                    .LineHeight(1);

                text.Line($"{model.AmountInWords}").FontFamily("Arial").FontSize(8).ExtraBold().LineHeight(0);
                text.Line("___________________________________________").FontFamily("Arial").LineHeight(0);
                text.Span("Challan Date.").FontFamily("Arial");
                text.Line($" {model.Date:dd/MM/yyyy}").FontFamily("Arial").LineHeight(1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error with Marathi fonts, falling back to English-only content");
                
                // Fallback to English-only content
                text.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
                text.Line("PUNE MUNICIPAL CORPORATION - Payment Challan").Bold();
                text.Line($"Challan Number: {model.ChallanNumber}");
                text.Line($"Name: {model.Name}");
                text.Line($"Position: {model.Position}");
                text.Line($"Amount: Rs. {model.Amount}.00");
                text.Line($"Amount in Words: {model.AmountInWords}");
                text.Line($"Date: {model.Date:dd/MM/yyyy}");
            }
        }

        private string GenerateChallanNumber()
        {
            return $"CH{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks.ToString().Substring(10)}";
        }
    }
}
