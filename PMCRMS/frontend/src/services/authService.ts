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
  /**
   * Send OTP to user's email for authentication
   */
  async sendOtp(
    email: string,
    purpose: string = "LOGIN"
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/send-otp`, { email, purpose });
  },

  /**
   * Verify OTP code and authenticate user
   */
  async verifyOtp(
    data: OtpVerificationRequest
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/verify-otp`, data);
  },

  /**
   * Officer login with email and password
   */
  async officerLogin(
    email: string,
    password: string
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/officer-login`, { email, password });
  },

  /**
   * Change password for authenticated user
   */
  async changePassword(
    currentPassword: string,
    newPassword: string,
    confirmPassword: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/change-password`, {
      currentPassword,
      newPassword,
      confirmPassword,
    });
  },

  /**
   * Legacy login method (for backward compatibility)
   */
  async login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/login`, data);
  },

  /**
   * Register new user
   */
  async register(userData: Partial<User>): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/register`, userData);
  },

  /**
   * Refresh authentication token
   */
  async refreshToken(): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/refresh-token`);
  },

  /**
   * Request password reset
   */
  async forgotPassword(email: string): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/forgot-password`, { email });
  },

  /**
   * Reset password using token
   */
  async resetPassword(
    token: string,
    newPassword: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/reset-password`, { token, newPassword });
  },
};
