// Example usage of the new API service structure
import {
  authService,
  applicationService,
  reportService,
  userService,
  documentService,
  apiClient,
} from "../services";
import type {
  ReportFilters,
  ApplicationFilters,
  UserRole,
  CreateApplicationRequest,
  DocumentType,
} from "../types";

// Example: Authentication
export async function loginWithOtp(identifier: string, otp: string) {
  // Send OTP
  await authService.sendOtp(identifier);

  // Verify OTP
  const response = await authService.verifyOtp({
    identifier: identifier,
    otpCode: otp,
    purpose: "login",
  });

  return response.data;
}

// Example: Export reports (similar to your reference)
export async function exportReports(
  filters?: ReportFilters,
  format: "csv" | "xlsx" = "csv"
) {
  return reportService.exportReports(filters, format);
}

// Example: Export applications
export async function exportApplications(
  filters?: ApplicationFilters,
  format: "csv" | "xlsx" = "csv"
) {
  return applicationService.exportApplications(filters, format);
}

// Example: Export users
export async function exportUsers(
  filters?: { role?: UserRole; isActive?: boolean; search?: string },
  format: "csv" | "xlsx" = "csv"
) {
  return userService.exportUsers(filters, format);
}

// Example: Using direct API client for custom endpoints
export async function customApiCall(
  endpoint: string,
  data?: Record<string, unknown>
) {
  return apiClient.post(endpoint, data);
}

// Example: Get dashboard data
export async function getDashboardData() {
  const stats = await reportService.getDashboardStats();
  const reportData = await reportService.getReportData();

  return {
    stats: stats.data,
    reports: reportData.data,
  };
}

// Example: Complex application workflow
export async function createApplicationWithDocuments(
  applicationData: CreateApplicationRequest,
  documents: { file: File; type: DocumentType }[]
) {
  // Create application
  const appResponse = await applicationService.createApplication(
    applicationData
  );

  if (!appResponse.success || !appResponse.data) {
    throw new Error("Failed to create application");
  }

  const applicationId = appResponse.data.id;

  // Upload documents
  const documentPromises = documents.map(({ file, type }) =>
    documentService.uploadDocument(applicationId, file, type)
  );

  const documentResults = await Promise.all(documentPromises);

  return {
    application: appResponse.data,
    documents: documentResults,
  };
}
