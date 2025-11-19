using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for generating and managing Structural Engineer / Position Licence Certificates
    /// Certificates are generated after payment completion and stored in database only (SEDocuments table)
    /// </summary>
    public class SECertificateGenerationService : ISECertificateGenerationService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<SECertificateGenerationService> _logger;
        private readonly IChallanService _challanService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public SECertificateGenerationService(
            PMCRMSDbContext context,
            ILogger<SECertificateGenerationService> logger,
            IChallanService challanService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _challanService = challanService;
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// Generate and save licence certificate with retry logic
        /// Certificate is stored in SEDocuments table ONLY (no physical file storage)
        /// </summary>
        public async Task<bool> GenerateAndSaveLicenceCertificateAsync(int applicationId)
        {
            var maxRetryAttempts = int.Parse(_configuration["RetrySettings:MaxAttempts"] ?? "3");
            var retryDelayMs = int.Parse(_configuration["RetrySettings:DelayMilliseconds"] ?? "2000");

            var application = await _context.PositionApplications
                .Include(a => a.Addresses)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                _logger.LogError("❌ Application {ApplicationId} not found for certificate generation", applicationId);
                return false;
            }

            // Check if certificate already exists
            var existingCertificate = await _context.SEDocuments
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate);

            if (existingCertificate != null)
            {
                _logger.LogInformation("✅ Licence certificate already exists for application {ApplicationId}", applicationId);
                return true;
            }

            Exception? lastException = null;
            bool success = false;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("🔄 Attempting to generate licence certificate for application {ApplicationId} (Attempt {Attempt}/{MaxAttempts})",
                        applicationId, attempt, maxRetryAttempts);

                    // Generate certificate PDF inline (simplified version)
                    var pdfBytes = await GenerateCertificatePdfInlineAsync(applicationId);

                    if (pdfBytes == null || pdfBytes.Length == 0)
                    {
                        throw new Exception("Failed to generate certificate PDF - result is empty");
                    }

                    // Create document record - Store PDF content in database ONLY (no physical file storage)
                    var certificate = new SEDocument
                    {
                        ApplicationId = applicationId,
                        DocumentType = SEDocumentType.LicenceCertificate,
                        FileName = $"LicenceCertificate_{application.ApplicationNumber ?? applicationId.ToString()}.pdf",
                        FilePath = string.Empty, // No physical file - database storage only
                        FileId = Guid.NewGuid().ToString(),
                        FileSize = (decimal)(pdfBytes.Length / 1024.0), // Size in KB
                        ContentType = "application/pdf",
                        FileContent = pdfBytes, // Store PDF binary data in database
                        IsVerified = false,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.SEDocuments.Add(certificate);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Licence certificate generated and saved to database for application {ApplicationId} on attempt {Attempt}",
                        applicationId, attempt);

                    success = true;
                    break; // Success - exit retry loop
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "❌ Failed to generate licence certificate for application {ApplicationId} on attempt {Attempt}/{MaxAttempts}",
                        applicationId, attempt, maxRetryAttempts);

                    if (attempt < maxRetryAttempts)
                    {
                        _logger.LogWarning("⏳ Retrying certificate generation after {Delay}ms delay...", retryDelayMs);
                        await Task.Delay(retryDelayMs);
                    }
                }
            }

            if (!success && lastException != null)
            {
                _logger.LogError("❌ Failed to generate licence certificate for application {ApplicationId} after {MaxAttempts} attempts. Last error: {Error}",
                    applicationId, maxRetryAttempts, lastException.Message);
            }

            return success;
        }

        /// <summary>
        /// Retrieve generated certificate PDF from database
        /// </summary>
        public async Task<byte[]?> GetCertificatePdfAsync(int applicationId)
        {
            try
            {
                var certificate = await _context.SEDocuments
                    .Where(d => d.ApplicationId == applicationId && d.DocumentType == SEDocumentType.LicenceCertificate)
                    .OrderByDescending(d => d.CreatedDate)
                    .FirstOrDefaultAsync();

                if (certificate?.FileContent != null)
                {
                    _logger.LogInformation("✅ Retrieved licence certificate from database for application {ApplicationId}", applicationId);
                    return certificate.FileContent;
                }

                _logger.LogWarning("⚠️ Licence certificate not found for application {ApplicationId}", applicationId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving certificate for application {ApplicationId}", applicationId);
                return null;
            }
        }

        /// <summary>
        /// Generate certificate PDF inline using QuestPDF (simplified version without complex templates)
        /// </summary>
        private async Task<byte[]> GenerateCertificatePdfInlineAsync(int applicationId)
        {
            var application = await _context.PositionApplications
                .Include(a => a.Addresses)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                throw new Exception($"Application {applicationId} not found");
            }

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
                            a.CreatedDate.Year == DateTime.Now.Year) + 1;

            string certificateNumber = $"PMC/{certificatePrefix}/{count}/{DateTime.Now.Year}-{DateTime.Now.Year + 3}";
            string marathiPosition = GetMarathiPosition(application.PositionType);
            string englishPosition = GetEnglishPosition(application.PositionType);
            string applicantName = $"{application.FirstName} {(string.IsNullOrEmpty(application.MiddleName) ? "" : application.MiddleName + " ")}{application.LastName}".Trim();
            
            var currentAddress = application.Addresses.FirstOrDefault(a => a.AddressType == "Current");
            string fullAddress = "";
            
            if (currentAddress != null)
            {
                fullAddress = $"{currentAddress.AddressLine1}, {currentAddress.City}, {currentAddress.State} - {currentAddress.PinCode}";
                _logger.LogInformation("✅ Address loaded for application {ApplicationId}: {Address}", applicationId, fullAddress);
            }
            else
            {
                _logger.LogWarning("⚠️ No current address found for application {ApplicationId}. Total addresses: {Count}", 
                    applicationId, application.Addresses?.Count ?? 0);
                    
                // Try any address if current is not available
                var anyAddress = application.Addresses?.FirstOrDefault();
                if (anyAddress != null)
                {
                    fullAddress = $"{anyAddress.AddressLine1}, {anyAddress.City}, {anyAddress.State} - {anyAddress.PinCode}";
                    _logger.LogInformation("✅ Using alternate address for application {ApplicationId}: {Address}", applicationId, fullAddress);
                }
            }

            DateTime fromDate = DateTime.Now;
            int toYear = DateTime.Now.Year + 3;

            // Load PMC logo - First try from database, then fallback to file system
            byte[]? logoBytes = null;
            
            // Try to fetch logo from SystemSettings table
            var logoSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "PMC_LOGO" && s.IsActive);
            
            if (logoSetting?.BinaryData != null && logoSetting.BinaryData.Length > 0)
            {
                logoBytes = logoSetting.BinaryData;
                _logger.LogInformation("✅ PMC Logo loaded from database (Size: {Size} bytes)", logoBytes.Length);
            }
            else
            {
                // Fallback to file system
                var logoPath = Path.Combine(_environment.WebRootPath, "Images", "Certificate", "logo.png");
                _logger.LogWarning("⚠️ PMC Logo not found in database. Trying file system at: {LogoPath}", logoPath);
                
                if (File.Exists(logoPath))
                {
                    logoBytes = await File.ReadAllBytesAsync(logoPath);
                    _logger.LogInformation("✅ PMC Logo loaded from file system (Size: {Size} bytes)", logoBytes.Length);
                }
                else
                {
                    _logger.LogError("❌ PMC Logo not found! Checked database (SettingKey: PMC_LOGO) and file system ({LogoPath})", logoPath);
                    _logger.LogError("❌ Please upload logo to database using SystemSettings API or place logo.png file at: {LogoPath}", logoPath);
                }
            }

            // Get profile photo from SEDocument table (DocumentType = 8 = ProfilePicture)
            var profilePhotoDocument = await _context.SEDocuments
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                        d.DocumentType == SEDocumentType.ProfilePicture);

            _logger.LogInformation("Profile photo document query for ApplicationId={ApplicationId}, DocumentType=ProfilePicture(8): {Found}", 
                applicationId, profilePhotoDocument != null ? "FOUND" : "NOT FOUND");

            byte[]? profilePhotoBytes = null;
            if (profilePhotoDocument != null)
            {
                _logger.LogInformation("Profile photo document details - FileName: {FileName}, FileContent Length: {Length}, FilePath: {FilePath}", 
                    profilePhotoDocument.FileName, 
                    profilePhotoDocument.FileContent?.Length ?? 0, 
                    profilePhotoDocument.FilePath);

                // If FileContent exists, use it; otherwise read from FilePath
                if (profilePhotoDocument.FileContent != null && profilePhotoDocument.FileContent.Length > 0)
                {
                    profilePhotoBytes = profilePhotoDocument.FileContent;
                    _logger.LogInformation("✅ Using profile photo from database FileContent for application {ApplicationId} (Size: {Size} bytes)", applicationId, profilePhotoBytes.Length);
                    
                    // Log image format info for debugging
                    var formatInfo = GetImageFormatInfo(profilePhotoBytes);
                    _logger.LogInformation("Profile photo format info: {Format}", formatInfo);
                    
                    // Check if it's AVIF format (not supported by QuestPDF/SkiaSharp)
                    if (formatInfo.StartsWith("AVIF"))
                    {
                        _logger.LogError("❌ AVIF format is not supported by QuestPDF. Please re-upload profile photo as JPEG or PNG for application {ApplicationId}", applicationId);
                        profilePhotoBytes = null; // Will use placeholder
                    }
                    // Don't validate other formats - let QuestPDF try to decode them
                }
                else if (!string.IsNullOrEmpty(profilePhotoDocument.FilePath))
                {
                    var photoPath = Path.Combine(_environment.WebRootPath, profilePhotoDocument.FilePath.TrimStart('/'));
                    _logger.LogInformation("Attempting to read profile photo from file path: {PhotoPath}", photoPath);
                    if (File.Exists(photoPath))
                    {
                        profilePhotoBytes = File.ReadAllBytes(photoPath);
                        _logger.LogInformation("✅ Using profile photo from file path {PhotoPath} for application {ApplicationId} (Size: {Size} bytes)", 
                            photoPath, applicationId, profilePhotoBytes.Length);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Profile photo file not found at path: {PhotoPath}", photoPath);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Profile photo document found but has no FileContent or FilePath for application {ApplicationId}", applicationId);
                }
            }

            // Use a placeholder if no profile photo found (1x1 white pixel PNG)
            if (profilePhotoBytes == null)
            {
                _logger.LogWarning("Profile photo not found for application {ApplicationId}, using placeholder", applicationId);
                profilePhotoBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
            }

            // Generate QR Code
            string qrData = $"{certificateNumber}|{applicantName}|{fromDate:dd/MM/yyyy}";
            byte[] qrCodeBytes = QrCodeGenerator.GenerateQrCode(qrData, 10);

            // Generate simple certificate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 950); // Custom size: A4 width (595) x increased height (950) instead of 842
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    page.Content().Column(column =>
                    {
                        // Top right corner - Department info
                        column.Item().Row(row =>
                        {
                            row.RelativeItem(); // Empty left space
                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text("बांधकाम विकास विभाग")
                                    .FontFamily(FontService.MarathiFontFamily)
                                    .FontSize(9)
                                    .Bold();

                                col.Item().Text("पुणे महानगरपालिका")
                                    .FontFamily(FontService.MarathiFontFamily)
                                    .FontSize(9)
                                    .Bold();
                            });
                        });

                        column.Item().Height(10);

                        // Header section: QR Code (Left) | PMC Logo (Center) | Profile Photo (Right)
                        column.Item().Row(row =>
                        {
                            // QR Code (Left) - 90x90
                            row.RelativeItem(1).AlignLeft().Column(qrCol =>
                            {
                                if (qrCodeBytes != null && qrCodeBytes.Length > 0)
                                {
                                    qrCol.Item().Height(90).Width(90).Image(qrCodeBytes);
                                }
                            });

                            // PMC Logo (Center) - 80x80
                            row.RelativeItem(1).AlignCenter().Column(logoCol =>
                            {
                                if (logoBytes != null)
                                {
                                    logoCol.Item().Height(80).Width(80).AlignCenter().Image(logoBytes);
                                }
                            });

                            // Profile Photo (Right) - 75x95 with border
                            row.RelativeItem(1).AlignRight().Column(photoCol =>
                            {
                                if (profilePhotoBytes != null && profilePhotoBytes.Length > 0)
                                {
                                    try
                                    {
                                        photoCol.Item()
                                            .Height(95)
                                            .Width(75)
                                            .Border(2)
                                            .BorderColor(Colors.Black)
                                            .Image(profilePhotoBytes);
                                        _logger.LogInformation("✅ Profile photo rendered successfully for application {ApplicationId}", applicationId);
                                    }
                                    catch (Exception imgEx)
                                    {
                                        _logger.LogError(imgEx, "❌ Failed to render profile photo image for application {ApplicationId}. Using placeholder.", applicationId);
                                        // Use placeholder - 75x95 yellow rectangle with border and text
                                        photoCol.Item()
                                            .Height(95)
                                            .Width(75)
                                            .Border(2)
                                            .BorderColor(Colors.Black)
                                            .Background(Colors.Grey.Lighten3)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text("No Photo")
                                            .FontSize(8)
                                            .FontColor(Colors.Grey.Darken2);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ No profile photo available for application {ApplicationId}. Using placeholder.", applicationId);
                                    // Use placeholder - 75x95 light grey rectangle with border and text
                                    photoCol.Item()
                                        .Height(95)
                                        .Width(75)
                                        .Border(2)
                                        .BorderColor(Colors.Black)
                                        .Background(Colors.Grey.Lighten3)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text("No Photo")
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken2);
                                }
                            });
                        });

                        column.Item().Height(12);

                        // Main title - पुणे महानगरपालिका (Centered, large and bold)
                        column.Item().AlignCenter().Text("पुणे महानगरपालिका")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(18)
                            .Bold();

                        column.Item().Height(6);

                        // Subtitle - स्ट्रक्चरल इंजिनिअर च्या कामासाठी परवाना (Centered with underline)
                        column.Item().AlignCenter().Text($"{marathiPosition} च्या कामासाठी परवाना")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(12)
                            .Underline();

                        column.Item().Height(12);

                        // Legal reference paragraph
                        column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२०२० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२०२०) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम १९४९ चे कलम ३७२ अन्वये {marathiPosition} काम करण्यास परवाना देण्यात येत आहे.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);

                        column.Item().Height(10);

                        // Certificate number and validity
                        column.Item().Text(text =>
                        {
                            text.Span($"परवाना क्र. :- ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span($"{certificateNumber}                     From {fromDate:dd/MM/yyyy} to 31/12/{toYear}                 ({englishPosition})")
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);
                        });

                        column.Item().Height(8);

                        // Applicant Name
                        column.Item().Text(text =>
                        {
                            text.Span($"नाव :- ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span(applicantName)
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);
                        });

                        column.Item().Height(4);

                        // Address
                        column.Item().Text(text =>
                        {
                            text.Span("पत्ता :- ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span(fullAddress)
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);
                        });

                        column.Item().Height(4);

                        // Date (Right aligned on separate row)
                        column.Item().AlignRight().Text(text =>
                        {
                            text.Span("दिनांक :- ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span($"{fromDate:dd/MM/yyyy}")
                                .FontFamily("Times New Roman")
                                .FontSize(10);
                        });

                        column.Item().Height(10);

                        // Terms and conditions - Paragraph 1
                        column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२०२० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२०२०) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम, १९४९ चे कलम ३७२ अन्वये मी तुम्हांस वर निर्देश केलेल्या कायदा व नियमानुसार ३ वर्षांकरीता दि. {fromDate:dd/MM/yyyy} ते 31/12/{toYear} अखेर {marathiPosition} म्हणून 'खालील मर्यादा व अटी यांचे पालन करणार' या अटीवर परवाना देत आहे.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);

                        column.Item().Height(6);

                        // Terms and conditions - Paragraph 2
                        column.Item().Text("'मा. महापालिका आयुक्त, यांनी वेळोवेळी स्थायी समितीच्या संमतीने वरील कायद्याचे कलम ३७३ परवानाधारण करणार यांच्या माहितीसाठी काढण्यात आलेल्या आज्ञेचे आणि विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीतील अपेंडिक्स 'सी' मधील कर्तव्ये व जबाबदारी यांचे पालन करणार' ही परवानगीची अट राहील आणि धंद्याच्या प्रत्येक बाबतीत परवान्याच्या मुदतीत ज्यावेळी तुमचा सल्ला घेण्यात येईल त्यावेळी तुम्ही आतापावेतो निघालेल्या आज्ञांचे पालन करून त्याप्रमाणे काम करावयाचे आहे.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);

                        column.Item().Height(6);

                        // Terms and conditions - Paragraph 3
                        column.Item().Text("जी आज्ञापत्रक वेळोवेळी काढण्यात आलेली आहेत, ती मुख्य कार्यालयाकडे माहितीसाठी ठेवण्यात आलेली असून, जरूर त्यावेळी कार्यालयाच्या वेळेमध्ये त्यांची पाहणी करता येईल.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);

                        column.Item().Height(6);

                        // Terms and conditions - Paragraph 4
                        column.Item().Text("मात्र हे लक्षात घेणे जरूर आहे की, मा. महापालिका आयुक्त सदरचा परवाना महाराष्ट्र महानगरपालिका अधिनियम, कलम ३८६ अनुसार जरूर तेव्हा तात्पुरता बंद अगर रद्द करू शकतात जर वर निर्दिष्ट केलेली बंधने अगर शर्थी यांचा भंग झाला अगर टाळल्या गेल्या अथवा तुम्ही सदर कायद्याच्या नियमांचे अगर वेळोवेळी काढण्यात आलेल्या आज्ञापत्रकाचे उल्लंघन केल्याचे दृष्टोपतीस आल्यास आणि जर सदरचा परवाना तात्पुरता तहकूब अगर रद्द झाल्यास अथवा सदरच्या परवान्याची मुदत संपल्यावर तुम्हास परवाना नसल्याचे समजले जाईल आणि महानगरपालिका अधिनियमाचे कलम ६९ अन्वये मा.महापालिका आयुक्त, अगर त्यांनी अधिकार दिलेल्या अधिका-यांनी सदर परवान्याची मागणी केल्यास सदरचा परवाना तुम्हास त्या त्या वेळी हजर करावा लागेल.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);

                        column.Item().Height(6);

                        // Payment information
                        column.Item().Text(text =>
                        {
                            text.Span("महाराष्ट्र शासनाने पुणे शहरासाठी मान्य केलेल्या विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीनुसार परवाना शुल्क म्हणून रु. ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span(challan.Amount.ToString())
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);

                            text.Span(" चलन क्र. ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span(challan.ChallanNumber)
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);

                            text.Span(" दि. ")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);

                            text.Span($"{challan.ChallanDate:dd/MM/yyyy}")
                                .FontFamily("Times New Roman")
                                .Bold()
                                .FontSize(10);

                            text.Span(" अन्वये भरले आहे.")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10);
                        });

                        column.Item().Height(100);

                        // Signatures
                        var engg = marathiPosition == "स्ट्रक्चरल इंजिनिअर" ? "कार्यकारी" : "उप";

                        column.Item().Row(sigRow =>
                        {
                            sigRow.RelativeItem(1).AlignCenter().Text($"{engg} अभियंता\n(बांधकाम विकास विभाग)\nपुणे महानगरपालिका")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10)
                                .LineHeight(1.4f);

                            sigRow.RelativeItem(1).AlignCenter().Text("शहर अभियंता\nपुणे महानगरपालिका")
                                .FontFamily(FontService.MarathiFontFamily)
                                .FontSize(10)
                                .LineHeight(1.4f);
                        });

                        column.Item().Height(15);

                        // Note (टीप)
                        column.Item().Text($"टीप – प्रस्तुत परवान्याची मुदत ३१ डिसेंबर रोजी संपते जर पुढील वर्षासाठी त्याचे नूतनीकरण करणे असेल तर यासाठी कमीत कमी १५ दिवस परवाना मुदत संपण्या अगोदर परवाना शुल्कासहित अर्ज सादर केला पाहिजे. परवान्याचे नूतनीकरण करून घेण्याबद्दल तुम्हास वेगळी समज दली जाणार नाही जोपर्यंत परवान्याच्या नूतनीकरणासाठी परवाना शुल्कासहित अर्ज दिलेला नाही तोपर्यंत {marathiPosition} म्हणून काम करता येणार नाही. तसेच परवाना नाकारल्यासही तुम्हास {marathiPosition} म्हणून काम करता येणार नाही.")
                            .FontFamily(FontService.MarathiFontFamily)
                            .FontSize(10)
                            .LineHeight(1.5f);
                    });
                });
            });

            return document.GeneratePdf();
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

        /// <summary>
        /// Validate if byte array is a valid image format (JPEG, PNG, BMP)
        /// Checks magic bytes (file signature) at the beginning of the file
        /// </summary>
        private bool IsValidImageFormat(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4)
                return false;

            // Check for common image format magic bytes
            // PNG: 89 50 4E 47
            if (imageBytes.Length >= 4 && imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && 
                imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
            {
                _logger.LogInformation("✅ Detected PNG image format");
                return true;
            }

            // JPEG: FF D8 FF
            if (imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
            {
                _logger.LogInformation("✅ Detected JPEG image format");
                return true;
            }

            // BMP: 42 4D
            if (imageBytes.Length >= 2 && imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
            {
                _logger.LogInformation("✅ Detected BMP image format");
                return true;
            }

            // Log first few bytes for debugging
            string hexDump = BitConverter.ToString(imageBytes.Take(Math.Min(16, imageBytes.Length)).ToArray());
            _logger.LogError("❌ Unknown image format. First bytes: {HexDump}", hexDump);
            return false;
        }

        /// <summary>
        /// Get image format information from byte array
        /// </summary>
        private string GetImageFormatInfo(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4)
                return "Invalid or too small";

            string hexDump = BitConverter.ToString(imageBytes.Take(Math.Min(16, imageBytes.Length)).ToArray());
            
            // PNG: 89 50 4E 47
            if (imageBytes.Length >= 4 && imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && 
                imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return $"PNG (Header: {hexDump})";

            // JPEG: FF D8 FF
            if (imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                return $"JPEG (Header: {hexDump})";

            // BMP: 42 4D
            if (imageBytes.Length >= 2 && imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                return $"BMP (Header: {hexDump})";

            // AVIF: ftyp avif (at offset 4)
            if (imageBytes.Length >= 12 && imageBytes[4] == 0x66 && imageBytes[5] == 0x74 && 
                imageBytes[6] == 0x79 && imageBytes[7] == 0x70)
                return $"AVIF/HEIF (Header: {hexDump})";

            // WebP: RIFF ... WEBP
            if (imageBytes.Length >= 12 && imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && 
                imageBytes[2] == 0x46 && imageBytes[3] == 0x46)
                return $"WebP (Header: {hexDump})";

            return $"Unknown format (Header: {hexDump})";
        }
    }
}
