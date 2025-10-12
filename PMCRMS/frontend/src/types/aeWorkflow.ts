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

export interface VerifyAndSignRequest {
  positionType: string;
  otp: string;
  comments?: string;
}

export interface RejectApplicationRequest {
  rejectionComments: string;
}

export interface WorkflowActionResult {
  success: boolean;
  message: string;
}
