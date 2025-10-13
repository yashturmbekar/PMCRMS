using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMCRMS.API.Configuration;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for interacting with HSM (Hardware Security Module) for digital signatures
    /// Implements eMudhra HSM integration for OTP generation and PDF signing
    /// </summary>
    public interface IHsmService
    {
        /// <summary>
        /// Generate OTP for digital signature
        /// </summary>
        Task<HsmOtpResult> GenerateOtpAsync(string transactionId, string keyLabel, string otpType = "single");

        /// <summary>
        /// Sign PDF document using HSM
        /// </summary>
        Task<HsmSignResult> SignPdfAsync(HsmSignRequest request);

        /// <summary>
        /// Check HSM service health
        /// </summary>
        Task<bool> CheckHealthAsync();

        /// <summary>
        /// Generate OTP for officer signature workflow (validates officer KeyLabel)
        /// </summary>
        Task<HsmWorkflowOtpResult> GenerateOtpForOfficerAsync(int applicationId, int officerId, PMCRMSDbContext context);

        /// <summary>
        /// Sign PDF with OTP verification for officer signature workflow
        /// </summary>
        Task<HsmWorkflowSignResult> SignPdfForOfficerAsync(int applicationId, int officerId, byte[] pdfContent, string otp, PMCRMSDbContext context);
    }

    public class HsmService : IHsmService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _signerHttpClient;
        private readonly ILogger<HsmService> _logger;
        private readonly HsmConfiguration _config;

        public HsmService(
            IHttpClientFactory httpClientFactory,
            IOptions<HsmConfiguration> hsmConfig,
            ILogger<HsmService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("HsmClient");
            _signerHttpClient = httpClientFactory.CreateClient("SignerClient");
            _logger = logger;
            _config = hsmConfig.Value;
        }

        /// <summary>
        /// Generate OTP for digital signature
        /// </summary>
        /// <param name="transactionId">Transaction/Application ID</param>
        /// <param name="keyLabel">HSM key label for the officer</param>
        /// <param name="otpType">single or bulk (default: single)</param>
        public async Task<HsmOtpResult> GenerateOtpAsync(string transactionId, string keyLabel, string otpType = "single")
        {
            try
            {
                _logger.LogInformation("Generating OTP for transaction {TransactionId}, keyLabel: {KeyLabel}, type: {OtpType}",
                    transactionId, keyLabel, otpType);

                var requestBody = new
                {
                    otptype = otpType,
                    ptno = "1",
                    txn = transactionId,
                    klabel = keyLabel
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("HSM/GenOtp", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("HSM OTP Response Status: {StatusCode}, Body: {Response}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse HSM response JSON
                        var hsmResponse = JsonSerializer.Deserialize<HsmOtpResponse>(responseContent);
                        
                        // Check status field (1 = success)
                        if (hsmResponse?.status == 1)
                        {
                            _logger.LogInformation("✅ OTP sent successfully: {Message}", hsmResponse.succMsg);
                            
                            return new HsmOtpResult
                            {
                                Success = true,
                                TransactionId = transactionId,
                                Message = hsmResponse.succMsg ?? "OTP sent successfully to officer's mobile",
                                RawResponse = responseContent
                            };
                        }
                        else
                        {
                            _logger.LogError("❌ HSM OTP failed. Status: {Status}, Error: {Error}", 
                                hsmResponse?.status, hsmResponse?.errMsg);
                            
                            return new HsmOtpResult
                            {
                                Success = false,
                                ErrorMessage = hsmResponse?.errMsg ?? "OTP generation failed",
                                RawResponse = responseContent
                            };
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse HSM OTP response JSON");
                        return new HsmOtpResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to parse HSM response",
                            RawResponse = responseContent
                        };
                    }
                }
                else
                {
                    _logger.LogError("HSM OTP generation failed. HTTP Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);

                    return new HsmOtpResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}",
                        RawResponse = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for transaction {TransactionId}", transactionId);
                return new HsmOtpResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sign PDF document using HSM digital signature
        /// </summary>
        public async Task<HsmSignResult> SignPdfAsync(HsmSignRequest request)
        {
            try
            {
                _logger.LogInformation("Signing PDF for transaction {TransactionId} with key {KeyLabel}",
                    request.TransactionId, request.KeyLabel);

                // Build SOAP envelope for eMudhra signature service - matches HSM specification exactly
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
    <signPdf xmlns=""http://ds.ws.emas/"">
      <arg0 xmlns="""">{request.TransactionId}</arg0>
      <arg1 xmlns="""">{request.KeyLabel}</arg1>
      <arg2 xmlns="""">{request.Base64Pdf}</arg2>
      <arg4 xmlns="""">{request.Coordinates}</arg4>
      <arg5 xmlns="""">{request.PageLocation ?? "last"}</arg5>
      <arg8 xmlns="""">True</arg8>
      <arg9 xmlns="""">{request.Otp}</arg9>
      <arg10 xmlns="""">{request.OtpType ?? "single"}</arg10>
    </signPdf>
  </s:Body>
</s:Envelope>";

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "application/xml");

                var response = await _signerHttpClient.PostAsync("services/dsverifyWS", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("HSM Sign Response (partial): {Response}",
                    responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Check if signature was successful or failed
                    if (responseContent.Contains($"{request.TransactionId}~FAILURE~"))
                    {
                        var failureReason = ExtractFailureReason(responseContent);
                        _logger.LogError("HSM signature failed: {Reason}", failureReason);

                        return new HsmSignResult
                        {
                            Success = false,
                            TransactionId = request.TransactionId,
                            ErrorMessage = failureReason,
                            RawResponse = responseContent
                        };
                    }

                    // Extract signed PDF from SOAP response
                    var signedPdfBase64 = ExtractSignedPdfFromSoap(responseContent, request.TransactionId);

                    if (!string.IsNullOrEmpty(signedPdfBase64))
                    {
                        return new HsmSignResult
                        {
                            Success = true,
                            TransactionId = request.TransactionId,
                            SignedPdfBase64 = signedPdfBase64,
                            Message = "PDF signed successfully",
                            RawResponse = responseContent
                        };
                    }
                    else
                    {
                        _logger.LogError("Failed to extract signed PDF from response");
                        return new HsmSignResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to extract signed PDF from response",
                            RawResponse = responseContent
                        };
                    }
                }
                else
                {
                    _logger.LogError("HSM signature request failed. Status: {Status}", response.StatusCode);
                    return new HsmSignResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP request failed with status {response.StatusCode}",
                        RawResponse = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing PDF for transaction {TransactionId}", request.TransactionId);
                return new HsmSignResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check if HSM service is accessible
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                // Try a simple GET request to the base URL
                var response = await _httpClient.GetAsync("");
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HSM health check failed");
                return false;
            }
        }

        /// <summary>
        /// Extract signed PDF Base64 from SOAP response
        /// </summary>
        private string? ExtractSignedPdfFromSoap(string soapResponse, string transactionId)
        {
            try
            {
                // Remove SOAP envelope tags
                var cleaned = soapResponse
                    .Replace("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">", "")
                    .Replace("<soap:Body><ns2:signPdfResponse xmlns:ns2=\"http://ds.ws.emas/\">", "")
                    .Replace($"<return>{transactionId}~SUCCESS~", "")
                    .Replace("</return></ns2:signPdfResponse></soap:Body></soap:Envelope>", "")
                    .Trim();

                // Validate it's Base64
                if (IsBase64String(cleaned))
                {
                    return cleaned;
                }

                // Try parsing as XML if string replacement didn't work
                try
                {
                    var xdoc = XDocument.Parse(soapResponse);
                    XNamespace ns = "http://ds.ws.emas/";
                    var returnElement = xdoc.Descendants(ns + "return").FirstOrDefault();

                    if (returnElement != null)
                    {
                        var value = returnElement.Value;
                        if (value.Contains("~SUCCESS~"))
                        {
                            var parts = value.Split(new[] { "~SUCCESS~" }, StringSplitOptions.None);
                            if (parts.Length > 1 && IsBase64String(parts[1]))
                            {
                                return parts[1];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SOAP XML");
                }

                return cleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting signed PDF from SOAP response");
                return null;
            }
        }

        /// <summary>
        /// Extract failure reason from SOAP response
        /// </summary>
        private string ExtractFailureReason(string soapResponse)
        {
            try
            {
                var startIndex = soapResponse.IndexOf("~FAILURE~");
                if (startIndex != -1)
                {
                    var endIndex = soapResponse.IndexOf("</return>", startIndex);
                    if (endIndex != -1)
                    {
                        return soapResponse.Substring(startIndex + 9, endIndex - (startIndex + 9)).Trim();
                    }
                }
                return "Signature failed - unknown reason";
            }
            catch
            {
                return "Signature failed - could not parse error message";
            }
        }

        /// <summary>
        /// Validate if string is Base64 encoded
        /// </summary>
        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            base64 = base64.Trim();

            return base64.Length % 4 == 0 &&
                   System.Text.RegularExpressions.Regex.IsMatch(base64, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }

        /// <summary>
        /// Generate OTP for officer signature workflow
        /// Validates officer exists and has KeyLabel configured
        /// </summary>
        public async Task<HsmWorkflowOtpResult> GenerateOtpForOfficerAsync(int applicationId, int officerId, PMCRMSDbContext context)
        {
            try
            {
                // Get officer with KeyLabel
                var officer = await context.Officers.FindAsync(officerId);
                if (officer == null)
                {
                    return new HsmWorkflowOtpResult
                    {
                        Success = false,
                        ErrorMessage = "Officer not found"
                    };
                }

                // Validate KeyLabel
                if (string.IsNullOrEmpty(officer.KeyLabel))
                {
                    return new HsmWorkflowOtpResult
                    {
                        Success = false,
                        ErrorMessage = $"Officer {officer.Name} ({officer.Role}) does not have a KeyLabel configured"
                    };
                }

                // 🧪 TESTING: Hardcoded KeyLabel for HSM OTP testing
                var testKeyLabel = "Test2025Sign";
                
                _logger.LogInformation(
                    "🧪 TESTING MODE: Generating OTP for officer {OfficerId} ({OfficerName}, {Role}) using TEST KeyLabel '{TestKeyLabel}' (Original: {KeyLabel}) for application {ApplicationId}",
                    officerId, officer.Name, officer.Role, testKeyLabel, officer.KeyLabel, applicationId);

                // Call HSM OTP service with hardcoded test KeyLabel
                var hsmResult = await GenerateOtpAsync(
                    transactionId: applicationId.ToString(),
                    keyLabel: testKeyLabel, // 🧪 Using hardcoded test KeyLabel
                    otpType: "single"
                );

                if (!hsmResult.Success)
                {
                    _logger.LogError("HSM OTP generation failed: {Error}", hsmResult.ErrorMessage);
                    return new HsmWorkflowOtpResult
                    {
                        Success = false,
                        ErrorMessage = hsmResult.ErrorMessage ?? "OTP send failed",
                        RawResponse = hsmResult.RawResponse
                    };
                }

                _logger.LogInformation("✅ OTP sent successfully to officer {OfficerId} mobile", officerId);

                // ✅ Return success message from HSM (no local storage needed)
                return new HsmWorkflowOtpResult
                {
                    Success = true,
                    Message = hsmResult.Message ?? "OTP sent successfully to your mobile",
                    RawResponse = hsmResult.RawResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for officer {OfficerId}", officerId);
                return new HsmWorkflowOtpResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sign PDF for officer signature workflow
        /// Validates officer, KeyLabel, and performs HSM signature
        /// </summary>
        public async Task<HsmWorkflowSignResult> SignPdfForOfficerAsync(
            int applicationId, 
            int officerId, 
            byte[] pdfContent, 
            string otp, 
            PMCRMSDbContext context)
        {
            try
            {
                // Get officer with KeyLabel
                var officer = await context.Officers.FindAsync(officerId);
                if (officer == null)
                {
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = "Officer not found"
                    };
                }

                // Validate KeyLabel
                if (string.IsNullOrEmpty(officer.KeyLabel))
                {
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = $"Officer {officer.Name} ({officer.Role}) does not have a KeyLabel configured"
                    };
                }

                _logger.LogInformation(
                    "Signing PDF for officer {OfficerId} ({OfficerName}, {Role}) with KeyLabel {KeyLabel} for application {ApplicationId}",
                    officerId, officer.Name, officer.Role, officer.KeyLabel, applicationId);

                // Convert PDF to Base64
                var base64Pdf = Convert.ToBase64String(pdfContent);

                // Sign PDF using HSM
                var signRequest = new HsmSignRequest
                {
                    TransactionId = applicationId.ToString(),
                    KeyLabel = officer.KeyLabel,
                    Base64Pdf = base64Pdf,
                    Otp = otp,
                    Coordinates = SignatureCoordinates.RecommendationForm,
                    PageLocation = "last",
                    OtpType = "single"
                };

                var hsmResult = await SignPdfAsync(signRequest);

                if (!hsmResult.Success)
                {
                    _logger.LogError("HSM signature failed: {Error}", hsmResult.ErrorMessage);
                    return new HsmWorkflowSignResult
                    {
                        Success = false,
                        ErrorMessage = $"HSM signature failed: {hsmResult.ErrorMessage}",
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

                _logger.LogInformation("PDF signed successfully for officer {OfficerId}", officerId);

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
                _logger.LogError(ex, "Error signing PDF for officer {OfficerId}", officerId);
                return new HsmWorkflowSignResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }
    }

    #region DTOs

    // HSM OTP Response format
    public class HsmOtpResponse
    {
        public int status { get; set; }
        public string? txn { get; set; }
        public string? errCode { get; set; }
        public string? errMsg { get; set; }
        public string? retValue { get; set; }
        public string? succMsg { get; set; }
        public string? otp { get; set; }
    }

    public class HsmOtpResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }

    public class HsmSignRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string KeyLabel { get; set; } = string.Empty;
        public string Base64Pdf { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string Coordinates { get; set; } = "117,383,236,324"; // Default coordinates
        public string? PageLocation { get; set; } = "last";
        public string? OtpType { get; set; } = "single";
    }

    public class HsmSignResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? SignedPdfBase64 { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }

    /// <summary>
    /// Result for workflow OTP generation (with officer validation)
    /// HSM sends OTP directly to officer's mobile - no local storage
    /// </summary>
    public class HsmWorkflowOtpResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }

    /// <summary>
    /// Result for workflow PDF signing (with officer validation)
    /// </summary>
    public class HsmWorkflowSignResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public byte[]? SignedPdfContent { get; set; }
        public string? RawResponse { get; set; }
    }

    #endregion
}
