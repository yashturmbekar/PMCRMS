import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiService } from "../../services/apiService";
import { useAuth } from "../../hooks/useAuth";
import { User, Phone, Building2, CheckCircle, UserCheck } from "lucide-react";

const ProfileSetupPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, refreshUser } = useAuth();
  const [formData, setFormData] = useState({
    name: user?.name || "",
    phoneNumber: "",
    department: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    if (error) setError("");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    // Validation
    if (!formData.name.trim()) {
      setError("Name is required");
      setLoading(false);
      return;
    }

    try {
      const response = await apiService.auth.completeProfile(
        formData.name,
        formData.phoneNumber || undefined,
        formData.department || undefined
      );

      if (response.success) {
        // Update user in localStorage and refresh context
        const updatedUser = { ...user, ...response.data };
        localStorage.setItem("pmcrms_user", JSON.stringify(updatedUser));

        // Refresh user data in context
        if (refreshUser) {
          refreshUser();
        }
        // Navigate to dashboard
        navigate("/dashboard");
      } else {
        throw new Error(response.message || "Failed to complete profile");
      }
    } catch (error: unknown) {
      let errorMessage = "Failed to complete profile. Please try again.";
      if (error instanceof Error) {
        errorMessage = error.message;
      } else if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as {
          response?: { data?: { message?: string } };
        };
        errorMessage =
          axiosError.response?.data?.message ||
          "Failed to complete profile. Please try again.";
      }
      setError(errorMessage);
      console.error("Profile completion error:", errorMessage);
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
                <p className="pmc-auth-subtitle">Complete Your Profile</p>
              </div>
            </div>
          </div>

          {/* Profile Setup Card */}
          <div className="pmc-max-w-2xl pmc-mx-auto pmc-fadeInUp">
            <div className="pmc-auth-card">
              {/* Header */}
              <div className="pmc-p-8 pmc-bg-gradient-to-br pmc-from-emerald-600 pmc-to-teal-700 pmc-text-white">
                <div className="pmc-flex pmc-items-center pmc-gap-4 pmc-mb-4">
                  <div className="pmc-w-16 pmc-h-16 pmc-bg-white/20 pmc-rounded-full pmc-flex pmc-items-center pmc-justify-center">
                    <UserCheck className="pmc-w-8 pmc-h-8" />
                  </div>
                  <div>
                    <h2 className="pmc-text-2xl pmc-font-bold">
                      Complete Your Profile
                    </h2>
                    <p className="pmc-text-emerald-100">
                      Welcome aboard! Just a few more details...
                    </p>
                  </div>
                </div>
                <div className="pmc-bg-white/10 pmc-rounded-lg pmc-p-4">
                  <div className="pmc-flex pmc-items-start pmc-gap-3">
                    <CheckCircle className="pmc-w-5 pmc-h-5 pmc-text-emerald-300 pmc-flex-shrink-0 pmc-mt-0.5" />
                    <div className="pmc-text-sm pmc-text-emerald-50">
                      <p className="pmc-font-semibold pmc-mb-2">
                        Almost There!
                      </p>
                      <p>
                        Your password has been changed successfully. Please
                        complete your profile to start using the system.
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

                {/* Account Information */}
                <div className="pmc-bg-gray-50 pmc-rounded-lg pmc-p-4 pmc-mb-6">
                  <h3 className="pmc-text-sm pmc-font-semibold pmc-text-gray-700 pmc-mb-3">
                    Account Information
                  </h3>
                  <div className="pmc-grid pmc-grid-cols-2 pmc-gap-4">
                    <div>
                      <p className="pmc-text-xs pmc-text-gray-500 pmc-mb-1">
                        Email
                      </p>
                      <p className="pmc-text-sm pmc-font-medium pmc-text-gray-900">
                        {user?.email}
                      </p>
                    </div>
                    <div>
                      <p className="pmc-text-xs pmc-text-gray-500 pmc-mb-1">
                        Role
                      </p>
                      <p className="pmc-text-sm pmc-font-medium pmc-text-gray-900">
                        {user?.role}
                      </p>
                    </div>
                    <div>
                      <p className="pmc-text-xs pmc-text-gray-500 pmc-mb-1">
                        Employee ID
                      </p>
                      <p className="pmc-text-sm pmc-font-medium pmc-text-gray-900">
                        {user?.employeeId}
                      </p>
                    </div>
                  </div>
                </div>

                <form onSubmit={handleSubmit}>
                  {/* Name Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label pmc-label-required">
                      Full Name
                    </label>
                    <div className="pmc-input-with-icon">
                      <input
                        type="text"
                        name="name"
                        value={formData.name}
                        onChange={handleInputChange}
                        placeholder="Enter your full name"
                        required
                      />
                      <div className="pmc-input-icon-left">
                        <User className="pmc-w-5 pmc-h-5" />
                      </div>
                    </div>
                    <p className="pmc-help-text">
                      This will be displayed in the system
                    </p>
                  </div>

                  {/* Phone Number Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label">Phone Number (Optional)</label>
                    <div className="pmc-input-with-icon">
                      <input
                        type="tel"
                        name="phoneNumber"
                        value={formData.phoneNumber}
                        onChange={handleInputChange}
                        placeholder="Enter your contact number"
                        pattern="[0-9]{10}"
                        maxLength={10}
                      />
                      <div className="pmc-input-icon-left">
                        <Phone className="pmc-w-5 pmc-h-5" />
                      </div>
                    </div>
                    <p className="pmc-help-text">
                      10-digit mobile number for communication
                    </p>
                  </div>

                  {/* Department Field */}
                  <div className="pmc-form-group pmc-mb-6">
                    <label className="pmc-label">Department (Optional)</label>
                    <div className="pmc-input-with-icon">
                      <input
                        type="text"
                        name="department"
                        value={formData.department}
                        onChange={handleInputChange}
                        placeholder="Enter your department"
                        maxLength={100}
                      />
                      <div className="pmc-input-icon-left">
                        <Building2 className="pmc-w-5 pmc-h-5" />
                      </div>
                    </div>
                    <p className="pmc-help-text">
                      Your working department within PMC
                    </p>
                  </div>

                  {/* Information Box */}
                  <div className="pmc-bg-blue-50 pmc-border pmc-border-blue-200 pmc-rounded-lg pmc-p-4 pmc-mb-6">
                    <div className="pmc-flex pmc-items-start pmc-gap-3">
                      <svg
                        className="pmc-w-5 pmc-h-5 pmc-text-blue-600 pmc-flex-shrink-0 pmc-mt-0.5"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                        />
                      </svg>
                      <div className="pmc-text-sm pmc-text-blue-800">
                        <p className="pmc-font-semibold pmc-mb-1">
                          Profile Information
                        </p>
                        <p>
                          Your profile information helps us identify you in the
                          system and enables better communication with other
                          team members.
                        </p>
                      </div>
                    </div>
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
                        <span>Complete Profile & Continue</span>
                      </div>
                    )}
                    {loading && <span>Completing Profile...</span>}
                  </button>
                </form>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="pmc-auth-footer pmc-fadeInUp pmc-mt-8">
            <div className="pmc-auth-footer-box">
              <CheckCircle className="pmc-w-5 pmc-h-5" />
              <span className="pmc-auth-footer-text">
                © {new Date().getFullYear()} Pune Municipal Corporation -
                Officer Onboarding
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfileSetupPage;
