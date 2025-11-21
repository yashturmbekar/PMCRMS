import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Phone, MapPin, Building2, CheckCircle2 } from "lucide-react";
import apiClient from "../../services/apiClient";
import type { ApiResponse } from "../../types";
import { useAuth } from "../../hooks/useAuth";

interface ProfileSetupRequest {
  phoneNumber: string;
  address?: string;
  department?: string;
}

interface UserProfile {
  id: number;
  name: string;
  email: string;
  role: string;
  employeeId: string;
  phoneNumber?: string;
  address?: string;
  department?: string;
  isProfileComplete: boolean;
}

const OfficerProfileSetupPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [department, setDepartment] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [profileData, setProfileData] = useState<UserProfile | null>(null);

  useEffect(() => {
    // Check if user is logged in and profile needs setup
    if (!user) {
      navigate("/officer-login");
      return;
    }

    loadProfileData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user, navigate]);

  const loadProfileData = async () => {
    try {
      const response: ApiResponse<UserProfile> = await apiClient.get(
        "/Officer/profile"
      );

      if (response.success && response.data) {
        setProfileData(response.data);

        // Pre-fill existing data if any
        if (response.data.phoneNumber)
          setPhoneNumber(response.data.phoneNumber);
        if (response.data.address) setAddress(response.data.address);
        if (response.data.department) setDepartment(response.data.department);

        // If profile is already complete, redirect to dashboard
        if (response.data.isProfileComplete) {
          const dashboardPath = getDashboardPath(response.data.role);
          navigate(dashboardPath);
        }
      }
    } catch (err) {
      console.error("Error loading profile:", err);
      setError("Failed to load profile data");
    }
  };

  const getDashboardPath = (role: string): string => {
    const roleDashboardMap: Record<string, string> = {
      JuniorArchitect: "/junior-architect/dashboard",
      AssistantArchitect: "/assistant-architect/dashboard",
      JuniorLicenceEngineer: "/junior-licence-engineer/dashboard",
      AssistantLicenceEngineer: "/assistant-licence-engineer/dashboard",
      JuniorStructuralEngineer: "/junior-structural-engineer/dashboard",
      AssistantStructuralEngineer: "/assistant-structural-engineer/dashboard",
      JuniorSupervisor1: "/junior-supervisor1/dashboard",
      AssistantSupervisor1: "/assistant-supervisor1/dashboard",
      JuniorSupervisor2: "/junior-supervisor2/dashboard",
      AssistantSupervisor2: "/assistant-supervisor2/dashboard",
      ExecutiveEngineer: "/executive-engineer/dashboard",
      CityEngineer: "/city-engineer/dashboard",
      Clerk: "/clerk/dashboard",
    };
    return roleDashboardMap[role] || "/";
  };

  const formatRole = (role: string): string => {
    return role
      .replace(/([A-Z])/g, " $1")
      .replace(/([0-9])/g, " $1")
      .trim();
  };

  const validatePhoneNumber = (phone: string): boolean => {
    const phoneRegex = /^[0-9]{10}$/;
    return phoneRegex.test(phone);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    // Validate phone number
    if (!validatePhoneNumber(phoneNumber)) {
      setError("Please enter a valid 10-digit phone number");
      return;
    }

    try {
      setLoading(true);
      const response: ApiResponse<void> = await apiClient.put(
        "/Officer/complete-profile",
        {
          phoneNumber,
          address: address || undefined,
          department: department || undefined,
        } as ProfileSetupRequest
      );

      if (response.success) {
        setSuccess(true);
        // Redirect to appropriate dashboard after 2 seconds
        setTimeout(() => {
          if (profileData) {
            const dashboardPath = getDashboardPath(profileData.role);
            navigate(dashboardPath);
          }
        }, 2000);
      } else {
        setError(response.message || "Failed to update profile");
      }
    } catch (err: unknown) {
      console.error("Profile setup error:", err);
      const error = err as { response?: { data?: { message?: string } } };
      setError(error?.response?.data?.message || "Failed to update profile");
    } finally {
      setLoading(false);
    }
  };

  const handleSkip = () => {
    if (profileData) {
      const dashboardPath = getDashboardPath(profileData.role);
      navigate(dashboardPath, {
        state: {
          message: "You can complete your profile later from settings.",
        },
      });
    }
  };

  // Success state
  if (success) {
    return (
      <div
        style={{
          minHeight: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
        }}
      >
        <div className="pmc-card" style={{ maxWidth: "450px", width: "100%" }}>
          <div style={{ padding: "32px", textAlign: "center" }}>
            <CheckCircle2
              style={{
                width: "64px",
                height: "64px",
                color: "#10b981",
                margin: "0 auto 16px",
              }}
            />
            <h2
              style={{
                fontSize: "24px",
                fontWeight: "700",
                color: "#111827",
                marginBottom: "8px",
              }}
            >
              Profile Setup Complete!
            </h2>
            <p
              style={{
                color: "#6b7280",
                fontSize: "16px",
                marginBottom: "24px",
              }}
            >
              Your profile has been updated. Redirecting to dashboard...
            </p>
            <div
              className="spinner"
              style={{
                margin: "0 auto",
                width: "32px",
                height: "32px",
                border: "3px solid #e5e7eb",
                borderTop: "3px solid #667eea",
                borderRadius: "50%",
                animation: "spin 1s linear infinite",
              }}
            />
          </div>
        </div>
      </div>
    );
  }

  // Main form
  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
        padding: "20px",
      }}
    >
      <div className="pmc-card" style={{ maxWidth: "600px", width: "100%" }}>
        {/* Header */}
        <div
          style={{
            padding: "32px 32px 24px",
            textAlign: "center",
            borderBottom: "1px solid #e5e7eb",
          }}
        >
          <img
            src="/pmc-logo.png"
            alt="PMC Logo"
            style={{ height: "60px", marginBottom: "16px" }}
          />
          <h2
            style={{
              fontSize: "24px",
              fontWeight: "700",
              color: "#111827",
              marginBottom: "8px",
            }}
          >
            Complete Your Profile
          </h2>
          <p style={{ color: "#6b7280", fontSize: "14px" }}>
            Please provide your contact information to complete your profile
            setup
          </p>

          {profileData && (
            <div
              style={{
                marginTop: "16px",
                padding: "16px",
                backgroundColor: "#f3f4f6",
                borderRadius: "8px",
                textAlign: "left",
              }}
            >
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "1fr 1fr",
                  gap: "12px",
                }}
              >
                <div>
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#6b7280",
                      marginBottom: "4px",
                    }}
                  >
                    Name
                  </p>
                  <p
                    style={{
                      fontSize: "14px",
                      color: "#111827",
                      fontWeight: "600",
                      marginBottom: 0,
                    }}
                  >
                    {profileData.name}
                  </p>
                </div>
                <div>
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#6b7280",
                      marginBottom: "4px",
                    }}
                  >
                    Employee ID
                  </p>
                  <p
                    style={{
                      fontSize: "14px",
                      color: "#111827",
                      fontWeight: "600",
                      marginBottom: 0,
                    }}
                  >
                    {profileData.employeeId}
                  </p>
                </div>
                <div>
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#6b7280",
                      marginBottom: "4px",
                    }}
                  >
                    Email
                  </p>
                  <p
                    style={{
                      fontSize: "14px",
                      color: "#111827",
                      fontWeight: "600",
                      marginBottom: 0,
                    }}
                  >
                    {profileData.email}
                  </p>
                </div>
                <div>
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#6b7280",
                      marginBottom: "4px",
                    }}
                  >
                    Role
                  </p>
                  <p
                    style={{
                      fontSize: "14px",
                      color: "#111827",
                      fontWeight: "600",
                      marginBottom: 0,
                    }}
                  >
                    {formatRole(profileData.role)}
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} style={{ padding: "32px" }}>
          {/* Error Message */}
          {error && (
            <div
              style={{
                padding: "12px",
                backgroundColor: "#fee2e2",
                border: "1px solid #fecaca",
                borderRadius: "6px",
                color: "#991b1b",
                fontSize: "14px",
                marginBottom: "20px",
              }}
            >
              {error}
            </div>
          )}

          {/* Phone Number Field */}
          <div style={{ marginBottom: "20px" }}>
            <label
              style={{
                display: "block",
                marginBottom: "8px",
                fontWeight: "600",
                color: "#374151",
                fontSize: "14px",
              }}
            >
              Phone Number *
            </label>
            <div style={{ position: "relative" }}>
              <Phone
                style={{
                  position: "absolute",
                  left: "12px",
                  top: "50%",
                  transform: "translateY(-50%)",
                  color: "#9ca3af",
                  width: "18px",
                  height: "18px",
                }}
              />
              <input
                type="tel"
                value={phoneNumber}
                onChange={(e) =>
                  setPhoneNumber(e.target.value.replace(/\D/g, ""))
                }
                maxLength={10}
                required
                placeholder="Enter 10-digit phone number"
                style={{
                  width: "100%",
                  padding: "12px 12px 12px 40px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                }}
              />
            </div>
            <p style={{ fontSize: "12px", color: "#6b7280", marginTop: "4px" }}>
              Enter your 10-digit mobile number
            </p>
          </div>

          {/* Department Field */}
          <div style={{ marginBottom: "20px" }}>
            <label
              style={{
                display: "block",
                marginBottom: "8px",
                fontWeight: "600",
                color: "#374151",
                fontSize: "14px",
              }}
            >
              Department (Optional)
            </label>
            <div style={{ position: "relative" }}>
              <Building2
                style={{
                  position: "absolute",
                  left: "12px",
                  top: "50%",
                  transform: "translateY(-50%)",
                  color: "#9ca3af",
                  width: "18px",
                  height: "18px",
                }}
              />
              <input
                type="text"
                value={department}
                onChange={(e) => setDepartment(e.target.value)}
                placeholder="e.g., Town Planning Department"
                style={{
                  width: "100%",
                  padding: "12px 12px 12px 40px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                }}
              />
            </div>
          </div>

          {/* Address Field */}
          <div style={{ marginBottom: "24px" }}>
            <label
              style={{
                display: "block",
                marginBottom: "8px",
                fontWeight: "600",
                color: "#374151",
                fontSize: "14px",
              }}
            >
              Address (Optional)
            </label>
            <div style={{ position: "relative" }}>
              <MapPin
                style={{
                  position: "absolute",
                  left: "12px",
                  top: "12px",
                  color: "#9ca3af",
                  width: "18px",
                  height: "18px",
                }}
              />
              <textarea
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                placeholder="Enter your full address"
                rows={3}
                style={{
                  width: "100%",
                  padding: "12px 12px 12px 40px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                  resize: "vertical",
                }}
              />
            </div>
          </div>

          {/* Action Buttons */}
          <div
            style={{
              display: "flex",
              gap: "12px",
            }}
          >
            <button
              type="button"
              onClick={handleSkip}
              className="pmc-button pmc-button-secondary"
              style={{ flex: 1, padding: "12px" }}
              disabled={loading}
            >
              Skip for Now
            </button>
            <button
              type="submit"
              disabled={loading}
              className="pmc-button pmc-button-primary"
              style={{ flex: 1, padding: "12px" }}
            >
              {loading ? "Saving..." : "Complete Setup"}
            </button>
          </div>

          {/* Info Note */}
          <div
            style={{
              marginTop: "20px",
              padding: "12px",
              backgroundColor: "#eff6ff",
              border: "1px solid #bfdbfe",
              borderRadius: "6px",
            }}
          >
            <p
              style={{
                fontSize: "13px",
                color: "#1e40af",
                marginBottom: 0,
                lineHeight: "1.5",
              }}
            >
              <strong>Note:</strong> You can update your profile information
              anytime from your account settings.
            </p>
          </div>
        </form>

        {/* Footer */}
        <div
          style={{
            padding: "16px",
            textAlign: "center",
            borderTop: "1px solid #e5e7eb",
            fontSize: "14px",
            color: "#6b7280",
          }}
        >
          © {new Date().getFullYear()} Pune Municipal Corporation
        </div>
      </div>
    </div>
  );
};

export default OfficerProfileSetupPage;
