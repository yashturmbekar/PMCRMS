/**
 * EE Stage 2 Workflow Service
 * Handles API calls for Executive Engineer Stage 2 Certificate Digital Signature workflow
 */

import axios from "axios";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5086";

// DTOs matching backend
export interface EEStage2ApplicationDto {
  id: number; // Backend uses 'id'
  applicationId?: number; // For compatibility
  applicationNumber: string;
  applicantName: string;
  applicantEmail: string;
  positionType: string; // Backend property name
  buildingType?: string; // Legacy/alternative property
  propertyAddress: string;
  paymentAmount?: number;
  processedByClerkDate?: string; // Backend property name
  clerkProcessedDate?: string; // Legacy/alternative property
  plotArea?: number; // Legacy property
  district?: string; // Legacy property
  clerkRemarks?: string;
  paymentDate?: string;
  paymentReference?: string;
  createdAt: string;
  updatedAt: string;
}

export interface EEStage2ApplicationDetailDto {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  applicantEmail: string;
  applicantContact: string;
  buildingType: string;
  plotArea: number;
  buildingArea: number;
  floors: number;
  district: string;
  taluka: string;
  village: string;
  clerkProcessedDate?: string;
  clerkRemarks?: string;
  paymentAmount: number;
  paymentDate?: string;
  paymentReference: string;
  certificateGeneratedDate?: string;
  certificatePath?: string;
  currentStatus: string;
  jeApprovedDate?: string;
  jeOfficerName?: string;
  aeApprovedDate?: string;
  aeOfficerName?: string;
  ee1ApprovedDate?: string;
  ee1OfficerName?: string;
  ce1ApprovedDate?: string;
  ce1OfficerName?: string;
}

export interface EEStage2OtpResult {
  success: boolean;
  message: string;
  otpReference?: string;
}

export interface EEStage2SignResult {
  success: boolean;
  message: string;
  applicationId?: number;
  newStatus?: string;
  signedCertificateUrl?: string;
}

export interface EEStage2SignRequest {
  otpCode: string;
}

export interface EEStage2Statistics {
  pendingCount: number;
  completedCount: number;
  todayProcessed: number;
  weekProcessed: number;
  monthProcessed: number;
}

class EEStage2WorkflowService {
  /**
   * Get pending applications for EE Stage 2 (status 18 - ProcessedByClerk)
   */
  async getPendingApplications(): Promise<EEStage2ApplicationDto[]> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const response = await axios.get(`${API_BASE_URL}/api/EEStage2/Pending`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      if (response.data.success) {
        return response.data.data as EEStage2ApplicationDto[];
      }
      throw new Error(
        response.data.message || "Failed to fetch pending applications"
      );
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }

  /**
   * Get completed applications by EE Stage 2
   */
  async getCompletedApplications(): Promise<EEStage2ApplicationDto[]> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const response = await axios.get(
        `${API_BASE_URL}/api/EEStage2/Completed`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as EEStage2ApplicationDto[];
      }
      throw new Error(
        response.data.message || "Failed to fetch completed applications"
      );
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }

  /**
   * Get detailed application information
   */
  async getApplicationDetails(
    applicationId: number
  ): Promise<EEStage2ApplicationDetailDto> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const response = await axios.get(
        `${API_BASE_URL}/api/EEStage2/Application/${applicationId}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as EEStage2ApplicationDetailDto;
      }
      throw new Error(
        response.data.message || "Failed to fetch application details"
      );
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }

  /**
   * Generate OTP for digital signature via HSM
   */
  async generateOtp(applicationId: number): Promise<EEStage2OtpResult> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const response = await axios.post(
        `${API_BASE_URL}/api/EEStage2/GenerateOtp/${applicationId}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as EEStage2OtpResult;
      }
      throw new Error(response.data.message || "Failed to generate OTP");
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }

  /**
   * Apply digital signature with OTP verification
   */
  async applyDigitalSignature(
    applicationId: number,
    otpCode: string
  ): Promise<EEStage2SignResult> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const requestData: EEStage2SignRequest = { otpCode };
      const response = await axios.post(
        `${API_BASE_URL}/api/EEStage2/Sign/${applicationId}`,
        requestData,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        }
      );
      if (response.data.success) {
        return response.data.data as EEStage2SignResult;
      }
      throw new Error(
        response.data.message || "Failed to apply digital signature"
      );
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }

  /**
   * Get statistics for EE Stage 2 dashboard
   */
  async getStatistics(): Promise<EEStage2Statistics> {
    try {
      const token = localStorage.getItem("pmcrms_token");
      const response = await axios.get(
        `${API_BASE_URL}/api/EEStage2/Statistics`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as EEStage2Statistics;
      }
      throw new Error(response.data.message || "Failed to fetch statistics");
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        throw new Error(
          error.response?.data?.message || error.message || "Network error"
        );
      }
      throw error;
    }
  }
}

export default new EEStage2WorkflowService();
