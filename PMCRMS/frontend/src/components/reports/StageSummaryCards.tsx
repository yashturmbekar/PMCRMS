import React from "react";
import {
  CheckCircle,
  Clock,
  FileText,
  ArrowLeft,
  XCircle,
  AlertCircle,
  FileCheck,
  CreditCard,
  Eye,
} from "lucide-react";
import type { StageSummary } from "../../types/reports";
import { StageDisplayNames } from "../../types/reports";

interface StageSummaryCardsProps {
  stages: StageSummary[];
  positionName: string;
  onStageClick: (stageName: string, stageDisplayName: string) => void;
  onBack: () => void;
  isLoading?: boolean;
}

const StageSummaryCards: React.FC<StageSummaryCardsProps> = ({
  stages,
  positionName,
  onStageClick,
  onBack,
  isLoading = false,
}) => {
  const getStageIcon = (stageName: string) => {
    const lower = stageName.toLowerCase();
    if (lower.includes("approved") || lower.includes("completed")) {
      return CheckCircle;
    }
    if (lower.includes("rejected")) {
      return XCircle;
    }
    if (lower.includes("certificate")) {
      return FileCheck;
    }
    if (lower.includes("payment")) {
      return CreditCard;
    }
    if (lower.includes("review") || lower.includes("verified")) {
      return Eye;
    }
    if (lower.includes("pending")) {
      return Clock;
    }
    if (lower.includes("document")) {
      return FileText;
    }
    return AlertCircle;
  };

  const getStageColor = (stageName: string) => {
    const lower = stageName.toLowerCase();
    if (
      lower.includes("approved") ||
      lower.includes("completed") ||
      lower.includes("certificate")
    ) {
      return {
        primary: "#10b981",
        secondary: "#059669",
        accent: "#34d399",
        light: "linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%)",
        border: "#86efac",
      };
    }
    if (lower.includes("rejected")) {
      return {
        primary: "#ef4444",
        secondary: "#dc2626",
        accent: "#f87171",
        light: "linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)",
        border: "#fca5a5",
      };
    }
    if (lower.includes("payment") || lower.includes("pending")) {
      return {
        primary: "#f59e0b",
        secondary: "#d97706",
        accent: "#fbbf24",
        light: "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
        border: "#fbbf24",
      };
    }
    if (lower.includes("review") || lower.includes("verified")) {
      return {
        primary: "#8b5cf6",
        secondary: "#7c3aed",
        accent: "#a78bfa",
        light: "linear-gradient(135deg, #ede9fe 0%, #ddd6fe 100%)",
        border: "#c4b5fd",
      };
    }
    if (lower.includes("document")) {
      return {
        primary: "#06b6d4",
        secondary: "#0891b2",
        accent: "#22d3ee",
        light: "linear-gradient(135deg, #cffafe 0%, #a5f3fc 100%)",
        border: "#67e8f9",
      };
    }
    return {
      primary: "#3b82f6",
      secondary: "#2563eb",
      accent: "#60a5fa",
      light: "linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)",
      border: "#93c5fd",
    };
  };

  if (isLoading) {
    return (
      <div>
        <div className="mb-6">
          <div className="h-8 bg-gray-200 rounded w-1/3 animate-pulse"></div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              className="pmc-card animate-pulse"
              style={{ padding: "0", overflow: "hidden" }}
            >
              <div style={{ padding: "20px 24px", background: "#f3f4f6" }}>
                <div className="h-6 bg-gray-300 rounded w-3/4"></div>
              </div>
              <div style={{ padding: "24px" }}>
                <div className="h-12 bg-gray-200 rounded mb-4"></div>
                <div className="h-10 bg-gray-200 rounded"></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div>
      {/* Header with back button */}
      <div style={{ marginBottom: "32px" }}>
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            marginBottom: "16px",
          }}
        >
          <div>
            <h2
              style={{
                fontSize: "28px",
                fontWeight: "700",
                color: "#1f2937",
                marginBottom: "4px",
              }}
            >
              {positionName}
            </h2>
            <p style={{ fontSize: "15px", color: "#6b7280" }}>
              Select a stage to view detailed applications
            </p>
          </div>
          <button
            onClick={onBack}
            className="pmc-button pmc-button-secondary"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              padding: "12px 24px",
              fontSize: "15px",
            }}
          >
            <ArrowLeft style={{ width: "20px", height: "20px" }} />
            Back to Positions
          </button>
        </div>
      </div>

      {stages.length === 0 ? (
        <div
          className="pmc-card"
          style={{
            padding: "64px 32px",
            textAlign: "center",
            background: "linear-gradient(135deg, #f8fafc 0%, #e0f2fe 100%)",
          }}
        >
          <FileText
            style={{
              width: "64px",
              height: "64px",
              margin: "0 auto 20px",
              color: "#cbd5e1",
            }}
          />
          <h3
            style={{
              fontSize: "20px",
              fontWeight: "700",
              color: "#1e293b",
              marginBottom: "8px",
            }}
          >
            No Stages Found
          </h3>
          <p style={{ color: "#64748b", fontSize: "14px" }}>
            There are no applications at any stage for this position.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {stages.map((stage) => {
            const Icon = getStageIcon(stage.stageName);
            const colors = getStageColor(stage.stageName);
            const displayName =
              StageDisplayNames[stage.stageName] || stage.stageDisplayName;

            return (
              <div
                key={stage.stageName}
                className="pmc-card cursor-pointer"
                style={{
                  padding: "0",
                  overflow: "hidden",
                  transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                  border: "1px solid #e2e8f0",
                }}
                onClick={() => onStageClick(stage.stageName, displayName)}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-8px)";
                  e.currentTarget.style.boxShadow =
                    "0 20px 40px -12px rgba(0, 0, 0, 0.15)";
                  e.currentTarget.style.borderColor = colors.primary;
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
                  e.currentTarget.style.borderColor = "#e2e8f0";
                }}
              >
                {/* Header with gradient */}
                <div
                  style={{
                    background: `linear-gradient(135deg, ${colors.primary} 0%, ${colors.secondary} 100%)`,
                    padding: "20px 24px",
                    position: "relative",
                    overflow: "hidden",
                  }}
                >
                  <div
                    style={{
                      position: "absolute",
                      top: "-50%",
                      right: "-20%",
                      width: "150%",
                      height: "200%",
                      background:
                        "radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)",
                      pointerEvents: "none",
                    }}
                  />

                  <div style={{ position: "relative", zIndex: 1 }}>
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        marginBottom: "12px",
                      }}
                    >
                      <div
                        style={{
                          width: "56px",
                          height: "56px",
                          borderRadius: "14px",
                          background: "rgba(255, 255, 255, 0.25)",
                          backdropFilter: "blur(10px)",
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          boxShadow: "0 8px 16px rgba(0, 0, 0, 0.1)",
                        }}
                      >
                        <Icon
                          style={{
                            width: "28px",
                            height: "28px",
                            color: "#fff",
                          }}
                        />
                      </div>
                    </div>
                    <h3
                      style={{
                        fontSize: "16px",
                        fontWeight: "700",
                        color: "#fff",
                        lineHeight: "1.4",
                        letterSpacing: "-0.01em",
                      }}
                    >
                      {displayName}
                    </h3>
                  </div>
                </div>

                {/* Body - Simplified */}
                <div style={{ padding: "32px 24px", textAlign: "center" }}>
                  {/* Count Display - Simplified */}
                  <div style={{ marginBottom: "8px" }}>
                    <div
                      style={{
                        fontSize: "64px",
                        fontWeight: "800",
                        background: `linear-gradient(135deg, ${colors.primary} 0%, ${colors.accent} 100%)`,
                        WebkitBackgroundClip: "text",
                        WebkitTextFillColor: "transparent",
                        backgroundClip: "text",
                        lineHeight: "1",
                      }}
                    >
                      {stage.applicationCount}
                    </div>
                  </div>
                  <p
                    style={{
                      fontSize: "15px",
                      color: "#64748b",
                      fontWeight: "600",
                      letterSpacing: "0.02em",
                    }}
                  >
                    {stage.applicationCount === 1
                      ? "Application"
                      : "Applications"}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default StageSummaryCards;
