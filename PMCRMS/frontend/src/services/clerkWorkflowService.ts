import axios from "axios";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5086";

// Ensure API_BASE_URL includes /api path
const API_URL = API_BASE_URL.endsWith("/api")
  ? API_BASE_URL
  : `${API_BASE_URL}/api`;

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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.get(`${API_URL}/Clerk/pending`, {
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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.get(`${API_URL}/Clerk/completed`, {
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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.get(`${API_URL}/Clerk/application/${id}`, {
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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.post(
      `${API_URL}/Clerk/approve/${id}`,
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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.post(
      `${API_URL}/Clerk/reject/${id}`,
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
    const token = localStorage.getItem("pmcrms_token");
    const response = await axios.get(`${API_URL}/Clerk/statistics`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    return response.data.data;
  },
};
