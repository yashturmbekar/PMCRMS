import React, { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Bell, X } from "lucide-react";
import { useNotifications } from "../hooks/useNotifications";
import type { Notification } from "../types";

const NotificationBell: React.FC = () => {
  const navigate = useNavigate();
  const { summary, unreadCount, markAsRead, fetchSummary } = useNotifications();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isOpen]);

  const handleBellClick = async () => {
    setIsOpen(!isOpen);
    if (!isOpen) {
      await fetchSummary();
    }
  };

  const handleNotificationClick = async (id: number) => {
    await markAsRead(id);
    setIsOpen(false);
    navigate("/notifications");
  };

  const handleViewAll = () => {
    setIsOpen(false);
    navigate("/notifications");
  };

  return (
    <div style={{ position: "relative" }} ref={dropdownRef}>
      <button
        onClick={handleBellClick}
        style={{
          position: "relative",
          padding: "10px",
          background: "rgba(255, 255, 255, 0.1)",
          border: "1px solid rgba(255, 255, 255, 0.2)",
          cursor: "pointer",
          borderRadius: "8px",
          transition: "all 0.2s",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
        onMouseEnter={(e) => {
          e.currentTarget.style.background = "rgba(255, 255, 255, 0.2)";
          e.currentTarget.style.borderColor = "rgba(255, 255, 255, 0.4)";
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.background = "rgba(255, 255, 255, 0.1)";
          e.currentTarget.style.borderColor = "rgba(255, 255, 255, 0.2)";
        }}
      >
        <Bell size={22} color="#ffffff" strokeWidth={2} />
        {unreadCount > 0 && (
          <span
            style={{
              position: "absolute",
              top: "2px",
              right: "2px",
              backgroundColor: "#ef4444",
              color: "white",
              borderRadius: "9999px",
              fontSize: "11px",
              fontWeight: "700",
              minWidth: "18px",
              height: "18px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              padding: "0 5px",
              border: "2px solid #0369a1",
              boxShadow: "0 2px 4px rgba(0,0,0,0.3)",
            }}
          >
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>
      {isOpen && (
        <div
          style={{
            position: "absolute",
            right: "0",
            top: "calc(100% + 8px)",
            width: "380px",
            maxHeight: "500px",
            backgroundColor: "white",
            borderRadius: "8px",
            boxShadow: "0 10px 15px -3px rgba(0,0,0,0.1)",
            border: "1px solid #e5e7eb",
            zIndex: 50,
            overflow: "hidden",
          }}
        >
          <div
            style={{
              padding: "16px",
              borderBottom: "1px solid #e5e7eb",
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              backgroundColor: "#f9fafb",
            }}
          >
            <h3
              style={{
                margin: 0,
                fontSize: "16px",
                fontWeight: "600",
                color: "#111827",
              }}
            >
              Notifications
            </h3>
            <button
              onClick={() => setIsOpen(false)}
              style={{
                padding: "4px",
                background: "transparent",
                border: "none",
                cursor: "pointer",
                borderRadius: "4px",
              }}
            >
              <X size={16} color="#6b7280" />
            </button>
          </div>
          <div style={{ maxHeight: "400px", overflowY: "auto" }}>
            {summary?.recentNotifications &&
            summary.recentNotifications.length > 0 ? (
              summary.recentNotifications.map((notification: Notification) => (
                <div
                  key={notification.id}
                  onClick={() => handleNotificationClick(notification.id)}
                  style={{
                    padding: "12px 16px",
                    borderBottom: "1px solid #f3f4f6",
                    cursor: "pointer",
                    backgroundColor: notification.isRead ? "white" : "#eff6ff",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      marginBottom: "4px",
                    }}
                  >
                    <span
                      style={{
                        fontSize: "13px",
                        fontWeight: notification.isRead ? "500" : "600",
                        color: "#111827",
                      }}
                    >
                      {notification.title}
                    </span>
                    {!notification.isRead && (
                      <span
                        style={{
                          width: "8px",
                          height: "8px",
                          borderRadius: "50%",
                          backgroundColor: "#3b82f6",
                          flexShrink: 0,
                          marginLeft: "8px",
                          marginTop: "4px",
                        }}
                      />
                    )}
                  </div>
                  <p
                    style={{
                      margin: "0 0 4px 0",
                      fontSize: "12px",
                      color: "#6b7280",
                      lineHeight: "1.4",
                    }}
                  >
                    {notification.message}
                  </p>
                  <span style={{ fontSize: "11px", color: "#9ca3af" }}>
                    {new Date(notification.createdDate).toLocaleDateString(
                      "en-IN",
                      {
                        day: "numeric",
                        month: "short",
                        hour: "2-digit",
                        minute: "2-digit",
                      }
                    )}
                  </span>
                </div>
              ))
            ) : (
              <div
                style={{
                  padding: "32px 16px",
                  textAlign: "center",
                  color: "#9ca3af",
                }}
              >
                <Bell
                  size={40}
                  color="#d1d5db"
                  style={{ margin: "0 auto 12px" }}
                />
                <p style={{ margin: 0, fontSize: "14px" }}>
                  No notifications yet
                </p>
              </div>
            )}
          </div>
          {summary?.recentNotifications &&
            summary.recentNotifications.length > 0 && (
              <div
                style={{
                  padding: "12px 16px",
                  borderTop: "1px solid #e5e7eb",
                  backgroundColor: "#f9fafb",
                }}
              >
                <button
                  onClick={handleViewAll}
                  style={{
                    width: "100%",
                    padding: "8px",
                    backgroundColor: "#3b82f6",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    fontSize: "13px",
                    fontWeight: "600",
                    cursor: "pointer",
                  }}
                >
                  View All Notifications
                </button>
              </div>
            )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
