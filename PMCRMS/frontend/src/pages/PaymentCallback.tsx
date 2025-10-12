import React, { useEffect, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { CheckCircle, XCircle, Loader2, AlertCircle } from "lucide-react";

const PaymentCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<"loading" | "success" | "failed">(
    "loading"
  );
  const [message, setMessage] = useState("Processing payment...");

  useEffect(() => {
    // Extract callback parameters
    const applicationId = searchParams.get("applicationId");
    const txnEntityId = searchParams.get("txnEntityId");
    const bdOrderId = searchParams.get("bdOrderId");
    const paymentStatus = searchParams.get("status");
    const amount = searchParams.get("amount");

    console.log("Payment callback received:", {
      applicationId,
      txnEntityId,
      bdOrderId,
      paymentStatus,
      amount,
    });

    // Simulate callback processing
    setTimeout(() => {
      if (paymentStatus?.toUpperCase() === "SUCCESS") {
        setStatus("success");
        setMessage("Payment completed successfully!");

        // Redirect to application details after 3 seconds
        setTimeout(() => {
          if (applicationId) {
            navigate(`/application/${applicationId}`);
          } else {
            navigate("/dashboard");
          }
        }, 3000);
      } else {
        setStatus("failed");
        setMessage(
          paymentStatus === "CANCELLED"
            ? "Payment was cancelled"
            : "Payment failed. Please try again."
        );

        // Redirect back after 5 seconds
        setTimeout(() => {
          if (applicationId) {
            navigate(`/application/${applicationId}`);
          } else {
            navigate("/dashboard");
          }
        }, 5000);
      }
    }, 2000);
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full">
        <div className="bg-white rounded-2xl shadow-xl p-8 text-center">
          {/* Status Icon */}
          <div className="mb-6">
            {status === "loading" && (
              <div className="inline-flex items-center justify-center w-20 h-20 bg-blue-100 rounded-full">
                <Loader2 className="w-10 h-10 text-blue-600 animate-spin" />
              </div>
            )}

            {status === "success" && (
              <div className="inline-flex items-center justify-center w-20 h-20 bg-green-100 rounded-full animate-bounce">
                <CheckCircle className="w-10 h-10 text-green-600" />
              </div>
            )}

            {status === "failed" && (
              <div className="inline-flex items-center justify-center w-20 h-20 bg-red-100 rounded-full">
                <XCircle className="w-10 h-10 text-red-600" />
              </div>
            )}
          </div>

          {/* Title */}
          <h1
            className={`text-2xl font-bold mb-3 ${
              status === "success"
                ? "text-green-600"
                : status === "failed"
                ? "text-red-600"
                : "text-blue-600"
            }`}
          >
            {status === "loading" && "Processing Payment"}
            {status === "success" && "Payment Successful!"}
            {status === "failed" && "Payment Failed"}
          </h1>

          {/* Message */}
          <p className="text-gray-600 mb-6">{message}</p>

          {/* Transaction Details */}
          {(status === "success" || status === "failed") && (
            <div className="bg-gray-50 rounded-lg p-4 mb-6 text-left">
              <h3 className="font-semibold text-gray-700 mb-3 text-sm">
                Transaction Details
              </h3>
              <div className="space-y-2 text-sm">
                {searchParams.get("bdOrderId") && (
                  <div className="flex justify-between">
                    <span className="text-gray-600">Order ID:</span>
                    <span className="font-mono text-gray-800">
                      {searchParams.get("bdOrderId")}
                    </span>
                  </div>
                )}
                {searchParams.get("amount") && (
                  <div className="flex justify-between">
                    <span className="text-gray-600">Amount:</span>
                    <span className="font-semibold text-gray-800">
                      ₹{searchParams.get("amount")}
                    </span>
                  </div>
                )}
                {searchParams.get("status") && (
                  <div className="flex justify-between">
                    <span className="text-gray-600">Status:</span>
                    <span
                      className={`font-semibold ${
                        searchParams.get("status")?.toUpperCase() === "SUCCESS"
                          ? "text-green-600"
                          : "text-red-600"
                      }`}
                    >
                      {searchParams.get("status")?.toUpperCase()}
                    </span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Success Actions */}
          {status === "success" && (
            <div className="space-y-3">
              <div className="flex items-start p-3 bg-green-50 border border-green-200 rounded-lg text-left">
                <AlertCircle className="w-5 h-5 text-green-600 mt-0.5 mr-2 flex-shrink-0" />
                <div className="text-sm text-green-700">
                  <p className="font-medium">What's next?</p>
                  <p className="mt-1">
                    Your application will be forwarded to the Clerk for
                    verification.
                  </p>
                </div>
              </div>

              <p className="text-sm text-gray-500">
                Redirecting to application details in 3 seconds...
              </p>
            </div>
          )}

          {/* Failed Actions */}
          {status === "failed" && (
            <div className="space-y-3">
              <div className="flex items-start p-3 bg-red-50 border border-red-200 rounded-lg text-left">
                <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 mr-2 flex-shrink-0" />
                <div className="text-sm text-red-700">
                  <p className="font-medium">Payment unsuccessful</p>
                  <p className="mt-1">
                    Please try again or contact support if the issue persists.
                  </p>
                </div>
              </div>

              <p className="text-sm text-gray-500">
                Redirecting back in 5 seconds...
              </p>
            </div>
          )}

          {/* Loading State */}
          {status === "loading" && (
            <div className="space-y-3">
              <div className="flex items-center justify-center space-x-2">
                <div
                  className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"
                  style={{ animationDelay: "0ms" }}
                ></div>
                <div
                  className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"
                  style={{ animationDelay: "150ms" }}
                ></div>
                <div
                  className="w-2 h-2 bg-blue-600 rounded-full animate-bounce"
                  style={{ animationDelay: "300ms" }}
                ></div>
              </div>
              <p className="text-sm text-gray-500">
                Please wait while we confirm your payment...
              </p>
            </div>
          )}
        </div>

        {/* Help Text */}
        <div className="mt-6 text-center">
          <p className="text-sm text-gray-600">
            Need help? Contact support at{" "}
            <a
              href="mailto:support@pmcrms.in"
              className="text-blue-600 hover:underline"
            >
              support@pmcrms.in
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default PaymentCallback;
