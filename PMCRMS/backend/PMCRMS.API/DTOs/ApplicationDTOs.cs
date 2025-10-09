namespace PMCRMS.API.DTOs
{
    public class CreateApplicationRequest
    {
        public string ApplicationType { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string SiteAddress { get; set; } = string.Empty;
        public decimal PlotArea { get; set; }
        public decimal BuiltUpArea { get; set; }
        public decimal? EstimatedCost { get; set; }
    }

    public class UpdateApplicationRequest
    {
        public string? ProjectTitle { get; set; }
        public string? ProjectDescription { get; set; }
        public string? SiteAddress { get; set; }
        public decimal? PlotArea { get; set; }
        public decimal? BuiltUpArea { get; set; }
        public decimal? EstimatedCost { get; set; }
    }

    public class AddCommentRequest
    {
        public string Comment { get; set; } = string.Empty;
        public string CommentType { get; set; } = "General";
        public bool IsInternal { get; set; } = false;
    }

    public class ApplicationDto
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public int ApplicantId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string SiteAddress { get; set; } = string.Empty;
        public decimal PlotArea { get; set; }
        public decimal BuiltUpArea { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CertificateIssuedDate { get; set; }
        public string? CertificateNumber { get; set; }
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
        public List<StatusHistoryDto> StatusHistory { get; set; } = new List<StatusHistoryDto>();
    }

    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CommentType { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class StatusHistoryDto
    {
        public string Status { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}