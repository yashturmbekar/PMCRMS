import apiClient from "./apiClient";

export interface PositionRegistrationRequest {
  positionType: number;
  firstName: string;
  middleName?: string;
  lastName: string;
  motherName: string;
  mobileNumber: string;
  emailAddress: string;
  bloodGroup?: string;
  height?: number;
  gender: number;
  dateOfBirth: string;
  panCardNumber: string;
  aadharCardNumber: string;
  coaCardNumber?: string;
  localAddress: Address;
  permanentAddress: Address;
  qualifications: Qualification[];
  experiences: Experience[];
  documents: DocumentUpload[];
  status: number; // 0 = Draft, 1 = Submitted
}

export interface Address {
  addressLine1: string;
  addressLine2?: string;
  addressLine3?: string;
  city: string;
  state: string;
  country: string;
  pinCode: string;
}

export interface Qualification {
  fileId: string;
  instituteName: string;
  universityName: string;
  specialization: number;
  degreeName: string;
  passingMonth: number;
  yearOfPassing: number;
}

export interface Experience {
  fileId: string;
  companyName: string;
  position: string;
  fromDate: string;
  toDate: string;
}

export interface DocumentUpload {
  fileId: string;
  documentType: number;
  fileName: string;
  filePath: string;
  fileSize?: number;
  contentType?: string;
}

export interface PositionRegistrationResponse {
  id: number;
  applicationNumber: string;
  positionType: number;
  positionTypeName: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  motherName: string;
  mobileNumber: string;
  emailAddress: string;
  bloodGroup?: string;
  height?: number;
  gender: number;
  genderName: string;
  dateOfBirth: string;
  age: number;
  panCardNumber: string;
  aadharCardNumber: string;
  coaCardNumber?: string;
  status: number;
  statusName: string;
  submittedDate?: string;
  approvedDate?: string;
  remarks?: string;
  createdDate: string;
  updatedDate?: string;
  addresses: AddressResponse[];
  qualifications: QualificationResponse[];
  experiences: ExperienceResponse[];
  documents: DocumentResponse[];
  workflowInfo?: WorkflowInfo;
}

export interface WorkflowInfo {
  assignedJuniorEngineerId?: number;
  assignedJuniorEngineerName?: string;
  assignedJuniorEngineerEmail?: string;
  assignedDate?: string;
  progressPercentage: number;
  currentStage: string;
  nextAction: string;
  hasAppointment: boolean;
  appointmentDate?: string;
  appointmentPlace?: string;
  totalDocumentsCount: number;
  verifiedDocumentsCount: number;
  allDocumentsVerified: boolean;
  hasDigitalSignature: boolean;
  signatureCompletedDate?: string;
}

export interface AddressResponse {
  id: number;
  addressType: string;
  addressLine1: string;
  addressLine2?: string;
  addressLine3?: string;
  city: string;
  state: string;
  country: string;
  pinCode: string;
  fullAddress: string;
}

export interface QualificationResponse {
  id: number;
  fileId: string;
  instituteName: string;
  universityName: string;
  specialization: number;
  specializationName: string;
  degreeName: string;
  passingMonth: number;
  passingMonthName: string;
  yearOfPassing: number;
}

export interface ExperienceResponse {
  id: number;
  fileId: string;
  companyName: string;
  position: string;
  yearsOfExperience: number;
  fromDate: string;
  toDate: string;
}

export interface DocumentResponse {
  id: number;
  fileId: string;
  documentType: number;
  documentTypeName: string;
  fileName: string;
  filePath: string;
  fileSize?: number;
  contentType?: string;
  isVerified: boolean;
  verifiedDate?: string;
  verificationRemarks?: string;
}

const positionRegistrationService = {
  /**
   * Create a new position registration application
   */
  createApplication: async (
    data: PositionRegistrationRequest
  ): Promise<PositionRegistrationResponse> => {
    return await apiClient.post("/PositionRegistration", data);
  },

  /**
   * Update an existing application
   */
  updateApplication: async (
    id: number,
    data: PositionRegistrationRequest
  ): Promise<PositionRegistrationResponse> => {
    return await apiClient.put(`/PositionRegistration/${id}`, data);
  },

  /**
   * Get application by ID
   */
  getApplication: async (id: number): Promise<PositionRegistrationResponse> => {
    return await apiClient.get(`/PositionRegistration/${id}`);
  },

  /**
   * Get all applications with optional filters
   */
  getAllApplications: async (filters?: {
    positionType?: number;
    status?: number;
    userId?: number;
  }): Promise<PositionRegistrationResponse[]> => {
    const params = new URLSearchParams();
    if (filters?.positionType !== undefined) {
      params.append("positionType", filters.positionType.toString());
    }
    if (filters?.status !== undefined) {
      params.append("status", filters.status.toString());
    }
    if (filters?.userId !== undefined) {
      params.append("userId", filters.userId.toString());
    }

    const queryString = params.toString();
    const url = queryString
      ? `/PositionRegistration?${queryString}`
      : "/PositionRegistration";

    return await apiClient.get(url);
  },

  /**
   * Delete an application (only draft status)
   */
  deleteApplication: async (id: number): Promise<void> => {
    return await apiClient.delete(`/PositionRegistration/${id}`);
  },

  /**
   * Upload a document file
   */
  uploadDocument: async (
    file: File,
    documentType: number
  ): Promise<DocumentUpload> => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("documentType", documentType.toString());

    // This endpoint needs to be created separately for file uploads
    const response = await apiClient.postWithFiles(
      "/Document/upload",
      formData
    );

    return {
      fileId: response.fileId,
      documentType: documentType,
      fileName: file.name,
      filePath: response.filePath,
      fileSize: file.size,
      contentType: file.type,
    };
  },
};

export default positionRegistrationService;
