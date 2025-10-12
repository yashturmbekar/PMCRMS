namespace PMCRMS.API.Services
{
    /// <summary>
    /// Plugin context service for handling BillDesk encryption/decryption and other plugin operations
    /// </summary>
    public interface IPluginContextService
    {
        /// <summary>
        /// Get application entity fields by ID
        /// </summary>
        Task<dynamic> GetEntityFieldsById(string entityId);

        /// <summary>
        /// Generate a random number string of specified length
        /// </summary>
        string RandomNumber(int length);

        /// <summary>
        /// Create a new entity in the system
        /// </summary>
        Task<string> CreateEntity(string entityType, string parentId, dynamic entity);

        /// <summary>
        /// Invoke a plugin service (BILLDESK, HTTPPayment, Challan)
        /// </summary>
        Task<dynamic> Invoke(string serviceName, dynamic input);
    }
}
