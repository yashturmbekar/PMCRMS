import React, { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

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
  Female: 0,
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
  const { user } = useAuth();
  const { positionType: positionParam } = useParams<{ positionType: string }>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [permanentSameAsLocal, setPermanentSameAsLocal] = useState(false);

  // Determine position type from URL parameter or default to StructuralEngineer
  const getPositionType = (): PositionTypeValue => {
    if (!positionParam) return PositionType.StructuralEngineer;

    const positionMap: { [key: string]: PositionTypeValue } = {
      architect: PositionType.Architect,
      "licence-engineer": PositionType.LicenceEngineer,
      "structural-engineer": PositionType.StructuralEngineer,
      supervisor1: PositionType.Supervisor1,
      supervisor2: PositionType.Supervisor2,
    };

    return (
      positionMap[positionParam.toLowerCase()] ??
      PositionType.StructuralEngineer
    );
  };

  // Use state for selected position type so it can be changed dynamically
  const [selectedPositionType, setSelectedPositionType] =
    useState<PositionTypeValue>(getPositionType());

  // Config updates automatically when selectedPositionType changes
  const config = POSITION_CONFIG[selectedPositionType];

  const [formData, setFormData] = useState<FormData>({
    firstName: "",
    middleName: "",
    lastName: "",
    motherName: "",
    mobileNumber: user?.phoneNumber || "",
    emailAddress: user?.email || "",
    positionType: selectedPositionType,
    bloodGroup: "",
    height: 0,
    gender: Gender.Male,
    dateOfBirth: "",
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
    setFormData((prev) => ({
      ...prev,
      [addressType]: {
        ...prev[addressType],
        [field]: value,
      },
    }));
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      // TODO: Implement actual API call
      console.log("Form Data:", formData);

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 2000));

      setSuccess("Application submitted successfully!");

      // Navigate to applications page after 2 seconds
      setTimeout(() => {
        navigate("/applications");
      }, 2000);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to submit application";
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveAsDraft = async () => {
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      // TODO: Implement actual API call to save draft
      console.log("Saving as Draft:", formData);

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1500));

      setSuccess(
        "Application saved as draft successfully! You can continue editing later."
      );

      // Navigate to applications page after 2 seconds
      setTimeout(() => {
        navigate("/dashboard");
      }, 2000);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to save draft";
      setError(errorMessage);
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

  return (
    <div className="pmc-fadeIn">
      {/* Header Section - Compact */}
      <div
        className="pmc-content-header pmc-fadeInDown"
        style={{ marginBottom: "16px" }}
      >
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

      <form onSubmit={handleSubmit}>
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
                    className="pmc-input"
                    value={formData.firstName}
                    onChange={(e) =>
                      handleInputChange("firstName", e.target.value)
                    }
                    required
                  />
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
                    className="pmc-input"
                    value={formData.lastName}
                    onChange={(e) =>
                      handleInputChange("lastName", e.target.value)
                    }
                    required
                  />
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Mother Name
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
                    value={formData.motherName}
                    onChange={(e) =>
                      handleInputChange("motherName", e.target.value)
                    }
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Mobile Number
                  </label>
                  <input
                    type="tel"
                    className="pmc-input"
                    value={formData.mobileNumber}
                    onChange={(e) =>
                      handleInputChange("mobileNumber", e.target.value)
                    }
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Email Address
                  </label>
                  <input
                    type="email"
                    className="pmc-input"
                    value={formData.emailAddress}
                    onChange={(e) =>
                      handleInputChange("emailAddress", e.target.value)
                    }
                    required
                  />
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Birth Date
                  </label>
                  <input
                    type="date"
                    className="pmc-input"
                    value={formData.dateOfBirth}
                    onChange={(e) =>
                      handleInputChange("dateOfBirth", e.target.value)
                    }
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Blood Group
                  </label>
                  <select
                    className="pmc-input pmc-select"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Height (in cms)
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    className="pmc-input"
                    value={formData.height || ""}
                    onChange={(e) =>
                      handleInputChange("height", parseFloat(e.target.value))
                    }
                    required
                  />
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
                      className="pmc-input"
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
                      className="pmc-input"
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
                      className="pmc-input"
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
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Street Address
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
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
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">State</label>
                  <input
                    type="text"
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">City</label>
                  <input
                    type="text"
                    className="pmc-input"
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
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Postal Code
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
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
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Street Address
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
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
                    disabled={permanentSameAsLocal}
                    required
                  />
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">State</label>
                  <input
                    type="text"
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">City</label>
                  <input
                    type="text"
                    className="pmc-input"
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
                </div>
              </div>

              <div className="pmc-form-grid pmc-form-grid-3">
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Postal Code
                  </label>
                  <input
                    type="text"
                    className="pmc-input"
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
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Upload PAN Card Attachment (Max 500KB)
                  </label>
                  <input
                    type="file"
                    className="pmc-input"
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
                    className="pmc-input"
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
                </div>
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Upload Aadhar Attachment (Max 500KB)
                  </label>
                  <input
                    type="file"
                    className="pmc-input"
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
                          className="pmc-input"
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
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          University Name
                        </label>
                        <input
                          type="text"
                          className="pmc-input"
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
                          className="pmc-input"
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
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Upload Certificate (Max 500KB)
                        </label>
                        <input
                          type="file"
                          className="pmc-input"
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
                      </div>
                    </div>

                    <div className="pmc-form-grid pmc-form-grid-3">
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Name of Degree
                        </label>
                        <input
                          type="text"
                          className="pmc-input"
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
                          {monthOptions.map((month, idx) => (
                            <option key={idx} value={idx + 1}>
                              {month}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Passing Year
                        </label>
                        <input
                          type="number"
                          className="pmc-input"
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
                          className="pmc-input"
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
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          Position
                        </label>
                        <input
                          type="text"
                          className="pmc-input"
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
                          className="pmc-input"
                          value={exp.fromDate.split("T")[0] || ""}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "fromDate",
                              `${e.target.value}T00:00:00.000Z`
                            )
                          }
                          required
                        />
                      </div>
                      <div className="pmc-form-group">
                        <label className="pmc-label pmc-label-required">
                          To Date
                        </label>
                        <input
                          type="date"
                          className="pmc-input"
                          value={exp.toDate.split("T")[0] || ""}
                          onChange={(e) =>
                            handleExperienceChange(
                              index,
                              "toDate",
                              `${e.target.value}T00:00:00.000Z`
                            )
                          }
                          required
                        />
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
                        className="pmc-input"
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
                      href="/files/self-declaration-form.pdf"
                      target="_blank"
                      rel="noopener noreferrer"
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
                    className="pmc-input"
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
                    className="pmc-input"
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
    </div>
  );
};
