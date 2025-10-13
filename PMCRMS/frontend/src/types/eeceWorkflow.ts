export interface EEWorkflowStatusDto {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  positionType: string;
  status: string;
  createdDate: string;

  // JE Info
  assignedJEName?: string;
  jeApprovalStatus?: boolean;
  jeApprovalDate?: string;

  // AE Info
  assignedAEName?: string;
  aeApprovalStatus?: boolean;
  aeApprovalDate?: string;

  // EE Assignment Info
  assignedExecutiveEngineerId?: number;
  assignedExecutiveEngineerName?: string;
  assignedExecutiveEngineerDate?: string;
  executiveEngineerApprovalStatus?: boolean;
  executiveEngineerApprovalComments?: string;
  executiveEngineerApprovalDate?: string;
  executiveEngineerRejectionStatus?: boolean;
  executiveEngineerRejectionComments?: string;
  executiveEngineerRejectionDate?: string;
  executiveEngineerDigitalSignatureApplied?: boolean;
  executiveEngineerDigitalSignatureDate?: string;
}

export interface CEWorkflowStatusDto {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  positionType: string;
  status: string;
  createdDate: string;

  // JE Info
  assignedJEName?: string;
  jeApprovalStatus?: boolean;
  jeApprovalDate?: string;

  // AE Info
  assignedAEName?: string;
  aeApprovalStatus?: boolean;
  aeApprovalDate?: string;

  // EE Info
  assignedExecutiveEngineerName?: string;
  executiveEngineerApprovalStatus?: boolean;
  executiveEngineerApprovalDate?: string;

  // CE Assignment Info
  assignedCityEngineerId?: number;
  assignedCityEngineerName?: string;
  assignedCityEngineerDate?: string;
  cityEngineerApprovalStatus?: boolean;
  cityEngineerApprovalComments?: string;
  cityEngineerApprovalDate?: string;
  cityEngineerRejectionStatus?: boolean;
  cityEngineerRejectionComments?: string;
  cityEngineerRejectionDate?: string;
  cityEngineerDigitalSignatureApplied?: boolean;
  cityEngineerDigitalSignatureDate?: string;
  approvedDate?: string;
}

export interface VerifyAndSignRequest {
  applicationId: number;
  otp: string;
  comments?: string;
}

export interface RejectApplicationRequest {
  applicationId: number;
  rejectionComments: string;
}

export interface WorkflowActionResult {
  success: boolean;
  message: string;
}
