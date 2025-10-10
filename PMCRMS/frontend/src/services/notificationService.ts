import axios from 'axios';
import type { Notification, NotificationSummary, ApiResponse } from '../types';

const API_BASE_URL = 'http://localhost:5086/api';

class NotificationService {
  private getAuthHeaders() {
    const token = localStorage.getItem('pmcrms_token');
    return {
      Authorization: token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json',
    };
  }

  async getNotifications(unreadOnly = false, page = 1, pageSize = 20): Promise<ApiResponse<Notification[]>> {
    try {
      const response = await axios.get<ApiResponse<Notification[]>>(
        `/notifications`,
        {
          params: { unreadOnly, page, pageSize },
          headers: this.getAuthHeaders(),
        }
      );
      return response.data;
    } catch (error) {
      console.error('Error fetching notifications:', error);
      throw error;
    }
  }

  async getNotificationSummary(): Promise<ApiResponse<NotificationSummary>> {
    try {
      const response = await axios.get<ApiResponse<NotificationSummary>>(
        `/notifications/summary`,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error fetching notification summary:', error);
      throw error;
    }
  }

  async getUnreadCount(): Promise<ApiResponse<number>> {
    try {
      const response = await axios.get<ApiResponse<number>>(
        `/notifications/unread-count`,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error fetching unread count:', error);
      throw error;
    }
  }

  async markAsRead(notificationIds: number[]): Promise<ApiResponse> {
    try {
      const response = await axios.post<ApiResponse>(
        `/notifications/mark-as-read`,
        { notificationIds },
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error marking notifications as read:', error);
      throw error;
    }
  }

  async markAllAsRead(): Promise<ApiResponse> {
    try {
      const response = await axios.post<ApiResponse>(
        `/notifications/mark-all-as-read`,
        {},
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error marking all notifications as read:', error);
      throw error;
    }
  }

  async deleteNotification(id: number): Promise<ApiResponse> {
    try {
      const response = await axios.delete<ApiResponse>(
        `/notifications/`,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error deleting notification:', error);
      throw error;
    }
  }
}

export const notificationService = new NotificationService();
