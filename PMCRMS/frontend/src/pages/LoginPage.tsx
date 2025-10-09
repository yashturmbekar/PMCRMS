import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

export const LoginPage: React.FC = () => {
  const [email, setEmail] = useState("admin@ezyconstruction.com");
  const [otp, setOtp] = useState("");
  const [isOtpSent, setIsOtpSent] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>("");

  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      // Simulate OTP verification
      if (otp === "123456") {
        navigate("/dashboard");
      } else {
        setError("Invalid OTP. Please try again.");
      }
    } catch (err) {
      setError("Login failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleChangeEmail = () => {
    setIsOtpSent(false);
  };

  const handleSendOtp = () => {
    setIsOtpSent(true);
  };

  return (
    <div className="min-h-screen bg-white">
      <div className="min-h-screen flex">
        {/* Left Side - PMC Branding */}
        <div className="flex-1 bg-gray-100 flex flex-col justify-center px-8">
          <div className="max-w-md">
            <div className="mb-8">
              <img
                src="/pmc-logo.png"
                alt="PMC Logo"
                className="h-24 w-auto"
                onError={(e) => {
                  const target = e.target as HTMLImageElement;
                  target.style.display = "none";
                  target.parentElement!.innerHTML = `
                    <div class="h-24 w-32 bg-orange-200 rounded-lg flex items-center justify-center border-2 border-orange-300">
                      <span class="text-orange-800 font-bold text-sm">PMC LOGO</span>
                    </div>
                  `;
                }}
              />
            </div>
            <h1 className="text-xl font-bold text-gray-900 mb-4">
              PMC Registration Management System
            </h1>
            <p className="text-gray-600 mb-2">
              If you have any questions, feel free to call us at +91
            </p>
            <p className="text-gray-600 font-semibold">9284334115.</p>
          </div>
        </div>

        {/* Right Side - Email/OTP Form */}
        <div className="flex-1 flex flex-col justify-center px-8">
          <div className="max-w-md w-full mx-auto bg-gray-50 p-8 rounded-lg">
            <form onSubmit={handleSubmit}>
              {error && (
                <div className="bg-red-50 border border-red-200 p-3 rounded-md mb-4">
                  <div className="text-sm text-red-700">{error}</div>
                </div>
              )}

              {/* Email Section */}
              <div className="mb-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">
                  Email
                </h3>
                <div className="mb-4">
                  <label className="block text-sm text-gray-600 mb-2">
                    Email id *
                  </label>
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                    disabled={isOtpSent}
                  />
                </div>
                {isOtpSent ? (
                  <button
                    type="button"
                    onClick={handleChangeEmail}
                    className="bg-gray-200 text-gray-700 px-4 py-2 rounded text-sm hover:bg-gray-300"
                  >
                    Change Email
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={handleSendOtp}
                    className="bg-blue-500 text-white px-4 py-2 rounded text-sm hover:bg-blue-600"
                  >
                    Send OTP
                  </button>
                )}
              </div>

              {/* OTP Section */}
              {isOtpSent && (
                <div className="mb-6">
                  <p className="text-sm text-gray-600 mb-4">
                    OTP sent to your email address. Please check!
                  </p>
                  <div className="mb-4">
                    <label className="block text-sm text-gray-600 mb-2">
                      One Time Password *
                    </label>
                    <input
                      type="text"
                      value={otp}
                      onChange={(e) => setOtp(e.target.value)}
                      placeholder="Enter OTP"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Resend OTP in 60 seconds
                    </p>
                  </div>
                </div>
              )}

              {/* Submit Button */}
              {isOtpSent && (
                <button
                  type="submit"
                  disabled={isLoading || !otp}
                  className="w-full bg-blue-500 text-white py-3 rounded-md text-lg font-medium hover:bg-blue-600 disabled:opacity-50"
                >
                  {isLoading ? "SUBMITTING..." : "SUBMIT"}
                </button>
              )}
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};
