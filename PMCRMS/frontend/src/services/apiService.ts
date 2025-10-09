import axios from "axios";
import type { AxiosInstance, AxiosResponse } from "axios";
import type {
  ApiResponse,
  AuthResponse,
  LoginRequest,
  OtpVerificationRequest,
  User,
  Application,
  CreateApplicationRequest,
  UpdateApplicationRequest,
  UpdateStatusRequest,
  ApplicationFilters,
  PaginatedResponse,
  DashboardStats,
  FileUploadResponse,
  ApplicationDocument,
  Payment,
  ReportData,
} from "../types";

class ApiService {
  private api: AxiosInstance;
  private baseURL = "http://localhost:5086/api";

  constructor() {
    this.api = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        "Content-Type": "application/json",
      },
    });

    // Request interceptor to add auth token
    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem("pmcrms_token");
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor to handle errors
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.logout();
          window.location.href = "/login";
        }
        return Promise.reject(error);
      }
    );
  }

  // Authentication Methods
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response: AxiosResponse<ApiResponse<AuthResponse>> =
      await this.api.post("/auth/login", data);
    if (response.data.success && response.data.data) {
      localStorage.setItem("pmcrms_token", response.data.data.token);
      localStorage.setItem(
        "pmcrms_user",
        JSON.stringify(response.data.data.user)
      );
      return response.data.data;
    }
    throw new Error(response.data.message || "Login failed");
  }

  async sendOtp(phoneNumber: string): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.post(
      "/auth/send-otp",
      { phoneNumber }
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to send OTP");
    }
  }

  async verifyOtp(data: OtpVerificationRequest): Promise<AuthResponse> {
    const response: AxiosResponse<ApiResponse<AuthResponse>> =
      await this.api.post("/auth/verify-otp", data);
    if (response.data.success && response.data.data) {
      localStorage.setItem("pmcrms_token", response.data.data.token);
      localStorage.setItem(
        "pmcrms_user",
        JSON.stringify(response.data.data.user)
      );
      return response.data.data;
    }
    throw new Error(response.data.message || "OTP verification failed");
  }

  async register(userData: Partial<User>): Promise<AuthResponse> {
    const response: AxiosResponse<ApiResponse<AuthResponse>> =
      await this.api.post("/auth/register", userData);
    if (response.data.success && response.data.data) {
      localStorage.setItem("pmcrms_token", response.data.data.token);
      localStorage.setItem(
        "pmcrms_user",
        JSON.stringify(response.data.data.user)
      );
      return response.data.data;
    }
    throw new Error(response.data.message || "Registration failed");
  }

  logout(): void {
    localStorage.removeItem("pmcrms_token");
    localStorage.removeItem("pmcrms_user");
  }

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem("pmcrms_user");
    return userStr ? JSON.parse(userStr) : null;
  }

  isAuthenticated(): boolean {
    return !!localStorage.getItem("pmcrms_token");
  }

  // Application Methods
  async getApplications(
    page = 1,
    pageSize = 10,
    filters?: ApplicationFilters
  ): Promise<PaginatedResponse<Application>> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
      ...filters,
    });

    const response: AxiosResponse<ApiResponse<PaginatedResponse<Application>>> =
      await this.api.get(`/applications?${params}`);

    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch applications");
  }

  async getApplication(id: number): Promise<Application> {
    const response: AxiosResponse<ApiResponse<Application>> =
      await this.api.get(`/applications/${id}`);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch application");
  }

  async createApplication(
    data: CreateApplicationRequest
  ): Promise<Application> {
    const response: AxiosResponse<ApiResponse<Application>> =
      await this.api.post("/applications", data);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to create application");
  }

  async updateApplication(
    data: UpdateApplicationRequest
  ): Promise<Application> {
    const response: AxiosResponse<ApiResponse<Application>> =
      await this.api.put(`/applications/${data.id}`, data);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to update application");
  }

  async deleteApplication(id: number): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.delete(
      `/applications/${id}`
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to delete application");
    }
  }

  // Status Management
  async updateApplicationStatus(
    applicationId: number,
    data: UpdateStatusRequest
  ): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.post(
      `/status/update/${applicationId}`,
      data
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to update status");
    }
  }

  async getPendingApplications(
    page = 1,
    pageSize = 10
  ): Promise<PaginatedResponse<Application>> {
    const response: AxiosResponse<ApiResponse<PaginatedResponse<Application>>> =
      await this.api.get(`/status/pending?page=${page}&pageSize=${pageSize}`);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(
      response.data.message || "Failed to fetch pending applications"
    );
  }

  // Document Management
  async uploadDocument(
    applicationId: number,
    file: File,
    documentType: string
  ): Promise<FileUploadResponse> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("documentType", documentType);

    const response: AxiosResponse<ApiResponse<FileUploadResponse>> =
      await this.api.post(`/documents/upload/${applicationId}`, formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });

    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to upload document");
  }

  async getApplicationDocuments(
    applicationId: number
  ): Promise<ApplicationDocument[]> {
    const response: AxiosResponse<ApiResponse<ApplicationDocument[]>> =
      await this.api.get(`/documents/application/${applicationId}`);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch documents");
  }

  async downloadDocument(documentId: number): Promise<Blob> {
    const response: AxiosResponse<Blob> = await this.api.get(
      `/documents/download/${documentId}`,
      {
        responseType: "blob",
      }
    );
    return response.data;
  }

  async deleteDocument(documentId: number): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.delete(
      `/documents/${documentId}`
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to delete document");
    }
  }

  // User Management
  async getUsers(page = 1, pageSize = 10): Promise<PaginatedResponse<User>> {
    const response: AxiosResponse<ApiResponse<PaginatedResponse<User>>> =
      await this.api.get(`/users?page=${page}&pageSize=${pageSize}`);
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch users");
  }

  async getUser(id: number): Promise<User> {
    const response: AxiosResponse<ApiResponse<User>> = await this.api.get(
      `/users/${id}`
    );
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch user");
  }

  async updateUserProfile(userData: Partial<User>): Promise<User> {
    const response: AxiosResponse<ApiResponse<User>> = await this.api.put(
      "/users/profile",
      userData
    );
    if (response.data.success && response.data.data) {
      // Update stored user data
      localStorage.setItem("pmcrms_user", JSON.stringify(response.data.data));
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to update profile");
  }

  async updateUserRole(userId: number, role: string): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.put(
      `/users/${userId}/role`,
      { role }
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to update user role");
    }
  }

  async updateUserStatus(userId: number, isActive: boolean): Promise<void> {
    const response: AxiosResponse<ApiResponse> = await this.api.put(
      `/users/${userId}/status`,
      { isActive }
    );
    if (!response.data.success) {
      throw new Error(response.data.message || "Failed to update user status");
    }
  }

  // Dashboard & Reports
  async getDashboardStats(): Promise<DashboardStats> {
    const response: AxiosResponse<ApiResponse<DashboardStats>> =
      await this.api.get("/dashboard/stats");
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch dashboard stats");
  }

  async getReportData(): Promise<ReportData> {
    const response: AxiosResponse<ApiResponse<ReportData>> = await this.api.get(
      "/reports/data"
    );
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch report data");
  }

  // Payment Methods
  async initiatePayment(
    applicationId: number,
    amount: number
  ): Promise<Payment> {
    const response: AxiosResponse<ApiResponse<Payment>> = await this.api.post(
      "/payments/initiate",
      { applicationId, amount }
    );
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to initiate payment");
  }

  async getPaymentStatus(paymentId: string): Promise<Payment> {
    const response: AxiosResponse<ApiResponse<Payment>> = await this.api.get(
      `/payments/status/${paymentId}`
    );
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch payment status");
  }

  async getApplicationPayments(applicationId: number): Promise<Payment[]> {
    const response: AxiosResponse<ApiResponse<Payment[]>> = await this.api.get(
      `/payments/application/${applicationId}`
    );
    if (response.data.success && response.data.data) {
      return response.data.data;
    }
    throw new Error(response.data.message || "Failed to fetch payments");
  }
}

export const apiService = new ApiService();
export default apiService;
