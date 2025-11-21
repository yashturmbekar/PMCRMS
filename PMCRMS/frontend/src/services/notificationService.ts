import apiClient from "./apiClient";
import type { Notification, ApiResponse } from "../types";

class NotificationService {
  async getNotifications(
    unreadOnly = false,
    page = 1,
    pageSize = 20
  ): Promise<ApiResponse<Notification[]>> {
    try {
      return await apiClient.get("/notifications", {
        params: { unreadOnly, page, pageSize },
      });
    } catch (error) {
      console.error("Error fetching notifications:", error);
      throw error;
    }
  }

  async markAsRead(notificationIds: number[]): Promise<ApiResponse> {
    try {
      return await apiClient.post("/notifications/mark-as-read", {
        notificationIds,
      });
    } catch (error) {
      console.error("Error marking notifications as read:", error);
      throw error;
    }
  }

  async markAllAsRead(): Promise<ApiResponse> {
    try {
      return await apiClient.post("/notifications/mark-all-as-read", {});
    } catch (error) {
      console.error("Error marking all notifications as read:", error);
      throw error;
    }
  }

  async deleteNotification(id: number): Promise<ApiResponse> {
    try {
      return await apiClient.delete(`/notifications/${id}`);
    } catch (error) {
      console.error("Error deleting notification:", error);
      throw error;
    }
  }
}

export const notificationService = new NotificationService();
