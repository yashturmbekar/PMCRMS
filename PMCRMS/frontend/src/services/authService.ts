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
   * System Admin login with email and password
   */
  async adminLogin(
    email: string,
    password: string
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/admin-login`, { email, password });
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
   * First-time password change for newly invited officers
   */
  async changePasswordFirstTime(
    temporaryPassword: string,
    newPassword: string,
    confirmPassword: string
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/change-password-first-time`, {
      temporaryPassword,
      newPassword,
      confirmPassword,
    });
  },

  /**
   * Complete officer profile after first-time password change
   */
  async completeProfile(
    name?: string,
    phoneNumber?: string,
    department?: string
  ): Promise<ApiResponse<User>> {
    return apiClient.post(`${endpoint}/complete-profile`, {
      name,
      phoneNumber,
      department,
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

  /**
   * Change password for authenticated officer
   */
  async officerChangePassword(
    currentPassword: string,
    newPassword: string,
    confirmPassword: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/officer/change-password`, {
      currentPassword,
      newPassword,
      confirmPassword,
    });
  },

  /**
   * Request password reset for officer (forgot password)
   */
  async officerForgotPassword(email: string): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/officer/forgot-password`, { email });
  },

  /**
   * Reset officer password using reset token
   */
  async officerResetPassword(
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/officer/reset-password`, {
      token,
      newPassword,
      confirmPassword,
    });
  },

  /**
   * Validate officer password reset token
   */
  async validateOfficerResetToken(
    token: string
  ): Promise<ApiResponse<{ officerName: string; email: string }>> {
    return apiClient.get(`${endpoint}/officer/validate-reset-token/${token}`);
  },

  /**
   * Validate officer invitation token
   */
  async validateInvitationToken(
    token: string
  ): Promise<ApiResponse<{ name: string; email: string; role: string }>> {
    return apiClient.get(`${endpoint}/validate-invitation/${token}`);
  },

  /**
   * Set password for officer using invitation token
   */
  async setPassword(
    token: string,
    password: string,
    confirmPassword: string
  ): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post(`${endpoint}/set-password`, {
      token,
      password,
      confirmPassword,
    });
  },
};
