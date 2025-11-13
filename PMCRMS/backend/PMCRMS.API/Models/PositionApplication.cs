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
        AdditionalDocument = 11,
        RecommendedForm = 12,
        PaymentChallan = 13,
        LicenceCertificate = 14,
        UGCRecognition = 15,
        AICTEApproval = 16,
        ITICertificate = 17,
        DiplomaCertificate = 18
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

        // Junior Engineer Workflow Properties
        /// <summary>
        /// Junior Engineer assigned to this application
        /// </summary>
        public int? AssignedJuniorEngineerId { get; set; }

        /// <summary>
        /// Date when application was assigned to Junior Engineer
        /// </summary>
        public DateTime? AssignedToJEDate { get; set; }

        /// <summary>
        /// Whether all documents have been verified by JE
        /// </summary>
        public bool JEAllDocumentsVerified { get; set; } = false;

        /// <summary>
        /// Date when all documents were verified by JE
        /// </summary>
        public DateTime? JEDocumentVerificationDate { get; set; }

        /// <summary>
        /// Whether digital signature has been applied by JE
        /// </summary>
        public bool JEDigitalSignatureApplied { get; set; } = false;

        /// <summary>
        /// Date when digital signature was applied by JE
        /// </summary>
        public DateTime? JEDigitalSignatureDate { get; set; }

        /// <summary>
        /// Whether appointment has been scheduled by JE
        /// </summary>
        public bool JEAppointmentScheduled { get; set; } = false;

        /// <summary>
        /// Date when appointment was scheduled by JE
        /// </summary>
        public DateTime? JEAppointmentScheduledDate { get; set; }

        /// <summary>
        /// Whether recommendation form has been successfully created and saved
        /// </summary>
        public bool IsRecommendationFormGenerated { get; set; } = false;

        /// <summary>
        /// Date when recommendation form was successfully generated
        /// </summary>
        public DateTime? RecommendationFormGeneratedDate { get; set; }

        /// <summary>
        /// Number of attempts made to generate recommendation form
        /// </summary>
        public int RecommendationFormGenerationAttempts { get; set; } = 0;

        /// <summary>
        /// Last error encountered during recommendation form generation
        /// </summary>
        [MaxLength(2000)]
        public string? RecommendationFormGenerationError { get; set; }

        /// <summary>
        /// Current workflow stage comments from JE
        /// </summary>
        [MaxLength(2000)]
        public string? JEComments { get; set; }

        // ==================== JUNIOR ENGINEER APPROVAL/REJECTION ====================
        public bool? JEApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? JEApprovalComments { get; set; }
        public DateTime? JEApprovalDate { get; set; }
        public bool? JERejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? JERejectionComments { get; set; }
        public DateTime? JERejectionDate { get; set; }

        // ==================== ASSISTANT ENGINEER - ARCHITECT ====================
        public int? AssignedAEArchitectId { get; set; }
        public DateTime? AssignedToAEArchitectDate { get; set; }
        public bool? AEArchitectApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? AEArchitectApprovalComments { get; set; }
        public DateTime? AEArchitectApprovalDate { get; set; }
        public bool? AEArchitectRejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? AEArchitectRejectionComments { get; set; }
        public DateTime? AEArchitectRejectionDate { get; set; }
        public bool AEArchitectDigitalSignatureApplied { get; set; } = false;
        public DateTime? AEArchitectDigitalSignatureDate { get; set; }

        // ==================== ASSISTANT ENGINEER - STRUCTURAL ====================
        public int? AssignedAEStructuralId { get; set; }
        public DateTime? AssignedToAEStructuralDate { get; set; }
        public bool? AEStructuralApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? AEStructuralApprovalComments { get; set; }
        public DateTime? AEStructuralApprovalDate { get; set; }
        public bool? AEStructuralRejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? AEStructuralRejectionComments { get; set; }
        public DateTime? AEStructuralRejectionDate { get; set; }
        public bool AEStructuralDigitalSignatureApplied { get; set; } = false;
        public DateTime? AEStructuralDigitalSignatureDate { get; set; }

        // ==================== ASSISTANT ENGINEER - LICENCE ====================
        public int? AssignedAELicenceId { get; set; }
        public DateTime? AssignedToAELicenceDate { get; set; }
        public bool? AELicenceApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? AELicenceApprovalComments { get; set; }
        public DateTime? AELicenceApprovalDate { get; set; }
        public bool? AELicenceRejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? AELicenceRejectionComments { get; set; }
        public DateTime? AELicenceRejectionDate { get; set; }
        public bool AELicenceDigitalSignatureApplied { get; set; } = false;
        public DateTime? AELicenceDigitalSignatureDate { get; set; }

        // ==================== ASSISTANT ENGINEER - SUPERVISOR 1 ====================
        public int? AssignedAESupervisor1Id { get; set; }
        public DateTime? AssignedToAESupervisor1Date { get; set; }
        public bool? AESupervisor1ApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? AESupervisor1ApprovalComments { get; set; }
        public DateTime? AESupervisor1ApprovalDate { get; set; }
        public bool? AESupervisor1RejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? AESupervisor1RejectionComments { get; set; }
        public DateTime? AESupervisor1RejectionDate { get; set; }
        public bool AESupervisor1DigitalSignatureApplied { get; set; } = false;
        public DateTime? AESupervisor1DigitalSignatureDate { get; set; }

        // ==================== ASSISTANT ENGINEER - SUPERVISOR 2 ====================
        public int? AssignedAESupervisor2Id { get; set; }
        public DateTime? AssignedToAESupervisor2Date { get; set; }
        public bool? AESupervisor2ApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? AESupervisor2ApprovalComments { get; set; }
        public DateTime? AESupervisor2ApprovalDate { get; set; }
        public bool? AESupervisor2RejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? AESupervisor2RejectionComments { get; set; }
        public DateTime? AESupervisor2RejectionDate { get; set; }
        public bool AESupervisor2DigitalSignatureApplied { get; set; } = false;
        public DateTime? AESupervisor2DigitalSignatureDate { get; set; }

        // ==================== EXECUTIVE ENGINEER ====================
        public int? AssignedExecutiveEngineerId { get; set; }
        public DateTime? AssignedToExecutiveEngineerDate { get; set; }
        public bool? ExecutiveEngineerApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? ExecutiveEngineerApprovalComments { get; set; }
        public DateTime? ExecutiveEngineerApprovalDate { get; set; }
        public bool? ExecutiveEngineerRejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? ExecutiveEngineerRejectionComments { get; set; }
        public DateTime? ExecutiveEngineerRejectionDate { get; set; }
        public bool ExecutiveEngineerDigitalSignatureApplied { get; set; } = false;
        public DateTime? ExecutiveEngineerDigitalSignatureDate { get; set; }

        // ==================== CITY ENGINEER ====================
        public int? AssignedCityEngineerId { get; set; }
        public DateTime? AssignedToCityEngineerDate { get; set; }
        public bool? CityEngineerApprovalStatus { get; set; }
        [MaxLength(2000)]
        public string? CityEngineerApprovalComments { get; set; }
        public DateTime? CityEngineerApprovalDate { get; set; }
        public bool? CityEngineerRejectionStatus { get; set; }
        [MaxLength(2000)]
        public string? CityEngineerRejectionComments { get; set; }
        public DateTime? CityEngineerRejectionDate { get; set; }
        public bool CityEngineerDigitalSignatureApplied { get; set; } = false;
        public DateTime? CityEngineerDigitalSignatureDate { get; set; }

        // ==================== CLERK (Post-Payment Processing) ====================
        /// <summary>
        /// Clerk assigned to process application after payment
        /// </summary>
        public int? AssignedClerkId { get; set; }
        
        /// <summary>
        /// Date when application was assigned to Clerk
        /// </summary>
        public DateTime? AssignedToClerkDate { get; set; }
        
        /// <summary>
        /// Clerk approval status (true = approved, false/null = pending)
        /// </summary>
        public bool? ClerkApprovalStatus { get; set; }
        
        /// <summary>
        /// Comments provided by Clerk during approval
        /// </summary>
        [MaxLength(2000)]
        public string? ClerkApprovalComments { get; set; }
        
        /// <summary>
        /// Date when Clerk approved the application
        /// </summary>
        public DateTime? ClerkApprovalDate { get; set; }
        
        /// <summary>
        /// Clerk rejection status (true = rejected)
        /// </summary>
        public bool? ClerkRejectionStatus { get; set; }
        
        /// <summary>
        /// Rejection reason provided by Clerk
        /// </summary>
        [MaxLength(2000)]
        public string? ClerkRejectionComments { get; set; }
        
        /// <summary>
        /// Date when Clerk rejected the application
        /// </summary>
        public DateTime? ClerkRejectionDate { get; set; }

        // ==================== EXECUTIVE ENGINEER STAGE 2 (Certificate Digital Signature) ====================
        /// <summary>
        /// Executive Engineer assigned for certificate digital signature (Stage 2)
        /// </summary>
        public int? AssignedEEStage2Id { get; set; }
        
        /// <summary>
        /// Date when assigned to EE Stage 2 for signature
        /// </summary>
        public DateTime? AssignedToEEStage2Date { get; set; }
        
        /// <summary>
        /// EE Stage 2 approval status (true = approved, false/null = pending)
        /// </summary>
        public bool? EEStage2ApprovalStatus { get; set; }
        
        /// <summary>
        /// Comments provided by EE Stage 2 during approval
        /// </summary>
        [MaxLength(2000)]
        public string? EEStage2ApprovalComments { get; set; }
        
        /// <summary>
        /// Date when EE Stage 2 approved the application
        /// </summary>
        public DateTime? EEStage2ApprovalDate { get; set; }
        
        /// <summary>
        /// EE Stage 2 digital signature applied status
        /// </summary>
        public bool EEStage2DigitalSignatureApplied { get; set; } = false;
        
        /// <summary>
        /// Date when EE Stage 2 applied digital signature
        /// </summary>
        public DateTime? EEStage2DigitalSignatureDate { get; set; }

        // ==================== CITY ENGINEER STAGE 2 (Final Certificate Signature) ====================
        /// <summary>
        /// City Engineer assigned for final certificate signature (Stage 2)
        /// </summary>
        public int? AssignedCEStage2Id { get; set; }
        
        /// <summary>
        /// Date when assigned to CE Stage 2 for final signature
        /// </summary>
        public DateTime? AssignedToCEStage2Date { get; set; }
        
        /// <summary>
        /// CE Stage 2 approval status (true = approved, false/null = pending)
        /// </summary>
        public bool? CEStage2ApprovalStatus { get; set; }
        
        /// <summary>
        /// Comments provided by CE Stage 2 during approval
        /// </summary>
        [MaxLength(2000)]
        public string? CEStage2ApprovalComments { get; set; }
        
        /// <summary>
        /// Date when CE Stage 2 approved the application
        /// </summary>
        public DateTime? CEStage2ApprovalDate { get; set; }
        
        /// <summary>
        /// CE Stage 2 digital signature applied status
        /// </summary>
        public bool CEStage2DigitalSignatureApplied { get; set; } = false;
        
        /// <summary>
        /// Date when CE Stage 2 applied final digital signature
        /// </summary>
        public DateTime? CEStage2DigitalSignatureDate { get; set; }

        // Foreign Keys
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("AssignedJuniorEngineerId")]
        public virtual Officer? AssignedJuniorEngineer { get; set; }

        [ForeignKey("AssignedAEArchitectId")]
        public virtual Officer? AssignedAEArchitect { get; set; }

        [ForeignKey("AssignedAEStructuralId")]
        public virtual Officer? AssignedAEStructural { get; set; }

        [ForeignKey("AssignedAELicenceId")]
        public virtual Officer? AssignedAELicence { get; set; }

        [ForeignKey("AssignedAESupervisor1Id")]
        public virtual Officer? AssignedAESupervisor1 { get; set; }

        [ForeignKey("AssignedAESupervisor2Id")]
        public virtual Officer? AssignedAESupervisor2 { get; set; }

        [ForeignKey("AssignedExecutiveEngineerId")]
        public virtual Officer? AssignedExecutiveEngineer { get; set; }

        [ForeignKey("AssignedCityEngineerId")]
        public virtual Officer? AssignedCityEngineer { get; set; }

        [ForeignKey("AssignedClerkId")]
        public virtual Officer? AssignedClerk { get; set; }

        [ForeignKey("AssignedEEStage2Id")]
        public virtual Officer? AssignedEEStage2 { get; set; }

        [ForeignKey("AssignedCEStage2Id")]
        public virtual Officer? AssignedCEStage2 { get; set; }

        // Navigation properties
        public virtual ICollection<SEAddress> Addresses { get; set; } = new List<SEAddress>();
        public virtual ICollection<SEQualification> Qualifications { get; set; } = new List<SEQualification>();
        public virtual ICollection<SEExperience> Experiences { get; set; } = new List<SEExperience>();
        public virtual ICollection<SEDocument> Documents { get; set; } = new List<SEDocument>();

        // Junior Engineer Workflow Collections
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<DocumentVerification> DocumentVerifications { get; set; } = new List<DocumentVerification>();
        public virtual ICollection<DigitalSignature> DigitalSignatures { get; set; } = new List<DigitalSignature>();
        public virtual ICollection<AssignmentHistory> AssignmentHistories { get; set; } = new List<AssignmentHistory>();
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

        [MaxLength(500)]
        public string? FilePath { get; set; } = string.Empty;

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

        // Store PDF content directly in database for RecommendedForm documents
        public byte[]? FileContent { get; set; }

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
