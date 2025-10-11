import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import positionRegistrationService, {
  type PositionRegistrationResponse,
} from "../services/positionRegistrationService";
import {
  FileText,
  CheckCircle,
  Clock,
  Calendar,
  ChevronDown,
  Plus,
  Eye,
} from "lucide-react";
import { PageLoader, SectionLoader } from "../components";

// Position Type Enum matching backend
const PositionType = {
  Architect: 0,
  LicenceEngineer: 1,
  StructuralEngineer: 2,
  Supervisor1: 3,
  Supervisor2: 4,
} as const;

type PositionTypeValue = (typeof PositionType)[keyof typeof PositionType];

interface DashboardStats {
  totalApplications: number;
  submittedApplications: number;
  draftApplications: number;
  completedApplications: number;
}

const Dashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [submittedApplications, setSubmittedApplications] = useState<
    PositionRegistrationResponse[]
  >([]);
  const [draftApplications, setDraftApplications] = useState<
    PositionRegistrationResponse[]
  >([]);
  const [stats, setStats] = useState<DashboardStats>({
    totalApplications: 0,
    submittedApplications: 0,
    draftApplications: 0,
    completedApplications: 0,
  });
  const [loading, setLoading] = useState(true);
  const [tabLoading, setTabLoading] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const [activeTab, setActiveTab] = useState<"submitted" | "draft">(
    "submitted"
  );

  // Redirect JuniorEngineer to JE Dashboard
  useEffect(() => {
    if (user?.role && user.role.includes("Junior")) {
      navigate("/je-dashboard", { replace: true });
    }
  }, [user, navigate]);

  // Position type options
  const positionTypes = [
    { value: PositionType.Architect, label: "Architect", icon: "🏛️" },
    {
      value: PositionType.StructuralEngineer,
      label: "Structural Engineer",
      icon: "👷",
    },
    {
      value: PositionType.LicenceEngineer,
      label: "Licence Engineer",
      icon: "📐",
    },
    { value: PositionType.Supervisor1, label: "Supervisor 1", icon: "👨‍💼" },
    { value: PositionType.Supervisor2, label: "Supervisor 2", icon: "👩‍💼" },
  ];

  // Fetch user's applications and stats
  useEffect(() => {
    if (!user) return;

    const fetchData = async () => {
      try {
        setLoading(true);
        console.log("📊 Fetching dashboard data for user:", user.id);

        // Fetch all applications for the logged-in user
        const allApplicationsResponse =
          await positionRegistrationService.getAllApplications({
            userId: user.id,
          });

        console.log("✅ Fetched applications:", allApplicationsResponse);

        // Separate submitted and draft applications
        const submitted = allApplicationsResponse.filter(
          (app) => app.status === 2
        ); // Status 2 = Submitted
        const drafts = allApplicationsResponse.filter(
          (app) => app.status === 1
        ); // Status 1 = Draft
        const completed = allApplicationsResponse.filter(
          (app) => app.status === 23
        ); // Status 23 = Completed

        console.log(
          "📋 Submitted:",
          submitted.length,
          "Draft:",
          drafts.length,
          "Completed:",
          completed.length
        );

        setSubmittedApplications(submitted);
        setDraftApplications(drafts);

        // Calculate stats
        setStats({
          totalApplications: allApplicationsResponse.length,
          submittedApplications: submitted.length,
          draftApplications: drafts.length,
          completedApplications: completed.length,
        });
      } catch (error) {
        console.error("❌ Error fetching dashboard data:", error);
        // Set empty state on error
        setSubmittedApplications([]);
        setDraftApplications([]);
        setStats({
          totalApplications: 0,
          submittedApplications: 0,
          draftApplications: 0,
          completedApplications: 0,
        });
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user]);

  const handleCreateForm = (positionType: PositionTypeValue) => {
    setShowDropdown(false);

    // Map position type to URL-friendly string
    const positionRoutes: Record<PositionTypeValue, string> = {
      [PositionType.Architect]: "architect",
      [PositionType.LicenceEngineer]: "licence-engineer",
      [PositionType.StructuralEngineer]: "structural-engineer",
      [PositionType.Supervisor1]: "supervisor1",
      [PositionType.Supervisor2]: "supervisor2",
    };

    navigate(`/register/${positionRoutes[positionType]}`);
  };

  const handleTabChange = (tab: "submitted" | "draft") => {
    if (tab !== activeTab) {
      setTabLoading(true);
      setActiveTab(tab);
      // Simulate loading delay for tab content
      setTimeout(() => {
        setTabLoading(false);
      }, 300);
    }
  };

  // Show page loader for initial load
  if (loading) {
    return <PageLoader message="Loading Dashboard..." />;
  }

  return (
    <div className="pmc-fadeIn">
      {/* Welcome Section */}
      <div className="pmc-content-header pmc-fadeInDown">
        <h1
          className="pmc-content-title pmc-text-3xl pmc-font-bold"
          style={{ color: "var(--pmc-gray-900)" }}
        >
          Welcome back, {user?.name || "User"}! 👋
        </h1>
        <p
          className="pmc-content-subtitle pmc-text-base"
          style={{ color: "var(--pmc-gray-600)" }}
        >
          Track your applications and create new registrations
        </p>
      </div>

      {/* Analytical Statistics Cards - Horizontal Layout */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(4, 1fr)",
          gap: "24px",
          marginBottom: "32px",
        }}
        className="pmc-fadeInUp"
      >
        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background:
              "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)",
            border: "none",
            color: "white",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <div
              style={{
                width: "56px",
                height: "56px",
                background: "rgba(255, 255, 255, 0.2)",
                borderRadius: "12px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <FileText style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Total Applications
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.totalApplications}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background:
              "linear-gradient(135deg, var(--pmc-success) 0%, #15803d 100%)",
            border: "none",
            color: "white",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <div
              style={{
                width: "56px",
                height: "56px",
                background: "rgba(255, 255, 255, 0.2)",
                borderRadius: "12px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <CheckCircle style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Submitted
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.submittedApplications}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background:
              "linear-gradient(135deg, var(--pmc-warning) 0%, #ca8a04 100%)",
            border: "none",
            color: "white",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <div
              style={{
                width: "56px",
                height: "56px",
                background: "rgba(255, 255, 255, 0.2)",
                borderRadius: "12px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <Clock style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Draft
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.draftApplications}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background:
              "linear-gradient(135deg, var(--pmc-primary) 0%, #1e40af 100%)",
            border: "none",
            color: "white",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <div
              style={{
                width: "56px",
                height: "56px",
                background: "rgba(255, 255, 255, 0.2)",
                borderRadius: "12px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <FileText style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Completed
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.completedApplications}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Create New Application Dropdown */}
      <div
        className="pmc-card pmc-slideInLeft"
        style={{
          marginBottom: showDropdown ? "320px" : "32px",
          overflow: "visible",
          position: "relative",
          zIndex: 100,
          transition: "margin-bottom 0.3s ease",
        }}
      >
        <div className="pmc-card-header">
          <h2 className="pmc-card-title">Create New Application</h2>
          <p className="pmc-card-subtitle">
            Select position type to start a new registration
          </p>
        </div>
        <div className="pmc-card-body" style={{ overflow: "visible" }}>
          <div style={{ position: "relative", maxWidth: "400px" }}>
            <button
              className="pmc-button pmc-button-primary pmc-button-full"
              onClick={() => setShowDropdown(!showDropdown)}
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                padding: "16px 20px",
              }}
            >
              <span
                style={{ display: "flex", alignItems: "center", gap: "8px" }}
              >
                <Plus style={{ width: "20px", height: "20px" }} />
                <span className="pmc-font-semibold">
                  Create New Registration
                </span>
              </span>
              <ChevronDown
                style={{
                  width: "20px",
                  height: "20px",
                  transform: showDropdown ? "rotate(180deg)" : "rotate(0deg)",
                  transition: "transform 0.3s ease",
                }}
              />
            </button>

            {showDropdown && (
              <div
                className="pmc-fadeIn"
                style={{
                  position: "absolute",
                  top: "calc(100% + 8px)",
                  left: 0,
                  right: 0,
                  background: "#ffffff",
                  borderRadius: "12px",
                  boxShadow:
                    "0 10px 25px rgba(0, 0, 0, 0.15), 0 4px 8px rgba(0, 0, 0, 0.08)",
                  border: "1px solid #e5e7eb",
                  zIndex: 1000,
                  overflow: "hidden",
                }}
              >
                {positionTypes.map((type, index) => (
                  <button
                    key={type.value}
                    onClick={() => handleCreateForm(type.value)}
                    style={{
                      width: "100%",
                      textAlign: "left",
                      borderRadius: 0,
                      border: "none",
                      borderBottom:
                        index < positionTypes.length - 1
                          ? "1px solid #f3f4f6"
                          : "none",
                      padding: "16px 20px",
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                      transition: "all 0.2s ease",
                      background: "#ffffff",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: "500",
                      color: "#1f2937",
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.background = "#f9fafb";
                      e.currentTarget.style.transform = "translateX(4px)";
                      e.currentTarget.style.color = "#1e40af";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.background = "#ffffff";
                      e.currentTarget.style.transform = "translateX(0)";
                      e.currentTarget.style.color = "#1f2937";
                    }}
                  >
                    <span style={{ fontSize: "24px" }}>{type.icon}</span>
                    <span className="pmc-font-medium pmc-text-sm">
                      {type.label}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Applications Tabs */}
      <div className="pmc-card" style={{ marginBottom: "8px" }}>
        <div style={{ display: "flex", borderBottom: "2px solid #e2e8f0" }}>
          <button
            onClick={() => handleTabChange("submitted")}
            style={{
              flex: 1,
              padding: "16px",
              border: "none",
              background:
                activeTab === "submitted"
                  ? "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)"
                  : "transparent",
              borderBottom:
                activeTab === "submitted"
                  ? "3px solid var(--pmc-primary)"
                  : "none",
              color:
                activeTab === "submitted"
                  ? "var(--pmc-primary)"
                  : "var(--pmc-gray-600)",
              fontWeight: activeTab === "submitted" ? 600 : 400,
              cursor: "pointer",
              transition: "all 0.3s ease",
            }}
          >
            Submitted Applications ({stats.submittedApplications})
          </button>
          <button
            onClick={() => handleTabChange("draft")}
            style={{
              flex: 1,
              padding: "16px",
              border: "none",
              background:
                activeTab === "draft"
                  ? "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)"
                  : "transparent",
              borderBottom:
                activeTab === "draft" ? "3px solid var(--pmc-primary)" : "none",
              color:
                activeTab === "draft"
                  ? "var(--pmc-primary)"
                  : "var(--pmc-gray-600)",
              fontWeight: activeTab === "draft" ? 600 : 400,
              cursor: "pointer",
              transition: "all 0.3s ease",
            }}
          >
            Draft Applications ({stats.draftApplications})
          </button>
        </div>
      </div>

      {/* Applications Table */}
      <div className="pmc-table-container pmc-slideInRight">
        <div className="pmc-table-responsive">
          {tabLoading ? (
            <div style={{ padding: "60px 24px" }}>
              <SectionLoader
                message={`Loading ${activeTab} applications...`}
                size="medium"
              />
            </div>
          ) : (activeTab === "submitted"
              ? submittedApplications
              : draftApplications
            ).length === 0 ? (
            <div
              style={{
                padding: "60px 24px",
                textAlign: "center",
                color: "var(--pmc-gray-500)",
              }}
            >
              <FileText
                style={{
                  width: "48px",
                  height: "48px",
                  margin: "0 auto 16px",
                  opacity: 0.3,
                }}
              />
              <p
                className="pmc-text-base pmc-font-medium"
                style={{ marginBottom: "8px" }}
              >
                No {activeTab} applications yet
              </p>
              <p className="pmc-text-sm">
                Create your first application using the dropdown above
              </p>
            </div>
          ) : (
            <table className="pmc-table">
              <thead>
                <tr>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Application #
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Position Type
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Applicant Name
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    {activeTab === "submitted"
                      ? "Submitted Date"
                      : "Created Date"}
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Status
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody>
                {(activeTab === "submitted"
                  ? submittedApplications
                  : draftApplications
                ).map((app) => (
                  <tr key={app.id}>
                    <td
                      className="pmc-text-sm pmc-font-semibold"
                      style={{ color: "var(--pmc-primary)" }}
                    >
                      #{app.applicationNumber || app.id}
                    </td>
                    <td
                      className="pmc-text-sm pmc-font-medium"
                      style={{ color: "var(--pmc-gray-800)" }}
                    >
                      {app.positionTypeName}
                    </td>
                    <td
                      className="pmc-text-sm pmc-font-medium"
                      style={{ color: "var(--pmc-gray-800)" }}
                    >
                      {app.fullName}
                    </td>
                    <td
                      className="pmc-text-sm"
                      style={{ color: "var(--pmc-gray-600)" }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "6px",
                        }}
                      >
                        <Calendar style={{ width: "14px", height: "14px" }} />
                        {new Date(
                          activeTab === "submitted"
                            ? app.submittedDate || app.createdDate
                            : app.createdDate
                        ).toLocaleDateString()}
                      </div>
                    </td>
                    <td>
                      <span
                        className={
                          app.status === 23
                            ? "pmc-badge pmc-status-approved"
                            : app.status === 2
                            ? "pmc-badge pmc-status-under-review"
                            : "pmc-badge pmc-status-pending"
                        }
                      >
                        {app.statusName}
                      </span>
                    </td>
                    <td>
                      <button
                        className="pmc-button pmc-button-secondary pmc-button-sm"
                        onClick={() => {
                          if (activeTab === "draft") {
                            // For drafts, navigate to edit page
                            const positionRoutes: Record<number, string> = {
                              0: "architect",
                              1: "licence-engineer",
                              2: "structural-engineer",
                              3: "supervisor1",
                              4: "supervisor2",
                            };
                            const positionRoute =
                              positionRoutes[app.positionType] ||
                              "structural-engineer";
                            navigate(`/register/${positionRoute}/${app.id}`);
                          } else {
                            // For submitted applications, view details
                            navigate(`/application/${app.id}`);
                          }
                        }}
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "6px",
                        }}
                      >
                        {activeTab === "draft" ? (
                          <>
                            <svg
                              width="14"
                              height="14"
                              viewBox="0 0 24 24"
                              fill="none"
                              stroke="currentColor"
                              strokeWidth="2"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            >
                              <path d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z" />
                            </svg>
                            Edit
                          </>
                        ) : (
                          <>
                            <Eye size={14} />
                            View
                          </>
                        )}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
