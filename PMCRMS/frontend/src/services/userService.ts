import apiClient from "./apiClient";
import type { User, PaginatedResponse, ApiResponse, UserRole } from "../types";

const endpoint = "/users";

export const userService = {
  async getUsers(
    page = 1,
    pageSize = 10,
    search?: string,
    role?: UserRole
  ): Promise<ApiResponse<PaginatedResponse<User>>> {
    const params = {
      page,
      pageSize,
      ...(search && { search }),
      ...(role && { role }),
    };
    return apiClient.get(endpoint, { params });
  },

  async getUser(id: number): Promise<ApiResponse<User>> {
    return apiClient.get(`${endpoint}/${id}`);
  },

  async getCurrentUser(): Promise<ApiResponse<User>> {
    return apiClient.get(`${endpoint}/profile`);
  },

  async updateUserProfile(userData: Partial<User>): Promise<ApiResponse<User>> {
    return apiClient.put(`${endpoint}/profile`, userData);
  },

  async updateUserRole(userId: number, role: UserRole): Promise<ApiResponse> {
    return apiClient.put(`${endpoint}/${userId}/role`, { role });
  },

  async updateUserStatus(
    userId: number,
    isActive: boolean
  ): Promise<ApiResponse> {
    return apiClient.put(`${endpoint}/${userId}/status`, { isActive });
  },

  async deleteUser(id: number): Promise<ApiResponse> {
    return apiClient.delete(`${endpoint}/${id}`);
  },

  async exportUsers(
    filters?: { role?: UserRole; isActive?: boolean; search?: string },
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
