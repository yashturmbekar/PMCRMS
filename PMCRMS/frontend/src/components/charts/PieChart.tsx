import React, { useState } from "react";

interface PieChartDataItem {
  label: string;
  value: number;
  color: string;
}

interface PieChartProps {
  data: PieChartDataItem[];
  width?: number;
  height?: number;
  showLegend?: boolean;
}

const PieChart: React.FC<PieChartProps> = ({
  data,
  width = 400,
  height = 400,
  showLegend = true,
}) => {
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);

  // Calculate total
  const total = data.reduce((sum, item) => sum + item.value, 0);

  if (total === 0) {
    return (
      <div
        style={{
          width: `${width}px`,
          height: `${height}px`,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          color: "#9ca3af",
          fontSize: "14px",
        }}
      >
        No data available
      </div>
    );
  }

  // Calculate pie slices
  const radius = Math.min(width, height) / 2 - 40;
  const centerX = width / 2;
  const centerY = height / 2;

  let currentAngle = -90; // Start from top

  const slices = data.map((item, index) => {
    const percentage = (item.value / total) * 100;
    const angle = (percentage / 100) * 360;
    const startAngle = currentAngle;
    const endAngle = currentAngle + angle;

    currentAngle = endAngle;

    // Calculate path for pie slice
    const startRad = (startAngle * Math.PI) / 180;
    const endRad = (endAngle * Math.PI) / 180;

    const x1 = centerX + radius * Math.cos(startRad);
    const y1 = centerY + radius * Math.sin(startRad);
    const x2 = centerX + radius * Math.cos(endRad);
    const y2 = centerY + radius * Math.sin(endRad);

    const largeArcFlag = angle > 180 ? 1 : 0;

    const pathData = [
      `M ${centerX} ${centerY}`,
      `L ${x1} ${y1}`,
      `A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2}`,
      "Z",
    ].join(" ");

    return {
      pathData,
      percentage: percentage.toFixed(1),
      item,
      index,
    };
  });

  return (
    <div style={{ display: "flex", gap: "32px", alignItems: "center" }}>
      {/* Pie Chart SVG */}
      <div style={{ position: "relative" }}>
        <svg width={width} height={height}>
          {slices.map((slice) => (
            <g key={slice.index}>
              <path
                d={slice.pathData}
                fill={slice.item.color}
                stroke="#fff"
                strokeWidth={2}
                style={{
                  cursor: "pointer",
                  opacity:
                    hoveredIndex === null || hoveredIndex === slice.index
                      ? 1
                      : 0.6,
                  transition: "opacity 0.2s, transform 0.2s",
                  transformOrigin: `${centerX}px ${centerY}px`,
                }}
                onMouseEnter={() => setHoveredIndex(slice.index)}
                onMouseLeave={() => setHoveredIndex(null)}
              />
            </g>
          ))}
        </svg>

        {/* Tooltip */}
        {hoveredIndex !== null && (
          <div
            style={{
              position: "absolute",
              top: "50%",
              left: "50%",
              transform: "translate(-50%, -50%)",
              background: "#fff",
              padding: "12px 16px",
              borderRadius: "8px",
              boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
              pointerEvents: "none",
              zIndex: 10,
              minWidth: "160px",
              textAlign: "center",
            }}
          >
            <div
              style={{
                fontWeight: "600",
                fontSize: "14px",
                color: "#1f2937",
                marginBottom: "4px",
              }}
            >
              {slices[hoveredIndex].item.label}
            </div>
            <div
              style={{
                fontSize: "20px",
                fontWeight: "700",
                color: "#3b82f6",
                marginBottom: "4px",
              }}
            >
              {slices[hoveredIndex].item.value}
            </div>
            <div style={{ fontSize: "12px", color: "#6b7280" }}>
              {slices[hoveredIndex].percentage}% of total
            </div>
          </div>
        )}
      </div>

      {/* Legend */}
      {showLegend && (
        <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
          {slices.map((slice) => (
            <div
              key={slice.index}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                cursor: "pointer",
                opacity:
                  hoveredIndex === null || hoveredIndex === slice.index
                    ? 1
                    : 0.6,
                transition: "opacity 0.2s",
              }}
              onMouseEnter={() => setHoveredIndex(slice.index)}
              onMouseLeave={() => setHoveredIndex(null)}
            >
              <div
                style={{
                  width: "16px",
                  height: "16px",
                  borderRadius: "4px",
                  background: slice.item.color,
                  flexShrink: 0,
                }}
              />
              <div>
                <div
                  style={{
                    fontSize: "14px",
                    fontWeight: "500",
                    color: "#374151",
                  }}
                >
                  {slice.item.label}
                </div>
                <div style={{ fontSize: "12px", color: "#6b7280" }}>
                  {slice.item.value} applications
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default PieChart;
