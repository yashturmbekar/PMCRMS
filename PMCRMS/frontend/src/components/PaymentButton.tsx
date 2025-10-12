import React, { useState } from "react";
import { CreditCard, Loader2, AlertCircle } from "lucide-react";
import billdeskPaymentService from "../services/billdeskPaymentService";

interface PaymentButtonProps {
  applicationId: number;
  applicationStatus: number;
  isPaymentComplete: boolean;
  onPaymentInitiated?: () => void;
  className?: string;
}

const PaymentButton: React.FC<PaymentButtonProps> = ({
  applicationId,
  applicationStatus,
  isPaymentComplete,
  onPaymentInitiated,
  className = "",
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // ApplicationCurrentStatus.ApprovedByCE1 = 13
  const APPROVED_BY_CE1_STATUS = 13;

  // Check if payment can be initiated
  const canInitiatePayment =
    applicationStatus === APPROVED_BY_CE1_STATUS && !isPaymentComplete;

  const handlePayment = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await billdeskPaymentService.initiatePayment(
        applicationId
      );

      if (response.success && response.gatewayUrl) {
        // Redirect user to BillDesk payment gateway
        window.location.href = response.gatewayUrl;
        onPaymentInitiated?.();
      } else {
        setError(response.message || "Failed to initiate payment");
      }
    } catch (err) {
      const error = err as Error;
      console.error("Payment error:", error);
      setError(error.message || "An error occurred while initiating payment");
    } finally {
      setLoading(false);
    }
  };

  // Don't show button if payment not applicable
  if (!canInitiatePayment) {
    if (isPaymentComplete) {
      return (
        <div className="inline-flex items-center px-4 py-2 bg-green-50 border border-green-200 rounded-lg">
          <span className="text-green-700 font-medium">
            ✓ Payment Completed
          </span>
        </div>
      );
    }
    return null;
  }

  return (
    <div className={`space-y-3 ${className}`}>
      <button
        onClick={handlePayment}
        disabled={loading}
        className="inline-flex items-center px-6 py-3 bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white font-semibold rounded-lg shadow-lg hover:shadow-xl transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {loading ? (
          <>
            <Loader2 className="w-5 h-5 mr-2 animate-spin" />
            Processing...
          </>
        ) : (
          <>
            <CreditCard className="w-5 h-5 mr-2" />
            Pay ₹3,000 - Proceed to Payment
          </>
        )}
      </button>

      {error && (
        <div className="flex items-start p-4 bg-red-50 border border-red-200 rounded-lg">
          <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 mr-3 flex-shrink-0" />
          <div>
            <p className="text-sm font-medium text-red-800">Payment Error</p>
            <p className="text-sm text-red-600 mt-1">{error}</p>
          </div>
        </div>
      )}

      <div className="text-sm text-gray-600">
        <p className="font-medium mb-2">Payment Details:</p>
        <ul className="list-disc list-inside space-y-1 text-gray-600">
          <li>License Certificate Fee: ₹3,000</li>
          <li>Secure payment via BillDesk gateway</li>
          <li>You will be redirected to complete payment</li>
          <li>Payment confirmation via email</li>
        </ul>
      </div>
    </div>
  );
};

export default PaymentButton;
