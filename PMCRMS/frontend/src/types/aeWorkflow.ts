export interface AEWorkflowStatusDto {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  positionType: string;
  status: string;
  createdDate: string;

  // JE Assignment Info
  assignedJEId?: number;
  assignedJEName?: string;
  assignedJEDate?: string;
  jeApprovalStatus?: boolean;
  jeApprovalComments?: string;
  jeApprovalDate?: string;
  jeDigitalSignatureApplied?: boolean;

  // AE Assignment Info (position-specific)
  assignedToAEId?: number;
  assignedToAEName?: string;
  assignedToAEDate?: string;
  aeApprovalStatus?: boolean;
  aeApprovalComments?: string;
  aeApprovalDate?: string;
  aeRejectionStatus?: boolean;
  aeRejectionComments?: string;
  aeRejectionDate?: string;
  aeDigitalSignatureApplied?: boolean;
  aeDigitalSignatureDate?: string;
}

// PositionType enum matching backend
export type PositionType = 0 | 1 | 2 | 3 | 4;

// Helper constants for PositionType
export const PositionTypeEnum = {
  Architect: 0 as PositionType,
  LicenceEngineer: 1 as PositionType,
  StructuralEngineer: 2 as PositionType,
  Supervisor1: 3 as PositionType,
  Supervisor2: 4 as PositionType,
} as const;

// Helper to convert string to PositionType
export const positionTypeFromString = (str: string): PositionType => {
  const map: Record<string, PositionType> = {
    Architect: 0,
    LicenceEngineer: 1,
    StructuralEngineer: 2,
    Supervisor1: 3,
    Supervisor2: 4,
    AssistantArchitect: 0,
    AssistantStructuralEngineer: 2,
    AssistantLicenceEngineer: 1,
    AssistantSupervisor1: 3,
    AssistantSupervisor2: 4,
  };
  return map[str] ?? 0;
};

export interface VerifyAndSignRequest {
  applicationId: number;
  positionType: PositionType; // Changed from string to PositionType enum
  otp: string;
  comments?: string;
}

export interface RejectApplicationRequest {
  applicationId: number;
  positionType: PositionType; // Changed from string to PositionType enum
  rejectionComments: string;
}

export interface WorkflowActionResult {
  success: boolean;
  message: string;
}
