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
   */
  async verifyAndSignDocuments(
    applicationId: number,
    request: VerifyAndSignRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(
      `${BASE_URL}/application/${applicationId}/verify-and-sign`,
      request
    );
    return response;
  },

  /**
   * Reject application with mandatory comments
   */
  async rejectApplication(
    applicationId: number,
    request: RejectApplicationRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(
      `${BASE_URL}/application/${applicationId}/reject`,
      request
    );
    return response;
  },
};

export default eeWorkflowService;
