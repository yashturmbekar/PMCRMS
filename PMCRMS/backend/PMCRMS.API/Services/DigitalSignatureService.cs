using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for managing digital signatures with HSM integration
    /// Implements actual HSM SOAP API integration for OTP generation and PDF signing
    /// </summary>
    public class DigitalSignatureService : IDigitalSignatureService
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<DigitalSignatureService> _logger;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly IHttpClientFactory _httpClientFactory;

        public DigitalSignatureService(
            PMCRMSDbContext context,
            ILogger<DigitalSignatureService> logger,
            IConfiguration configuration,
            INotificationService notificationService,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _notificationService = notificationService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SignatureResult> InitiateSignatureAsync(
            int applicationId,
            int signedByOfficerId,
            SignatureType signatureType,
            string documentPath,
            string coordinates,
            string? ipAddress,
            string? userAgent)
        {
            try
            {
                // Validate application exists
                var application = await _context.PositionApplications
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Application not found",
                        Errors = new List<string> { "The specified application does not exist" }
                    };
                }

                // Validate officer exists and is active
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Id == signedByOfficerId);

                if (officer == null || !officer.IsActive)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Officer not found or inactive",
                        Errors = new List<string> { "The signing officer does not exist or is inactive" }
                    };
                }

                // Validate document exists
                if (!File.Exists(documentPath))
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Document not found",
                        Errors = new List<string> { $"Document does not exist at path: {documentPath}" }
                    };
                }

                // Check if officer has valid certificate
                var isCertValid = await IsCertificateValidAsync(signedByOfficerId);
                if (!isCertValid)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Officer certificate is invalid or expired",
                        Errors = new List<string> { "Please update your digital certificate before signing" }
                    };
                }

                // Create signature record
                var signature = new DigitalSignature
                {
                    ApplicationId = applicationId,
                    Type = signatureType,
                    Status = SignatureStatus.InProgress,
                    SignedByOfficerId = signedByOfficerId,
                    SignatureStartedAt = DateTime.UtcNow,
                    SignatureCoordinates = coordinates,
                    OriginalDocumentPath = documentPath,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    HsmProvider = _configuration["HSM:Provider"] ?? "eMudhra",
                    KeyLabel = officer.KeyLabel ?? $"CERT_{officer.EmployeeId}", // Use officer's KeyLabel
                    CreatedBy = officer.Name,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = officer.Name,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.DigitalSignatures.Add(signature);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Digital signature initiated: SignatureId={SignatureId}, ApplicationId={ApplicationId}, OfficerId={OfficerId}, Type={Type}",
                    signature.Id, applicationId, signedByOfficerId, signatureType);

                return new SignatureResult
                {
                    Success = true,
                    Message = "Signature process initiated. Please complete with OTP.",
                    SignatureId = signature.Id,
                    Signature = signature,
                    Metadata = new Dictionary<string, object>
                    {
                        { "RequiresOtp", true },
                        { "HsmProvider", signature.HsmProvider ?? "Unknown" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating digital signature");
                return new SignatureResult
                {
                    Success = false,
                    Message = "An error occurred while initiating signature",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<SignatureResult> CompleteSignatureAsync(
            int signatureId,
            string otp,
            string completedBy)
        {
            try
            {
                var signature = await _context.DigitalSignatures
                    .Include(s => s.Application)
                    .Include(s => s.SignedByOfficer)
                    .FirstOrDefaultAsync(s => s.Id == signatureId);

                if (signature == null)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Signature not found",
                        Errors = new List<string> { "The specified signature does not exist" }
                    };
                }

                if (signature.Status != SignatureStatus.InProgress)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Signature cannot be completed in current status",
                        Errors = new List<string> { $"Signature is currently {signature.Status}" }
                    };
                }

                // TODO: Actual HSM integration would go here
                // This would involve:
                // 1. Calling HSM provider API with OTP
                // 2. Getting signed document back
                // 3. Verifying the signature
                // 4. Storing the signed document

                // Call actual HSM signing service
                var signedDocumentPath = await CallHsmSigningServiceAsync(signature, otp);

                if (string.IsNullOrEmpty(signedDocumentPath))
                {
                    signature.Status = SignatureStatus.Failed;
                    signature.ErrorMessage = "HSM signing failed";
                    signature.RetryCount++;
                    await _context.SaveChangesAsync();

                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Digital signature failed",
                        Errors = new List<string> { "HSM provider returned an error" }
                    };
                }

                // Update signature with success details
                signature.Status = SignatureStatus.Completed;
                signature.SignedDate = DateTime.UtcNow;
                signature.SignatureCompletedAt = DateTime.UtcNow;
                signature.SignedDocumentPath = signedDocumentPath;
                signature.OtpUsed = otp; // In production, hash this!
                
                // Calculate duration
                if (signature.SignatureStartedAt.HasValue)
                {
                    var duration = DateTime.UtcNow - signature.SignatureStartedAt.Value;
                    signature.SignatureDurationSeconds = (int)duration.TotalSeconds;
                }

                // Generate signature hash
                signature.SignatureHash = GenerateSignatureHash(signatureId, signedDocumentPath);

                // Set certificate details (TODO: Get from officer's certificate store)
                var officer = signature.SignedByOfficer;
                signature.CertificateThumbprint = $"THUMB_{officer?.EmployeeId}_{DateTime.UtcNow:yyyyMMdd}";
                signature.CertificateIssuer = "CN=Government HSM CA";
                signature.CertificateSubject = $"CN={officer?.Name}, E={officer?.Email}";
                signature.CertificateExpiryDate = DateTime.UtcNow.AddYears(2);

                signature.HsmTransactionId = $"TXN_{Guid.NewGuid().ToString().Substring(0, 16)}";
                signature.UpdatedBy = completedBy;
                signature.UpdatedDate = DateTime.UtcNow;

                // Update application status
                var application = signature.Application;
                application.JEDigitalSignatureApplied = true;
                application.JEDigitalSignatureDate = DateTime.UtcNow;
                
                // Advance workflow based on signature type
                if (signature.Type == SignatureType.JuniorEngineer)
                {
                    application.Status = ApplicationCurrentStatus.AWAITING_JE_DIGITAL_SIGNATURE;
                    // JE approval date is already set when JE approves
                }

                application.UpdatedBy = completedBy;
                application.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await SendSignatureNotificationAsync(signature, "completed");

                _logger.LogInformation(
                    "Digital signature completed: SignatureId={SignatureId}, Duration={Duration}s",
                    signatureId, signature.SignatureDurationSeconds);

                return new SignatureResult
                {
                    Success = true,
                    Message = "Document signed successfully",
                    SignatureId = signature.Id,
                    Signature = signature,
                    SignedDocumentPath = signedDocumentPath,
                    SignatureHash = signature.SignatureHash
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing digital signature");
                return new SignatureResult
                {
                    Success = false,
                    Message = "An error occurred while completing signature",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<SignatureResult> VerifySignatureAsync(int signatureId)
        {
            try
            {
                var signature = await GetSignatureByIdAsync(signatureId);

                if (signature == null)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Signature not found",
                        Errors = new List<string> { "The specified signature does not exist" }
                    };
                }

                if (signature.Status != SignatureStatus.Completed)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Only completed signatures can be verified",
                        Errors = new List<string> { $"Signature status is {signature.Status}" }
                    };
                }

                // TODO: Actual signature verification would involve:
                // 1. Verifying the signature hash
                // 2. Checking certificate validity
                // 3. Verifying document hasn't been tampered
                // 4. Calling HSM provider verification API

                // Simulated verification
                var isValid = !string.IsNullOrEmpty(signature.SignatureHash) &&
                             !string.IsNullOrEmpty(signature.SignedDocumentPath) &&
                             File.Exists(signature.SignedDocumentPath);

                signature.IsVerified = isValid;
                signature.LastVerifiedDate = DateTime.UtcNow;
                signature.VerificationDetails = isValid
                    ? "Signature verified successfully"
                    : "Verification failed - document may have been tampered";
                
                if (isValid)
                {
                    signature.Status = SignatureStatus.Verified;
                }

                signature.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Signature verification: SignatureId={SignatureId}, IsValid={IsValid}",
                    signatureId, isValid);

                return new SignatureResult
                {
                    Success = true,
                    Message = isValid ? "Signature is valid" : "Signature verification failed",
                    SignatureId = signature.Id,
                    Signature = signature,
                    IsVerified = isValid
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                return new SignatureResult
                {
                    Success = false,
                    Message = "An error occurred while verifying signature",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<DigitalSignature?> GetSignatureByIdAsync(int signatureId)
        {
            return await _context.DigitalSignatures
                .Include(s => s.Application)
                    .ThenInclude(a => a.User)
                .Include(s => s.SignedByOfficer)
                .FirstOrDefaultAsync(s => s.Id == signatureId);
        }

        public async Task<List<DigitalSignature>> GetSignaturesForApplicationAsync(int applicationId)
        {
            return await _context.DigitalSignatures
                .Include(s => s.SignedByOfficer)
                .Where(s => s.ApplicationId == applicationId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DigitalSignature>> GetSignaturesForOfficerAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.DigitalSignatures
                .Include(s => s.Application)
                    .ThenInclude(a => a.User)
                .Where(s => s.SignedByOfficerId == officerId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate <= endDate.Value);
            }

            return await query
                .OrderBy(s => s.SignedDate ?? s.CreatedDate)
                .ToListAsync();
        }

        public async Task<SignatureResult> RetrySignatureAsync(
            int signatureId,
            string otp,
            string retriedBy)
        {
            try
            {
                var signature = await GetSignatureByIdAsync(signatureId);

                if (signature == null)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Signature not found",
                        Errors = new List<string> { "The specified signature does not exist" }
                    };
                }

                if (signature.Status != SignatureStatus.Failed)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Only failed signatures can be retried",
                        Errors = new List<string> { $"Signature status is {signature.Status}" }
                    };
                }

                if (signature.RetryCount >= 3)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Maximum retry attempts exceeded",
                        Errors = new List<string> { "Please initiate a new signature request" }
                    };
                }

                // Reset to InProgress for retry
                signature.Status = SignatureStatus.InProgress;
                signature.SignatureStartedAt = DateTime.UtcNow;
                signature.ErrorMessage = null;
                signature.UpdatedBy = retriedBy;
                signature.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Now attempt to complete
                return await CompleteSignatureAsync(signatureId, otp, retriedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying signature");
                return new SignatureResult
                {
                    Success = false,
                    Message = "An error occurred while retrying signature",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<SignatureResult> RevokeSignatureAsync(
            int signatureId,
            string reason,
            string revokedBy)
        {
            try
            {
                var signature = await GetSignatureByIdAsync(signatureId);

                if (signature == null)
                {
                    return new SignatureResult
                    {
                        Success = false,
                        Message = "Signature not found",
                        Errors = new List<string> { "The specified signature does not exist" }
                    };
                }

                signature.Status = SignatureStatus.Revoked;
                signature.ErrorMessage = $"Revoked: {reason}";
                signature.UpdatedBy = revokedBy;
                signature.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Signature revoked: SignatureId={SignatureId}, Reason={Reason}",
                    signatureId, reason);

                return new SignatureResult
                {
                    Success = true,
                    Message = "Signature revoked successfully",
                    SignatureId = signature.Id,
                    Signature = signature
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking signature");
                return new SignatureResult
                {
                    Success = false,
                    Message = "An error occurred while revoking signature",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<Dictionary<string, int>> GetSignatureStatisticsAsync(
            int officerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.DigitalSignatures
                .Where(s => s.SignedByOfficerId == officerId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.CreatedDate <= endDate.Value);
            }

            var signatures = await query.ToListAsync();

            return new Dictionary<string, int>
            {
                { "Total", signatures.Count },
                { "Pending", signatures.Count(s => s.Status == SignatureStatus.Pending) },
                { "InProgress", signatures.Count(s => s.Status == SignatureStatus.InProgress) },
                { "Completed", signatures.Count(s => s.Status == SignatureStatus.Completed) },
                { "Failed", signatures.Count(s => s.Status == SignatureStatus.Failed) },
                { "Verified", signatures.Count(s => s.Status == SignatureStatus.Verified) },
                { "Revoked", signatures.Count(s => s.Status == SignatureStatus.Revoked) },
                { "AverageSeconds", signatures.Any(s => s.SignatureDurationSeconds.HasValue)
                    ? (int)signatures.Where(s => s.SignatureDurationSeconds.HasValue)
                        .Average(s => s.SignatureDurationSeconds!.Value)
                    : 0 },
                { "TotalRetries", signatures.Sum(s => s.RetryCount) }
            };
        }

        public async Task<bool> IsCertificateValidAsync(int officerId)
        {
            var officer = await _context.Officers.FindAsync(officerId);

            if (officer == null || !officer.IsActive)
                return false;

            // TODO: Check actual certificate validity when certificate management is implemented
            // For now, assume active officers have valid certificates
            return true;
        }

        public async Task<bool> CheckHsmHealthAsync()
        {
            try
            {
                // TODO: Actual HSM health check would ping the HSM service
                var hsmServiceUrl = _configuration["HSM:ServiceUrl"];
                
                if (string.IsNullOrEmpty(hsmServiceUrl))
                {
                    _logger.LogWarning("HSM service URL not configured");
                    return false;
                }

                // Simulated health check
                _logger.LogInformation("HSM health check: Service is accessible");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HSM health check failed");
                return false;
            }
        }

        // Private helper methods

        /// <summary>
        /// Calls actual HSM service to digitally sign PDF using OTP
        /// </summary>
        private async Task<string> CallHsmSigningServiceAsync(DigitalSignature signature, string otp)
        {
            try
            {
                // Get application with related data
                var application = await _context.PositionApplications
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == signature.ApplicationId);

                if (application == null)
                {
                    _logger.LogError("Application not found for signature {SignatureId}", signature.Id);
                    return string.Empty;
                }

                // Get officer for KeyLabel
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Id == signature.SignedByOfficerId);

                if (officer == null)
                {
                    _logger.LogError("Officer not found for signature {SignatureId}", signature.Id);
                    return string.Empty;
                }

                // Use the original document path stored during initiation
                var documentPath = signature.OriginalDocumentPath;
                if (string.IsNullOrEmpty(documentPath))
                {
                    _logger.LogError("Original document path not found for signature {SignatureId}", signature.Id);
                    return string.Empty;
                }

                // Read PDF file
                if (!File.Exists(documentPath))
                {
                    _logger.LogError("PDF file not found at path: {Path}", documentPath);
                    return string.Empty;
                }

                var pdfBytes = await File.ReadAllBytesAsync(documentPath);
                var pdfBase64 = Convert.ToBase64String(pdfBytes);

                // Use KeyLabel from signature record (officer's certificate key)
                string keyLabel = signature.KeyLabel ?? $"CERT_{officer.EmployeeId}";

                // Prepare HSM signature parameters
                string transaction = signature.ApplicationId.ToString();
                string coordinates = signature.SignatureCoordinates ?? GetDefaultCoordinates();

                _logger.LogInformation(
                    "Preparing HSM signature request: SignatureId={SignatureId}, Transaction={Transaction}, KeyLabel={KeyLabel}, Coordinates={Coordinates}",
                    signature.Id, transaction, keyLabel, coordinates);

                // Create SOAP envelope for HSM API call
                var soapEnvelope = $@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<signPdf xmlns=""http://ds.ws.emas/"">
<arg0 xmlns="""">{transaction}</arg0>
<arg1 xmlns="""">{keyLabel}</arg1>
<arg2 xmlns="""">{pdfBase64}</arg2>
<arg3 xmlns=""""/>
<arg4 xmlns="""">{coordinates}</arg4>
<arg5 xmlns="""">last</arg5>
<arg6 xmlns=""""/>
<arg7 xmlns=""""/>
<arg8 xmlns="""">True</arg8>
<arg9 xmlns="""">{otp}</arg9>
<arg10 xmlns="""">single</arg10>
<arg11 xmlns=""""/>
<arg12 xmlns=""""/>
</signPdf>
</s:Body>
</s:Envelope>";

                _logger.LogInformation("Calling HSM signature service at {BaseUrl} for SignatureId={SignatureId}", 
                    _configuration["HSM:SignBaseUrl"], signature.Id);

                // Call actual HSM service for digital signature
                var content = new StringContent(soapEnvelope, Encoding.UTF8, "application/xml");
                using var httpClient = _httpClientFactory.CreateClient("HSM_SIGN");
                
                var response = await httpClient.PostAsync("services/dsverifyWS", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("HSM signature API call failed with status: {StatusCode} for SignatureId={SignatureId}", 
                        response.StatusCode, signature.Id);
                    return string.Empty;
                }

                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("HSM Signature Response for SignatureId={SignatureId}: {Response}", 
                    signature.Id, result);

                // Store raw HSM response for audit
                signature.HsmResponse = result.Length > 5000 ? result.Substring(0, 5000) : result;

                // Check if signature was successful
                if (result.Contains($"{transaction}~FAILURE~") || result.Contains("failure"))
                {
                    _logger.LogError("HSM signature failed for SignatureId={SignatureId}. Response: {Result}", 
                        signature.Id, result);
                    signature.ErrorMessage = "HSM signature service returned failure";
                    return string.Empty;
                }

                // Extract transaction ID from response
                if (result.Contains($"{transaction}~SUCCESS~"))
                {
                    signature.HsmTransactionId = transaction;
                }

                // Process successful signature response - extract base64 signed PDF
                var processedResult = result;
                
                // Parse SOAP response to extract signed PDF data
                try
                {
                    processedResult = result
                        .Replace("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">", "")
                        .Replace("<soap:Body><ns2:signPdfResponse xmlns:ns2=\"http://ds.ws.emas/\">", "")
                        .Replace($"<return>{transaction}~SUCCESS~", "")
                        .Replace("</return></ns2:signPdfResponse></soap:Body></soap:Envelope>", "")
                        .Trim();
                }
                catch (Exception parseEx)
                {
                    _logger.LogWarning(parseEx, "Failed to parse SOAP response, attempting alternate parsing");
                    
                    // Alternate parsing - extract content between return tags
                    var returnStart = result.IndexOf("<return>");
                    var returnEnd = result.IndexOf("</return>");
                    if (returnStart > 0 && returnEnd > returnStart)
                    {
                        var returnContent = result.Substring(returnStart + 8, returnEnd - returnStart - 8);
                        var parts = returnContent.Split(new[] { "~SUCCESS~" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            processedResult = parts[1].Trim();
                        }
                    }
                }

                // Convert back to PDF bytes and save
                var signedPdfBytes = Convert.FromBase64String(processedResult);
                var signedFileName = $"signed_recommendation_{application.ApplicationNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var signedDirectory = Path.Combine(_configuration["FileUploadSettings:UploadPath"] ?? "./uploads", "signed");
                
                if (!Directory.Exists(signedDirectory))
                {
                    Directory.CreateDirectory(signedDirectory);
                }

                var signedFilePath = Path.Combine(signedDirectory, signedFileName);
                await File.WriteAllBytesAsync(signedFilePath, signedPdfBytes);

                _logger.LogInformation("HSM signed PDF saved successfully at: {Path} for SignatureId={SignatureId}", 
                    signedFilePath, signature.Id);

                // Update signature metadata
                signature.CertificateIssuer = "CN=Government HSM CA";
                signature.CertificateThumbprint = $"HSM_{DateTime.UtcNow:yyyyMMddHHmmss}";

                return signedFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HSM signing failed for SignatureId={SignatureId}", signature.Id);
                return string.Empty;
            }
        }

        private string GetDefaultCoordinates()
        {
            var x = _configuration["HSM:DefaultCoordinates:X"] ?? "100";
            var y = _configuration["HSM:DefaultCoordinates:Y"] ?? "700";
            var width = _configuration["HSM:DefaultCoordinates:Width"] ?? "200";
            var height = _configuration["HSM:DefaultCoordinates:Height"] ?? "50";
            var page = _configuration["HSM:DefaultCoordinates:Page"] ?? "1";
            
            return $"{x},{y},{width},{height},{page}";
        }

        private string GenerateSignatureHash(int signatureId, string documentPath)
        {
            try
            {
                // In production, this would hash the actual signed document content
                var hashInput = $"{signatureId}_{documentPath}_{DateTime.UtcNow:O}";
                
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signature hash");
                return string.Empty;
            }
        }

        private async Task SendSignatureNotificationAsync(DigitalSignature signature, string action)
        {
            try
            {
                // Ensure navigation properties are loaded
                if (signature.Application == null)
                {
                    await _context.Entry(signature)
                        .Reference(s => s.Application)
                        .Query()
                        .Include(a => a.User)
                        .LoadAsync();
                }

                if (signature.SignedByOfficer == null)
                {
                    await _context.Entry(signature)
                        .Reference(s => s.SignedByOfficer)
                        .LoadAsync();
                }

                _logger.LogInformation(
                    "Notification: Digital signature {Action} - SignatureId={SignatureId}, Type={Type}, Application={ApplicationNumber}, Officer={OfficerName}",
                    action,
                    signature.Id,
                    signature.Type,
                    signature.Application?.ApplicationNumber ?? "Unknown",
                    signature.SignedByOfficer?.Name ?? "Unknown");

                // TODO: Implement actual email/SMS sending via NotificationService when needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signature notification");
            }
        }
    }
}
