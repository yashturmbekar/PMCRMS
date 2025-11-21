import React, { useState, useEffect } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { authService } from "../../services";
import {
  Lock,
  Eye,
  EyeOff,
  CheckCircle2,
  AlertCircle,
  XCircle,
  ArrowRight,
  Mail,
} from "lucide-react";
import { FullScreenLoader } from "../../components";

const OfficerResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const [validatingToken, setValidatingToken] = useState(true);
  const [tokenValid, setTokenValid] = useState(false);
  const [tokenError, setTokenError] = useState("");
  const [officerInfo, setOfficerInfo] = useState<{
    name: string;
    email: string;
  } | null>(null);

  const [formData, setFormData] = useState({
    newPassword: "",
    confirmPassword: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  useEffect(() => {
    if (!token) {
      setTokenValid(false);
      setTokenError("No reset token provided");
      setValidatingToken(false);
      return;
    }

    validateToken();
  }, [token]);

  const validateToken = async () => {
    try {
      const response = await authService.validateOfficerResetToken(token!);
      if (response.success && response.data) {
        setTokenValid(true);
        setOfficerInfo({
          name: response.data.officerName,
          email: response.data.email,
        });
      } else {
        setTokenValid(false);
        setTokenError(response.message || "Invalid or expired reset token");
      }
    } catch (error: any) {
      setTokenValid(false);
      setTokenError(
        error.response?.data?.message ||
          error.message ||
          "Invalid or expired reset token"
      );
    } finally {
      setValidatingToken(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (error) setError("");
  };

  const validatePassword = (password: string): boolean => {
    return (
      password.length >= 8 &&
      /[a-z]/.test(password) &&
      /[A-Z]/.test(password) &&
      /\d/.test(password) &&
      /[@$!%*?&#]/.test(password)
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    if (formData.newPassword !== formData.confirmPassword) {
      setError("Passwords do not match");
      setLoading(false);
      return;
    }

    if (!validatePassword(formData.newPassword)) {
      setError(
        "Password must be at least 8 characters with uppercase, lowercase, number, and special character"
      );
      setLoading(false);
      return;
    }

    try {
      const response = await authService.officerResetPassword(
        token!,
        formData.newPassword,
        formData.confirmPassword
      );

      if (response.success) {
        setSuccess(true);
        setTimeout(() => navigate("/officer/login"), 3000);
      } else {
        throw new Error(response.message || "Failed to reset password");
      }
    } catch (error: any) {
      setError(
        error.response?.data?.message ||
          error.message ||
          "Failed to reset password"
      );
    } finally {
      setLoading(false);
    }
  };

  // Validating Token State
  if (validatingToken) {
    return (
      <FullScreenLoader
        message="Validating Reset Token..."
        submessage="Please wait while we verify your password reset link"
      />
    );
  }

  // Invalid Token State
  if (!tokenValid) {
    return (
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full pmc-p-8">
          <div className="pmc-text-center pmc-mb-6">
            <div className="pmc-w-16 pmc-h-16 pmc-bg-red-100 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center pmc-mx-auto pmc-mb-4">
              <XCircle className="pmc-w-10 pmc-h-10 pmc-text-red-600" />
            </div>
            <h2 className="pmc-text-2xl pmc-font-bold pmc-text-gray-900 pmc-mb-2">
              Invalid Reset Link
            </h2>
            <p className="pmc-text-gray-600 pmc-mb-6">{tokenError}</p>
          </div>

          <div className="pmc-bg-amber-50 pmc-border pmc-border-amber-200 pmc-rounded-lg pmc-p-4 pmc-mb-6">
            <div className="pmc-flex pmc-items-start pmc-gap-3">
              <AlertCircle className="pmc-w-5 pmc-h-5 pmc-text-amber-600 pmc-flex-shrink-0 pmc-mt-0.5" />
              <div className="pmc-text-sm pmc-text-amber-800">
                <p className="pmc-font-semibold pmc-mb-2">Possible Reasons:</p>
                <ul className="pmc-list-disc pmc-list-inside pmc-space-y-1">
                  <li>The reset link has expired (links expire in 1 hour)</li>
                  <li>The link has already been used</li>
                  <li>The link is invalid or incomplete</li>
                  <li>A newer reset request was made</li>
                </ul>
              </div>
            </div>
          </div>

          <Link
            to="/officer/forgot-password"
            className="pmc-button pmc-button-primary pmc-button-full"
          >
            <Mail className="pmc-w-5 pmc-h-5" />
            <span>Request New Reset Link</span>
          </Link>

          <div className="pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200 pmc-text-center">
            <Link
              to="/officer/login"
              className="pmc-text-gray-600 hover:pmc-text-gray-900"
            >
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Success State
  if (success) {
    return (
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full pmc-text-center pmc-p-8">
          <CheckCircle2 className="pmc-w-16 pmc-h-16 pmc-text-green-600 pmc-mx-auto pmc-mb-4" />
          <h2 className="pmc-text-2xl pmc-font-bold pmc-text-gray-900 pmc-mb-2">
            Password Reset Successfully!
          </h2>
          <p className="pmc-text-gray-600 pmc-mb-6">
            Your password has been updated. You can now sign in with your new
            password.
          </p>
          <div className="pmc-flex pmc-items-center pmc-justify-center pmc-gap-2 pmc-text-sky-600">
            <span>Redirecting to login page...</span>
            <ArrowRight className="pmc-w-5 pmc-h-5 pmc-animate-pulse" />
          </div>
        </div>
      </div>
    );
  }

  // Reset Password Form
  return (
    <>
      {loading && (
        <FullScreenLoader
          message="Resetting Password..."
          submessage="Please wait while we update your password"
        />
      )}
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full">
          {/* Card Header */}
          <div className="pmc-p-8 pmc-bg-gradient-to-br pmc-from-sky-600 pmc-to-blue-700 pmc-text-white pmc-rounded-t-xl">
            <div className="pmc-text-center pmc-mb-4">
              <div className="pmc-w-16 pmc-h-16 pmc-bg-white/20 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center pmc-mx-auto pmc-mb-4">
                <Lock className="pmc-w-8 pmc-h-8" />
              </div>
              <h2 className="pmc-text-2xl pmc-font-bold pmc-mb-2">
                Reset Your Password
              </h2>
              <p className="pmc-text-sky-100">
                Create a new password for your account
              </p>
            </div>

            {officerInfo && (
              <div className="pmc-bg-white/10 pmc-rounded-lg pmc-p-4">
                <p className="pmc-text-sm pmc-text-sky-100 pmc-mb-1">
                  Resetting password for:
                </p>
                <p className="pmc-font-semibold pmc-text-white">
                  {officerInfo.name}
                </p>
                <p className="pmc-text-sm pmc-text-sky-100">
                  {officerInfo.email}
                </p>
              </div>
            )}
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
              {/* New Password */}
              <div className="pmc-form-group">
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

                {/* Password Requirements */}
                <div className="pmc-mt-3 pmc-bg-blue-50 pmc-border pmc-border-blue-200 pmc-rounded-lg pmc-p-3">
                  <p className="pmc-text-sm pmc-font-semibold pmc-text-blue-900 pmc-mb-2">
                    Password Requirements:
                  </p>
                  <ul className="pmc-text-sm pmc-text-blue-700 pmc-space-y-1">
                    <li className="pmc-flex pmc-items-center pmc-gap-2">
                      <CheckCircle2
                        className={`pmc-w-4 pmc-h-4 ${
                          formData.newPassword.length >= 8
                            ? "pmc-text-green-600"
                            : "pmc-text-gray-400"
                        }`}
                      />
                      At least 8 characters
                    </li>
                    <li className="pmc-flex pmc-items-center pmc-gap-2">
                      <CheckCircle2
                        className={`pmc-w-4 pmc-h-4 ${
                          /[A-Z]/.test(formData.newPassword)
                            ? "pmc-text-green-600"
                            : "pmc-text-gray-400"
                        }`}
                      />
                      One uppercase letter
                    </li>
                    <li className="pmc-flex pmc-items-center pmc-gap-2">
                      <CheckCircle2
                        className={`pmc-w-4 pmc-h-4 ${
                          /[a-z]/.test(formData.newPassword)
                            ? "pmc-text-green-600"
                            : "pmc-text-gray-400"
                        }`}
                      />
                      One lowercase letter
                    </li>
                    <li className="pmc-flex pmc-items-center pmc-gap-2">
                      <CheckCircle2
                        className={`pmc-w-4 pmc-h-4 ${
                          /\d/.test(formData.newPassword)
                            ? "pmc-text-green-600"
                            : "pmc-text-gray-400"
                        }`}
                      />
                      One number
                    </li>
                    <li className="pmc-flex pmc-items-center pmc-gap-2">
                      <CheckCircle2
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

              {/* Confirm Password */}
              <div className="pmc-form-group">
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
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
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
                className="pmc-button pmc-button-primary pmc-button-full"
              >
                <CheckCircle2 className="pmc-w-5 pmc-h-5" />
                <span>Reset Password</span>
              </button>
            </form>

            <div className="pmc-mt-6 pmc-pt-6 pmc-border-t pmc-border-gray-200 pmc-text-center">
              <Link
                to="/officer/login"
                className="pmc-text-gray-600 hover:pmc-text-gray-900"
              >
                Back to Login
              </Link>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default OfficerResetPasswordPage;
