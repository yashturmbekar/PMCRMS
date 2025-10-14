namespace PMCRMS.API.DTOs
{
    public class ChallanGenerationRequest
    {
        public int ApplicationId { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public class ChallanGenerationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ChallanPath { get; set; }
        public string? ChallanNumber { get; set; }
        public byte[]? PdfContent { get; set; }
    }
}
