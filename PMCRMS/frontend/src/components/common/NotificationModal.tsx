import React from "react";
import { CheckCircle, XCircle, AlertCircle, Info } from "lucide-react";

export type NotificationType = "success" | "error" | "warning" | "info";

interface NotificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  type: NotificationType;
  title: string;
  message: string;
  autoClose?: boolean;
  autoCloseDuration?: number;
  autoCloseMessage?: string; // Custom message for auto-close indicator
}

const NotificationModal: React.FC<NotificationModalProps> = ({
  isOpen,
  onClose,
  type,
  title,
  message,
  autoClose = false,
  autoCloseDuration = 2000,
  autoCloseMessage,
}) => {
  React.useEffect(() => {
    if (isOpen && autoClose) {
      const timer = setTimeout(() => {
        onClose();
      }, autoCloseDuration);
      return () => clearTimeout(timer);
    }
  }, [isOpen, autoClose, autoCloseDuration, onClose]);

  if (!isOpen) return null;

  const getConfig = () => {
    switch (type) {
      case "success":
        return {
          icon: CheckCircle,
          iconColor: "#10b981",
          iconBg: "#dcfce7",
          titleColor: "#059669",
        };
      case "error":
        return {
          icon: XCircle,
          iconColor: "#ef4444",
          iconBg: "#fee2e2",
          titleColor: "#dc2626",
        };
      case "warning":
        return {
          icon: AlertCircle,
          iconColor: "#f59e0b",
          iconBg: "#fef3c7",
          titleColor: "#d97706",
        };
      case "info":
        return {
          icon: Info,
          iconColor: "#3b82f6",
          iconBg: "#dbeafe",
          titleColor: "#2563eb",
        };
    }
  };

  const config = getConfig();
  const Icon = config.icon;

  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        background: "rgba(0, 0, 0, 0.5)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 9999,
        padding: "20px",
      }}
      onClick={onClose}
      className="pmc-fadeIn"
    >
      <div
        style={{
          background: "white",
          borderRadius: "16px",
          maxWidth: "440px",
          width: "100%",
          padding: "40px 30px",
          textAlign: "center",
          boxShadow:
            "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)",
        }}
        onClick={(e) => e.stopPropagation()}
        className="pmc-slideInUp"
      >
        {/* Icon */}
        <div
          style={{
            width: "80px",
            height: "80px",
            borderRadius: "50%",
            background: config.iconBg,
            margin: "0 auto 24px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Icon
            style={{
              width: "48px",
              height: "48px",
              color: config.iconColor,
            }}
          />
        </div>

        {/* Title */}
        <h2
          style={{
            fontSize: "24px",
            fontWeight: "700",
            color: config.titleColor,
            margin: "0 0 12px 0",
            lineHeight: "1.3",
          }}
        >
          {title}
        </h2>

        {/* Message */}
        <p
          style={{
            fontSize: "15px",
            color: "#64748b",
            margin: "0 0 24px 0",
            lineHeight: "1.6",
          }}
        >
          {message}
        </p>

        {/* Auto-close indicator */}
        {autoClose && autoCloseMessage && (
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: "8px",
              marginTop: "20px",
            }}
          >
            <div
              className="pmc-spinner"
              style={{
                width: "20px",
                height: "20px",
                border: "2px solid #e2e8f0",
                borderTopColor: config.iconColor,
                borderRadius: "50%",
                animation: "spin 1s linear infinite",
              }}
            />
            <p
              style={{
                fontSize: "13px",
                color: "#94a3b8",
                margin: 0,
              }}
            >
              {autoCloseMessage}
            </p>
          </div>
        )}

        {/* Manual close button (if not auto-closing) */}
        {!autoClose && (
          <button
            onClick={onClose}
            className="pmc-button pmc-button-primary"
            style={{
              marginTop: "8px",
              padding: "10px 24px",
              fontSize: "14px",
            }}
          >
            OK
          </button>
        )}
      </div>

      <style>
        {`
          @keyframes spin {
            to {
              transform: rotate(360deg);
            }
          }
        `}
      </style>
    </div>
  );
};

export default NotificationModal;
