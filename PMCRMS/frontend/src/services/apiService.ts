// Main API Service - combines all service modules
import { authService } from "./authService";
import { applicationService } from "./applicationService";
import { userService } from "./userService";
import { documentService } from "./documentService";
import { statusService } from "./statusService";
import { paymentService } from "./paymentService";
import { reportService } from "./reportService";
import { getToken, setToken, removeToken } from "./apiClient";
import type {
  User,
  AuthResponse,
  LoginRequest,
  OtpVerificationRequest,
  ApplicationFilters,
} from "../types";

class ApiService {
  // Service modules
  auth = authService;
  applications = applicationService;
  users = userService;
  documents = documentService;
  status = statusService;
  payments = paymentService;
  reports = reportService;

  // Convenience methods for backward compatibility and auth management
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await this.auth.login(data);
    if (response.success && response.data) {
      setToken(response.data.token);
      localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));
      return response.data;
    }
    throw new Error(response.message || "Login failed");
  }

  async sendOtp(identifier: string): Promise<void> {
    const response = await this.auth.sendOtp(identifier);
    if (!response.success) {
      throw new Error(response.message || "Failed to send OTP");
    }
  }

  async verifyOtp(data: OtpVerificationRequest): Promise<AuthResponse> {
    const response = await this.auth.verifyOtp(data);
    if (response.success && response.data) {
      setToken(response.data.token);
      localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));
      return response.data;
    }
    throw new Error(response.message || "OTP verification failed");
  }

  async register(userData: Partial<User>): Promise<AuthResponse> {
    const response = await this.auth.register(userData);
    if (response.success && response.data) {
      setToken(response.data.token);
      localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));
      return response.data;
    }
    throw new Error(response.message || "Registration failed");
  }

  logout(): void {
    removeToken();
  }

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem("pmcrms_user");
    return userStr ? JSON.parse(userStr) : null;
  }

  isAuthenticated(): boolean {
    return !!getToken();
  }

  // Convenience method for updating user profile
  async updateUserProfile(userData: Partial<User>): Promise<User> {
    const response = await this.users.updateUserProfile(userData);
    if (response.success && response.data) {
      // Update localStorage
      localStorage.setItem("pmcrms_user", JSON.stringify(response.data));
      return response.data;
    }
    throw new Error(response.message || "Failed to update profile");
  }

  // Convenience method for getting dashboard stats
  async getDashboardStats() {
    const response = await this.reports.getDashboardStats();
    if (response.success && response.data) {
      return response.data;
    }
    throw new Error(response.message || "Failed to fetch dashboard stats");
  }

  // Convenience method for getting applications
  async getApplications(
    page = 1,
    pageSize = 10,
    filters?: ApplicationFilters
  ) {
    const response = await this.applications.getApplications(
      page,
      pageSize,
      filters
    );
    if (response.success && response.data) {
      return response.data;
    }
    throw new Error(response.message || "Failed to fetch applications");
  }
}

export const apiService = new ApiService();
export default apiService;
