namespace PMCRMS.API.Services
{
    /// <summary>
    /// Interface for Structural Engineer / Position Licence Certificate Generation Service
    /// </summary>
    public interface ISECertificateGenerationService
    {
        /// <summary>
        /// Generate licence certificate after payment completion
        /// Includes retry logic and stores PDF in SEDocuments table (database-only, no physical file)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Success status</returns>
        Task<bool> GenerateAndSaveLicenceCertificateAsync(int applicationId);

        /// <summary>
        /// Retrieve generated certificate from database
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Certificate PDF bytes or null if not found</returns>
        Task<byte[]?> GetCertificatePdfAsync(int applicationId);
    }
}
