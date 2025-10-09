import apiClient from "./apiClient";
import type {
  DashboardStats,
  ReportData,
  ReportFilters,
  ApiResponse,
} from "../types";

const endpoint = "/reports";

export const reportService = {
  async getDashboardStats(): Promise<ApiResponse<DashboardStats>> {
    // Use the correct analytics endpoint
    return apiClient.get("/applications/analytics");
  },

  async getReportData(
    filters?: ReportFilters
  ): Promise<ApiResponse<ReportData>> {
    const params = filters
      ? Object.fromEntries(
          Object.entries(filters).filter(([, v]) => v !== undefined)
        )
      : {};
    return apiClient.get(`${endpoint}/data`, { params });
  },

  async exportReports(
    filters?: ReportFilters,
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

  async getApplicationsReport(
    filters?: ReportFilters
  ): Promise<ApiResponse<ReportData>> {
    const params = filters
      ? Object.fromEntries(
          Object.entries(filters).filter(([, v]) => v !== undefined)
        )
      : {};
    return apiClient.get(`${endpoint}/applications`, { params });
  },

  async getPaymentsReport(filters?: {
    fromDate?: string;
    toDate?: string;
  }): Promise<ApiResponse<ReportData>> {
    const params = filters
      ? Object.fromEntries(
          Object.entries(filters).filter(([, v]) => v !== undefined)
        )
      : {};
    return apiClient.get(`${endpoint}/payments`, { params });
  },

  async getUserActivityReport(filters?: {
    userId?: number;
    fromDate?: string;
    toDate?: string;
  }): Promise<ApiResponse<ReportData>> {
    const params = filters
      ? Object.fromEntries(
          Object.entries(filters).filter(([, v]) => v !== undefined)
        )
      : {};
    return apiClient.get(`${endpoint}/user-activity`, { params });
  },

  async generateCustomReport(
    reportType: string,
    filters?: Record<string, unknown>
  ): Promise<ApiResponse<ReportData>> {
    return apiClient.post(`${endpoint}/custom`, {
      reportType,
      filters: filters || {},
    });
  },
};
