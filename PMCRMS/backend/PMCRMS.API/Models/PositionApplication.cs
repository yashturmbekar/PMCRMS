using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMCRMS.API.Models
{
    public enum PositionType
    {
        Architect = 0,
        LicenceEngineer = 1,
        StructuralEngineer = 2,
        Supervisor1 = 3,
        Supervisor2 = 4
    }

    public enum Gender
    {
        Male = 0,
        Female = 1,
        Other = 2
    }

    public enum Specialization
    {
        Diploma = 0,
        BE = 1,
        ME = 2,
        PhD = 3
    }

    public enum SEDocumentType
    {
        AddressProof = 0,
        PanCard = 1,
        AadharCard = 2,
        DegreeCertificate = 3,
        Marksheet = 4,
        ExperienceCertificate = 5,
        ISSECertificate = 6,
        PropertyTaxReceipt = 7,
        ProfilePicture = 8,
        SelfDeclaration = 9,
        COACertificate = 10,
        AdditionalDocument = 11
    }

    public class PositionApplication : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string MotherName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        public PositionType PositionType { get; set; }

        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Height { get; set; }

        public Gender Gender { get; set; }

        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(10)]
        public string PanCardNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(12)]
        public string AadharCardNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CoaCardNumber { get; set; }

        public int UserId { get; set; }

        [MaxLength(20)]
        public string? ApplicationNumber { get; set; }

        public ApplicationCurrentStatus Status { get; set; } = ApplicationCurrentStatus.Draft;

        public DateTime? SubmittedDate { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        // Foreign Keys
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<SEAddress> Addresses { get; set; } = new List<SEAddress>();
        public virtual ICollection<SEQualification> Qualifications { get; set; } = new List<SEQualification>();
        public virtual ICollection<SEExperience> Experiences { get; set; } = new List<SEExperience>();
        public virtual ICollection<SEDocument> Documents { get; set; } = new List<SEDocument>();
    }

    public class SEAddress : BaseEntity
    {
        public int ApplicationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string AddressType { get; set; } = string.Empty; // "Current" or "Permanent"

        [Required]
        [MaxLength(500)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AddressLine2 { get; set; }

        [MaxLength(500)]
        public string? AddressLine3 { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PinCode { get; set; } = string.Empty;

        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;
    }

    public class SEQualification : BaseEntity
    {
        public int ApplicationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FileId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string InstituteName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string UniversityName { get; set; } = string.Empty;

        public Specialization Specialization { get; set; }

        [Required]
        [MaxLength(200)]
        public string DegreeName { get; set; } = string.Empty;

        [Range(1, 12)]
        public int PassingMonth { get; set; }

        public DateTime YearOfPassing { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;
    }

    public class SEExperience : BaseEntity
    {
        public int ApplicationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FileId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal YearsOfExperience { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;
    }

    public class SEDocument : BaseEntity
    {
        public int ApplicationId { get; set; }

        public SEDocumentType DocumentType { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? FileSize { get; set; }

        [MaxLength(50)]
        public string? ContentType { get; set; }

        public bool IsVerified { get; set; } = false;

        public int? VerifiedBy { get; set; }

        public DateTime? VerifiedDate { get; set; }

        [MaxLength(500)]
        public string? VerificationRemarks { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual PositionApplication Application { get; set; } = null!;

        [ForeignKey("VerifiedBy")]
        public virtual Officer? VerifiedByOfficer { get; set; }
    }
}
