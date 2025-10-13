import React, { useRef, useState, useEffect } from "react";
import type { KeyboardEvent, ClipboardEvent } from "react";

interface OtpInputProps {
  length?: number;
  value: string;
  onChange: (otp: string) => void;
  disabled?: boolean;
  error?: boolean;
}

const OtpInput: React.FC<OtpInputProps> = ({
  length = 6,
  value,
  onChange,
  disabled = false,
  error = false,
}) => {
  const [otp, setOtp] = useState<string[]>(Array(length).fill(""));
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Initialize otp state from value prop
  useEffect(() => {
    if (value) {
      const otpArray = value.split("").slice(0, length);
      setOtp([...otpArray, ...Array(length - otpArray.length).fill("")]);
    } else {
      setOtp(Array(length).fill(""));
    }
  }, [value, length]);

  const handleChange = (index: number, digit: string) => {
    // Only allow numbers
    if (digit && !/^\d$/.test(digit)) return;

    const newOtp = [...otp];
    newOtp[index] = digit;
    setOtp(newOtp);

    // Call onChange with the complete OTP
    onChange(newOtp.join(""));

    // Auto-focus next input
    if (digit && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (index: number, e: KeyboardEvent<HTMLInputElement>) => {
    // Handle backspace
    if (e.key === "Backspace") {
      if (!otp[index] && index > 0) {
        // If current input is empty, focus previous and clear it
        const newOtp = [...otp];
        newOtp[index - 1] = "";
        setOtp(newOtp);
        onChange(newOtp.join(""));
        inputRefs.current[index - 1]?.focus();
      } else {
        // Clear current input
        const newOtp = [...otp];
        newOtp[index] = "";
        setOtp(newOtp);
        onChange(newOtp.join(""));
      }
    }
    // Handle left arrow
    else if (e.key === "ArrowLeft" && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
    // Handle right arrow
    else if (e.key === "ArrowRight" && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handlePaste = (e: ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData("text/plain");

    // Extract only digits from pasted data
    const digits = pastedData.replace(/\D/g, "").slice(0, length);

    if (digits) {
      const newOtp = digits.split("");
      // Fill remaining slots with empty strings
      while (newOtp.length < length) {
        newOtp.push("");
      }
      setOtp(newOtp);
      onChange(newOtp.join(""));

      // Focus the last filled input or the first empty one
      const lastFilledIndex = Math.min(digits.length - 1, length - 1);
      inputRefs.current[lastFilledIndex]?.focus();
    }
  };

  const handleFocus = (index: number) => {
    // Select the input content on focus
    inputRefs.current[index]?.select();
  };

  return (
    <div className="pmc-otp-input-container">
      <div className="pmc-otp-input-wrapper">
        {otp.map((digit, index) => (
          <input
            key={index}
            ref={(el) => {
              inputRefs.current[index] = el;
            }}
            type="text"
            inputMode="numeric"
            pattern="\d*"
            maxLength={1}
            value={digit}
            onChange={(e) => handleChange(index, e.target.value)}
            onKeyDown={(e) => handleKeyDown(index, e)}
            onPaste={handlePaste}
            onFocus={() => handleFocus(index)}
            disabled={disabled}
            className={`pmc-otp-input ${error ? "pmc-otp-input-error" : ""} ${
              disabled ? "pmc-otp-input-disabled" : ""
            }`}
            aria-label={`OTP digit ${index + 1}`}
          />
        ))}
      </div>
      <style>{`
        .pmc-otp-input-container {
          width: 100%;
        }

        .pmc-otp-input-wrapper {
          display: flex;
          gap: 12px;
          justify-content: center;
          align-items: center;
          margin: 0 auto;
          max-width: 420px;
        }

        .pmc-otp-input {
          width: 56px;
          height: 64px;
          font-size: 24px;
          font-weight: 600;
          text-align: center;
          border: 2px solid #e5e7eb;
          border-radius: 12px;
          background: #ffffff;
          color: #1f2937;
          transition: all 0.2s ease;
          outline: none;
          caret-color: #4f46e5;
          font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
        }

        .pmc-otp-input:hover:not(:disabled) {
          border-color: #d1d5db;
          box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
        }

        .pmc-otp-input:focus {
          border-color: #4f46e5;
          box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1),
                      0 2px 8px rgba(79, 70, 229, 0.15);
          transform: translateY(-2px);
        }

        .pmc-otp-input:not(:placeholder-shown) {
          border-color: #10b981;
          background: linear-gradient(135deg, #ffffff 0%, #f0fdf4 100%);
        }

        .pmc-otp-input-error {
          border-color: #ef4444 !important;
          background: linear-gradient(135deg, #ffffff 0%, #fef2f2 100%);
          animation: shake 0.4s ease;
        }

        .pmc-otp-input-error:focus {
          border-color: #dc2626 !important;
          box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1),
                      0 2px 8px rgba(239, 68, 68, 0.15);
        }

        .pmc-otp-input-disabled {
          background: #f9fafb;
          color: #9ca3af;
          cursor: not-allowed;
          border-color: #e5e7eb;
        }

        @keyframes shake {
          0%, 100% {
            transform: translateX(0);
          }
          10%, 30%, 50%, 70%, 90% {
            transform: translateX(-4px);
          }
          20%, 40%, 60%, 80% {
            transform: translateX(4px);
          }
        }

        /* Responsive design */
        @media (max-width: 640px) {
          .pmc-otp-input-wrapper {
            gap: 8px;
          }

          .pmc-otp-input {
            width: 44px;
            height: 52px;
            font-size: 20px;
          }
        }

        @media (max-width: 400px) {
          .pmc-otp-input-wrapper {
            gap: 6px;
          }

          .pmc-otp-input {
            width: 38px;
            height: 48px;
            font-size: 18px;
            border-radius: 8px;
          }
        }

        /* Remove number input arrows */
        .pmc-otp-input::-webkit-outer-spin-button,
        .pmc-otp-input::-webkit-inner-spin-button {
          -webkit-appearance: none;
          margin: 0;
        }

        .pmc-otp-input[type="number"] {
          -moz-appearance: textfield;
        }
      `}</style>
    </div>
  );
};

export default OtpInput;
