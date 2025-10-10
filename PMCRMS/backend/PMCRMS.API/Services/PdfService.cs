using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.ViewModels;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PMCRMS.API.Services
{
    public class PdfService
    {
        private readonly PMCRMSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PdfService> _logger;
        private static bool _fontsRegistered = false;

        public PdfService(PMCRMSDbContext context, IWebHostEnvironment environment, ILogger<PdfService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            RegisterFonts();
        }

        private static void RegisterFonts()
        {
            if (_fontsRegistered) return;

            try
            {
                // Using system fonts available on Windows
                // Nirmala UI: Excellent Devanagari support
                // Segoe UI: Standard Windows UI font
                // These fonts are available on Windows 10+ by default
                _fontsRegistered = true;
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the service initialization
                Console.WriteLine($"Failed to register fonts: {ex.Message}");
            }
        }

        public async Task<PdfGenerationResponse> GenerateApplicationPdfAsync(int applicationId)
        {
            try
            {
                var applicationData = await GetApplicationDataAsync(applicationId);
                if (applicationData == null)
                {
                    return new PdfGenerationResponse
                    {
                        IsSuccess = false,
                        Message = "Application not found"
                    };
                }

                var pdfBytes = GeneratePdf(applicationData);
                var fileName = $"Application_{applicationId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var filePath = await SavePdfFileAsync(pdfBytes, fileName);

                // Update application with PDF path
                await UpdateApplicationPdfPathAsync(applicationId, filePath);

                return new PdfGenerationResponse
                {
                    IsSuccess = true,
                    Message = "PDF generated successfully",
                    FilePath = filePath,
                    FileContent = pdfBytes,
                    FileName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for application ID: {ApplicationId}", applicationId);
                return new PdfGenerationResponse
                {
                    IsSuccess = false,
                    Message = $"Error generating PDF: {ex.Message}"
                };
            }
        }

        private async Task<ApplicationPdfModel?> GetApplicationDataAsync(int applicationId)
        {
            var application = await _context.PositionApplications
                .Include(a => a.Addresses)
                .Include(a => a.Qualifications)
                .Include(a => a.Experiences)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return null;

            // Calculate experience
            var totalExperience = CalculateTotalExperience(application.Experiences.ToList());

            // Get addresses
            var permanentAddress = application.Addresses.FirstOrDefault(a => a.AddressType == "Permanent");
            var currentAddress = application.Addresses.FirstOrDefault(a => a.AddressType == "Current");

            // Get officer names - For now, using placeholder names
            // In the future, you can add officer assignment tracking to PositionApplication
            var jrEnggName = "शाखा अभियंता"; // Placeholder
            var assEnggName = "उपअभियंता"; // Placeholder
            var exeEnggName = "कार्यकारी अभियंता"; // Placeholder
            var cityEnggName = "शहर अभियंता"; // Placeholder

            return new ApplicationPdfModel
            {
                Name = $"{application.FirstName} {application.MiddleName} {application.LastName}".Trim(),
                Address1 = GetFormattedAddress(permanentAddress),
                Address2 = GetFormattedAddress(currentAddress),
                Position = GetPositionInMarathi(application.PositionType),
                Date = application.SubmittedDate ?? application.CreatedDate,
                Qualification = application.Qualifications?.Select(q => q.DegreeName).ToList() ?? new List<string>(),
                MobileNumber = application.MobileNumber,
                MonthDifference = totalExperience.Months.ToString(),
                YearDifference = totalExperience.Years.ToString(),
                IsBothAddressSame = permanentAddress?.Id == currentAddress?.Id,
                JrEnggName = jrEnggName,
                AssEnggName = assEnggName,
                ExeEnggName = exeEnggName,
                CityEnggName = cityEnggName
            };
        }

        private (int Years, int Months) CalculateTotalExperience(List<SEExperience>? experiences)
        {
            if (experiences == null || !experiences.Any())
                return (0, 0);

            var totalMonths = 0;
            foreach (var exp in experiences)
            {
                var months = ((exp.ToDate.Year - exp.FromDate.Year) * 12) + exp.ToDate.Month - exp.FromDate.Month;
                totalMonths += months;
            }

            var years = totalMonths / 12;
            var remainingMonths = totalMonths % 12;

            return (years, remainingMonths);
        }

        private string GetFormattedAddress(SEAddress? address)
        {
            if (address == null) return string.Empty;

            var addressParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address.AddressLine1))
                addressParts.Add(address.AddressLine1);

            if (!string.IsNullOrWhiteSpace(address.AddressLine2))
                addressParts.Add(address.AddressLine2);

            if (!string.IsNullOrWhiteSpace(address.AddressLine3))
                addressParts.Add(address.AddressLine3);

            if (!string.IsNullOrWhiteSpace(address.City))
                addressParts.Add(address.City);

            if (!string.IsNullOrWhiteSpace(address.State))
                addressParts.Add(address.State);

            if (!string.IsNullOrWhiteSpace(address.PinCode))
                addressParts.Add(address.PinCode);

            return string.Join(", ", addressParts);
        }

        private string GetPositionInMarathi(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "आर्किटेक्ट",
                PositionType.StructuralEngineer => "स्ट्रक्चरल इंजिनिअर",
                PositionType.LicenceEngineer => "लायसन्स इंजिनिअर",
                PositionType.Supervisor1 => "सुपरवायझर1",
                PositionType.Supervisor2 => "सुपरवायझर2",
                _ => "अज्ञात"
            };
        }

        private byte[] GeneratePdf(ApplicationPdfModel model)
        {
            try
            {
                var document = new ApplicationPdfDocument(model);
                return document.GeneratePdf();
            }
            catch (Exception ex) when (ex.Message.Contains("typeface") || ex.Message.Contains("font"))
            {
                // If font issue occurs, provide more helpful error message
                throw new Exception($"Font rendering error: {ex.Message}. Please ensure your system has Unicode-capable fonts installed.");
            }
        }

        private async Task<string> SavePdfFileAsync(byte[] pdfBytes, string fileName)
        {
            var uploadsDirectory = Path.Combine(_environment.WebRootPath ?? "wwwroot", "generated-pdfs");

            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }

            var filePath = Path.Combine(uploadsDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return Path.Combine("generated-pdfs", fileName); // Return relative path for storage
        }

        private async Task UpdateApplicationPdfPathAsync(int applicationId, string filePath)
        {
            var application = await _context.PositionApplications.FindAsync(applicationId);
            if (application != null)
            {
                application.Remarks = $"PDF generated: {filePath}"; // Store PDF path in Remarks for now
                await _context.SaveChangesAsync();
            }
        }
    }

    public class ApplicationPdfDocument : IDocument
    {
        private readonly ApplicationPdfModel _model;

        // Font names - use available system fonts that support Unicode characters
        // Nirmala UI: Excellent Devanagari support (Windows 8+)
        // Fallbacks: Microsoft Sans Serif, Arial, Tahoma
        private const string MarathiFont = "Nirmala UI";
        private const string EnglishFont = "Segoe UI";

        public ApplicationPdfDocument(ApplicationPdfModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Legal);
                page.Background().Padding(20).Border(1);
                page.Margin(1, Unit.Inch);
                page.Margin(30);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .AlignLeft()
                        .Text("मा. शहर अभियंता\nपुणे महानगरपालिका")
                        .Bold()
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .LineHeight(0.8f);

                    row.RelativeItem();
                });

                column.Item()
                    .PaddingLeft(20)
                    .PaddingRight(20)
                    .AlignLeft()
                    .Text(text =>
                    {
                        text.Span($"                   यांजकडे सादर....")
                            .FontSize(14)
                            .FontFamily(MarathiFont);

                        DateTime date = _model.Date;
                        int year = date.Year;
                        int toYear = year + 2;

                        text.Span($"\n    विषय:- जानेवारी {year} ते डिसेंबर {toYear} करीता {_model.Position} नवीन परवान्याबाबत.")
                            .FontSize(12)
                            .FontFamily(MarathiFont);

                        text.Span($"\n    विषयांकित प्रकरणी खाली निर्देशित व्यक्तीने जानेवारी {year} ते डिसेंबर {toYear} या कालावधीकरीता पुणे महानगरपालिकेच्या मा. शहर अभियंता कार्यालयाकडे {_model.Position} (नवीन) परवान्याकरिता  अर्ज केला आहे.\n")
                            .FontSize(12)
                            .FontFamily(MarathiFont)
                            .LineHeight(0.8f);

                        text.Span($"          अर्जदाराचे नाव - ")
                            .FontSize(12)
                            .FontFamily(MarathiFont)
                            .LineHeight(0.8f);

                        text.Span(_model.Name)
                            .FontSize(12)
                            .FontFamily(EnglishFont)
                            .Bold()
                            .LineHeight(0.8f);

                        text.Span($"\n          अर्जदाराचे शिक्षण - ")
                            .FontSize(12)
                            .FontFamily(MarathiFont)
                            .LineHeight(0.8f);

                        if (_model.Qualification.Count > 0)
                        {
                            text.Span($"1) {_model.Qualification[0]}")
                                .FontSize(12)
                                .FontFamily(EnglishFont)
                                .Bold()
                                .LineHeight(0.8f);

                            if (_model.Qualification.Count > 1 && !string.IsNullOrWhiteSpace(_model.Qualification[1]))
                            {
                                text.Span($"\n                                                     2) {_model.Qualification[1]}")
                                    .FontSize(12)
                                    .FontFamily(EnglishFont)
                                    .Bold()
                                    .LineHeight(0.8f);
                            }
                            else
                            {
                                text.Span($"\n")
                                    .FontSize(12)
                                    .FontFamily(EnglishFont)
                                    .Bold();
                            }
                        }

                        text.Span($"\n          पत्ता : ")
                            .FontSize(12)
                            .FontFamily(MarathiFont);

                        text.Span($"1) {_model.Address1}")
                            .FontSize(12)
                            .FontFamily(EnglishFont)
                            .Bold();

                        if (!_model.IsBothAddressSame)
                        {
                            text.Span($"\n                                2) {_model.Address2}")
                                .FontSize(12)
                                .FontFamily(EnglishFont)
                                .Bold();
                        }
                        else
                        {
                            text.Span($"\n ")
                                .FontSize(12)
                                .FontFamily(EnglishFont)
                                .Bold();
                        }

                        text.Span($"\n          मोबाईलनं.- ")
                            .FontSize(12)
                            .FontFamily(MarathiFont);

                        text.Span(_model.MobileNumber)
                            .FontSize(12)
                            .FontFamily(EnglishFont)
                            .Bold()
                            .LineHeight(0.8f);

                        text.Span("\n          आवश्यक अनुभव - २ वर्षे (युडीसीपीआर २०२० मधील अपेंडिक्स 'सी', सी-४.१")
                            .FontFamily(MarathiFont)
                            .FontSize(12)
                            .LineHeight(0.8f);

                        text.Span("(ii)")
                            .FontFamily(EnglishFont)
                            .FontSize(12);

                        text.Span(" नुसार)")
                            .FontFamily(MarathiFont)
                            .FontSize(12)
                            .LineHeight(0.8f);

                        text.Span($"\n          अनुभव- ")
                            .FontSize(12)
                            .FontFamily(MarathiFont)
                            .LineHeight(0.8f);

                        text.Span(_model.YearDifference ?? "0")
                            .FontSize(12)
                            .FontFamily(EnglishFont)
                            .Bold();

                        text.Span($" वर्षे ")
                            .FontSize(12)
                            .FontFamily(MarathiFont);

                        text.Span(_model.MonthDifference ?? "0")
                            .FontSize(12)
                            .FontFamily(EnglishFont)
                            .Bold();

                        text.Span($" महिने")
                            .FontSize(12)
                            .FontFamily(MarathiFont);
                    });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(5).PaddingLeft(20).PaddingRight(20).Text(text =>
                {
                    var num = _model.Position switch
                    {
                        "स्ट्रक्चरल इंजिनिअर" => "4",
                        "लायसन्स इंजिनिअर" => "3",
                        "सुपरवायझर1" => "5.1",
                        "सुपरवायझर2" => "5.1",
                        _ => "4"
                    };

                    text.Line($"    उपरोक्त नमूद केलेल्या व्यक्तीचा मागणी अर्ज, शैक्षणिक पात्रता, अनुभव व पत्त्याचा पुरावा इ. कागदपत्राची तपासणी केली ती बरोबर व नियमानुसार आहेत. त्यानुसार वरील अर्जदाराची मान्य युडीसीपीआर २०२० मधील अपेंडिक्स सी, सी-{num} नुसार पुणे महानगरपालिकेच्या {_model.Position} (नवीन) परवाना धारण करण्यास आवश्यक शैक्षणिक पात्रता व अनुभव असल्याने त्यांचा अर्ज आपले मान्यतेकरिता सादर करीत आहोत.")
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .LineHeight(0.8f);

                    DateTime date = _model.Date;
                    int year = date.Year;
                    int toYear = year + 2;

                    text.Span("     तरी सदर प्रकरणी ")
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .LineHeight(0.8f);

                    text.Span(_model.Name + " ")
                        .FontSize(12)
                        .FontFamily(EnglishFont)
                        .Bold()
                        .LineHeight(0.8f);

                    text.Span($" यांचेकडून जानेवारी {year} ते डिसेंबर {toYear} या कालावधी करिता आवश्यक ती फी भरून घेवून {_model.Position} (नवीन) परवाना देणेबाबत मान्यता मिळणेस विनंती आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .LineHeight(0.8f);
                });

                column.Item().PaddingLeft(20).PaddingRight(20).Text(text =>
                {
                    text.Line("मा.स.कळावे.")
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .LineHeight(0.8f);
                });

                // Fixed-position signature/footer section
                column.Item().Extend().AlignBottom().Column(column =>
                {
                    column.Item().PaddingLeft(20).PaddingRight(20).Column(column2 =>
                    {
                        column2.Item().PaddingTop(80).Row(row =>
                        {
                            row.RelativeItem(1).Column(col =>
                            {
                                col.Item().PaddingTop(10).Height(60);
                                col.Item().AlignCenter().Text(text =>
                                {
                                    text.Line($"({_model.JrEnggName ?? ""})")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शाखा अभियंता")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शहर-अभियंता कार्यालय")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                });
                            });

                            row.RelativeItem(1).Column(col =>
                            {
                                col.Item().PaddingTop(20).Height(60);
                                col.Item().AlignCenter().Text(text =>
                                {
                                    text.Line($"({_model.AssEnggName ?? ""})")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("उपअभियंता")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                });
                            });
                        });

                        column2.Item().AlignLeft().Text(text =>
                        {
                            text.Line("प्रस्तुत प्रकरणी उपरोक्त प्रमाणे छाननी झाली असल्याने मान्यतेस शिफारस आहे.")
                                .FontFamily(MarathiFont)
                                .FontSize(12)
                                .LineHeight(0.8f);
                        });

                        column2.Item().AlignRight().PaddingRight(110).PaddingTop(30).PaddingBottom(0).Text(text =>
                        {
                            text.Line("क्ष मान्य")
                                .FontFamily(MarathiFont)
                                .FontSize(12)
                                .LineHeight(0.2f);
                        });

                        column2.Item().Row(row =>
                        {
                            row.RelativeItem(1).Column(col =>
                            {
                                col.Item().PaddingTop(10).Height(60);
                                col.Item().AlignCenter().Text(text =>
                                {
                                    text.Line($"({_model.ExeEnggName ?? ""})")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("कार्यकारी अभियंता")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                });
                            });

                            row.RelativeItem(1).Column(col =>
                            {
                                col.Item().PaddingTop(10).Height(60);
                                col.Item().AlignCenter().Text(text =>
                                {
                                    text.Line($"({_model.CityEnggName ?? ""})")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शहर अभियंता ")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        .FontFamily(MarathiFont)
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                });
                            });
                        });
                    });
                });
            });
        }
    }
}
