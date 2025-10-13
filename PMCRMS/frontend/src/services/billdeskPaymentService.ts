// BillDesk Payment Service for PMCRMS
// Handles payment initiation, status checking, and history

import axios from "axios";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5086";

// Ensure API_BASE_URL includes /api path
const API_URL = API_BASE_URL.endsWith("/api")
  ? API_BASE_URL
  : `${API_BASE_URL}/api`;

// ==================== INTERFACES ====================

export interface InitiatePaymentRequest {
  applicationId: number;
}

export interface PaymentResponse {
  success: boolean;
  message: string;
  data?: {
    bdOrderId?: string;
    rData?: string;
    paymentGatewayUrl?: string;
  };
  gatewayUrl?: string; // For backward compatibility
  transactionId?: string;
  bdOrderId?: string;
}

export interface PaymentStatusResponse {
  success: boolean;
  message: string;
  isPaymentComplete: boolean;
  paymentStatus?: string;
  transactionId?: string;
  bdOrderId?: string;
  amountPaid?: number;
  paymentDate?: string;
}

export interface Transaction {
  id: string;
  transactionId: string;
  bdOrderId: string;
  status: string;
  price: number;
  amountPaid: number;
  applicationId: number;
  bankAccountNumber?: string;
  bankIfscCode?: string;
  mode?: string;
  platform?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PaymentHistoryResponse {
  success: boolean;
  message: string;
  data?: Transaction[];
}

export interface VerifyPaymentRequest {
  applicationId: number;
  bdOrderId: string;
}

// Backend API Response Structure
interface PaymentInitiateApiResponse {
  success: boolean;
  message: string;
  data?: {
    bdOrderId: string;
    rData: string;
    paymentGatewayUrl: string;
  };
  error?: string;
}

// ==================== PAYMENT SERVICE CLASS ====================

class BillDeskPaymentService {
  private getAuthHeader() {
    const token =
      localStorage.getItem("pmcrms_token") || localStorage.getItem("token");
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  /**
   * Initiate a new payment for an application
   * @param applicationId - The application ID to pay for
   * @returns Payment response with gateway URL to redirect user
   */
  async initiatePayment(applicationId: number): Promise<PaymentResponse> {
    try {
      const response = await axios.post<PaymentInitiateApiResponse>(
        `${API_URL}/Payment/Initiate`,
        { applicationId },
        { headers: this.getAuthHeader() }
      );

      const responseData = response.data;

      // Handle nested response structure
      if (responseData.success && responseData.data) {
        return {
          success: true,
          message: responseData.message || "Payment initiated successfully",
          data: responseData.data,
          gatewayUrl: responseData.data.paymentGatewayUrl,
          bdOrderId: responseData.data.bdOrderId,
        };
      }

      return responseData;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response) {
        throw new Error(
          error.response.data.message || "Failed to initiate payment"
        );
      }
      throw new Error("Network error: Unable to initiate payment");
    }
  }

  /**
   * Get current payment status for an application
   * @param applicationId - The application ID
   * @returns Payment status details
   */
  async getPaymentStatus(
    applicationId: number
  ): Promise<PaymentStatusResponse> {
    try {
      const response = await axios.get<PaymentStatusResponse>(
        `${API_URL}/Payment/Status/${applicationId}`,
        { headers: this.getAuthHeader() }
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response) {
        throw new Error(
          error.response.data.message || "Failed to get payment status"
        );
      }
      throw new Error("Network error: Unable to fetch payment status");
    }
  }

  /**
   * Get payment transaction history for an application
   * @param applicationId - The application ID
   * @returns List of all payment transactions
   */
  async getPaymentHistory(
    applicationId: number
  ): Promise<PaymentHistoryResponse> {
    try {
      const response = await axios.get<PaymentHistoryResponse>(
        `${API_URL}/Payment/History/${applicationId}`,
        { headers: this.getAuthHeader() }
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response) {
        throw new Error(
          error.response.data.message || "Failed to get payment history"
        );
      }
      throw new Error("Network error: Unable to fetch payment history");
    }
  }

  /**
   * Verify payment with BillDesk
   * @param request - Verification request with applicationId and bdOrderId
   * @returns Verification result
   */
  async verifyPayment(request: VerifyPaymentRequest): Promise<PaymentResponse> {
    try {
      const response = await axios.post<PaymentResponse>(
        `${API_URL}/Payment/Verify`,
        request,
        { headers: this.getAuthHeader() }
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response) {
        throw new Error(
          error.response.data.message || "Failed to verify payment"
        );
      }
      throw new Error("Network error: Unable to verify payment");
    }
  }

  // ==================== HELPER METHODS ====================

  /**
   * Format amount in Indian Rupees
   */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  }

  /**
   * Format date to readable format
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat("en-IN", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }).format(date);
  }

  /**
   * Get status badge color classes
   */
  getStatusColor(status: string): string {
    switch (status?.toUpperCase()) {
      case "SUCCESS":
        return "bg-green-100 text-green-800";
      case "PENDING":
        return "bg-yellow-100 text-yellow-800";
      case "FAILED":
        return "bg-red-100 text-red-800";
      case "INITIATED":
        return "bg-blue-100 text-blue-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  }

  /**
   * Check if user can initiate payment
   * @param applicationStatus - Current application status code
   * @param isPaymentComplete - Whether payment is already completed
   */
  canInitiatePayment(
    applicationStatus: number,
    isPaymentComplete: boolean
  ): boolean {
    const APPROVED_BY_CE1_STATUS = 13;
    return applicationStatus === APPROVED_BY_CE1_STATUS && !isPaymentComplete;
  }
}

// Export singleton instance
const billdeskPaymentService = new BillDeskPaymentService();
export default billdeskPaymentService;
