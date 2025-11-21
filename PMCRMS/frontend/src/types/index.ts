// Core Types for PMCRMS Application

export interface User {
  id: number;
  name: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  isActive: boolean;
  address?: string;
  createdDate: string;
  createdBy: string;
  updatedDate?: string;
  updatedBy?: string;
  mustChangePassword?: boolean;
  department?: string;
  employeeId?: string;
}

export type UserRole =
  | "Applicant"
  | "Admin"
  | "JuniorArchitect"
  | "AssistantArchitect"
  | "JuniorLicenceEngineer"
  | "AssistantLicenceEngineer"
  | "JuniorStructuralEngineer"
  | "AssistantStructuralEngineer"
  | "JuniorSupervisor1"
  | "AssistantSupervisor1"
  | "JuniorSupervisor2"
  | "AssistantSupervisor2"
  | "ExecutiveEngineer"
  | "CityEngineer"
  | "Clerk"
  | "JuniorEngineer" // Backward compatibility
  | "AssistantEngineer"; // Backward compatibility

export const UserRoles = {
  Applicant: "Applicant" as const,
  Admin: "Admin" as const,
  JuniorArchitect: "JuniorArchitect" as const,
  AssistantArchitect: "AssistantArchitect" as const,
  JuniorLicenceEngineer: "JuniorLicenceEngineer" as const,
  AssistantLicenceEngineer: "AssistantLicenceEngineer" as const,
  JuniorStructuralEngineer: "JuniorStructuralEngineer" as const,
  AssistantStructuralEngineer: "AssistantStructuralEngineer" as const,
  JuniorSupervisor1: "JuniorSupervisor1" as const,
  AssistantSupervisor1: "AssistantSupervisor1" as const,
  JuniorSupervisor2: "JuniorSupervisor2" as const,
  AssistantSupervisor2: "AssistantSupervisor2" as const,
  ExecutiveEngineer: "ExecutiveEngineer" as const,
  CityEngineer: "CityEngineer" as const,
  Clerk: "Clerk" as const,
  JuniorEngineer: "JuniorEngineer" as const,
  AssistantEngineer: "AssistantEngineer" as const,
};

export interface Application {
  id: number;
  applicationNumber: string;
  type: ApplicationType;
  applicantId: number;
  applicant?: User;
  currentStatus: ApplicationCurrentStatus;
  submissionDate: string;
  lastUpdatedDate: string;

  // Assigned Officer Details
  assignedOfficerId?: number;
  assignedOfficerName?: string;
  assignedOfficerDesignation?: string;
  assignedDate?: string;

  // Property Details
  propertyDetails: {
    surveyNumber: string;
    plotNumber: string;
    areaInSqFt: number;
    propertyType: string;
    location: string;
    pincode: string;
    zone: string;
    ward: string;
  };

  // Construction Details
  constructionDetails: {
    proposedUse: string;
    buildingHeight: number;
    numberOfFloors: number;
    builtUpArea: number;
    estimatedCost: number;
    architectName?: string;
    structuralEngineerName?: string;
  };

  // Owner Details
  ownerDetails: {
    ownerName: string;
    ownerAddress: string;
    ownerContactNumber: string;
    ownerEmail?: string;
    ownerAadharNumber: string;
  };

  documents?: ApplicationDocument[];
  statusHistory?: ApplicationStatus[];
  comments?: ApplicationComment[];
  payments?: Payment[];
}

export type ApplicationType =
  | "NewConstruction"
  | "Addition"
  | "Alteration"
  | "Renovation"
  | "Demolition";

export const ApplicationTypes = {
  NewConstruction: "NewConstruction" as const,
  Addition: "Addition" as const,
  Alteration: "Alteration" as const,
  Renovation: "Renovation" as const,
  Demolition: "Demolition" as const,
};

export type ApplicationCurrentStatus =
  | "Draft"
  | "Submitted"
  | "UnderReviewByJE"
  | "ApprovedByJE"
  | "RejectedByJE"
  | "UnderReviewByAE"
  | "ApprovedByAE"
  | "RejectedByAE"
  | "UnderReviewByEE1"
  | "ApprovedByEE1"
  | "RejectedByEE1"
  | "UnderReviewByCE1"
  | "ApprovedByCE1"
  | "RejectedByCE1"
  | "PaymentPending"
  | "PaymentCompleted"
  | "UnderProcessingByClerk"
  | "ProcessedByClerk"
  | "UnderDigitalSignatureByEE2"
  | "DigitalSignatureCompletedByEE2"
  | "UnderFinalApprovalByCE2"
  | "CertificateIssued"
  | "Completed";

export const ApplicationStatuses = {
  Draft: "Draft" as const,
  Submitted: "Submitted" as const,
  UnderReviewByJE: "UnderReviewByJE" as const,
  ApprovedByJE: "ApprovedByJE" as const,
  RejectedByJE: "RejectedByJE" as const,
  UnderReviewByAE: "UnderReviewByAE" as const,
  ApprovedByAE: "ApprovedByAE" as const,
  RejectedByAE: "RejectedByAE" as const,
  UnderReviewByEE1: "UnderReviewByEE1" as const,
  ApprovedByEE1: "ApprovedByEE1" as const,
  RejectedByEE1: "RejectedByEE1" as const,
  UnderReviewByCE1: "UnderReviewByCE1" as const,
  ApprovedByCE1: "ApprovedByCE1" as const,
  RejectedByCE1: "RejectedByCE1" as const,
  PaymentPending: "PaymentPending" as const,
  PaymentCompleted: "PaymentCompleted" as const,
  UnderProcessingByClerk: "UnderProcessingByClerk" as const,
  ProcessedByClerk: "ProcessedByClerk" as const,
  UnderDigitalSignatureByEE2: "UnderDigitalSignatureByEE2" as const,
  DigitalSignatureCompletedByEE2: "DigitalSignatureCompletedByEE2" as const,
  UnderFinalApprovalByCE2: "UnderFinalApprovalByCE2" as const,
  CertificateIssued: "CertificateIssued" as const,
  Completed: "Completed" as const,
};

export interface ApplicationDocument {
  id: number;
  applicationId: number;
  type: DocumentType;
  fileName: string;
  filePath: string;
  uploadedDate: string;
  uploadedBy: number;
  isVerified: boolean;
  verifiedBy?: number;
  verifiedDate?: string;
  remarks?: string;
}

export type DocumentType =
  | "PropertyDocument"
  | "ArchitecturalPlan"
  | "StructuralPlan"
  | "SitePlan"
  | "AadharCard"
  | "PropertyCard"
  | "NOC"
  | "Other";

export const DocumentTypes = {
  PropertyDocument: "PropertyDocument" as const,
  ArchitecturalPlan: "ArchitecturalPlan" as const,
  StructuralPlan: "StructuralPlan" as const,
  SitePlan: "SitePlan" as const,
  AadharCard: "AadharCard" as const,
  PropertyCard: "PropertyCard" as const,
  NOC: "NOC" as const,
  Other: "Other" as const,
};

export interface ApplicationStatus {
  id: number;
  applicationId: number;
  status: ApplicationCurrentStatus;
  updatedDate: string;
  updatedByUserId: number;
  updatedByUser?: User;
  remarks?: string;
}

export interface ApplicationComment {
  id: number;
  applicationId: number;
  commentText: string;
  commentedBy: number;
  commentedByUser?: User;
  commentedDate: string;
  parentCommentId?: number;
  parentComment?: ApplicationComment;
  replies?: ApplicationComment[];
}

export interface Payment {
  id: number;
  applicationId: number;
  paymentId: string;
  transactionId?: string;
  amount: number;
  status: PaymentStatus;
  method: PaymentMethod;
  initiatedDate: string;
  completedDate?: string;
  processedBy?: number;
  processedByUser?: User;
  gatewayResponse?: string;
}

export type PaymentStatus = "Pending" | "Completed" | "Failed" | "Refunded";

export const PaymentStatuses = {
  Pending: "Pending" as const,
  Completed: "Completed" as const,
  Failed: "Failed" as const,
  Refunded: "Refunded" as const,
};

export type PaymentMethod = "BillDesk" | "Cash" | "Cheque";

export const PaymentMethods = {
  BillDesk: "BillDesk" as const,
  Cash: "Cash" as const,
  Cheque: "Cheque" as const,
};

// API Response Types
export interface ApiResponse<T = unknown> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

// Authentication Types
export interface LoginRequest {
  phoneNumber: string;
  password?: string;
}

export interface OtpVerificationRequest {
  identifier: string; // Email or phone number
  otpCode: string;
  purpose: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  user: User;
  expiresIn?: number;
  expiresAt?: string;
}

// Form Types
export interface CreateApplicationRequest {
  type: ApplicationType;
  propertyDetails: Application["propertyDetails"];
  constructionDetails: Application["constructionDetails"];
  ownerDetails: Application["ownerDetails"];
}

export interface UpdateApplicationRequest
  extends Partial<CreateApplicationRequest> {
  id: number;
}

export interface UpdateStatusRequest {
  status: ApplicationCurrentStatus;
  remarks?: string;
}

// Dashboard Types
export interface DashboardStats {
  totalApplications: number;
  pendingApplications: number;
  approvedApplications: number;
  rejectedApplications: number;
  paymentsCompleted: number;
  totalRevenue: number;
}

// Notification Types
export interface Notification {
  id: number;
  userId: number;
  type: NotificationType;
  title: string;
  message: string;
  applicationId?: number;
  applicationNumber?: string;
  isRead: boolean;
  readAt?: string;
  actionUrl?: string;
  actorName?: string;
  actorRole?: string;
  priority: NotificationPriority;
  createdDate: string;
}

export type NotificationType =
  | "Submission"
  | "Assignment"
  | "Approval"
  | "Rejection"
  | "StatusChange"
  | "Comment"
  | "DocumentUpdate"
  | "PaymentReceived";

export const NotificationTypes = {
  Submission: "Submission" as const,
  Assignment: "Assignment" as const,
  Approval: "Approval" as const,
  Rejection: "Rejection" as const,
  StatusChange: "StatusChange" as const,
  Comment: "Comment" as const,
  DocumentUpdate: "DocumentUpdate" as const,
  PaymentReceived: "PaymentReceived" as const,
};

export type NotificationPriority = "Low" | "Normal" | "High" | "Urgent";

export const NotificationPriorities = {
  Low: 0,
  Normal: 1,
  High: 2,
  Urgent: 3,
};

export interface CreateNotificationRequest {
  userId: number;
  type: NotificationType;
  title: string;
  message: string;
  applicationId?: number;
  applicationNumber?: string;
  actionUrl?: string;
  actorName?: string;
  actorRole?: string;
  priority?: NotificationPriority;
}

export interface NotificationSummary {
  unreadCount: number;
  totalCount: number;
  recentNotifications: Notification[];
}

// Search and Filter Types
export interface ApplicationFilters {
  status?: ApplicationCurrentStatus;
  type?: ApplicationType;
  fromDate?: string;
  toDate?: string;
  applicantName?: string;
  applicationNumber?: string;
}

// Workflow Step Information
export interface WorkflowStep {
  step: number;
  status: string;
  description: string;
  role: string;
  isCompleted: boolean;
  completedDate?: string;
  completedBy?: string;
  remarks?: string;
}

// File Upload Types
export interface FileUploadResponse {
  fileName: string;
  filePath: string;
  fileSize: number;
  uploadedDate: string;
}

// Report Types
export interface ReportFilters {
  fromDate?: string;
  toDate?: string;
  status?: ApplicationCurrentStatus;
  type?: ApplicationType;
  userId?: number;
  department?: string;
}

export interface ReportData {
  applicationsByStatus: { [key: string]: number };
  applicationsByType: { [key: string]: number };
  monthlyApplications: { month: string; count: number }[];
  averageProcessingTime: number;
  revenueByMonth: { month: string; revenue: number }[];
}

// JE Workflow Types
export * from "./jeWorkflow";

// Report Types (PMC Admin Drill-Down)
export * from "./reports";
