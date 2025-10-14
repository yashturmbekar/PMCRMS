import React from "react";
import "../styles/loaders.css";

interface ModalLoaderProps {
  message?: string;
  isVisible: boolean;
}

/**
 * Modal overlay loader with animated PMC logo
 * Use this for modal/popup loading states
 * Shows a semi-transparent overlay over the entire modal with centered loader
 */
const ModalLoader: React.FC<ModalLoaderProps> = ({
  message = "Processing...",
  isVisible,
}) => {
  if (!isVisible) return null;

  return (
    <div className="pmc-modal-loader-overlay">
      <div className="pmc-modal-loader-content">
        {/* Animated Logo Container */}
        <div className="pmc-logo-container pmc-logo-container-modal">
          {/* Outer rotating ring */}
          <div className="pmc-loader-ring pmc-loader-ring-outer"></div>

          {/* Middle pulsing ring */}
          <div className="pmc-loader-ring pmc-loader-ring-middle"></div>

          {/* Inner rotating ring */}
          <div className="pmc-loader-ring pmc-loader-ring-inner"></div>

          {/* Logo with fade animation */}
          <div className="pmc-logo-wrapper">
            <img
              src="/pmc-logo.png"
              alt="PMC Logo"
              className="pmc-logo-animated"
            />
          </div>
        </div>

        {/* Loading Text */}
        <div className="pmc-loader-text">
          <h3 className="pmc-loader-title pmc-loader-title-modal">{message}</h3>
          <div className="pmc-loader-dots">
            <span className="pmc-loader-dot"></span>
            <span className="pmc-loader-dot"></span>
            <span className="pmc-loader-dot"></span>
          </div>
        </div>

        {/* Progress Bar */}
        <div className="pmc-loader-progress-container pmc-loader-progress-container-modal">
          <div className="pmc-loader-progress-bar"></div>
        </div>
      </div>

      {/* Decorative Elements */}
      <div className="pmc-loader-particles pmc-loader-particles-modal">
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
      </div>
    </div>
  );
};

export default ModalLoader;
