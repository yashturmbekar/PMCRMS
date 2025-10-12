namespace PMCRMS.API.Services
{
    /// <summary>
    /// Configuration service for BillDesk payment gateway integration
    /// </summary>
    public interface IBillDeskConfigService
    {
        /// <summary>
        /// BillDesk Merchant ID provided by BillDesk
        /// </summary>
        string MerchantId { get; }

        /// <summary>
        /// Encryption key for securing payment data
        /// </summary>
        string EncryptionKey { get; }

        /// <summary>
        /// Signing key for HMAC signature generation
        /// </summary>
        string SigningKey { get; }

        /// <summary>
        /// Key ID for identification in BillDesk system
        /// </summary>
        string KeyId { get; }

        /// <summary>
        /// Client ID for API authentication
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// BillDesk payment gateway URL
        /// </summary>
        string PaymentGatewayUrl { get; }

        /// <summary>
        /// Base URL for payment callback/return URL
        /// </summary>
        string ReturnUrlBase { get; }
    }
}
