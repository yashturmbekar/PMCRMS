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

        /// <summary>
        /// Generates payment challan (receipt) PDF
        /// </summary>
        public async Task<byte[]> GenerateChallanAsync(int applicationId, Guid transactionId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (application == null || transaction == null)
                    throw new Exception("Application or Transaction not found");

                var challanNumber = $"PMC-{transactionId.ToString("N").Substring(0, 8).ToUpper()}-{DateTime.Now:yyyyMMdd}";
                var amountInWords = ConvertToWords(transaction.Price);

                var userName = await _context.Users
                    .Where(u => u.Id == application.UserId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync() ?? "N/A";

                var positionName = GetPositionInMarathi(application.PositionType);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor(Colors.White);

                        page.Content().PaddingVertical(10).Column(column =>
                        {
                            column.Spacing(15);

                            // Title
                            column.Item().AlignCenter().Text("PAYMENT CHALLAN / पेमेंट चलान")
                                .FontSize(20).Bold();

                            column.Item().AlignCenter().Text("Pune Municipal Corporation / पुणे महानगरपालिका")
                                .FontSize(16);

                            column.Item().BorderBottom(2);

                            // Challan Details
                            column.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Challan Number / चलान क्रमांक: {challanNumber}").FontSize(11);
                                    c.Item().Text($"Date / तारीख: {transaction.CreatedAt:dd/MM/yyyy}").FontSize(11);
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignRight().Text($"Transaction ID: {transaction.TransactionId}").FontSize(10);
                                    c.Item().AlignRight().Text($"BillDesk Order: {transaction.BdOrderId}").FontSize(10);
                                });
                            });

                            column.Item().PaddingTop(10).BorderBottom(1);

                            // Applicant Details
                            column.Item().PaddingTop(10).Text("Applicant Details / अर्जदाराची माहिती")
                                .FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Border(1).Padding(5).Text("Name / नाव:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text(userName).FontSize(10);

                                table.Cell().Border(1).Padding(5).Text("Position / पद:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text(positionName).FontSize(10);

                                table.Cell().Border(1).Padding(5).Text("Application No. / अर्ज क्रमांक:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text($"APP-{application.Id:D6}").FontSize(10);
                            });

                            // Payment Details
                            column.Item().PaddingTop(15).Text("Payment Details / पेमेंट तपशील")
                                .FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Border(1).Padding(5).Text("Description / वर्णन:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text("Position Registration Fee / पद नोंदणी शुल्क").FontSize(10);

                                table.Cell().Border(1).Padding(5).Text("Amount / रक्कम:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text($"₹ {transaction.Price:N2}").FontSize(11).Bold();

                                table.Cell().Border(1).Padding(5).Text("Amount in Words / अक्षरी रक्कम:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text(amountInWords).FontSize(10);

                                table.Cell().Border(1).Padding(5).Text("Payment Mode / पेमेंट पद्धत:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text("Online").FontSize(10);

                                table.Cell().Border(1).Padding(5).Text("Status / स्थिती:").FontSize(10);
                                table.Cell().Border(1).Padding(5).Text(transaction.Status ?? "PENDING").FontSize(10).SemiBold()
                                    .FontColor(transaction.Status == "SUCCESS" ? Colors.Green.Medium : Colors.Orange.Medium);
                            });

                            // Footer Note
                            column.Item().PaddingTop(20).Border(1).Padding(10)
                                .Background(Colors.Grey.Lighten3)
                                .Text("This is a computer-generated receipt. No signature required. / ही संगणक-निर्मित पावती आहे. स्वाक्षरीची आवश्यकता नाही.")
                                .FontSize(9).Italic();

                            column.Item().PaddingTop(10).AlignCenter()
                                .Text("For queries, contact: pmcrms@pmc.gov.in | 020-XXXXXXXX")
                                .FontSize(9);
                        });
                    });
                });

                _logger.LogInformation($"Challan generated for application {applicationId}, transaction {transactionId}");
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating challan for application {applicationId}");
                throw;
            }
        }

        /// <summary>
        /// Generates preliminary certificate (before digital signatures)
        /// </summary>
        public async Task<byte[]> GeneratePreliminaryCertificateAsync(int applicationId)
        {
            try
            {
                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                    throw new Exception($"Application {applicationId} not found");

                var userName = await _context.Users
                    .Where(u => u.Id == application.UserId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync() ?? "N/A";

                var userEmail = await _context.Users
                    .Where(u => u.Id == application.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync() ?? "N/A";

                var certificateNumber = $"PMC/CERT/{DateTime.Now.Year}/{application.Id:D6}";
                var issueDate = DateTime.Now;
                var positionName = GetPositionInMarathi(application.PositionType);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.PageColor(Colors.White);

                        page.Content().PaddingVertical(20).Column(column =>
                        {
                            column.Spacing(12);

                            // Header - PMC Logo and Title
                            column.Item().AlignCenter().Text("Pune Municipal Corporation")
                                .FontSize(20).Bold();
                            column.Item().AlignCenter().Text("पुणे महानगरपालिका")
                                .FontSize(18).SemiBold();
                            column.Item().AlignCenter().PaddingBottom(5).BorderBottom(2);

                            // Certificate Title
                            column.Item().PaddingTop(15).AlignCenter().Text("CERTIFICATE OF POSITION REGISTRATION")
                                .FontSize(16).Bold();
                            column.Item().AlignCenter().Text("पद नोंदणीचे प्रमाणपत्र")
                                .FontSize(14).SemiBold();

                            column.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Certificate No.: {certificateNumber}").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"Date: {issueDate:dd/MM/yyyy}").FontSize(9);
                            });

                            column.Item().PaddingTop(15).BorderBottom(1);

                            // Certificate Body
                            column.Item().PaddingTop(15).Text(text =>
                            {
                                text.Span("This is to certify that ").FontSize(11);
                                text.Span(userName).FontSize(11).Bold();
                                text.Span(" has been registered for the position of ").FontSize(11);
                                text.Span(positionName).FontSize(11).Bold();
                                text.Span(" under the Pune Municipal Corporation's Position Registration and Management System.").FontSize(11);
                            });

                            column.Item().PaddingTop(10).Text(text =>
                            {
                                text.Span("याद्वारे प्रमाणित केले जाते की ").FontSize(10);
                                text.Span(userName).FontSize(10).SemiBold();
                                text.Span(" यांची पुणे महानगरपालिकेच्या पद नोंदणी व्यवस्थापन प्रणालीअंतर्गत ").FontSize(10);
                                text.Span(positionName).FontSize(10).SemiBold();
                                text.Span(" या पदासाठी नोंदणी करण्यात आली आहे.").FontSize(10);
                            });

                            // Applicant Details Table
                            column.Item().PaddingTop(20).Text("Applicant Details / अर्जदाराची माहिती")
                                .FontSize(12).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Border(1).Padding(5).Text("Name / नाव").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text(userName).FontSize(9);

                                table.Cell().Border(1).Padding(5).Text("Mobile / मोबाईल").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text(application.MobileNumber ?? "N/A").FontSize(9);

                                table.Cell().Border(1).Padding(5).Text("Email / ईमेल").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text(userEmail).FontSize(9);

                                table.Cell().Border(1).Padding(5).Text("Position / पद").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text(positionName).FontSize(9);

                                table.Cell().Border(1).Padding(5).Text("Application No. / अर्ज क्रमांक").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text($"APP-{application.Id:D6}").FontSize(9);

                                table.Cell().Border(1).Padding(5).Text("Registration Date / नोंदणी तारीख").FontSize(9);
                                table.Cell().Border(1).Padding(5).Text($"{application.CreatedDate:dd/MM/yyyy}").FontSize(9);
                            });

                            // Verification Note
                            column.Item().PaddingTop(20).Border(1).Padding(10)
                                .Background(Colors.Blue.Lighten4)
                                .Column(c =>
                                {
                                    c.Item().Text("Verification Status / पडताळणी स्थिती").FontSize(10).Bold();
                                    c.Item().PaddingTop(5).Text("✓ Document Verification: Completed").FontSize(9);
                                    c.Item().Text("✓ कागदपत्र पडताळणी: पूर्ण").FontSize(9);
                                    c.Item().PaddingTop(5).Text("✓ Payment Verification: Completed").FontSize(9);
                                    c.Item().Text("✓ पेमेंट पडताळणी: पूर्ण").FontSize(9);
                                });

                            // Digital Signature Placeholders
                            column.Item().PaddingTop(30).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Border(1).Padding(10).Height(60).AlignMiddle().AlignCenter()
                                        .Text("[Digital Signature - Executive Engineer]\n[कार्यकारी अभियंता - डिजिटल स्वाक्षरी]")
                                        .FontSize(8).Italic();
                                    c.Item().PaddingTop(5).AlignCenter().Text("Executive Engineer").FontSize(9).Bold();
                                    c.Item().AlignCenter().Text("कार्यकारी अभियंता").FontSize(8);
                                });

                                row.RelativeItem().PaddingLeft(20).Column(c =>
                                {
                                    c.Item().Border(1).Padding(10).Height(60).AlignMiddle().AlignCenter()
                                        .Text("[Digital Signature - City Engineer]\n[शहर अभियंता - डिजिटल स्वाक्षरी]")
                                        .FontSize(8).Italic();
                                    c.Item().PaddingTop(5).AlignCenter().Text("City Engineer").FontSize(9).Bold();
                                    c.Item().AlignCenter().Text("शहर अभियंता").FontSize(8);
                                });
                            });

                            // Footer
                            column.Item().PaddingTop(15).Border(1).Padding(8)
                                .Background(Colors.Grey.Lighten3)
                                .Text("This is a digitally generated certificate. Digital signatures will be applied by authorized officers.\nहे डिजिटलरीत्या तयार केलेले प्रमाणपत्र आहे. अधिकृत अधिकार्‍यांद्वारे डिजिटल स्वाक्षर्‍या लागू केल्या जातील.")
                                .FontSize(7).Italic();
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Generated on: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " | ").FontSize(7);
                            text.Span("Verify at: www.pmc.gov.in/verify").FontSize(7);
                        });
                    });
                });

                _logger.LogInformation($"Preliminary certificate generated for application {applicationId}");
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating certificate for application {applicationId}");
                throw;
            }
        }

        private string ConvertToWords(decimal amount)
        {
            try
            {
                var wholePart = (int)amount;
                var decimalPart = (int)((amount - wholePart) * 100);

                string words = NumberToWords(wholePart);

                if (decimalPart > 0)
                {
                    words += $" Rupees and {NumberToWords(decimalPart)} Paise Only";
                }
                else
                {
                    words += " Rupees Only";
                }

                return words;
            }
            catch
            {
                return amount.ToString("N2") + " Rupees";
            }
        }

        private string NumberToWords(int number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "") words += "and ";

                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words;
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
                            ;

                        DateTime date = _model.Date;
                        int year = date.Year;
                        int toYear = year + 2;

                        text.Span($"\n    विषय:- जानेवारी {year} ते डिसेंबर {toYear} करीता {_model.Position} नवीन परवान्याबाबत.")
                            .FontSize(12)
                            ;

                        text.Span($"\n    विषयांकित प्रकरणी खाली निर्देशित व्यक्तीने जानेवारी {year} ते डिसेंबर {toYear} या कालावधीकरीता पुणे महानगरपालिकेच्या मा. शहर अभियंता कार्यालयाकडे {_model.Position} (नवीन) परवान्याकरिता  अर्ज केला आहे.\n")
                            .FontSize(12)
                            
                            .LineHeight(0.8f);

                        text.Span($"          अर्जदाराचे नाव - ")
                            .FontSize(12)
                            
                            .LineHeight(0.8f);

                        text.Span(_model.Name)
                            .FontSize(12)
                            
                            .Bold()
                            .LineHeight(0.8f);

                        text.Span($"\n          अर्जदाराचे शिक्षण - ")
                            .FontSize(12)
                            
                            .LineHeight(0.8f);

                        if (_model.Qualification.Count > 0)
                        {
                            text.Span($"1) {_model.Qualification[0]}")
                                .FontSize(12)
                                
                                .Bold()
                                .LineHeight(0.8f);

                            if (_model.Qualification.Count > 1 && !string.IsNullOrWhiteSpace(_model.Qualification[1]))
                            {
                                text.Span($"\n                                                     2) {_model.Qualification[1]}")
                                    .FontSize(12)
                                    
                                    .Bold()
                                    .LineHeight(0.8f);
                            }
                            else
                            {
                                text.Span($"\n")
                                    .FontSize(12)
                                    
                                    .Bold();
                            }
                        }

                        text.Span($"\n          पत्ता : ")
                            .FontSize(12)
                            ;

                        text.Span($"1) {_model.Address1}")
                            .FontSize(12)
                            
                            .Bold();

                        if (!_model.IsBothAddressSame)
                        {
                            text.Span($"\n                                2) {_model.Address2}")
                                .FontSize(12)
                                
                                .Bold();
                        }
                        else
                        {
                            text.Span($"\n ")
                                .FontSize(12)
                                
                                .Bold();
                        }

                        text.Span($"\n          मोबाईलनं.- ")
                            .FontSize(12)
                            ;

                        text.Span(_model.MobileNumber)
                            .FontSize(12)
                            
                            .Bold()
                            .LineHeight(0.8f);

                        text.Span("\n          आवश्यक अनुभव - २ वर्षे (युडीसीपीआर २०२० मधील अपेंडिक्स 'सी', सी-४.१")
                            
                            .FontSize(12)
                            .LineHeight(0.8f);

                        text.Span("(ii)")
                            
                            .FontSize(12);

                        text.Span(" नुसार)")
                            
                            .FontSize(12)
                            .LineHeight(0.8f);

                        text.Span($"\n          अनुभव- ")
                            .FontSize(12)
                            
                            .LineHeight(0.8f);

                        text.Span(_model.YearDifference ?? "0")
                            .FontSize(12)
                            
                            .Bold();

                        text.Span($" वर्षे ")
                            .FontSize(12)
                            ;

                        text.Span(_model.MonthDifference ?? "0")
                            .FontSize(12)
                            
                            .Bold();

                        text.Span($" महिने")
                            .FontSize(12)
                            ;
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
                        
                        .FontSize(12)
                        .LineHeight(0.8f);

                    DateTime date = _model.Date;
                    int year = date.Year;
                    int toYear = year + 2;

                    text.Span("     तरी सदर प्रकरणी ")
                        
                        .FontSize(12)
                        .LineHeight(0.8f);

                    text.Span(_model.Name + " ")
                        .FontSize(12)
                        
                        .Bold()
                        .LineHeight(0.8f);

                    text.Span($" यांचेकडून जानेवारी {year} ते डिसेंबर {toYear} या कालावधी करिता आवश्यक ती फी भरून घेवून {_model.Position} (नवीन) परवाना देणेबाबत मान्यता मिळणेस विनंती आहे.")
                        
                        .FontSize(12)
                        .LineHeight(0.8f);
                });

                column.Item().PaddingLeft(20).PaddingRight(20).Text(text =>
                {
                    text.Line("मा.स.कळावे.")
                        
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
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शाखा अभियंता")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शहर-अभियंता कार्यालय")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        
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
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("उपअभियंता")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                });
                            });
                        });

                        column2.Item().AlignLeft().Text(text =>
                        {
                            text.Line("प्रस्तुत प्रकरणी उपरोक्त प्रमाणे छाननी झाली असल्याने मान्यतेस शिफारस आहे.")
                                
                                .FontSize(12)
                                .LineHeight(0.8f);
                        });

                        column2.Item().AlignRight().PaddingRight(110).PaddingTop(30).PaddingBottom(0).Text(text =>
                        {
                            text.Line("क्ष मान्य")
                                
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
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("कार्यकारी अभियंता")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        
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
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("शहर अभियंता ")
                                        
                                        .FontSize(12)
                                        .LineHeight(0.8f);
                                    text.Line("पुणे महानगरपालिका")
                                        
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
