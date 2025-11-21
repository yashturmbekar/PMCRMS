import React from "react";
import { FileText, TrendingUp } from "lucide-react";
import type { PositionSummary } from "../../types/reports";
import { PositionDisplayNames } from "../../types/reports";

interface PositionSummaryCardsProps {
  positions: PositionSummary[];
  onPositionClick: (positionType: string, positionName: string) => void;
  isLoading?: boolean;
}

const positionColors: Record<
  string,
  { primary: string; secondary: string; accent: string }
> = {
  Architect: { primary: "#667eea", secondary: "#764ba2", accent: "#8b9eff" },
  LicenceEngineer: {
    primary: "#f59e0b",
    secondary: "#d97706",
    accent: "#fbbf24",
  },
  StructuralEngineer: {
    primary: "#10b981",
    secondary: "#059669",
    accent: "#34d399",
  },
  Supervisor1: { primary: "#ef4444", secondary: "#dc2626", accent: "#f87171" },
  Supervisor2: { primary: "#8b5cf6", secondary: "#7c3aed", accent: "#a78bfa" },
};

const PositionSummaryCards: React.FC<PositionSummaryCardsProps> = ({
  positions,
  onPositionClick,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          gap: "24px",
          flexWrap: "wrap",
          justifyContent: "space-between",
        }}
      >
        {[1, 2, 3, 4, 5].map((i) => (
          <div
            key={i}
            className="pmc-card animate-pulse"
            style={{
              flex: "1 1 calc(20% - 20px)",
              minWidth: "180px",
              padding: "0",
              overflow: "hidden",
            }}
          >
            <div style={{ padding: "24px" }}>
              <div className="h-6 bg-gray-200 rounded w-3/4 mb-4"></div>
              <div className="h-10 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="h-20 bg-gray-200 rounded"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (positions.length === 0) {
    return (
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
          No Applications Found
        </h3>
        <p style={{ color: "#64748b", fontSize: "14px" }}>
          There are no applications in the system yet.
        </p>
      </div>
    );
  }

  return (
    <div
      style={{
        display: "flex",
        gap: "24px",
        flexWrap: "wrap",
        justifyContent: "space-between",
      }}
    >
      {positions.map((position) => {
        const colors =
          positionColors[position.positionType] || positionColors.Architect;
        const approvalRate =
          position.totalApplications > 0
            ? Math.round(
                (position.approvedCount / position.totalApplications) * 100
              )
            : 0;

        return (
          <div
            key={position.positionType}
            className="pmc-card cursor-pointer"
            style={{
              flex: "1 1 calc(20% - 20px)",
              minWidth: "180px",
              padding: "0",
              overflow: "hidden",
              transition: "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
              border: "1px solid #e2e8f0",
            }}
            onClick={() =>
              onPositionClick(position.positionType, position.positionName)
            }
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
            {/* Header with Gradient */}
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
                    <FileText
                      style={{ width: "28px", height: "28px", color: "#fff" }}
                    />
                  </div>
                  <div
                    style={{
                      padding: "6px 12px",
                      borderRadius: "20px",
                      background: "rgba(255, 255, 255, 0.25)",
                      backdropFilter: "blur(10px)",
                      fontSize: "12px",
                      fontWeight: "700",
                      color: "#fff",
                      display: "flex",
                      alignItems: "center",
                      gap: "4px",
                    }}
                  >
                    <TrendingUp style={{ width: "14px", height: "14px" }} />
                    {approvalRate}% Approved
                  </div>
                </div>
                <h3
                  style={{
                    fontSize: "18px",
                    fontWeight: "700",
                    color: "#fff",
                    marginBottom: "4px",
                    letterSpacing: "-0.01em",
                  }}
                >
                  {PositionDisplayNames[position.positionType] ||
                    position.positionName}
                </h3>
                <p
                  style={{
                    fontSize: "13px",
                    color: "rgba(255,255,255,0.9)",
                    fontWeight: "500",
                  }}
                >
                  Position Applications
                </p>
              </div>
            </div>

            {/* Body */}
            <div style={{ padding: "32px 24px", textAlign: "center" }}>
              {/* Main Count - Simplified */}
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
                  {position.totalApplications}
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
                Total Applications
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
};

export default PositionSummaryCards;
