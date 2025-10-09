# PMCRMS API Services

This document describes the new structured API service architecture for the PMCRMS frontend application.

## Architecture Overview

The API services are now organized into modular, specialized services that handle different aspects of the application:

```
src/services/
├── apiClient.ts          # Core HTTP client with interceptors
├── authService.ts        # Authentication endpoints
├── applicationService.ts # Application management
├── userService.ts        # User management
├── documentService.ts    # Document handling
├── statusService.ts      # Status updates and workflow
├── paymentService.ts     # Payment processing
├── reportService.ts      # Reports and analytics
├── apiService.ts         # Main service combining all modules
└── index.ts             # Export barrel file
```

## Core Features

### 1. **Centralized HTTP Client** (`apiClient.ts`)

- Axios-based HTTP client with request/response interceptors
- Automatic token management
- Error handling and retry logic
- Support for file uploads with extended timeouts

### 2. **Modular Service Structure**

Each service module handles a specific domain:

```typescript
// Example usage
import { authService, applicationService } from "../services";

// Authentication
await authService.sendOtp("user@example.com");
await authService.verifyOtp({
  phoneNumber: "user@example.com",
  otp: "123456",
  purpose: "login",
});

// Applications
const applications = await applicationService.getApplications(1, 10, {
  status: "Pending",
});
```

### 3. **Export Functions with Filters**

Similar to your reference example, all services support filtered exports:

```typescript
// Export reports with filters
export async function exportReports(
  filters?: ReportFilters,
  format: "csv" | "xlsx" = "csv"
) {
  const params = {
    ...(filters
      ? Object.fromEntries(
          Object.entries(filters).filter(([, v]) => v !== undefined)
        )
      : {}),
    format,
  };
  return apiClient.get(`/reports/export`, { params, responseType: "blob" });
}
```

## Service Modules

### Authentication Service (`authService.ts`)

```typescript
const authService = {
  sendOtp(identifier: string): Promise<ApiResponse>
  verifyOtp(data: OtpVerificationRequest): Promise<ApiResponse<AuthResponse>>
  login(data: LoginRequest): Promise<ApiResponse<AuthResponse>>
  register(userData: Partial<User>): Promise<ApiResponse<AuthResponse>>
  refreshToken(): Promise<ApiResponse<AuthResponse>>
  forgotPassword(email: string): Promise<ApiResponse>
  resetPassword(token: string, newPassword: string): Promise<ApiResponse>
}
```

### Application Service (`applicationService.ts`)

```typescript
const applicationService = {
  getApplications(page, pageSize, filters): Promise<ApiResponse<PaginatedResponse<Application>>>
  getApplication(id): Promise<ApiResponse<Application>>
  createApplication(data): Promise<ApiResponse<Application>>
  updateApplication(data): Promise<ApiResponse<Application>>
  deleteApplication(id): Promise<ApiResponse>
  exportApplications(filters, format): Promise<Blob>
  // ... more methods
}
```

### Report Service (`reportService.ts`)

```typescript
const reportService = {
  getDashboardStats(): Promise<ApiResponse<DashboardStats>>
  getReportData(filters): Promise<ApiResponse<ReportData>>
  exportReports(filters, format): Promise<Blob>
  getApplicationsReport(filters): Promise<ApiResponse<ReportData>>
  // ... more methods
}
```

## Usage Examples

### Basic Usage

```typescript
import { authService, applicationService } from "../services";

// Login flow
const loginUser = async (email: string, otp: string) => {
  await authService.sendOtp(email);
  const response = await authService.verifyOtp({
    phoneNumber: email,
    otp,
    purpose: "login",
  });
  return response.data;
};

// Get applications with filters
const getFilteredApplications = async () => {
  const response = await applicationService.getApplications(1, 10, {
    status: "Pending",
    type: "NewConstruction",
  });
  return response.data;
};
```

### Export Functions

```typescript
import { reportService, applicationService } from "../services";

// Export reports
const downloadReports = async () => {
  const blob = await reportService.exportReports(
    {
      fromDate: "2024-01-01",
      toDate: "2024-12-31",
      status: "Completed",
    },
    "xlsx"
  );

  // Create download link
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "reports.xlsx";
  link.click();
};
```

### Advanced Workflows

```typescript
import { applicationService, documentService } from "../services";

// Create application with documents
const createCompleteApplication = async (appData, documents) => {
  // Create application
  const appResponse = await applicationService.createApplication(appData);

  if (!appResponse.success) {
    throw new Error("Failed to create application");
  }

  // Upload documents
  const documentPromises = documents.map(({ file, type }) =>
    documentService.uploadDocument(appResponse.data.id, file, type)
  );

  const documentResults = await Promise.all(documentPromises);

  return {
    application: appResponse.data,
    documents: documentResults,
  };
};
```

## Migration from Old Service

The main `apiService` class is still available for backward compatibility:

```typescript
import apiService from "../services/apiService";

// Old way (still works)
await apiService.sendOtp(email);
const user = apiService.getCurrentUser();

// New way (recommended)
import { authService } from "../services";
await authService.sendOtp(email);
```

## Configuration

### Constants (`constants/index.ts`)

```typescript
export const API_TIMEOUT = 30000; // 30 seconds
export const FILE_UPLOAD_TIMEOUT = 300000; // 5 minutes
export const AUTH_TOKEN_KEY = "pmcrms_token";
export const UNAUTH_ROUTES = ["/auth/send-otp", "/auth/verify-otp"];
```

### Environment Variables

```env
VITE_API_BASE_URL=http://localhost:5086
```

## Benefits

1. **Modular Architecture**: Each service handles a specific domain
2. **Type Safety**: Full TypeScript support with proper typing
3. **Reusability**: Services can be used independently
4. **Maintainability**: Clear separation of concerns
5. **Extensibility**: Easy to add new services or endpoints
6. **Export Support**: Built-in support for data export in multiple formats
7. **Error Handling**: Centralized error handling with user-friendly messages
8. **Token Management**: Automatic token handling and refresh

## Adding New Services

To add a new service:

1. Create a new service file (e.g., `notificationService.ts`)
2. Define the service endpoints and methods
3. Export the service from `index.ts`
4. Add it to the main `apiService` class if needed

```typescript
// notificationService.ts
import apiClient from "./apiClient";

const endpoint = "/notifications";

export const notificationService = {
  async getNotifications(page = 1, pageSize = 10) {
    return apiClient.get(endpoint, { params: { page, pageSize } });
  },

  async markAsRead(id: number) {
    return apiClient.patch(`${endpoint}/${id}/read`);
  },
};
```
