namespace PMCRMS.API.Configuration
{
    /// <summary>
    /// Configuration for HSM (Hardware Security Module) integration
    /// </summary>
    public class HsmConfiguration
    {
        public const string SectionName = "HsmConfiguration";

        /// <summary>
        /// Base URL for HSM OTP generation service
        /// Example: http://210.212.188.44:8001/jrequest/
        /// </summary>
        public string OtpServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for HSM signer service
        /// Example: http://210.212.188.35:8080/emSigner/
        /// </summary>
        public string SignerServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Default timeout for HTTP requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Key labels for different officer positions
        /// </summary>
        public OfficerKeyLabels KeyLabels { get; set; } = new();

        /// <summary>
        /// Enable/disable HSM integration
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enable detailed logging of HSM requests/responses
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Retry configuration for HSM operations
        /// </summary>
        public RetryConfiguration Retry { get; set; } = new();
    }

    /// <summary>
    /// HSM key labels for different officer positions
    /// </summary>
    public class OfficerKeyLabels
    {
        /// <summary>
        /// Junior Engineer key labels mapped by position type
        /// </summary>
        public Dictionary<string, string> JuniorEngineers { get; set; } = new()
        {
            { "Architect", "09160" },
            { "StructuralEngineer", "09161" },
            { "LicenceEngineer", "09162" },
            { "Supervisor1", "09163" },
            { "Supervisor2", "09164" }
        };

        /// <summary>
        /// Assistant Engineer key labels mapped by position type
        /// </summary>
        public Dictionary<string, string> AssistantEngineers { get; set; } = new()
        {
            { "Architect", "09170" },
            { "StructuralEngineer", "09171" },
            { "LicenceEngineer", "09172" },
            { "Supervisor1", "09173" },
            { "Supervisor2", "09174" }
        };

        /// <summary>
        /// Executive Engineer key labels
        /// </summary>
        public Dictionary<string, string> ExecutiveEngineers { get; set; } = new()
        {
            { "Default", "09180" }
        };

        /// <summary>
        /// City Engineer key labels
        /// </summary>
        public Dictionary<string, string> CityEngineers { get; set; } = new()
        {
            { "Default", "09190" }
        };

        /// <summary>
        /// Get key label for specific officer type and position
        /// </summary>
        public string? GetKeyLabel(string officerType, string positionType = "Default")
        {
            return officerType switch
            {
                "JE" => JuniorEngineers.GetValueOrDefault(positionType),
                "AE" => AssistantEngineers.GetValueOrDefault(positionType),
                "EE" => ExecutiveEngineers.GetValueOrDefault("Default"),
                "CE" => CityEngineers.GetValueOrDefault("Default"),
                _ => null
            };
        }
    }

    /// <summary>
    /// Retry configuration for HSM operations
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Enable exponential backoff for retries
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }

    /// <summary>
    /// Signature coordinates for different document types
    /// </summary>
    public class SignatureCoordinates
    {
        /// <summary>
        /// Default coordinates for recommendation form
        /// Format: x,y,width,height
        /// </summary>
        public static string RecommendationForm => "117,383,236,324";

        /// <summary>
        /// Coordinates for certificate
        /// </summary>
        public static string Certificate => "369,275,488,216";

        /// <summary>
        /// Coordinates for approval letter
        /// </summary>
        public static string ApprovalLetter => "100,100,200,150";

        /// <summary>
        /// Get coordinates for specific document type
        /// </summary>
        public static string GetCoordinates(string documentType)
        {
            return documentType.ToLower() switch
            {
                "recommendation" => RecommendationForm,
                "certificate" => Certificate,
                "approval" => ApprovalLetter,
                _ => RecommendationForm // Default
            };
        }
    }
}
