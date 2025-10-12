import React, { useState } from "react";
import { X } from "lucide-react";
import { jeWorkflowService } from "../../services/jeWorkflowService";
import NotificationModal from "../common/NotificationModal";
import type { NotificationType } from "../common/NotificationModal";

interface Document {
  id: number;
  documentTypeName: string;
  fileName: string;
  fileSize?: number;
  isVerified: boolean;
}

interface DocumentApprovalModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId: number;
  documents: Document[];
  onApprovalComplete?: () => void;
}

const DocumentApprovalModal: React.FC<DocumentApprovalModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  onApprovalComplete,
}) => {
  const [comments, setComments] = useState("");
  const [otp, setOtp] = useState(["", "", "", "", "", ""]);
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

  const handleOtpChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return; // Only allow digits

    const newOtp = [...otp];
    newOtp[index] = value.slice(-1); // Take only last digit
    setOtp(newOtp);

    // Auto-focus next input
    if (value && index < 5) {
      const nextInput = document.getElementById(`otp-${index + 1}`);
      nextInput?.focus();
    }
  };

  const handleOtpKeyDown = (
    index: number,
    e: React.KeyboardEvent<HTMLInputElement>
  ) => {
    if (e.key === "Backspace" && !otp[index] && index > 0) {
      const prevInput = document.getElementById(`otp-${index - 1}`);
      prevInput?.focus();
    }
  };

  const handleGenerateOtp = async () => {
    try {
      setIsGeneratingOtp(true);
      const response = await jeWorkflowService.generateOtpForSignature(
        applicationId
      );

      if (response.success) {
        setOtpGenerated(true);
        setNotification({
          isOpen: true,
          message: "OTP has been sent to your registered email address",
          type: "success",
          title: "OTP Sent Successfully",
          autoClose: true,
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

  const handleAddDigitalSignature = async () => {
    const otpValue = otp.join("");

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

    try {
      setIsSubmitting(true);

      // Call verify document API with OTP
      const response = await jeWorkflowService.verifyDocument({
        applicationId,
        comments,
        otp: otpValue,
      });

      if (response.success) {
        setNotification({
          isOpen: true,
          message:
            "Documents verified and recommendation form digitally signed successfully!",
          type: "success",
          title: "Digital Signature Applied",
          autoClose: true,
        });

        // Close modal and refresh after notification
        setTimeout(() => {
          onApprovalComplete?.();
          onClose();
        }, 2000);
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
              background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
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
              Document Verification
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
                  e.target.style.borderColor = "#10b981";
                }}
                onBlur={(e) => {
                  e.target.style.borderColor = "#d1d5db";
                }}
              />
            </div>

            {/* OTP Section */}
            {otpGenerated && (
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
                <div
                  style={{
                    display: "flex",
                    gap: "10px",
                    justifyContent: "center",
                    marginBottom: "8px",
                  }}
                >
                  {otp.map((digit, index) => (
                    <input
                      key={index}
                      id={`otp-${index}`}
                      type="text"
                      inputMode="numeric"
                      maxLength={1}
                      value={digit}
                      onChange={(e) => handleOtpChange(index, e.target.value)}
                      onKeyDown={(e) => handleOtpKeyDown(index, e)}
                      style={{
                        width: "50px",
                        height: "55px",
                        textAlign: "center",
                        fontSize: "22px",
                        fontWeight: "bold",
                        border: "2px solid #d1d5db",
                        borderRadius: "8px",
                        outline: "none",
                        transition: "all 0.2s",
                        backgroundColor: digit ? "#f0fdf4" : "white",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                        e.target.select();
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  ))}
                </div>
                <p
                  style={{
                    fontSize: "13px",
                    color: "#64748b",
                    textAlign: "center",
                    margin: 0,
                  }}
                >
                  OTP sent to your registered email address (valid for 5
                  minutes)
                </p>
              </div>
            )}

            {/* Info Message */}
            {!otpGenerated && (
              <div
                style={{
                  padding: "12px 14px",
                  background: "#fef3c7",
                  border: "1px solid #fbbf24",
                  borderRadius: "6px",
                  marginBottom: "16px",
                }}
              >
                <p style={{ fontSize: "13px", color: "#92400e", margin: 0 }}>
                  <strong>Note:</strong> Click "GET OTP" to receive a
                  verification code on your email. You'll need to enter this OTP
                  to digitally sign the recommendation form.
                </p>
              </div>
            )}
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

            {!otpGenerated ? (
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
                    "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                  border: "none",
                }}
              >
                {isGeneratingOtp ? "Generating..." : "GET OTP"}
              </button>
            ) : (
              <button
                className="pmc-button pmc-button-success"
                onClick={handleAddDigitalSignature}
                disabled={isSubmitting || otp.join("").length !== 6}
                style={{
                  minWidth: "200px",
                  padding: "8px 16px",
                  fontSize: "14px",
                  fontWeight: 600,
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  border: "none",
                }}
              >
                {isSubmitting ? "Processing..." : "ADD DIGITAL SIGNATURE"}
              </button>
            )}
          </div>
        </div>
      </div>
    </>
  );
};

export default DocumentApprovalModal;
