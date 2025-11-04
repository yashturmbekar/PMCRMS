import React from "react";

interface SpinnerProps {
  size?: "xs" | "sm" | "md" | "lg" | "xl";
  color?: "primary" | "secondary" | "white" | "accent";
  className?: string;
}

/**
 * Modern, professional spinner component
 * Can be used inline in buttons or standalone
 */
const Spinner: React.FC<SpinnerProps> = ({
  size = "md",
  color = "primary",
  className = "",
}) => {
  const sizes = {
    xs: "w-3 h-3 border-2",
    sm: "w-4 h-4 border-2",
    md: "w-6 h-6 border-2",
    lg: "w-8 h-8 border-3",
    xl: "w-12 h-12 border-4",
  };

  const colors = {
    primary: "border-pmc-primary border-t-pmc-primary-light",
    secondary: "border-gray-300 border-t-gray-600",
    white: "border-white/30 border-t-white",
    accent: "border-pmc-accent border-t-pmc-accent-dark",
  };

  return (
    <div
      className={`inline-block rounded-full border-solid border-t-solid animate-spin ${sizes[size]} ${colors[color]} ${className}`}
      style={{
        borderTopWidth: "inherit",
        animation: "spin 0.8s cubic-bezier(0.4, 0, 0.2, 1) infinite",
      }}
      role="status"
      aria-label="Loading"
    >
      <span className="sr-only">Loading...</span>
    </div>
  );
};

export default Spinner;
