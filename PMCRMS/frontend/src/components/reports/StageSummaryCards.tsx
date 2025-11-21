import React from "react";
import {
  CheckCircle,
  Clock,
  FileText,
  ChevronRight,
  ArrowLeft,
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
    if (
      stageName.toLowerCase().includes("approved") ||
      stageName.toLowerCase().includes("certificate")
    ) {
      return CheckCircle;
    }
    if (
      stageName.toLowerCase().includes("pending") ||
      stageName.toLowerCase().includes("review")
    ) {
      return Clock;
    }
    return FileText;
  };

  const getStageColor = (stageName: string) => {
    if (
      stageName.toLowerCase().includes("approved") ||
      stageName.toLowerCase().includes("certificate")
    ) {
      return {
        bg: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
        light: "#d1fae5",
      };
    }
    if (stageName.toLowerCase().includes("rejected")) {
      return {
        bg: "linear-gradient(135deg, #ef4444 0%, #dc2626 100%)",
        light: "#fee2e2",
      };
    }
    if (
      stageName.toLowerCase().includes("pending") ||
      stageName.toLowerCase().includes("payment")
    ) {
      return {
        bg: "linear-gradient(135deg, #f59e0b 0%, #d97706 100%)",
        light: "#fef3c7",
      };
    }
    return {
      bg: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
      light: "#dbeafe",
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
              style={{ padding: "24px" }}
            >
              <div className="h-6 bg-gray-200 rounded w-3/4 mb-4"></div>
              <div className="h-10 bg-gray-200 rounded w-1/2"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div>
      {/* Header with back button */}
      <div className="mb-6 flex items-center gap-4">
        <button
          onClick={onBack}
          className="pmc-button pmc-button-secondary flex items-center gap-2"
        >
          <ArrowLeft style={{ width: "16px", height: "16px" }} />
          Back to Positions
        </button>
        <div>
          <h2 style={{ fontSize: "24px", fontWeight: "700", color: "#1f2937" }}>
            {positionName}
          </h2>
          <p style={{ fontSize: "14px", color: "#6b7280", marginTop: "4px" }}>
            Select a stage to view applications
          </p>
        </div>
      </div>

      {stages.length === 0 ? (
        <div
          className="pmc-card"
          style={{ padding: "48px", textAlign: "center" }}
        >
          <FileText
            style={{
              width: "48px",
              height: "48px",
              margin: "0 auto 16px",
              color: "#9ca3af",
            }}
          />
          <h3 style={{ fontSize: "18px", fontWeight: "600", color: "#374151" }}>
            No Stages Found
          </h3>
          <p style={{ color: "#6b7280", marginTop: "8px" }}>
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
                className="pmc-card pmc-card-hover cursor-pointer transition-all duration-200 hover:shadow-lg"
                style={{ padding: "24px" }}
                onClick={() => onStageClick(stage.stageName, displayName)}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-3">
                      <div
                        style={{
                          width: "48px",
                          height: "48px",
                          borderRadius: "12px",
                          background: colors.bg,
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                        }}
                      >
                        <Icon
                          style={{
                            width: "24px",
                            height: "24px",
                            color: "#fff",
                          }}
                        />
                      </div>
                      <div className="flex-1">
                        <h3
                          style={{
                            fontSize: "14px",
                            fontWeight: "600",
                            color: "#1f2937",
                            lineHeight: "1.4",
                          }}
                        >
                          {displayName}
                        </h3>
                      </div>
                    </div>

                    <div className="flex items-baseline gap-2">
                      <span
                        style={{
                          fontSize: "32px",
                          fontWeight: "700",
                          color: "#1f2937",
                        }}
                      >
                        {stage.applicationCount}
                      </span>
                      <span style={{ fontSize: "14px", color: "#9ca3af" }}>
                        {stage.applicationCount === 1
                          ? "application"
                          : "applications"}
                      </span>
                    </div>
                  </div>

                  <div
                    style={{
                      width: "32px",
                      height: "32px",
                      borderRadius: "8px",
                      background: colors.light,
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      transition: "all 0.2s",
                    }}
                  >
                    <ChevronRight
                      style={{
                        width: "20px",
                        height: "20px",
                        color: "#374151",
                      }}
                    />
                  </div>
                </div>

                <div
                  style={{
                    marginTop: "16px",
                    paddingTop: "16px",
                    borderTop: "1px solid #e5e7eb",
                  }}
                >
                  <button
                    className="text-sm font-medium"
                    style={{ color: "#3b82f6" }}
                    onClick={(e) => {
                      e.stopPropagation();
                      onStageClick(stage.stageName, displayName);
                    }}
                  >
                    View Applications →
                  </button>
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
