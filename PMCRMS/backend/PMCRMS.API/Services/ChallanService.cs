using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PMCRMS.API.Services
{
    public class ChallanService : IChallanService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<ChallanService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _challanStoragePath;

        static ChallanService()
        {
            // Configure QuestPDF to handle missing glyphs (for Marathi/Devanagari fonts)
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public ChallanService(
            PMCRMSDbContext context,
            ILogger<ChallanService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            // Get storage path from configuration or use default
            _challanStoragePath = _configuration["ChallanStoragePath"] 
                ?? Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage", "Challans");

            // Ensure directory exists
            if (!Directory.Exists(_challanStoragePath))
            {
                Directory.CreateDirectory(_challanStoragePath);
                _logger.LogInformation("Created challan storage directory: {Path}", _challanStoragePath);
            }
        }

        public async Task<ChallanGenerationResponse> GenerateChallanAsync(ChallanGenerationRequest request)
        {
            try
            {
                // Check if challan already exists for this application
                var existingChallan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == request.ApplicationId);

                if (existingChallan != null && existingChallan.IsGenerated)
                {
                    _logger.LogWarning("Challan already generated for application {ApplicationId}", request.ApplicationId);
                    return new ChallanGenerationResponse
                    {
                        Success = false,
                        Message = "Challan already generated for this application",
                        ChallanPath = existingChallan.FilePath,
                        ChallanNumber = existingChallan.ChallanNumber
                    };
                }

                // Generate unique challan number
                var challanNumber = GenerateChallanNumber();
                var fileName = $"{challanNumber}.pdf";
                var filePath = Path.Combine(_challanStoragePath, fileName);

                // Generate PDF
                byte[] pdfContent;
                try
                {
                    pdfContent = GenerateChallanPdf(request, challanNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Marathi font rendering failed, falling back to English-only");
                    // Fallback to English-only if Marathi fails
                    pdfContent = GenerateChallanPdfEnglishOnly(request, challanNumber);
                }

                // Save PDF to file
                await File.WriteAllBytesAsync(filePath, pdfContent);
                _logger.LogInformation("Challan PDF saved to {FilePath}", filePath);

                // Save/Update challan record in database
                if (existingChallan != null)
                {
                    existingChallan.ChallanNumber = challanNumber;
                    existingChallan.Name = request.Name;
                    existingChallan.Position = request.Position;
                    existingChallan.Amount = request.Amount;
                    existingChallan.AmountInWords = request.AmountInWords;
                    existingChallan.ChallanDate = request.Date;
                    existingChallan.FilePath = filePath;
                    existingChallan.IsGenerated = true;
                    existingChallan.LastModifiedAt = DateTime.UtcNow;
                    _context.Challans.Update(existingChallan);
                }
                else
                {
                    var challan = new Challan
                    {
                        ApplicationId = request.ApplicationId,
                        ChallanNumber = challanNumber,
                        Name = request.Name,
                        Position = request.Position,
                        Amount = request.Amount,
                        AmountInWords = request.AmountInWords,
                        ChallanDate = request.Date,
                        FilePath = filePath,
                        IsGenerated = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Challans.Add(challan);
                }

                await _context.SaveChangesAsync();

                return new ChallanGenerationResponse
                {
                    Success = true,
                    Message = "Challan generated successfully",
                    ChallanPath = filePath,
                    ChallanNumber = challanNumber,
                    PdfContent = pdfContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating challan for application {ApplicationId}", request.ApplicationId);
                return new ChallanGenerationResponse
                {
                    Success = false,
                    Message = $"Error generating challan: {ex.Message}"
                };
            }
        }

        public async Task<byte[]?> GetChallanPdfAsync(int applicationId)
        {
            try
            {
                var challan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.IsGenerated);

                if (challan == null || !File.Exists(challan.FilePath))
                {
                    _logger.LogWarning("Challan not found for application {ApplicationId}", applicationId);
                    return null;
                }

                return await File.ReadAllBytesAsync(challan.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challan PDF for application {ApplicationId}", applicationId);
                return null;
            }
        }

        public async Task<string?> GetChallanPathAsync(int applicationId)
        {
            try
            {
                var challan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.IsGenerated);

                return challan?.FilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challan path for application {ApplicationId}", applicationId);
                return null;
            }
        }

        public async Task<bool> IsChallanGeneratedAsync(int applicationId)
        {
            try
            {
                return await _context.Challans
                    .AnyAsync(c => c.ApplicationId == applicationId && c.IsGenerated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking challan status for application {ApplicationId}", applicationId);
                return false;
            }
        }

        private string GenerateChallanNumber()
        {
            // Generate unique challan number: CH{yyyyMMdd}{Ticks}
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var uniquePart = DateTime.Now.Ticks.ToString().Substring(8); // Last 10 digits
            return $"CH{datePart}{uniquePart}";
        }

        private byte[] GenerateChallanPdf(ChallanGenerationRequest request, string challanNumber)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);

                    page.Content().Row(row =>
                    {
                        // Left copy of challan
                        row.RelativeItem().Border(1).Width(280).Height(374).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, true));

                        row.ConstantItem(10); // Spacer

                        // Right copy of challan
                        row.RelativeItem().Border(1).Width(280).Height(374).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, true));
                    });
                });
            });

            return document.GeneratePdf();
        }

        private byte[] GenerateChallanPdfEnglishOnly(ChallanGenerationRequest request, string challanNumber)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);

                    page.Content().Row(row =>
                    {
                        // Left copy of challan
                        row.RelativeItem().Border(1).Width(280).Height(374).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, false));

                        row.ConstantItem(10); // Spacer

                        // Right copy of challan
                        row.RelativeItem().Border(1).Width(280).Height(374).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, false));
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void BuildChallanContent(ColumnDescriptor column, ChallanGenerationRequest request, 
            string challanNumber, bool includeMarathi)
        {
            // Header
            column.Item().AlignCenter().Text(text =>
            {
                text.Span("PUNE MUNICIPAL CORPORATION").FontFamily("Arial").FontSize(12).Bold();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("पुणे महानगरपालिका").FontFamily("Nirmala UI").FontSize(12).Bold();
                });
            }

            column.Item().PaddingVertical(5);

            // Challan Title
            column.Item().AlignCenter().Text(text =>
            {
                text.Span("CHALLAN RECEIPT").FontFamily("Arial").FontSize(10).Bold();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("चलन पावती").FontFamily("Nirmala UI").FontSize(10).Bold();
                });
            }

            column.Item().PaddingVertical(5).LineHorizontal(1);

            // Challan Number
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Challan No. / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("चलन क्र.: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span(challanNumber).FontFamily("Arial").FontSize(9).Bold();
            });

            // Date
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Date / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("दिनांक: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span(request.Date.ToString("dd/MM/yyyy")).FontFamily("Arial").FontSize(9).Bold();
            });

            column.Item().PaddingVertical(2).LineHorizontal(1);

            // Name
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Name / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("नाव: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span(request.Name).FontFamily("Arial").FontSize(9);
            });

            // Position
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Position / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("पद: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span(request.Position).FontFamily("Arial").FontSize(9);
            });

            column.Item().PaddingVertical(2).LineHorizontal(1);

            // Amount
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Amount / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("रक्कम: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span($"₹ {request.Amount:N2}").FontFamily("Arial").FontSize(9).Bold();
            });

            // Amount in Words
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("In Words / ").FontFamily("Arial").FontSize(9);
                if (includeMarathi)
                    text.Span("शब्दांत: ").FontFamily("Nirmala UI").FontSize(9);
                text.Span(request.AmountInWords).FontFamily("Arial").FontSize(9).Italic();
            });

            column.Item().PaddingVertical(5).LineHorizontal(1);

            // Footer
            column.Item().AlignCenter().PaddingTop(10).Text(text =>
            {
                text.Span("*** Official Challan ***").FontFamily("Arial").FontSize(8).Italic();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("*** अधिकृत चलन ***").FontFamily("Nirmala UI").FontSize(8).Italic();
                });
            }
        }
    }
}
