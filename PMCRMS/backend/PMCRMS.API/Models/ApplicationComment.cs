using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public class ApplicationComment : BaseEntity
    {
        [Required]
        public int ApplicationId { get; set; }
        
        [Required]
        public int CommentedBy { get; set; }
        
        [Required]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? CommentType { get; set; }
        
        public bool IsInternal { get; set; } = false;
        
        public bool IsVisible { get; set; } = true;
        
        public int? ParentCommentId { get; set; }
        
        // Foreign Keys
        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; } = null!;
        
        [ForeignKey("CommentedBy")]
        public virtual Officer CommentedByOfficer { get; set; } = null!;
        
        [ForeignKey("ParentCommentId")]
        public virtual ApplicationComment? ParentComment { get; set; }
        
        // Navigation properties
        public virtual ICollection<ApplicationComment> Replies { get; set; } = new List<ApplicationComment>();
    }
}
