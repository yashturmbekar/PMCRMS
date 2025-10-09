import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { apiService } from "../services/apiService";
import {
  FileText,
  CheckCircle,
  Clock,
  XCircle,
  Calendar,
  ChevronDown,
  Plus,
} from "lucide-react";

// Position Type Enum matching backend
const PositionType = {
  Architect: 0,
  LicenceEngineer: 1,
  StructuralEngineer: 2,
  Supervisor1: 3,
  Supervisor2: 4,
} as const;

type PositionTypeValue = (typeof PositionType)[keyof typeof PositionType];

// Application Stage Enum matching backend
const ApplicationStage = {
  JUNIOR_ENGINEER_PENDING: 0,
  DOCUMENT_VERIFICATION_PENDING: 1,
  ASSISTANT_ENGINEER_PENDING: 2,
  EXECUTIVE_ENGINEER_PENDING: 3,
  CITY_ENGINEER_PENDING: 4,
  PAYMENT_PENDING: 5,
  CLERK_PENDING: 6,
  EXECUTIVE_ENGINEER_SIGN_PENDING: 7,
  CITY_ENGINEER_SIGN_PENDING: 8,
  APPROVED: 9,
  REJECTED: 10,
} as const;

type ApplicationStageValue =
  (typeof ApplicationStage)[keyof typeof ApplicationStage];

interface UserApplication {
  id: number;
  applicationNumber: string;
  positionType: PositionTypeValue;
  applicantName: string;
  submissionDate: string;
  stage: ApplicationStageValue;
  status: string;
}

interface DashboardStats {
  totalApplications: number;
  approvedApplications: number;
  pendingApplications: number;
  rejectedApplications: number;
}

const Dashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [applications, setApplications] = useState<UserApplication[]>([]);
  const [stats, setStats] = useState<DashboardStats>({
    totalApplications: 0,
    approvedApplications: 0,
    pendingApplications: 0,
    rejectedApplications: 0,
  });
  const [loading, setLoading] = useState(true);
  const [showDropdown, setShowDropdown] = useState(false);

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

        // Fetch analytics from dedicated endpoint
        const analyticsResponse =
          await apiService.applications.getDashboardAnalytics();
        if (analyticsResponse.success && analyticsResponse.data) {
          setStats({
            totalApplications: analyticsResponse.data.totalApplications,
            approvedApplications: analyticsResponse.data.approvedApplications,
            pendingApplications: analyticsResponse.data.pendingApplications,
            rejectedApplications: analyticsResponse.data.rejectedApplications,
          });
        }

        // Fetch user's applications
        const applicationsResponse =
          await apiService.applications.getMyApplications(1, 100);

        if (applicationsResponse.success && applicationsResponse.data) {
          // Handle different response structures
          const responseData = applicationsResponse.data as unknown as Record<
            string,
            unknown
          >;
          const userApps = (responseData.items ||
            applicationsResponse.data) as unknown[];
          setApplications(userApps as UserApplication[]);
        }
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
        // Set empty state on error
        setApplications([]);
        setStats({
          totalApplications: 0,
          approvedApplications: 0,
          pendingApplications: 0,
          rejectedApplications: 0,
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

  const getStatusBadge = (stage: ApplicationStageValue) => {
    if (stage === ApplicationStage.APPROVED) {
      return "pmc-badge pmc-status-approved";
    } else if (stage === ApplicationStage.REJECTED) {
      return "pmc-badge pmc-status-rejected";
    } else if (
      stage === ApplicationStage.PAYMENT_PENDING ||
      stage === ApplicationStage.CLERK_PENDING
    ) {
      return "pmc-badge pmc-status-pending";
    } else {
      return "pmc-badge pmc-status-under-review";
    }
  };

  const getStatusText = (stage: ApplicationStageValue): string => {
    const stageNames: Record<ApplicationStageValue, string> = {
      [ApplicationStage.JUNIOR_ENGINEER_PENDING]: "Junior Engineer Review",
      [ApplicationStage.DOCUMENT_VERIFICATION_PENDING]: "Document Verification",
      [ApplicationStage.ASSISTANT_ENGINEER_PENDING]:
        "Assistant Engineer Review",
      [ApplicationStage.EXECUTIVE_ENGINEER_PENDING]:
        "Executive Engineer Review",
      [ApplicationStage.CITY_ENGINEER_PENDING]: "City Engineer Review",
      [ApplicationStage.PAYMENT_PENDING]: "Payment Pending",
      [ApplicationStage.CLERK_PENDING]: "Clerk Processing",
      [ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING]:
        "Executive Engineer Signature",
      [ApplicationStage.CITY_ENGINEER_SIGN_PENDING]: "City Engineer Signature",
      [ApplicationStage.APPROVED]: "Approved",
      [ApplicationStage.REJECTED]: "Rejected",
    };
    return stageNames[stage] || "Unknown";
  };

  const getPositionTypeLabel = (type: PositionTypeValue): string => {
    const typeLabels: Record<PositionTypeValue, string> = {
      [PositionType.Architect]: "Architect",
      [PositionType.StructuralEngineer]: "Structural Engineer",
      [PositionType.LicenceEngineer]: "Licence Engineer",
      [PositionType.Supervisor1]: "Supervisor 1",
      [PositionType.Supervisor2]: "Supervisor 2",
    };
    return typeLabels[type] || "Unknown";
  };

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
                Approved
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.approvedApplications}
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
                Pending
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.pendingApplications}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background:
              "linear-gradient(135deg, var(--pmc-error) 0%, #b91c1c 100%)",
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
              <XCircle style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Rejected
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {loading ? "..." : stats.rejectedApplications}
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

      {/* Submitted Applications Table */}
      <div className="pmc-table-container pmc-slideInRight">
        <div
          className="pmc-card-header"
          style={{
            padding: "20px 24px",
            borderBottom: "1px solid var(--pmc-gray-200)",
          }}
        >
          <h2 className="pmc-card-title">My Submitted Applications</h2>
          <p className="pmc-card-subtitle">
            Track the status of your applications
          </p>
        </div>
        <div className="pmc-table-responsive">
          {loading ? (
            <div
              style={{
                padding: "60px 24px",
                textAlign: "center",
                color: "var(--pmc-gray-500)",
              }}
            >
              <div
                className="pmc-loading-spinner"
                style={{ margin: "0 auto 16px" }}
              ></div>
              <p className="pmc-text-sm">Loading applications...</p>
            </div>
          ) : applications.length === 0 ? (
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
                No applications yet
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
                    Submission Date
                  </th>
                  <th
                    className="pmc-text-xs pmc-font-semibold"
                    style={{
                      textTransform: "uppercase",
                      letterSpacing: "0.05em",
                      color: "var(--pmc-gray-700)",
                    }}
                  >
                    Current Stage
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
                {applications.map((app) => (
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
                      {getPositionTypeLabel(app.positionType)}
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
                        {new Date(app.submissionDate).toLocaleDateString()}
                      </div>
                    </td>
                    <td
                      className="pmc-text-sm"
                      style={{ color: "var(--pmc-gray-700)" }}
                    >
                      {getStatusText(app.stage)}
                    </td>
                    <td>
                      <span className={getStatusBadge(app.stage)}>
                        {app.stage === ApplicationStage.APPROVED
                          ? "APPROVED"
                          : app.stage === ApplicationStage.REJECTED
                          ? "REJECTED"
                          : "PENDING"}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: "8px" }}>
                        <button
                          className="pmc-button pmc-button-primary pmc-button-sm"
                          onClick={() => navigate(`/applications/${app.id}`)}
                        >
                          View
                        </button>
                        {app.stage !== ApplicationStage.APPROVED &&
                          app.stage !== ApplicationStage.REJECTED && (
                            <button
                              className="pmc-button pmc-button-outline pmc-button-sm"
                              onClick={() =>
                                navigate(`/applications/${app.id}/edit`)
                              }
                            >
                              Edit
                            </button>
                          )}
                      </div>
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
