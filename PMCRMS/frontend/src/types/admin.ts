// Admin-specific types and interfaces

export interface OfficerInvitation {
  id: number;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  employeeId: string;
  department?: string;
  status: "Pending" | "Accepted" | "Expired" | "Revoked";
  invitedAt: string;
  sentDate: string; // Alias for invitedAt
  acceptedAt?: string;
  expiresAt: string;
  expiryDate: string; // Alias for expiresAt
  invitedBy: string;
  isExpired: boolean;
  userId?: number;
}

export interface Officer {
  id: number;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  employeeId: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdDate: string;
  applicationsProcessed?: number;
}

export interface OfficerDetail extends Officer {
  address?: string;
  updatedDate?: string;
  createdBy?: string;
  recentStatusUpdates?: ApplicationStatusSummary[];
}

export interface ApplicationStatusSummary {
  applicationId: number;
  applicationNumber: string;
  status: string;
  updatedAt: string;
  remarks?: string;
}

export interface FormConfiguration {
  id: number;
  formName: string;
  formType: string;
  description?: string;
  baseFee: number;
  processingFee: number;
  lateFee: number;
  totalFee: number;
  isActive: boolean;
  allowOnlineSubmission: boolean;
  processingDays: number;
  maxFileSizeMB?: number;
  maxFilesAllowed?: number;
  customFields?: string; // JSON string
  requiredDocuments?: string; // JSON string
}

export interface FormConfigurationDetail extends FormConfiguration {
  createdDate: string;
  updatedDate?: string;
  feeHistory: FormFeeHistory[];
}

export interface FormFeeHistory {
  id: number;
  oldBaseFee: number;
  newBaseFee: number;
  oldProcessingFee: number;
  newProcessingFee: number;
  effectiveFrom: string;
  changedBy: string;
  changeReason?: string;
  changedDate: string;
}

export interface AdminDashboardStats {
  totalApplications: number;
  pendingApplications: number;
  approvedApplications: number;
  rejectedApplications: number;
  totalOfficers: number;
  activeOfficers: number;
  pendingInvitations: number;
  totalRevenueCollected: number;
  revenueThisMonth: number;
  applicationTrends: ApplicationTrend[];
  roleDistribution: RoleDistribution[];
}

export interface ApplicationTrend {
  date: string;
  count: number;
  status: string;
}

export interface RoleDistribution {
  role: string;
  count: number;
  activeCount: number;
}

// Request types
export interface InviteOfficerRequest {
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  employeeId?: string; // Optional - auto-generated on backend if not provided
  department?: string;
  expiryDays?: number;
}

export interface UpdateOfficerRequest {
  name?: string;
  email?: string;
  phoneNumber?: string;
  role?: string;
  department?: string;
  isActive?: boolean;
}

export interface UpdateFormFeesRequest {
  baseFee: number;
  processingFee: number;
  lateFee?: number;
  effectiveFrom?: string;
  changeReason?: string;
}

export interface UpdateFormCustomFieldsRequest {
  customFieldsJson: string;
}

export interface UpdateFormConfigurationRequest {
  formName?: string;
  description?: string;
  isActive?: boolean;
  allowOnlineSubmission?: boolean;
  processingDays?: number;
  maxFileSizeMB?: number;
  maxFilesAllowed?: number;
  requiredDocuments?: string;
}

export const UserRoles = {
  ADMIN: "Admin",
  USER: "User",
  JUNIOR_ARCHITECT: "JuniorArchitect",
  ASSISTANT_ARCHITECT: "AssistantArchitect",
  JUNIOR_LICENCE_ENGINEER: "JuniorLicenceEngineer",
  ASSISTANT_LICENCE_ENGINEER: "AssistantLicenceEngineer",
  JUNIOR_STRUCTURAL_ENGINEER: "JuniorStructuralEngineer",
  ASSISTANT_STRUCTURAL_ENGINEER: "AssistantStructuralEngineer",
  JUNIOR_SUPERVISOR_1: "JuniorSupervisor1",
  ASSISTANT_SUPERVISOR_1: "AssistantSupervisor1",
  JUNIOR_SUPERVISOR_2: "JuniorSupervisor2",
  ASSISTANT_SUPERVISOR_2: "AssistantSupervisor2",
  EXECUTIVE_ENGINEER: "ExecutiveEngineer",
  CITY_ENGINEER: "CityEngineer",
  CLERK: "Clerk",
} as const;

export const OfficerRoles = [
  { value: UserRoles.JUNIOR_ARCHITECT, label: "Junior Architect" },
  { value: UserRoles.ASSISTANT_ARCHITECT, label: "Assistant Architect" },
  {
    value: UserRoles.JUNIOR_LICENCE_ENGINEER,
    label: "Junior Licence Engineer",
  },
  {
    value: UserRoles.ASSISTANT_LICENCE_ENGINEER,
    label: "Assistant Licence Engineer",
  },
  {
    value: UserRoles.JUNIOR_STRUCTURAL_ENGINEER,
    label: "Junior Structural Engineer",
  },
  {
    value: UserRoles.ASSISTANT_STRUCTURAL_ENGINEER,
    label: "Assistant Structural Engineer",
  },
  { value: UserRoles.JUNIOR_SUPERVISOR_1, label: "Junior Supervisor 1" },
  { value: UserRoles.ASSISTANT_SUPERVISOR_1, label: "Assistant Supervisor 1" },
  { value: UserRoles.JUNIOR_SUPERVISOR_2, label: "Junior Supervisor 2" },
  { value: UserRoles.ASSISTANT_SUPERVISOR_2, label: "Assistant Supervisor 2" },
  { value: UserRoles.EXECUTIVE_ENGINEER, label: "Executive Engineer" },
  { value: UserRoles.CITY_ENGINEER, label: "City Engineer" },
  { value: UserRoles.CLERK, label: "Clerk" },
];
