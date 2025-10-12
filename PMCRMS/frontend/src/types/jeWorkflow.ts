/**
 * JE Workflow Types
 * Corresponds to backend JEWorkflowDTOs and PositionRegistrationDTOs
 */

// ============================================
// Workflow Status & Information
// ============================================

export interface JEWorkflowStatusDto {
  applicationId: number;
  applicationNumber: string;
  firstName: string;
  lastName: string;
  currentStatus: string;
  progressPercentage: number;
  currentStage: string;
  nextAction: string;
  assignedOfficer: AssignedOfficerDto | null;
  appointmentInfo: AppointmentInfoDto | null;
  verificationInfo: VerificationInfoDto | null;
  signatureInfo: SignatureInfoDto | null;
  canProceed: boolean;
  blockers: string[];
  lastUpdated: string;
}

export interface AssignedOfficerDto {
  id: number;
  name: string;
  email: string;
  role: string;
  assignedDate: string;
}

export interface AppointmentInfoDto {
  isScheduled: boolean;
  appointmentId: number | null;
  date: string | null;
  place: string | null;
  contactPerson: string | null;
  roomNumber: string | null;
  status: string | null;
}

export interface VerificationInfoDto {
  totalDocuments: number;
  verifiedCount: number;
  rejectedCount: number;
  pendingCount: number;
  allVerified: boolean;
  lastVerifiedDate: string | null;
}

export interface SignatureInfoDto {
  isInitiated: boolean;
  signatureId: number | null;
  documentPath: string | null;
  initiatedDate: string | null;
  isCompleted: boolean;
  signedDate: string | null;
  certificateSerialNumber: string | null;
}

// ============================================
// Workflow History & Timeline
// ============================================

export interface WorkflowHistoryDto {
  applicationId: number;
  applicationNumber: string;
  statusHistory: StatusHistoryItemDto[];
  assignmentHistory: AssignmentHistoryItemDto[];
  totalSteps: number;
  completedSteps: number;
}

export interface StatusHistoryItemDto {
  status: string;
  statusDate: string;
  updatedBy: string;
  remarks: string | null;
}

export interface AssignmentHistoryItemDto {
  officerId: number;
  officerName: string;
  officerRole: string;
  assignedDate: string;
  assignedBy: string;
  strategy: string;
}

export interface WorkflowTimelineEventDto {
  eventType: string;
  description: string;
  timestamp: string;
  performedBy: string | null;
  metadata: Record<string, unknown> | null;
}

// ============================================
// Workflow Actions - Request DTOs
// ============================================

export interface StartJEWorkflowRequestDto {
  applicationId: number;
  assignmentStrategy?: string;
  remarks?: string;
}

export interface ScheduleAppointmentRequestDto {
  applicationId: number;
  reviewDate: string;
  place: string;
  contactPerson: string;
  roomNumber: string;
  remarks?: string;
}

export interface CompleteAppointmentRequestDto {
  appointmentId: number;
  remarks?: string;
}

export interface VerifyDocumentRequestDto {
  applicationId: number;
  comments?: string;
  otp?: string;
}

export interface InitiateSignatureRequestDto {
  applicationId: number;
  documentPath: string;
}

export interface CompleteSignatureRequestDto {
  applicationId: number;
  signatureId: number;
  otp: string;
}

export interface TransitionWorkflowRequestDto {
  applicationId: number;
  newStatus: string;
  remarks: string;
}

export interface BulkWorkflowActionRequestDto {
  applicationIds: number[];
  action: string;
  remarks?: string;
}

// ============================================
// Workflow Metrics & Reporting
// ============================================

export interface WorkflowSummaryDto {
  totalApplications: number;
  byStatus: Record<string, number>;
  averageProcessingTime: number;
  completedToday: number;
  pendingActions: number;
  delayedApplications: number;
  dateRange: {
    from: string;
    to: string;
  };
}

export interface WorkflowMetricsDto {
  period: string;
  totalApplications: number;
  completedApplications: number;
  inProgressApplications: number;
  averageCompletionTime: number;
  averageTimeByStage: Record<string, number>;
  officerWorkload: OfficerWorkloadDto[];
  bottlenecks: WorkflowBottleneckDto[];
}

export interface OfficerWorkloadDto {
  officerId: number;
  officerName: string;
  assignedCount: number;
  completedCount: number;
  pendingCount: number;
  averageCompletionTime: number;
}

export interface WorkflowBottleneckDto {
  stage: string;
  averageTimeInStage: number;
  applicationsStuck: number;
  recommendations: string[];
}

// ============================================
// Workflow Validation
// ============================================

export interface WorkflowValidationResultDto {
  isValid: boolean;
  canProceed: boolean;
  errors: string[];
  warnings: string[];
  nextSteps: string[];
}

// ============================================
// PositionRegistration Workflow Info
// ============================================

export interface JEWorkflowInfo {
  assignedJuniorEngineerId: number | null;
  assignedJuniorEngineerName: string | null;
  assignedJuniorEngineerEmail: string | null;
  assignedDate: string | null;
  progressPercentage: number;
  currentStage: string;
  nextAction: string;
  hasAppointment: boolean;
  appointmentDate: string | null;
  appointmentPlace: string | null;
  allDocumentsVerified: boolean;
  verifiedDocumentsCount: number;
  totalDocumentsCount: number;
  hasDigitalSignature: boolean;
  signatureCompletedDate: string | null;
  timeline: WorkflowTimelineEvent[];
}

export interface WorkflowTimelineEvent {
  eventType: string;
  description: string;
  timestamp: string;
  performedBy: string | null;
}

// ============================================
// API Response Wrappers
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface WorkflowActionResultDto {
  success: boolean;
  message: string;
  applicationId: number;
  newStatus: string;
  nextAction: string | null;
  errors: string[];
}

// ============================================
// Workflow Enums (as constants)
// ============================================

export const WorkflowEventType = {
  SUBMISSION: "Submission",
  ASSIGNMENT: "Assignment",
  APPOINTMENT: "Appointment",
  APPOINTMENT_COMPLETED: "AppointmentCompleted",
  DOCUMENT_VERIFIED: "DocumentVerified",
  DOCUMENT_REJECTED: "DocumentRejected",
  SIGNATURE_INITIATED: "SignatureInitiated",
  SIGNATURE_COMPLETED: "SignatureCompleted",
  STATUS_CHANGE: "StatusChange",
} as const;

export const ApplicationStatus = {
  DRAFT: "Draft",
  SUBMITTED: "Submitted",
  JUNIOR_ENGINEER_PENDING: "JUNIOR_ENGINEER_PENDING",
  APPOINTMENT_SCHEDULED: "APPOINTMENT_SCHEDULED",
  DOCUMENT_VERIFICATION_PENDING: "DOCUMENT_VERIFICATION_PENDING",
  DOCUMENT_VERIFICATION_IN_PROGRESS: "DOCUMENT_VERIFICATION_IN_PROGRESS",
  DOCUMENT_VERIFICATION_COMPLETED: "DOCUMENT_VERIFICATION_COMPLETED",
  AWAITING_JE_DIGITAL_SIGNATURE: "AWAITING_JE_DIGITAL_SIGNATURE",
  ASSISTANT_ENGINEER_PENDING: "ASSISTANT_ENGINEER_PENDING",
} as const;

export const AssignmentStrategy = {
  ROUND_ROBIN: "RoundRobin",
  LEAST_ASSIGNED: "LeastAssigned",
  RANDOM: "Random",
  MANUAL: "Manual",
} as const;
