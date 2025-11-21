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

// All possible stages that should be displayed
const ALL_STAGES = [
  "Draft",
  "Submitted",
  "UnderReviewByJE",
  "ApprovedByJE",
  "RejectedByJE",
  "UnderReviewByAE",
  "ApprovedByAE",
  "RejectedByAE",
  "UnderReviewByEE1",
  "ApprovedByEE1",
  "RejectedByEE1",
  "UnderReviewByCE1",
  "ApprovedByCE1",
  "RejectedByCE1",
  "PaymentPending",
  "PaymentCompleted",
  "UnderProcessingByClerk",
  "ProcessedByClerk",
  "UnderDigitalSignatureByEE2",
  "DigitalSignatureCompletedByEE2",
  "UnderFinalApprovalByCE2",
  "CertificateIssued",
  "Completed",
  "JUNIOR_ENGINEER_PENDING",
  "APPOINTMENT_SCHEDULED",
  "DOCUMENT_VERIFICATION_PENDING",
  "DOCUMENT_VERIFICATION_IN_PROGRESS",
  "DOCUMENT_VERIFICATION_COMPLETED",
  "AWAITING_JE_DIGITAL_SIGNATURE",
  "ASSISTANT_ENGINEER_PENDING",
  "EXECUTIVE_ENGINEER_PENDING",
  "EXECUTIVE_ENGINEER_SIGN_PENDING",
  "CITY_ENGINEER_PENDING",
  "CITY_ENGINEER_SIGN_PENDING",
  "CLERK_PENDING",
  "APPROVED",
  "REJECTED",
];

const StageSummaryCards: React.FC<StageSummaryCardsProps> = ({
  stages,
  positionName,
  onStageClick,
  onBack,
  isLoading = false,
}) => {
  // Create a complete list of stages with counts (0 if not in data)
  const allStagesWithCounts = ALL_STAGES.map((stageName) => {
    const existingStage = stages.find((s) => s.stageName === stageName);
    return {
      stageName,
      stageDisplayName:
        StageDisplayNames[stageName] ||
        stageName.replace(/([A-Z])/g, " $1").trim(),
      applicationCount: existingStage?.applicationCount || 0,
    };
  })
    // Filter out only Draft and Submitted
    // Also filter out stages with 0 count
    .filter(
      (stage) =>
        stage.stageName !== "Draft" &&
        stage.stageName !== "Submitted" &&
        stage.applicationCount > 0
    );

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
        <div style={{ display: "flex", gap: "20px", flexWrap: "wrap" }}>
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              className="pmc-card animate-pulse"
              style={{
                flex: "1 1 calc(16.666% - 17px)",
                minWidth: "140px",
                padding: "0",
                overflow: "hidden",
              }}
            >
              <div style={{ padding: "12px 16px", background: "#f3f4f6" }}>
                <div className="h-4 bg-gray-300 rounded w-3/4"></div>
              </div>
              <div style={{ padding: "16px", textAlign: "center" }}>
                <div className="h-8 bg-gray-200 rounded mb-2"></div>
                <div className="h-3 bg-gray-200 rounded"></div>
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
      <div style={{ marginBottom: "24px" }}>
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
                fontSize: "24px",
                fontWeight: "700",
                color: "#1f2937",
                marginBottom: "4px",
              }}
            >
              {positionName}
            </h2>
            <p style={{ fontSize: "14px", color: "#6b7280" }}>
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
              padding: "10px 20px",
              fontSize: "14px",
            }}
          >
            <ArrowLeft style={{ width: "18px", height: "18px" }} />
            Back to Positions
          </button>
        </div>
      </div>

      <div style={{ display: "flex", gap: "20px", flexWrap: "wrap" }}>
        {allStagesWithCounts.map((stage) => {
          const Icon = getStageIcon(stage.stageName);
          const colors = getStageColor(stage.stageName);
          const displayName = stage.stageDisplayName;

          return (
            <div
              key={stage.stageName}
              className="pmc-card cursor-pointer"
              style={{
                flex: "1 1 calc(16.666% - 17px)",
                minWidth: "140px",
                padding: "0",
                overflow: "hidden",
                transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                border: "1px solid #e2e8f0",
              }}
              onClick={() => onStageClick(stage.stageName, displayName)}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = "translateY(-4px)";
                e.currentTarget.style.boxShadow =
                  "0 12px 24px -8px rgba(0, 0, 0, 0.15)";
                e.currentTarget.style.borderColor = colors.primary;
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = "translateY(0)";
                e.currentTarget.style.boxShadow =
                  "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
                e.currentTarget.style.borderColor = "#e2e8f0";
              }}
            >
              {/* Compact Header with gradient */}
              <div
                style={{
                  background: `linear-gradient(135deg, ${colors.primary} 0%, ${colors.secondary} 100%)`,
                  padding: "12px 16px",
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

                <div
                  style={{
                    position: "relative",
                    zIndex: 1,
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <div
                    style={{
                      width: "32px",
                      height: "32px",
                      borderRadius: "8px",
                      background: "rgba(255, 255, 255, 0.25)",
                      backdropFilter: "blur(10px)",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      flexShrink: 0,
                    }}
                  >
                    <Icon
                      style={{
                        width: "18px",
                        height: "18px",
                        color: "#fff",
                      }}
                    />
                  </div>
                  <h3
                    style={{
                      fontSize: "11px",
                      fontWeight: "700",
                      color: "#fff",
                      lineHeight: "1.3",
                      letterSpacing: "-0.01em",
                      textTransform: "uppercase",
                    }}
                  >
                    {displayName}
                  </h3>
                </div>
              </div>

              {/* Compact Body */}
              <div style={{ padding: "16px 12px", textAlign: "center" }}>
                <div style={{ marginBottom: "4px" }}>
                  <div
                    style={{
                      fontSize: "36px",
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
                    fontSize: "11px",
                    color: "#64748b",
                    fontWeight: "600",
                    letterSpacing: "0.02em",
                  }}
                >
                  {stage.applicationCount === 1 ? "App" : "Apps"}
                </p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default StageSummaryCards;
