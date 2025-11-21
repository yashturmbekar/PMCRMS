import React, { useState } from "react";
import { Link } from "react-router-dom";
import { authService } from "../../services";
import { Mail, Send, CheckCircle2, AlertCircle, ArrowLeft } from "lucide-react";
import { FullScreenLoader } from "../../components";

const OfficerForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await authService.officerForgotPassword(email);
      if (response.success) {
        setSuccess(true);
      } else {
        throw new Error(response.message || "Failed to send reset email");
      }
    } catch (error: any) {
      setError(
        error.response?.data?.message ||
          error.message ||
          "Failed to send reset email"
      );
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full pmc-p-8">
          <div className="pmc-text-center pmc-mb-6">
            <div className="pmc-w-16 pmc-h-16 pmc-bg-green-100 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center pmc-mx-auto pmc-mb-4">
              <CheckCircle2 className="pmc-w-10 pmc-h-10 pmc-text-green-600" />
            </div>
            <h2 className="pmc-text-2xl pmc-font-bold pmc-text-gray-900 pmc-mb-2">
              Check Your Email
            </h2>
            <p className="pmc-text-gray-600">
              If an account exists with{" "}
              <span className="pmc-font-semibold">{email}</span>, you'll receive
              password reset instructions.
            </p>
          </div>

          <div className="pmc-bg-blue-50 pmc-border pmc-border-blue-200 pmc-rounded-lg pmc-p-4 pmc-mb-6">
            <div className="pmc-flex pmc-items-start pmc-gap-3">
              <Mail className="pmc-w-5 pmc-h-5 pmc-text-blue-600 pmc-flex-shrink-0 pmc-mt-0.5" />
              <div className="pmc-text-sm pmc-text-blue-800">
                <p className="pmc-font-semibold pmc-mb-2">Next Steps:</p>
                <ul className="pmc-list-disc pmc-list-inside pmc-space-y-1">
                  <li>Check your email inbox</li>
                  <li>Look for a message from PMCRMS</li>
                  <li>Click the reset link or use the provided token</li>
                  <li>The link will expire in 1 hour</li>
                </ul>
              </div>
            </div>
          </div>

          <div className="pmc-text-center pmc-space-y-3">
            <p className="pmc-text-sm pmc-text-gray-600">
              Didn't receive the email? Check your spam folder.
            </p>
            <button
              onClick={() => setSuccess(false)}
              className="pmc-text-sky-600 hover:pmc-text-sky-800 pmc-font-medium pmc-text-sm"
            >
              Try another email address
            </button>
          </div>

          <div className="pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200 pmc-text-center">
            <Link
              to="/officer/login"
              className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2 pmc-text-gray-600 hover:pmc-text-gray-900"
            >
              <ArrowLeft className="pmc-w-4 pmc-h-4" />
              <span>Back to Login</span>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
      {loading && (
        <FullScreenLoader
          message="Sending Reset Email..."
          submessage="Please wait while we send password reset instructions"
        />
      )}
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full">
          {/* Card Header */}
          <div className="pmc-p-8 pmc-bg-gradient-to-br pmc-from-sky-600 pmc-to-blue-700 pmc-text-white pmc-rounded-t-xl">
            <div className="pmc-text-center">
              <div className="pmc-w-16 pmc-h-16 pmc-bg-white/20 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center pmc-mx-auto pmc-mb-4">
                <Mail className="pmc-w-8 pmc-h-8" />
              </div>
              <h2 className="pmc-text-2xl pmc-font-bold pmc-mb-2">
                Forgot Password?
              </h2>
              <p className="pmc-text-sky-100">
                No worries! Enter your email and we'll send you reset
                instructions.
              </p>
            </div>
          </div>

          {/* Form */}
          <div className="pmc-p-8">
            {error && (
              <div className="pmc-alert pmc-alert-error pmc-mb-6">
                <AlertCircle className="pmc-w-5 pmc-h-5" />
                <p>{error}</p>
              </div>
            )}

            <form onSubmit={handleSubmit} className="pmc-space-y-6">
              <div className="pmc-form-group">
                <label className="pmc-label pmc-label-required">
                  Email Address
                </label>
                <div className="pmc-input-with-icon">
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="officer@pmc.gov.in"
                    required
                  />
                  <div className="pmc-input-icon-left">
                    <Mail className="pmc-w-5 pmc-h-5" />
                  </div>
                </div>
                <p className="pmc-text-sm pmc-text-gray-600 pmc-mt-2">
                  Enter the email associated with your officer account
                </p>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="pmc-button pmc-button-primary pmc-button-full"
              >
                <Send className="pmc-w-5 pmc-h-5" />
                <span>Send Reset Instructions</span>
              </button>
            </form>

            <div className="pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200">
              <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2 pmc-text-sm">
                <span className="pmc-text-gray-600">
                  Remember your password?
                </span>
                <Link
                  to="/officer/login"
                  className="pmc-text-sky-600 hover:pmc-text-sky-800 pmc-font-medium"
                >
                  Sign In
                </Link>
              </div>
            </div>

            <div className="pmc-mt-6 pmc-bg-amber-50 pmc-border pmc-border-amber-200 pmc-rounded-lg pmc-p-4">
              <div className="pmc-flex pmc-items-start pmc-gap-3">
                <AlertCircle className="pmc-w-5 pmc-h-5 pmc-text-amber-600 pmc-flex-shrink-0 pmc-mt-0.5" />
                <div className="pmc-text-sm pmc-text-amber-800">
                  <p className="pmc-font-semibold pmc-mb-1">Security Notice</p>
                  <p>
                    For security reasons, we don't disclose whether an email
                    exists in our system. You'll receive an email only if your
                    account is registered.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default OfficerForgotPasswordPage;
