import React, { useState, useEffect } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, AlertCircle } from "lucide-react";
import positionRegistrationService, {
  type PositionRegistrationRequest,
  type Qualification as ApiQualification,
  type Experience as ApiExperience,
  type Address as ApiAddress,
} from "../services/positionRegistrationService";
import { PageLoader, SectionLoader } from "../components";

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
      "Address Proof",
      "Identity Proof",
      "Self Declaration Form",
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
      "Address Proof - Aadhar Card",
      "Identity Proof - PAN Card",
      "Self Declaration Form",
      "Experience Certificate",
      "Degree out of Maharashtra - UGC Recognition",
      "A.I.C.T.E Approved",
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
      "Experience Certificate (BE: 3 years, ME: 2 years, PhD: 1 year)",
      "Address Proof - Aadhar Card",
      "Identity Proof - PAN Card",
      "Degree from UGC recognized University",
      "AICTE Approved",
      "Self Declaration Form",
      "Photo",
    ],
  },
  [PositionType.Supervisor1]: {
    name: "Supervisor 1",
    icon: "👷",
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
      "Experience Certificate (Diploma: 2 years, ITI: 10 years)",
      "Address Proof",
      "Identity Proof",
      "Self Declaration Form + Photo",
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
      "Experience Certificate (Diploma: 2 years, ITI: 10 years)",
      "Address Proof",
      "Identity Proof",
      "Self Declaration Form + Photo",
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
  fileId: string;
  file?: File;
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
    file: File
  ) => {
    // TODO: Implement actual file upload to server
    // For now, create a local URL
    const document: Document = {
      documentType,
      filePath: URL.createObjectURL(file),
      fileName: file.name,
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
        // If file exists, convert it to base64
        if (d.file) {
          const uploadedDoc = await positionRegistrationService.uploadDocument(
            d.file,
            d.documentType
          );
          return uploadedDoc;
        }
        // Otherwise, assume it's already in the correct format with base64
        return {
          fileId: d.fileId,
          documentType: d.documentType,
          fileName: d.fileName,
          fileBase64: "", // Empty for existing documents (shouldn't happen in create flow)
          fileSize: undefined,
          contentType: undefined,
        };
      })
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
      documents,
    };

    return request;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Set attemptedSubmit first to trigger validation UI
    setAttemptedSubmit(true);

    // Comprehensive frontend validation
    const errors: string[] = [];

    // Personal Details validation
    if (!formData.firstName) errors.push("First name is required");
    if (!formData.lastName) errors.push("Last name is required");
    if (!formData.motherName) errors.push("Mother name is required");
    if (!formData.mobileNumber) errors.push("Mobile number is required");
    if (!formData.emailAddress) errors.push("Email address is required");
    if (!formData.dateOfBirth) errors.push("Date of birth is required");
    if (!formData.bloodGroup) errors.push("Blood group is required");
    if (!formData.height || formData.height <= 0)
      errors.push("Height is required");

    // Local Address validation
    if (!formData.currentAddress.addressLine1)
      errors.push("Local address line 1 is required");
    if (!formData.currentAddress.addressLine2)
      errors.push("Local address line 2 is required");
    if (!formData.currentAddress.city) errors.push("Local city is required");
    if (!formData.currentAddress.state) errors.push("Local state is required");
    // Country is always "India" - no validation needed
    if (!formData.currentAddress.pinCode)
      errors.push("Local postal code is required");

    // Permanent Address validation (if not same as local)
    if (!permanentSameAsLocal) {
      if (!formData.permanentAddress.addressLine1)
        errors.push("Permanent address line 1 is required");
      if (!formData.permanentAddress.addressLine2)
        errors.push("Permanent address line 2 is required");
      if (!formData.permanentAddress.city)
        errors.push("Permanent city is required");
      if (!formData.permanentAddress.state)
        errors.push("Permanent state is required");
      // Country is always "India" - no validation needed
      if (!formData.permanentAddress.pinCode)
        errors.push("Permanent postal code is required");
    }

    // PAN validation
    if (!formData.panCardNumber) errors.push("PAN card number is required");
    const panDoc = formData.documents.find(
      (d) => d.documentType === SEDocumentType.PanCard
    );
    if (!panDoc) errors.push("PAN card document upload is required");

    // Aadhar validation
    if (!formData.aadharCardNumber) errors.push("Aadhar number is required");
    const aadharDoc = formData.documents.find(
      (d) => d.documentType === SEDocumentType.AadharCard
    );
    if (!aadharDoc) errors.push("Aadhar card document upload is required");

    // Qualifications validation
    if (config.sections.qualifications) {
      formData.qualifications.forEach((qual, index) => {
        if (!qual.instituteName)
          errors.push(`Qualification ${index + 1}: Institute name is required`);
        if (!qual.universityName)
          errors.push(
            `Qualification ${index + 1}: University name is required`
          );
        if (!qual.degreeName)
          errors.push(`Qualification ${index + 1}: Degree name is required`);
        if (!qual.yearOfPassing)
          errors.push(`Qualification ${index + 1}: Passing year is required`);
      });
    }

    // Experience validation
    if (config.sections.experience) {
      formData.experiences.forEach((exp, index) => {
        if (!exp.companyName)
          errors.push(`Experience ${index + 1}: Company name is required`);
        if (!exp.position)
          errors.push(`Experience ${index + 1}: Position is required`);
        if (!exp.fromDate)
          errors.push(`Experience ${index + 1}: From date is required`);
        if (!exp.toDate)
          errors.push(`Experience ${index + 1}: To date is required`);
      });
    }

    // Property Tax Receipt validation
    if (config.sections.propertyTaxReceipt) {
      const propertyTaxDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.PropertyTaxReceipt
      );
      if (!propertyTaxDoc)
        errors.push(
          "Property tax receipt / rent agreement / electricity bill is required"
        );
    }

    // ISSE Certificate validation
    if (config.sections.isseCertificate) {
      const isseDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.ISSECertificate
      );
      if (!isseDoc) errors.push("ISSE certificate is required");
    }

    // COA Certificate validation
    if (config.sections.coaCertificate) {
      const coaDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.COACertificate
      );
      if (!coaDoc)
        errors.push("Council of Architecture certificate is required");
    }

    // Self Declaration validation
    if (config.sections.selfDeclaration) {
      const selfDecDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.SelfDeclaration
      );
      if (!selfDecDoc) errors.push("Self declaration document is required");
    }

    // Profile Picture validation
    if (config.sections.profilePicture) {
      const profilePicDoc = formData.documents.find(
        (d) => d.documentType === SEDocumentType.ProfilePicture
      );
      if (!profilePicDoc) errors.push("Profile picture is required");
    }

    if (errors.length > 0) {
      setError(
        "Please fill in all required fields and scroll through the form to see validation errors"
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

  const getTotalExperience = () => {
    const total = formData.experiences.reduce(
      (sum, exp) => sum + exp.yearsOfExperience,
      0
    );
    const years = Math.floor(total);
    const months = Math.round((total - years) * 12);
    return `${years} years and ${months} months`;
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

  // Initialize the form - simulate loading to prevent blank page
  useEffect(() => {
    // Small delay to ensure smooth transition
    const timer = setTimeout(() => {
      setInitializing(false);
    }, 100);
    return () => clearTimeout(timer);
  }, []);

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

        // Store rejection comments if in resubmit mode
        if (isResubmit && response.status === 37 && response.remarks) {
          // Status 37 = REJECTED
          setRejectionComments(response.remarks);
        }

        // Map API response to form data
        const addressLocal = response.addresses?.find(
          (a: { addressType: string }) => a.addressType === "Local"
        );
        const addressPermanent = response.addresses?.find(
          (a: { addressType: string }) => a.addressType === "Permanent"
        );

        setFormData({
          firstName: response.firstName || "",
          middleName: response.middleName || "",
          lastName: response.lastName || "",
          motherName: response.motherName || "",
          mobileNumber: response.mobileNumber || "",
          emailAddress: response.emailAddress || "",
          positionType:
            (response.positionType as PositionTypeValue) ??
            selectedPositionType,
          bloodGroup: response.bloodGroup || "",
          height: response.height || 0,
          gender: (response.gender as GenderValue) ?? 0,
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
            response.qualifications?.map((q) => ({
              fileId: q.fileId || `QUAL_${Date.now()}_${Math.random()}`,
              instituteName: q.instituteName || "",
              universityName: q.universityName || "",
              specialization: (q.specialization as SpecializationValue) ?? 0,
              degreeName: q.degreeName || "",
              passingMonth: q.passingMonth || 1,
              yearOfPassing: q.yearOfPassing?.toString() || "",
            })) || [],
          experiences:
            response.experiences?.map((e) => ({
              fileId: e.fileId || `EXP_${Date.now()}_${Math.random()}`,
              companyName: e.companyName || "",
              position: e.position || "",
              yearsOfExperience: e.yearsOfExperience || 0,
              fromDate: e.fromDate ? e.fromDate.split("T")[0] : "",
              toDate: e.toDate ? e.toDate.split("T")[0] : "",
            })) || [],
          documents:
            response.documents?.map((d) => ({
              fileId: d.fileId || `DOC_${Date.now()}_${Math.random()}`,
              documentType: (d.documentType as SEDocumentTypeValue) ?? 0,
              fileName: d.fileName || "",
              filePath: d.filePath || "",
            })) || [],
        });

        // Update position type if different
        if (
          response.positionType !== undefined &&
          response.positionType !== selectedPositionType
        ) {
          setSelectedPositionType(response.positionType as PositionTypeValue);
        }

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
    <div className="pmc-fadeIn">
      {/* Validation Error Popup */}
      {showValidationPopup && validationErrors.length > 0 && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.5)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 9999,
          }}
          onClick={() => setShowValidationPopup(false)}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "12px",
              padding: "24px",
              maxWidth: "500px",
              width: "90%",
              maxHeight: "70vh",
              overflow: "auto",
              boxShadow: "0 20px 60px rgba(0, 0, 0, 0.3)",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: "12px",
                marginBottom: "16px",
                paddingBottom: "16px",
                borderBottom: "2px solid #fee2e2",
              }}
            >
              <svg
                style={{
                  width: "32px",
                  height: "32px",
                  color: "#dc2626",
                  flexShrink: 0,
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
              <h2
                style={{
                  fontSize: "20px",
                  fontWeight: "700",
                  color: "#991b1b",
                  margin: 0,
                }}
              >
                Validation Error
              </h2>
            </div>
            <div
              style={{
                marginBottom: "20px",
              }}
            >
              <p
                style={{
                  fontSize: "14px",
                  color: "#64748b",
                  marginBottom: "12px",
                }}
              >
                Please fix the following errors:
              </p>
              <ul
                style={{
                  listStyle: "none",
                  padding: 0,
                  margin: 0,
                }}
              >
                {validationErrors.map((error, index) => (
                  <li
                    key={index}
                    style={{
                      padding: "10px 12px",
                      marginBottom: "8px",
                      backgroundColor: "#fef2f2",
                      border: "1px solid #fecaca",
                      borderRadius: "6px",
                      color: "#991b1b",
                      fontSize: "13px",
                      lineHeight: "1.5",
                    }}
                  >
                    • {error}
                  </li>
                ))}
              </ul>
            </div>
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <button
                onClick={() => setShowValidationPopup(false)}
                style={{
                  padding: "10px 24px",
                  backgroundColor: "#dc2626",
                  color: "white",
                  border: "none",
                  borderRadius: "8px",
                  fontWeight: "600",
                  fontSize: "14px",
                  cursor: "pointer",
                  transition: "all 0.2s ease",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.backgroundColor = "#b91c1c";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.backgroundColor = "#dc2626";
                }}
              >
                Close
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
        {/* Rejection Banner - Only shown in resubmit mode */}
        {isResubmitMode && rejectionComments && (
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
                  Application Rejected - Corrections Required
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
                corrections to your application, and resubmit. Your application
                will be reviewed again from the beginning.
              </p>
            </div>
          </div>
        )}

        {/* Success/Error Messages */}
        {success && (
          <div
            className="pmc-fadeIn"
            style={{
              padding: "12px 16px",
              marginBottom: "16px",
              background: "linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%)",
              border: "1px solid #86efac",
              borderRadius: "8px",
              color: "#166534",
              fontWeight: 500,
              display: "flex",
              alignItems: "center",
              gap: "10px",
              fontSize: "13px",
            }}
          >
            <svg
              style={{ width: "18px", height: "18px", flexShrink: 0 }}
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
              padding: "12px 16px",
              marginBottom: "16px",
              background: "linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)",
              border: "1px solid #fca5a5",
              borderRadius: "8px",
              color: "#991b1b",
              fontWeight: 500,
              display: "flex",
              alignItems: "center",
              gap: "10px",
              fontSize: "13px",
            }}
          >
            <svg
              style={{ width: "18px", height: "18px", flexShrink: 0 }}
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
            {error}
          </div>
        )}

        <div>
          {/* Basic Information */}
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
                <span style={{ fontSize: "16px" }}>📋</span>
                Basic Information
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Position selection and fee information
              </p>
            </div>
            <div className="pmc-card-body" style={{ padding: "16px" }}>
              <div className="pmc-form-grid pmc-form-grid-2">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Position
                  </label>
                  <select
                    className="pmc-input pmc-select"
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
                      padding: "12px 20px",
                      background: "#fef3c7",
                      borderRadius: "8px",
                      border: "1px solid #fbbf24",
                    }}
                  >
                    <span style={{ fontWeight: 600, color: "#92400e" }}>
                      Fees - ₹{config.fee} for {config.feeDuration}
                    </span>
                  </div>
                )}
                {config.fee === 0 && (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      padding: "12px 20px",
                      background: "#dcfce7",
                      borderRadius: "8px",
                      border: "1px solid #86efac",
                    }}
                  >
                    <span style={{ fontWeight: 600, color: "#166534" }}>
                      No Registration Fee
                    </span>
                  </div>
                )}
              </div>

              {/* Qualifications Info Box */}
              <div
                style={{
                  marginTop: "20px",
                  padding: "16px",
                  background: "#fef3c7",
                  borderRadius: "8px",
                  fontSize: "13px",
                  lineHeight: "1.6",
                }}
              >
                <h3 style={{ fontWeight: 600, marginBottom: "8px" }}>
                  1. Qualifications
                </h3>
                <div
                  style={{
                    paddingLeft: "10px",
                    margin: "8px 0",
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
                        marginTop: "12px",
                        marginBottom: "8px",
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
                    marginTop: "12px",
                    marginBottom: "8px",
                  }}
                >
                  {config.scope ? "3" : "2"}. Duties and Responsibilities
                </h3>
                <p style={{ margin: "4px 0", paddingLeft: "10px" }}>
                  It will be incumbent on every architect / licensed technical
                  personnel to assist and co-operate with the Metropolitan
                  Commissioner and other Officers in carrying out and enforcing
                  the provisions of Maharashtra Regional & Town Planning Act,
                  1966.
                </p>

                <h3
                  style={{
                    fontWeight: 600,
                    marginTop: "12px",
                    marginBottom: "8px",
                  }}
                >
                  {config.scope ? "4" : "3"}. Documents Required for{" "}
                  {config.name}
                </h3>
                <ol style={{ paddingLeft: "20px", margin: "8px 0" }}>
                  {config.documentsRequired.map((doc, idx) => (
                    <li key={idx}>{doc}</li>
                  ))}
                </ol>
              </div>
            </div>
          </div>

          {/* Personal Details */}
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
                <span style={{ fontSize: "24px" }}>👤</span>
                Personal Details
              </h2>
              <p
                className="pmc-card-subtitle"
                style={{
                  color: "#64748b",
                  fontSize: "13px",
                  margin: "2px 0 0 0",
                }}
              >
                Enter your personal information
              </p>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
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
                    value={formData.firstName}
                    onChange={(e) =>
                      handleInputChange("firstName", e.target.value)
                    }
                    required
                  />
                  {attemptedSubmit && !formData.firstName && (
                    <span className="pmc-text-error">
                      First name is required
                    </span>
                  )}
                  {hasFieldError("firstName") && (
                    <span className="pmc-text-error">
                      {fieldErrors["firstname"]}
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label">Middle Name</label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.middleName}
                    onChange={(e) =>
                      handleInputChange("middleName", e.target.value)
                    }
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
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
                    value={formData.lastName}
                    onChange={(e) =>
                      handleInputChange("lastName", e.target.value)
                    }
                    required
                  />
                  {attemptedSubmit && !formData.lastName && (
                    <span className="pmc-text-error">
                      Last name is required
                    </span>
                  )}
                  {hasFieldError("lastName") && (
                    <span className="pmc-text-error">
                      {fieldErrors["lastname"]}
                    </span>
                  )}
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Mother Name
                  </label>
                  <input
                    type="text"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.motherName) ||
                      hasFieldError("motherName")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.motherName}
                    onChange={(e) =>
                      handleInputChange("motherName", e.target.value)
                    }
                    required
                  />
                  {attemptedSubmit && !formData.motherName && (
                    <span className="pmc-text-error">
                      Mother name is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
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
                    value={formData.mobileNumber}
                    onChange={(e) =>
                      handleInputChange("mobileNumber", e.target.value)
                    }
                    required
                  />
                  {attemptedSubmit && !formData.mobileNumber && (
                    <span className="pmc-text-error">
                      Mobile number is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
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
                    value={formData.emailAddress}
                    onChange={(e) =>
                      handleInputChange("emailAddress", e.target.value)
                    }
                    required
                  />
                  {attemptedSubmit && !formData.emailAddress && (
                    <span className="pmc-text-error">
                      Email address is required
                    </span>
                  )}
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Birth Date
                  </label>
                  <input
                    type="date"
                    className={`pmc-input ${
                      (attemptedSubmit && !formData.dateOfBirth) ||
                      hasFieldError("dateOfBirth")
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.dateOfBirth}
                    onChange={(e) =>
                      handleInputChange("dateOfBirth", e.target.value)
                    }
                    max={getMaxBirthDate()}
                    required
                  />
                  {attemptedSubmit && !formData.dateOfBirth && (
                    <span className="pmc-text-error">
                      Date of birth is required
                    </span>
                  )}
                  {hasFieldError("dateOfBirth") && (
                    <span className="pmc-text-error">
                      {fieldErrors["dateofbirth"]}
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Blood Group
                  </label>
                  <select
                    className={`pmc-input pmc-select ${
                      attemptedSubmit && !formData.bloodGroup
                        ? "pmc-input-error"
                        : ""
                    }`}
                    value={formData.bloodGroup}
                    onChange={(e) =>
                      handleInputChange("bloodGroup", e.target.value)
                    }
                    required
                  >
                    <option value="">Select Blood Group</option>
                    {bloodGroupOptions.map((bg) => (
                      <option key={bg} value={bg}>
                        {bg}
                      </option>
                    ))}
                  </select>
                  {attemptedSubmit && !formData.bloodGroup && (
                    <span className="pmc-text-error">
                      Blood group is required
                    </span>
                  )}
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Height (in cms)
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
                    value={formData.height || ""}
                    onChange={(e) =>
                      handleInputChange("height", parseFloat(e.target.value))
                    }
                    required
                  />
                  {attemptedSubmit &&
                    (!formData.height || formData.height <= 0) && (
                      <span className="pmc-text-error">Height is required</span>
                    )}
                </div>
              </div>

              <div className="pmc-form-group">
                <label className="pmc-label pmc-label-required">Gender</label>
                <div style={{ display: "flex", gap: "20px", marginTop: "8px" }}>
                  <label
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
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
                      gap: "8px",
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
                      gap: "8px",
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

              <div className="pmc-form-grid pmc-form-grid-2">
                {config.sections.propertyTaxReceipt && (
                  <div className="pmc-form-group">
                    <label className="pmc-label pmc-label-required">
                      Upload - Property Tax Receipt / Copy Of Rent Agreement /
                      Electricity Bill
                    </label>
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
                    <span className="pmc-help-text">Max file size: 500KB</span>
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
                    <span className="pmc-help-text">Max file size: 500KB</span>
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
                    <span className="pmc-help-text">Max file size: 500KB</span>
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
                    required
                  />
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
                    required
                  />
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

                {formData.experiences.length > 0 && (
                  <div
                    style={{
                      padding: "16px",
                      background: "#dcfce7",
                      borderRadius: "8px",
                      fontSize: "16px",
                      fontWeight: 600,
                      color: "#166534",
                      textAlign: "center",
                    }}
                  >
                    Total Experience: {getTotalExperience()}
                  </div>
                )}
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
                <div className="pmc-form-grid pmc-form-grid-2">
                  <div className="pmc-form-group">
                    <label className="pmc-label">Document Name</label>
                    <input
                      type="text"
                      className="pmc-input"
                      placeholder="Enter document name"
                    />
                  </div>
                  <div className="pmc-form-group">
                    <label className="pmc-label">
                      Upload Attachment (Max 500KB)
                    </label>
                    <input
                      type="file"
                      className="pmc-input"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) {
                          handleFileUpload(
                            SEDocumentType.AdditionalDocument,
                            `DOC_ADD_${Date.now()}`,
                            file
                          );
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
                    required
                  />
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
                    required
                  />
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
                    ? "linear-gradient(135deg, #94a3b8 0%, #64748b 100%)"
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
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
                onMouseEnter={(e) =>
                  !isSubmitting &&
                  (e.currentTarget.style.transform = "translateY(-2px)")
                }
                onMouseLeave={(e) =>
                  (e.currentTarget.style.transform = "translateY(0)")
                }
              >
                {isSubmitting && (
                  <SectionLoader variant="minimal" size="small" inline />
                )}
                {isSubmitting ? "Submitting..." : "✅ Confirm & Submit"}
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
    </div>
  );
};
