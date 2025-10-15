using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
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

        public SECertificateGenerationService(
            PMCRMSDbContext context,
            ILogger<SECertificateGenerationService> logger,
            IChallanService challanService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _challanService = challanService;
            _environment = environment;
        }

        /// <summary>
        /// Generate and save licence certificate with retry logic
        /// Certificate is stored in SEDocuments table ONLY (no physical file storage)
        /// </summary>
        public async Task<bool> GenerateAndSaveLicenceCertificateAsync(int applicationId)
        {
            const int MAX_RETRY_ATTEMPTS = 3;
            const int RETRY_DELAY_MS = 2000; // 2 seconds between retries

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

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    _logger.LogInformation("🔄 Attempting to generate licence certificate for application {ApplicationId} (Attempt {Attempt}/{MaxAttempts})",
                        applicationId, attempt, MAX_RETRY_ATTEMPTS);

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
                        applicationId, attempt, MAX_RETRY_ATTEMPTS);

                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        _logger.LogWarning("⏳ Retrying certificate generation after {Delay}ms delay...", RETRY_DELAY_MS);
                        await Task.Delay(RETRY_DELAY_MS);
                    }
                }
            }

            if (!success && lastException != null)
            {
                _logger.LogError("❌ Failed to generate licence certificate for application {ApplicationId} after {MaxAttempts} attempts. Last error: {Error}",
                    applicationId, MAX_RETRY_ATTEMPTS, lastException.Message);
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
            string fullAddress = currentAddress != null
                ? $"{currentAddress.AddressLine1}, {currentAddress.City} {currentAddress.State} {currentAddress.PinCode}"
                : "";

            DateTime fromDate = DateTime.Now;
            int toYear = DateTime.Now.Year + 3;

            // Load PMC logo from wwwroot/Images/Certificate
            var logoPath = Path.Combine(_environment.WebRootPath, "Images", "Certificate", "logo.png");
            byte[]? logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

            if (logoBytes == null)
            {
                _logger.LogWarning("Logo not found at {LogoPath}", logoPath);
            }

            // Get profile photo from SEDocument table
            var profilePhotoDocument = await _context.SEDocuments
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                        d.DocumentType == SEDocumentType.ProfilePicture);

            byte[]? profilePhotoBytes = null;
            if (profilePhotoDocument != null)
            {
                // If FileContent exists, use it; otherwise read from FilePath
                if (profilePhotoDocument.FileContent != null && profilePhotoDocument.FileContent.Length > 0)
                {
                    profilePhotoBytes = profilePhotoDocument.FileContent;
                    _logger.LogInformation("Using profile photo from database FileContent for application {ApplicationId}", applicationId);
                }
                else if (!string.IsNullOrEmpty(profilePhotoDocument.FilePath))
                {
                    var photoPath = Path.Combine(_environment.WebRootPath, profilePhotoDocument.FilePath.TrimStart('/'));
                    if (File.Exists(photoPath))
                    {
                        profilePhotoBytes = File.ReadAllBytes(photoPath);
                        _logger.LogInformation("Using profile photo from file path {PhotoPath} for application {ApplicationId}", photoPath, applicationId);
                    }
                }
            }

            // Use a placeholder if no profile photo found (1x1 white pixel PNG)
            if (profilePhotoBytes == null)
            {
                _logger.LogWarning("Profile photo not found for application {ApplicationId}, using placeholder", applicationId);
                profilePhotoBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
            }

            // Generate simple certificate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);

                    page.Content().Column(column =>
                    {
                        // Header section with logo, title, and profile photo
                        column.Item().Row(row =>
                        {
                            // Logo (Left)
                            if (logoBytes != null)
                            {
                                row.ConstantItem(100).AlignCenter().PaddingTop(5).Height(80).Width(80)
                                    .Image(logoBytes);
                            }
                            else
                            {
                                row.ConstantItem(100); // Empty space if no logo
                            }

                            // Title (Center)
                            row.RelativeItem().AlignCenter().Column(col =>
                            {
                                col.Item().Text("पुणे महानगरपालिका")
                                    .FontSize(18).Bold();

                                col.Item().Text($"{marathiPosition} च्या कामासाठी परवाना")
                                    .FontSize(14);
                            });

                            // Profile Photo (Right)
                            if (profilePhotoBytes != null)
                            {
                                row.ConstantItem(100).AlignCenter().PaddingTop(5).Height(100).Width(80)
                                    .Border(1)
                                    .Image(profilePhotoBytes);
                            }
                            else
                            {
                                row.ConstantItem(100); // Empty space if no photo
                            }
                        });

                        column.Item().Height(15);

                        // Certificate number and validity
                        column.Item().Text(text =>
                        {
                            text.Span($"परवाना क्र. :- ").FontSize(11);
                            text.Span($"{certificateNumber}    From {fromDate:dd/MM/yyyy} to 31/12/{toYear}    ({englishPosition})")
                                .Bold().FontSize(11);
                        });

                        column.Item().Height(10);

                        // Applicant details
                        column.Item().Text(text =>
                        {
                            text.Span($"नाव :- ").FontSize(11);
                            text.Span(applicantName).Bold().FontSize(11);
                        });

                        column.Item().Text(text =>
                        {
                            text.Span("पत्ता :- ").FontSize(11);
                            text.Span(fullAddress).Bold().FontSize(11);
                        });

                        column.Item().Height(10);

                        // Payment information
                        column.Item().Text(text =>
                        {
                            text.Span("परवाना शुल्क: रु. ").FontSize(11);
                            text.Span(challan.Amount.ToString()).Bold().FontSize(11);
                            text.Span(" | चलन क्र.: ").FontSize(11);
                            text.Span(challan.ChallanNumber).Bold().FontSize(11);
                            text.Span($" | दिनांक: {challan.ChallanDate:dd/MM/yyyy}").FontSize(11);
                        });

                        column.Item().Height(50);

                        // Signatures
                        var engg = marathiPosition == "स्ट्रक्चरल इंजिनिअर" ? "कार्यकारी" : "उप";

                        column.Item().Row(row =>
                        {
                            row.RelativeItem(1).AlignCenter().Text($"{engg} अभियंता\nपुणे महानगरपालिका")
                                .FontSize(11);

                            row.RelativeItem(1).AlignCenter().Text("शहर अभियंता\nपुणे महानगरपालिका")
                                .FontSize(11);
                        });
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
    }
}
