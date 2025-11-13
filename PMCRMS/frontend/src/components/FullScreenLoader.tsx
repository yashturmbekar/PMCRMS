import React from "react";

interface FullScreenLoaderProps {
  message?: string;
  submessage?: string;
}

/**
 * Professional full-screen loader overlay for all async operations
 * Provides consistent UX across the entire application
 */
const FullScreenLoader: React.FC<FullScreenLoaderProps> = ({
  message = "Processing...",
  submessage,
}) => {
  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: "rgba(0, 0, 0, 0.75)",
        backdropFilter: "blur(4px)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 99999,
        animation: "fadeIn 0.2s ease-in",
      }}
    >
      <div
        style={{
          background: "white",
          borderRadius: "16px",
          padding: "40px 48px",
          boxShadow: "0 20px 60px rgba(0, 0, 0, 0.3)",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: "24px",
          maxWidth: "400px",
          animation: "scaleIn 0.3s ease-out",
        }}
      >
        {/* Spinner */}
        <div
          style={{
            width: "56px",
            height: "56px",
            border: "4px solid #e5e7eb",
            borderTopColor: "#2563eb",
            borderRadius: "50%",
            animation: "spin 0.8s linear infinite",
          }}
        />

        {/* Message */}
        <div style={{ textAlign: "center" }}>
          <h3
            style={{
              margin: 0,
              fontSize: "18px",
              fontWeight: "600",
              color: "#1f2937",
              marginBottom: submessage ? "8px" : "0",
            }}
          >
            {message}
          </h3>
          {submessage && (
            <p
              style={{
                margin: 0,
                fontSize: "14px",
                color: "#6b7280",
                lineHeight: "1.5",
              }}
            >
              {submessage}
            </p>
          )}
        </div>

        {/* Progress indicator */}
        <div
          style={{
            width: "100%",
            height: "3px",
            background: "#e5e7eb",
            borderRadius: "2px",
            overflow: "hidden",
          }}
        >
          <div
            style={{
              height: "100%",
              background: "linear-gradient(90deg, #2563eb 0%, #3b82f6 100%)",
              animation: "progressBar 1.5s ease-in-out infinite",
            }}
          />
        </div>
      </div>

      <style>
        {`
          @keyframes fadeIn {
            from {
              opacity: 0;
            }
            to {
              opacity: 1;
            }
          }

          @keyframes scaleIn {
            from {
              transform: scale(0.9);
              opacity: 0;
            }
            to {
              transform: scale(1);
              opacity: 1;
            }
          }

          @keyframes spin {
            from {
              transform: rotate(0deg);
            }
            to {
              transform: rotate(360deg);
            }
          }

          @keyframes progressBar {
            0% {
              transform: translateX(-100%);
            }
            100% {
              transform: translateX(400%);
            }
          }
        `}
      </style>
    </div>
  );
};

export default FullScreenLoader;
