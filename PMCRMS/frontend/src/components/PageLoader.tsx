import React from "react";
import "../styles/loaders.css";

interface PageLoaderProps {
  message?: string;
  fullScreen?: boolean;
}

/**
 * Full-page loader with animated PMC logo
 * Use this for initial page loads, route transitions, or full-page loading states
 */
const PageLoader: React.FC<PageLoaderProps> = ({
  message = "Loading...",
  fullScreen = true,
}) => {
  return (
    <div
      className={`pmc-page-loader ${
        fullScreen ? "pmc-page-loader-fullscreen" : ""
      }`}
    >
      <div className="pmc-page-loader-content">
        {/* Animated Logo Container */}
        <div className="pmc-logo-container">
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
          <h2 className="pmc-loader-title">{message}</h2>
          <div className="pmc-loader-dots">
            <span className="pmc-loader-dot"></span>
            <span className="pmc-loader-dot"></span>
            <span className="pmc-loader-dot"></span>
          </div>
        </div>

        {/* Progress Bar */}
        <div className="pmc-loader-progress-container">
          <div className="pmc-loader-progress-bar"></div>
        </div>
      </div>

      {/* Decorative Elements */}
      <div className="pmc-loader-particles">
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
        <div className="pmc-particle"></div>
      </div>
    </div>
  );
};

export default PageLoader;
