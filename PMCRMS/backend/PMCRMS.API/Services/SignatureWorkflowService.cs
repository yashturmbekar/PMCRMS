using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Text;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for managing sequential digital signature workflow
    /// Handles JE → AE → EE → CE signature progression on recommendation forms
    /// </summary>
    public interface ISignatureWorkflowService
    {
        /// <summary>
        /// Get signature coordinates based on officer role and current signature count
        /// </summary>
        string GetSignatureCoordinates(OfficerRole role, int signatureOrder);

        /// <summary>
        /// Sign recommendation form as Junior Engineer (First signature)
        /// </summary>
        Task<SignatureWorkflowResult> SignAsJuniorEngineerAsync(int applicationId, int officerId, string otp);

        /// <summary>
        /// Sign recommendation form as Assistant Engineer (Second signature)
        /// </summary>
        Task<SignatureWorkflowResult> SignAsAssistantEngineerAsync(int applicationId, int officerId, string otp);

        /// <summary>
        /// Sign recommendation form as Executive Engineer (Third signature)
        /// </summary>
        Task<SignatureWorkflowResult> SignAsExecutiveEngineerAsync(int applicationId, int officerId, string otp);

        /// <summary>
        /// Sign recommendation form as City Engineer (Fourth and final signature)
        /// </summary>
        Task<SignatureWorkflowResult> SignAsCityEngineerAsync(int applicationId, int officerId, string otp);

        /// <summary>
        /// Get current signature status for an application
        /// </summary>
        Task<SignatureStatusResult> GetSignatureStatusAsync(int applicationId);
    }

    public class SignatureWorkflowService : ISignatureWorkflowService
    {
        private readonly PMCRMSDbContext _context;
        private readonly IHsmService _hsmService;
        private readonly ILogger<SignatureWorkflowService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public SignatureWorkflowService(
            PMCRMSDbContext context,
            IHsmService hsmService,
            ILogger<SignatureWorkflowService> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _hsmService = hsmService;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// Get signature coordinates based on officer role from appsettings.json
        /// </summary>
        public string GetSignatureCoordinates(OfficerRole role, int signatureOrder)
        {
            // Junior roles get first signature position
            if (role == OfficerRole.JuniorArchitect || role == OfficerRole.JuniorLicenceEngineer ||
                role == OfficerRole.JuniorStructuralEngineer || role == OfficerRole.JuniorSupervisor1 ||
                role == OfficerRole.JuniorSupervisor2)
            {
                return _configuration["HSM:SignatureCoordinates:JuniorEngineer"] ?? "50,650,200,720";
            }

            // Assistant roles get second signature position
            if (role == OfficerRole.AssistantArchitect || role == OfficerRole.AssistantLicenceEngineer ||
                role == OfficerRole.AssistantStructuralEngineer || role == OfficerRole.AssistantSupervisor1 ||
                role == OfficerRole.AssistantSupervisor2)
            {
                return _configuration["HSM:SignatureCoordinates:AssistantEngineer"] ?? "350,650,500,720";
            }

            // Executive Engineer gets third position
            if (role == OfficerRole.ExecutiveEngineer)
            {
                return _configuration["HSM:SignatureCoordinates:ExecutiveEngineer"] ?? "50,200,200,270";
            }

            // City Engineer gets fourth position
            if (role == OfficerRole.CityEngineer)
            {
                return _configuration["HSM:SignatureCoordinates:CityEngineer"] ?? "350,200,500,270";
            }

            return _configuration["HSM:SignatureCoordinates:JuniorEngineer"] ?? "50,650,200,720"; // Default
        }

        /// <summary>
        /// Sign recommendation form as Junior Engineer
        /// </summary>
        public async Task<SignatureWorkflowResult> SignAsJuniorEngineerAsync(int applicationId, int officerId, string otp)
        {
            return await SignRecommendationFormAsync(
                applicationId,
                officerId,
                otp,
                new[] { OfficerRole.JuniorArchitect, OfficerRole.JuniorLicenceEngineer, 
                        OfficerRole.JuniorStructuralEngineer, OfficerRole.JuniorSupervisor1, 
                        OfficerRole.JuniorSupervisor2 },
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING,
                "Junior Engineer signature completed. Forwarded to Assistant Engineer."
            );
        }

        /// <summary>
        /// Sign recommendation form as Assistant Engineer
        /// </summary>
        public async Task<SignatureWorkflowResult> SignAsAssistantEngineerAsync(int applicationId, int officerId, string otp)
        {
            return await SignRecommendationFormAsync(
                applicationId,
                officerId,
                otp,
                new[] { OfficerRole.AssistantArchitect, OfficerRole.AssistantLicenceEngineer, 
                        OfficerRole.AssistantStructuralEngineer, OfficerRole.AssistantSupervisor1, 
                        OfficerRole.AssistantSupervisor2 },
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING,
                "Assistant Engineer signature completed. Forwarded to Executive Engineer."
            );
        }

        /// <summary>
        /// Sign recommendation form as Executive Engineer
        /// </summary>
        public async Task<SignatureWorkflowResult> SignAsExecutiveEngineerAsync(int applicationId, int officerId, string otp)
        {
            return await SignRecommendationFormAsync(
                applicationId,
                officerId,
                otp,
                new[] { OfficerRole.ExecutiveEngineer },
                ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING,
                "Executive Engineer signature completed. Forwarded to City Engineer."
            );
        }

        /// <summary>
        /// Sign recommendation form as City Engineer (Final signature)
        /// </summary>
        public async Task<SignatureWorkflowResult> SignAsCityEngineerAsync(int applicationId, int officerId, string otp)
        {
            return await SignRecommendationFormAsync(
                applicationId,
                officerId,
                otp,
                new[] { OfficerRole.CityEngineer },
                ApplicationCurrentStatus.CLERK_PENDING,
                "All signatures completed. Forwarded to Clerk for certificate generation."
            );
        }

        /// <summary>
        /// Core signing logic for recommendation form
        /// </summary>
        private async Task<SignatureWorkflowResult> SignRecommendationFormAsync(
            int applicationId,
            int officerId,
            string otp,
            OfficerRole[] expectedRoles,
            ApplicationCurrentStatus nextStatus,
            string successMessage)
        {
            // Use execution strategy for PostgreSQL retry support (no manual transactions)
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                try
                {
                    // 1. Get application with documents
                    var application = await _context.PositionApplications
                        .FirstOrDefaultAsync(a => a.Id == applicationId);

                    if (application == null)
                    {
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = "Application not found"
                        };
                    }

                    // 2. Validate officer exists and has correct role
                    var officer = await _context.Officers.FindAsync(officerId);
                    if (officer == null)
                    {
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = "Officer not found"
                        };
                    }

                    if (!expectedRoles.Contains(officer.Role))
                    {
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = $"Invalid officer role. Expected one of: {string.Join(", ", expectedRoles)}, got {officer.Role}"
                        };
                    }

                    // 3. Get recommendation form document from SEDocuments table
                    var recommendationDoc = await _context.SEDocuments
                        .FirstOrDefaultAsync(d => d.ApplicationId == applicationId && 
                                                d.DocumentType == SEDocumentType.RecommendedForm);

                    if (recommendationDoc == null)
                    {
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = "Recommendation form not found in database"
                        };
                    }

                    // 4. Read PDF content from database
                    byte[] pdfContent;
                    if (recommendationDoc.FileContent != null && recommendationDoc.FileContent.Length > 0)
                    {
                        // PDF is stored in database
                        pdfContent = recommendationDoc.FileContent;
                        _logger.LogInformation("Reading PDF from database (FileContent), size: {Size} bytes", pdfContent.Length);
                    }
                    else if (!string.IsNullOrEmpty(recommendationDoc.FilePath) && File.Exists(recommendationDoc.FilePath))
                    {
                        // Fallback: PDF is stored as file
                        try
                        {
                            pdfContent = await File.ReadAllBytesAsync(recommendationDoc.FilePath);
                            _logger.LogInformation("Reading PDF from file: {FilePath}", recommendationDoc.FilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to read PDF from file path");
                            return new SignatureWorkflowResult
                            {
                                Success = false,
                                ErrorMessage = "Failed to read recommendation form from file"
                            };
                        }
                    }
                    else
                    {
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = "Recommendation form has no content (neither FileContent nor FilePath)"
                        };
                    }

                    // 5. Sign PDF using HSM
                    var signResult = await SignPdfWithOfficerKeyLabelAsync(
                        applicationId,
                        officerId,
                        pdfContent,
                        otp,
                        officer.Role,
                        officer.KeyLabel
                    );

                    if (!signResult.Success)
                    {
                        _logger.LogError("HSM signature failed for {Role}: {Error}", 
                            officer.Role, signResult.ErrorMessage);
                        
                        return new SignatureWorkflowResult
                        {
                            Success = false,
                            ErrorMessage = signResult.ErrorMessage,
                            RawHsmResponse = signResult.RawResponse
                        };
                    }

                    // 6. Save signed PDF back to database (update FileContent)
                    recommendationDoc.FileContent = signResult.SignedPdfContent;
                    recommendationDoc.FileSize = (decimal)(signResult.SignedPdfContent!.Length / 1024.0); // Size in KB
                    recommendationDoc.FileName = $"RecommendedForm_Signed_{applicationId}_{officer.Role}.pdf";
                    recommendationDoc.IsVerified = true;
                    recommendationDoc.VerifiedBy = officerId;
                    recommendationDoc.VerifiedDate = DateTime.UtcNow;
                    recommendationDoc.VerificationRemarks = $"Digitally signed by {officer.Role}";
                    recommendationDoc.UpdatedDate = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Updated signed PDF in database for application {ApplicationId}, size: {Size} KB",
                        applicationId, recommendationDoc.FileSize);

                    // 7. Update application status
                    application.Status = nextStatus;
                    application.UpdatedDate = DateTime.UtcNow;

                    // 8. Assign to next officer if not final signature
                    if (nextStatus != ApplicationCurrentStatus.CLERK_PENDING)
                    {
                        await AssignToNextOfficerAsync(application, nextStatus);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "✅ {Role} signature completed for application {ApplicationId}. New status: {Status}",
                        officer.Role, applicationId, nextStatus);

                    return new SignatureWorkflowResult
                    {
                        Success = true,
                        Message = successMessage,
                        SignedDocumentId = recommendationDoc.Id,
                        NextStatus = nextStatus
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during signature workflow");
                    
                    return new SignatureWorkflowResult
                    {
                        Success = false,
                        ErrorMessage = $"Exception: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Sign PDF using HSM with officer's KeyLabel (or test KeyLabel for local development)
        /// </summary>
        private async Task<HsmWorkflowSignResult> SignPdfWithOfficerKeyLabelAsync(
            int applicationId,
            int officerId,
            byte[] pdfContent,
            string otp,
            OfficerRole role,
            string? officerKeyLabel)
        {
            try
            {
                // Get KeyLabel from configuration based on officer role
                // Map specific roles to generic categories
                var roleKey = SignatureWorkflowHelpers.MapRoleToConfigKey(role);
                var keyLabel = _configuration[$"HSM:KeyLabels:{roleKey}"];

                if (string.IsNullOrEmpty(keyLabel))
                {
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = $"KeyLabel not configured for {roleKey} role"
                    };
                }
                
                _logger.LogInformation(
                    "Signing PDF for officer {OfficerId} ({Role}) using KeyLabel '{KeyLabel}'",
                    officerId, role, keyLabel);

                // Get signature coordinates for this role
                var coordinates = GetSignatureCoordinates(role, 0);
                
                _logger.LogInformation(
                    "📍 Signature coordinates for {Role}: {Coordinates}",
                    role, coordinates);

                // Convert PDF to Base64
                var base64Pdf = Convert.ToBase64String(pdfContent);

                // Sign PDF using HSM
                var signRequest = new HsmSignRequest
                {
                    TransactionId = applicationId.ToString(),
                    KeyLabel = keyLabel,
                    Base64Pdf = base64Pdf,
                    Otp = otp,
                    Coordinates = coordinates,
                    PageLocation = "last",
                    OtpType = "single"
                };

                var hsmResult = await _hsmService.SignPdfAsync(signRequest);

                if (!hsmResult.Success)
                {
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = hsmResult.ErrorMessage,
                        RawResponse = hsmResult.RawResponse
                    };
                }

                if (string.IsNullOrEmpty(hsmResult.SignedPdfBase64))
                {
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = "Signed PDF not returned by HSM",
                        RawResponse = hsmResult.RawResponse
                    };
                }

                // Convert Base64 back to bytes
                var signedPdfBytes = Convert.FromBase64String(hsmResult.SignedPdfBase64);

                return new HsmWorkflowSignResult
                {
                    Success = true,
                    Message = "PDF signed successfully",
                    SignedPdfContent = signedPdfBytes,
                    RawResponse = hsmResult.RawResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing PDF with test KeyLabel");
                return new HsmWorkflowSignResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Assign application to next officer in workflow
        /// </summary>
        private async Task AssignToNextOfficerAsync(PositionApplication application, ApplicationCurrentStatus status)
        {
            OfficerRole[]? nextRoles = status switch
            {
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => new[] 
                { 
                    OfficerRole.AssistantArchitect, OfficerRole.AssistantLicenceEngineer,
                    OfficerRole.AssistantStructuralEngineer, OfficerRole.AssistantSupervisor1,
                    OfficerRole.AssistantSupervisor2
                },
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => new[] { OfficerRole.ExecutiveEngineer },
                ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING => new[] { OfficerRole.CityEngineer },
                _ => null
            };

            if (nextRoles == null) return;

            // Find available officer with next role
            var nextOfficer = await _context.Officers
                .Where(o => nextRoles.Contains(o.Role) && o.IsActive)
                .OrderBy(o => Guid.NewGuid()) // Random assignment for load balancing
                .FirstOrDefaultAsync();

            if (nextOfficer != null)
            {
                // Assign based on status
                switch (status)
                {
                    case ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING:
                        // Assignment already done in JEWorkflowService
                        break;
                    case ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING:
                        application.AssignedExecutiveEngineerId = nextOfficer.Id;
                        application.AssignedToExecutiveEngineerDate = DateTime.UtcNow;
                        break;
                    case ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING:
                        application.AssignedCityEngineerId = nextOfficer.Id;
                        application.AssignedToCityEngineerDate = DateTime.UtcNow;
                        break;
                }

                _logger.LogInformation(
                    "Application {ApplicationId} assigned to {Role}: {OfficerName}",
                    application.Id, nextOfficer.Role, nextOfficer.Name);
            }
        }

        /// <summary>
        /// Get signature status for application
        /// </summary>
        public async Task<SignatureStatusResult> GetSignatureStatusAsync(int applicationId)
        {
            var application = await _context.PositionApplications
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return new SignatureStatusResult
                {
                    Success = false,
                    ErrorMessage = "Application not found"
                };
            }

            // Check which signatures have been completed based on application status
            var jeSignature = application.JEDigitalSignatureApplied;
            var aeSignature = application.AEArchitectDigitalSignatureApplied || 
                             application.AELicenceDigitalSignatureApplied || 
                             application.AEStructuralDigitalSignatureApplied || 
                             application.AESupervisor1DigitalSignatureApplied || 
                             application.AESupervisor2DigitalSignatureApplied;
            var eeSignature = application.ExecutiveEngineerDigitalSignatureApplied;
            var ceSignature = application.CityEngineerDigitalSignatureApplied;

            return new SignatureStatusResult
            {
                Success = true,
                ApplicationId = applicationId,
                CurrentStatus = application.Status,
                JuniorEngineerSigned = jeSignature,
                AssistantEngineerSigned = aeSignature,
                ExecutiveEngineerSigned = eeSignature,
                CityEngineerSigned = ceSignature,
                AllSignaturesComplete = ceSignature,
                NextSigner = GetNextSignerRole(application.Status)
            };
        }

        /// <summary>
        /// Determine next signer based on current status
        /// </summary>
        private string? GetNextSignerRole(ApplicationCurrentStatus status)
        {
            return status switch
            {
                ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE => "Junior Engineer",
                ApplicationCurrentStatus.ASSISTANT_ENGINEER_PENDING => "Assistant Engineer",
                ApplicationCurrentStatus.EXECUTIVE_ENGINEER_PENDING => "Executive Engineer",
                ApplicationCurrentStatus.CITY_ENGINEER_SIGN_PENDING => "City Engineer",
                ApplicationCurrentStatus.CLERK_PENDING => null, // All signatures complete
                _ => null
            };
        }
    }

    #region DTOs

    public class SignatureWorkflowResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int? SignedDocumentId { get; set; } // SEDocument ID in database
        public ApplicationCurrentStatus? NextStatus { get; set; }
        public string? RawHsmResponse { get; set; }
    }

    public class SignatureStatusResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationCurrentStatus CurrentStatus { get; set; }
        public bool JuniorEngineerSigned { get; set; }
        public bool AssistantEngineerSigned { get; set; }
        public bool ExecutiveEngineerSigned { get; set; }
        public bool CityEngineerSigned { get; set; }
        public bool AllSignaturesComplete { get; set; }
        public string? NextSigner { get; set; }
    }

    #endregion

    #region Helper Extensions

    /// <summary>
    /// Helper methods for role mapping
    /// </summary>
    public static class SignatureWorkflowHelpers
    {
        /// <summary>
        /// Maps specific officer roles to generic configuration keys
        /// </summary>
        public static string MapRoleToConfigKey(OfficerRole role)
        {
            return role switch
            {
                // Junior Engineer variants
                OfficerRole.JuniorArchitect or
                OfficerRole.JuniorStructuralEngineer or
                OfficerRole.JuniorLicenceEngineer or
                OfficerRole.JuniorSupervisor1 or
                OfficerRole.JuniorSupervisor2 => "JuniorEngineer",

                // Assistant Engineer variants
                OfficerRole.AssistantArchitect or
                OfficerRole.AssistantStructuralEngineer or
                OfficerRole.AssistantLicenceEngineer or
                OfficerRole.AssistantSupervisor1 or
                OfficerRole.AssistantSupervisor2 => "AssistantEngineer",

                // Executive Engineer
                OfficerRole.ExecutiveEngineer => "ExecutiveEngineer",

                // City Engineer
                OfficerRole.CityEngineer => "CityEngineer",

                // Default fallback
                _ => role.ToString().Replace(" ", "")
            };
        }
    }

    #endregion
}
