import React from "react";

interface SkeletonProps {
  variant?: "text" | "circular" | "rectangular" | "rounded";
  width?: string | number;
  height?: string | number;
  className?: string;
  count?: number;
  animation?: "pulse" | "wave" | "none";
}

/**
 * Modern Skeleton Loader Component
 * Professional content placeholder for better UX
 */
const Skeleton: React.FC<SkeletonProps> = ({
  variant = "text",
  width,
  height,
  className = "",
  count = 1,
  animation = "wave",
}) => {
  const baseClasses =
    "bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 bg-[length:200%_100%]";

  const animationClasses = {
    wave: "animate-[skeleton-wave_2s_cubic-bezier(0.4,0,0.2,1)_infinite]",
    pulse: "animate-pulse",
    none: "",
  };

  const variantClasses = {
    text: "rounded h-4",
    circular: "rounded-full",
    rectangular: "rounded-none",
    rounded: "rounded-lg",
  };

  const variantDimensions = {
    text: { width: width || "100%", height: height || "1rem" },
    circular: { width: width || "40px", height: height || width || "40px" },
    rectangular: { width: width || "100%", height: height || "120px" },
    rounded: { width: width || "100%", height: height || "120px" },
  };

  const dimensions = {
    width: width || variantDimensions[variant].width,
    height: height || variantDimensions[variant].height,
  };

  const skeletonStyle: React.CSSProperties = {
    width:
      typeof dimensions.width === "number"
        ? `${dimensions.width}px`
        : dimensions.width,
    height:
      typeof dimensions.height === "number"
        ? `${dimensions.height}px`
        : dimensions.height,
  };

  const skeletons = Array.from({ length: count }, (_, index) => (
    <div
      key={index}
      className={`${baseClasses} ${variantClasses[variant]} ${animationClasses[animation]} ${className}`}
      style={skeletonStyle}
      role="status"
      aria-label="Loading"
    >
      <span className="sr-only">Loading...</span>
    </div>
  ));

  return <>{skeletons}</>;
};

// Specialized skeleton components
export const SkeletonText: React.FC<Omit<SkeletonProps, "variant">> = (
  props
) => <Skeleton variant="text" {...props} />;

export const SkeletonCircle: React.FC<Omit<SkeletonProps, "variant">> = (
  props
) => <Skeleton variant="circular" {...props} />;

export const SkeletonCard: React.FC<{ className?: string }> = ({
  className = "",
}) => (
  <div
    className={`bg-white rounded-xl p-6 shadow-sm border border-gray-200 ${className}`}
  >
    <div className="flex items-center gap-4 mb-4">
      <Skeleton variant="circular" width={48} height={48} />
      <div className="flex-1 space-y-2">
        <Skeleton variant="text" width="60%" height={20} />
        <Skeleton variant="text" width="40%" height={16} />
      </div>
    </div>
    <div className="space-y-3">
      <Skeleton variant="text" width="100%" height={16} />
      <Skeleton variant="text" width="90%" height={16} />
      <Skeleton variant="text" width="75%" height={16} />
    </div>
  </div>
);

export const SkeletonTable: React.FC<{ rows?: number; columns?: number }> = ({
  rows = 5,
  columns = 4,
}) => (
  <div className="bg-white rounded-xl overflow-hidden shadow-sm border border-gray-200">
    {/* Header */}
    <div className="bg-gray-50 p-4 border-b border-gray-200">
      <div
        className="grid gap-4"
        style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
      >
        {Array.from({ length: columns }, (_, i) => (
          <Skeleton key={i} variant="text" width="80%" height={16} />
        ))}
      </div>
    </div>
    {/* Rows */}
    <div className="divide-y divide-gray-200">
      {Array.from({ length: rows }, (_, rowIndex) => (
        <div key={rowIndex} className="p-4">
          <div
            className="grid gap-4"
            style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
          >
            {Array.from({ length: columns }, (_, colIndex) => (
              <Skeleton key={colIndex} variant="text" width="90%" height={14} />
            ))}
          </div>
        </div>
      ))}
    </div>
  </div>
);

export const SkeletonForm: React.FC<{ fields?: number }> = ({ fields = 4 }) => (
  <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200 space-y-5">
    {Array.from({ length: fields }, (_, index) => (
      <div key={index} className="space-y-2">
        <Skeleton variant="text" width="30%" height={16} />
        <Skeleton variant="rounded" width="100%" height={44} />
      </div>
    ))}
  </div>
);

export default Skeleton;
