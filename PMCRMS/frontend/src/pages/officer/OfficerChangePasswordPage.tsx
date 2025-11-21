import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authService } from "../../services";
import {
  Lock,
  Eye,
  EyeOff,
  CheckCircle2,
  AlertCircle,
  ArrowLeft,
} from "lucide-react";
import { FullScreenLoader } from "../../components";

const OfficerChangePasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

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
      setError("New passwords do not match");
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
      const response = await authService.officerChangePassword(
        formData.currentPassword,
        formData.newPassword,
        formData.confirmPassword
      );

      if (response.success) {
        setSuccess(true);
        setTimeout(() => navigate("/officer/dashboard"), 2000);
      } else {
        throw new Error(response.message || "Failed to change password");
      }
    } catch (error: any) {
      setError(
        error.response?.data?.message ||
          error.message ||
          "Failed to change password"
      );
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="pmc-min-h-screen pmc-flex pmc-items-center pmc-justify-center pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-card pmc-max-w-md pmc-w-full pmc-text-center pmc-p-8">
          <CheckCircle2 className="pmc-w-16 pmc-h-16 pmc-text-green-600 pmc-mx-auto pmc-mb-4" />
          <h2 className="pmc-text-2xl pmc-font-bold pmc-text-gray-900 pmc-mb-2">
            Password Changed Successfully!
          </h2>
          <p className="pmc-text-gray-600">Redirecting you to dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <>
      {loading && (
        <FullScreenLoader
          message="Changing Password..."
          submessage="Please wait while we update your password"
        />
      )}
      <div className="pmc-min-h-screen pmc-bg-gradient-to-br pmc-from-sky-50 pmc-to-blue-100 pmc-p-6">
        <div className="pmc-max-w-2xl pmc-mx-auto">
          {/* Header */}
          <div className="pmc-mb-6">
            <button
              onClick={() => navigate("/officer/dashboard")}
              className="pmc-flex pmc-items-center pmc-gap-2 pmc-text-sky-700 hover:pmc-text-sky-900 pmc-mb-4"
            >
              <ArrowLeft className="pmc-w-5 pmc-h-5" />
              <span>Back to Dashboard</span>
            </button>
          </div>

          <div className="pmc-card">
            {/* Card Header */}
            <div className="pmc-p-8 pmc-bg-gradient-to-br pmc-from-sky-600 pmc-to-blue-700 pmc-text-white pmc-rounded-t-xl">
              <div className="pmc-flex pmc-items-center pmc-gap-4 pmc-mb-4">
                <div className="pmc-w-16 pmc-h-16 pmc-bg-white/20 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center">
                  <Lock className="pmc-w-8 pmc-h-8" />
                </div>
                <div>
                  <h2 className="pmc-text-2xl pmc-font-bold">
                    Change Password
                  </h2>
                  <p className="pmc-text-sky-100">
                    Update your account password
                  </p>
                </div>
              </div>
              <div className="pmc-bg-white/10 pmc-rounded-lg pmc-p-4">
                <div className="pmc-flex pmc-items-start pmc-gap-3">
                  <AlertCircle className="pmc-w-5 pmc-h-5 pmc-text-amber-300 pmc-flex-shrink-0 pmc-mt-0.5" />
                  <p className="pmc-text-sm pmc-text-sky-50">
                    Choose a strong password that you haven't used elsewhere.
                    For security, we'll log you out of all devices after
                    changing your password.
                  </p>
                </div>
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
                {/* Current Password */}
                <div className="pmc-form-group">
                  <label className="pmc-label pmc-label-required">
                    Current Password
                  </label>
                  <div className="pmc-input-with-icon">
                    <input
                      type={showCurrentPassword ? "text" : "password"}
                      name="currentPassword"
                      value={formData.currentPassword}
                      onChange={handleInputChange}
                      placeholder="Enter your current password"
                      required
                    />
                    <div className="pmc-input-icon-left">
                      <Lock className="pmc-w-5 pmc-h-5" />
                    </div>
                    <div
                      className="pmc-input-icon-right pmc-cursor-pointer"
                      onClick={() =>
                        setShowCurrentPassword(!showCurrentPassword)
                      }
                    >
                      {showCurrentPassword ? (
                        <EyeOff className="pmc-w-5 pmc-h-5" />
                      ) : (
                        <Eye className="pmc-w-5 pmc-h-5" />
                      )}
                    </div>
                  </div>
                </div>

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
                  className="pmc-button pmc-button-primary pmc-button-full"
                >
                  <CheckCircle2 className="pmc-w-5 pmc-h-5" />
                  <span>Change Password</span>
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default OfficerChangePasswordPage;
