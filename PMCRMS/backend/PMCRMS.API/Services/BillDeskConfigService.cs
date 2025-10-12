namespace PMCRMS.API.Services
{
    /// <summary>
    /// Implementation of BillDesk configuration service
    /// Validates and provides BillDesk payment gateway configuration
    /// </summary>
    public class BillDeskConfigService : IBillDeskConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillDeskConfigService> _logger;

        public BillDeskConfigService(
            IConfiguration configuration,
            ILogger<BillDeskConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            ValidateConfiguration();
        }

        public string MerchantId => _configuration["BillDesk:MerchantId"] ?? string.Empty;
        public string EncryptionKey => _configuration["BillDesk:EncryptionKey"] ?? string.Empty;
        public string SigningKey => _configuration["BillDesk:SigningKey"] ?? string.Empty;
        public string KeyId => _configuration["BillDesk:KeyId"] ?? string.Empty;
        public string ClientId => _configuration["BillDesk:ClientId"] ?? string.Empty;
        public string PaymentGatewayUrl => _configuration["BillDesk:PaymentGatewayUrl"] ?? "https://pay.billdesk.com/web/v1_2/embeddedsdk";
        public string ReturnUrlBase => _configuration["BillDesk:ReturnUrlBase"] ?? string.Empty;

        /// <summary>
        /// Validates that all required BillDesk configuration values are present
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when required configuration is missing</exception>
        private void ValidateConfiguration()
        {
            var requiredKeys = new[]
            {
                "BillDesk:MerchantId",
                "BillDesk:EncryptionKey",
                "BillDesk:SigningKey",
                "BillDesk:KeyId",
                "BillDesk:ClientId",
                "BillDesk:PaymentGatewayUrl",
                "BillDesk:ReturnUrlBase"
            };

            var missingKeys = new List<string>();

            foreach (var key in requiredKeys)
            {
                if (string.IsNullOrEmpty(_configuration[key]))
                {
                    _logger.LogError($"Missing required BillDesk configuration: {key}");
                    missingKeys.Add(key);
                }
            }

            if (missingKeys.Any())
            {
                throw new InvalidOperationException(
                    $"Missing required BillDesk configuration keys: {string.Join(", ", missingKeys)}");
            }

            _logger.LogInformation("BillDesk configuration validated successfully");
            _logger.LogInformation($"Payment Gateway URL: {PaymentGatewayUrl}");
            _logger.LogInformation($"Merchant ID: {MerchantId}");
        }
    }
}
