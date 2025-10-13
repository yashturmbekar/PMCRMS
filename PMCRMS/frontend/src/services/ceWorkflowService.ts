import apiClient from "./apiClient";
import type {
  CEWorkflowStatusDto,
  VerifyAndSignRequest,
  RejectApplicationRequest,
  WorkflowActionResult,
} from "../types/eeceWorkflow";

const BASE_URL = "/CityEngineer";

export const ceWorkflowService = {
  /**
   * Get pending applications for the current CE officer (all position types - Final Approval)
   */
  async getPendingApplications(): Promise<CEWorkflowStatusDto[]> {
    const response = await apiClient.get(`${BASE_URL}/pending`);
    return response;
  },

  /**
   * Get completed applications for the current CE officer
   */
  async getCompletedApplications(): Promise<CEWorkflowStatusDto[]> {
    const response = await apiClient.get(`${BASE_URL}/completed`);
    return response;
  },

  /**
   * Get workflow status for a specific application
   */
  async getApplicationStatus(
    applicationId: number
  ): Promise<CEWorkflowStatusDto> {
    const response = await apiClient.get(
      `${BASE_URL}/application/${applicationId}/status`
    );
    return response;
  },

  /**
   * Generate OTP for digital signature (Final Approval)
   */
  async generateOtpForSignature(
    applicationId: number
  ): Promise<{ success: boolean; message?: string; otp?: string }> {
    const response = await apiClient.post(
      `${BASE_URL}/generate-otp-for-signature`,
      { applicationId }
    );
    return {
      success: true,
      message: response.message,
      otp: response.otp,
    };
  },

  /**
   * Verify documents, apply digital signature, and set FINAL APPROVAL
   * POST /api/CityEngineer/verify-and-sign
   */
  async verifyAndSignDocuments(
    request: VerifyAndSignRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(
      `${BASE_URL}/verify-and-sign`,
      request
    );
    return response;
  },

  /**
   * Reject application with mandatory comments (FINAL REJECTION)
   * POST /api/CityEngineer/reject
   */
  async rejectApplication(
    request: RejectApplicationRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(`${BASE_URL}/reject`, request);
    return response;
  },
};

export default ceWorkflowService;
