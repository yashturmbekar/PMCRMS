import apiClient from "./apiClient";
import type {
  Application,
  CreateApplicationRequest,
  UpdateApplicationRequest,
  ApplicationFilters,
  PaginatedResponse,
  ApiResponse,
} from "../types";

const endpoint = "/applications";

export const applicationService = {
  async getApplications(
    page = 1,
    pageSize = 10,
    filters?: ApplicationFilters
  ): Promise<ApiResponse<PaginatedResponse<Application>>> {
    const params = {
      page,
      pageSize,
      ...(filters
        ? Object.fromEntries(
            Object.entries(filters).filter(([, v]) => v !== undefined)
          )
        : {}),
    };
    return apiClient.get(endpoint, { params });
  },

  async getApplication(id: number): Promise<ApiResponse<Application>> {
    return apiClient.get(`${endpoint}/${id}`);
  },

  async createApplication(
    data: CreateApplicationRequest
  ): Promise<ApiResponse<Application>> {
    return apiClient.post(endpoint, data);
  },

  async updateApplication(
    data: UpdateApplicationRequest
  ): Promise<ApiResponse<Application>> {
    return apiClient.put(`${endpoint}/${data.id}`, data);
  },

  async deleteApplication(id: number): Promise<ApiResponse> {
    return apiClient.delete(`${endpoint}/${id}`);
  },

  async getMyApplications(
    page = 1,
    pageSize = 10
  ): Promise<ApiResponse<PaginatedResponse<Application>>> {
    const params = { page, pageSize };
    return apiClient.get(`${endpoint}/my-applications`, { params });
  },

  async duplicateApplication(id: number): Promise<ApiResponse<Application>> {
    return apiClient.post(`${endpoint}/${id}/duplicate`);
  },

  async exportApplications(
    filters?: ApplicationFilters,
    format: "csv" | "xlsx" = "csv"
  ): Promise<Blob> {
    const params = {
      ...(filters
        ? Object.fromEntries(
            Object.entries(filters).filter(([, v]) => v !== undefined)
          )
        : {}),
      format,
    };
    return apiClient.get(`${endpoint}/export`, {
      params,
      responseType: "blob",
    });
  },
};
