import apiClient from "./apiClient";
import type {
  AEWorkflowStatusDto,
  VerifyAndSignRequest,
  RejectApplicationRequest,
  WorkflowActionResult,
} from "../types/aeWorkflow";

const BASE_URL = "/AssistantEngineer";

export const aeWorkflowService = {
  /**
   * Get pending applications for the current AE officer filtered by position type
   */
  async getPendingApplications(
    positionType: string
  ): Promise<AEWorkflowStatusDto[]> {
    const response = await apiClient.get(`${BASE_URL}/pending/${positionType}`);
    return response;
  },

  /**
   * Get completed applications for the current AE officer filtered by position type
   */
  async getCompletedApplications(
    positionType: string
  ): Promise<AEWorkflowStatusDto[]> {
    const response = await apiClient.get(
      `${BASE_URL}/completed/${positionType}`
    );
    return response;
  },

  /**
   * Get workflow status for a specific application
   */
  async getApplicationStatus(
    applicationId: number,
    positionType: string
  ): Promise<AEWorkflowStatusDto> {
    const response = await apiClient.get(
      `${BASE_URL}/application/${applicationId}/status?positionType=${positionType}`
    );
    return response;
  },

  /**
   * Generate OTP for digital signature
   * POST /api/AssistantEngineer/application/{id}/generate-otp
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
   * Verify documents, apply digital signature, and forward to Executive Engineer
   * POST /api/AssistantEngineer/verify-and-sign
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
   * POST /api/AssistantEngineer/reject
   */
  async rejectApplication(
    request: RejectApplicationRequest
  ): Promise<WorkflowActionResult> {
    const response = await apiClient.post(`${BASE_URL}/reject`, request);
    return response;
  },
};

export default aeWorkflowService;
