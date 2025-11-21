import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
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
  const location = useLocation();

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

  // Handle drill-down restoration from navigation state
  useEffect(() => {
    const navigationState = location.state as {
      restoreDrillDown?: boolean;
      positionType?: string;
      stageName?: string;
      stageDisplayName?: string;
    } | null;

    if (
      navigationState?.restoreDrillDown &&
      navigationState.positionType &&
      navigationState.stageName &&
      navigationState.stageDisplayName
    ) {
      // Find the position name from the positions array
      const position = positions.find(
        (p) => p.positionType === navigationState.positionType
      );

      if (position) {
        // First drill down to stages view
        setDrillDownState({
          view: "stages",
          selectedPosition: {
            type: navigationState.positionType,
            name: position.positionName,
          },
        });

        // Load stages for this position
        loadStages(navigationState.positionType).then(() => {
          // Then drill down to applications view
          setDrillDownState({
            view: "applications",
            selectedPosition: {
              type: navigationState.positionType!,
              name: position.positionName,
            },
            selectedStage: {
              name: navigationState.stageName!,
              displayName: navigationState.stageDisplayName!,
            },
          });
          setCurrentPage(1);
          setSearchTerm("");
        });
      }

      // Clear the navigation state to prevent re-triggering
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.state, positions, navigate, location.pathname]);

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
            {/* Position Cards in Single Row */}
            <PositionSummaryCards
              positions={positions}
              onPositionClick={handlePositionClick}
              isLoading={positionsLoading}
            />

            {/* Pie Chart Below Position Cards */}
            {positions.length > 0 && !positionsLoading && (
              <div
                className="pmc-card"
                style={{
                  marginTop: "32px",
                  padding: "32px",
                  background:
                    "linear-gradient(135deg, #ffffff 0%, #f8fafc 100%)",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "12px",
                    marginBottom: "32px",
                    justifyContent: "center",
                  }}
                >
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
                      boxShadow: "0 8px 20px rgba(102, 126, 234, 0.3)",
                    }}
                  >
                    <BarChart3
                      style={{ width: "24px", height: "24px", color: "#fff" }}
                    />
                  </div>
                  <h2
                    style={{
                      fontSize: "24px",
                      fontWeight: "700",
                      color: "#111827",
                    }}
                  >
                    Application Distribution by Position
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
                    width={600}
                    height={450}
                    showLegend
                  />
                </div>
              </div>
            )}
          </>
        );
      }

      case "stages": {
        return (
          <StageSummaryCards
            stages={stages}
            positionName={drillDownState.selectedPosition?.name || ""}
            onStageClick={handleStageClick}
            onBack={handleBackToPositions}
            isLoading={stagesLoading}
          />
        );
      }

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

  // Render breadcrumbs for all views
  const renderBreadcrumbs = () => {
    const breadcrumbStyle = {
      display: "flex",
      alignItems: "center",
      gap: "8px",
      fontSize: "14px",
      padding: "12px 20px",
      background: "#f8fafc",
      borderRadius: "10px",
      border: "1px solid #e2e8f0",
    };

    const buttonStyle = {
      display: "flex",
      alignItems: "center",
      gap: "6px",
      background: "transparent",
      border: "none",
      cursor: "pointer",
      color: "#667eea",
      padding: "6px 12px",
      borderRadius: "6px",
      transition: "all 0.2s",
      fontSize: "14px",
      fontWeight: "600",
    };

    const separatorStyle = {
      color: "#cbd5e1",
      fontSize: "16px",
    };

    const currentStyle = {
      color: "#1e293b",
      fontWeight: "700",
      fontSize: "14px",
    };

    switch (drillDownState.view) {
      case "positions":
        return (
          <div className="pmc-fadeInDown" style={breadcrumbStyle}>
            <button
              onClick={() => navigate("/admin")}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              <Home style={{ width: "18px", height: "18px" }} />
              Dashboard
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <span style={currentStyle}>Reports</span>
          </div>
        );

      case "stages":
        return (
          <div className="pmc-fadeInDown" style={breadcrumbStyle}>
            <button
              onClick={() => navigate("/admin")}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              <Home style={{ width: "18px", height: "18px" }} />
              Dashboard
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <button
              onClick={handleBackToPositions}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              Reports
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <span style={currentStyle}>
              {drillDownState.selectedPosition?.name}
            </span>
          </div>
        );

      case "applications":
        return (
          <div className="pmc-fadeInDown" style={breadcrumbStyle}>
            <button
              onClick={() => navigate("/admin")}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              <Home style={{ width: "18px", height: "18px" }} />
              Dashboard
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <button
              onClick={handleBackToPositions}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              Reports
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <button
              onClick={handleBackToStages}
              style={buttonStyle}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = "#ede9fe";
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = "transparent";
              }}
            >
              {drillDownState.selectedPosition?.name}
            </button>
            <ChevronRight
              style={{ width: "18px", height: "18px", ...separatorStyle }}
            />
            <span style={currentStyle}>
              {drillDownState.selectedStage?.displayName}
            </span>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div
      className="pmc-fadeIn"
      style={{ padding: "32px", maxWidth: "1400px", margin: "0 auto" }}
    >
      {/* Breadcrumbs for all views */}
      <div style={{ marginBottom: "24px" }}>{renderBreadcrumbs()}</div>

      {/* Page Header - Only show on positions view */}
      {drillDownState.view === "positions" && (
        <div style={{ marginBottom: "32px" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: "24px",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <div
                style={{
                  width: "56px",
                  height: "56px",
                  borderRadius: "14px",
                  background:
                    "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  boxShadow: "0 8px 20px rgba(102, 126, 234, 0.25)",
                }}
              >
                <BarChart3
                  style={{ width: "28px", height: "28px", color: "#fff" }}
                />
              </div>
              <div>
                <h1
                  style={{
                    fontSize: "32px",
                    fontWeight: "700",
                    color: "#1f2937",
                    marginBottom: "4px",
                  }}
                >
                  Application Reports
                </h1>
                <p style={{ fontSize: "15px", color: "#6b7280" }}>
                  Select a position to view detailed stage reports
                </p>
              </div>
            </div>
            <button
              onClick={() => navigate("/admin")}
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
              Back to Dashboard
            </button>
          </div>
        </div>
      )}

      {/* Main Content */}
      {renderContent()}
    </div>
  );
};

export default AdminReportsPage;
