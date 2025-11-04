import React from "react";

interface ButtonLoaderProps {
  size?: "sm" | "md" | "lg";
  color?: "white" | "primary" | "dark";
}

/**
 * Inline button loader with modern spinner
 * Specifically designed for button loading states
 */
const ButtonLoader: React.FC<ButtonLoaderProps> = ({
  size = "md",
  color = "white",
}) => {
  const sizeClasses = {
    sm: "w-4 h-4 border-2",
    md: "w-5 h-5 border-[3px]",
    lg: "w-6 h-6 border-[3px]",
  };

  const colorClasses = {
    white: "border-white/30 border-t-white",
    primary: "border-blue-200 border-t-blue-600",
    dark: "border-gray-300 border-t-gray-700",
  };

  return (
    <div className="inline-flex items-center justify-center">
      <div
        className={`rounded-full animate-spin ${sizeClasses[size]} ${colorClasses[color]}`}
        style={{
          animation: "spin 0.8s cubic-bezier(0.4, 0, 0.2, 1) infinite",
        }}
      />
    </div>
  );
};

export default ButtonLoader;
