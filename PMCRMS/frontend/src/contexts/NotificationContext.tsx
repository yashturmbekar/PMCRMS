import React, { createContext, useState, useEffect } from "react";
import type { ReactNode } from "react";
import { notificationService } from "../services/notificationService";
import { useAuth } from "../hooks/useAuth";
import type { Notification, NotificationSummary } from "../types";
import { NOTIFICATION_POLL_INTERVAL } from "../constants";

interface NotificationContextType {
  notifications: Notification[];
  summary: NotificationSummary | null;
  unreadCount: number;
  loading: boolean;
  fetchNotifications: (unreadOnly?: boolean) => Promise<void>;
  fetchSummary: () => Promise<void>;
  markAsRead: (id: number) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  deleteNotification: (id: number) => Promise<void>;
}

export const NotificationContext = createContext<
  NotificationContextType | undefined
>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);

  const fetchNotifications = async (unreadOnly = false) => {
    if (!user) return;
    setLoading(true);
    try {
      const response = await notificationService.getNotifications(unreadOnly);
      if (response.success && response.data) {
        setNotifications(response.data);
      }
    } catch (error) {
      console.error("Error fetching notifications:", error);
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (id: number) => {
    try {
      const response = await notificationService.markAsRead([id]);
      if (response.success) {
        await fetchNotifications();
      }
    } catch (error) {
      console.error("Error marking as read:", error);
    }
  };

  const markAllAsRead = async () => {
    try {
      const response = await notificationService.markAllAsRead();
      if (response.success) {
        await fetchNotifications();
      }
    } catch (error) {
      console.error("Error marking all as read:", error);
    }
  };

  const deleteNotification = async (id: number) => {
    try {
      const response = await notificationService.deleteNotification(id);
      if (response.success) {
        await fetchNotifications();
      }
    } catch (error) {
      console.error("Error deleting notification:", error);
    }
  };

  useEffect(() => {
    // Notification polling disabled
    // let interval: number | null = null;
    // if (user) {
    //   fetchUnreadCount();
    //   interval = setInterval(
    //     fetchUnreadCount,
    //     NOTIFICATION_POLL_INTERVAL
    //   ) as unknown as number;
    // }
    // return () => {
    //   if (interval) clearInterval(interval);
    // };
  }, [user]);

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        unreadCount,
        loading,
        fetchNotifications,
        markAsRead,
        markAllAsRead,
        deleteNotification,
      }}
    >
      {children}
    </NotificationContext.Provider>
  );
};
