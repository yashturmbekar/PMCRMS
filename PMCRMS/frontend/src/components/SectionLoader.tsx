import React from "react";
import "../styles/loaders.css";

interface SectionLoaderProps {
  message?: string;
  size?: "small" | "medium" | "large";
  variant?: "default" | "minimal" | "skeleton";
  inline?: boolean;
}

/**
 * Section/Component loader with animated PMC logo
 * Use this for loading sections, tables, cards, or any component-level loading
 */
const SectionLoader: React.FC<SectionLoaderProps> = ({
  message = "Loading...",
  size = "medium",
  variant = "default",
  inline = false,
}) => {
  if (variant === "minimal") {
    return (
      <div
        className={`pmc-section-loader pmc-section-loader-minimal pmc-section-loader-${size} ${
          inline ? "pmc-section-loader-inline" : ""
        }`}
      >
        <div className="pmc-spinner-simple"></div>
        {message && <span className="pmc-loader-text-simple">{message}</span>}
      </div>
    );
  }

  if (variant === "skeleton") {
    return (
      <div className="pmc-skeleton-loader">
        <div className="pmc-skeleton-header"></div>
        <div className="pmc-skeleton-line"></div>
        <div className="pmc-skeleton-line"></div>
        <div className="pmc-skeleton-line pmc-skeleton-line-short"></div>
      </div>
    );
  }

  return (
    <div
      className={`pmc-section-loader pmc-section-loader-${size} ${
        inline ? "pmc-section-loader-inline" : ""
      }`}
    >
      <div className="pmc-section-loader-content">
        {/* Logo with rotating ring */}
        <div className="pmc-section-logo-container">
          <div className="pmc-section-ring"></div>
          <img src="/pmc-logo.png" alt="Loading" className="pmc-section-logo" />
        </div>

        {/* Loading Text */}
        {message && <p className="pmc-section-loader-text">{message}</p>}

        {/* Animated Dots */}
        <div className="pmc-section-loader-dots">
          <span className="pmc-section-dot"></span>
          <span className="pmc-section-dot"></span>
          <span className="pmc-section-dot"></span>
        </div>
      </div>
    </div>
  );
};

export default SectionLoader;
