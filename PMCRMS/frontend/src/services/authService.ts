import apiClient from "./apiClient";
import type {
  AuthResponse,
  LoginRequest,
  OtpVerificationRequest,
  User,
  ApiResponse,
} from "../types";

const endpoint = "/auth";

export const authService = {
  async sendOtp(identifier: string): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/send-otp`, { identifier });
  },

  async verifyOtp(
    data: OtpVerificationRequest
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/verify-otp`, data);
  },

  async login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/login`, data);
  },

  async register(userData: Partial<User>): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/register`, userData);
  },

  async refreshToken(): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/refresh-token`);
  },

  async forgotPassword(email: string): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/forgot-password`, { email });
  },

  async resetPassword(
    token: string,
    newPassword: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/reset-password`, { token, newPassword });
  },
};
