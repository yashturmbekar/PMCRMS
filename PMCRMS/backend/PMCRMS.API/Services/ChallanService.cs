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

        static ChallanService()
        {
            // Configure QuestPDF license for Community use (revenue < $1M USD)
            QuestPDF.Settings.License = LicenseType.Community;
            
            // Enable debugging to get detailed layout information
            QuestPDF.Settings.EnableDebugging = true;
            
            // Configure QuestPDF to handle missing glyphs (for Marathi/Devanagari fonts)
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public ChallanService(
            PMCRMSDbContext context,
            ILogger<ChallanService> logger)
        {
            _context = context;
            _logger = logger;

            _logger.LogInformation("ChallanService initialized - Database-only storage (no physical files)");
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

                // Generate PDF in memory only (NO physical file storage)
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

                _logger.LogInformation("Challan PDF generated in memory (database-only storage), size: {Size} KB", pdfContent.Length / 1024.0);

                // Save/Update challan record in database (NO physical file storage)
                if (existingChallan != null)
                {
                    existingChallan.ChallanNumber = challanNumber;
                    existingChallan.Name = request.Name;
                    existingChallan.Position = request.Position;
                    existingChallan.Amount = request.Amount;
                    existingChallan.AmountInWords = request.AmountInWords;
                    existingChallan.ChallanDate = request.Date.ToUniversalTime();
                    existingChallan.FilePath = string.Empty; // No physical file - database storage only
                    existingChallan.PdfContent = pdfContent; // Store PDF binary in database
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
                        ChallanDate = request.Date.ToUniversalTime(),
                        FilePath = string.Empty, // No physical file - database storage only
                        PdfContent = pdfContent, // Store PDF binary in database
                        IsGenerated = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Challans.Add(challan);
                }

                // Save challan PDF to SEDocuments table (primary binary storage)
                var existingDocument = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == request.ApplicationId 
                                           && d.DocumentType == SEDocumentType.PaymentChallan);

                if (existingDocument != null)
                {
                    existingDocument.FileName = fileName;
                    existingDocument.FilePath = string.Empty; // No physical file - database storage only
                    existingDocument.FileContent = pdfContent; // Store PDF binary in database
                    existingDocument.FileSize = (decimal)(pdfContent.Length / 1024.0); // Size in KB
                    existingDocument.ContentType = "application/pdf";
                    existingDocument.UpdatedDate = DateTime.UtcNow;
                    _context.SEDocuments.Update(existingDocument);
                }
                else
                {
                    var seDocument = new SEDocument
                    {
                        ApplicationId = request.ApplicationId,
                        DocumentType = SEDocumentType.PaymentChallan,
                        FileName = fileName,
                        FileId = challanNumber,
                        FilePath = string.Empty, // No physical file - database storage only
                        FileContent = pdfContent, // Store PDF binary in database
                        FileSize = (decimal)(pdfContent.Length / 1024.0), // Size in KB
                        ContentType = "application/pdf",
                        IsVerified = true, // Auto-verify payment challans
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.SEDocuments.Add(seDocument);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Challan saved to database only (Challans + SEDocuments tables) for application {ApplicationId}", request.ApplicationId);

                return new ChallanGenerationResponse
                {
                    Success = true,
                    Message = "Challan generated successfully (stored in database)",
                    ChallanPath = string.Empty, // No physical file - database storage only
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
                // First try to get from SEDocuments table (primary storage)
                var document = await _context.SEDocuments
                    .FirstOrDefaultAsync(d => d.ApplicationId == applicationId 
                                           && d.DocumentType == SEDocumentType.PaymentChallan);

                if (document?.FileContent != null)
                {
                    _logger.LogInformation("Retrieved challan from SEDocuments table for application {ApplicationId}", applicationId);
                    return document.FileContent;
                }

                // Fallback to Challans table
                var challan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.IsGenerated);

                if (challan?.PdfContent != null)
                {
                    _logger.LogInformation("Retrieved challan PDF content from Challans table for application {ApplicationId}", applicationId);
                    return challan.PdfContent;
                }

                _logger.LogWarning("Challan not found in database for application {ApplicationId}", applicationId);
                return null;
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
                        row.RelativeItem().Border(1).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, true));

                        row.ConstantItem(10); // Spacer

                        // Right copy of challan
                        row.RelativeItem().Border(1).Padding(10)
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
                        row.RelativeItem().Border(1).Padding(10)
                            .Column(column => BuildChallanContent(column, request, challanNumber, false));

                        row.ConstantItem(10); // Spacer

                        // Right copy of challan
                        row.RelativeItem().Border(1).Padding(10)
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
                text.Span("PUNE MUNICIPAL CORPORATION").FontFamily(FontService.EnglishFontFamily).FontSize(12).Bold();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("पुणे महानगरपालिका").FontFamily(FontService.MarathiFontFamily).FontSize(12).Bold();
                });
            }

            column.Item().PaddingVertical(5);

            // Challan Title
            column.Item().AlignCenter().Text(text =>
            {
                text.Span("CHALLAN RECEIPT").FontFamily(FontService.EnglishFontFamily).FontSize(10).Bold();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("चलन पावती").FontFamily(FontService.MarathiFontFamily).FontSize(10).Bold();
                });
            }

            column.Item().PaddingVertical(5).LineHorizontal(1);

            // Challan Number
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Challan No. / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("चलन क्र.: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span(challanNumber).FontFamily(FontService.EnglishFontFamily).FontSize(9).Bold();
            });

            // Date
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Date / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("दिनांक: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span(request.Date.ToString("dd/MM/yyyy")).FontFamily(FontService.EnglishFontFamily).FontSize(9).Bold();
            });

            column.Item().PaddingVertical(2).LineHorizontal(1);

            // Name
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Name / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("नाव: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span(request.Name).FontFamily(FontService.EnglishFontFamily).FontSize(9);
            });

            // Position
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Position / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("पद: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span(request.Position).FontFamily(FontService.EnglishFontFamily).FontSize(9);
            });

            column.Item().PaddingVertical(2).LineHorizontal(1);

            // Amount
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("Amount / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("रक्कम: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span($"₹ {request.Amount:N2}").FontFamily(FontService.EnglishFontFamily).FontSize(9).Bold();
            });

            // Amount in Words
            column.Item().PaddingVertical(3).Text(text =>
            {
                text.Span("In Words / ").FontFamily(FontService.EnglishFontFamily).FontSize(9);
                if (includeMarathi)
                    text.Span("शब्दांत: ").FontFamily(FontService.MarathiFontFamily).FontSize(9);
                text.Span(request.AmountInWords).FontFamily(FontService.EnglishFontFamily).FontSize(9).Italic();
            });

            column.Item().PaddingVertical(5).LineHorizontal(1);

            // Footer
            column.Item().AlignCenter().PaddingTop(10).Text(text =>
            {
                text.Span("*** Official Challan ***").FontFamily(FontService.EnglishFontFamily).FontSize(8).Italic();
            });

            if (includeMarathi)
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span("*** अधिकृत चलन ***").FontFamily(FontService.MarathiFontFamily).FontSize(8).Italic();
                });
            }
        }
    }
}
