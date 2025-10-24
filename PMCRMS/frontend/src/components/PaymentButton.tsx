import React, { useState } from "react";
import {
  CreditCard,
  Loader2,
  AlertCircle,
  CheckCircle,
  Shield,
  Clock,
  Mail,
  X,
} from "lucide-react";
import billdeskPaymentService from "../services/billdeskPaymentService";

interface PaymentButtonProps {
  applicationId: number;
  applicationStatus: number | string; // Can be number or string from API
  isPaymentComplete: boolean;
  onPaymentInitiated?: () => void;
  onPaymentSuccess?: () => void;
  className?: string;
}

const PaymentButton: React.FC<PaymentButtonProps> = ({
  applicationId,
  applicationStatus,
  isPaymentComplete,
  onPaymentInitiated,
  onPaymentSuccess,
  className = "",
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // ApplicationCurrentStatus.ApprovedByCE1 = 13, PaymentPending = 15
  const APPROVED_BY_CE1_STATUS = 13;
  const PAYMENT_PENDING_STATUS = 15;

  // Debug logging
  console.log("PaymentButton Debug:", {
    applicationId,
    applicationStatus,
    applicationStatusType: typeof applicationStatus,
    isPaymentComplete,
    APPROVED_BY_CE1_STATUS,
    PAYMENT_PENDING_STATUS,
  });

  // Check if payment can be initiated
  // Status can be either number (13, 15) or string ("ApprovedByCE1", "PaymentPending")
  const canInitiatePayment =
    (applicationStatus === APPROVED_BY_CE1_STATUS ||
      applicationStatus === "ApprovedByCE1" ||
      applicationStatus === PAYMENT_PENDING_STATUS ||
      applicationStatus === "PaymentPending") &&
    !isPaymentComplete;

  const handlePayment = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await billdeskPaymentService.initiatePayment(
        applicationId
      );

      if (response.success && response.data) {
        const { bdOrderId, rData, paymentGatewayUrl } = response.data;

        // Check if this is a MOCK payment (TEST MODE)
        if (response.message && response.message.includes("TEST MODE")) {
          // Mock payment completed immediately
          console.log("✅ Mock payment completed successfully");

          // Call success callback if provided
          if (onPaymentSuccess) {
            onPaymentSuccess();
          } else {
            // Fallback: reload the page after a short delay to show updated status
            setTimeout(() => {
              window.location.reload();
            }, 1500);
          }
          return;
        }

        // BillDesk Embedded SDK Integration (for real payments)
        if (bdOrderId && rData && paymentGatewayUrl) {
          console.log("🔄 Redirecting to BillDesk payment gateway...");
          console.log("BdOrderId:", bdOrderId);
          console.log("RData:", rData);
          console.log("Payment Gateway URL:", paymentGatewayUrl);

          // Notify parent component
          onPaymentInitiated?.();

          // BillDesk requires POST form submission (not URL redirect)
          // Create a hidden form and submit it
          const form = document.createElement("form");
          form.method = "POST";
          form.action = paymentGatewayUrl;
          form.style.display = "none";

          // Add merchantid field
          const merchantIdInput = document.createElement("input");
          merchantIdInput.type = "hidden";
          merchantIdInput.name = "merchantid";
          merchantIdInput.value = "UATPMCNTYA"; // Your merchant ID
          form.appendChild(merchantIdInput);

          // Add bdorderid field
          const bdOrderIdInput = document.createElement("input");
          bdOrderIdInput.type = "hidden";
          bdOrderIdInput.name = "bdorderid";
          bdOrderIdInput.value = bdOrderId;
          form.appendChild(bdOrderIdInput);

          // Add rdata field
          const rDataInput = document.createElement("input");
          rDataInput.type = "hidden";
          rDataInput.name = "rdata";
          rDataInput.value = rData;
          form.appendChild(rDataInput);

          // Append form to body, submit it, then remove it
          document.body.appendChild(form);
          console.log("Submitting BillDesk payment form...");
          form.submit();

          return;
        } else {
          setError("Invalid payment gateway response. Please try again.");
          setLoading(false);
        }
      } else {
        setError(response.message || "Failed to initiate payment");
        setLoading(false);
      }
    } catch (err) {
      const error = err as Error;
      console.error("Payment error:", error);
      setError(error.message || "An error occurred while initiating payment");
      setLoading(false);
    }
  };

  // Don't show button if payment not applicable
  if (!canInitiatePayment) {
    if (isPaymentComplete) {
      return (
        <div
          style={{
            display: "inline-flex",
            alignItems: "center",
            padding: "12px 20px",
            background: "linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%)",
            border: "1px solid #bbf7d0",
            borderRadius: "8px",
            gap: "8px",
          }}
        >
          <CheckCircle size={20} color="#10b981" />
          <span style={{ color: "#065f46", fontWeight: 600 }}>
            ✓ Payment Completed
          </span>
        </div>
      );
    }
    return null;
  }

  return (
    <div className={className} style={{ width: "100%" }}>
      {/* Primary Payment Button */}
      <button
        onClick={handlePayment}
        disabled={loading}
        style={{
          width: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          gap: "12px",
          padding: "16px 24px",
          background: loading
            ? "#9ca3af"
            : "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
          color: "white",
          fontSize: "16px",
          fontWeight: 600,
          borderRadius: "8px",
          border: "none",
          cursor: loading ? "not-allowed" : "pointer",
          boxShadow: loading ? "none" : "0 4px 12px rgba(59, 130, 246, 0.4)",
          transition: "all 0.3s ease",
          marginBottom: "20px",
        }}
        onMouseOver={(e) => {
          if (!loading) {
            e.currentTarget.style.transform = "translateY(-2px)";
            e.currentTarget.style.boxShadow =
              "0 6px 16px rgba(59, 130, 246, 0.5)";
          }
        }}
        onMouseOut={(e) => {
          e.currentTarget.style.transform = "translateY(0)";
          e.currentTarget.style.boxShadow =
            "0 4px 12px rgba(59, 130, 246, 0.4)";
        }}
      >
        {loading ? (
          <>
            <Loader2
              size={20}
              style={{ animation: "spin 1s linear infinite" }}
            />
            Processing Payment...
          </>
        ) : (
          <>
            <CreditCard size={20} />
            Pay ₹3,000 - Proceed to Secure Payment
          </>
        )}
      </button>

      {/* Error Message */}
      {error && (
        <div
          style={{
            display: "flex",
            alignItems: "flex-start",
            gap: "12px",
            padding: "16px",
            background: "#fef2f2",
            border: "1px solid #fecaca",
            borderRadius: "8px",
            marginBottom: "20px",
          }}
        >
          <AlertCircle size={24} color="#dc2626" style={{ flexShrink: 0 }} />
          <div style={{ flex: 1 }}>
            <p
              style={{
                margin: 0,
                fontWeight: 600,
                color: "#991b1b",
                marginBottom: "4px",
              }}
            >
              Payment Error
            </p>
            <p style={{ margin: 0, fontSize: "14px", color: "#dc2626" }}>
              {error}
            </p>
          </div>
          <button
            onClick={() => setError(null)}
            style={{
              background: "none",
              border: "none",
              cursor: "pointer",
              padding: "4px",
              color: "#dc2626",
            }}
          >
            <X size={18} />
          </button>
        </div>
      )}

      {/* Payment Information Grid */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(2, 1fr)",
          gap: "12px",
          marginBottom: "16px",
        }}
      >
        <div
          style={{
            background: "white",
            padding: "14px",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            gap: "10px",
          }}
        >
          <div
            style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: "#eff6ff",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Shield size={18} color="#3b82f6" />
          </div>
          <div>
            <p
              style={{
                margin: 0,
                fontSize: "11px",
                color: "#6b7280",
                fontWeight: 500,
              }}
            >
              Secure Gateway
            </p>
            <p
              style={{
                margin: 0,
                fontSize: "13px",
                color: "#1f2937",
                fontWeight: 600,
              }}
            >
              BillDesk
            </p>
          </div>
        </div>

        <div
          style={{
            background: "white",
            padding: "14px",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            gap: "10px",
          }}
        >
          <div
            style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: "#fef3c7",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Clock size={18} color="#f59e0b" />
          </div>
          <div>
            <p
              style={{
                margin: 0,
                fontSize: "11px",
                color: "#6b7280",
                fontWeight: 500,
              }}
            >
              Process Time
            </p>
            <p
              style={{
                margin: 0,
                fontSize: "13px",
                color: "#1f2937",
                fontWeight: 600,
              }}
            >
              Instant
            </p>
          </div>
        </div>

        <div
          style={{
            background: "white",
            padding: "14px",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            gap: "10px",
          }}
        >
          <div
            style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: "#fef2f2",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <CreditCard size={18} color="#dc2626" />
          </div>
          <div>
            <p
              style={{
                margin: 0,
                fontSize: "11px",
                color: "#6b7280",
                fontWeight: 500,
              }}
            >
              License Fee
            </p>
            <p
              style={{
                margin: 0,
                fontSize: "13px",
                color: "#1f2937",
                fontWeight: 600,
              }}
            >
              ₹3,000
            </p>
          </div>
        </div>

        <div
          style={{
            background: "white",
            padding: "14px",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            gap: "10px",
          }}
        >
          <div
            style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: "#f0fdf4",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Mail size={18} color="#10b981" />
          </div>
          <div>
            <p
              style={{
                margin: 0,
                fontSize: "11px",
                color: "#6b7280",
                fontWeight: 500,
              }}
            >
              Confirmation
            </p>
            <p
              style={{
                margin: 0,
                fontSize: "13px",
                color: "#1f2937",
                fontWeight: 600,
              }}
            >
              Via Email
            </p>
          </div>
        </div>
      </div>

      {/* Payment Instructions */}
      <div
        style={{
          background: "#f9fafb",
          padding: "14px",
          borderRadius: "8px",
          border: "1px solid #e5e7eb",
        }}
      >
        <p
          style={{
            margin: 0,
            fontSize: "12px",
            color: "#6b7280",
            fontWeight: 500,
            marginBottom: "8px",
          }}
        >
          What happens next:
        </p>
        <ul
          style={{
            margin: 0,
            paddingLeft: "20px",
            fontSize: "12px",
            color: "#6b7280",
            lineHeight: "1.6",
          }}
        >
          <li>You'll be redirected to BillDesk secure payment gateway</li>
          <li>Complete payment using your preferred method</li>
          <li>Receive instant confirmation via email</li>
          <li>Application automatically forwarded to Clerk for review</li>
        </ul>
      </div>

      {/* Loading Spinner CSS */}
      <style>{`
        @keyframes spin {
          from {
            transform: rotate(0deg);
          }
          to {
            transform: rotate(360deg);
          }
        }
      `}</style>
    </div>
  );
};

export default PaymentButton;
