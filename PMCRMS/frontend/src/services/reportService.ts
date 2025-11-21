import apiClient from "./apiClient";
import type {
  DashboardStats,
  ReportData,
  ReportFilters,
  ApiResponse,
} from "../types";
import type {
  GetPositionSummariesResponse,
  GetStageSummariesResponse,
  GetApplicationsByStageRequest,
  GetApplicationsByStageResponse,
} from "../types/reports";

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

  // ========== PMC Admin Drill-Down Reports ==========

  /**
   * Get position-level summary with application counts
   * @returns Promise containing position summaries
   */
  async getPositionSummaries(): Promise<
    ApiResponse<GetPositionSummariesResponse>
  > {
    try {
      const response = await apiClient.get("/api/admin/reports/positions");
      return {
        success: true,
        data: response.data,
        message: "Position summaries retrieved successfully",
      };
    } catch (error: any) {
      console.error("Error fetching position summaries:", error);
      return {
        success: false,
        message:
          error.response?.data?.message || "Failed to fetch position summaries",
      };
    }
  },

  /**
   * Get stage-level summary for a specific position
   * @param positionType - The position type to get stage summaries for
   * @returns Promise containing stage summaries
   */
  async getStageSummaries(
    positionType: string
  ): Promise<ApiResponse<GetStageSummariesResponse>> {
    try {
      const response = await apiClient.get(
        `/api/admin/reports/positions/${positionType}/stages`
      );
      return {
        success: true,
        data: response.data,
        message: "Stage summaries retrieved successfully",
      };
    } catch (error: any) {
      console.error("Error fetching stage summaries:", error);
      return {
        success: false,
        message:
          error.response?.data?.message || "Failed to fetch stage summaries",
      };
    }
  },

  /**
   * Get detailed applications list for a specific position and stage
   * @param request - Filter and pagination parameters
   * @returns Promise containing paginated applications
   */
  async getApplicationsByStage(
    request: GetApplicationsByStageRequest
  ): Promise<ApiResponse<GetApplicationsByStageResponse>> {
    try {
      const params: any = {
        pageNumber: request.pageNumber || 1,
        pageSize: request.pageSize || 20,
      };

      if (request.searchTerm) {
        params.searchTerm = request.searchTerm;
      }

      if (request.sortBy) {
        params.sortBy = request.sortBy;
        params.sortDirection = request.sortDirection || "desc";
      }

      const response = await apiClient.get(
        `/api/admin/reports/positions/${request.positionType}/stages/${request.stageName}/applications`,
        { params }
      );

      return {
        success: true,
        data: response.data,
        message: "Applications retrieved successfully",
      };
    } catch (error: any) {
      console.error("Error fetching applications by stage:", error);
      return {
        success: false,
        message:
          error.response?.data?.message || "Failed to fetch applications",
      };
    }
  },
};
