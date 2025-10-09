import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiService } from "../services/apiService";

const OfficerLoginPage: React.FC = () => {
  const [formData, setFormData] = useState({
    email: "",
    password: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const navigate = useNavigate();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    if (error) setError("");
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await apiService.auth.officerLogin(
        formData.email,
        formData.password
      );

      if (response.success && response.data) {
        // Store token and user data
        localStorage.setItem("pmcrms_token", response.data.token);
        localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));

        // Navigate to dashboard
        navigate("/dashboard");
      } else {
        throw new Error(
          response.message || "Login failed. Please check your credentials."
        );
      }
    } catch (error: unknown) {
      let errorMessage = "Login failed. Please check your credentials.";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as {
          response?: { data?: { message?: string } };
        };
        errorMessage =
          axiosError.response?.data?.message ||
          "Login failed. Please check your credentials.";
      }
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
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
                  Officer Management & Administrative Portal
                </p>
              </div>
            </div>
          </div>

          {/* Login Card */}
          <div className="pmc-max-w-7xl pmc-mx-auto pmc-fadeInUp">
            <div className="pmc-auth-card">
              <div className="pmc-auth-grid">
                {/* Left Panel - Officer Portal Branding */}
                <div
                  className="pmc-auth-brand-panel"
                  style={{
                    background:
                      "linear-gradient(135deg, #4338ca 0%, #6366f1 50%, #3b82f6 100%)",
                  }}
                >
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
                          d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                        />
                      </svg>
                    </div>

                    {/* Title */}
                    <h2 className="pmc-auth-brand-title">Officer Portal</h2>
                    <div className="pmc-auth-brand-description">
                      <p>Administrative access for officials</p>
                      <p>Application processing & certificate management</p>
                    </div>

                    {/* Feature List */}
                    <ul className="pmc-auth-features">
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
                              d="M9 5H7a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Review & process applications
                        </span>
                      </li>
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
                          Document verification & approval
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
                              d="M13 10V3L4 14h7v7l9-11h-7z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Real-time status updates
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
                              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                            />
                          </svg>
                        </div>
                        <span className="pmc-auth-feature-text">
                          Analytics & reporting dashboard
                        </span>
                      </li>
                    </ul>

                    {/* Support Contact */}
                    <div className="pmc-auth-support">
                      <p className="pmc-auth-support-label">
                        Technical support & assistance
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
                      <h3 className="pmc-auth-form-title">Officer Login</h3>
                      <button
                        onClick={() => navigate("/login")}
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
                            d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                          />
                        </svg>
                        User Portal
                      </button>
                    </div>
                    <p className="pmc-auth-form-description">
                      Sign in with your official PMC credentials to access
                      administrative functions
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

                  {/* Login Form */}
                  <form onSubmit={handleLogin}>
                    {/* Email Field */}
                    <div className="pmc-form-group pmc-mb-6">
                      <label className="pmc-label pmc-label-required">
                        Official Email Address
                      </label>
                      <div className="pmc-input-with-icon">
                        <input
                          type="email"
                          name="email"
                          value={formData.email}
                          onChange={handleInputChange}
                          placeholder="Enter your PMC email"
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
                        Use your official PMC email address
                      </p>
                    </div>

                    {/* Password Field */}
                    <div className="pmc-form-group pmc-mb-6">
                      <label className="pmc-label pmc-label-required">
                        Password
                      </label>
                      <div className="pmc-input-with-icon">
                        <input
                          type={showPassword ? "text" : "password"}
                          name="password"
                          value={formData.password}
                          onChange={handleInputChange}
                          placeholder="Enter your secure password"
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
                        <div
                          className="pmc-input-icon-right"
                          onClick={() => setShowPassword(!showPassword)}
                        >
                          {showPassword ? (
                            <svg
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21"
                              />
                            </svg>
                          ) : (
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
                          )}
                        </div>
                      </div>
                      <p className="pmc-help-text">
                        Your secure PMC administrative account password
                      </p>
                    </div>

                    {/* Login Options */}
                    <div className="pmc-flex pmc-items-center pmc-justify-between pmc-mb-6">
                      <label className="pmc-flex pmc-items-center pmc-cursor-pointer">
                        <input type="checkbox" className="pmc-checkbox" />
                        <span
                          className="pmc-text-sm pmc-text-gray-600"
                          style={{ marginLeft: "8px" }}
                        >
                          Remember me on this device
                        </span>
                      </label>
                      <button
                        type="button"
                        className="pmc-text-sm pmc-text-primary pmc-font-semibold"
                      >
                        Reset password?
                      </button>
                    </div>

                    {/* Login Button */}
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
                              d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"
                            />
                          </svg>
                          <span>Access Officer Portal</span>
                        </div>
                      )}
                      {loading && <span>Authenticating...</span>}
                    </button>

                    {/* Additional Information */}
                    <div className="pmc-text-center pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200">
                      <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2 pmc-mb-3">
                        <div
                          style={{
                            width: "8px",
                            height: "8px",
                            background: "#10b981",
                            borderRadius: "50%",
                          }}
                        ></div>
                        <p className="pmc-text-sm pmc-text-gray-600 pmc-font-semibold">
                          Secure Administrative Access
                        </p>
                      </div>
                      <p className="pmc-text-xs pmc-text-gray-500">
                        This portal is restricted to authorized PMC officers
                        only
                      </p>
                    </div>
                  </form>
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
                Administrative Portal
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OfficerLoginPage;
