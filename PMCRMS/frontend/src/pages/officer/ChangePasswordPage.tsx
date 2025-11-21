import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiService } from "../../services/apiService";
import { useAuth } from "../../hooks/useAuth";
import {
  Lock,
  Eye,
  EyeOff,
  CheckCircle,
  Shield,
  AlertCircle,
} from "lucide-react";

const ChangePasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, refreshUser } = useAuth();
  const [formData, setFormData] = useState({
    temporaryPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showTempPassword, setShowTempPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [passwordStrength, setPasswordStrength] = useState<{
    score: number;
    message: string;
    color: string;
  }>({ score: 0, message: "", color: "" });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    if (error) setError("");

    // Check password strength
    if (name === "newPassword") {
      checkPasswordStrength(value);
    }
  };

  const checkPasswordStrength = (password: string) => {
    let score = 0;
    if (password.length >= 8) score++;
    if (password.length >= 12) score++;
    if (/[a-z]/.test(password)) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[@$!%*?&#]/.test(password)) score++;

    let message = "";
    let color = "";

    if (score <= 2) {
      message = "Weak";
      color = "#ef4444";
    } else if (score <= 4) {
      message = "Fair";
      color = "#f59e0b";
    } else if (score <= 5) {
      message = "Good";
      color = "#10b981";
    } else {
      message = "Excellent";
      color = "#059669";
    }

    setPasswordStrength({ score, message, color });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    // Validation
    if (formData.newPassword !== formData.confirmPassword) {
      setError("Passwords do not match");
      setLoading(false);
      return;
    }

    if (formData.newPassword.length < 8) {
      setError("Password must be at least 8 characters long");
      setLoading(false);
      return;
    }

    if (
      !/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])/.test(
        formData.newPassword
      )
    ) {
      setError(
        "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character"
      );
      setLoading(false);
      return;
    }

    try {
      const response = await apiService.auth.changePasswordFirstTime(
        formData.temporaryPassword,
        formData.newPassword,
        formData.confirmPassword
      );

      if (response.success && response.data) {
        // Update token and user data in localStorage
        localStorage.setItem("pmcrms_token", response.data.token);
        localStorage.setItem("pmcrms_user", JSON.stringify(response.data.user));

        // Refresh user data in context
        if (refreshUser) {
          refreshUser();
        }
        // Navigate to profile setup
        navigate("/officer/profile-setup");
      } else {
        throw new Error(response.message || "Failed to change password");
      }
    } catch (error: unknown) {
      let errorMessage = "Failed to change password. Please try again.";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as {
          response?: { data?: { message?: string } };
        };
        errorMessage =
          axiosError.response?.data?.message ||
          "Failed to change password. Please try again.";
      }
      setError(errorMessage);
      console.error("Password change error:", errorMessage);
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
                  Pune Municipal Corporation
                </h1>
                <p className="pmc-auth-subtitle">
                  Change Your Temporary Password
                </p>
              </div>
            </div>
          </div>

          {/* Change Password Card */}
          <div className="pmc-max-w-2xl pmc-mx-auto pmc-fadeInUp">
            <div className="pmc-auth-card">
              {/* Header */}
              <div className="pmc-p-8 pmc-bg-gradient-to-br pmc-from-sky-600 pmc-to-blue-700 pmc-text-white">
                <div className="pmc-flex pmc-items-center pmc-gap-4 pmc-mb-4">
                  <div className="pmc-w-16 pmc-h-16 pmc-bg-white/20 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center">
                    <Shield className="pmc-w-8 pmc-h-8" />
                  </div>
                  <div>
                    <h2 className="pmc-text-2xl pmc-font-bold">
                      Change Temporary Password
                    </h2>
                    <p className="pmc-text-sky-100">
                      Welcome, {user?.name || "Officer"}!
                    </p>
                  </div>
                </div>
                <div className="pmc-bg-white/10 pmc-rounded-lg pmc-p-4">
                  <div className="pmc-flex pmc-items-start pmc-gap-3">
                    <AlertCircle className="pmc-w-5 pmc-h-5 pmc-text-amber-300 pmc-flex-shrink-0 pmc-mt-0.5" />
                    <div className="pmc-text-sm pmc-text-sky-50">
                      <p className="pmc-font-semibold pmc-mb-2">
                        Security Requirement:
                      </p>
                      <p>
                        For your security, you must change your temporary
                        password before accessing the system. Please create a
                        strong password that you'll remember.
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Form */}
              <div className="pmc-p-8">
                {error && (
                  <div className="pmc-alert pmc-alert-error pmc-mb-6 pmc-fadeIn">
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
                      <p className="pmc-alert-title">Error</p>
                      <p className="pmc-alert-message">{error}</p>
                    </div>
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  {/* Temporary Password Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label pmc-label-required">
                      Temporary Password
                    </label>
                    <div className="pmc-input-with-icon">
                      <input
                        type={showTempPassword ? "text" : "password"}
                        name="temporaryPassword"
                        value={formData.temporaryPassword}
                        onChange={handleInputChange}
                        placeholder="Enter the temporary password from email"
                        required
                      />
                      <div className="pmc-input-icon-left">
                        <Lock className="pmc-w-5 pmc-h-5" />
                      </div>
                      <div
                        className="pmc-input-icon-right pmc-cursor-pointer"
                        onClick={() => setShowTempPassword(!showTempPassword)}
                      >
                        {showTempPassword ? (
                          <EyeOff className="pmc-w-5 pmc-h-5" />
                        ) : (
                          <Eye className="pmc-w-5 pmc-h-5" />
                        )}
                      </div>
                    </div>
                    <p className="pmc-help-text">
                      The password sent to your email
                    </p>
                  </div>

                  {/* New Password Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label pmc-label-required">
                      New Password
                    </label>
                    <div className="pmc-input-with-icon">
                      <input
                        type={showNewPassword ? "text" : "password"}
                        name="newPassword"
                        value={formData.newPassword}
                        onChange={handleInputChange}
                        placeholder="Create a strong password"
                        required
                      />
                      <div className="pmc-input-icon-left">
                        <Lock className="pmc-w-5 pmc-h-5" />
                      </div>
                      <div
                        className="pmc-input-icon-right pmc-cursor-pointer"
                        onClick={() => setShowNewPassword(!showNewPassword)}
                      >
                        {showNewPassword ? (
                          <EyeOff className="pmc-w-5 pmc-h-5" />
                        ) : (
                          <Eye className="pmc-w-5 pmc-h-5" />
                        )}
                      </div>
                    </div>

                    {/* Password Strength Indicator */}
                    {formData.newPassword && (
                      <div className="pmc-mt-3">
                        <div className="pmc-flex pmc-items-center pmc-justify-between pmc-mb-2">
                          <span className="pmc-text-sm pmc-font-medium pmc-text-gray-700">
                            Password Strength:
                          </span>
                          <span
                            className="pmc-text-sm pmc-font-semibold"
                            style={{ color: passwordStrength.color }}
                          >
                            {passwordStrength.message}
                          </span>
                        </div>
                        <div className="pmc-w-full pmc-bg-gray-200 pmc-rounded-full pmc-h-2">
                          <div
                            className="pmc-h-2 pmc-rounded-full pmc-transition-all pmc-duration-300"
                            style={{
                              width: `${(passwordStrength.score / 6) * 100}%`,
                              backgroundColor: passwordStrength.color,
                            }}
                          ></div>
                        </div>
                      </div>
                    )}

                    <div className="pmc-mt-3 pmc-bg-blue-50 pmc-border pmc-border-blue-200 pmc-rounded-lg pmc-p-3">
                      <p className="pmc-text-sm pmc-font-semibold pmc-text-blue-900 pmc-mb-2">
                        Password Requirements:
                      </p>
                      <ul className="pmc-text-sm pmc-text-blue-700 pmc-space-y-1">
                        <li className="pmc-flex pmc-items-center pmc-gap-2">
                          <CheckCircle
                            className={`pmc-w-4 pmc-h-4 ${
                              formData.newPassword.length >= 8
                                ? "pmc-text-green-600"
                                : "pmc-text-gray-400"
                            }`}
                          />
                          At least 8 characters
                        </li>
                        <li className="pmc-flex pmc-items-center pmc-gap-2">
                          <CheckCircle
                            className={`pmc-w-4 pmc-h-4 ${
                              /[A-Z]/.test(formData.newPassword)
                                ? "pmc-text-green-600"
                                : "pmc-text-gray-400"
                            }`}
                          />
                          One uppercase letter
                        </li>
                        <li className="pmc-flex pmc-items-center pmc-gap-2">
                          <CheckCircle
                            className={`pmc-w-4 pmc-h-4 ${
                              /[a-z]/.test(formData.newPassword)
                                ? "pmc-text-green-600"
                                : "pmc-text-gray-400"
                            }`}
                          />
                          One lowercase letter
                        </li>
                        <li className="pmc-flex pmc-items-center pmc-gap-2">
                          <CheckCircle
                            className={`pmc-w-4 pmc-h-4 ${
                              /\d/.test(formData.newPassword)
                                ? "pmc-text-green-600"
                                : "pmc-text-gray-400"
                            }`}
                          />
                          One number
                        </li>
                        <li className="pmc-flex pmc-items-center pmc-gap-2">
                          <CheckCircle
                            className={`pmc-w-4 pmc-h-4 ${
                              /[@$!%*?&#]/.test(formData.newPassword)
                                ? "pmc-text-green-600"
                                : "pmc-text-gray-400"
                            }`}
                          />
                          One special character (@$!%*?&#)
                        </li>
                      </ul>
                    </div>
                  </div>

                  {/* Confirm Password Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label pmc-label-required">
                      Confirm New Password
                    </label>
                    <div className="pmc-input-with-icon">
                      <input
                        type={showConfirmPassword ? "text" : "password"}
                        name="confirmPassword"
                        value={formData.confirmPassword}
                        onChange={handleInputChange}
                        placeholder="Re-enter your new password"
                        required
                      />
                      <div className="pmc-input-icon-left">
                        <Lock className="pmc-w-5 pmc-h-5" />
                      </div>
                      <div
                        className="pmc-input-icon-right pmc-cursor-pointer"
                        onClick={() =>
                          setShowConfirmPassword(!showConfirmPassword)
                        }
                      >
                        {showConfirmPassword ? (
                          <EyeOff className="pmc-w-5 pmc-h-5" />
                        ) : (
                          <Eye className="pmc-w-5 pmc-h-5" />
                        )}
                      </div>
                    </div>
                    {formData.confirmPassword &&
                      formData.newPassword !== formData.confirmPassword && (
                        <p className="pmc-text-sm pmc-text-red-600 pmc-mt-2">
                          Passwords do not match
                        </p>
                      )}
                  </div>

                  {/* Submit Button */}
                  <button
                    type="submit"
                    disabled={loading}
                    className={`pmc-button pmc-button-primary pmc-button-full ${
                      loading ? "pmc-button-loading" : ""
                    }`}
                  >
                    {!loading && (
                      <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2">
                        <CheckCircle className="pmc-w-5 pmc-h-5" />
                        <span>Change Password & Continue</span>
                      </div>
                    )}
                    {loading && <span>Changing Password...</span>}
                  </button>
                </form>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="pmc-auth-footer pmc-fadeInUp pmc-mt-8">
            <div className="pmc-auth-footer-box">
              <Shield className="pmc-w-5 pmc-h-5" />
              <span className="pmc-auth-footer-text">
                © {new Date().getFullYear()} Pune Municipal Corporation - Secure
                Officer Portal
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ChangePasswordPage;
