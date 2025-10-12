/**
 * JE Workflow Service
 * Integrates with JEWorkflowController and PositionRegistration workflow endpoints
 * Provides comprehensive workflow management for Junior Engineer stage
 */

import apiClient from "./apiClient";
import type {
  JEWorkflowStatusDto,
  WorkflowHistoryDto,
  WorkflowTimelineEventDto,
  StartJEWorkflowRequestDto,
  ScheduleAppointmentRequestDto,
  CompleteAppointmentRequestDto,
  VerifyDocumentRequestDto,
  InitiateSignatureRequestDto,
  CompleteSignatureRequestDto,
  TransitionWorkflowRequestDto,
  BulkWorkflowActionRequestDto,
  WorkflowSummaryDto,
  WorkflowMetricsDto,
  WorkflowValidationResultDto,
  WorkflowActionResultDto,
  ApiResponse,
} from "../types/jeWorkflow";

const workflowEndpoint = "/JEWorkflow";
const positionEndpoint = "/PositionRegistration";

/**
 * JE Workflow Service
 * Manages complete workflow for Junior Engineer application processing
 */
export const jeWorkflowService = {
  // ============================================
  // Workflow Actions
  // ============================================

  /**
   * Start JE workflow - Auto-assign application to Junior Engineer
   * POST /api/JEWorkflow/start
   */
  async startWorkflow(
    request: StartJEWorkflowRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/start`, request);
  },

  /**
   * Schedule appointment with applicant
   * POST /api/JEWorkflow/schedule-appointment
   */
  async scheduleAppointment(
    request: ScheduleAppointmentRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/schedule-appointment`, request);
  },

  /**
   * Complete appointment and transition to document verification
   * POST /api/JEWorkflow/complete-appointment
   */
  async completeAppointment(
    request: CompleteAppointmentRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/complete-appointment`, request);
  },

  /**
   * Verify individual document
   * POST /api/JEWorkflow/verify-document
   */
  async verifyDocument(
    request: VerifyDocumentRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/verify-document`, request);
  },

  /**
   * Generate OTP for digital signature
   * POST /api/JEWorkflow/generate-otp-for-signature
   */
  async generateOtpForSignature(
    applicationId: number
  ): Promise<ApiResponse<any>> {
    return apiClient.post(`${workflowEndpoint}/generate-otp-for-signature`, {
      applicationId,
    });
  },

  /**
   * Complete all document verifications
   * POST /api/JEWorkflow/complete-verification/{applicationId}
   */
  async completeDocumentVerification(
    applicationId: number
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(
      `${workflowEndpoint}/complete-verification/${applicationId}`
    );
  },

  /**
   * Initiate digital signature process
   * POST /api/JEWorkflow/initiate-signature/{applicationId}
   */
  async initiateDigitalSignature(
    request: InitiateSignatureRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(
      `${workflowEndpoint}/initiate-signature/${request.applicationId}`,
      { documentPath: request.documentPath }
    );
  },

  /**
   * Complete digital signature with OTP
   * POST /api/JEWorkflow/complete-signature
   */
  async completeDigitalSignature(
    request: CompleteSignatureRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/complete-signature`, request);
  },

  // ============================================
  // Status Queries
  // ============================================

  /**
   * Get detailed workflow status
   * GET /api/JEWorkflow/status/{applicationId}
   */
  async getWorkflowStatus(
    applicationId: number
  ): Promise<ApiResponse<JEWorkflowStatusDto>> {
    return apiClient.get(`${workflowEndpoint}/status/${applicationId}`);
  },

  /**
   * Get workflow history with status and assignment changes
   * GET /api/JEWorkflow/history/{applicationId}
   */
  async getWorkflowHistory(
    applicationId: number
  ): Promise<ApiResponse<WorkflowHistoryDto>> {
    return apiClient.get(`${workflowEndpoint}/history/${applicationId}`);
  },

  /**
   * Get workflow timeline events
   * GET /api/JEWorkflow/timeline/{applicationId}
   */
  async getWorkflowTimeline(
    applicationId: number
  ): Promise<ApiResponse<WorkflowTimelineEventDto[]>> {
    return apiClient.get(`${workflowEndpoint}/timeline/${applicationId}`);
  },

  /**
   * Validate if workflow can proceed to next stage
   * GET /api/JEWorkflow/validate/{applicationId}
   */
  async validateWorkflowProgress(
    applicationId: number
  ): Promise<ApiResponse<WorkflowValidationResultDto>> {
    return apiClient.get(`${workflowEndpoint}/validate/${applicationId}`);
  },

  // ============================================
  // Reporting & Analytics
  // ============================================

  /**
   * Get workflow summary statistics
   * GET /api/JEWorkflow/summary?fromDate&toDate
   */
  async getWorkflowSummary(
    fromDate?: string,
    toDate?: string
  ): Promise<ApiResponse<WorkflowSummaryDto>> {
    const params: Record<string, string> = {};
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return apiClient.get(`${workflowEndpoint}/summary`, { params });
  },

  /**
   * Get workflow metrics with detailed analytics
   * GET /api/JEWorkflow/metrics?fromDate&toDate
   */
  async getWorkflowMetrics(
    fromDate?: string,
    toDate?: string
  ): Promise<ApiResponse<WorkflowMetricsDto>> {
    const params: Record<string, string> = {};
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return apiClient.get(`${workflowEndpoint}/metrics`, { params });
  },

  /**
   * Get applications assigned to specific officer
   * GET /api/JEWorkflow/officer/{officerId}/applications
   */
  async getOfficerApplications(
    officerId: number
  ): Promise<ApiResponse<JEWorkflowStatusDto[]>> {
    return apiClient.get(
      `${workflowEndpoint}/officer/${officerId}/applications`
    );
  },

  /**
   * Get applications by workflow stage/status
   * GET /api/JEWorkflow/stage/{status}
   */
  async getApplicationsByStage(
    status: string
  ): Promise<ApiResponse<JEWorkflowStatusDto[]>> {
    return apiClient.get(`${workflowEndpoint}/stage/${status}`);
  },

  // ============================================
  // Admin Operations
  // ============================================

  /**
   * Transition application to specific status (Admin override)
   * POST /api/JEWorkflow/transition
   */
  async transitionWorkflow(
    request: TransitionWorkflowRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/transition`, request);
  },

  /**
   * Perform bulk action on multiple applications
   * POST /api/JEWorkflow/bulk-action
   */
  async performBulkAction(
    request: BulkWorkflowActionRequestDto
  ): Promise<ApiResponse<WorkflowActionResultDto[]>> {
    return apiClient.post(`${workflowEndpoint}/bulk-action`, request);
  },

  /**
   * Retry failed workflow step
   * POST /api/JEWorkflow/retry/{applicationId}/{stepName}
   */
  async retryWorkflowStep(
    applicationId: number,
    stepName: string
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(
      `${workflowEndpoint}/retry/${applicationId}/${stepName}`
    );
  },

  /**
   * Cancel workflow and reject application
   * POST /api/JEWorkflow/cancel/{applicationId}
   */
  async cancelWorkflow(
    applicationId: number,
    reason: string
  ): Promise<ApiResponse<WorkflowActionResultDto>> {
    return apiClient.post(`${workflowEndpoint}/cancel/${applicationId}`, {
      reason,
    });
  },

  /**
   * Send reminders for delayed applications
   * POST /api/JEWorkflow/send-reminders
   */
  async sendDelayedReminders(): Promise<ApiResponse<{ sent: number }>> {
    return apiClient.post(`${workflowEndpoint}/send-reminders`);
  },

  // ============================================
  // PositionRegistration Workflow Integration
  // ============================================

  /**
   * Get workflow info from PositionRegistration endpoint
   * GET /api/PositionRegistration/{id}/workflow
   */
  async getPositionApplicationWorkflow(
    applicationId: number
  ): Promise<ApiResponse<JEWorkflowStatusDto>> {
    return apiClient.get(`${positionEndpoint}/${applicationId}/workflow`);
  },

  /**
   * Get workflow history from PositionRegistration endpoint
   * GET /api/PositionRegistration/{id}/workflow/history
   */
  async getPositionApplicationWorkflowHistory(
    applicationId: number
  ): Promise<ApiResponse<WorkflowHistoryDto>> {
    return apiClient.get(
      `${positionEndpoint}/${applicationId}/workflow/history`
    );
  },

  /**
   * Get workflow timeline from PositionRegistration endpoint
   * GET /api/PositionRegistration/{id}/workflow/timeline
   */
  async getPositionApplicationWorkflowTimeline(
    applicationId: number
  ): Promise<ApiResponse<WorkflowTimelineEventDto[]>> {
    return apiClient.get(
      `${positionEndpoint}/${applicationId}/workflow/timeline`
    );
  },

  // ============================================
  // Helper Methods
  // ============================================

  /**
   * Check if application is in JE workflow stage
   */
  isInJEWorkflowStage(status: string): boolean {
    const jeStatuses = [
      "JUNIOR_ENGINEER_PENDING",
      "APPOINTMENT_SCHEDULED",
      "DOCUMENT_VERIFICATION_PENDING",
      "DOCUMENT_VERIFICATION_IN_PROGRESS",
      "DOCUMENT_VERIFICATION_COMPLETED",
      "AWAITING_JE_DIGITAL_SIGNATURE",
    ];
    return jeStatuses.includes(status);
  },

  /**
   * Get human-readable stage name
   */
  getStageName(status: string): string {
    const stageNames: Record<string, string> = {
      JUNIOR_ENGINEER_PENDING: "Assignment Pending",
      APPOINTMENT_SCHEDULED: "Appointment Scheduled",
      DOCUMENT_VERIFICATION_PENDING: "Awaiting Document Verification",
      DOCUMENT_VERIFICATION_IN_PROGRESS: "Documents Being Verified",
      DOCUMENT_VERIFICATION_COMPLETED: "Documents Verified",
      AWAITING_JE_DIGITAL_SIGNATURE: "Awaiting JE Signature",
      ASSISTANT_ENGINEER_PENDING: "Forwarded to Assistant Engineer",
    };
    return stageNames[status] || status;
  },

  /**
   * Get progress color based on percentage
   */
  getProgressColor(percentage: number): string {
    if (percentage < 25) return "red";
    if (percentage < 50) return "orange";
    if (percentage < 75) return "yellow";
    if (percentage < 100) return "blue";
    return "green";
  },

  /**
   * Format timeline event type
   */
  formatEventType(eventType: string): string {
    return eventType
      .replace(/([A-Z])/g, " $1")
      .trim()
      .replace(/^\w/, (c) => c.toUpperCase());
  },
};

export default jeWorkflowService;
