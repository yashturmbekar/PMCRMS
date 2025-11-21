import React from "react";
import { FileText, ChevronRight } from "lucide-react";
import type { PositionSummary } from "../../types/reports";
import { PositionDisplayNames } from "../../types/reports";

interface PositionSummaryCardsProps {
  positions: PositionSummary[];
  onPositionClick: (positionType: string, positionName: string) => void;
  isLoading?: boolean;
}

const PositionSummaryCards: React.FC<PositionSummaryCardsProps> = ({
  positions,
  onPositionClick,
  isLoading = false,
}) => {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[1, 2, 3, 4, 5].map((i) => (
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
    );
  }

  if (positions.length === 0) {
    return (
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
          No Applications Found
        </h3>
        <p style={{ color: "#6b7280", marginTop: "8px" }}>
          There are no applications in the system yet.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {positions.map((position) => (
        <div
          key={position.positionType}
          className="pmc-card pmc-card-hover cursor-pointer transition-all duration-200 hover:shadow-lg"
          style={{ padding: "24px" }}
          onClick={() =>
            onPositionClick(position.positionType, position.positionName)
          }
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-3 mb-3">
                <div
                  style={{
                    width: "48px",
                    height: "48px",
                    borderRadius: "12px",
                    background:
                      "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  <FileText
                    style={{ width: "24px", height: "24px", color: "#fff" }}
                  />
                </div>
                <div>
                  <h3
                    style={{
                      fontSize: "16px",
                      fontWeight: "600",
                      color: "#1f2937",
                      marginBottom: "4px",
                    }}
                  >
                    {PositionDisplayNames[position.positionType] ||
                      position.positionName}
                  </h3>
                  <p style={{ fontSize: "12px", color: "#6b7280" }}>
                    Total Applications
                  </p>
                </div>
              </div>

              <div className="flex items-baseline gap-2">
                <span
                  style={{
                    fontSize: "36px",
                    fontWeight: "700",
                    color: "#667eea",
                  }}
                >
                  {position.totalApplications}
                </span>
                <span style={{ fontSize: "14px", color: "#9ca3af" }}>
                  applications
                </span>
              </div>
            </div>

            <div
              style={{
                width: "32px",
                height: "32px",
                borderRadius: "8px",
                background: "#f3f4f6",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                transition: "all 0.2s",
              }}
              className="hover:bg-blue-50"
            >
              <ChevronRight
                style={{ width: "20px", height: "20px", color: "#667eea" }}
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
              style={{ color: "#667eea" }}
              onClick={(e) => {
                e.stopPropagation();
                onPositionClick(position.positionType, position.positionName);
              }}
            >
              View Details →
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};

export default PositionSummaryCards;
