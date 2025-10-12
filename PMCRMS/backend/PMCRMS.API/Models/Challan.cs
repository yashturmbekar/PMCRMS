namespace PMCRMS.API.Models
{
    public class Challan
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public DateTime ChallanDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public bool IsGenerated { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        // Navigation property
        public PositionApplication? PositionApplication { get; set; }
    }
}
