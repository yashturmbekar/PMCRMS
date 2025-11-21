// Admin Service - Handles all admin-related API calls
import apiClient from "./apiClient";
import type { ApiResponse } from "../types";

// Admin-specific types
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

export interface OfficerInvitation {
  id: number;
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  employeeId: string;
  department?: string;
  status: string;
  invitedAt: string;
  acceptedAt?: string;
  expiresAt: string;
  invitedByName: string;
  isExpired: boolean;
  userId?: number;
}

export interface InviteOfficerRequest {
  name: string;
  email: string;
  phoneNumber?: string;
  role: string;
  employeeId?: string; // Optional - auto-generated on backend if not provided
  department?: string;
  expiryDays?: number;
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
  applicationsProcessed: number;
}

export interface OfficerDetail extends Officer {
  address?: string;
  updatedDate?: string;
  createdBy?: string;
  recentStatusUpdates: ApplicationStatusSummary[];
}

export interface ApplicationStatusSummary {
  applicationId: number;
  applicationNumber: string;
  status: string;
  updatedAt: string;
  remarks?: string;
}

export interface UpdateOfficerRequest {
  officerId: number;
  name?: string;
  email?: string;
  phoneNumber?: string;
  role?: string;
  department?: string;
  isActive?: boolean;
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
  customFields?: CustomField[];
  requiredDocuments?: string[];
}

export interface CustomField {
  fieldName: string;
  fieldType: string;
  isRequired: boolean;
  label?: string;
  placeholder?: string;
  options?: string[];
  validationRule?: string;
}

export interface CreateFormConfigurationRequest {
  formName: string;
  formType: string;
  description?: string;
  baseFee: number;
  processingFee: number;
  lateFee: number;
  processingDays: number;
  maxFileSizeMB: number;
  maxFilesAllowed: number;
  customFields?: CustomField[];
  requiredDocuments?: string[];
}

export interface UpdateFormConfigurationRequest {
  formId: number;
  formName?: string;
  description?: string;
  baseFee?: number;
  processingFee?: number;
  lateFee?: number;
  isActive?: boolean;
  allowOnlineSubmission?: boolean;
  processingDays?: number;
  maxFileSizeMB?: number;
  maxFilesAllowed?: number;
  customFields?: CustomField[];
  requiredDocuments?: string[];
  changeReason?: string;
}

class AdminService {
  // Dashboard
  async getDashboardStats(): Promise<ApiResponse<AdminDashboardStats>> {
    return apiClient.get("/admin/dashboard");
  }

  // Officer Invitations
  async inviteOfficer(
    data: InviteOfficerRequest
  ): Promise<ApiResponse<OfficerInvitation>> {
    return apiClient.post("/admin/invite-officer", data);
  }

  async getInvitations(
    status?: string
  ): Promise<ApiResponse<OfficerInvitation[]>> {
    const params = status ? { status } : {};
    return apiClient.get("/admin/invitations", { params });
  }

  async resendInvitation(
    invitationId: number,
    expiryDays: number = 7
  ): Promise<ApiResponse<void>> {
    return apiClient.post("/admin/resend-invitation", {
      invitationId,
      expiryDays,
    });
  }

  async revokeInvitation(invitationId: number): Promise<ApiResponse<void>> {
    return apiClient.post(`/admin/revoke-invitation`, { invitationId });
  }

  async deleteInvitation(invitationId: number): Promise<ApiResponse<void>> {
    return apiClient.delete(`/admin/invitations/${invitationId}`);
  }

  // Officer Management
  async getOfficers(
    role?: string,
    isActive?: boolean
  ): Promise<ApiResponse<Officer[]>> {
    const params: Record<string, string | boolean> = {};
    if (role) params.role = role;
    if (isActive !== undefined) params.isActive = isActive;
    return apiClient.get("/admin/officers", { params });
  }

  async getOfficerDetail(id: number): Promise<ApiResponse<OfficerDetail>> {
    return apiClient.get(`/admin/officers/${id}`);
  }

  async updateOfficer(
    id: number,
    data: UpdateOfficerRequest
  ): Promise<ApiResponse<void>> {
    return apiClient.put(`/admin/officers/${id}`, data);
  }

  async deleteOfficer(id: number): Promise<ApiResponse<void>> {
    return apiClient.delete(`/admin/officers/${id}`);
  }

  // Form Configuration
  async getFormConfigurations(
    isActive?: boolean
  ): Promise<ApiResponse<FormConfiguration[]>> {
    const params = isActive !== undefined ? { isActive } : {};
    return apiClient.get("/FormConfiguration", { params });
  }

  async getFormConfiguration(
    id: number
  ): Promise<ApiResponse<FormConfiguration>> {
    return apiClient.get(`/FormConfiguration/${id}`);
  }

  async createFormConfiguration(
    data: CreateFormConfigurationRequest
  ): Promise<ApiResponse<FormConfiguration>> {
    return apiClient.post("/FormConfiguration", data);
  }

  async updateFormConfiguration(
    id: number,
    data: UpdateFormConfigurationRequest
  ): Promise<ApiResponse<void>> {
    return apiClient.put(`/FormConfiguration/${id}`, data);
  }

  async deleteFormConfiguration(id: number): Promise<ApiResponse<void>> {
    return apiClient.delete(`/FormConfiguration/${id}`);
  }

  // Form Fee Management (Admin endpoints)
  async getAllForms(): Promise<ApiResponse<FormConfiguration[]>> {
    return apiClient.get("/admin/forms");
  }

  async getFormDetail(
    formId: number
  ): Promise<ApiResponse<FormConfiguration & { feeHistory: unknown[] }>> {
    return apiClient.get(`/admin/forms/${formId}`);
  }

  async updateFormFees(
    formId: number,
    data: {
      baseFee: number;
      processingFee: number;
      lateFee?: number;
      effectiveFrom?: string;
      changeReason?: string;
    }
  ): Promise<ApiResponse<void>> {
    return apiClient.put(`/admin/forms/${formId}/fees`, data);
  }

  async updateFormCustomFields(
    formId: number,
    customFieldsJson: string
  ): Promise<ApiResponse<void>> {
    return apiClient.put(`/admin/forms/${formId}/custom-fields`, {
      customFieldsJson,
    });
  }

  async updateFormConfig(
    formId: number,
    data: {
      formName?: string;
      description?: string;
      isActive?: boolean;
      allowOnlineSubmission?: boolean;
      processingDays?: number;
      maxFileSizeMB?: number;
      maxFilesAllowed?: number;
      requiredDocuments?: string;
    }
  ): Promise<ApiResponse<void>> {
    return apiClient.put(`/admin/forms/${formId}`, data);
  }

  async deleteForm(formId: number): Promise<ApiResponse<void>> {
    return apiClient.delete(`/admin/forms/${formId}`);
  }

  // Applications Management - Admin View
  async getAllApplications(filters?: {
    status?: string;
    type?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ApiResponse<unknown[]>> {
    return apiClient.get("/admin/applications", { params: filters });
  }

  async getApplicationDetail(id: number): Promise<ApiResponse<unknown>> {
    return apiClient.get(`/admin/applications/${id}`);
  }
}

export const adminService = new AdminService();
