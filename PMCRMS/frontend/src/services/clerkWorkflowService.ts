/**
 * Clerk Workflow Service
 * Handles API calls for Clerk application processing workflow
 */

import axios from "axios";
import { getApiUrl, getToken } from "./apiClient";

export interface ClerkApplicationDto {
  id: number;
  applicationNumber: string;
  applicantName: string;
  applicantEmail: string;
  applicantMobile: string;
  positionType: string; // Changed from applicationType to match backend
  assignedAEName: string | null;
  assignedToClerkDate: string | null;
  submittedDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface ClerkApplicationDetailDto extends ClerkApplicationDto {
  currentStatus: string;
  transactionId: string | null;
  bdOrderId: string | null;
  statusHistoryCount: number;
}

export interface ClerkActionResult {
  success: boolean;
  message: string;
  applicationId: number;
  newStatus: string;
}

export interface ClerkStatistics {
  pendingCount: number;
  completedCount: number;
  totalProcessed: number;
  todayProcessed: number;
  weekProcessed: number;
  monthProcessed: number;
}

export const clerkWorkflowService = {
  /**
   * Get pending applications for clerk review (PaymentCompleted status)
   */
  getPendingApplications: async (): Promise<ClerkApplicationDto[]> => {
    const token = getToken();
    const response = await axios.get(`${getApiUrl()}/Clerk/pending`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return response.data.data;
  },

  /**
   * Get completed applications processed by clerk
   */
  getCompletedApplications: async (): Promise<ClerkApplicationDto[]> => {
    const token = getToken();
    const response = await axios.get(`${getApiUrl()}/Clerk/completed`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return response.data.data;
  },

  /**
   * Get application details for clerk review
   */
  getApplicationDetails: async (
    id: number
  ): Promise<ClerkApplicationDetailDto> => {
    const token = getToken();
    const response = await axios.get(`${getApiUrl()}/Clerk/application/${id}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return response.data.data;
  },

  /**
   * Approve application and forward to EE Stage 2
   */
  approveApplication: async (
    id: number,
    remarks?: string
  ): Promise<ClerkActionResult> => {
    const token = getToken();
    const response = await axios.post(
      `${getApiUrl()}/Clerk/approve/${id}`,
      { Remarks: remarks || "" }, // Capital R to match C# property
      {
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
      }
    );
    // Return the full response data which includes success, message, data
    return response.data;
  },

  /**
   * Reject application with reason
   */
  rejectApplication: async (
    id: number,
    reason: string
  ): Promise<ClerkActionResult> => {
    const token = getToken();
    const response = await axios.post(
      `${getApiUrl()}/Clerk/reject/${id}`,
      { Reason: reason }, // Capital R to match C# property
      {
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
      }
    );
    return response.data; // Return full response data
  },

  /**
   * Get statistics for clerk dashboard
   */
  getStatistics: async (): Promise<ClerkStatistics> => {
    const token = getToken();
    const response = await axios.get(`${getApiUrl()}/Clerk/statistics`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return response.data.data;
  },
};
