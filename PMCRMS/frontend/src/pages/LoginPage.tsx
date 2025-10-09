import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiService } from "../services/apiService";

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState("");
  const [otp, setOtp] = useState("");
  const [step, setStep] = useState<"email" | "otp">("email");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const navigate = useNavigate();

  const handleSendOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      const response = await apiService.auth.sendOtp(email, "LOGIN");

      if (response.success) {
        setSuccess(response.message || "OTP sent successfully to your email!");
        setStep("otp");
      } else {
        throw new Error(
          response.message || "Failed to send OTP. Please try again."
        );
      }
    } catch (error: unknown) {
      let errorMessage = "Failed to send OTP";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as {
          response?: { data?: { message?: string } };
        };
        errorMessage =
          axiosError.response?.data?.message || "Failed to send OTP";
      }
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await apiService.auth.verifyOtp({
        identifier: email,
        otpCode: otp,
        purpose: "LOGIN",
      });

      if (response.success && response.data) {
        // Store token and user data
        localStorage.setItem("pmcrms_token", response.data.token);
        localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));

        // Navigate to dashboard
        navigate("/dashboard");
      } else {
        throw new Error(
          response.message || "OTP verification failed. Please try again."
        );
      }
    } catch (error: unknown) {
      let errorMessage = "OTP verification failed";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as {
          response?: { data?: { message?: string } };
        };
        errorMessage =
          axiosError.response?.data?.message || "OTP verification failed";
      }
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleResendOtp = async () => {
    setOtp("");
    setError("");
    setSuccess("");
    setLoading(true);

    try {
      // Simulate API call to resend OTP
      await new Promise((resolve) => setTimeout(resolve, 1500));
      setSuccess("OTP resent successfully to your email!");
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : "Failed to resend OTP";
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleBackToEmail = () => {
    setStep("email");
    setOtp("");
    setError("");
    setSuccess("");
  };

  return (
    <div className="pmc-auth-page">
      {/* Background elements */}
      <div className="pmc-auth-orb-1"></div>
      <div className="pmc-auth-orb-2"></div>
      <div className="pmc-auth-orb-3"></div>

      {/* Main Content */}
      <div className="pmc-auth-content">
        <div className="pmc-w-full pmc-max-w-7xl pmc-mx-auto">
          {/* Government Header */}
          <div className="pmc-auth-header pmc-fadeInDown">
            <div className="pmc-auth-logo-group">
              <div className="pmc-auth-logo-box">
                <img
                  src="/pmc-logo.png"
                  alt="PMC Logo"
                  onError={(e) => {
                    const target = e.target as HTMLImageElement;
                    target.style.display = "none";
                    const parent = target.parentElement;
                    if (parent) {
                      parent.innerHTML =
                        '<div style="color: white; font-weight: 700; font-size: 1.25rem;">PMC</div>';
                    }
                  }}
                />
              </div>
              <div className="pmc-auth-title-group">
                <h1 className="pmc-auth-main-title">
                  Pimpri Chinchwad Municipal Corporation
                </h1>
                <p className="pmc-auth-subtitle">
                  Permit Management & Certificate Recommendation System
                </p>
              </div>
            </div>
          </div>

          {/* Login Card */}
          <div className="pmc-max-w-7xl pmc-mx-auto pmc-fadeInUp">
            <div className="pmc-auth-card">
              <div className="pmc-auth-grid">
                {/* Left Panel - User Portal Branding */}
                <div className="pmc-auth-brand-panel">
                  <div className="pmc-auth-brand-pattern"></div>
                  <div className="pmc-auth-brand-accent-1"></div>
                  <div className="pmc-auth-brand-accent-2"></div>

                  <div className="pmc-auth-brand-content">
                    {/* Icon */}
                    <div className="pmc-auth-brand-icon">
                      <svg
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                        />
                      </svg>
                    </div>

                    {/* Title */}
                    <h2 className="pmc-auth-brand-title">User Portal</h2>
                    <div className="pmc-auth-brand-description">
                      <p>Secure access for citizens</p>
                      <p>
                        Building permit applications & certificate management
                      </p>
                    </div>

                    {/* Feature List */}
                    <ul className="pmc-auth-features">
                      <li className="pmc-auth-feature-item">
                        <div className="pmc-auth-feature-icon">
                          <svg
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Submit permit applications online
                        </span>
                      </li>
                      <li className="pmc-auth-feature-item">
                        <div className="pmc-auth-feature-icon blue">
                          <svg
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                            />
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Track application status in real-time
                        </span>
                      </li>
                      <li className="pmc-auth-feature-item">
                        <div className="pmc-auth-feature-icon purple">
                          <svg
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Download approved certificates
                        </span>
                      </li>
                      <li className="pmc-auth-feature-item">
                        <div className="pmc-auth-feature-icon amber">
                          <svg
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          24/7 application support
                        </span>
                      </li>
                    </ul>

                    {/* Support Contact */}
                    <div className="pmc-auth-support">
                      <p className="pmc-auth-support-label">
                        Need assistance? Contact support
                      </p>
                      <div className="pmc-auth-support-contact">
                        <svg
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
                          />
                        </svg>
                        <span className="pmc-auth-support-phone">
                          +91 9284341115
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Right Panel - Login Form */}
                <div className="pmc-auth-form-panel">
                  {/* Form Header */}
                  <div className="pmc-auth-form-header">
                    <div className="pmc-auth-form-title-row">
                      <h3 className="pmc-auth-form-title">User Login</h3>
                      <button
                        onClick={() => navigate("/officer-login")}
                        className="pmc-auth-switch-btn"
                      >
                        <svg
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                          />
                        </svg>
                        Officer Portal
                      </button>
                    </div>
                    <p className="pmc-auth-form-description">
                      Access your citizen account to manage applications and
                      certificates
                    </p>
                  </div>

                  {/* Error Alert */}
                  {error && (
                    <div className="pmc-alert pmc-alert-error pmc-fadeIn">
                      <div className="pmc-alert-icon">
                        <svg
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                          />
                        </svg>
                      </div>
                      <div className="pmc-alert-content">
                        <p className="pmc-alert-title">Authentication Failed</p>
                        <p className="pmc-alert-message">{error}</p>
                      </div>
                    </div>
                  )}

                  {/* Success Alert */}
                  {success && (
                    <div className="pmc-alert pmc-alert-success pmc-fadeIn">
                      <div className="pmc-alert-icon">
                        <svg
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                          />
                        </svg>
                      </div>
                      <div className="pmc-alert-content">
                        <p className="pmc-alert-title">Success</p>
                        <p className="pmc-alert-message">{success}</p>
                      </div>
                    </div>
                  )}

                  {/* Step 1: Email Entry */}
                  {step === "email" && (
                    <form onSubmit={handleSendOtp}>
                      <div className="pmc-form-group pmc-mb-6">
                        <label className="pmc-label pmc-label-required">
                          Email Address
                        </label>
                        <div className="pmc-input-with-icon">
                          <input
                            type="email"
                            name="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="Enter your registered email"
                            required
                          />
                          <div className="pmc-input-icon-left">
                            <svg
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207"
                              />
                            </svg>
                          </div>
                        </div>
                        <p className="pmc-help-text">
                          We'll send a 6-digit OTP to verify your identity
                        </p>
                      </div>

                      <button
                        type="submit"
                        disabled={loading}
                        className={`pmc-button pmc-button-primary pmc-button-full ${
                          loading ? "pmc-button-loading" : ""
                        }`}
                      >
                        {!loading && (
                          <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2">
                            <svg
                              style={{ width: "20px", height: "20px" }}
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                              />
                            </svg>
                            <span>Send OTP</span>
                          </div>
                        )}
                        {loading && <span>Sending OTP...</span>}
                      </button>

                      {/* Registration Link */}
                      <div className="pmc-text-center pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200">
                        <p className="pmc-text-gray-600">
                          Don't have an account?{" "}
                          <button
                            type="button"
                            className="pmc-text-primary pmc-font-semibold"
                          >
                            Create User Account
                          </button>
                        </p>
                        <p className="pmc-text-xs pmc-text-gray-500 pmc-mt-2">
                          Register to apply for building permits and
                          certificates
                        </p>
                      </div>
                    </form>
                  )}

                  {/* Step 2: OTP Verification */}
                  {step === "otp" && (
                    <form onSubmit={handleVerifyOtp}>
                      {/* Back Button */}
                      <button
                        type="button"
                        onClick={handleBackToEmail}
                        className="pmc-flex pmc-items-center pmc-gap-2 pmc-text-gray-600 pmc-mb-6 pmc-text-sm"
                      >
                        <svg
                          style={{ width: "16px", height: "16px" }}
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M15 19l-7-7 7-7"
                          />
                        </svg>
                        Change email address
                      </button>

                      <div className="pmc-mb-6 pmc-p-4 pmc-bg-blue-50 pmc-rounded-lg pmc-border pmc-border-blue-200">
                        <p className="pmc-text-sm pmc-text-blue-800 pmc-mb-1">
                          <strong>OTP sent to:</strong> {email}
                        </p>
                        <p className="pmc-text-xs pmc-text-blue-600">
                          Please check your email inbox (and spam folder)
                        </p>
                      </div>

                      <div className="pmc-form-group pmc-mb-6">
                        <label className="pmc-label pmc-label-required">
                          Enter OTP Code
                        </label>
                        <div className="pmc-input-with-icon">
                          <input
                            type="text"
                            name="otp"
                            value={otp}
                            onChange={(e) =>
                              setOtp(
                                e.target.value.replace(/\D/g, "").slice(0, 6)
                              )
                            }
                            placeholder="Enter 6-digit OTP"
                            maxLength={6}
                            required
                          />
                          <div className="pmc-input-icon-left">
                            <svg
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                              />
                            </svg>
                          </div>
                        </div>
                        <p className="pmc-help-text">
                          OTP is valid for 10 minutes
                        </p>
                      </div>

                      <button
                        type="submit"
                        disabled={loading || otp.length !== 6}
                        className={`pmc-button pmc-button-primary pmc-button-full ${
                          loading ? "pmc-button-loading" : ""
                        }`}
                      >
                        {!loading && (
                          <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2">
                            <svg
                              style={{ width: "20px", height: "20px" }}
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"
                              />
                            </svg>
                            <span>Verify & Sign In</span>
                          </div>
                        )}
                        {loading && <span>Verifying...</span>}
                      </button>

                      {/* Resend OTP */}
                      <div className="pmc-text-center pmc-mt-6">
                        <p className="pmc-text-gray-600 pmc-text-sm">
                          Didn't receive the OTP?{" "}
                          <button
                            type="button"
                            onClick={handleResendOtp}
                            disabled={loading}
                            className="pmc-text-primary pmc-font-semibold"
                          >
                            Resend OTP
                          </button>
                        </p>
                      </div>
                    </form>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="pmc-auth-footer pmc-fadeInUp">
            <div className="pmc-auth-footer-box">
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                />
              </svg>
              <span className="pmc-auth-footer-text">
                © 2024 Pimpri Chinchwad Municipal Corporation - Secure
                Government Portal
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
