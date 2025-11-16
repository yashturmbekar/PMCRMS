import React, { useState, useEffect } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, AlertCircle } from "lucide-react";
import positionRegistrationService, {
  type PositionRegistrationRequest,
  type Qualification as ApiQualification,
  type Experience as ApiExperience,
  type Address as ApiAddress,
} from "../services/positionRegistrationService";
import { PageLoader, FullScreenLoader } from "../components";

// Enums matching backend
const PositionType = {
  Architect: 0,
  LicenceEngineer: 1,
  StructuralEngineer: 2,
  Supervisor1: 3,
  Supervisor2: 4,
} as const;

// Position Configuration
const POSITION_CONFIG = {
  [PositionType.Architect]: {
    name: "Architect",
    icon: "🏛️",
    fee: 0, // No fee mentioned
    feeDuration: "3 years",
    sections: {
      basicInfo: true,
      personalDetails: true,
      localAddress: true,
      permanentAddress: true,
      panCard: true,
      aadharCard: true,
      qualifications: true,
      experience: false,
      coaCertificate: true, // Council of Architecture Certificate
      isseCertificate: false,
      ugcRecognition: false,
      aicteApproval: false,
      propertyTaxReceipt: true,
      additionalDocuments: true,
      selfDeclaration: true,
      profilePicture: true,
    },
    qualificationInfo: `A) The qualifications for Licensing Engineer will be the corporate membership (Civil) of the Institution of Engineers or such Degree or Diploma in Civil or Structural Engineering or equivalent.
B) Diploma in Civil Engineering or equivalent, having experience of 10 years in the field of land and building planning. (AS PER DCPR RULE C-3.1)`,
    scope: undefined,
    documentsRequired: [
      "Council of Architecture Certificate",
      "Degree Certificate",
      "Marksheet",
      "Address Proof (Aadhar Card)",
      "Identity Proof (PAN Card)",
      "Self Declaration Form",
      "Photo",
    ],
  },
  [PositionType.LicenceEngineer]: {
    name: "Licence Engineer",
    icon: "⚙️",
    fee: 3000,
    feeDuration: "3 years",
    sections: {
      basicInfo: true,
      personalDetails: true,
      localAddress: true,
      permanentAddress: true,
      panCard: true,
      aadharCard: true,
      qualifications: true,
      experience: true,
      coaCertificate: false,
      isseCertificate: false,
      ugcRecognition: true, // For out of Maharashtra degrees
      aicteApproval: true, // AICTE approval required
      propertyTaxReceipt: true,
      additionalDocuments: true,
      selfDeclaration: true,
      profilePicture: true,
    },
    experienceYears: 10,
    qualificationInfo: `A) The qualifications for Licensing Engineer will be the corporate membership (Civil) of the Institution of Engineers or such Degree or Diploma in Civil or Structural Engineering or equivalent.
B) Diploma in Civil Engineering or equivalent, having experience of 10 years in the field of land and building planning. (AS PER DCPR RULE C-3.1)`,
    scope: undefined,
    documentsRequired: [
      "Degree Certificate",
      "Marksheet",
      "Address Proof (Aadhar Card)",
      "Identity Proof (PAN Card)",
      "Self Declaration Form",
      "Experience Certificate (10 years in land and building planning)",
      "Degree out of Maharashtra - UGC Recognition",
      "AICTE Approval",
    ],
  },
  [PositionType.StructuralEngineer]: {
    name: "Structural Engineer",
    icon: "🏗️",
    fee: 1500,
    feeDuration: "3 years",
    sections: {
      basicInfo: true,
      personalDetails: true,
      localAddress: true,
      permanentAddress: true,
      panCard: true,
      aadharCard: true,
      qualifications: true,
      experience: true,
      coaCertificate: false,
      isseCertificate: true, // Indian Society of Structural Engineers
      ugcRecognition: true, // For degree recognition
      aicteApproval: true, // AICTE approval required
      propertyTaxReceipt: true,
      additionalDocuments: true,
      selfDeclaration: true,
      profilePicture: true,
    },
    experienceByDegree: {
      BE: 3,
      ME: 2,
      PhD: 1,
    },
    qualificationInfo: `Qualifications for Licensing of structural engineers shall be as given below, with minimum 3 years of experience of structural engineering practice in designing and field work;
(A) Graduate in Civil Engineering of recognized Indian or Foreign University and Chartered Engineer or Associated Member in Civil Engineering Division of Institution of Engineers (India) or equivalent Overseas Institution; or DCPR-2018 FOR PMRDA 159.
(B) Shall have a postgraduate degree in structural engineering. Three years' experience will be reduced to two years for those with Post Graduation in Structural engineering. In the case of a doctorate in structural engineering, the experience criteria are reduced to one year. (AS PER DCPR RULE C-4.1).`,
    scope: undefined,
    documentsRequired: [
      "Degree Certificate",
      "Marksheet",
      "I.S.S.E Certificate",
      "Experience Certificate - BE: 3 years, ME/M.Tech/M.S: 2 years, PhD: 1 year in structural designing",
      "Address Proof (Aadhar Card)",
      "Identity Proof (PAN Card)",
      "Degree from UGC recognized University",
      "AICTE Approval",
      "Self Declaration Form",
      "Photo",
    ],
  },
  [PositionType.Supervisor1]: {
    name: "Supervisor 1",
    icon: "👷",
    fee: 1500,
    feeDuration: "3 years",
    sections: {
      basicInfo: true,
      personalDetails: true,
      localAddress: true,
      permanentAddress: true,
      panCard: true,
      aadharCard: true,
      qualifications: true,
      experience: true,
      coaCertificate: false,
      isseCertificate: false,
      ugcRecognition: false,
      aicteApproval: false,
      propertyTaxReceipt: true,
      additionalDocuments: true,
      selfDeclaration: true,
      profilePicture: true,
    },
    experienceByQualification: {
      Diploma: 2,
      ITI: 10,
    },
    qualificationInfo: `(A) Three years architectural assistantship or intermediate in architecture with two years experience, or
(B) Diploma in Civil engineering or equivalent qualifications with two years experience.
(C) Draftsman in Civil Engineering from ITI or equivalent qualifications with ten years experience, out of which five years shall be under Architect/Engineer. (AS PER DCPR RULE C-5.1.a).`,
    scope: `(A) All plans and related information connected with development permission on a plot up to 500 sq.m.
(B) Certificate of supervision of buildings on a plot up to 500 sq. m. and completion thereof. (AS PER DCPR RULE C-5.2.a).`,
    documentsRequired: [
      "Diploma / I.T.I Certificate",
      "Marksheet",
      "Experience Certificate - Diploma in Civil/Equivalent: 2 years, ITI in Civil/Equivalent: 10 years (5 years under Architect/Engineer)",
      "Address Proof (Aadhar Card)",
      "Identity Proof (PAN Card)",
      "Self Declaration Form",
      "Photo",
    ],
  },
  [PositionType.Supervisor2]: {
    name: "Supervisor 2",
    icon: "👷‍♂️",
    fee: 900,
    feeDuration: "3 years",
    sections: {
      basicInfo: true,
      personalDetails: true,
      localAddress: true,
      permanentAddress: true,
      panCard: true,
      aadharCard: true,
      qualifications: true,
      experience: true,
      coaCertificate: false,
      isseCertificate: false,
      ugcRecognition: false,
      aicteApproval: false,
      propertyTaxReceipt: true,
      additionalDocuments: true,
      selfDeclaration: true,
      profilePicture: true,
    },
    experienceByQualification: {
      Diploma: 2,
      ITI: 10,
    },
    qualificationInfo: `(A) Three years architectural assistantship or intermediate in architecture with two years experience, or
(B) Diploma in Civil engineering or equivalent qualifications with two years experience.
(C) Draftsman in Civil Engineering from ITI or equivalent qualifications with ten years experience, out of which five years shall be under Architect/Engineer. (AS PER DCPR RULE C-5.1.a).`,
    scope: `(A) All plans and related information connected with development permission on a plot up to 500 sq.m.
(B) Certificate of supervision of buildings on a plot up to 500 sq. m. and completion thereof. (AS PER DCPR RULE C-5.2.a).`,
    documentsRequired: [
      "Diploma / I.T.I Certificate",
      "Marksheet",
      "Experience Certificate - Diploma in Civil/Equivalent: 2 years, ITI in Civil/Equivalent: 10 years (5 years under Architect/Engineer)",
      "Address Proof (Aadhar Card)",
      "Identity Proof (PAN Card)",
      "Self Declaration Form",
      "Photo",
    ],
  },
};

const Gender = {
  Male: 0,
  Female: 1,
  Other: 2,
} as const;

const Specialization = {
  Diploma: 0,
  BE: 1,
  ME: 2,
  PhD: 3,
} as const;

const SEDocumentType = {
  AddressProof: 0,
  PanCard: 1,
  AadharCard: 2,
  DegreeCertificate: 3,
  Marksheet: 4,
  ExperienceCertificate: 5,
  ISSECertificate: 6,
  PropertyTaxReceipt: 7,
  ProfilePicture: 8,
  SelfDeclaration: 9,
  COACertificate: 10,
  AdditionalDocument: 11,
  RecommendedForm: 12,
  PaymentChallan: 13,
  LicenceCertificate: 14,
  UGCRecognition: 15,
  AICTEApproval: 16,
  ITICertificate: 17,
  DiplomaCertificate: 18,
} as const;

type PositionTypeValue = (typeof PositionType)[keyof typeof PositionType];
type GenderValue = (typeof Gender)[keyof typeof Gender];
type SpecializationValue = (typeof Specialization)[keyof typeof Specialization];
type SEDocumentTypeValue = (typeof SEDocumentType)[keyof typeof SEDocumentType];

interface Address {
  addressLine1: string;
  addressLine2: string;
  addressLine3: string;
  city: string;
  state: string;
  country: string;
  pinCode: string;
}

interface Qualification {
  fileId: string;
  instituteName: string;
  universityName: string;
  specialization: SpecializationValue;
  degreeName: string;
  passingMonth: number;
  yearOfPassing: string;
}

interface Experience {
  fileId: string;
  companyName: string;
  position: string;
  yearsOfExperience: number;
  fromDate: string;
  toDate: string;
}

interface Document {
  documentType: SEDocumentTypeValue;
  filePath: string;
  fileName: string;
  documentName?: string; // Custom name for the document (e.g., "Tax Receipt")
  fileId: string;
  file?: File;
  fileBase64?: string;
  fileSize?: number;
  contentType?: string;
}

interface FormData {
  firstName: string;
  middleName: string;
  lastName: string;
  motherName: string;
  mobileNumber: string;
  emailAddress: string;
  positionType: PositionTypeValue;
  bloodGroup: string;
  height: number;
  gender: GenderValue;
  dateOfBirth: string;
  permanentAddress: Address;
  currentAddress: Address;
  panCardNumber: string;
  aadharCardNumber: string;
  coaCardNumber: string;
  qualifications: Qualification[];
  experiences: Experience[];
  documents: Document[];
}

export const PositionRegistrationPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { positionType: positionParam, applicationId } = useParams<{
    positionType: string;
    applicationId?: string;
  }>();
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [permanentSameAsLocal, setPermanentSameAsLocal] = useState(false);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [showValidationPopup, setShowValidationPopup] = useState(false);
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const [showReviewPopup, setShowReviewPopup] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);
  const [submittedApplicationNumber, setSubmittedApplicationNumber] =
    useState("");
  const [showDraftSuccessPopup, setShowDraftSuccessPopup] = useState(false);
  const [draftApplicationNumber, setDraftApplicationNumber] = useState("");
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingApplicationId, setEditingApplicationId] = useState<
    number | null
  >(null);
  const [isResubmitMode, setIsResubmitMode] = useState(false);
  const [rejectionComments, setRejectionComments] = useState("");
  const [additionalDocumentName, setAdditionalDocumentName] = useState("");
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [showUnsavedChangesPopup, setShowUnsavedChangesPopup] = useState(false);
  const [pendingNavigation, setPendingNavigation] = useState<string | null>(
    null
  );

  // Determine position type from URL parameter or default to StructuralEngineer
  const getPositionType = (): PositionTypeValue => {
    if (!positionParam) {
      console.log(
        "[DEBUG] No positionParam in URL, defaulting to StructuralEngineer (2)"
      );
      return PositionType.StructuralEngineer;
    }

    const positionMap: { [key: string]: PositionTypeValue } = {
      architect: PositionType.Architect,
      "licence-engineer": PositionType.LicenceEngineer,
      "structural-engineer": PositionType.StructuralEngineer,
      supervisor1: PositionType.Supervisor1,
      supervisor2: PositionType.Supervisor2,
    };

    const mappedValue =
      positionMap[positionParam.toLowerCase()] ??
      PositionType.StructuralEngineer;

    return mappedValue;
  };

  // Helper function to get default birth date (18 years ago from today)
  const getDefaultBirthDate = (): string => {
    const today = new Date();
    const eighteenYearsAgo = new Date(
      today.getFullYear() - 18,
      today.getMonth(),
      today.getDate()
    );
    // Format as YYYY-MM-DD for HTML date input
    return eighteenYearsAgo.toISOString().split("T")[0];
  };

  // Use state for selected position type so it can be changed dynamically
  const [selectedPositionType, setSelectedPositionType] =
    useState<PositionTypeValue>(getPositionType());

  // Config updates automatically when selectedPositionType changes
  // Fallback to StructuralEngineer config if selected position type doesn't have a config
  const config =
    POSITION_CONFIG[selectedPositionType] ||
    POSITION_CONFIG[PositionType.StructuralEngineer];

  const [formData, setFormData] = useState<FormData>({
    firstName: "",
    middleName: "",
    lastName: "",
    motherName: "",
    mobileNumber: "",
    emailAddress: "",
    positionType: selectedPositionType,
    bloodGroup: "",
    height: 0,
    gender: Gender.Male,
    dateOfBirth: getDefaultBirthDate(),
    permanentAddress: {
      addressLine1: "",
      addressLine2: "",
      addressLine3: "",
      city: "",
      state: "",
      country: "India",
      pinCode: "",
    },
    currentAddress: {
      addressLine1: "",
      addressLine2: "",
      addressLine3: "",
      city: "",
      state: "",
      country: "India",
      pinCode: "",
    },
    panCardNumber: "",
    aadharCardNumber: "",
    coaCardNumber: "",
    qualifications: [
      {
        fileId: `QN_${Date.now()}`,
        instituteName: "",
        universityName: "",
        specialization: Specialization.BE,
        degreeName: "",
        passingMonth: 1,
        yearOfPassing: "",
      },
    ],
    experiences: [
      {
        fileId: `EXP_${Date.now()}`,
        companyName: "",
        position: "",
        yearsOfExperience: 0,
        fromDate: "",
        toDate: "",
      },
    ],
    documents: [],
  });

  const positionOptions = [
    { value: PositionType.Architect, label: "Architect" },
    { value: PositionType.LicenceEngineer, label: "Licence Engineer" },
    { value: PositionType.StructuralEngineer, label: "Structural Engineer" },
    { value: PositionType.Supervisor1, label: "Supervisor1" },
    { value: PositionType.Supervisor2, label: "Supervisor2" },
  ];

  const bloodGroupOptions = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

  const specializationOptions = [
    { value: Specialization.Diploma, label: "Diploma" },
    { value: Specialization.BE, label: "B.E / B.Tech" },
    { value: Specialization.ME, label: "M.E / M.Tech / M.S" },
    { value: Specialization.PhD, label: "Ph.D" },
  ];

  const monthOptions = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];

  // Format date for display (removes timestamp)
  const formatDate = (dateString: string) => {
    if (!dateString) return "N/A";
    try {
      const date = new Date(dateString);
      // Check if it's a valid date
      if (isNaN(date.getTime())) return dateString;
      return date.toLocaleDateString("en-IN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return dateString;
    }
  };

  // Format year only (for passing year)
  const formatYear = (yearString: string) => {
    if (!yearString) return "N/A";
    try {
      // If it's already just a year (4 digits), return it
      if (/^\d{4}$/.test(yearString)) return yearString;
      // If it's a date string with timestamp, extract year
      const date = new Date(yearString);
      if (!isNaN(date.getTime())) {
        return date.getFullYear().toString();
      }
      return yearString;
    } catch {
      return yearString;
    }
  };

  const handleInputChange = (field: string, value: string | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }));

    // Update selectedPositionType when position dropdown changes
    if (field === "positionType" && typeof value === "number") {
      setSelectedPositionType(value as PositionTypeValue);
    }
  };

  const handleAddressChange = (
    addressType: "permanentAddress" | "currentAddress",
    field: string,
    value: string
  ) => {
    setFormData((prev) => {
      const newFormData = {
        ...prev,
        [addressType]: {
          ...prev[addressType],
          [field]: value,
        },
      };

      // If changing local address and permanent is synced, update permanent too
      if (addressType === "currentAddress" && permanentSameAsLocal) {
        newFormData.permanentAddress = {
          ...newFormData.currentAddress,
        };
      }

      return newFormData;
    });
  };

  const handlePermanentSameAsLocal = (checked: boolean) => {
    setPermanentSameAsLocal(checked);
    if (checked) {
      setFormData((prev) => ({
        ...prev,
        permanentAddress: { ...prev.currentAddress },
      }));
    }
  };

  const addQualification = () => {
    setFormData((prev) => ({
      ...prev,
      qualifications: [
        ...prev.qualifications,
        {
          fileId: `QN_${Date.now()}`,
          instituteName: "",
          universityName: "",
          specialization: Specialization.BE,
          degreeName: "",
          passingMonth: 1,
          yearOfPassing: "",
        },
      ],
    }));
  };

  const removeQualification = (index: number) => {
    // Only allow removal if there's more than one qualification
    if (formData.qualifications.length > 1) {
      setFormData((prev) => {
        const newQualifications = prev.qualifications.filter(
          (_, i) => i !== index
        );
        return {
          ...prev,
          qualifications: newQualifications,
        };
      });
    }
  };

  const handleQualificationChange = (
    index: number,
    field: string,
    value: string | number
  ) => {
    setFormData((prev) => ({
      ...prev,
      qualifications: prev.qualifications.map((qual, i) =>
        i === index ? { ...qual, [field]: value } : qual
      ),
    }));
  };

  const addExperience = () => {
    setFormData((prev) => ({
      ...prev,
      experiences: [
        ...prev.experiences,
        {
          fileId: `EXP_${Date.now()}`,
          companyName: "",
          position: "",
          yearsOfExperience: 0,
          fromDate: "",
          toDate: "",
        },
      ],
    }));
  };

  const removeExperience = (index: number) => {
    // Only allow removal if there's more than one experience
    if (formData.experiences.length > 1) {
      setFormData((prev) => {
        const newExperiences = prev.experiences.filter((_, i) => i !== index);
        return {
          ...prev,
          experiences: newExperiences,
        };
      });
    }
  };

  const handleExperienceChange = (
    index: number,
    field: string,
    value: string | number
  ) => {
    setFormData((prev) => ({
      ...prev,
      experiences: prev.experiences.map((exp, i) =>
        i === index ? { ...exp, [field]: value } : exp
      ),
    }));
  };

  const calculateExperience = (index: number) => {
    const exp = formData.experiences[index];
    if (exp.fromDate && exp.toDate) {
      const from = new Date(exp.fromDate);
      const to = new Date(exp.toDate);
      const diffTime = Math.abs(to.getTime() - from.getTime());
      const diffYears = diffTime / (1000 * 60 * 60 * 24 * 365.25);
      handleExperienceChange(
        index,
        "yearsOfExperience",
        Math.round(diffYears * 10) / 10
      );
    }
  };

  const handleFileUpload = async (
    documentType: SEDocumentTypeValue,
    fileId: string,
    file: File,
    customFileName?: string,
    documentName?: string
  ) => {
    // TODO: Implement actual file upload to server
    // For now, create a local URL
    const document: Document = {
      documentType,
      filePath: URL.createObjectURL(file),
      fileName: customFileName || file.name, // Use custom name if provided
      documentName: documentName, // Store custom document name separately
      fileId,
      file,
    };

    setFormData((prev) => ({
      ...prev,
      documents: [
        ...prev.documents.filter((d) => d.fileId !== fileId),
        document,
      ],
    }));
  };

  // Map form data to API request format
  const mapFormDataToRequest = async (
    data: FormData,
    status: number
  ): Promise<PositionRegistrationRequest> => {
    // Helper function to convert date strings to ISO 8601 UTC format
    const toUTCDate = (dateStr: string) => {
      if (!dateStr) return dateStr;
      // If already in ISO format, return as is
      if (dateStr.includes("T") && dateStr.includes("Z")) return dateStr;
      // Convert to UTC ISO format
      const date = new Date(dateStr);
      return date.toISOString();
    };

    // Map local address (handle empty/partial data for drafts)
    const localAddress: ApiAddress = {
      addressLine1: data.currentAddress.addressLine1 || "",
      addressLine2: data.currentAddress.addressLine2 || undefined,
      addressLine3: data.currentAddress.addressLine3 || undefined,
      city: data.currentAddress.city || "",
      state: data.currentAddress.state || "",
      country: "India", // Always India - ignore user input
      pinCode: data.currentAddress.pinCode || "",
    };

    // Map permanent address (handle empty/partial data for drafts)
    const permanentAddress: ApiAddress = permanentSameAsLocal
      ? { ...localAddress }
      : {
          addressLine1: data.permanentAddress.addressLine1 || "",
          addressLine2: data.permanentAddress.addressLine2 || undefined,
          addressLine3: data.permanentAddress.addressLine3 || undefined,
          city: data.permanentAddress.city || "",
          state: data.permanentAddress.state || "",
          country: "India", // Always India - ignore user input
          pinCode: data.permanentAddress.pinCode || "",
        };

    // Map qualifications (allow empty for drafts)
    const qualifications: ApiQualification[] = data.qualifications
      .filter((q) => q.instituteName && q.instituteName.trim() !== "")
      .map((q) => ({
        fileId: q.fileId,
        instituteName: q.instituteName,
        universityName: q.universityName || "",
        specialization: q.specialization,
        degreeName: q.degreeName || "",
        passingMonth: q.passingMonth || 1,
        yearOfPassing: q.yearOfPassing ? parseInt(q.yearOfPassing) : 0,
      }));

    // Map experiences (allow empty for drafts)
    const experiences: ApiExperience[] = data.experiences
      .filter((e) => e.companyName && e.companyName.trim() !== "")
      .map((e) => ({
        fileId: e.fileId,
        companyName: e.companyName,
        position: e.position || "",
        fromDate: e.fromDate ? toUTCDate(e.fromDate) : "", // Convert to UTC or empty
        toDate: e.toDate ? toUTCDate(e.toDate) : "", // Convert to UTC or empty
      }));

    // Convert documents to base64 format
    const documents = await Promise.all(
      data.documents.map(async (d) => {
        // If file exists (newly uploaded), convert it to base64
        if (d.file) {
          const uploadedDoc = await positionRegistrationService.uploadDocument(
            d.file,
            d.documentType
          );
          // Use the custom fileName and documentName from the Document object if available
          return {
            ...uploadedDoc,
            fileName: d.fileName || uploadedDoc.fileName,
            documentName: d.documentName, // Include custom document name
          };
        }
        // For existing documents (edit/resubmit mode), send metadata only - don't resend file content
        // The backend will keep the existing file content
        if ((d.fileBase64 && d.fileBase64.length > 0) || d.filePath) {
          return {
            fileId: d.fileId,
            documentType: d.documentType,
            fileName: d.fileName,
            documentName: d.documentName, // Include custom document name
            fileBase64: "", // Don't send existing file content - saves bandwidth
            fileSize: d.fileSize,
            contentType: d.contentType,
          };
        }
        // Skip documents without any file data
        return null;
      })
    );

    // Filter out null documents and ensure type safety
    const validDocuments = documents.filter(
      (d): d is NonNullable<typeof d> => d !== null
    );

    // Build request (handle empty/partial data for drafts)
    const request: PositionRegistrationRequest = {
      firstName: data.firstName || "",
      middleName: data.middleName || undefined,
      lastName: data.lastName || "",
      motherName: data.motherName || "",
      mobileNumber: data.mobileNumber || "",
      emailAddress: data.emailAddress || "",
      positionType: data.positionType,
      bloodGroup: data.bloodGroup || undefined,
      height: data.height || undefined,
      gender: data.gender,
      dateOfBirth: data.dateOfBirth ? toUTCDate(data.dateOfBirth) : "", // Convert to UTC or empty
      panCardNumber: data.panCardNumber || "",
      aadharCardNumber: data.aadharCardNumber || "",
      coaCardNumber: data.coaCardNumber || undefined,
      status,
      localAddress,
      permanentAddress,
      qualifications,
      experiences,
      documents: validDocuments, // Use filtered documents
    };

    return request;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Set attemptedSubmit first to trigger validation UI
    setAttemptedSubmit(true);

    // Comprehensive frontend validation with detailed error messages
    const errors: string[] = [];

    // Personal Details validation
    if (!formData.firstName)
      errors.push("📝 Personal Details - First name is required");
    if (!formData.lastName)
      errors.push("📝 Personal Details - Last name is required");
    if (!formData.motherName)
      errors.push(
        "📝 Personal Details - Mother's name is required for official records"
      );
    if (!formData.mobileNumber)
      errors.push(
        "📱 Personal Details - Mobile number is required for communication"
      );
    else if (!/^[6-9]\d{9}$/.test(formData.mobileNumber))
      errors.push(
        "📱 Personal Details - Please enter a valid 10-digit mobile number starting with 6-9"
      );
    if (!formData.emailAddress)
      errors.push(
        "📧 Personal Details - Email address is required for notifications"
      );
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.emailAddress))
      errors.push("📧 Personal Details - Please enter a valid email address");
    if (!formData.dateOfBirth)
      errors.push("📅 Personal Details - Date of birth is required");
    if (!formData.bloodGroup)
      errors.push("🩸 Personal Details - Blood group is required");
    if (!formData.height || formData.height <= 0)
      errors.push("📏 Personal Details - Height (in cms) is required");

    // Local Address validation
    if (!formData.currentAddress.addressLine1)
      errors.push("🏠 Current Address - Address Line 1 is required");
    if (!formData.currentAddress.addressLine2)
      errors.push("🏠 Current Address - Address Line 2 is required");
    if (!formData.currentAddress.city)
      errors.push("🏙️ Current Address - City name is required");
    if (!formData.currentAddress.state)
      errors.push("📍 Current Address - State name is required");
    // Country is always "India" - no validation needed
    if (!formData.currentAddress.pinCode)
      errors.push("📮 Current Address - PIN code is required");
    else if (!/^\d{6}$/.test(formData.currentAddress.pinCode))
      errors.push("📮 Current Address - Please enter a valid 6-digit PIN code");

    // Permanent Address validation (if not same as local)
    if (!permanentSameAsLocal) {
      if (!formData.permanentAddress.addressLine1)
        errors.push("🏡 Permanent Address - Address Line 1 is required");
      if (!formData.permanentAddress.addressLine2)
        errors.push("🏡 Permanent Address - Address Line 2 is required");
      if (!formData.permanentAddress.city)
        errors.push("🏙️ Permanent Address - City name is required");
      if (!formData.permanentAddress.state)
        errors.push("📍 Permanent Address - State name is required");
      // Country is always "India" - no validation needed
      if (!formData.permanentAddress.pinCode)
        errors.push("📮 Permanent Address - PIN code is required");
      else if (!/^\d{6}$/.test(formData.permanentAddress.pinCode))
        errors.push(
          "📮 Permanent Address - Please enter a valid 6-digit PIN code"
        );
    }

    // PAN validation
    if (!formData.panCardNumber)
      errors.push("💳 Identity Documents - PAN card number is required");
    else if (!/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/.test(formData.panCardNumber))
      errors.push(
        "💳 Identity Documents - Please enter a valid PAN card number (e.g., ABCDE1234F)"
      );
    const panDoc = formData.documents.find(
      (d) => d.documentType === SEDocumentType.PanCard
    );
    // Only require upload if document doesn't already exist (resubmit mode keeps existing docs)
    if (
      !panDoc ||
      ((!panDoc.fileBase64 || panDoc.fileBase64.trim() === "") &&
        (!panDoc.filePath || panDoc.filePath.trim() === ""))
    ) {
      errors.push(
        "📄 Identity Documents - PAN card document upload is required"
      );
    }

    // Aadhar validation
    if (!formData.aadharCardNumber)
      errors.push("🆔 Identity Documents - Aadhar card number is required");
    else if (!/^\d{12}$/.test(formData.aadharCardNumber))
      errors.push(
        "🆔 Identity Documents - Please enter a valid 12-digit Aadhar number"
      );
    const aadharDoc = formData.documents.find(
      (d) => d.documentType === SEDocumentType.AadharCard
    );
    // Only require upload if document doesn't already exist (resubmit mode keeps existing docs)
    if (
      !aadharDoc ||
      ((!aadharDoc.fileBase64 || aadharDoc.fileBase64.trim() === "") &&
        (!aadharDoc.filePath || aadharDoc.filePath.trim() === ""))
    ) {
      errors.push(
        "📄 Identity Documents - Aadhar card document upload is required"
      );
    }

    // Qualifications validation
    if (config.sections.qualifications) {
      if (formData.qualifications.length === 0) {
        errors.push(
          "🎓 Qualifications - At least one qualification must be added"
        );
      }
      formData.qualifications.forEach((qual, index) => {
        if (!qual.instituteName)
          errors.push(
            `🎓 Qualification #${index + 1} - Institute name is required`
          );
        if (!qual.universityName)
          errors.push(
            `🎓 Qualification #${index + 1} - University/Board name is required`
          );
        if (!qual.degreeName)
          errors.push(
            `🎓 Qualification #${index + 1} - Degree/Diploma name is required`
          );
        if (!qual.yearOfPassing)
          errors.push(
            `🎓 Qualification #${index + 1} - Year of passing is required`
          );
      });
    }

    // Experience validation
    if (config.sections.experience) {
      if (formData.experiences.length === 0) {
        errors.push(
          "💼 Work Experience - At least one work experience must be added"
        );
      }
      formData.experiences.forEach((exp, index) => {
        if (!exp.companyName)
          errors.push(
            `💼 Experience #${
              index + 1
            } - Company/Organization name is required`
          );
        if (!exp.position)
          errors.push(
            `💼 Experience #${index + 1} - Position/Designation is required`
          );
        if (!exp.fromDate)
          errors.push(`💼 Experience #${index + 1} - Start date is required`);
        if (!exp.toDate)
          errors.push(`💼 Experience #${index + 1} - End date is required`);
      });
    }

    // Property Tax Receipt validation
    if (config.sections.propertyTaxReceipt) {
      const propertyTaxDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.PropertyTaxReceipt
      );
      if (
        !propertyTaxDoc ||
        ((!propertyTaxDoc.fileBase64 ||
          propertyTaxDoc.fileBase64.trim() === "") &&
          (!propertyTaxDoc.filePath || propertyTaxDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Required Documents - Property tax receipt / Rent agreement / Electricity bill is required as address proof"
        );
      }
    }

    // ISSE Certificate validation
    if (config.sections.isseCertificate) {
      const isseDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.ISSECertificate
      );
      if (
        !isseDoc ||
        ((!isseDoc.fileBase64 || isseDoc.fileBase64.trim() === "") &&
          (!isseDoc.filePath || isseDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Professional Certificates - ISSE (Indian Society of Structural Engineers) certificate is required"
        );
      }
    }

    // COA Certificate validation
    if (config.sections.coaCertificate) {
      const coaDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.COACertificate
      );
      if (
        !coaDoc ||
        ((!coaDoc.fileBase64 || coaDoc.fileBase64.trim() === "") &&
          (!coaDoc.filePath || coaDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Professional Certificates - Council of Architecture (COA) certificate is required"
        );
      }
    }

    // UGC Recognition validation
    if (config.sections.ugcRecognition) {
      const ugcDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.UGCRecognition
      );
      if (
        !ugcDoc ||
        ((!ugcDoc.fileBase64 || ugcDoc.fileBase64.trim() === "") &&
          (!ugcDoc.filePath || ugcDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Academic Certificates - UGC Recognition certificate is required (for degrees from outside Maharashtra)"
        );
      }
    }

    // AICTE Approval validation
    if (config.sections.aicteApproval) {
      const aicteDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.AICTEApproval
      );
      if (
        !aicteDoc ||
        ((!aicteDoc.fileBase64 || aicteDoc.fileBase64.trim() === "") &&
          (!aicteDoc.filePath || aicteDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Academic Certificates - AICTE Approval certificate is required for your degree"
        );
      }
    }

    // Self Declaration validation
    if (config.sections.selfDeclaration) {
      const selfDecDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.SelfDeclaration
      );
      if (
        !selfDecDoc ||
        ((!selfDecDoc.fileBase64 || selfDecDoc.fileBase64.trim() === "") &&
          (!selfDecDoc.filePath || selfDecDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📄 Required Documents - Self declaration form must be uploaded (signed and stamped)"
        );
      }
    }

    // Profile Picture validation
    if (config.sections.profilePicture) {
      const profilePicDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.ProfilePicture
      );
      if (
        !profilePicDoc ||
        ((!profilePicDoc.fileBase64 ||
          profilePicDoc.fileBase64.trim() === "") &&
          (!profilePicDoc.filePath || profilePicDoc.filePath.trim() === ""))
      ) {
        errors.push(
          "📷 Required Documents - Passport-size photograph is required"
        );
      }
    }

    if (errors.length > 0) {
      setValidationErrors(errors);
      setShowValidationPopup(true);
      setError(
        `Found ${errors.length} validation error${
          errors.length > 1 ? "s" : ""
        }. Please review and fix them before submitting.`
      );
      window.scrollTo({ top: 0, behavior: "smooth" });
      return;
    }

    // Show review popup instead of submitting directly
    setShowReviewPopup(true);
  };

  // New function to handle actual submission after review
  const handleConfirmSubmit = async () => {
    setIsSubmitting(true);
    setError("");
    setSuccess("");
    setValidationErrors([]);
    setFieldErrors({});
    setShowValidationPopup(false);

    try {
      // Map form data to API request with Submitted status (2)
      const request = await mapFormDataToRequest(formData, 2);

      // Debug logging before API call

      let response;
      // If in resubmit mode, call resubmit endpoint
      if (isResubmitMode && editingApplicationId) {
        response = await positionRegistrationService.resubmitApplication(
          editingApplicationId,
          request
        );
      }
      // If in edit mode (but not resubmit), update existing application
      else if (isEditMode && editingApplicationId) {
        response = await positionRegistrationService.updateApplication(
          editingApplicationId,
          request
        );
      }
      // Otherwise create new application
      else {
        response = await positionRegistrationService.createApplication(request);
      }

      // Close review popup and show success popup
      setShowReviewPopup(false);
      setSubmittedApplicationNumber(response.applicationNumber || "Pending");
      setShowSuccessPopup(true);

      // Clear unsaved changes flag
      setHasUnsavedChanges(false);

      // Navigate to dashboard after 3 seconds
      setTimeout(() => {
        navigate("/dashboard");
      }, 3000);
    } catch (err: unknown) {
      console.error("Submission error:", err);

      // Close review popup
      setShowReviewPopup(false);

      // Handle different types of errors from backend
      const error = err as {
        response?: {
          status?: number;
          data?: {
            errors?: Record<string, string[]>;
            error?: string;
            message?: string;
            title?: string;
            detail?: string;
          };
        };
        message?: string;
      };

      if (error?.response?.data) {
        const errorData = error.response.data;

        // Handle validation errors (400 Bad Request with errors object)
        if (error.response.status === 400 && errorData.errors) {
          const errors: string[] = [];
          const fields: Record<string, string> = {};

          // Extract validation errors from ASP.NET Core ModelState
          Object.keys(errorData.errors).forEach((field) => {
            const fieldErrors = errorData.errors?.[field];
            if (Array.isArray(fieldErrors)) {
              fieldErrors.forEach((errorMsg: string) => {
                errors.push(errorMsg);
                // Map backend field names to frontend field names
                const fieldName = field.toLowerCase();
                if (!fields[fieldName]) {
                  fields[fieldName] = errorMsg;
                }
              });
            }
          });

          if (errors.length > 0) {
            setValidationErrors(errors);
            setFieldErrors(fields);
            setShowValidationPopup(true); // Show popup for backend validation
            setError("");
          } else if (errorData.message || errorData.title) {
            const errorMsg = errorData.message || errorData.title || "";
            setError(errorMsg);
            setValidationErrors([errorMsg]);
            setShowValidationPopup(true); // Show popup
          } else {
            setError("Validation failed. Please check your input.");
            setValidationErrors([
              "Validation failed. Please check your input.",
            ]);
            setShowValidationPopup(true); // Show popup
          }
        }
        // Handle single error field from backend (like "error": "message")
        else if (errorData.error) {
          setError(errorData.error);
          setValidationErrors([errorData.error]);
          setShowValidationPopup(true); // Show popup for backend errors
        }
        // Handle general error messages
        else if (errorData.message) {
          setError(errorData.message);
          setValidationErrors([errorData.message]);
          setShowValidationPopup(true); // Show popup for backend errors
        }
        // Handle exception messages (title/detail from ProblemDetails)
        else if (errorData.title || errorData.detail) {
          const errorMsg = errorData.detail || errorData.title || "";
          setError(errorMsg);
          setValidationErrors([errorMsg]);
          setShowValidationPopup(true); // Show popup for exceptions
        } else {
          const errorMsg = "Failed to submit application. Please try again.";
          setError(errorMsg);
          setValidationErrors([errorMsg]);
          setShowValidationPopup(true); // Show popup for unknown errors
        }
      } else if (error.message) {
        // Network errors or other exceptions
        setError(error.message);
        setValidationErrors([error.message]);
        setShowValidationPopup(true); // Show popup for exceptions
      } else {
        const errorMsg = "Failed to submit application. Please try again.";
        setError(errorMsg);
        setValidationErrors([errorMsg]);
        setShowValidationPopup(true); // Show popup for unknown errors
      }

      // Scroll to top to see error message
      window.scrollTo({ top: 0, behavior: "smooth" });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSaveAsDraft = async () => {
    setLoading(true);
    setError("");
    setSuccess("");
    setValidationErrors([]);
    setFieldErrors({});
    setShowValidationPopup(false);

    try {
      // Map form data to API request with Draft status (1)
      // NO VALIDATION required for draft save - allow partial/incomplete data
      const request = await mapFormDataToRequest(formData, 1);

      let response;
      // If in edit mode, update existing draft; otherwise create new
      if (isEditMode && editingApplicationId) {
        response = await positionRegistrationService.updateDraft(
          editingApplicationId,
          request
        );
      } else {
        response = await positionRegistrationService.saveDraft(request);
      }

      // Set draft application number and show success popup
      setDraftApplicationNumber(
        response.applicationNumber || `DRAFT-${response.id}`
      );
      setShowDraftSuccessPopup(true);

      // Clear unsaved changes flag since draft is saved
      setHasUnsavedChanges(false);

      // Navigate to dashboard after 3 seconds
      setTimeout(() => {
        navigate("/dashboard");
      }, 3000);
    } catch (err: unknown) {
      console.error("Draft save error:", err);

      const error = err as {
        response?: {
          data?: {
            message?: string;
            title?: string;
            error?: string;
          };
        };
        message?: string;
      };

      // Show simple error message for draft save failures
      const errorMsg =
        error?.response?.data?.error ||
        error?.response?.data?.message ||
        error?.response?.data?.title ||
        error?.message ||
        "Failed to save draft. Please try again.";
      setError(errorMsg);

      // Scroll to top to see error message
      window.scrollTo({ top: 0, behavior: "smooth" });
    } finally {
      setLoading(false);
    }
  };

  // Date validation helpers
  const getMaxBirthDate = () => {
    const today = new Date();
    const maxDate = new Date(
      today.getFullYear() - 18,
      today.getMonth(),
      today.getDate()
    );
    return maxDate.toISOString().split("T")[0];
  };

  const getMaxExperienceDate = () => {
    const today = new Date();
    return today.toISOString().split("T")[0];
  };

  const getCurrentYear = () => {
    return new Date().getFullYear();
  };

  const getCurrentMonth = () => {
    return new Date().getMonth() + 1; // JavaScript months are 0-indexed
  };

  // Handle cancel button click with unsaved changes check
  const handleCancel = () => {
    if (hasUnsavedChanges) {
      setPendingNavigation("/dashboard");
      setShowUnsavedChangesPopup(true);
    } else {
      navigate("/dashboard");
    }
  };

  // Confirm navigation despite unsaved changes
  const handleConfirmNavigation = () => {
    setShowUnsavedChangesPopup(false);
    setHasUnsavedChanges(false);
    if (pendingNavigation) {
      navigate(pendingNavigation);
    }
  };

  // Cancel navigation and stay on form
  const handleCancelNavigation = () => {
    setShowUnsavedChangesPopup(false);
    setPendingNavigation(null);
  };

  // Initialize the form - simulate loading to prevent blank page
  useEffect(() => {
    // Small delay to ensure smooth transition
    const timer = setTimeout(() => {
      setInitializing(false);
    }, 100);
    return () => clearTimeout(timer);
  }, []);

  // Track form changes to detect unsaved changes
  useEffect(() => {
    // Mark as changed if user has modified the form
    // Skip tracking during initial load or when data is being loaded from backend
    if (!loading && !initializing) {
      setHasUnsavedChanges(true);
    }
  }, [formData, loading, initializing]);

  // Warn user before leaving page with unsaved changes
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges && !isSubmitting) {
        e.preventDefault();
        e.returnValue =
          "You have unsaved changes. Are you sure you want to leave?";
        return e.returnValue;
      }
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasUnsavedChanges, isSubmitting]);

  // Load application data if editing (applicationId present)
  useEffect(() => {
    const loadApplicationData = async () => {
      if (!applicationId) return;

      try {
        setLoading(true);
        setIsEditMode(true);
        setEditingApplicationId(parseInt(applicationId));

        // Check if this is a resubmission (URL param: ?resubmit=true)
        const isResubmit = searchParams.get("resubmit") === "true";
        setIsResubmitMode(isResubmit);

        // Fetch application data
        const response = await positionRegistrationService.getApplication(
          parseInt(applicationId)
        );

        // Store rejection comments if application is rejected
        // Status can be either number 37 or string "REJECTED"
        if (response.status === 37 || response.status === "REJECTED") {
          // Status 37 = REJECTED - Get rejection comments from the officer who rejected
          let comments = "";
          if (response.jeRejectionStatus && response.jeRejectionComments) {
            comments = `Junior Engineer: ${response.jeRejectionComments}`;
          } else if (
            response.aeArchitectRejectionStatus &&
            response.aeArchitectRejectionComments
          ) {
            comments = `Assistant Engineer (Architect): ${response.aeArchitectRejectionComments}`;
          } else if (
            response.aeStructuralRejectionStatus &&
            response.aeStructuralRejectionComments
          ) {
            comments = `Assistant Engineer (Structural): ${response.aeStructuralRejectionComments}`;
          } else if (
            response.executiveEngineerRejectionStatus &&
            response.executiveEngineerRejectionComments
          ) {
            comments = `Executive Engineer: ${response.executiveEngineerRejectionComments}`;
          } else if (
            response.cityEngineerRejectionStatus &&
            response.cityEngineerRejectionComments
          ) {
            comments = `City Engineer: ${response.cityEngineerRejectionComments}`;
          } else {
            comments = response.remarks || "No rejection comments provided";
          }
          setRejectionComments(comments);
        }

        // Map API response to form data
        const addressLocal = response.addresses?.find(
          (a: { addressType: string }) => a.addressType === "Local"
        );
        const addressPermanent = response.addresses?.find(
          (a: { addressType: string }) => a.addressType === "Permanent"
        );

        // Convert position type from backend (could be string or number)
        let loadedPositionType = selectedPositionType;
        if (typeof response.positionType === "number") {
          loadedPositionType = response.positionType as PositionTypeValue;
        } else if (typeof response.positionType === "string") {
          // Map string name to enum value
          const positionTypeMap: { [key: string]: PositionTypeValue } = {
            Architect: PositionType.Architect,
            LicenceEngineer: PositionType.LicenceEngineer,
            StructuralEngineer: PositionType.StructuralEngineer,
            Supervisor1: PositionType.Supervisor1,
            Supervisor2: PositionType.Supervisor2,
          };
          loadedPositionType =
            positionTypeMap[response.positionType] ?? selectedPositionType;
        }

        // Convert gender from backend (could be string or number)
        let loadedGender: GenderValue = Gender.Male; // default
        if (typeof response.gender === "number") {
          loadedGender = response.gender as GenderValue;
        } else if (typeof response.gender === "string") {
          // Map string name to enum value
          const genderMap: { [key: string]: GenderValue } = {
            Male: Gender.Male,
            Female: Gender.Female,
            Other: Gender.Other,
          };
          loadedGender = genderMap[response.gender] ?? Gender.Male;
        }

        const newFormData = {
          firstName: response.firstName || "",
          middleName: response.middleName || "",
          lastName: response.lastName || "",
          motherName: response.motherName || "",
          mobileNumber: response.mobileNumber || "",
          emailAddress: response.emailAddress || "",
          positionType: loadedPositionType,
          bloodGroup: response.bloodGroup || "",
          height: response.height || 0,
          gender: loadedGender,
          dateOfBirth: response.dateOfBirth
            ? response.dateOfBirth.split("T")[0]
            : "",
          currentAddress: addressLocal
            ? {
                addressLine1: addressLocal.addressLine1 || "",
                addressLine2: addressLocal.addressLine2 || "",
                addressLine3: addressLocal.addressLine3 || "",
                city: addressLocal.city || "",
                state: addressLocal.state || "",
                country: "India", // Always India - ignore loaded data
                pinCode: addressLocal.pinCode || "",
              }
            : {
                addressLine1: "",
                addressLine2: "",
                addressLine3: "",
                city: "",
                state: "",
                country: "India", // Always India
                pinCode: "",
              },
          permanentAddress: addressPermanent
            ? {
                addressLine1: addressPermanent.addressLine1 || "",
                addressLine2: addressPermanent.addressLine2 || "",
                addressLine3: addressPermanent.addressLine3 || "",
                city: addressPermanent.city || "",
                state: addressPermanent.state || "",
                country: "India", // Always India - ignore loaded data
                pinCode: addressPermanent.pinCode || "",
              }
            : {
                addressLine1: "",
                addressLine2: "",
                addressLine3: "",
                city: "",
                state: "",
                country: "India", // Always India
                pinCode: "",
              },
          panCardNumber: response.panCardNumber || "",
          aadharCardNumber: response.aadharCardNumber || "",
          coaCardNumber: response.coaCardNumber || "",
          qualifications:
            response.qualifications?.map((q: any) => {
              return {
                fileId: q.fileId || `QUAL_${Date.now()}_${Math.random()}`,
                instituteName: q.instituteName || "",
                universityName: q.universityName || "",
                specialization: (q.specialization as SpecializationValue) ?? 0,
                degreeName: q.degreeName || "",
                passingMonth: q.passingMonth || 1,
                // Backend sends yearOfPassing as int, but frontend expects YYYY-MM-DD format
                yearOfPassing: q.yearOfPassing
                  ? `${q.yearOfPassing}-01-01T00:00:00.000Z`
                  : "",
              };
            }) || [],
          experiences:
            response.experiences?.map((e: any) => {
              return {
                fileId: e.fileId || `EXP_${Date.now()}_${Math.random()}`,
                companyName: e.companyName || "",
                position: e.position || "",
                yearsOfExperience: e.yearsOfExperience || 0,
                fromDate: e.fromDate ? e.fromDate.split("T")[0] : "",
                toDate: e.toDate ? e.toDate.split("T")[0] : "",
              };
            }) || [],
          documents:
            response.documents?.map((d: any) => {
              // Convert document type from string name to enum number
              let docType = 0;
              if (typeof d.documentType === "number") {
                docType = d.documentType;
              } else if (typeof d.documentType === "string") {
                // Map string name to enum value
                docType =
                  SEDocumentType[
                    d.documentType as keyof typeof SEDocumentType
                  ] ?? 0;
              }

              return {
                fileId: d.fileId || `DOC_${Date.now()}_${Math.random()}`,
                documentType: docType as SEDocumentTypeValue,
                fileName: d.fileName || "",
                documentName: d.documentName, // Load custom document name
                filePath: d.filePath || "",
                fileBase64: d.fileBase64 ?? "", // Keep existing file content (use ?? to preserve empty strings)
                fileSize: d.fileSize || 0,
                contentType: d.contentType || "application/pdf",
              };
            }) || [],
        };

        setFormData(newFormData);

        // Update the selected position type state to trigger config update
        setSelectedPositionType(loadedPositionType);

        // Reset unsaved changes flag after loading data
        setHasUnsavedChanges(false);

        setSuccess(
          `Editing ${response.status === 1 ? "Draft" : "Application"} #${
            response.applicationNumber || applicationId
          }`
        );
      } catch (err) {
        console.error("Error loading application:", err);
        setError(
          "Failed to load application data. Redirecting to dashboard..."
        );
        setTimeout(() => {
          navigate("/dashboard");
        }, 2000);
      } finally {
        setLoading(false);
      }
    };

    loadApplicationData();
  }, [applicationId, navigate, selectedPositionType, searchParams]);

  // Helper function to determine if a field should have error styling
  const hasFieldError = (fieldName: string): boolean => {
    return !!fieldErrors[fieldName.toLowerCase()];
  };

  // Safety check - if config is undefined, show error
  if (!config) {
    return (
      <div
        className="pmc-fadeIn"
        style={{ padding: "40px", textAlign: "center" }}
      >
        <AlertCircle
          size={48}
          color="#dc2626"
          style={{ margin: "0 auto 16px" }}
        />
        <h2 style={{ color: "#dc2626", marginBottom: "8px" }}>
          Invalid Position Type
        </h2>
        <p style={{ color: "#6b7280", marginBottom: "24px" }}>
          The selected position type is not configured. Please select a valid
          position type.
        </p>
        <button
          onClick={() => navigate("/dashboard")}
          className="pmc-button pmc-button-primary"
        >
          Back to Dashboard
        </button>
      </div>
    );
  }

  // Show page loader during initialization
  if (initializing) {
    return (
      <PageLoader message={`Loading ${config.name} Registration Form...`} />
    );
  }

  return (
    <div
      className="pmc-fadeIn"
      style={{
        maxWidth: "1400px",
        margin: "0 auto",
      }}
    >
      {/* Compact Form Styling */}
      <style>{`
        .pmc-card {
          box-shadow: 0 2px 8px rgba(0,0,0,0.08) !important;
        }
        .pmc-card-body {
          padding: 20px !important;
        }
        .pmc-form-group {
          margin-bottom: 0 !important;
        }
        .pmc-label {
          margin-bottom: 6px !important;
          font-size: 13px !important;
          font-weight: 600 !important;
        }
        .pmc-input, .pmc-select {
          padding: 9px 12px !important;
          font-size: 14px !important;
        }
        .pmc-text-error {
          font-size: 12px !important;
          display: block !important;
          margin-top: 4px !important;
        }
        .pmc-form-grid {
          display: grid !important;
          gap: 16px !important;
        }
        .pmc-form-grid-2 {
          grid-template-columns: repeat(2, 1fr) !important;
        }
        .pmc-form-grid-3 {
          grid-template-columns: repeat(3, 1fr) !important;
        }
        .pmc-form-grid-4 {
          grid-template-columns: repeat(4, 1fr) !important;
        }
        @media (max-width: 1024px) {
          .pmc-form-grid-4 {
            grid-template-columns: repeat(2, 1fr) !important;
          }
        }
        @media (max-width: 768px) {
          .pmc-form-grid-2,
          .pmc-form-grid-3,
          .pmc-form-grid-4 {
            grid-template-columns: 1fr !important;
          }
        }
        details[open] summary span:first-child {
          transform: rotate(90deg);
          transition: transform 0.2s;
        }
        details summary span:first-child {
          transition: transform 0.2s;
          display: inline-block;
        }
      `}</style>
      {/* Validation Error Popup */}
      {showValidationPopup && validationErrors.length > 0 && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.6)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 9999,
            padding: "20px",
          }}
          onClick={() => setShowValidationPopup(false)}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              maxWidth: "650px",
              width: "100%",
              maxHeight: "85vh",
              overflow: "hidden",
              boxShadow: "0 25px 80px rgba(0, 0, 0, 0.4)",
              display: "flex",
              flexDirection: "column",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div
              style={{
                background: "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                padding: "24px",
                color: "white",
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  marginBottom: "8px",
                }}
              >
                <svg
                  style={{ width: "32px", height: "32px", flexShrink: 0 }}
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fillRule="evenodd"
                    d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                    clipRule="evenodd"
                  />
                </svg>
                <h2 style={{ fontSize: "24px", fontWeight: "700", margin: 0 }}>
                  Validation Failed
                </h2>
              </div>
              <p style={{ margin: 0, opacity: 0.95, fontSize: "14px" }}>
                {validationErrors.length} error
                {validationErrors.length > 1 ? "s" : ""} found. Please fix the
                following issues before submitting:
              </p>
            </div>

            {/* Content */}
            <div
              style={{
                padding: "24px",
                overflow: "auto",
                flex: 1,
              }}
            >
              <div
                style={{ display: "flex", flexDirection: "column", gap: "8px" }}
              >
                {validationErrors.map((error, index) => (
                  <div
                    key={index}
                    style={{
                      padding: "14px 16px",
                      backgroundColor: "#fef2f2",
                      border: "2px solid #fecaca",
                      borderLeft: "4px solid #dc2626",
                      borderRadius: "8px",
                      display: "flex",
                      alignItems: "flex-start",
                      gap: "12px",
                      transition: "all 0.2s",
                    }}
                  >
                    <span
                      style={{
                        fontSize: "20px",
                        flexShrink: 0,
                        marginTop: "-2px",
                      }}
                    >
                      {error.split(" ")[0]}
                    </span>
                    <div style={{ flex: 1 }}>
                      <span
                        style={{
                          color: "#991b1b",
                          fontSize: "14px",
                          lineHeight: "1.6",
                          fontWeight: "500",
                        }}
                      >
                        {error.substring(error.indexOf(" ") + 1)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              <div
                style={{
                  marginTop: "20px",
                  padding: "16px",
                  background:
                    "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
                  border: "2px solid #fbbf24",
                  borderRadius: "10px",
                }}
              >
                <p
                  style={{
                    margin: 0,
                    color: "#92400e",
                    fontSize: "13px",
                    lineHeight: "1.6",
                    fontWeight: "500",
                  }}
                >
                  💡 <strong>Tip:</strong> Scroll through the form to locate
                  highlighted fields with errors. All required fields are marked
                  with a red asterisk (*).
                </p>
              </div>
            </div>

            {/* Footer */}
            <div
              style={{
                padding: "20px 24px",
                borderTop: "1px solid #e5e7eb",
                background: "#f9fafb",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                gap: "12px",
              }}
            >
              <span
                style={{
                  fontSize: "13px",
                  color: "#6b7280",
                  fontWeight: "500",
                }}
              >
                Review and fix all errors to proceed
              </span>
              <button
                onClick={() => setShowValidationPopup(false)}
                style={{
                  padding: "12px 28px",
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                  color: "white",
                  border: "none",
                  borderRadius: "8px",
                  fontWeight: "700",
                  fontSize: "14px",
                  cursor: "pointer",
                  transition: "all 0.2s ease",
                  boxShadow: "0 4px 12px rgba(220, 38, 38, 0.3)",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 6px 16px rgba(220, 38, 38, 0.4)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 4px 12px rgba(220, 38, 38, 0.3)";
                }}
              >
                Got it, Fix Errors
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header Section - Compact */}
      <div
        className="pmc-content-header pmc-fadeInDown"
        style={{ marginBottom: "16px" }}
      >
        {/* Back to Dashboard Button */}
        <button
          type="button"
          onClick={() => navigate("/dashboard")}
          className="pmc-button pmc-button-secondary"
          style={{
            marginBottom: "16px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <ArrowLeft size={16} />
          Back to Dashboard
        </button>

        {/* Rejection Banner - Shown at the top when application is rejected */}
        {rejectionComments && (
          <div
            className="pmc-fadeIn"
            style={{
              padding: "16px 20px",
              marginBottom: "20px",
              background: "linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)",
              border: "2px solid #fca5a5",
              borderRadius: "10px",
              boxShadow: "0 4px 12px rgba(220, 38, 38, 0.1)",
            }}
          >
            <div
              style={{
                display: "flex",
                gap: "12px",
                alignItems: "flex-start",
                marginBottom: "12px",
              }}
            >
              <AlertCircle
                size={24}
                style={{ color: "#dc2626", flexShrink: 0, marginTop: "2px" }}
              />
              <div style={{ flex: 1 }}>
                <h3
                  className="pmc-text-lg pmc-font-bold"
                  style={{ color: "#dc2626", marginBottom: "8px" }}
                >
                  Application Rejected -{" "}
                  {isResubmitMode
                    ? "Corrections Required"
                    : "View Rejection Reason"}
                </h3>
                <p
                  className="pmc-text-sm pmc-font-semibold"
                  style={{ color: "#7f1d1d", marginBottom: "6px" }}
                >
                  Rejection Reason:
                </p>
                <p
                  className="pmc-text-sm"
                  style={{
                    color: "#7f1d1d",
                    lineHeight: "1.7",
                    background: "rgba(127, 29, 29, 0.05)",
                    padding: "10px 12px",
                    borderRadius: "6px",
                    border: "1px solid rgba(127, 29, 29, 0.15)",
                  }}
                >
                  {rejectionComments}
                </p>
              </div>
            </div>
            {isResubmitMode && (
              <div
                style={{
                  paddingTop: "12px",
                  borderTop: "1px solid rgba(220, 38, 38, 0.2)",
                }}
              >
                <p
                  className="pmc-text-sm pmc-font-medium"
                  style={{ color: "#991b1b" }}
                >
                  📝 Please review the rejection comments above, make necessary
                  corrections to your application, and resubmit. Your
                  application will be reviewed again from the beginning.
                </p>
              </div>
            )}
          </div>
        )}

        <h1
          className="pmc-content-title"
          style={{
            color: "var(--pmc-gray-900)",
            fontSize: "24px",
            marginBottom: "4px",
          }}
        >
          {config.icon} {config.name} Registration
        </h1>
        <p
          className="pmc-content-subtitle"
          style={{ color: "var(--pmc-gray-600)", fontSize: "13px" }}
        >
          Complete all sections to register as a {config.name} with PMC
        </p>
      </div>

      <form onSubmit={handleSubmit} noValidate>
        {/* Success/Error Messages */}
        {success && (
          <div
            className="pmc-fadeIn"
            style={{
              padding: "14px 18px",
              marginBottom: "16px",
              background: "linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%)",
              border: "2px solid #86efac",
              borderLeft: "6px solid #10b981",
              borderRadius: "8px",
              color: "#166534",
              fontWeight: "600",
              display: "flex",
              alignItems: "center",
              gap: "12px",
              fontSize: "14px",
              boxShadow: "0 4px 12px rgba(16, 185, 129, 0.2)",
            }}
          >
            <svg
              style={{ width: "22px", height: "22px", flexShrink: 0 }}
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
            {success}
          </div>
        )}
        {error && (
          <div
            className="pmc-fadeIn"
            style={{
              padding: "14px 18px",
              marginBottom: "16px",
              background: "linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)",
              border: "2px solid #f87171",
              borderLeft: "6px solid #dc2626",
              borderRadius: "8px",
              color: "#991b1b",
              fontWeight: "600",
              display: "flex",
              alignItems: "flex-start",
              gap: "12px",
              fontSize: "14px",
              boxShadow: "0 4px 12px rgba(220, 38, 38, 0.2)",
              position: "sticky",
              top: "10px",
              zIndex: 100,
            }}
          >
            <svg
              style={{
                width: "22px",
                height: "22px",
                flexShrink: 0,
                marginTop: "1px",
              }}
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
            <div style={{ flex: 1 }}>
              <div
                style={{
                  marginBottom: validationErrors.length > 0 ? "8px" : "0",
                }}
              >
                {error}
              </div>
              {validationErrors.length > 0 && (
                <button
                  type="button"
                  onClick={() => setShowValidationPopup(true)}
                  style={{
                    padding: "6px 14px",
                    background: "#dc2626",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    fontSize: "12px",
                    fontWeight: "700",
                    cursor: "pointer",
                    transition: "all 0.2s",
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.background = "#b91c1c")
                  }
                  onMouseLeave={(e) =>
                    (e.currentTarget.style.background = "#dc2626")
                  }
                >
                  View {validationErrors.length} Error
                  {validationErrors.length > 1 ? "s" : ""}
                </button>
              )}
            </div>
          </div>
        )}

        <div>
          {/* Basic Information */}
          <div
            className="pmc-card pmc-slideInLeft"
            style={{ marginBottom: "16px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)",
                color: "white",
                padding: "14px 20px",
                borderBottom: "none",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "white",
                  display: "flex",
                  alignItems: "center",
                  gap: "10px",
                  fontSize: "17px",
                  fontWeight: "700",
                  margin: 0,
                }}
              >
                <span style={{ fontSize: "20px" }}>📋</span>
                Basic Information
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "rgba(255,255,255,0.9)",
                  fontSize: "12px",
                  margin: "4px 0 0 30px",
                }}
              >
                Position selection and registration fee details
              </p>
            </div>
            <div className="pmc-card-body" style={{ padding: "20px" }}>
              <div
                className="pmc-form-grid pmc-form-grid-2"
                style={{ gap: "16px" }}
              >
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Position Type
                  </label>
                  <select
                    className="pmc-input pmc-select"
                    style={{ fontSize: "14px", fontWeight: "600" }}
                    value={formData.positionType}
                    onChange={(e) =>
                      handleInputChange("positionType", Number(e.target.value))
                    }
                    required
                  >
                    {positionOptions.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </div>
                {config.fee > 0 && (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      padding: "10px 18px",
                      background:
                        "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
                      borderRadius: "8px",
                      border: "2px solid #fbbf24",
                    }}
                  >
                    <span
                      style={{
                        fontWeight: "700",
                        color: "#92400e",
                        fontSize: "15px",
                      }}
                    >
                      💰 Registration Fee: ₹{config.fee} for{" "}
                      {config.feeDuration}
                    </span>
                  </div>
                )}
                {config.fee === 0 && (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      padding: "10px 18px",
                      background:
                        "linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%)",
                      borderRadius: "8px",
                      border: "2px solid #86efac",
                    }}
                  >
                    <span
                      style={{
                        fontWeight: "700",
                        color: "#166534",
                        fontSize: "15px",
                      }}
                    >
                      ✓ No Registration Fee
                    </span>
                  </div>
                )}
              </div>

              {/* Qualifications Info Box - More Compact */}
              <details
                style={{
                  marginTop: "16px",
                  padding: "14px",
                  background:
                    "linear-gradient(135deg, #fffbeb 0%, #fef3c7 100%)",
                  borderRadius: "8px",
                  fontSize: "12px",
                  lineHeight: "1.6",
                  border: "2px solid #fde68a",
                }}
              >
                <summary
                  style={{
                    fontWeight: "700",
                    marginBottom: "8px",
                    cursor: "pointer",
                    fontSize: "13px",
                    color: "#92400e",
                    listStyle: "none",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <span>▶</span> View Qualification Requirements & Documents
                </summary>
                <div
                  style={{
                    marginTop: "12px",
                    paddingTop: "12px",
                    borderTop: "1px solid #fde68a",
                  }}
                >
                  <h3
                    style={{
                      fontWeight: 600,
                      marginBottom: "6px",
                      fontSize: "13px",
                    }}
                  >
                    1. Qualifications
                  </h3>
                  <div
                    style={{
                      paddingLeft: "10px",
                      margin: "6px 0",
                      whiteSpace: "pre-line",
                    }}
                  >
                    {config.qualificationInfo}
                  </div>

                  {config.scope && (
                    <>
                      <h3
                        style={{
                          fontWeight: 600,
                          marginTop: "10px",
                          marginBottom: "6px",
                          fontSize: "13px",
                        }}
                      >
                        2. Scope of Work
                      </h3>
                      <div
                        style={{
                          margin: "4px 0",
                          paddingLeft: "10px",
                          whiteSpace: "pre-line",
                        }}
                      >
                        {config.scope}
                      </div>
                    </>
                  )}

                  <h3
                    style={{
                      fontWeight: 600,
                      marginTop: "10px",
                      marginBottom: "6px",
                      fontSize: "13px",
                    }}
                  >
                    {config.scope ? "3" : "2"}. Duties and Responsibilities
                  </h3>
                  <p style={{ margin: "4px 0", paddingLeft: "10px" }}>
                    It will be incumbent on every architect / licensed technical
                    personnel to assist and co-operate with the Metropolitan
                    Commissioner and other Officers in carrying out and
                    enforcing the provisions of Maharashtra Regional & Town
                    Planning Act, 1966.
                  </p>

                  <h3
                    style={{
                      fontWeight: 600,
                      marginTop: "10px",
                      marginBottom: "6px",
                      fontSize: "13px",
                    }}
                  >
                    {config.scope ? "4" : "3"}. Documents Required for{" "}
                    {config.name}
                  </h3>
                  <ol style={{ paddingLeft: "20px", margin: "6px 0" }}>
                    {config.documentsRequired.map((doc, idx) => (
                      <li key={idx} style={{ marginBottom: "4px" }}>
                        {doc}
                      </li>
                    ))}
                  </ol>
                </div>
              </details>
            </div>
          </div>

          {/* Personal Details */}
          <div
            className="pmc-card pmc-slideInRight"
            style={{ marginBottom: "16px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                color: "white",
                padding: "14px 20px",
                borderBottom: "none",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
              }}
            >
              <div>
                <h2
                  className="pmc-card-title"
                  style={{
                    color: "white",
                    display: "flex",
                    alignItems: "center",
                    gap: "10px",
                    fontSize: "17px",
                    fontWeight: "700",
                    margin: 0,
                  }}
                >
                  <span style={{ fontSize: "22px" }}>👤</span>
                  Personal Details
                </h2>
                <p
                  className="pmc-card-subtitle"
                  style={{
                    color: "rgba(255,255,255,0.9)",
                    fontSize: "12px",
                    margin: "4px 0 0 32px",
                  }}
                >
                  Your personal information for identification
                </p>
              </div>
            </div>
            <div className="pmc-card-body" style={{ padding: "20px" }}>
              <div
                className="pmc-form-grid pmc-form-grid-3"
                style={{ gap: "16px" }}
              >
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    First Name
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.firstName) ||
                      hasFieldError("firstName")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.firstName}
                    onChange={(e) =>
                      handleInputChange("firstName", e.target.value)
                    }
                    placeholder="Enter first name"
                    required
                  />
                  {attemptedSubmit && !formData.firstName && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      First name is required
                    </span>
                  )}
                  {hasFieldError("firstName") && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      {fieldErrors["firstname"]}
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Middle Name
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
                    style={{ fontSize: "14px" }}
                    value={formData.middleName}
                    onChange={(e) =>
                      handleInputChange("middleName", e.target.value)
                    }
                    placeholder="Enter middle name (optional)"
                  />
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Last Name
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.lastName) ||
                      hasFieldError("lastName")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.lastName}
                    onChange={(e) =>
                      handleInputChange("lastName", e.target.value)
                    }
                    placeholder="Enter last name"
                    required
                  />
                  {attemptedSubmit && !formData.lastName && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Last name is required
                    </span>
                  )}
                  {hasFieldError("lastName") && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      {fieldErrors["lastname"]}
                    </span>
                  )}
                </div>
              </div>

              <div
                className="pmc-form-grid pmc-form-grid-3"
                style={{ gap: "16px", marginTop: "16px" }}
              >
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Mother's Name
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.motherName) ||
                      hasFieldError("motherName")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.motherName}
                    onChange={(e) =>
                      handleInputChange("motherName", e.target.value)
                    }
                    placeholder="Enter mother's name"
                    required
                  />
                  {attemptedSubmit && !formData.motherName && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Mother's name is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Mobile Number
                  </label>
                  <input
                    type="tel"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.mobileNumber) ||
                      hasFieldError("mobileNumber")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.mobileNumber}
                    onChange={(e) =>
                      handleInputChange("mobileNumber", e.target.value)
                    }
                    placeholder="10-digit mobile number"
                    maxLength={10}
                    required
                  />
                  {attemptedSubmit && !formData.mobileNumber && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Mobile number is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Email Address
                  </label>
                  <input
                    type="email"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.emailAddress) ||
                      hasFieldError("emailAddress")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.emailAddress}
                    onChange={(e) =>
                      handleInputChange("emailAddress", e.target.value)
                    }
                    placeholder="your.email@example.com"
                    required
                  />
                  {attemptedSubmit && !formData.emailAddress && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Email address is required
                    </span>
                  )}
                </div>
              </div>

              <div
                className="pmc-form-grid pmc-form-grid-4"
                style={{ gap: "16px", marginTop: "16px" }}
              >
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Date of Birth
                  </label>
                  <input
                    type="date"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.dateOfBirth) ||
                      hasFieldError("dateOfBirth")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.dateOfBirth}
                    onChange={(e) =>
                      handleInputChange("dateOfBirth", e.target.value)
                    }
                    max={getMaxBirthDate()}
                    required
                  />
                  {attemptedSubmit && !formData.dateOfBirth && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Date of birth is required
                    </span>
                  )}
                  {hasFieldError("dateOfBirth") && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      {fieldErrors["dateofbirth"]}
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Blood Group
                  </label>
                  <select
                    className={`pmc-input pmc-select ${
                      attemptedSubmit && !formData.bloodGroup
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.bloodGroup}
                    onChange={(e) =>
                      handleInputChange("bloodGroup", e.target.value)
                    }
                    required
                  >
                    <option value="">Select</option>
                    {bloodGroupOptions.map((bg) => (
                      <option key={bg} value={bg}>
                        {bg}
                      </option>
                    ))}
                  </select>
                  {attemptedSubmit && !formData.bloodGroup && (
                    <span
                      className="pmc-text-error"
                      style={{ fontSize: "12px", marginTop: "4px" }}
                    >
                      Blood group is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Height (cm)
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      (!formData.height || formData.height <= 0)
                        ? "pmc-input-error"
                        : ""
                    }`}
                    style={{ fontSize: "14px" }}
                    value={formData.height || ""}
                    onChange={(e) =>
                      handleInputChange("height", parseFloat(e.target.value))
                    }
                    placeholder="e.g., 170.5"
                    required
                  />
                  {attemptedSubmit &&
                    (!formData.height || formData.height <= 0) && (
                      <span
                        className="pmc-text-error"
                        style={{ fontSize: "12px", marginTop: "4px" }}
                      >
                        Height is required
                      </span>
                    )}
                </div>
                <div className="pmc-form-group">
                  <label
                    className="pmc-label pmc-label-required"
                    style={{
                      fontSize: "13px",
                      fontWeight: "600",
                      marginBottom: "6px",
                    }}
                  >
                    Gender
                  </label>
                  <div
                    style={{
                      display: "flex",
                      gap: "12px",
                      marginTop: "8px",
                      height: "38px",
                      alignItems: "center",
                    }}
                  >
                    <label
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "6px",
                        cursor: "pointer",
                        fontSize: "14px",
                      }}
                    >
                      <input
                        type="radio"
                        name="gender"
                        value={Gender.Male}
                        checked={formData.gender === Gender.Male}
                        onChange={(e) =>
                          handleInputChange("gender", Number(e.target.value))
                        }
                        className="pmc-radio"
                      />
                      <span>Male</span>
                    </label>
                    <label
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "6px",
                        cursor: "pointer",
                        fontSize: "14px",
                      }}
                    >
                      <input
                        type="radio"
                        name="gender"
                        value={Gender.Female}
                        checked={formData.gender === Gender.Female}
                        onChange={(e) =>
                          handleInputChange("gender", Number(e.target.value))
                        }
                        className="pmc-radio"
                      />
                      <span>Female</span>
                    </label>
                    <label
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "6px",
                        cursor: "pointer",
                        fontSize: "14px",
                      }}
                    >
                      <input
                        type="radio"
                        name="gender"
                        value={Gender.Other}
                        checked={formData.gender === Gender.Other}
                        onChange={(e) =>
                          handleInputChange("gender", Number(e.target.value))
                        }
                        className="pmc-radio"
                      />
                      <span>Other</span>
                    </label>
                  </div>
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-2">
                {config.sections.propertyTaxReceipt && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - Property Tax Receipt / Copy Of Rent Agreement /
                      Electricity Bill
                    </label>
                    {/* Show existing document if available */}
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.PropertyTaxReceipt &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file // Only show for existing docs, not newly uploaded ones
                    ) && (
                      <div
                        style={{
                          padding: "8px 12px",
                          marginBottom: "8px",
                          background: "#dcfce7",
                          border: "1px solid #86efac",
                          borderRadius: "6px",
                          fontSize: "13px",
                          color: "#166534",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <span style={{ fontSize: "16px" }}>✓</span>
                        <span>
                          Document already uploaded:{" "}
                          {formData.documents.find(
                            (d) =>
                              d.documentType ===
                              SEDocumentType.PropertyTaxReceipt
                          )?.fileName || "Previous upload"}
                        </span>
                      </div>
                    )}
                    <input
                      type="file"
                      className={`pmc-input ${
                        attemptedSubmit &&
                        !formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.PropertyTaxReceipt
                        )
                          ? "pmc-input-error"
                          : ""
                      }`}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.PropertyTaxReceipt,
                            "DOC_PROPERTY_TAX",
                            file
                          );
                        }
                      }}
                      accept=".pdf,.jpg,.jpeg,.png"
                    />
                    <span className="pmc-help-text">
                      Max file size: 500KB
                      {formData.documents.find(
                        (d) =>
                          d.documentType ===
                            SEDocumentType.PropertyTaxReceipt &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file
                      )
                        ? " (Upload new file to replace existing)"
                        : ""}
                    </span>
                    {attemptedSubmit &&
                      !formData.documents.find(
                        (d) =>
                          d.documentType === SEDocumentType.PropertyTaxReceipt
                      ) && (
                        <span className="pmc-text-error">
                          Property tax receipt / rent agreement / electricity
                          bill is required
                        </span>
                      )}
                  </div>
                )}
                {config.sections.isseCertificate && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - Indian Society Of Structural Engineer Certificate
                      (PDF File Upto 500 KB)
                    </label>
                    {/* Show existing document if available */}
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.ISSECertificate &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    ) && (
                      <div
                        style={{
                          padding: "8px 12px",
                          marginBottom: "8px",
                          background: "#dcfce7",
                          border: "1px solid #86efac",
                          borderRadius: "6px",
                          fontSize: "13px",
                          color: "#166534",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <span style={{ fontSize: "16px" }}>✓</span>
                        <span>
                          Document already uploaded:{" "}
                          {formData.documents.find(
                            (d) =>
                              d.documentType === SEDocumentType.ISSECertificate
                          )?.fileName || "Previous upload"}
                        </span>
                      </div>
                    )}
                    <input
                      type="file"
                      className={`pmc-input ${
                        attemptedSubmit &&
                        !formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.ISSECertificate
                        )
                          ? "pmc-input-error"
                          : ""
                      }`}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.ISSECertificate,
                            "DOC_ISSE",
                            file
                          );
                        }
                      }}
                      accept=".pdf"
                    />
                    <span className="pmc-help-text">
                      Max file size: 500KB
                      {formData.documents.find(
                        (d) =>
                          d.documentType === SEDocumentType.ISSECertificate &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file
                      )
                        ? " (Upload new file to replace existing)"
                        : ""}
                    </span>
                    {attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.ISSECertificate
                      ) && (
                        <span className="pmc-text-error">
                          ISSE certificate is required
                        </span>
                      )}
                  </div>
                )}
                {config.sections.coaCertificate && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - Council of Architecture Certificate (PDF File
                      Upto 500 KB)
                    </label>
                    {/* Show existing document if available */}
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.COACertificate &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    ) && (
                      <div
                        style={{
                          padding: "8px 12px",
                          marginBottom: "8px",
                          background: "#dcfce7",
                          border: "1px solid #86efac",
                          borderRadius: "6px",
                          fontSize: "13px",
                          color: "#166534",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <span style={{ fontSize: "16px" }}>✓</span>
                        <span>
                          Document already uploaded:{" "}
                          {formData.documents.find(
                            (d) =>
                              d.documentType === SEDocumentType.COACertificate
                          )?.fileName || "Previous upload"}
                        </span>
                      </div>
                    )}
                    <input
                      type="file"
                      className={`pmc-input ${
                        attemptedSubmit &&
                        !formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.COACertificate
                        )
                          ? "pmc-input-error"
                          : ""
                      }`}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.COACertificate,
                            "DOC_COA",
                            file
                          );
                        }
                      }}
                      accept=".pdf"
                    />
                    <span className="pmc-help-text">
                      Max file size: 500KB
                      {formData.documents.find(
                        (d) =>
                          d.documentType === SEDocumentType.COACertificate &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file
                      )
                        ? " (Upload new file to replace existing)"
                        : ""}
                    </span>
                    {attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.COACertificate
                      ) && (
                        <span className="pmc-text-error">
                          Council of Architecture certificate is required
                        </span>
                      )}
                  </div>
                )}
                {config.sections.ugcRecognition && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - UGC Recognition Certificate (PDF File Upto 500
                      KB)
                    </label>
                    {/* Show existing document if available */}
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.UGCRecognition &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    ) && (
                      <div
                        style={{
                          padding: "8px 12px",
                          marginBottom: "8px",
                          background: "#dcfce7",
                          border: "1px solid #86efac",
                          borderRadius: "6px",
                          fontSize: "13px",
                          color: "#166534",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <span style={{ fontSize: "16px" }}>✓</span>
                        <span>
                          Document already uploaded:{" "}
                          {formData.documents.find(
                            (d) =>
                              d.documentType === SEDocumentType.UGCRecognition
                          )?.fileName || "Previous upload"}
                        </span>
                      </div>
                    )}
                    <input
                      type="file"
                      className={`pmc-input ${
                        attemptedSubmit &&
                        !formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.UGCRecognition
                        )
                          ? "pmc-input-error"
                          : ""
                      }`}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.UGCRecognition,
                            "DOC_UGC",
                            file
                          );
                        }
                      }}
                      accept=".pdf"
                    />
                    <span className="pmc-help-text">
                      Max file size: 500KB
                      {formData.documents.find(
                        (d) =>
                          d.documentType === SEDocumentType.UGCRecognition &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file
                      )
                        ? " (Upload new file to replace existing)"
                        : ""}
                    </span>
                    {attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.UGCRecognition
                      ) && (
                        <span className="pmc-text-error">
                          UGC Recognition certificate is required
                        </span>
                      )}
                  </div>
                )}
                {config.sections.aicteApproval && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - AICTE Approval Certificate (PDF File Upto 500 KB)
                    </label>
                    {/* Show existing document if available */}
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.AICTEApproval &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    ) && (
                      <div
                        style={{
                          padding: "8px 12px",
                          marginBottom: "8px",
                          background: "#dcfce7",
                          border: "1px solid #86efac",
                          borderRadius: "6px",
                          fontSize: "13px",
                          color: "#166534",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <span style={{ fontSize: "16px" }}>✓</span>
                        <span>
                          Document already uploaded:{" "}
                          {formData.documents.find(
                            (d) =>
                              d.documentType === SEDocumentType.AICTEApproval
                          )?.fileName || "Previous upload"}
                        </span>
                      </div>
                    )}
                    <input
                      type="file"
                      className={`pmc-input ${
                        attemptedSubmit &&
                        !formData.documents.find(
                          (d) => d.documentType === SEDocumentType.AICTEApproval
                        )
                          ? "pmc-input-error"
                          : ""
                      }`}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.AICTEApproval,
                            "DOC_AICTE",
                            file
                          );
                        }
                      }}
                      accept=".pdf"
                    />
                    <span className="pmc-help-text">
                      Max file size: 500KB
                      {formData.documents.find(
                        (d) =>
                          d.documentType === SEDocumentType.AICTEApproval &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file
                      )
                        ? " (Upload new file to replace existing)"
                        : ""}
                    </span>
                    {attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.AICTEApproval
                      ) && (
                        <span className="pmc-text-error">
                          AICTE Approval certificate is required
                        </span>
                      )}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Local Address */}
          <div
            className="pmc-card pmc-slideInLeft"
            style={{ marginBottom: "12px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "#334155",
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  fontSize: "16px",
                  fontWeight: "600",
                  margin: 0,
                }}
              >
                <span style={{ fontSize: "24px" }}>🏠</span>
                Local Address
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Provide your current residential address
              </p>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Flat/House Number
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.currentAddress.addressLine1
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.currentAddress.addressLine1}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "addressLine1",
                        e.target.value
                      )
                    }
                    required
                  />
                  {attemptedSubmit && !formData.currentAddress.addressLine1 && (
                    <span className="pmc-text-error">
                      Local address line 1 is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Street Address
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.currentAddress.addressLine2
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.currentAddress.addressLine2}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "addressLine2",
                        e.target.value
                      )
                    }
                    required
                  />
                  {attemptedSubmit && !formData.currentAddress.addressLine2 && (
                    <span className="pmc-text-error">
                      Local address line 2 is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label">Address Line</label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.currentAddress.addressLine3}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "addressLine3",
                        e.target.value
                      )
                    }
                  />
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Country
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.currentAddress.country}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "country",
                        e.target.value
                      )
                    }
                    disabled
                    readOnly
                    style={{
                      backgroundColor: "#f1f5f9",
                      cursor: "not-allowed",
                    }}
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">State</label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.currentAddress.state
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.currentAddress.state}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "state",
                        e.target.value
                      )
                    }
                    required
                  />
                  {attemptedSubmit && !formData.currentAddress.state && (
                    <span className="pmc-text-error">State is required</span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">City</label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.currentAddress.city
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.currentAddress.city}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "city",
                        e.target.value
                      )
                    }
                    required
                  />
                  {attemptedSubmit && !formData.currentAddress.city && (
                    <span className="pmc-text-error">City is required</span>
                  )}
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Postal Code
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.currentAddress.pinCode
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.currentAddress.pinCode}
                    onChange={(e) =>
                      handleAddressChange(
                        "currentAddress",
                        "pinCode",
                        e.target.value
                      )
                    }
                    required
                  />
                  {attemptedSubmit && !formData.currentAddress.pinCode && (
                    <span className="pmc-text-error">
                      Postal code is required
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Permanent Address */}
          <div
            className="pmc-card pmc-slideInRight"
            style={{ marginBottom: "12px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "#334155",
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  fontSize: "16px",
                  fontWeight: "600",
                  margin: 0,
                }}
              >
                <span style={{ fontSize: "24px" }}>🏘️</span>
                Permanent Address
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Enter your permanent residence details
              </p>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-group">
                <label
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    cursor: "pointer",
                  }}
                >
                  <input
                    type="checkbox"
                    checked={permanentSameAsLocal}
                    onChange={(e) =>
                      handlePermanentSameAsLocal(e.target.checked)
                    }
                    className="pmc-checkbox"
                  />
                  <span>Permanent Same As Local</span>
                </label>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Flat/House Number
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !permanentSameAsLocal &&
                      !formData.permanentAddress.addressLine1
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.permanentAddress.addressLine1}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "addressLine1",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                    required
                  />
                  {attemptedSubmit &&
                    !permanentSameAsLocal &&
                    !formData.permanentAddress.addressLine1 && (
                      <span className="pmc-text-error">
                        Permanent address line 1 is required
                      </span>
                    )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Street Address
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !permanentSameAsLocal &&
                      !formData.permanentAddress.addressLine2
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.permanentAddress.addressLine2}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "addressLine2",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                    required
                  />
                  {attemptedSubmit &&
                    !permanentSameAsLocal &&
                    !formData.permanentAddress.addressLine2 && (
                      <span className="pmc-text-error">
                        Permanent address line 2 is required
                      </span>
                    )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label">Address</label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.permanentAddress.addressLine3}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "addressLine3",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                  />
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Country
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.permanentAddress.country}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "country",
                        e.target.value
                      )
                    }
                    disabled
                    readOnly
                    style={{
                      backgroundColor: "#f1f5f9",
                      cursor: "not-allowed",
                    }}
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">State</label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !permanentSameAsLocal &&
                      !formData.permanentAddress.state
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.permanentAddress.state}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "state",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                    required
                  />
                  {attemptedSubmit &&
                    !permanentSameAsLocal &&
                    !formData.permanentAddress.state && (
                      <span className="pmc-text-error">State is required</span>
                    )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">City</label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !permanentSameAsLocal &&
                      !formData.permanentAddress.city
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.permanentAddress.city}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "city",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                    required
                  />
                  {attemptedSubmit &&
                    !permanentSameAsLocal &&
                    !formData.permanentAddress.city && (
                      <span className="pmc-text-error">City is required</span>
                    )}
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Postal Code
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !permanentSameAsLocal &&
                      !formData.permanentAddress.pinCode
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.permanentAddress.pinCode}
                    onChange={(e) =>
                      handleAddressChange(
                        "permanentAddress",
                        "pinCode",
                        e.target.value
                      )
                    }
                    disabled={permanentSameAsLocal}
                    required
                  />
                  {attemptedSubmit &&
                    !permanentSameAsLocal &&
                    !formData.permanentAddress.pinCode && (
                      <span className="pmc-text-error">
                        Postal code is required
                      </span>
                    )}
                </div>
              </div>
            </div>
          </div>

          {/* PAN Information */}
          <div
            className="pmc-card pmc-slideInLeft"
            style={{ marginBottom: "12px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "#334155",
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  fontSize: "16px",
                  fontWeight: "600",
                  margin: 0,
                }}
              >
                <span style={{ fontSize: "24px" }}>🆔</span>
                PAN Information
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Enter your Permanent Account Number details
              </p>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    PAN Card Number
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.panCardNumber
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.panCardNumber}
                    onChange={(e) =>
                      handleInputChange(
                        "panCardNumber",
                        e.target.value.toUpperCase()
                      )
                    }
                    pattern="[A-Z]{5}[0-9]{4}[A-Z]{1}"
                    maxLength={10}
                    placeholder="ABCDE1234F"
                    required
                  />
                  {attemptedSubmit && !formData.panCardNumber && (
                    <span className="pmc-text-error">
                      PAN card number is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Upload PAN Card Attachment (Max 500KB)
                  </label>
                  {/* Show existing document if available */}
                  {formData.documents.find(
                    (d) =>
                      d.documentType === SEDocumentType.PanCard &&
                      (d.fileBase64 || d.filePath) &&
                      !d.file
                  ) && (
                    <div
                      style={{
                        padding: "8px 12px",
                        marginBottom: "8px",
                        background: "#dcfce7",
                        border: "1px solid #86efac",
                        borderRadius: "6px",
                        fontSize: "13px",
                        color: "#166534",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                      }}
                    >
                      <span style={{ fontSize: "16px" }}>✓</span>
                      <span>
                        Document already uploaded:{" "}
                        {formData.documents.find(
                          (d) => d.documentType === SEDocumentType.PanCard
                        )?.fileName || "Previous upload"}
                      </span>
                    </div>
                  )}
                  <input
                    type="file"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.PanCard
                      )
                        ? "pmc-input-error"
                        : ""
                    }`}
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) {
                        handleFileUpload(
                          SEDocumentType.PanCard,
                          "DOC_PAN",
                          file
                        );
                      }
                    }}
                    accept=".pdf,.jpg,.jpeg,.png"
                  />
                  <span className="pmc-help-text">
                    Max file size: 500KB
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.PanCard &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    )
                      ? " (Upload new file to replace existing)"
                      : ""}
                  </span>
                  {attemptedSubmit &&
                    !formData.documents.find(
                      (d) => d.documentType === SEDocumentType.PanCard
                    ) && (
                      <span className="pmc-text-error">
                        PAN card document is required
                      </span>
                    )}
                </div>
              </div>
            </div>
          </div>

          {/* Aadhar Information */}
          <div
            className="pmc-card pmc-slideInRight"
            style={{ marginBottom: "12px" }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "#334155",
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  fontSize: "16px",
                  fontWeight: "600",
                  margin: 0,
                }}
              >
                <span style={{ fontSize: "24px" }}>🪪</span>
                Aadhar Information
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Provide your Aadhaar card details
              </p>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Aadhar Number
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      attemptedSubmit && !formData.aadharCardNumber
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.aadharCardNumber}
                    onChange={(e) =>
                      handleInputChange(
                        "aadharCardNumber",
                        e.target.value.replace(/\D/g, "")
                      )
                    }
                    pattern="[0-9]{12}"
                    maxLength={12}
                    placeholder="123456789012"
                    required
                  />
                  {attemptedSubmit && !formData.aadharCardNumber && (
                    <span className="pmc-text-error">
                      Aadhar number is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Upload Aadhar Attachment (Max 500KB)
                  </label>
                  {/* Show existing document if available */}
                  {formData.documents.find(
                    (d) =>
                      d.documentType === SEDocumentType.AadharCard &&
                      (d.fileBase64 || d.filePath) &&
                      !d.file
                  ) && (
                    <div
                      style={{
                        padding: "8px 12px",
                        marginBottom: "8px",
                        background: "#dcfce7",
                        border: "1px solid #86efac",
                        borderRadius: "6px",
                        fontSize: "13px",
                        color: "#166534",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                      }}
                    >
                      <span style={{ fontSize: "16px" }}>✓</span>
                      <span>
                        Document already uploaded:{" "}
                        {formData.documents.find(
                          (d) => d.documentType === SEDocumentType.AadharCard
                        )?.fileName || "Previous upload"}
                      </span>
                    </div>
                  )}
                  <input
                    type="file"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.AadharCard
                      )
                        ? "pmc-input-error"
                        : ""
                    }`}
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) {
                        handleFileUpload(
                          SEDocumentType.AadharCard,
                          "DOC_AADHAAR",
                          file
                        );
                      }
                    }}
                    accept=".pdf,.jpg,.jpeg,.png"
                  />
                  <span className="pmc-help-text">
                    Max file size: 500KB
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.AadharCard &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    )
                      ? " (Upload new file to replace existing)"
                      : ""}
                  </span>
                  {attemptedSubmit &&
                    !formData.documents.find(
                      (d) => d.documentType === SEDocumentType.AadharCard
                    ) && (
                      <span className="pmc-text-error">
                        Aadhar card document is required
                      </span>
                    )}
                </div>
              </div>
            </div>
          </div>

          {/* Qualifications */}
          {config.sections.qualifications && (
            <div
              className="pmc-card pmc-slideInLeft"
              style={{ marginBottom: "12px" }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                  color: "#334155",
                  padding: "12px 16px",
                  borderBottom: "2px solid #cbd5e1",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                  }}
                >
                  <div>
                    <h2
                      className="pmc-card-title"
                      style={{
                        color: "#334155",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                        fontSize: "16px",
                        fontWeight: "600",
                        margin: 0,
                      }}
                    >
                      <span style={{ fontSize: "24px" }}>🎓</span>
                      Qualification
                    </h2>
                    <p
                      className="pmc-card-subtitle"
                      style={{
                        color: "#64748b",
                        fontSize: "13px",
                        margin: "2px 0 0 0",
                      }}
                    >
                      Add your educational qualifications
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={addQualification}
                    className="pmc-button pmc-button-light pmc-button-sm"
                    style={{
                      background: "white",
                      color: "var(--pmc-primary)",
                      border: "none",
                      fontWeight: "600",
                    }}
                  >
                    + Add
                  </button>
                </div>
              </div>
              <div className="pmc-card-body">
                {formData.qualifications.map((qual, index) => (
                  <div
                    key={qual.fileId}
                    style={{
                      marginBottom: "12px",
                      padding: "20px",
                      background: "#f8fafc",
                      borderRadius: "12px",
                      border: "1px solid #e2e8f0",
                      position: "relative",
                    }}
                  >
                    {formData.qualifications.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeQualification(index)}
                        className="pmc-button pmc-button-danger pmc-button-sm"
                        style={{
                          position: "absolute",
                          top: "16px",
                          right: "16px",
                          background:
                            "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
                          color: "white",
                          border: "none",
                          padding: "8px 12px",
                          borderRadius: "8px",
                          cursor: "pointer",
                          fontSize: "18px",
                          display: "flex",
                          alignItems: "center",
                          gap: "4px",
                        }}
                        title="Remove this qualification"
                      >
                        🗑️
                      </button>
                    )}

                    <div className="pmc-form-grid pmc-form-grid-3">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Institute Name
                        </label>
                        <input
                          type="text"
                          className={`pmc-input ${
                            attemptedSubmit && !qual.instituteName
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={qual.instituteName}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "instituteName",
                              e.target.value
                            )
                          }
                          required
                        />
                        {attemptedSubmit && !qual.instituteName && (
                          <span className="pmc-text-error">
                            Institute name is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          University Name
                        </label>
                        <input
                          type="text"
                          className={`pmc-input ${
                            attemptedSubmit && !qual.universityName
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={qual.universityName}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "universityName",
                              e.target.value
                            )
                          }
                          required
                        />
                        {attemptedSubmit && !qual.universityName && (
                          <span className="pmc-text-error">
                            University name is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Course Specialization
                        </label>
                        <select
                          className="pmc-input pmc-select"
                          value={qual.specialization}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "specialization",
                              Number(e.target.value)
                            )
                          }
                          required
                        >
                          {specializationOptions.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                              {opt.label}
                            </option>
                          ))}
                        </select>
                      </div>
                    </div>

                    <div className="pmc-form-grid pmc-form-grid-2">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Upload Last Year Marksheet (Max 500KB)
                        </label>
                        {/* Show existing document if available - match by type for this qualification index */}
                        {(() => {
                          // Find marksheet documents
                          const marksheets = formData.documents.filter(
                            (d) =>
                              d.documentType === SEDocumentType.Marksheet &&
                              (d.fileBase64 || d.filePath) &&
                              !d.file // Only show for existing docs, not newly uploaded ones
                          );
                          // Show the one at this index if available
                          const marksheet = marksheets[index];
                          return marksheet ? (
                            <div
                              style={{
                                padding: "8px 12px",
                                marginBottom: "8px",
                                background: "#dcfce7",
                                border: "1px solid #86efac",
                                borderRadius: "6px",
                                fontSize: "13px",
                                color: "#166534",
                                display: "flex",
                                alignItems: "center",
                                gap: "8px",
                              }}
                            >
                              <span>✓</span>
                              <span>
                                Document already uploaded: {marksheet.fileName}
                              </span>
                            </div>
                          ) : null;
                        })()}
                        <input
                          type="file"
                          className={`pmc-input ${
                            attemptedSubmit &&
                            !formData.documents.find(
                              (d) =>
                                d.documentType === SEDocumentType.Marksheet &&
                                d.fileId === qual.fileId
                            )
                              ? "pmc-input-error"
                              : ""
                          }`}
                          onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) {
                              handleFileUpload(
                                SEDocumentType.Marksheet,
                                qual.fileId,
                                file
                              );
                            }
                          }}
                          accept=".pdf"
                        />
                        <span
                          style={{
                            fontSize: "12px",
                            color: "#64748b",
                            marginTop: "4px",
                            display: "block",
                          }}
                        >
                          Max file size: 500KB (Upload new file to replace
                          existing)
                        </span>
                        {attemptedSubmit &&
                          !formData.documents.find(
                            (d) =>
                              d.documentType === SEDocumentType.Marksheet &&
                              d.fileId === qual.fileId
                          ) && (
                            <span className="pmc-text-error">
                              Marksheet is required
                            </span>
                          )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Upload Certificate (Max 500KB)
                        </label>
                        {/* Show existing document if available - match by type for this qualification index */}
                        {(() => {
                          // Find degree certificate documents
                          const certificates = formData.documents.filter(
                            (d) =>
                              d.documentType ===
                                SEDocumentType.DegreeCertificate &&
                              (d.fileBase64 || d.filePath) &&
                              !d.file // Only show for existing docs, not newly uploaded ones
                          );
                          // Show the one at this index if available
                          const certificate = certificates[index];
                          return certificate ? (
                            <div
                              style={{
                                padding: "8px 12px",
                                marginBottom: "8px",
                                background: "#dcfce7",
                                border: "1px solid #86efac",
                                borderRadius: "6px",
                                fontSize: "13px",
                                color: "#166534",
                                display: "flex",
                                alignItems: "center",
                                gap: "8px",
                              }}
                            >
                              <span>✓</span>
                              <span>
                                Document already uploaded:{" "}
                                {certificate.fileName}
                              </span>
                            </div>
                          ) : null;
                        })()}
                        <input
                          type="file"
                          className={`pmc-input ${
                            attemptedSubmit &&
                            !formData.documents.find(
                              (d) =>
                                d.documentType ===
                                  SEDocumentType.DegreeCertificate &&
                                d.fileId === `${qual.fileId}_CERT`
                            )
                              ? "pmc-input-error"
                              : ""
                          }`}
                          onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) {
                              handleFileUpload(
                                SEDocumentType.DegreeCertificate,
                                `${qual.fileId}_CERT`,
                                file
                              );
                            }
                          }}
                          accept=".pdf"
                        />
                        <span
                          style={{
                            fontSize: "12px",
                            color: "#64748b",
                            marginTop: "4px",
                            display: "block",
                          }}
                        >
                          Max file size: 500KB (Upload new file to replace
                          existing)
                        </span>
                        {attemptedSubmit &&
                          !formData.documents.find(
                            (d) =>
                              d.documentType ===
                                SEDocumentType.DegreeCertificate &&
                              d.fileId === `${qual.fileId}_CERT`
                          ) && (
                            <span className="pmc-text-error">
                              Certificate is required
                            </span>
                          )}
                      </div>
                    </div>

                    <div className="pmc-form-grid pmc-form-grid-3">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Name of Degree
                        </label>
                        <input
                          type="text"
                          className={`pmc-input ${
                            attemptedSubmit && !qual.degreeName
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={qual.degreeName}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "degreeName",
                              e.target.value
                            )
                          }
                          required
                        />
                        {attemptedSubmit && !qual.degreeName && (
                          <span className="pmc-text-error">
                            Degree name is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Passing Month
                        </label>
                        <select
                          className="pmc-input pmc-select"
                          value={qual.passingMonth}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "passingMonth",
                              Number(e.target.value)
                            )
                          }
                          required
                        >
                          {monthOptions.map((month, idx) => {
                            const monthValue = idx + 1;
                            const yearOfPassing =
                              qual.yearOfPassing.split("-")[0];
                            const isCurrentYear =
                              yearOfPassing &&
                              parseInt(yearOfPassing) === getCurrentYear();
                            const isDisabled =
                              isCurrentYear && monthValue > getCurrentMonth();

                            return (
                              <option
                                key={idx}
                                value={monthValue}
                                disabled={isDisabled ? true : undefined}
                              >
                                {month}
                              </option>
                            );
                          })}
                        </select>
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Passing Year
                        </label>
                        <input
                          type="number"
                          className={`pmc-input ${
                            attemptedSubmit && !qual.yearOfPassing
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={qual.yearOfPassing.split("-")[0] || ""}
                          onChange={(e) =>
                            handleQualificationChange(
                              index,
                              "yearOfPassing",
                              `${e.target.value}-01-01T00:00:00.000Z`
                            )
                          }
                          min="1950"
                          max={new Date().getFullYear()}
                          required
                        />
                        {attemptedSubmit && !qual.yearOfPassing && (
                          <span className="pmc-text-error">
                            Passing year is required
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Experience */}
          {config.sections.experience && (
            <div
              className="pmc-card pmc-slideInRight"
              style={{ marginBottom: "12px" }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                  color: "#334155",
                  padding: "12px 16px",
                  borderBottom: "2px solid #cbd5e1",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                  }}
                >
                  <div>
                    <h2
                      className="pmc-card-title"
                      style={{
                        color: "#334155",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                        fontSize: "16px",
                        fontWeight: "600",
                        margin: 0,
                      }}
                    >
                      <span style={{ fontSize: "24px" }}>💼</span>
                      Experience
                    </h2>
                    <p
                      className="pmc-card-subtitle"
                      style={{
                        color: "#64748b",
                        fontSize: "13px",
                        margin: "2px 0 0 0",
                      }}
                    >
                      Add your professional work experience
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={addExperience}
                    className="pmc-button pmc-button-light pmc-button-sm"
                    style={{
                      background: "white",
                      color: "var(--pmc-primary)",
                      border: "none",
                      fontWeight: "600",
                    }}
                  >
                    + Add
                  </button>
                </div>
              </div>
              <div className="pmc-card-body">
                {formData.experiences.map((exp, index) => (
                  <div
                    key={exp.fileId}
                    style={{
                      marginBottom: "12px",
                      padding: "20px",
                      background: "#f8fafc",
                      borderRadius: "12px",
                      border: "1px solid #e2e8f0",
                      position: "relative",
                    }}
                  >
                    {formData.experiences.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeExperience(index)}
                        className="pmc-button pmc-button-danger pmc-button-sm"
                        style={{
                          position: "absolute",
                          top: "16px",
                          right: "16px",
                          background:
                            "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
                          color: "white",
                          border: "none",
                          padding: "8px 12px",
                          borderRadius: "8px",
                          cursor: "pointer",
                          fontSize: "18px",
                          display: "flex",
                          alignItems: "center",
                          gap: "4px",
                        }}
                        title="Remove this experience"
                      >
                        🗑️
                      </button>
                    )}

                    <div className="pmc-form-grid pmc-form-grid-3">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Company Name
                        </label>
                        <input
                          type="text"
                          className={`pmc-input ${
                            attemptedSubmit && !exp.companyName
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={exp.companyName}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "companyName",
                              e.target.value
                            )
                          }
                          required
                        />
                        {attemptedSubmit && !exp.companyName && (
                          <span className="pmc-text-error">
                            Company name is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Position
                        </label>
                        <input
                          type="text"
                          className={`pmc-input ${
                            attemptedSubmit && !exp.position
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={exp.position}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "position",
                              e.target.value
                            )
                          }
                          required
                        />
                        {attemptedSubmit && !exp.position && (
                          <span className="pmc-text-error">
                            Position is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Years of Experience
                        </label>
                        <input
                          type="number"
                          step="0.1"
                          className="pmc-input"
                          value={exp.yearsOfExperience || ""}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "yearsOfExperience",
                              parseFloat(e.target.value)
                            )
                          }
                          readOnly
                          disabled
                          style={{
                            backgroundColor: "#f1f5f9",
                            cursor: "not-allowed",
                          }}
                        />
                      </div>
                    </div>

                    <div className="pmc-form-grid pmc-form-grid-3">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          From Date
                        </label>
                        <input
                          type="date"
                          className={`pmc-input ${
                            attemptedSubmit && !exp.fromDate
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={exp.fromDate.split("T")[0] || ""}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "fromDate",
                              `${e.target.value}T00:00:00.000Z`
                            )
                          }
                          max={getMaxExperienceDate()}
                          required
                        />
                        {attemptedSubmit && !exp.fromDate && (
                          <span className="pmc-text-error">
                            From date is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          To Date
                        </label>
                        <input
                          type="date"
                          className={`pmc-input ${
                            attemptedSubmit && !exp.toDate
                              ? "pmc-input-error"
                              : ""
                          }`}
                          value={exp.toDate.split("T")[0] || ""}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "toDate",
                              `${e.target.value}T00:00:00.000Z`
                            )
                          }
                          max={getMaxExperienceDate()}
                          required
                        />
                        {attemptedSubmit && !exp.toDate && (
                          <span className="pmc-text-error">
                            To date is required
                          </span>
                        )}
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label">&nbsp;</label>
                        <button
                          type="button"
                          onClick={() => calculateExperience(index)}
                          className="pmc-button pmc-button-primary pmc-button-sm"
                          style={{ width: "100%" }}
                        >
                          Calculate Experience
                        </button>
                      </div>
                    </div>

                    <div className="pmc-form-group">
                      <label className="pmc-label pmc-label-required">
                        Upload Certificate (Max 500KB)
                      </label>
                      {/* Show existing document if available - match by type for this experience index */}
                      {(() => {
                        // Find experience certificate documents
                        const certificates = formData.documents.filter(
                          (d) =>
                            d.documentType ===
                              SEDocumentType.ExperienceCertificate &&
                            (d.fileBase64 || d.filePath) &&
                            !d.file // Only show for existing docs, not newly uploaded ones
                        );
                        // Show the one at this index if available
                        const certificate = certificates[index];
                        return certificate ? (
                          <div
                            style={{
                              padding: "8px 12px",
                              marginBottom: "8px",
                              background: "#dcfce7",
                              border: "1px solid #86efac",
                              borderRadius: "6px",
                              fontSize: "13px",
                              color: "#166534",
                              display: "flex",
                              alignItems: "center",
                              gap: "8px",
                            }}
                          >
                            <span>✓</span>
                            <span>
                              Document already uploaded: {certificate.fileName}
                            </span>
                          </div>
                        ) : null;
                      })()}
                      <input
                        type="file"
                        className={`pmc-input ${
                          attemptedSubmit &&
                          !formData.documents.find(
                            (d) =>
                              d.documentType ===
                                SEDocumentType.ExperienceCertificate &&
                              d.fileId === exp.fileId
                          )
                            ? "pmc-input-error"
                            : ""
                        }`}
                        onChange={(e) => {
                          const file = e.target.files?.[0];
                          if (file) {
                            handleFileUpload(
                              SEDocumentType.ExperienceCertificate,
                              exp.fileId,
                              file
                            );
                          }
                        }}
                        accept=".pdf"
                      />
                      <span
                        style={{
                          fontSize: "12px",
                          color: "#64748b",
                          marginTop: "4px",
                          display: "block",
                        }}
                      >
                        Max file size: 500KB (Upload new file to replace
                        existing)
                      </span>
                      {attemptedSubmit &&
                        !formData.documents.find(
                          (d) =>
                            d.documentType ===
                              SEDocumentType.ExperienceCertificate &&
                            d.fileId === exp.fileId
                        ) && (
                          <span className="pmc-text-error">
                            Experience certificate is required
                          </span>
                        )}
                    </div>

                    {exp.yearsOfExperience > 0 && (
                      <div
                        style={{
                          marginTop: "12px",
                          padding: "12px",
                          background: "#dbeafe",
                          borderRadius: "8px",
                          fontSize: "13px",
                          fontWeight: 600,
                          color: "#1e40af",
                        }}
                      >
                        Total Experience: {exp.yearsOfExperience} years
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Additional Documents */}
          {config.sections.additionalDocuments && (
            <div
              className="pmc-card pmc-slideInLeft"
              style={{ marginBottom: "12px" }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                  color: "#334155",
                  padding: "12px 16px",
                  borderBottom: "2px solid #cbd5e1",
                }}
              >
                <h2
                  className="pmc-card-title"
                  style={{
                    color: "#334155",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    fontSize: "16px",
                    fontWeight: "600",
                    margin: 0,
                  }}
                >
                  <span style={{ fontSize: "24px" }}>📄</span>
                  Additional Documents
                </h2>
                <p
                  className="pmc-card-subtitle"
                  style={{
                    color: "#64748b",
                    fontSize: "13px",
                    margin: "2px 0 0 0",
                  }}
                >
                  Upload any additional supporting documents
                </p>
              </div>
              <div className="pmc-card-body">
                {/* Show existing additional documents if any */}
                {formData.documents.filter(
                  (d) =>
                    d.documentType === SEDocumentType.AdditionalDocument &&
                    (d.fileBase64 || d.filePath) &&
                    !d.file // Only show existing docs, not newly uploaded ones
                ).length > 0 && (
                  <div style={{ marginBottom: "16px" }}>
                    <h4
                      style={{
                        fontSize: "14px",
                        fontWeight: 600,
                        marginBottom: "8px",
                        color: "#334155",
                      }}
                    >
                      Existing Additional Documents:
                    </h4>
                    {formData.documents
                      .filter(
                        (d) =>
                          d.documentType ===
                            SEDocumentType.AdditionalDocument &&
                          (d.fileBase64 || d.filePath) &&
                          !d.file // Only show existing docs, not newly uploaded ones
                      )
                      .map((doc, idx) => (
                        <div
                          key={idx}
                          style={{
                            padding: "8px 12px",
                            marginBottom: "8px",
                            background: "#dcfce7",
                            border: "1px solid #86efac",
                            borderRadius: "6px",
                            fontSize: "13px",
                            color: "#166534",
                            display: "flex",
                            alignItems: "center",
                            gap: "8px",
                          }}
                        >
                          <span>✓</span>
                          <span>
                            Document already uploaded: {doc.fileName}
                            {doc.documentName && (
                              <span
                                style={{
                                  fontStyle: "italic",
                                  marginLeft: "8px",
                                }}
                              >
                                ({doc.documentName})
                              </span>
                            )}
                          </span>
                        </div>
                      ))}
                  </div>
                )}
                <div className="pmc-form-grid pmc-form-grid-2">
                  <div className="pmc-form-group">
                    <label className="pmc-label">Document Name</label>
                    <input
                      type="text"
                      className="pmc-input"
                      placeholder="Enter document name"
                      value={additionalDocumentName}
                      onChange={(e) =>
                        setAdditionalDocumentName(e.target.value)
                      }
                    />
                  </div>
                  <div className="pmc-form-group">
                    <label className="pmc-label">
                      Upload Attachment (Max 500KB)
                    </label>
                    <input
                      type="file"
                      className="pmc-input"
                      id="additionalDocFileInput"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          // Use the document name as documentName and keep original filename
                          const documentName =
                            additionalDocumentName.trim() || undefined;

                          handleFileUpload(
                            SEDocumentType.AdditionalDocument,
                            `DOC_ADD_${Date.now()}`,
                            file,
                            file.name, // Keep original file name
                            documentName // Pass custom document name separately
                          );

                          // Clear only the document name field, keep the file input showing the filename
                          setAdditionalDocumentName("");
                        }
                      }}
                      accept=".pdf,.jpg,.jpeg,.png"
                    />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Self Declaration */}
          {config.sections.selfDeclaration && (
            <div
              className="pmc-card pmc-slideInRight"
              style={{ marginBottom: "12px" }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                  color: "#334155",
                  padding: "12px 16px",
                  borderBottom: "2px solid #cbd5e1",
                }}
              >
                <h2
                  className="pmc-card-title"
                  style={{
                    color: "#334155",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    fontSize: "16px",
                    fontWeight: "600",
                    margin: 0,
                  }}
                >
                  <span style={{ fontSize: "24px" }}>📋</span>
                  Self Declaration
                </h2>
                <p
                  className="pmc-card-subtitle"
                  style={{
                    color: "#64748b",
                    fontSize: "13px",
                    margin: "2px 0 0 0",
                  }}
                >
                  Download, fill, and upload the self declaration form
                </p>
              </div>
              <div className="pmc-card-body">
                <div className="pmc-form-group">
                  <p style={{ marginBottom: "12px" }}>
                    Self Declaration -{" "}
                    <a
                      href="/Self Declaration.pdf"
                      download="Self_Declaration_Form.pdf"
                      style={{ color: "#0c4a6e", fontWeight: 600 }}
                    >
                      Download Self Declaration Form
                    </a>
                  </p>
                  <label className="pmc-label pmc-label-required">
                    Upload Self Declaration
                  </label>
                  {/* Show existing document if available */}
                  {formData.documents.find(
                    (d) =>
                      d.documentType === SEDocumentType.SelfDeclaration &&
                      (d.fileBase64 || d.filePath) &&
                      !d.file
                  ) && (
                    <div
                      style={{
                        padding: "8px 12px",
                        marginBottom: "8px",
                        background: "#dcfce7",
                        border: "1px solid #86efac",
                        borderRadius: "6px",
                        fontSize: "13px",
                        color: "#166534",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                      }}
                    >
                      <span style={{ fontSize: "16px" }}>✓</span>
                      <span>
                        Document already uploaded:{" "}
                        {formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.SelfDeclaration
                        )?.fileName || "Previous upload"}
                      </span>
                    </div>
                  )}
                  <input
                    type="file"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.SelfDeclaration
                      )
                        ? "pmc-input-error"
                        : ""
                    }`}
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) {
                        handleFileUpload(
                          SEDocumentType.SelfDeclaration,
                          "DOC_SELF_DEC",
                          file
                        );
                      }
                    }}
                    accept=".pdf"
                  />
                  <span className="pmc-help-text">
                    Max file size: 500KB
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.SelfDeclaration &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    )
                      ? " (Upload new file to replace existing)"
                      : ""}
                  </span>
                  {attemptedSubmit &&
                    !formData.documents.find(
                      (d) => d.documentType === SEDocumentType.SelfDeclaration
                    ) && (
                      <span className="pmc-text-error">
                        Self declaration document is required
                      </span>
                    )}
                </div>
              </div>
            </div>
          )}

          {/* Upload Profile Picture */}
          {config.sections.profilePicture && (
            <div
              className="pmc-card pmc-slideInLeft"
              style={{ marginBottom: "12px" }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                  color: "#334155",
                  padding: "12px 16px",
                  borderBottom: "2px solid #cbd5e1",
                }}
              >
                <h2
                  className="pmc-card-title"
                  style={{
                    color: "#334155",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    fontSize: "16px",
                    fontWeight: "600",
                    margin: 0,
                  }}
                >
                  <span style={{ fontSize: "24px" }}>📷</span>
                  Upload Profile Picture
                </h2>
                <p
                  className="pmc-card-subtitle"
                  style={{
                    color: "#64748b",
                    fontSize: "13px",
                    margin: "2px 0 0 0",
                  }}
                >
                  Upload a clear photo (Max 500KB, JPG/PNG format)
                </p>
              </div>
              <div className="pmc-card-body">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Upload ProfilePicture (Max 500KB)
                  </label>
                  {/* Show existing document if available */}
                  {formData.documents.find(
                    (d) =>
                      d.documentType === SEDocumentType.ProfilePicture &&
                      (d.fileBase64 || d.filePath) &&
                      !d.file
                  ) && (
                    <div
                      style={{
                        padding: "8px 12px",
                        marginBottom: "8px",
                        background: "#dcfce7",
                        border: "1px solid #86efac",
                        borderRadius: "6px",
                        fontSize: "13px",
                        color: "#166534",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                      }}
                    >
                      <span style={{ fontSize: "16px" }}>✓</span>
                      <span>
                        Document already uploaded:{" "}
                        {formData.documents.find(
                          (d) =>
                            d.documentType === SEDocumentType.ProfilePicture
                        )?.fileName || "Previous upload"}
                      </span>
                    </div>
                  )}
                  <input
                    type="file"
                    className={`pmc-input ${
                      attemptedSubmit &&
                      !formData.documents.find(
                        (d) => d.documentType === SEDocumentType.ProfilePicture
                      )
                        ? "pmc-input-error"
                        : ""
                    }`}
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) {
                        handleFileUpload(
                          SEDocumentType.ProfilePicture,
                          "DOC_PROFILE",
                          file
                        );
                      }
                    }}
                    accept=".jpg,.jpeg,.png"
                  />
                  <span className="pmc-help-text">
                    Max file size: 500KB
                    {formData.documents.find(
                      (d) =>
                        d.documentType === SEDocumentType.ProfilePicture &&
                        (d.fileBase64 || d.filePath) &&
                        !d.file
                    )
                      ? " (Upload new file to replace existing)"
                      : ""}
                  </span>
                  {attemptedSubmit &&
                    !formData.documents.find(
                      (d) => d.documentType === SEDocumentType.ProfilePicture
                    ) && (
                      <span className="pmc-text-error">
                        Profile picture is required
                      </span>
                    )}
                </div>
              </div>
            </div>
          )}

          {/* Submit Buttons */}
          <div
            style={{
              display: "flex",
              justifyContent: "center",
              gap: "12px",
              marginTop: "24px",
              marginBottom: "12px",
              flexWrap: "wrap",
            }}
          >
            {/* Save as Draft Button */}
            <button
              type="button"
              onClick={handleSaveAsDraft}
              className="pmc-button pmc-button-lg"
              disabled={loading}
              style={{
                minWidth: "180px",
                background: loading
                  ? "linear-gradient(135deg, #94a3b8 0%, #64748b 100%)"
                  : "linear-gradient(135deg, #0f766e 0%, #115e59 100%)",
                border: "none",
                color: "white",
                fontWeight: "700",
                fontSize: "15px",
                padding: "12px 28px",
                borderRadius: "10px",
                cursor: loading ? "not-allowed" : "pointer",
                boxShadow: loading
                  ? "none"
                  : "0 8px 20px rgba(245, 158, 11, 0.3)",
                transition: "all 0.3s ease",
                transform: loading ? "none" : "translateY(0)",
              }}
              onMouseEnter={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 12px 25px rgba(245, 158, 11, 0.4)";
                }
              }}
              onMouseLeave={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 8px 20px rgba(245, 158, 11, 0.3)";
                }
              }}
            >
              {loading ? "⏳ Saving..." : "💾 SAVE AS DRAFT"}
            </button>

            {/* Cancel Button */}
            <button
              type="button"
              onClick={handleCancel}
              className="pmc-button pmc-button-lg"
              disabled={loading}
              style={{
                minWidth: "180px",
                background: loading
                  ? "linear-gradient(135deg, #94a3b8 0%, #64748b 100%)"
                  : "linear-gradient(135deg, #64748b 0%, #475569 100%)",
                border: "none",
                color: "white",
                fontWeight: "700",
                fontSize: "15px",
                padding: "12px 28px",
                borderRadius: "10px",
                cursor: loading ? "not-allowed" : "pointer",
                boxShadow: loading
                  ? "none"
                  : "0 8px 20px rgba(100, 116, 139, 0.3)",
                transition: "all 0.3s ease",
                transform: loading ? "none" : "translateY(0)",
              }}
              onMouseEnter={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 12px 25px rgba(100, 116, 139, 0.4)";
                }
              }}
              onMouseLeave={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 8px 20px rgba(100, 116, 139, 0.3)";
                }
              }}
            >
              ❌ CANCEL
            </button>

            {/* Submit Button */}
            <button
              type="submit"
              className="pmc-button pmc-button-lg"
              disabled={loading}
              style={{
                minWidth: "180px",
                background: loading
                  ? "linear-gradient(135deg, #94a3b8 0%, #64748b 100%)"
                  : "linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%)",
                border: "none",
                color: "white",
                fontWeight: "700",
                fontSize: "15px",
                padding: "12px 28px",
                borderRadius: "10px",
                cursor: loading ? "not-allowed" : "pointer",
                boxShadow: loading
                  ? "none"
                  : "0 8px 20px rgba(16, 185, 129, 0.3)",
                transition: "all 0.3s ease",
                transform: loading ? "none" : "translateY(0)",
              }}
              onMouseEnter={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 12px 25px rgba(16, 185, 129, 0.4)";
                }
              }}
              onMouseLeave={(e) => {
                if (!loading) {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 8px 20px rgba(16, 185, 129, 0.3)";
                }
              }}
            >
              {loading ? "⏳ Submitting..." : "✅ SUBMIT APPLICATION"}
            </button>
          </div>
        </div>
      </form>

      {/* Review Popup */}
      {showReviewPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.7)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            zIndex: 9999,
            padding: "20px",
          }}
          onClick={() => !isSubmitting && setShowReviewPopup(false)}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              maxWidth: "900px",
              width: "100%",
              maxHeight: "90vh",
              overflow: "auto",
              boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div
              style={{
                background: "linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%)",
                padding: "24px",
                borderTopLeftRadius: "16px",
                borderTopRightRadius: "16px",
                color: "white",
              }}
            >
              <h2 style={{ margin: 0, fontSize: "24px", fontWeight: "700" }}>
                📋 Review Application Details
              </h2>
              <p
                style={{ margin: "8px 0 0 0", opacity: 0.9, fontSize: "14px" }}
              >
                Please review all information before submitting
              </p>
            </div>

            {/* Content */}
            <div style={{ padding: "24px" }}>
              {/* Basic Information */}
              <div style={{ marginBottom: "24px" }}>
                <h3
                  style={{
                    fontSize: "18px",
                    fontWeight: "600",
                    marginBottom: "12px",
                    color: "#1e40af",
                  }}
                >
                  👤 Personal Information
                </h3>
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
                    gap: "12px",
                    background: "#f8fafc",
                    padding: "16px",
                    borderRadius: "8px",
                  }}
                >
                  <div>
                    <strong>Name:</strong> {formData.firstName}{" "}
                    {formData.middleName} {formData.lastName}
                  </div>
                  <div>
                    <strong>Mother's Name:</strong> {formData.motherName}
                  </div>
                  <div>
                    <strong>Date of Birth:</strong> {formData.dateOfBirth}
                  </div>
                  <div>
                    <strong>Gender:</strong>{" "}
                    {formData.gender === 0
                      ? "Male"
                      : formData.gender === 1
                      ? "Female"
                      : formData.gender === 2
                      ? "Other"
                      : "N/A"}
                  </div>
                  <div>
                    <strong>Blood Group:</strong> {formData.bloodGroup || "N/A"}
                  </div>
                  <div>
                    <strong>Height:</strong> {formData.height || "N/A"} cm
                  </div>
                  <div>
                    <strong>Mobile:</strong> {formData.mobileNumber}
                  </div>
                  <div>
                    <strong>Email:</strong> {formData.emailAddress}
                  </div>
                  <div>
                    <strong>PAN:</strong> {formData.panCardNumber}
                  </div>
                  <div>
                    <strong>Aadhar:</strong> {formData.aadharCardNumber}
                  </div>
                  {formData.coaCardNumber && (
                    <div>
                      <strong>COA Number:</strong> {formData.coaCardNumber}
                    </div>
                  )}
                </div>
              </div>

              {/* Local Address */}
              <div style={{ marginBottom: "24px" }}>
                <h3
                  style={{
                    fontSize: "18px",
                    fontWeight: "600",
                    marginBottom: "12px",
                    color: "#1e40af",
                  }}
                >
                  🏠 Current Address
                </h3>
                <div
                  style={{
                    background: "#f8fafc",
                    padding: "16px",
                    borderRadius: "8px",
                  }}
                >
                  <p style={{ margin: 0 }}>
                    {formData.currentAddress.addressLine1},{" "}
                    {formData.currentAddress.addressLine2 &&
                      `${formData.currentAddress.addressLine2}, `}
                    {formData.currentAddress.addressLine3 &&
                      `${formData.currentAddress.addressLine3}, `}
                    {formData.currentAddress.city},{" "}
                    {formData.currentAddress.state},{" "}
                    {formData.currentAddress.country} -{" "}
                    {formData.currentAddress.pinCode}
                  </p>
                </div>
              </div>

              {/* Permanent Address */}
              <div style={{ marginBottom: "24px" }}>
                <h3
                  style={{
                    fontSize: "18px",
                    fontWeight: "600",
                    marginBottom: "12px",
                    color: "#1e40af",
                  }}
                >
                  🏡 Permanent Address
                </h3>
                <div
                  style={{
                    background: "#f8fafc",
                    padding: "16px",
                    borderRadius: "8px",
                  }}
                >
                  <p style={{ margin: 0 }}>
                    {formData.permanentAddress.addressLine1},{" "}
                    {formData.permanentAddress.addressLine2 &&
                      `${formData.permanentAddress.addressLine2}, `}
                    {formData.permanentAddress.addressLine3 &&
                      `${formData.permanentAddress.addressLine3}, `}
                    {formData.permanentAddress.city},{" "}
                    {formData.permanentAddress.state},{" "}
                    {formData.permanentAddress.country} -{" "}
                    {formData.permanentAddress.pinCode}
                  </p>
                </div>
              </div>

              {/* Qualifications */}
              {formData.qualifications.length > 0 && (
                <div style={{ marginBottom: "24px" }}>
                  <h3
                    style={{
                      fontSize: "18px",
                      fontWeight: "600",
                      marginBottom: "12px",
                      color: "#1e40af",
                    }}
                  >
                    🎓 Qualifications
                  </h3>
                  <div
                    style={{
                      background: "#f8fafc",
                      padding: "16px",
                      borderRadius: "8px",
                    }}
                  >
                    {formData.qualifications.map((qual, index) => (
                      <div
                        key={index}
                        style={{
                          marginBottom:
                            index < formData.qualifications.length - 1
                              ? "12px"
                              : "0",
                          paddingBottom:
                            index < formData.qualifications.length - 1
                              ? "12px"
                              : "0",
                          borderBottom:
                            index < formData.qualifications.length - 1
                              ? "1px solid #e2e8f0"
                              : "none",
                        }}
                      >
                        <strong>{qual.degreeName}</strong> -{" "}
                        {qual.specialization}
                        <br />
                        {qual.instituteName}, {qual.universityName}
                        <br />
                        Passing: {qual.passingMonth}/
                        {formatYear(qual.yearOfPassing)}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Experiences */}
              {formData.experiences.length > 0 && (
                <div style={{ marginBottom: "24px" }}>
                  <h3
                    style={{
                      fontSize: "18px",
                      fontWeight: "600",
                      marginBottom: "12px",
                      color: "#1e40af",
                    }}
                  >
                    💼 Work Experience
                  </h3>
                  <div
                    style={{
                      background: "#f8fafc",
                      padding: "16px",
                      borderRadius: "8px",
                    }}
                  >
                    {formData.experiences.map((exp, index) => (
                      <div
                        key={index}
                        style={{
                          marginBottom:
                            index < formData.experiences.length - 1
                              ? "12px"
                              : "0",
                          paddingBottom:
                            index < formData.experiences.length - 1
                              ? "12px"
                              : "0",
                          borderBottom:
                            index < formData.experiences.length - 1
                              ? "1px solid #e2e8f0"
                              : "none",
                        }}
                      >
                        <strong>{exp.position}</strong> at {exp.companyName}
                        <br />
                        Duration: {formatDate(exp.fromDate)} to{" "}
                        {formatDate(exp.toDate)}
                        <br />
                        Experience: {exp.yearsOfExperience} years
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Documents */}
              {formData.documents.length > 0 && (
                <div style={{ marginBottom: "24px" }}>
                  <h3
                    style={{
                      fontSize: "18px",
                      fontWeight: "600",
                      marginBottom: "12px",
                      color: "#1e40af",
                    }}
                  >
                    📎 Documents Attached
                  </h3>
                  <div
                    style={{
                      background: "#f8fafc",
                      padding: "16px",
                      borderRadius: "8px",
                      display: "grid",
                      gridTemplateColumns:
                        "repeat(auto-fill, minmax(200px, 1fr))",
                      gap: "8px",
                    }}
                  >
                    {formData.documents.map((doc, index) => (
                      <div
                        key={index}
                        style={{
                          padding: "8px",
                          background: "white",
                          borderRadius: "6px",
                          fontSize: "13px",
                        }}
                      >
                        ✓ {doc.fileName}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* Footer Actions */}
            <div
              style={{
                padding: "20px 24px",
                borderTop: "1px solid #e2e8f0",
                display: "flex",
                justifyContent: "flex-end",
                gap: "12px",
                background: "#f8fafc",
                borderBottomLeftRadius: "16px",
                borderBottomRightRadius: "16px",
              }}
            >
              <button
                type="button"
                onClick={() => setShowReviewPopup(false)}
                disabled={isSubmitting}
                style={{
                  padding: "12px 24px",
                  background: "white",
                  border: "2px solid #cbd5e1",
                  borderRadius: "8px",
                  fontWeight: "600",
                  cursor: isSubmitting ? "not-allowed" : "pointer",
                  opacity: isSubmitting ? 0.5 : 1,
                  fontSize: "14px",
                  transition: "all 0.2s",
                }}
                onMouseEnter={(e) =>
                  !isSubmitting &&
                  (e.currentTarget.style.background = "#f1f5f9")
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.background = "white")
                }
              >
                ✏️ Edit
              </button>
              <button
                type="button"
                onClick={handleConfirmSubmit}
                disabled={isSubmitting}
                style={{
                  padding: "12px 32px",
                  background: isSubmitting
                    ? "#9ca3af"
                    : "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  border: "none",
                  borderRadius: "8px",
                  color: "white",
                  fontWeight: "700",
                  cursor: isSubmitting ? "not-allowed" : "pointer",
                  fontSize: "14px",
                  boxShadow: isSubmitting
                    ? "none"
                    : "0 4px 12px rgba(16, 185, 129, 0.3)",
                  transition: "all 0.2s",
                }}
                onMouseEnter={(e) =>
                  !isSubmitting &&
                  (e.currentTarget.style.transform = "translateY(-2px)")
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.transform = "translateY(0)")
                }
              >
                ✅ Confirm & Submit
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Success Popup */}
      {showSuccessPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.7)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            zIndex: 10000,
            padding: "20px",
          }}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              maxWidth: "500px",
              width: "100%",
              padding: "40px",
              textAlign: "center",
              boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
            }}
          >
            <div
              style={{
                width: "80px",
                height: "80px",
                borderRadius: "50%",
                background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                margin: "0 auto 24px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: "40px",
              }}
            >
              ✓
            </div>
            <h2
              style={{
                fontSize: "28px",
                fontWeight: "700",
                color: "#10b981",
                marginBottom: "16px",
              }}
            >
              {isResubmitMode
                ? "Application Resubmitted Successfully!"
                : "Application Submitted Successfully!"}
            </h2>
            <p
              style={{
                fontSize: "16px",
                color: "#64748b",
                marginBottom: "8px",
              }}
            >
              {isResubmitMode
                ? "Your application has been resubmitted and will be reviewed again from the beginning."
                : "Your application has been submitted successfully."}
            </p>
            <p
              style={{
                fontSize: "18px",
                fontWeight: "600",
                color: "#1e40af",
                marginBottom: "24px",
              }}
            >
              Application Number:{" "}
              <span style={{ color: "#10b981" }}>
                {submittedApplicationNumber}
              </span>
            </p>
            <div
              style={{
                width: "40px",
                height: "40px",
                border: "4px solid #10b981",
                borderTopColor: "transparent",
                borderRadius: "50%",
                margin: "0 auto",
                animation: "spin 1s linear infinite",
              }}
            />
            <p
              style={{
                marginTop: "16px",
                fontSize: "14px",
                color: "#64748b",
              }}
            >
              Redirecting to dashboard...
            </p>
            <style>
              {`
                @keyframes spin {
                  to { transform: rotate(360deg); }
                }
              `}
            </style>
          </div>
        </div>
      )}

      {/* Draft Success Popup */}
      {showDraftSuccessPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.7)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            zIndex: 10000,
            padding: "20px",
          }}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              maxWidth: "550px",
              width: "100%",
              padding: "40px",
              textAlign: "center",
              boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
            }}
          >
            <div
              style={{
                width: "90px",
                height: "90px",
                borderRadius: "50%",
                background: "linear-gradient(135deg, #0f766e 0%, #115e59 100%)",
                margin: "0 auto 24px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: "48px",
                boxShadow: "0 8px 20px rgba(15, 118, 110, 0.3)",
              }}
            >
              💾
            </div>
            <h2
              style={{
                fontSize: "32px",
                fontWeight: "700",
                color: "#0f766e",
                marginBottom: "16px",
              }}
            >
              Draft Saved Successfully!
            </h2>
            <div
              style={{
                background: "linear-gradient(135deg, #f0fdfa 0%, #ccfbf1 100%)",
                padding: "20px",
                borderRadius: "12px",
                marginBottom: "24px",
                border: "2px solid #99f6e4",
              }}
            >
              <p
                style={{
                  fontSize: "14px",
                  color: "#115e59",
                  marginBottom: "12px",
                  fontWeight: 500,
                }}
              >
                ✅ Your application has been saved as draft
              </p>
              <p
                style={{
                  fontSize: "20px",
                  fontWeight: "700",
                  color: "#0f766e",
                  marginBottom: "8px",
                }}
              >
                {draftApplicationNumber}
              </p>
              <div
                style={{
                  borderTop: "1px solid #5eead4",
                  paddingTop: "12px",
                  marginTop: "12px",
                }}
              >
                <p
                  style={{
                    fontSize: "13px",
                    color: "#14b8a6",
                    lineHeight: "1.6",
                    marginBottom: "8px",
                  }}
                >
                  📝 <strong>You can come back anytime</strong> to complete and
                  submit your application
                </p>
                <p
                  style={{
                    fontSize: "13px",
                    color: "#14b8a6",
                    lineHeight: "1.6",
                  }}
                >
                  📍 Find your draft in the{" "}
                  <strong>"Draft Applications"</strong> tab on your dashboard
                </p>
              </div>
            </div>
            <div
              style={{
                width: "45px",
                height: "45px",
                border: "5px solid #0f766e",
                borderTopColor: "transparent",
                borderRadius: "50%",
                margin: "0 auto",
                animation: "spin 1s linear infinite",
              }}
            />
            <p
              style={{
                marginTop: "20px",
                fontSize: "14px",
                color: "#64748b",
                fontWeight: 500,
              }}
            >
              Redirecting to dashboard...
            </p>
            <style>
              {`
                @keyframes spin {
                  to { transform: rotate(360deg); }
                }
              `}
            </style>
          </div>
        </div>
      )}

      {/* Unsaved Changes Popup */}
      {showUnsavedChangesPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.6)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 10000,
            backdropFilter: "blur(4px)",
          }}
          onClick={handleCancelNavigation}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              padding: "40px",
              maxWidth: "500px",
              width: "90%",
              boxShadow: "0 20px 60px rgba(0, 0, 0, 0.3)",
              textAlign: "center",
              position: "relative",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                width: "80px",
                height: "80px",
                background: "linear-gradient(135deg, #f59e0b 0%, #d97706 100%)",
                borderRadius: "50%",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                margin: "0 auto 24px auto",
                fontSize: "40px",
              }}
            >
              ⚠️
            </div>
            <h2
              style={{
                fontSize: "28px",
                fontWeight: "700",
                color: "#d97706",
                marginBottom: "16px",
              }}
            >
              Unsaved Changes
            </h2>
            <p
              style={{
                fontSize: "16px",
                color: "#64748b",
                marginBottom: "24px",
                lineHeight: "1.6",
              }}
            >
              You have unsaved changes in your form. If you leave now, all your
              progress will be lost.
            </p>
            <div
              style={{
                background: "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
                padding: "16px",
                borderRadius: "12px",
                marginBottom: "24px",
                border: "2px solid #fbbf24",
              }}
            >
              <p
                style={{
                  fontSize: "14px",
                  color: "#78350f",
                  lineHeight: "1.6",
                  marginBottom: "8px",
                }}
              >
                💡 <strong>Tip:</strong> Click <strong>"Save as Draft"</strong>{" "}
                to save your progress without validation
              </p>
            </div>
            <div
              style={{
                display: "flex",
                gap: "12px",
                justifyContent: "center",
              }}
            >
              <button
                onClick={handleCancelNavigation}
                style={{
                  padding: "12px 24px",
                  background:
                    "linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%)",
                  color: "white",
                  border: "none",
                  borderRadius: "8px",
                  fontWeight: "600",
                  fontSize: "15px",
                  cursor: "pointer",
                  boxShadow: "0 4px 12px rgba(30, 64, 175, 0.3)",
                  transition: "all 0.3s ease",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 6px 16px rgba(30, 64, 175, 0.4)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 4px 12px rgba(30, 64, 175, 0.3)";
                }}
              >
                Stay on Page
              </button>
              <button
                onClick={handleConfirmNavigation}
                style={{
                  padding: "12px 24px",
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                  color: "white",
                  border: "none",
                  borderRadius: "8px",
                  fontWeight: "600",
                  fontSize: "15px",
                  cursor: "pointer",
                  boxShadow: "0 4px 12px rgba(220, 38, 38, 0.3)",
                  transition: "all 0.3s ease",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 6px 16px rgba(220, 38, 38, 0.4)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 4px 12px rgba(220, 38, 38, 0.3)";
                }}
              >
                Leave Without Saving
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Full Screen Loader */}
      {isSubmitting && (
        <FullScreenLoader
          message="Submitting Application"
          submessage="Please wait while we process your application..."
        />
      )}
    </div>
  );
};
