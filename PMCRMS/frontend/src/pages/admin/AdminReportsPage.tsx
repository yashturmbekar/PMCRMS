import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  BarChart3,
  AlertCircle,
  ArrowLeft,
  Home,
  ChevronRight,
} from "lucide-react";
import { PageLoader } from "../../components";
import PositionSummaryCards from "../../components/reports/PositionSummaryCards";
import StageSummaryCards from "../../components/reports/StageSummaryCards";
import ApplicationsTable from "../../components/reports/ApplicationsTable";
import PieChart from "../../components/charts/PieChart";
import { reportService } from "../../services/reportService";
import type {
  PositionSummary,
  StageSummary,
  ReportApplication,
} from "../../types/reports";

type DrillDownView = "positions" | "stages" | "applications";

interface DrillDownState {
  view: DrillDownView;
  selectedPosition?: {
    type: string;
    name: string;
  };
  selectedStage?: {
    name: string;
    displayName: string;
  };
}

const AdminReportsPage: React.FC = () => {
  const navigate = useNavigate();

  // State for drill-down navigation
  const [drillDownState, setDrillDownState] = useState<DrillDownState>({
    view: "positions",
  });

  // Position data
  const [positions, setPositions] = useState<PositionSummary[]>([]);
  const [positionsLoading, setPositionsLoading] = useState(true);

  // Stage data
  const [stages, setStages] = useState<StageSummary[]>([]);
  const [stagesLoading, setStagesLoading] = useState(false);

  // Applications data
  const [applications, setApplications] = useState<ReportApplication[]>([]);
  const [applicationsLoading, setApplicationsLoading] = useState(false);
  const [totalApplicationsCount, setTotalApplicationsCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(20);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("createdDate");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");

  // Error handling
  const [error, setError] = useState("");

  // Load positions on mount
  useEffect(() => {
    loadPositions();
  }, []);

  const loadPositions = async () => {
    try {
      setPositionsLoading(true);
      setError("");
      const response = await reportService.getPositionSummaries();

      if (response.success && response.data) {
        setPositions(response.data.positions);
      } else {
        setError(response.message || "Failed to load position summaries");
      }
    } catch (err) {
      console.error("Error loading positions:", err);
      setError("Unable to load position summaries. Please try again later.");
    } finally {
      setPositionsLoading(false);
    }
  };

  const loadStages = async (positionType: string) => {
    try {
      setStagesLoading(true);
      setError("");
      const response = await reportService.getStageSummaries(positionType);

      if (response.success && response.data) {
        setStages(response.data.stages);
      } else {
        setError(response.message || "Failed to load stage summaries");
      }
    } catch (err) {
      console.error("Error loading stages:", err);
      setError("Unable to load stage summaries. Please try again later.");
    } finally {
      setStagesLoading(false);
    }
  };

  const loadApplications = async () => {
    if (!drillDownState.selectedPosition || !drillDownState.selectedStage) {
      return;
    }

    try {
      setApplicationsLoading(true);
      setError("");
      const response = await reportService.getApplicationsByStage({
        positionType: drillDownState.selectedPosition.type,
        stageName: drillDownState.selectedStage.name,
        pageNumber: currentPage,
        pageSize,
        searchTerm: searchTerm || undefined,
        sortBy,
        sortDirection,
      });

      if (response.success && response.data) {
        setApplications(response.data.applications);
        setTotalApplicationsCount(response.data.totalCount);
      } else {
        setError(response.message || "Failed to load applications");
      }
    } catch (err) {
      console.error("Error loading applications:", err);
      setError("Unable to load applications. Please try again later.");
    } finally {
      setApplicationsLoading(false);
    }
  };

  // Reload applications when filters change
  useEffect(() => {
    if (drillDownState.view === "applications") {
      loadApplications();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [drillDownState.view, currentPage, searchTerm, sortBy, sortDirection]);

  // Handlers for drill-down navigation
  const handlePositionClick = async (
    positionType: string,
    positionName: string
  ) => {
    setDrillDownState({
      view: "stages",
      selectedPosition: { type: positionType, name: positionName },
    });
    await loadStages(positionType);
  };

  const handleStageClick = (stageName: string, stageDisplayName: string) => {
    setDrillDownState({
      ...drillDownState,
      view: "applications",
      selectedStage: { name: stageName, displayName: stageDisplayName },
    });
    setCurrentPage(1); // Reset to first page
    setSearchTerm(""); // Reset search
  };

  const handleBackToPositions = () => {
    setDrillDownState({ view: "positions" });
    setStages([]);
  };

  const handleBackToStages = () => {
    setDrillDownState({
      view: "stages",
      selectedPosition: drillDownState.selectedPosition,
    });
    setApplications([]);
    setCurrentPage(1);
    setSearchTerm("");
  };

  // Pagination handlers
  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  // Search handler
  const handleSearch = (term: string) => {
    setSearchTerm(term);
    setCurrentPage(1); // Reset to first page on search
  };

  // Sort handler
  const handleSort = (column: string, direction: "asc" | "desc") => {
    setSortBy(column);
    setSortDirection(direction);
    setCurrentPage(1); // Reset to first page on sort
  };

  // Render based on current view
  const renderContent = () => {
    // Show error if any
    if (error && drillDownState.view === "positions") {
      return (
        <div
          className="pmc-card"
          style={{
            padding: "48px",
            textAlign: "center",
            background: "#fee2e2",
            border: "1px solid #fecaca",
          }}
        >
          <AlertCircle
            style={{
              width: "48px",
              height: "48px",
              margin: "0 auto 16px",
              color: "#dc2626",
            }}
          />
          <h3 style={{ fontSize: "18px", fontWeight: "600", color: "#991b1b" }}>
            Error Loading Reports
          </h3>
          <p style={{ color: "#991b1b", marginTop: "8px" }}>{error}</p>
          <button
            onClick={loadPositions}
            className="pmc-button pmc-button-primary"
            style={{ marginTop: "16px" }}
          >
            Retry
          </button>
        </div>
      );
    }

    switch (drillDownState.view) {
      case "positions": {
        // Define color palette for positions
        const positionColors: Record<string, string> = {
          Architect: "#667eea",
          LicenceEngineer: "#f59e0b",
          StructuralEngineer: "#10b981",
          Supervisor1: "#ef4444",
          Supervisor2: "#8b5cf6",
        };

        // Transform positions data for pie chart
        const chartData = positions.map((pos) => ({
          label: pos.positionName,
          value: pos.totalApplications,
          color: positionColors[pos.positionType] || "#6b7280",
        }));

        return (
          <>
            <PositionSummaryCards
              positions={positions}
              onPositionClick={handlePositionClick}
              isLoading={positionsLoading}
            />

            {/* Pie Chart Section */}
            {positions.length > 0 && !positionsLoading && (
              <div
                className="pmc-card"
                style={{
                  marginTop: "32px",
                  padding: "24px",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "12px",
                    marginBottom: "24px",
                  }}
                >
                  <div
                    style={{
                      width: "40px",
                      height: "40px",
                      borderRadius: "10px",
                      background:
                        "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                    }}
                  >
                    <BarChart3
                      style={{ width: "20px", height: "20px", color: "#fff" }}
                    />
                  </div>
                  <h2
                    style={{
                      fontSize: "20px",
                      fontWeight: 600,
                      color: "#111827",
                    }}
                  >
                    Application Distribution
                  </h2>
                </div>

                <div
                  style={{
                    display: "flex",
                    justifyContent: "center",
                    width: "100%",
                  }}
                >
                  <PieChart
                    data={chartData}
                    width={500}
                    height={400}
                    showLegend
                  />
                </div>
              </div>
            )}
          </>
        );
      }

      case "stages":
        return (
          <StageSummaryCards
            stages={stages}
            positionName={drillDownState.selectedPosition?.name || ""}
            onStageClick={handleStageClick}
            onBack={handleBackToPositions}
            isLoading={stagesLoading}
          />
        );

      case "applications":
        return (
          <ApplicationsTable
            applications={applications}
            positionType={drillDownState.selectedPosition?.type || ""}
            stageName={drillDownState.selectedStage?.name || ""}
            totalCount={totalApplicationsCount}
            currentPage={currentPage}
            pageSize={pageSize}
            onPageChange={handlePageChange}
            onSearch={handleSearch}
            onSort={handleSort}
            onBack={handleBackToStages}
            isLoading={applicationsLoading}
          />
        );

      default:
        return null;
    }
  };

  if (positionsLoading && drillDownState.view === "positions") {
    return <PageLoader message="Loading Reports..." />;
  }

  return (
    <div className="pmc-fadeIn" style={{ padding: "24px" }}>
      {/* Breadcrumbs - Only show on positions view */}
      {drillDownState.view === "positions" && (
        <div
          className="pmc-fadeInDown"
          style={{
            marginBottom: "16px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
            fontSize: "14px",
            color: "var(--pmc-gray-600)",
          }}
        >
          <button
            onClick={() => navigate("/admin")}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "4px",
              background: "none",
              border: "none",
              cursor: "pointer",
              color: "var(--pmc-primary)",
              padding: "4px 8px",
              borderRadius: "4px",
              transition: "background 0.2s",
            }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.background = "var(--pmc-gray-100)")
            }
            onMouseLeave={(e) => (e.currentTarget.style.background = "none")}
          >
            <Home style={{ width: "16px", height: "16px" }} />
            Dashboard
          </button>
          <ChevronRight style={{ width: "16px", height: "16px" }} />
          <span style={{ color: "var(--pmc-gray-900)", fontWeight: "600" }}>
            Reports
          </span>
        </div>
      )}

      {/* Page Header - Only show on positions view */}
      {drillDownState.view === "positions" && (
        <div className="mb-8">
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "16px",
              marginBottom: "8px",
            }}
          >
            <button
              onClick={() => navigate("/admin")}
              className="pmc-button pmc-button-secondary"
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                padding: "10px 16px",
              }}
            >
              <ArrowLeft style={{ width: "18px", height: "18px" }} />
              Back
            </button>
          </div>
          <div className="flex items-center gap-3 mb-2">
            <div
              style={{
                width: "48px",
                height: "48px",
                borderRadius: "12px",
                background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <BarChart3
                style={{ width: "24px", height: "24px", color: "#fff" }}
              />
            </div>
            <div>
              <h1
                style={{
                  fontSize: "28px",
                  fontWeight: "700",
                  color: "#1f2937",
                }}
              >
                Application Reports
              </h1>
              <p
                style={{ fontSize: "14px", color: "#6b7280", marginTop: "4px" }}
              >
                Comprehensive drill-down reporting for all position applications
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Main Content */}
      {renderContent()}
    </div>
  );
};

export default AdminReportsPage;
