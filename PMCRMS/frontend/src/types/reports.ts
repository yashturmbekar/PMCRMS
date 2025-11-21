// Report-specific types and interfaces

export interface PositionSummary {
  positionType: string;
  positionName: string;
  totalApplications: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  underReviewCount: number;
  inProgressCount: number;
}

export interface StageSummary {
  stageName: string;
  stageDisplayName: string;
  applicationCount: number;
}

export interface ReportApplication {
  applicationId: number;
  applicationNumber: string;
  firstName: string;
  lastName: string;
  positionType: string;
  currentStage: string;
  submittedDate: string;
  createdDate: string;
}

export interface GetPositionSummariesResponse {
  positions: PositionSummary[];
  totalApplications: number;
}

export interface GetStageSummariesRequest {
  positionType: string;
}

export interface GetStageSummariesResponse {
  positionType: string;
  positionName: string;
  stages: StageSummary[];
  totalApplications: number;
}

export interface GetApplicationsByStageRequest {
  positionType: string;
  stageName: string;
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface GetApplicationsByStageResponse {
  applications: ReportApplication[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  positionType: string;
  stageName: string;
}

// Position types as const
export const PositionType = {
  ARCHITECT: "Architect",
  LICENCE_ENGINEER: "LicenceEngineer",
  STRUCTURAL_ENGINEER: "StructuralEngineer",
  SUPERVISOR_1: "Supervisor1",
  SUPERVISOR_2: "Supervisor2",
} as const;

export type PositionTypeValue =
  (typeof PositionType)[keyof typeof PositionType];

// Position display names
export const PositionDisplayNames: Record<string, string> = {
  Architect: "Architect Applications",
  LicenceEngineer: "Licence Engineer Applications",
  StructuralEngineer: "Structural Engineer Applications",
  Supervisor1: "Supervisor 1 Applications",
  Supervisor2: "Supervisor 2 Applications",
};

// Stage display names
export const StageDisplayNames: Record<string, string> = {
  ApplicationSubmitted: "Application Submitted",
  DocumentsUploaded: "Documents Uploaded",
  VerifiedByJuniorEngineer: "Verified by Junior Engineer",
  VerifiedByAssistantEngineer: "Verified by Assistant Engineer",
  VerifiedByExecutiveEngineer: "Verified by Executive Engineer",
  PaymentPending: "Payment Pending",
  PaymentCompleted: "Payment Completed",
  CertificateGenerated: "Certificate Generated",
  Approved: "Approved",
  Rejected: "Rejected",
  UnderReview: "Under Review",
  PendingDocuments: "Pending Documents",
  Stage2Pending: "Stage 2 Pending",
  Stage2Completed: "Stage 2 Completed",

  // Junior Engineer stages
  UnderReviewByJE: "Under Review by Junior Engineer",
  ApprovedByJE: "Approved by Junior Engineer",
  RejectedByJE: "Rejected by Junior Engineer",
  JUNIOR_ENGINEER_PENDING: "Junior Engineer Pending",
  AWAITING_JE_DIGITAL_SIGNATURE: "Awaiting Junior Engineer Digital Signature",

  // Assistant Engineer stages
  UnderReviewByAE: "Under Review by Assistant Engineer",
  ApprovedByAE: "Approved by Assistant Engineer",
  RejectedByAE: "Rejected by Assistant Engineer",
  ASSISTANT_ENGINEER_PENDING: "Assistant Engineer Pending",

  // Executive Engineer stages
  UnderReviewByEE1: "Under Review by Executive Engineer",
  ApprovedByEE1: "Approved by Executive Engineer",
  RejectedByEE1: "Rejected by Executive Engineer",
  UnderDigitalSignatureByEE2: "Under Digital Signature by Executive Engineer",
  DigitalSignatureCompletedByEE2:
    "Digital Signature Completed by Executive Engineer",
  EXECUTIVE_ENGINEER_PENDING: "Executive Engineer Pending",
  EXECUTIVE_ENGINEER_SIGN_PENDING: "Executive Engineer Sign Pending",

  // City Engineer stages
  UnderReviewByCE1: "Under Review by City Engineer",
  ApprovedByCE1: "Approved by City Engineer",
  RejectedByCE1: "Rejected by City Engineer",
  UnderFinalApprovalByCE2: "Under Final Approval by City Engineer",
  CITY_ENGINEER_PENDING: "City Engineer Pending",
  CITY_ENGINEER_SIGN_PENDING: "City Engineer Sign Pending",

  // Other stages
  UnderProcessingByClerk: "Under Processing by Clerk",
  ProcessedByClerk: "Processed by Clerk",
  CLERK_PENDING: "Clerk Pending",
  CertificateIssued: "Certificate Issued",
  Completed: "Completed",
  APPOINTMENT_SCHEDULED: "Appointment Scheduled",
  DOCUMENT_VERIFICATION_PENDING: "Document Verification Pending",
  DOCUMENT_VERIFICATION_IN_PROGRESS: "Document Verification In Progress",
  DOCUMENT_VERIFICATION_COMPLETED: "Document Verification Completed",
  APPROVED: "Approved",
  REJECTED: "Rejected",
};
