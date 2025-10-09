using PMCRMS.API.Models;
using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.DTOs
{
    // Request DTOs
    public class CreateStructuralEngineerApplicationRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string MotherName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        public int PositionType { get; set; }

        public string? BloodGroup { get; set; }

        [Required]
        public decimal Height { get; set; }

        [Required]
        public int Gender { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public AddressDto PermanentAddress { get; set; } = new AddressDto();

        [Required]
        public AddressDto CurrentAddress { get; set; } = new AddressDto();

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PanCardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(12, MinimumLength = 12)]
        public string AadharCardNumber { get; set; } = string.Empty;

        public string? CoaCardNumber { get; set; }

        [Required]
        public List<QualificationDto> Qualifications { get; set; } = new List<QualificationDto>();

        [Required]
        public List<ExperienceDto> Experiences { get; set; } = new List<ExperienceDto>();

        [Required]
        public List<SEDocumentDto> Documents { get; set; } = new List<SEDocumentDto>();
    }

    public class AddressDto
    {
        [Required]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string? AddressLine3 { get; set; }

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        [Required]
        public string PinCode { get; set; } = string.Empty;
    }

    public class QualificationDto
    {
        [Required]
        public string FileId { get; set; } = string.Empty;

        [Required]
        public string InstituteName { get; set; } = string.Empty;

        [Required]
        public string UniversityName { get; set; } = string.Empty;

        [Required]
        public int Specialization { get; set; }

        [Required]
        public string DegreeName { get; set; } = string.Empty;

        [Required]
        [Range(1, 12)]
        public int PassingMonth { get; set; }

        [Required]
        public DateTime YearOfPassing { get; set; }
    }

    public class ExperienceDto
    {
        [Required]
        public string FileId { get; set; } = string.Empty;

        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        [Required]
        public decimal YearsOfExperience { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }
    }

    public class SEDocumentDto
    {
        [Required]
        public int DocumentType { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileId { get; set; } = string.Empty;

        public decimal? FileSize { get; set; }

        public string? ContentType { get; set; }
    }

    // Response DTOs
    public class StructuralEngineerApplicationResponse
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
        public string MotherName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PositionType { get; set; } = string.Empty;
        public string? BloodGroup { get; set; }
        public decimal Height { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PanCardNumber { get; set; } = string.Empty;
        public string AadharCardNumber { get; set; } = string.Empty;
        public string? CoaCardNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<AddressResponseDto> Addresses { get; set; } = new List<AddressResponseDto>();
        public List<QualificationResponseDto> Qualifications { get; set; } = new List<QualificationResponseDto>();
        public List<ExperienceResponseDto> Experiences { get; set; } = new List<ExperienceResponseDto>();
        public List<DocumentResponseDto> Documents { get; set; } = new List<DocumentResponseDto>();

        public decimal TotalExperience { get; set; }
    }

    public class AddressResponseDto
    {
        public int Id { get; set; }
        public string AddressType { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string FullAddress => $"{AddressLine1}, {AddressLine2}, {AddressLine3}, {City}, {State}, {Country} - {PinCode}".Replace(", ,", ",").Trim();
    }

    public class QualificationResponseDto
    {
        public int Id { get; set; }
        public string FileId { get; set; } = string.Empty;
        public string InstituteName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string DegreeName { get; set; } = string.Empty;
        public int PassingMonth { get; set; }
        public string PassingMonthName { get; set; } = string.Empty;
        public DateTime YearOfPassing { get; set; }
        public int PassingYear { get; set; }
    }

    public class ExperienceResponseDto
    {
        public int Id { get; set; }
        public string FileId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal YearsOfExperience { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Duration => $"{YearsOfExperience} years";
    }

    public class DocumentResponseDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public decimal? FileSize { get; set; }
        public string? ContentType { get; set; }
        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public string? VerificationRemarks { get; set; }
    }

    public class UpdateApplicationStatusRequest
    {
        [Required]
        public int Status { get; set; }

        public string? Remarks { get; set; }
    }
}
