import apiClient from "./apiClient";
import type {
  UpdateStatusRequest,
  Application,
  ApplicationStatus,
  PaginatedResponse,
  ApiResponse,
  ApplicationCurrentStatus,
} from "../types";

const endpoint = "/status";

export const statusService = {
  async updateApplicationStatus(
    applicationId: number,
    data: UpdateStatusRequest
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/update/${applicationId}`, data);
  },

  async getPendingApplications(
    page = 1,
    pageSize = 10,
    status?: ApplicationCurrentStatus
  ): Promise<ApiResponse<PaginatedResponse<Application>>> {
    const params = {
      page,
      pageSize,
      ...(status && { status }),
    };
    return apiClient.get(`${endpoint}/pending`, { params });
  },

  async getApplicationStatusHistory(
    applicationId: number
  ): Promise<ApiResponse<ApplicationStatus[]>> {
    return apiClient.get(`${endpoint}/history/${applicationId}`);
  },

  async bulkUpdateStatus(
    applicationIds: number[],
    status: ApplicationCurrentStatus,
    remarks?: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/bulk-update`, {
      applicationIds,
      status,
      remarks,
    });
  },

  async getMyPendingTasks(
    page = 1,
    pageSize = 10
  ): Promise<ApiResponse<PaginatedResponse<Application>>> {
    const params = { page, pageSize };
    return apiClient.get(`${endpoint}/my-pending-tasks`, { params });
  },

  async assignApplication(
    applicationId: number,
    assigneeId: number,
    remarks?: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/assign/${applicationId}`, {
      assigneeId,
      remarks,
    });
  },
};
