namespace PMCRMS.API.ViewModels
{
    public class PdfGenerationRequest
    {
        public int ApplicationId { get; set; }
    }

    public class PdfGenerationResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public byte[]? FileContent { get; set; }
        public string? FileName { get; set; }
    }

    public class ApplicationPdfModel
    {
        public string Name { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<string> Qualification { get; set; } = new List<string>();
        public string MobileNumber { get; set; } = string.Empty;
        public string? MonthDifference { get; set; }
        public string? YearDifference { get; set; }
        public bool IsBothAddressSame { get; set; }
        public string? JrEnggName { get; set; }
        public string? AssEnggName { get; set; }
        public string? ExeEnggName { get; set; }
        public string? CityEnggName { get; set; }
    }
}
