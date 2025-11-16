import React, { useState } from "react";
import { X } from "lucide-react";
import NotificationModal from "../common/NotificationModal";
import type { NotificationType } from "../common/NotificationModal";
import OtpInput from "../OtpInput";
import FullScreenLoader from "../FullScreenLoader";

interface OTPVerificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId: number;
  title?: string;
  officerType: "AE" | "EE" | "CE";
  positionType?: string; // Required for AE
  onGenerateOtp: (
    applicationId: number
  ) => Promise<{ success: boolean; message?: string }>;
  onVerifyAndSign: (
    applicationId: number,
    otp: string,
    comments?: string
  ) => Promise<{ success: boolean; message?: string }>;
  onSuccess?: () => void;
}

const OTPVerificationModal: React.FC<OTPVerificationModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  title,
  officerType,
  onGenerateOtp,
  onVerifyAndSign,
  onSuccess,
}) => {
  const TESTING_MODE = false; // Production mode - OTP verification enabled

  const [comments, setComments] = useState("");
  const [otpValue, setOtpValue] = useState("");
  const [otpGenerated, setOtpGenerated] = useState(false);
  const [isGeneratingOtp, setIsGeneratingOtp] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notification, setNotification] = useState<{
    isOpen: boolean;
    message: string;
    type: NotificationType;
    title: string;
    autoClose?: boolean;
  }>({
    isOpen: false,
    message: "",
    type: "success",
    title: "",
  });

  if (!isOpen) return null;

  const getOfficerTitle = () => {
    switch (officerType) {
      case "AE":
        return "Assistant Engineer";
      case "EE":
        return "Executive Engineer";
      case "CE":
        return "City Engineer";
      default:
        return "Officer";
    }
  };

  const getDocumentName = () => {
    // EE and CE (Stage 2) sign License Certificate
    // AE signs Recommendation Form
    return officerType === "AE" ? "Recommendation Form" : "License Certificate";
  };

  const handleOtpChange = (value: string) => {
    setOtpValue(value);
  };

  const handleGenerateOtp = async () => {
    try {
      setIsGeneratingOtp(true);
      const response = await onGenerateOtp(applicationId);

      if (response.success) {
        setOtpGenerated(true);
        setNotification({
          isOpen: true,
          message:
            response.message ||
            "OTP has been sent to your registered mobile number",
          type: "success",
          title: "OTP Sent Successfully",
          autoClose: true,
        });
      } else {
        setNotification({
          isOpen: true,
          message: response.message || "Failed to generate OTP",
          type: "error",
          title: "OTP Generation Failed",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error generating OTP:", error);
      setNotification({
        isOpen: true,
        message: "Failed to generate OTP. Please try again.",
        type: "error",
        title: "OTP Generation Failed",
        autoClose: false,
      });
    } finally {
      setIsGeneratingOtp(false);
    }
  };

  const handleVerifyAndSign = async () => {
    // ========== TESTING MODE: Skip OTP validation ==========
    if (TESTING_MODE) {
      console.log("[TESTING MODE] Bypassing OTP validation");
    } else {
      // ========== END TESTING MODE ==========
      if (otpValue.length !== 6) {
        setNotification({
          isOpen: true,
          message: "Please enter the complete 6-digit OTP",
          type: "warning",
          title: "Incomplete OTP",
          autoClose: false,
        });
        return;
      }
    }

    try {
      setIsSubmitting(true);

      // ========== TESTING MODE: Use dummy OTP ==========
      const otpToSend = TESTING_MODE ? "000000" : otpValue;
      // ========== END TESTING MODE ==========

      const response = await onVerifyAndSign(
        applicationId,
        otpToSend,
        comments
      );

      if (response.success) {
        setNotification({
          isOpen: true,
          message:
            officerType === "CE"
              ? "Application FINALLY APPROVED and digitally signed successfully!"
              : `${getDocumentName()} digitally signed successfully! Forwarded to ${
                  officerType === "AE" ? "Executive Engineer" : "City Engineer"
                }.`,
          type: "success",
          title: "Digital Signature Applied",
          autoClose: true,
        });

        // Close modal and refresh after notification
        setTimeout(() => {
          onSuccess?.();
          onClose();
          // Reset state
          setComments("");
          setOtpValue("");
          setOtpGenerated(false);
        }, 2000);
      } else {
        setNotification({
          isOpen: true,
          message: response.message || "Failed to apply digital signature",
          type: "error",
          title: "Signature Failed",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error applying digital signature:", error);
      setNotification({
        isOpen: true,
        message:
          "Failed to apply digital signature. Please check your OTP and try again.",
        type: "error",
        title: "Signature Failed",
        autoClose: false,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <NotificationModal
        isOpen={notification.isOpen}
        onClose={() => setNotification({ ...notification, isOpen: false })}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        autoClose={notification.autoClose}
        autoCloseDuration={2000}
      />
      <div
        className="pmc-modal-overlay"
        onClick={onClose}
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
          zIndex: 1000,
          padding: "20px",
        }}
      >
        <div
          className="pmc-modal pmc-slideInUp"
          onClick={(e) => e.stopPropagation()}
          style={{
            background: "white",
            borderRadius: "8px",
            maxWidth: "520px",
            width: "100%",
            maxHeight: "90vh",
            overflow: "hidden",
            display: "flex",
            flexDirection: "column",
            boxShadow:
              "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)",
          }}
        >
          {/* Header */}
          <div
            className="pmc-modal-header"
            style={{
              padding: "14px 18px",
              borderBottom: "1px solid #e5e7eb",
              background:
                officerType === "CE"
                  ? "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)"
                  : officerType === "EE"
                  ? "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)"
                  : "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              flexShrink: 0,
            }}
          >
            <h3
              style={{
                color: "white",
                margin: 0,
                fontSize: "17px",
                fontWeight: "600",
              }}
            >
              {title ||
                `${getOfficerTitle()} - Adding Signature to ${getDocumentName()}`}
            </h3>
            <button
              onClick={onClose}
              style={{
                background: "transparent",
                border: "none",
                cursor: "pointer",
                padding: "4px",
              }}
            >
              <X style={{ width: "20px", height: "20px", color: "white" }} />
            </button>
          </div>

          {/* Body - Scrollable */}
          <div
            className="pmc-modal-body"
            style={{
              padding: "20px",
              overflowY: "auto",
              flexGrow: 1,
            }}
          >
            {/* Comments */}
            <div style={{ marginBottom: "20px" }}>
              <label
                className="pmc-label"
                style={{
                  display: "block",
                  marginBottom: "8px",
                  fontWeight: 600,
                  color: "#334155",
                  fontSize: "14px",
                }}
              >
                Comments
              </label>
              <textarea
                placeholder="Enter your comments (optional)"
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                style={{
                  width: "100%",
                  minHeight: "80px",
                  padding: "10px 12px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                  fontFamily: "inherit",
                  resize: "vertical",
                  outline: "none",
                  transition: "border-color 0.2s",
                }}
                onFocus={(e) => {
                  e.target.style.borderColor =
                    officerType === "CE"
                      ? "#dc2626"
                      : officerType === "EE"
                      ? "#7c3aed"
                      : "#3b82f6";
                }}
                onBlur={(e) => {
                  e.target.style.borderColor = "#d1d5db";
                }}
              />
            </div>

            {/* OTP Section */}
            {/* ========== TESTING MODE: Hide OTP section ========== */}
            {!TESTING_MODE && otpGenerated && (
              <div style={{ marginBottom: "20px" }}>
                <label
                  className="pmc-label"
                  style={{
                    display: "block",
                    marginBottom: "12px",
                    fontWeight: 600,
                    color: "#334155",
                    fontSize: "14px",
                  }}
                >
                  Enter OTP
                </label>
                <OtpInput
                  length={6}
                  value={otpValue}
                  onChange={handleOtpChange}
                  disabled={isSubmitting}
                />
                <p
                  style={{
                    fontSize: "13px",
                    color: "#64748b",
                    textAlign: "center",
                    margin: "8px 0 0 0",
                  }}
                >
                  OTP sent to your registered mobile number (valid for 5
                  minutes)
                </p>
              </div>
            )}
            {/* ========== END TESTING MODE ========== */}

            {/* Info Message */}
            {/* ========== TESTING MODE: Show testing mode message ========== */}
            {TESTING_MODE ? (
              <div
                style={{
                  padding: "12px 14px",
                  background: "#dcfce7",
                  border: "1px solid #86efac",
                  borderRadius: "6px",
                  marginBottom: "16px",
                }}
              >
                <p
                  style={{
                    fontSize: "13px",
                    color: "#92400e",
                    margin: 0,
                  }}
                >
                  <strong>🧪 TESTING MODE:</strong> HSM OTP verification is
                  bypassed. Click "VERIFY & SIGN (TESTING)" to directly approve
                  without OTP.
                </p>
              </div>
            ) : (
              !otpGenerated && (
                <div
                  style={{
                    padding: "12px 14px",
                    background: officerType === "CE" ? "#fef2f2" : "#fef3c7",
                    border: `1px solid ${
                      officerType === "CE" ? "#fca5a5" : "#fbbf24"
                    }`,
                    borderRadius: "6px",
                    marginBottom: "16px",
                  }}
                >
                  <p
                    style={{
                      fontSize: "13px",
                      color: officerType === "CE" ? "#7f1d1d" : "#92400e",
                      margin: 0,
                    }}
                  >
                    <strong>Note:</strong> Click "GET OTP" to receive a
                    verification code on your registered mobile number. You'll
                    need to enter this OTP to digitally sign the{" "}
                    {getDocumentName().toLowerCase()}.
                  </p>
                </div>
              )
            )}
            {/* ========== END TESTING MODE ========== */}
          </div>

          {/* Footer */}
          <div
            className="pmc-modal-footer"
            style={{
              padding: "14px 20px",
              borderTop: "1px solid #e5e7eb",
              display: "flex",
              gap: "10px",
              justifyContent: "flex-end",
              background: "#f9fafb",
              flexShrink: 0,
            }}
          >
            <button
              className="pmc-button pmc-button-outline"
              onClick={onClose}
              disabled={isSubmitting || isGeneratingOtp}
              style={{
                minWidth: "90px",
                padding: "8px 16px",
                fontSize: "14px",
                fontWeight: 500,
              }}
            >
              Cancel
            </button>

            {/* ========== TESTING MODE: Direct verify button ========== */}
            {TESTING_MODE ? (
              <button
                className="pmc-button pmc-button-success"
                onClick={handleVerifyAndSign}
                disabled={isSubmitting}
                style={{
                  minWidth: "240px",
                  padding: "8px 16px",
                  fontSize: "14px",
                  fontWeight: 600,
                  background:
                    officerType === "CE"
                      ? "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)"
                      : "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  border: "none",
                }}
              >
                {isSubmitting ? "Processing..." : "VERIFY & SIGN (TESTING)"}
              </button>
            ) : !otpGenerated ? (
              <button
                className="pmc-button pmc-button-primary"
                onClick={handleGenerateOtp}
                disabled={isGeneratingOtp}
                style={{
                  minWidth: "130px",
                  padding: "8px 16px",
                  fontSize: "14px",
                  fontWeight: 600,
                  background:
                    officerType === "CE"
                      ? "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)"
                      : officerType === "EE"
                      ? "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)"
                      : "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                  border: "none",
                }}
              >
                {isGeneratingOtp ? "Generating..." : "GET OTP"}
              </button>
            ) : (
              <button
                className="pmc-button pmc-button-success"
                onClick={handleVerifyAndSign}
                disabled={isSubmitting || otpValue.length !== 6}
                style={{
                  minWidth: "200px",
                  padding: "8px 16px",
                  fontSize: "14px",
                  fontWeight: 600,
                  background:
                    officerType === "CE"
                      ? "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)"
                      : "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  border: "none",
                }}
              >
                {isSubmitting ? "Processing..." : "VERIFY & SIGN"}
              </button>
            )}
            {/* ========== END TESTING MODE ========== */}
          </div>
        </div>
      </div>

      {/* Full Screen Loader */}
      {isSubmitting && (
        <FullScreenLoader
          message="Processing Signature"
          submessage="Please wait while we verify and sign the application..."
        />
      )}
    </>
  );
};

export default OTPVerificationModal;
