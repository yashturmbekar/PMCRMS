import apiClient from "./apiClient";
import type {
  EEWorkflowStatusDto,
  VerifyAndSignRequest,
  RejectApplicationRequest,
  WorkflowActionResult,
} from "../types/eeceWorkflow";

const BASE_URL = "/ExecutiveEngineer";

export const eeWorkflowService = {
  /**
   * Get pending applications for the current EE officer (all position types)
   */
  async getPendingApplications(): Promise<EEWorkflowStatusDto[]> {
    const response = await apiClient.get(`${BASE_URL}/pending`);
    return response;
  },

  /**
   * Get completed applications for the current EE officer
   */
  async getCompletedApplications(): Promise<EEWorkflowStatusDto[]> {
    const response = await apiClient.get(`${BASE_URL}/completed`);
    return response;
  },

  /**
   * Get workflow status for a specific application
   */
  async getApplicationStatus(
    applicationId: number
  ): Promise<EEWorkflowStatusDto> {
    const response = await apiClient.get(
      `${BASE_URL}/application/${applicationId}/status`
    );
    return response;
  },

  /**
   * Generate OTP for digital signature
   * POST /api/ExecutiveEngineer/application/{id}/generate-otp
   */
  async generateOtpForSignature(
    applicationId: number
  ): Promise<{ success: boolean; message?: string; otp?: string }> {
    const response = await apiClient.post(
      `${BASE_URL}/application/${applicationId}/generate-otp`
    );
    return {
      success: true,
      message: response.message,
      otp: response.otp,
    };
  },

  /**
   * Verify documents, apply digital signature, and forward to City Engineer
   * POST /api/ExecutiveEngineer/verify-and-sign
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
   * Reject application with mandatory comments
   * POST /api/ExecutiveEngineer/reject
   */
  async rejectApplication(
    request: RejectApplicationRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(`${BASE_URL}/reject`, request);
    return response;
  },
};

export default eeWorkflowService;
