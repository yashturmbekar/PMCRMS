import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState("");
  const [otp, setOtp] = useState("");
  const [otpSent, setOtpSent] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [countdown, setCountdown] = useState(0);
  const { login } = useAuth();
  const navigate = useNavigate();

  // Countdown timer for resend OTP
  useEffect(() => {
    let timer: number;
    if (countdown > 0) {
      timer = window.setTimeout(() => setCountdown(countdown - 1), 1000);
    }
    return () => window.clearTimeout(timer);
  }, [countdown]);

  const handleSendOTP = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await fetch("http://localhost:5086/api/auth/send-otp", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ identifier: email }),
      });

      if (response.ok) {
        setOtpSent(true);
        setCountdown(300);
        setError("");
      } else {
        const errorData = await response.json();
        setError(errorData.message || "Failed to send OTP");
      }
    } catch {
      setError("Network error. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await fetch(
        "http://localhost:5086/api/auth/verify-otp",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ identifier: email, otp }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        login(data.data.token, data.data.user);
        navigate("/dashboard");
      } else {
        const errorData = await response.json();
        setError(errorData.message || "Invalid OTP");
      }
    } catch {
      setError("Network error. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const handleBackToEmail = () => {
    setOtpSent(false);
    setOtp("");
    setError("");
    setCountdown(0);
  };

  const handleResendOTP = async () => {
    setLoading(true);
    setError("");

    try {
      const response = await fetch("http://localhost:5086/api/auth/send-otp", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ identifier: email }),
      });

      if (response.ok) {
        setCountdown(300);
        setError("");
      } else {
        const errorData = await response.json();
        setError(errorData.message || "Failed to resend OTP");
      }
    } catch {
      setError("Failed to resend OTP");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex">
      <div className="flex-1 bg-gray-100 flex items-center justify-center p-8">
        <div className="text-center max-w-lg">
          <div className="mb-12">
            <div className="w-32 h-32 mx-auto bg-white rounded-full flex items-center justify-center shadow-lg border-4 border-orange-400">
              <img
                src="/pmc-logo.png"
                alt="PMC Logo"
                className="w-20 h-20 object-contain"
                onError={(e) => {
                  const target = e.target as HTMLImageElement;
                  target.style.display = "none";
                  const parent = target.parentElement;
                  if (parent) {
                    parent.innerHTML =
                      '<div class="text-orange-600 font-bold text-2xl">PMC</div>';
                  }
                }}
              />
            </div>
          </div>
          <h1 className="text-4xl font-bold text-gray-800 mb-8 leading-tight">
            PMC Registration Management System
          </h1>
          <p className="text-gray-700 text-lg">
            If you have any questions, feel free to call us at <br />
            <strong className="text-xl">+91 9284341115</strong>.
          </p>
        </div>
      </div>
      <div className="w-96 bg-white shadow-2xl flex flex-col">
        <div className="text-right p-4">
          <span className="text-blue-500 text-sm font-medium cursor-pointer hover:underline">
            LOGOUT
          </span>
        </div>
        <div className="flex-1 p-8 flex flex-col justify-center">
          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-800 mb-2">Email</h2>
          </div>
          {error && (
            <div className="mb-6 p-3 bg-red-50 border border-red-200 rounded-md">
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          )}
          {!otpSent ? (
            <form onSubmit={handleSendOTP} className="space-y-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Email Id <span className="text-red-500">*</span>
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-4 py-3 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors bg-gray-50"
                  placeholder="abhishek@mailinator.com"
                  required
                />
                <p className="text-xs text-gray-500 mt-1">
                  Enter the email address associated with your PMC account
                </p>
              </div>
              <button
                type="submit"
                disabled={loading}
                className="w-full bg-blue-500 hover:bg-blue-600 disabled:bg-blue-300 text-white font-semibold py-3 px-6 rounded-md transition-colors text-lg"
              >
                {loading ? "SENDING OTP..." : "SEND OTP"}
              </button>
            </form>
          ) : (
            <form onSubmit={handleLogin} className="space-y-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Email Id <span className="text-red-500">*</span>
                </label>
                <div className="flex items-center justify-between bg-gray-50 border border-gray-300 rounded-md px-4 py-3">
                  <span className="text-gray-700">{email}</span>
                  <button
                    type="button"
                    onClick={handleBackToEmail}
                    className="text-gray-500 hover:text-gray-700 text-sm border border-gray-300 px-3 py-1 rounded-md hover:bg-gray-100 transition-colors"
                  >
                    Change Email
                  </button>
                </div>
              </div>
              <div>
                <p className="text-sm text-gray-700 mb-4">
                  OTP sent to your email address. Please check!
                </p>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  One Time Password <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={otp}
                  onChange={(e) =>
                    setOtp(e.target.value.replace(/\D/g, "").slice(0, 6))
                  }
                  className="w-full px-4 py-3 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors bg-gray-50 text-center text-xl font-mono tracking-wider"
                  placeholder="Enter OTP"
                  maxLength={6}
                  required
                />
                {countdown > 0 && (
                  <p className="text-xs text-green-600 mt-1">
                    Resend OTP in {countdown} seconds
                  </p>
                )}
              </div>
              <button
                type="submit"
                disabled={loading || otp.length !== 6}
                className="w-full bg-blue-500 hover:bg-blue-600 disabled:bg-blue-300 text-white font-semibold py-3 px-6 rounded-md transition-colors text-lg"
              >
                {loading ? "VERIFYING..." : "SUBMIT"}
              </button>
              {countdown === 0 && (
                <div className="text-center">
                  <button
                    type="button"
                    onClick={handleResendOTP}
                    disabled={loading}
                    className="text-blue-500 hover:text-blue-700 text-sm font-medium hover:underline disabled:text-gray-400"
                  >
                    Resend OTP
                  </button>
                </div>
              )}
            </form>
          )}
        </div>
        <div className="p-6 text-center border-t border-gray-200">
          <p className="text-xs text-gray-500">
            © 2024 Pimpri Chinchwad Municipal Corporation
          </p>
          <p className="text-xs text-gray-400 mt-1">
            All rights reserved. Developed for citizen services.
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
