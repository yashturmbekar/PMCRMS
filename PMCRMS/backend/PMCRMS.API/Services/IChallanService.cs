using PMCRMS.API.DTOs;

namespace PMCRMS.API.Services
{
    public interface IChallanService
    {
        Task<ChallanGenerationResponse> GenerateChallanAsync(ChallanGenerationRequest request);
        Task<byte[]?> GetChallanPdfAsync(int applicationId);
        Task<string?> GetChallanPathAsync(int applicationId);
        Task<bool> IsChallanGeneratedAsync(int applicationId);
    }
}
