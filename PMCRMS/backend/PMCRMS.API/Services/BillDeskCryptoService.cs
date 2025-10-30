using System.Text;
using Jose;
using Newtonsoft.Json;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// BillDesk JWT/JWE Encryption and Signing Service
    /// Implements BillDesk payment gateway security requirements
    /// Uses JOSE (JSON Object Signing and Encryption) standards with Jose.JWT library
    /// PROVEN WORKING - Uses JWE (encryption) + JWS (signing) double-layer security
    /// </summary>
    public interface IBillDeskCryptoService
    {
        string CreateJWE(object payload, string merchantId, string keyId);
        string ParseJWE(string jweToken);
    }

    public class BillDeskCryptoService : IBillDeskCryptoService
    {
        private readonly IBillDeskConfigService _configService;
        private readonly ILogger<BillDeskCryptoService> _logger;

        public BillDeskCryptoService(
            IBillDeskConfigService configService,
            ILogger<BillDeskCryptoService> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Create JWE (JSON Web Encryption) + JWS (JSON Web Signature) token for BillDesk
        /// This is the PROVEN WORKING implementation:
        /// 1. Encrypt payload using JWE (A256GCM with direct key)
        /// 2. Sign the JWE using JWS (HS256)
        /// Result: Signed encrypted token that BillDesk UAT accepts
        /// </summary>
        public string CreateJWE(object payload, string merchantId, string keyId)
        {
            try
            {
                _logger.LogInformation("[CRYPTO] Creating JWE token");

                // Step 1: Serialize payload to JSON
                string jsonPayload = JsonConvert.SerializeObject(payload);
                _logger.LogInformation($"[CRYPTO] Payload serialized, length: {jsonPayload.Length}");

                // Step 2: Get encryption and signing keys
                string encryptionKey = _configService.EncryptionKey;
                string signingKey = _configService.SigningKey;
                string clientId = _configService.ClientId;

                byte[] aesKey = Encoding.UTF8.GetBytes(encryptionKey);
                
                // Validate AES key length (must be 32 bytes for A256GCM)
                if (aesKey.Length != 32)
                {
                    _logger.LogError($"[CRYPTO] Encryption key length is {aesKey.Length} bytes, expected 32");
                    throw new Exception($"Encryption key must be exactly 32 bytes for A256GCM. Current length: {aesKey.Length}");
                }

                // Step 3: Create JWE header with kid and clientid
                var jweHeader = new Dictionary<string, object>
                {
                    { "kid", keyId },
                    { "clientid", clientId }
                };

                _logger.LogInformation("[CRYPTO] JWE header configured");

                // Step 4: Encrypt payload using JWE (A256GCM with direct key algorithm)
                string encryptedPayload = JWT.Encode(
                    jsonPayload,
                    aesKey,
                    JweAlgorithm.DIR,           // Direct encryption - key used directly
                    JweEncryption.A256GCM,      // AES-256 GCM encryption
                    extraHeaders: jweHeader
                );

                _logger.LogInformation($"[CRYPTO] JWE encryption completed, length: {encryptedPayload.Length}");

                // Step 5: Sign the encrypted payload using JWS (HS256)
                byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
                
                string signedToken = JWT.Encode(
                    encryptedPayload,
                    signingKeyBytes,
                    JwsAlgorithm.HS256,         // HMAC SHA-256 signing
                    extraHeaders: jweHeader     // Include same headers in JWS
                );

                _logger.LogInformation($"[CRYPTO] JWS signing completed, final token length: {signedToken.Length}");
                
                return signedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CRYPTO] Error creating JWE/JWS token");
                throw new Exception($"JWE/JWS creation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse and decrypt JWE+JWS token from BillDesk response
        /// This reverses the double-layer encryption:
        /// 1. Verify and decode JWS signature
        /// 2. Decrypt JWE to get original JSON
        /// </summary>
        public string ParseJWE(string jweToken)
        {
            try
            {
                _logger.LogInformation($"[CRYPTO] Parsing JWE/JWS token, length: {jweToken?.Length ?? 0}");

                if (string.IsNullOrWhiteSpace(jweToken))
                {
                    throw new ArgumentException("JWE/JWS token is empty");
                }

                string encryptionKey = _configService.EncryptionKey;
                string signingKey = _configService.SigningKey;

                byte[] aesKey = Encoding.UTF8.GetBytes(encryptionKey);
                byte[] jwsKey = Encoding.UTF8.GetBytes(signingKey);

                // Step 1: Decode JWS (verify signature and get encrypted payload)
                string jwsDecoded = JWT.Decode(jweToken, jwsKey);
                _logger.LogInformation($"[CRYPTO] JWS signature verified");

                // Step 2: Decrypt JWE (get original JSON payload)
                string decryptedPayload = JWT.Decode(
                    jwsDecoded,
                    aesKey,
                    JweAlgorithm.DIR,
                    JweEncryption.A256GCM
                );

                _logger.LogInformation($"[CRYPTO] JWE decryption completed");

                // Step 3: Format JSON for readability
                var parsedJson = Newtonsoft.Json.Linq.JToken.Parse(decryptedPayload);
                string formattedJson = parsedJson.ToString(Formatting.Indented);

                return formattedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CRYPTO] Error parsing JWE/JWS token");
                throw new Exception($"JWE/JWS decryption failed: {ex.Message}", ex);
            }
        }
    }
}
