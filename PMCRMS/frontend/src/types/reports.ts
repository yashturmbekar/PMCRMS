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
};
