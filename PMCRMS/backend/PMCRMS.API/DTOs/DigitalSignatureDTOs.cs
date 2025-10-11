using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    /// <summary>
    /// Request to initiate digital signature
    /// </summary>
    public class InitiateSignatureRequestDto
    {
        public int ApplicationId { get; set; }
        public SignatureType SignatureType { get; set; }
        public string DocumentPath { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
    }

    /// <summary>
    /// Request to complete signature with OTP
    /// </summary>
    public class CompleteSignatureRequestDto
    {
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to retry failed signature
    /// </summary>
    public class RetrySignatureRequestDto
    {
        public string Otp { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request to revoke signature
    /// </summary>
    public class RevokeSignatureRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Full signature details response
    /// </summary>
    public class SignatureResponseDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public SignatureType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
        public SignatureStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public int SignedByOfficerId { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public DateTime? SignedDate { get; set; }
        public string? SignedDocumentPath { get; set; }
        public string? SignatureHash { get; set; }
        public string? CertificateThumbprint { get; set; }
        public string? CertificateIssuer { get; set; }
        public string? CertificateSubject { get; set; }
        public DateTime? CertificateExpiryDate { get; set; }
        public string? HsmProvider { get; set; }
        public string? HsmTransactionId { get; set; }
        public string? SignatureCoordinates { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? LastVerifiedDate { get; set; }
        public string? VerificationDetails { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? SignatureStartedAt { get; set; }
        public DateTime? SignatureCompletedAt { get; set; }
        public int? SignatureDurationSeconds { get; set; }
        public bool IsCertificateValid { get; set; }
        public int DaysUntilCertificateExpiry { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Simplified signature list item
    /// </summary>
    public class SignatureListDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public SignatureType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
        public SignatureStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime? SignedDate { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public bool IsPending { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasError { get; set; }
    }

    /// <summary>
    /// Certificate information
    /// </summary>
    public class CertificateInfoDto
    {
        public string? Thumbprint { get; set; }
        public string? Issuer { get; set; }
        public string? Subject { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public int DaysUntilExpiry { get; set; }
        public string? Provider { get; set; }
        public string? KeyLabel { get; set; }
    }

    /// <summary>
    /// Signature verification result
    /// </summary>
    public class SignatureVerificationDto
    {
        public int SignatureId { get; set; }
        public bool IsValid { get; set; }
        public bool IsVerified { get; set; }
        public string? VerificationMessage { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? CertificateStatus { get; set; }
        public bool IsCertificateValid { get; set; }
        public bool IsDocumentTampered { get; set; }
        public Dictionary<string, object> VerificationDetails { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Signature statistics
    /// </summary>
    public class SignatureStatisticsDto
    {
        public int TotalSignatures { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int VerifiedCount { get; set; }
        public int RevokedCount { get; set; }
        public int AverageSignatureDurationSeconds { get; set; }
        public double SuccessRate { get; set; }
        public double VerificationRate { get; set; }
        public int TotalRetries { get; set; }
    }

    /// <summary>
    /// HSM health status
    /// </summary>
    public class HsmHealthDto
    {
        public bool IsHealthy { get; set; }
        public string ServiceUrl { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
        public int ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> ServiceInfo { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Signature coordinates for PDF
    /// </summary>
    public class SignatureCoordinatesDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Page { get; set; } = 1;

        public override string ToString()
        {
            return $"{X},{Y},{Width},{Height},{Page}";
        }

        public static SignatureCoordinatesDto Parse(string coordinates)
        {
            var parts = coordinates.Split(',');
            if (parts.Length < 4)
                throw new ArgumentException("Invalid coordinates format. Expected: x,y,width,height,page");

            return new SignatureCoordinatesDto
            {
                X = int.Parse(parts[0]),
                Y = int.Parse(parts[1]),
                Width = int.Parse(parts[2]),
                Height = int.Parse(parts[3]),
                Page = parts.Length > 4 ? int.Parse(parts[4]) : 1
            };
        }
    }

    /// <summary>
    /// Application signature summary
    /// </summary>
    public class ApplicationSignatureSummaryDto
    {
        public int ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public int TotalSignatures { get; set; }
        public int CompletedSignatures { get; set; }
        public int PendingSignatures { get; set; }
        public int FailedSignatures { get; set; }
        public bool AllSignaturesCompleted { get; set; }
        public double SignatureProgress { get; set; }
        public List<SignatureListDto> Signatures { get; set; } = new List<SignatureListDto>();
    }
}
