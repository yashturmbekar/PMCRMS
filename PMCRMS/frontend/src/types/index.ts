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
}

export type UserRole =
  | "Applicant"
  | "JuniorEngineer"
  | "AssistantEngineer"
  | "ExecutiveEngineer"
  | "CityEngineer"
  | "Clerk"
  | "Admin";

export const UserRoles = {
  Applicant: "Applicant" as const,
  JuniorEngineer: "JuniorEngineer" as const,
  AssistantEngineer: "AssistantEngineer" as const,
  ExecutiveEngineer: "ExecutiveEngineer" as const,
  CityEngineer: "CityEngineer" as const,
  Clerk: "Clerk" as const,
  Admin: "Admin" as const,
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
  phoneNumber: string;
  otp: string;
  purpose: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiresIn: number;
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
  title: string;
  message: string;
  type: "info" | "success" | "warning" | "error";
  isRead: boolean;
  createdDate: string;
  userId: number;
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
export interface ReportData {
  applicationsByStatus: { [key: string]: number };
  applicationsByType: { [key: string]: number };
  monthlyApplications: { month: string; count: number }[];
  averageProcessingTime: number;
  revenueByMonth: { month: string; revenue: number }[];
}
