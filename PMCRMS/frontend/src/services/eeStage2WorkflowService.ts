/**
 * EE Stage 2 Workflow Service
 * Handles API calls for Executive Engineer Stage 2 Certificate Digital Signature workflow
 */

import axios from "axios";
import { getApiUrl, getToken } from "./apiClient";

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
  comments?: string;
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
      const token = getToken();
      const response = await axios.get(`${getApiUrl()}/EEStage2/Pending`, {
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
      const token = getToken();
      const response = await axios.get(`${getApiUrl()}/EEStage2/Completed`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
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
      const token = getToken();
      const response = await axios.get(
        `${getApiUrl()}/EEStage2/Application/${applicationId}`,
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
      const token = getToken();
      const response = await axios.post(
        `${getApiUrl()}/EEStage2/GenerateOtp/${applicationId}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return {
          success: true,
          message: response.data.message,
          otpReference: response.data.data?.otpReference,
        };
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
    otpCode: string,
    comments?: string
  ): Promise<EEStage2SignResult> {
    try {
      const token = getToken();
      const requestData: EEStage2SignRequest = { otpCode, comments };
      const response = await axios.post(
        `${getApiUrl()}/EEStage2/Sign/${applicationId}`,
        requestData,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        }
      );
      if (response.data.success) {
        return {
          success: true,
          message: response.data.message,
          applicationId: response.data.data?.applicationId,
          newStatus: response.data.data?.newStatus,
          signedCertificateUrl: response.data.data?.signedCertificateUrl,
        };
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
      const token = getToken();
      const response = await axios.get(`${getApiUrl()}/EEStage2/Statistics`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
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
