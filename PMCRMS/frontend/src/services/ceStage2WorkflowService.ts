/**
 * CE Stage 2 Workflow Service
 * Handles API calls for City Engineer Stage 2 Final Certificate Signature workflow
 */

import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5062";

// DTOs matching backend
export interface CEStage2ApplicationDto {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  buildingType: string;
  plotArea: number;
  district: string;
  ee2SignedDate?: string;
  paymentAmount: number;
  paymentDate?: string;
  paymentReference: string;
}

export interface CEStage2ApplicationDetailDto {
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
  ee2SignedDate?: string;
  paymentAmount: number;
  paymentDate?: string;
  paymentReference: string;
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

export interface CEStage2OtpResult {
  success: boolean;
  message: string;
  otpReference?: string;
}

export interface CEStage2SignResult {
  success: boolean;
  message: string;
  applicationId?: number;
  newStatus?: string;
  signedCertificateUrl?: string;
}

export interface CEStage2SignRequest {
  otpCode: string;
}

export interface CEStage2Statistics {
  pendingCount: number;
  completedCount: number;
  todayProcessed: number;
  weekProcessed: number;
  monthProcessed: number;
}

class CEStage2WorkflowService {
  /**
   * Get pending applications for CE Stage 2 (status 20 - DigitalSignatureCompletedByEE2)
   */
  async getPendingApplications(): Promise<CEStage2ApplicationDto[]> {
    try {
      const token = localStorage.getItem("token");
      const response = await axios.get(`${API_BASE_URL}/api/CEStage2/Pending`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      if (response.data.success) {
        return response.data.data as CEStage2ApplicationDto[];
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
   * Get completed applications by CE Stage 2
   */
  async getCompletedApplications(): Promise<CEStage2ApplicationDto[]> {
    try {
      const token = localStorage.getItem("token");
      const response = await axios.get(
        `${API_BASE_URL}/api/CEStage2/Completed`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as CEStage2ApplicationDto[];
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
  ): Promise<CEStage2ApplicationDetailDto> {
    try {
      const token = localStorage.getItem("token");
      const response = await axios.get(
        `${API_BASE_URL}/api/CEStage2/Application/${applicationId}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as CEStage2ApplicationDetailDto;
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
   * Generate OTP for final digital signature via HSM
   */
  async generateOtp(applicationId: number): Promise<CEStage2OtpResult> {
    try {
      const token = localStorage.getItem("token");
      const response = await axios.post(
        `${API_BASE_URL}/api/CEStage2/GenerateOtp/${applicationId}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as CEStage2OtpResult;
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
   * Apply final digital signature with OTP verification
   */
  async applyFinalSignature(
    applicationId: number,
    otpCode: string
  ): Promise<CEStage2SignResult> {
    try {
      const token = localStorage.getItem("token");
      const requestData: CEStage2SignRequest = { otpCode };
      const response = await axios.post(
        `${API_BASE_URL}/api/CEStage2/Sign/${applicationId}`,
        requestData,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        }
      );
      if (response.data.success) {
        return response.data.data as CEStage2SignResult;
      }
      throw new Error(
        response.data.message || "Failed to apply final signature"
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
   * Get statistics for CE Stage 2 dashboard
   */
  async getStatistics(): Promise<CEStage2Statistics> {
    try {
      const token = localStorage.getItem("token");
      const response = await axios.get(
        `${API_BASE_URL}/api/CEStage2/Statistics`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (response.data.success) {
        return response.data.data as CEStage2Statistics;
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

export default new CEStage2WorkflowService();
