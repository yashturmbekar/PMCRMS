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

            // Font registration is now handled by FontService in Program.cs startup
            // This ensures fonts are registered once globally for all PDF services
            _fontsRegistered = true;
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
                var fileName = $"RecommendedForm_{applicationId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                // NO physical file storage - return PDF bytes only for database storage
                _logger.LogInformation("Recommendation form PDF generated in memory (database-only storage) for application {ApplicationId}, size: {Size} KB", 
                    applicationId, pdfBytes.Length / 1024.0);

                return new PdfGenerationResponse
                {
                    IsSuccess = true,
                    Message = "PDF generated successfully (stored in database)",
                    FilePath = string.Empty, // No physical file - database storage only
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

        // REMOVED: SavePdfFileAsync and UpdateApplicationPdfPathAsync methods
        // Recommendation forms are now stored ONLY in database (SEDocuments table)
        // No physical file storage is used

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

        /// <summary>
        /// Generate Licence Certificate PDF with Marathi content, logo, profile photo, and QR code
        /// Based on the old SECertificateGenerationService template
        /// </summary>
        public async Task<byte[]> GenerateLicenceCertificatePdfAsync(int applicationId)
        {
            try
            {
                _logger.LogInformation("Generating licence certificate PDF for application {ApplicationId}", applicationId);

                // Get application details
                var application = await _context.PositionApplications
                    .Include(a => a.Addresses)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    throw new Exception($"Application {applicationId} not found");
                }

                // Get challan information for payment details
                var challan = await _context.Challans
                    .Where(c => c.ApplicationId == applicationId)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (challan == null)
                {
                    throw new Exception($"Challan not found for application {applicationId}");
                }

                // Generate certificate number
                string certificatePrefix = GetCertificatePrefix(application.PositionType);
                int count = await _context.PositionApplications
                    .CountAsync(a => a.ApplicationNumber != null &&
                                a.ApplicationNumber.Contains(certificatePrefix) &&
                                a.CreatedDate.Year == DateTime.Now.Year) + 1;

                string certificateNumber = $"PMC/{certificatePrefix}/{count}/{DateTime.Now.Year}-{DateTime.Now.Year + 3}";

                // Get current address
                var currentAddress = application.Addresses.FirstOrDefault(a => a.AddressType == "Current");
                string fullAddress = currentAddress != null
                    ? $"{currentAddress.AddressLine1}, {currentAddress.City} {currentAddress.State} {currentAddress.PinCode}"
                    : "";

                // Get position in Marathi
                string marathiPosition = GetMarathiPosition(application.PositionType);
                string englishPosition = GetEnglishPosition(application.PositionType);

                // Generate QR Code data
                string qrData = $"{certificateNumber}|{application.FirstName} {application.LastName}|{DateTime.Now:dd/MM/yyyy}";

                // Get applicant name
                string applicantName = $"{application.FirstName} {(string.IsNullOrEmpty(application.MiddleName) ? "" : application.MiddleName + " ")}{application.LastName}".Trim();

                // Load logo and profile photo from wwwroot/Images/Certificate
                var logoPath = Path.Combine(_environment.WebRootPath, "Images", "Certificate", "logo.png");
                var profilePhotoPath = Path.Combine(_environment.WebRootPath, "Images", "Certificate", "profile.png");

                byte[]? logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
                byte[]? profilePhotoBytes = File.Exists(profilePhotoPath) ? File.ReadAllBytes(profilePhotoPath) : null;

                if (logoBytes == null || profilePhotoBytes == null)
                {
                    throw new Exception("Required images not found. Please ensure logo.png and profile.png exist in wwwroot/Images/Certificate");
                }

                DateTime fromDate = DateTime.Now;
                int toYear = DateTime.Now.Year + 3;

                // Generate PDF using QuestPDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape()); // Legal size landscape for certificate
                        page.Margin(30);
                        page.PageColor(Colors.White);

                        page.Content().Column(column =>
                        {
                            // Header section
                            column.Item().Row(row =>
                            {
                                // Logo (Left)
                                row.RelativeItem(1).AlignCenter().PaddingTop(10).Height(80).Width(100)
                                    .Image(logoBytes);

                                // Title (Center)
                                row.RelativeItem(2).AlignCenter().Column(col =>
                                {
                                    col.Item().Text("पुणे महानगरपालिका")
                                        .FontSize(16)
                                        .Bold();

                                    col.Item().Text($"{marathiPosition} च्या कामासाठी परवाना")
                                        .FontSize(12);
                                });

                                // Profile Photo (Right)
                                row.RelativeItem(1).AlignCenter().PaddingTop(10).Height(80).Width(100)
                                    .Border(1)
                                    .Image(profilePhotoBytes);
                            });

                            column.Item().Height(10);

                            // Legal reference paragraph
                            column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२०२० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२०२०) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम १९४९ चे कलम ३७२ अन्वये {marathiPosition} काम करण्यास परवाना देण्यात येत आहे.")
                                .FontSize(10)
                                .LineHeight(1.2f);

                            column.Item().Height(10);

                            // Certificate number and validity
                            column.Item().Text(text =>
                            {
                                text.Span($"परवाना क्र. :- ").FontSize(10);
                                text.Span($"{certificateNumber}    From {fromDate:dd/MM/yyyy} to 31/12/{toYear}    ({englishPosition})")
                                    .Bold()
                                    .FontSize(10);
                            });

                            column.Item().Height(10);

                            // Name
                            column.Item().Text(text =>
                            {
                                text.Span($"नाव :- ").FontSize(10);
                                text.Span(applicantName).Bold().FontSize(10);
                            });

                            // Address and Date Row
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("पत्ता :- ").FontSize(10);
                                    text.Span(fullAddress).Bold().FontSize(10);
                                });

                                row.ConstantItem(150).AlignRight().Text(text =>
                                {
                                    text.Span("दिनांक :- ").FontSize(10);
                                    text.Span($"{fromDate:dd/MM/yyyy}").FontSize(10);
                                });
                            });

                            column.Item().Height(10);

                            // Terms paragraph 1
                            column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२०२० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२०२०) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम, १९४९ चे कलम ३७२ अन्वये मी तुम्हांस वर निर्देश केलेल्या कायदा व नियमानुसार ३ वर्षांकरीता दि. {fromDate:dd/MM/yyyy} ते 31/12/{toYear} अखेर {marathiPosition} म्हणून 'खालील मर्यादा व अटी यांचे पालन करणार' या अटीवर परवाना देत आहे.")
                                .FontSize(10)
                                .LineHeight(1.2f);

                            column.Item().Height(5);

                            // Terms paragraph 2
                            column.Item().Text("'मा. महापालिका आयुक्त, यांनी वेळोवेळी स्थायी समितीच्या संमतीने वरील कायद्याचे कलम ३७३ परवानाधारण करणार यांच्या माहितीसाठी काढण्यात आलेल्या आज्ञेचे आणि विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीतील अपेंडिक्स 'सी' मधील कर्तव्ये व जबाबदारी यांचे पालन करणार' ही परवानगीची अट राहील आणि धंद्याच्या प्रत्येक बाबतीत परवान्याच्या मुदतीत ज्यावेळी तुमचा सल्ला घेण्यात येईल त्यावेळी तुम्ही आतापावेतो निघालेल्या आज्ञांचे पालन करून त्याप्रमाणे काम करावयाचे आहे.")
                                .FontSize(10)
                                .LineHeight(1.2f);

                            column.Item().Height(5);

                            // Terms paragraphs 3-4
                            column.Item().Text("जी आज्ञापत्रक वेळोवेळी काढण्यात आलेली आहेत, ती मुख्य कार्यालयाकडे माहितीसाठी ठेवण्यात आलेली असून, जरूर त्यावेळी कार्यालयाच्या वेळेमध्ये त्यांची पाहणी करता येईल.")
                                .FontSize(10)
                                .LineHeight(1.2f);

                            column.Item().Height(5);

                            column.Item().Text("मात्र हे लक्षात घेणे जरूर आहे की, मा. महापालिका आयुक्त सदरचा परवाना महाराष्ट्र महानगरपालिका अधिनियम, कलम ३८६ अनुसार जरूर तेव्हा तात्पुरता बंद अगर रद्द करू शकतात जर वर निर्दिष्ट केलेली बंधने अगर शर्थी यांचा भंग झाला अगर टाळल्या गेल्या अथवा तुम्ही सदर कायद्याच्या नियमांचे अगर वेळोवेळी काढण्यात आलेल्या आज्ञापत्रकाचे उल्लंघन केल्याचे दृष्टोपतीस आल्यास आणि जर सदरचा परवाना तात्पुरता तहकूब अगर रद्द झाल्यास अथवा सदरच्या परवान्याची मुदत संपल्यावर तुम्हास परवाना नसल्याचे समजले जाईल आणि महानगरपालिका अधिनियमाचे कलम ६९ अन्वये मा.महापालिका आयुक्त, अगर त्यांनी अधिकार दिलेल्या अधिका-यांनी सदर परवान्याची मागणी केल्यास सदरचा परवाना तुम्हास त्या त्या वेळी हजर करावा लागेल.")
                                .FontSize(10)
                                .LineHeight(1.2f);

                            column.Item().Height(5);

                            // Payment information
                            column.Item().Text(text =>
                            {
                                text.Span("महाराष्ट्र शासनाने पुणे शहरासाठी मान्य केलेल्या विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीनुसार परवाना शुल्क म्हणून रु. ")
                                    .FontSize(10);
                                text.Span(challan.Amount.ToString()).Bold().FontSize(10);
                                text.Span(" चलन क्र. ").FontSize(10);
                                text.Span(challan.ChallanNumber).Bold().FontSize(10);
                                text.Span(" दि. ").FontSize(10);
                                text.Span(challan.ChallanDate.ToString("dd/MM/yyyy")).Bold().FontSize(10);
                                text.Span(" अन्वये भरले आहे.").FontSize(10);
                            });

                            column.Item().Height(30);

                            // Signatures
                            var engg = marathiPosition == "स्ट्रक्चरल इंजिनिअर" ? "कार्यकारी" : "उप";

                            column.Item().Row(row =>
                            {
                                row.RelativeItem(1).AlignCenter().Text($"{engg} अभियंता, (बांधकाम विकास विभाग) पुणे महानगरपालिका")
                                    .FontSize(10);

                                row.RelativeItem(1).AlignCenter().Text("शहर अभियंता पुणे महानगरपालिका")
                                    .FontSize(10);
                            });

                            column.Item().Height(10);

                            // Note
                            column.Item().Text($"टीप – प्रस्तुत परवान्याची मुदत ३१ डिसेंबर रोजी संपते जर पुढील वर्षासाठी त्याचे नूतनीकरण करणे असेल तर यासाठी कमीत कमी १५ दिवस परवाना मुदत संपण्या अगोदर परवाना शुल्कासहित अर्ज सादर केला पाहिजे. परवान्याचे नूतनीकरण करून घेण्याबद्दल तुम्हास वेगळी समज दली जाणार नाही जोपर्यंत परवान्याच्या नूतनीकरणासाठी परवाना शुल्कासहित अर्ज दिलेला नाही तोपर्यंत {marathiPosition} म्हणून काम करता येणार नाही. तसेच परवाना नाकारल्यासही तुम्हास {marathiPosition} म्हणून काम करता येणार नाही.")
                                .FontSize(10)
                                .LineHeight(1.2f);
                        });
                    });
                });

                var pdfBytes = document.GeneratePdf();
                _logger.LogInformation("✅ Successfully generated licence certificate PDF for application {ApplicationId}", applicationId);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating licence certificate PDF for application {ApplicationId}", applicationId);
                throw;
            }
        }

        private string GetCertificatePrefix(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "ARCH",
                PositionType.StructuralEngineer => "STR.ENGG",
                PositionType.LicenceEngineer => "LIC.ENGG",
                PositionType.Supervisor1 => "SUPER1",
                PositionType.Supervisor2 => "SUPER2",
                _ => "ENGG"
            };
        }

        private string GetMarathiPosition(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "आर्किटेक्ट",
                PositionType.StructuralEngineer => "स्ट्रक्चरल इंजिनिअर",
                PositionType.LicenceEngineer => "लायसन्स इंजिनिअर",
                PositionType.Supervisor1 => "सुपरवायझर१",
                PositionType.Supervisor2 => "सुपरवायझर२",
                _ => "इंजिनिअर"
            };
        }

        private string GetEnglishPosition(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "Architect",
                PositionType.StructuralEngineer => "Structural Engineer",
                PositionType.LicenceEngineer => "Licence Engineer",
                PositionType.Supervisor1 => "Supervisor1",
                PositionType.Supervisor2 => "Supervisor2",
                _ => "Engineer"
            };
        }
    }

    public class ApplicationPdfDocument : IDocument
    {
        private readonly ApplicationPdfModel _model;

        // Use FontService for consistent font naming across the application
        private static string MarathiFont => FontService.MarathiFontFamily;
        private static string EnglishFont => FontService.EnglishFontFamily;

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
                column.Spacing(5);

                // Top header
                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .AlignLeft()
                        .Text("मा. शहर अभियंता\nपुणे महानगरपालिका")
                        .Bold()
                        .FontFamily(MarathiFont)
                        .FontSize(11)
                        .LineHeight(1.4f);

                    row.RelativeItem();
                });

                column.Item().PaddingTop(15);

                column.Item()
                    .AlignCenter()
                    .Text("यांजकडे सादर....")
                    .FontSize(13)
                    .FontFamily(MarathiFont);

                DateTime date = _model.Date;
                int year = date.Year;
                int toYear = year + 2;

                column.Item().PaddingTop(8).AlignLeft().Text($"विषय:- जानेवारी {year} ते डिसेंबर {toYear} करीता {_model.Position} नवीन परवान्याबाबत.")
                    .FontSize(11)
                    .FontFamily(MarathiFont)
                    .LineHeight(1.4f);

                column.Item().PaddingTop(10).AlignLeft().Text($"विषयांकित प्रकरणी खाली निर्देशित व्यक्तीने जानेवारी {year} ते डिसेंबर {toYear} या कालावधीकरीता पुणे महानगरपालिकेच्या मा. शहर अभियंता कार्यालयाकडे {_model.Position} (नवीन) परवान्याकरिता अर्ज केला आहे.")
                    .FontSize(11)
                    .FontFamily(MarathiFont)
                    .LineHeight(1.5f);

                column.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("अर्जदाराचे नाव - ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    text.Span(_model.Name)
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();
                });

                column.Item().PaddingTop(6).Text(text =>
                {
                    text.Span("अर्जदाराचे शिक्षण - ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    if (_model.Qualification.Count > 0)
                    {
                        text.Span($"1) {_model.Qualification[0]}")
                            .FontSize(11)
                            .FontFamily(EnglishFont)
                            .Bold();
                    }

                    if (_model.Qualification.Count > 1 && !string.IsNullOrWhiteSpace(_model.Qualification[1]))
                    {
                        text.Span($"\n                                        2) {_model.Qualification[1]}")
                            .FontSize(11)
                            .FontFamily(EnglishFont)
                            .Bold();
                    }
                });

                column.Item().PaddingTop(6).Text(text =>
                {
                    text.Span("पत्ता : 1) ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    text.Span(_model.Address1)
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();

                    if (!_model.IsBothAddressSame && !string.IsNullOrWhiteSpace(_model.Address2))
                    {
                        text.Span($"\n             2) {_model.Address2}")
                            .FontSize(11)
                            .FontFamily(EnglishFont)
                            .Bold();
                    }
                });

                column.Item().PaddingTop(6).Text(text =>
                {
                    text.Span("मोबाईलनं.- ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    text.Span(_model.MobileNumber)
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();
                });

                column.Item().PaddingTop(6).Text("आवश्यक अनुभव - २ वर्षे (युडीसीपीआर २०२० मधील अपेंडिक्स 'सी', सी-४.१(ii) नुसार)")
                    .FontFamily(MarathiFont)
                    .FontSize(11)
                    .LineHeight(1.4f);

                column.Item().PaddingTop(6).Text(text =>
                {
                    text.Span("अनुभव- ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    text.Span(_model.YearDifference ?? "0")
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();

                    text.Span(" वर्षे ")
                        .FontSize(11)
                        .FontFamily(MarathiFont);

                    text.Span(_model.MonthDifference ?? "0")
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();

                    text.Span(" महिने")
                        .FontSize(11)
                        .FontFamily(MarathiFont);
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                var num = _model.Position switch
                {
                    "स्ट्रक्चरल इंजिनिअर" => "4",
                    "लायसन्स इंजिनिअर" => "3",
                    "सुपरवायझर1" => "5.1",
                    "सुपरवायझर2" => "5.1",
                    _ => "4"
                };

                column.Item().PaddingTop(12).Text($"उपरोक्त नमूद केलेल्या व्यक्तीचा मागणी अर्ज, शैक्षणिक पात्रता, अनुभव व पत्त्याचा पुरावा इ. कागदपत्राची तपासणी केली ती बरोबर व नियमानुसार आहेत. त्यानुसार वरील अर्जदाराची मान्य युडीसीपीआर २०२० मधील अपेंडिक्स सी, सी-{num} नुसार पुणे महानगरपालिकेच्या {_model.Position} (नवीन) परवाना धारण करण्यास आवश्यक शैक्षणिक पात्रता व अनुभव असल्याने त्यांचा अर्ज आपले मान्यतेकरिता सादर करीत आहोत.")
                    .FontFamily(MarathiFont)
                    .FontSize(11)
                    .LineHeight(1.5f);

                column.Item().PaddingTop(8).Text(text =>
                {
                    DateTime date = _model.Date;
                    int year = date.Year;
                    int toYear = year + 2;

                    text.Span("तरी सदर प्रकरणी ")
                        .FontFamily(MarathiFont)
                        .FontSize(11);

                    text.Span(_model.Name)
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();

                    text.Span($" यांचेकडून जानेवारी {year} ते डिसेंबर {toYear} या कालावधी करिता आवश्यक ती फी भरून घेवून {_model.Position} (नवीन) परवाना देणेबाबत मान्यता मिळणेस विनंती आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(11);
                });

                column.Item().PaddingTop(12).Text("मा.स.कळावे.")
                    .FontFamily(MarathiFont)
                    .FontSize(11);

                // Signature/footer section - positioned at bottom
                column.Item().Extend().AlignBottom().Column(bottomColumn =>
                {
                    // First row of signatures (JE and AE)
                    bottomColumn.Item().Row(row =>
                    {
                        // JE signature
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Height(50); // Space for signature
                            col.Item().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"({_model.JrEnggName ?? ""})").FontFamily(EnglishFont).FontSize(10);
                                c.Item().Text("शाखा अभियंता").FontFamily(MarathiFont).FontSize(10);
                                c.Item().Text("शहर-अभियंता कार्यालय").FontFamily(MarathiFont).FontSize(10);
                                c.Item().Text("पुणे महानगरपालिका").FontFamily(MarathiFont).FontSize(10);
                            });
                        });

                        // AE signature
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Height(50); // Space for signature
                            col.Item().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"({_model.AssEnggName ?? ""})").FontFamily(EnglishFont).FontSize(10);
                                c.Item().Text("उपअभियंता").FontFamily(MarathiFont).FontSize(10);
                                c.Item().Text("पुणे महानगरपालिका").FontFamily(MarathiFont).FontSize(10);
                            });
                        });
                    });

                    // Recommendation text
                    bottomColumn.Item().PaddingTop(15).AlignLeft().Text("प्रस्तुत प्रकरणी उपरोक्त प्रमाणे छाननी झाली असल्याने मान्यतेस शिफारस आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(11)
                        .LineHeight(1.4f);

                    // "क्ष मान्य" text aligned to right
                    bottomColumn.Item().PaddingTop(25).AlignRight().PaddingRight(100).Text("क्ष मान्य")
                        .FontFamily(MarathiFont)
                        .FontSize(11);

                    // Second row of signatures (EE and CE)
                    bottomColumn.Item().PaddingTop(5).Row(row =>
                    {
                        // EE signature
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Height(50); // Space for signature
                            col.Item().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"({_model.ExeEnggName ?? ""})").FontFamily(EnglishFont).FontSize(10);
                                c.Item().Text("कार्यकारी अभियंता").FontFamily(MarathiFont).FontSize(10);
                                c.Item().Text("पुणे महानगरपालिका").FontFamily(MarathiFont).FontSize(10);
                            });
                        });

                        // CE signature
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Height(50); // Space for signature
                            col.Item().AlignCenter().Column(c =>
                            {
                                c.Item().Text($"({_model.CityEnggName ?? ""})").FontFamily(EnglishFont).FontSize(10);
                                c.Item().Text("शहर अभियंता").FontFamily(MarathiFont).FontSize(10);
                                c.Item().Text("पुणे महानगरपालिका").FontFamily(MarathiFont).FontSize(10);
                            });
                        });
                    });
                });
            });
        }
    }
}
