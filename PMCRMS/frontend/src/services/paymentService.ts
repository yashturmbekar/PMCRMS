import apiClient from "./apiClient";
import type {
  Payment,
  PaymentStatus,
  PaymentMethod,
  ApiResponse,
} from "../types";

const endpoint = "/payments";

export const paymentService = {
  async initiatePayment(
    applicationId: number,
    amount: number,
    method: PaymentMethod = "BillDesk"
  ): Promise<ApiResponse<Payment>> {
    return apiClient.post(`${endpoint}/initiate`, {
      applicationId,
      amount,
      method,
    });
  },

  async getPaymentStatus(paymentId: string): Promise<ApiResponse<Payment>> {
    return apiClient.get(`${endpoint}/status/${paymentId}`);
  },

  async getApplicationPayments(
    applicationId: number
  ): Promise<ApiResponse<Payment[]>> {
    return apiClient.get(`${endpoint}/application/${applicationId}`);
  },

  async updatePaymentStatus(
    paymentId: string,
    status: PaymentStatus,
    transactionId?: string,
    gatewayResponse?: string
  ): Promise<ApiResponse> {
    return apiClient.patch(`${endpoint}/${paymentId}/status`, {
      status,
      transactionId,
      gatewayResponse,
    });
  },

  async refundPayment(paymentId: string, reason: string): Promise<ApiResponse> {
    return apiClient.post(`${endpoint}/${paymentId}/refund`, { reason });
  },

  async getPaymentHistory(
    page = 1,
    pageSize = 10,
    filters?: {
      status?: PaymentStatus;
      method?: PaymentMethod;
      fromDate?: string;
      toDate?: string;
    }
  ): Promise<ApiResponse<Payment[]>> {
    const params = {
      page,
      pageSize,
      ...(filters
        ? Object.fromEntries(
            Object.entries(filters).filter(([, v]) => v !== undefined)
          )
        : {}),
    };
    return apiClient.get(`${endpoint}/history`, { params });
  },

  async exportPayments(
    filters?: {
      status?: PaymentStatus;
      method?: PaymentMethod;
      fromDate?: string;
      toDate?: string;
    },
    format: "csv" | "xlsx" = "csv"
  ): Promise<Blob> {
    const params = {
      ...(filters
        ? Object.fromEntries(
            Object.entries(filters).filter(([, v]) => v !== undefined)
          )
        : {}),
      format,
    };
    return apiClient.get(`${endpoint}/export`, {
      params,
      responseType: "blob",
    });
  },
};
