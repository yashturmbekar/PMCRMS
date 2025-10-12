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

        public PluginContextService(
            PMCRMSDbContext context,
            ILogger<PluginContextService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<dynamic> GetEntityFieldsById(string entityId)
        {
            if (!int.TryParse(entityId, out var applicationId))
            {
                throw new ArgumentException("Invalid entity ID format");
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                throw new InvalidOperationException("Application not found");
            }

            // Fixed price for license certificate - can be made configurable
            const string CERTIFICATE_PRICE = "3000";

            var applicant = await _context.Users.FindAsync(application.ApplicantId);

            return new
            {
                FirstName = applicant?.Name ?? "",
                LastName = "", // User model doesn't have LastName
                EmailAddress = applicant?.Email ?? "",
                MobileNumber = applicant?.PhoneNumber ?? "",
                Price = CERTIFICATE_PRICE
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
                _logger.LogInformation($"BillDesk service action: {action}");

                if (action == "Encrypt")
                {
                    // In production, this would call actual BillDesk encryption library/API
                    // For now, we'll create a mock encrypted response
                    var mockEncryptedData = $"mock_encrypted_data_{DateTime.UtcNow.Ticks}";
                    
                    _logger.LogInformation("BillDesk encryption completed");
                    return new { Status = "SUCCESS", Message = mockEncryptedData };
                }
                else if (action == "Decrypt")
                {
                    // In production, this would call actual BillDesk decryption library/API
                    // Mock decryption - return a sample payment response
                    var mockResponse = new
                    {
                        links = new[]
                        {
                            new
                            {
                                rel = "redirect",
                                parameters = new
                                {
                                    bdorderid = $"BD{DateTime.UtcNow:yyyyMMddHHmmss}",
                                    rdata = $"RDATA{DateTime.UtcNow.Ticks}"
                                }
                            }
                        }
                    };

                    var jsonResponse = JsonSerializer.Serialize(mockResponse);
                    _logger.LogInformation("BillDesk decryption completed");
                    return new { Status = "SUCCESS", Message = jsonResponse };
                }

                return new { Status = "ERROR", Message = "Unsupported action" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BillDesk service");
                return new { Status = "ERROR", Message = ex.Message };
            }
        }

        private async Task<dynamic> HandleHttpPaymentService(dynamic input)
        {
            try
            {
                _logger.LogInformation("Calling BillDesk HTTP Payment API");

                // In production, this would make actual HTTP calls to BillDesk API
                // Mock response for now
                await Task.Delay(100); // Simulate network delay

                var mockResponse = new
                {
                    links = new[]
                    {
                        new
                        {
                            rel = "redirect",
                            parameters = new
                            {
                                bdorderid = $"BD{DateTime.UtcNow:yyyyMMddHHmmss}",
                                rdata = $"RDATA{DateTime.UtcNow.Ticks}"
                            }
                        }
                    }
                };

                var jsonResponse = JsonSerializer.Serialize(mockResponse);
                var responseBytes = Encoding.UTF8.GetBytes(jsonResponse);

                _logger.LogInformation("HTTP Payment API call completed");
                return new { Status = "SUCCESS", Content = responseBytes };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HTTP Payment service");
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
