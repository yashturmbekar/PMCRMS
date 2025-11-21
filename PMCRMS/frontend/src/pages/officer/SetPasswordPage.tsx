import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Lock, Eye, EyeOff, CheckCircle2, XCircle } from "lucide-react";
import apiClient from "../../services/apiClient";
import type { ApiResponse } from "../../types";

interface SetPasswordRequest {
  token: string;
  password: string;
  confirmPassword: string;
}

interface SetPasswordResponse {
  message: string;
  userId: number;
  email: string;
}

const SetPasswordPage: React.FC = () => {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [validatingToken, setValidatingToken] = useState(true);
  const [tokenValid, setTokenValid] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [invitationDetails, setInvitationDetails] = useState<{
    name: string;
    email: string;
    role: string;
  } | null>(null);

  // Password strength validation
  const [passwordStrength, setPasswordStrength] = useState({
    hasMinLength: false,
    hasUpperCase: false,
    hasLowerCase: false,
    hasNumber: false,
    hasSpecialChar: false,
  });

  useEffect(() => {
    if (token) {
      validateToken();
    } else {
      setValidatingToken(false);
      setError("Invalid invitation link");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  useEffect(() => {
    // Update password strength indicators
    if (password) {
      setPasswordStrength({
        hasMinLength: password.length >= 8,
        hasUpperCase: /[A-Z]/.test(password),
        hasLowerCase: /[a-z]/.test(password),
        hasNumber: /[0-9]/.test(password),
        hasSpecialChar: /[!@#$%^&*(),.?":{}|<>]/.test(password),
      });
    } else {
      setPasswordStrength({
        hasMinLength: false,
        hasUpperCase: false,
        hasLowerCase: false,
        hasNumber: false,
        hasSpecialChar: false,
      });
    }
  }, [password]);

  const validateToken = async () => {
    try {
      setValidatingToken(true);
      const response: ApiResponse<{
        isValid: boolean;
        name?: string;
        email?: string;
        role?: string;
      }> = await apiClient.get(`/Auth/validate-invitation/${token}`);

      if (response.success && response.data?.isValid) {
        setTokenValid(true);
        if (response.data.name && response.data.email && response.data.role) {
          setInvitationDetails({
            name: response.data.name,
            email: response.data.email,
            role: response.data.role,
          });
        }
      } else {
        setTokenValid(false);
        setError(response.message || "Invalid or expired invitation link");
      }
    } catch (err) {
      console.error("Token validation error:", err);
      setTokenValid(false);
      setError("Invalid or expired invitation link");
    } finally {
      setValidatingToken(false);
    }
  };

  const validatePassword = (): boolean => {
    const allValid = Object.values(passwordStrength).every(
      (criteria) => criteria
    );
    if (!allValid) {
      setError("Password does not meet all requirements");
      return false;
    }
    if (password !== confirmPassword) {
      setError("Passwords do not match");
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!validatePassword()) {
      return;
    }

    if (!token) {
      setError("Invalid invitation token");
      return;
    }

    try {
      setLoading(true);
      const response: ApiResponse<SetPasswordResponse> = await apiClient.post(
        "/Auth/set-password",
        {
          token,
          password,
          confirmPassword,
        } as SetPasswordRequest
      );

      if (response.success) {
        setSuccess(true);
        // Redirect to officer login after 3 seconds
        setTimeout(() => {
          navigate("/officer-login", {
            state: {
              message:
                "Password set successfully! Please login with your credentials.",
              email: invitationDetails?.email,
            },
          });
        }, 3000);
      } else {
        setError(response.message || "Failed to set password");
      }
    } catch (err: unknown) {
      console.error("Set password error:", err);
      const error = err as { response?: { data?: { message?: string } } };
      setError(error?.response?.data?.message || "Failed to set password");
    } finally {
      setLoading(false);
    }
  };

  const formatRole = (role: string): string => {
    return role
      .replace(/([A-Z])/g, " $1")
      .replace(/([0-9])/g, " $1")
      .trim();
  };

  // Loading state while validating token
  if (validatingToken) {
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
            <div
              className="spinner"
              style={{
                margin: "0 auto 16px",
                width: "40px",
                height: "40px",
                border: "3px solid #e5e7eb",
                borderTop: "3px solid #667eea",
                borderRadius: "50%",
                animation: "spin 1s linear infinite",
              }}
            />
            <p style={{ color: "#6b7280", fontSize: "16px" }}>
              Validating invitation...
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Invalid token state
  if (!tokenValid) {
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
            <XCircle
              style={{
                width: "64px",
                height: "64px",
                color: "#ef4444",
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
              Invalid Invitation
            </h2>
            <p
              style={{
                color: "#6b7280",
                fontSize: "16px",
                marginBottom: "24px",
              }}
            >
              {error || "This invitation link is invalid or has expired."}
            </p>
            <button
              onClick={() => navigate("/officer-login")}
              className="pmc-button pmc-button-primary"
            >
              Go to Login
            </button>
          </div>
        </div>
      </div>
    );
  }

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
              Password Set Successfully!
            </h2>
            <p
              style={{
                color: "#6b7280",
                fontSize: "16px",
                marginBottom: "24px",
              }}
            >
              Your password has been set. Redirecting to login...
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
      <div className="pmc-card" style={{ maxWidth: "500px", width: "100%" }}>
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
            Set Your Password
          </h2>
          {invitationDetails && (
            <div
              style={{
                marginTop: "16px",
                padding: "12px",
                backgroundColor: "#f3f4f6",
                borderRadius: "8px",
                textAlign: "left",
              }}
            >
              <p
                style={{
                  fontSize: "14px",
                  color: "#374151",
                  marginBottom: "4px",
                }}
              >
                <strong>Name:</strong> {invitationDetails.name}
              </p>
              <p
                style={{
                  fontSize: "14px",
                  color: "#374151",
                  marginBottom: "4px",
                }}
              >
                <strong>Email:</strong> {invitationDetails.email}
              </p>
              <p
                style={{ fontSize: "14px", color: "#374151", marginBottom: 0 }}
              >
                <strong>Role:</strong> {formatRole(invitationDetails.role)}
              </p>
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

          {/* Password Field */}
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
              Password *
            </label>
            <div style={{ position: "relative" }}>
              <Lock
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
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                placeholder="Enter your password"
                style={{
                  width: "100%",
                  padding: "12px 40px 12px 40px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                }}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                style={{
                  position: "absolute",
                  right: "12px",
                  top: "50%",
                  transform: "translateY(-50%)",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  padding: "4px",
                  color: "#9ca3af",
                }}
              >
                {showPassword ? (
                  <EyeOff style={{ width: "18px", height: "18px" }} />
                ) : (
                  <Eye style={{ width: "18px", height: "18px" }} />
                )}
              </button>
            </div>
          </div>

          {/* Confirm Password Field */}
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
              Confirm Password *
            </label>
            <div style={{ position: "relative" }}>
              <Lock
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
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                placeholder="Confirm your password"
                style={{
                  width: "100%",
                  padding: "12px 40px 12px 40px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                }}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                style={{
                  position: "absolute",
                  right: "12px",
                  top: "50%",
                  transform: "translateY(-50%)",
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  padding: "4px",
                  color: "#9ca3af",
                }}
              >
                {showConfirmPassword ? (
                  <EyeOff style={{ width: "18px", height: "18px" }} />
                ) : (
                  <Eye style={{ width: "18px", height: "18px" }} />
                )}
              </button>
            </div>
          </div>

          {/* Password Requirements */}
          {password && (
            <div
              style={{
                padding: "16px",
                backgroundColor: "#f9fafb",
                borderRadius: "8px",
                marginBottom: "24px",
              }}
            >
              <p
                style={{
                  fontSize: "13px",
                  fontWeight: "600",
                  color: "#374151",
                  marginBottom: "12px",
                }}
              >
                Password Requirements:
              </p>
              <div
                style={{ display: "flex", flexDirection: "column", gap: "8px" }}
              >
                <PasswordRequirement
                  met={passwordStrength.hasMinLength}
                  text="At least 8 characters"
                />
                <PasswordRequirement
                  met={passwordStrength.hasUpperCase}
                  text="One uppercase letter"
                />
                <PasswordRequirement
                  met={passwordStrength.hasLowerCase}
                  text="One lowercase letter"
                />
                <PasswordRequirement
                  met={passwordStrength.hasNumber}
                  text="One number"
                />
                <PasswordRequirement
                  met={passwordStrength.hasSpecialChar}
                  text="One special character"
                />
              </div>
            </div>
          )}

          {/* Submit Button */}
          <button
            type="submit"
            disabled={loading}
            className="pmc-button pmc-button-primary"
            style={{ width: "100%", padding: "12px" }}
          >
            {loading ? "Setting Password..." : "Set Password & Continue"}
          </button>
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

// Password Requirement Component
const PasswordRequirement: React.FC<{ met: boolean; text: string }> = ({
  met,
  text,
}) => (
  <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
    {met ? (
      <CheckCircle2
        style={{ width: "16px", height: "16px", color: "#10b981" }}
      />
    ) : (
      <XCircle style={{ width: "16px", height: "16px", color: "#ef4444" }} />
    )}
    <span
      style={{
        fontSize: "13px",
        color: met ? "#10b981" : "#6b7280",
      }}
    >
      {text}
    </span>
  </div>
);

export default SetPasswordPage;
