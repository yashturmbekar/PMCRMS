using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using PMCRMS.API.Models;

namespace PMCRMS.API.DTOs
{
    // Request DTOs
    public class PositionRegistrationRequestDTO
    {
        [Required(ErrorMessage = "Position type is required")]
        public PositionType PositionType { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters and spaces")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Middle name can only contain letters and spaces")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name can only contain letters and spaces")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mother's name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Mother's name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Mother's name can only contain letters and spaces")]
        public string MotherName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Blood group cannot exceed 10 characters")]
        [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Please enter a valid blood group (e.g., A+, B-, O+)")]
        public string? BloodGroup { get; set; }

        [Range(0, 300, ErrorMessage = "Height must be between 0 and 300 cm")]
        public decimal? Height { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "PAN card number is required")]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Please enter a valid PAN card number (e.g., ABCDE1234F)")]
        public string PanCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aadhar card number is required")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Please enter a valid 12-digit Aadhar card number")]
        public string AadharCardNumber { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "COA card number cannot exceed 50 characters")]
        public string? CoaCardNumber { get; set; }

        // Addresses
        [Required(ErrorMessage = "Local address is required")]
        public AddressDTO LocalAddress { get; set; } = new AddressDTO();

        [Required(ErrorMessage = "Permanent address is required")]
        public AddressDTO PermanentAddress { get; set; } = new AddressDTO();

        // Qualifications
        [Required(ErrorMessage = "At least one qualification is required")]
        [MinLength(1, ErrorMessage = "At least one qualification is required")]
        public List<QualificationDTO> Qualifications { get; set; } = new List<QualificationDTO>();

        // Experiences
        public List<ExperienceDTO> Experiences { get; set; } = new List<ExperienceDTO>();

        // Documents
        public List<DocumentUploadDTO> Documents { get; set; } = new List<DocumentUploadDTO>();

        // Status
        public ApplicationCurrentStatus Status { get; set; } = ApplicationCurrentStatus.Draft;
    }

    public class AddressDTO
    {
        [Required(ErrorMessage = "Address line 1 is required", AllowEmptyStrings = false)]
        [StringLength(500, ErrorMessage = "Address line 1 cannot exceed 500 characters")]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address line 2 cannot exceed 500 characters")]
        public string? AddressLine2 { get; set; }

        [StringLength(500, ErrorMessage = "Address line 3 cannot exceed 500 characters")]
        public string? AddressLine3 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pin code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Please enter a valid 6-digit pin code")]
        public string PinCode { get; set; } = string.Empty;
    }

    public class QualificationDTO
    {
        [Required(ErrorMessage = "File ID is required")]
        public string FileId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Institute name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Institute name must be between 2 and 200 characters")]
        public string InstituteName { get; set; } = string.Empty;

        [Required(ErrorMessage = "University name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "University name must be between 2 and 200 characters")]
        public string UniversityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required")]
        public Specialization Specialization { get; set; }

        [Required(ErrorMessage = "Degree name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Degree name must be between 2 and 200 characters")]
        public string DegreeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passing month is required")]
        [Range(1, 12, ErrorMessage = "Passing month must be between 1 and 12")]
        public int PassingMonth { get; set; }

        [Required(ErrorMessage = "Year of passing is required")]
        [Range(1950, 2100, ErrorMessage = "Please enter a valid year")]
        public int YearOfPassing { get; set; }
    }

    public class ExperienceDTO
    {
        [Required(ErrorMessage = "File ID is required")]
        public string FileId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 200 characters")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "From date is required")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To date is required")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
    }

    public class DocumentUploadDTO
    {
        [Required(ErrorMessage = "File ID is required")]
        public string FileId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Document type is required")]
        public SEDocumentType DocumentType { get; set; }

        [Required(ErrorMessage = "File name is required")]
        public string FileName { get; set; } = string.Empty;

        // Base64 encoded file content (for binary storage in database)
        [Required(ErrorMessage = "File content is required")]
        public string FileBase64 { get; set; } = string.Empty;

        public decimal? FileSize { get; set; }

        public string? ContentType { get; set; }
        
        // Deprecated - keeping for backward compatibility
        [Obsolete("Use FileBase64 instead. FilePath is no longer used.")]
        public string? FilePath { get; set; }
    }

    // Response DTOs
    public class PositionRegistrationResponseDTO
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public PositionType PositionType { get; set; }
        public string PositionTypeName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string? BloodGroup { get; set; }
        public decimal? Height { get; set; }
        public Gender Gender { get; set; }
        public string GenderName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string PanCardNumber { get; set; } = string.Empty;
        public string AadharCardNumber { get; set; } = string.Empty;
        public string? CoaCardNumber { get; set; }
        public ApplicationCurrentStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        
        // Payment Information
        public bool IsPaymentComplete { get; set; }
        public DateTime? PaymentCompletedDate { get; set; }
        
        // Assigned Officer Information
        public int? AssignedJuniorEngineerId { get; set; }
        public string? AssignedJuniorEngineerName { get; set; }

        public List<AddressResponseDTO> Addresses { get; set; } = new List<AddressResponseDTO>();
        public List<QualificationResponseDTO> Qualifications { get; set; } = new List<QualificationResponseDTO>();
        public List<ExperienceResponseDTO> Experiences { get; set; } = new List<ExperienceResponseDTO>();
        public List<DocumentResponseDTO> Documents { get; set; } = new List<DocumentResponseDTO>();
        
        // Recommendation Form PDF (system-generated, stored as binary data)
        public RecommendationFormDTO? RecommendationForm { get; set; }
        
        // JE Workflow Information
        public JEWorkflowInfo? WorkflowInfo { get; set; }
    }
    
    /// <summary>
    /// Recommendation Form PDF data
    /// </summary>
    public class RecommendationFormDTO
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public decimal FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string PdfBase64 { get; set; } = string.Empty; // Base64 encoded PDF data
        public DateTime CreatedDate { get; set; }
    }
    
    /// <summary>
    /// Workflow information for JE stage applications
    /// </summary>
    public class JEWorkflowInfo
    {
        public int? AssignedJuniorEngineerId { get; set; }
        public string? AssignedJuniorEngineerName { get; set; }
        public string? AssignedJuniorEngineerEmail { get; set; }
        public DateTime? AssignedDate { get; set; }
        public int ProgressPercentage { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
        public bool HasAppointment { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? AppointmentPlace { get; set; }
        public string? AppointmentRoomNumber { get; set; }
        public string? AppointmentContactPerson { get; set; }
        public string? AppointmentComments { get; set; }
        public bool AllDocumentsVerified { get; set; }
        public int VerifiedDocumentsCount { get; set; }
        public int TotalDocumentsCount { get; set; }
        public bool HasDigitalSignature { get; set; }
        public DateTime? SignatureCompletedDate { get; set; }
        public List<WorkflowTimelineEvent> Timeline { get; set; } = new List<WorkflowTimelineEvent>();
    }
    
    /// <summary>
    /// Timeline event for workflow history
    /// </summary>
    public class WorkflowTimelineEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? PerformedBy { get; set; }
    }

    public class AddressResponseDTO
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
        public string FullAddress { get; set; } = string.Empty;
    }

    public class QualificationResponseDTO
    {
        public int Id { get; set; }
        public string FileId { get; set; } = string.Empty;
        public string InstituteName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public Specialization Specialization { get; set; }
        public string SpecializationName { get; set; } = string.Empty;
        public string DegreeName { get; set; } = string.Empty;
        public int PassingMonth { get; set; }
        public string PassingMonthName { get; set; } = string.Empty;
        public int YearOfPassing { get; set; }
    }

    public class ExperienceResponseDTO
    {
        public int Id { get; set; }
        public string FileId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal YearsOfExperience { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class DocumentResponseDTO
    {
        public int Id { get; set; }
        public string FileId { get; set; } = string.Empty;
        public SEDocumentType DocumentType { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; } // Deprecated - keeping for backward compatibility
        public decimal? FileSize { get; set; }
        public string? ContentType { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public string? VerificationRemarks { get; set; }
        public string? FileBase64 { get; set; } // Base64 encoded file content from database
    }

    // Custom Validation Attributes
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Applicant must be at least {_minimumAge} years old";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < _minimumAge)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }

    public class DateNotInFutureAttribute : ValidationAttribute
    {
        public DateNotInFutureAttribute()
        {
            ErrorMessage = "Date cannot be in the future";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date.Date > DateTime.Today)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
