import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  adminService,
  type AdminDashboardStats,
} from "../../services/adminService";
import {
  Users,
  FileText,
  CheckCircle,
  XCircle,
  Clock,
  DollarSign,
  UserCog,
  Mail,
  Eye,
} from "lucide-react";
import { PageLoader } from "../../components";
import { parseLocalDateTime } from "../../utils/dateUtils";

interface ApplicationSummary {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  applicationType: string;
  status: string;
  submittedOn: string;
}

const AdminDashboard: React.FC = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [recentApplications, setRecentApplications] = useState<
    ApplicationSummary[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    loadDashboardStats();
  }, []);

  const loadDashboardStats = async () => {
    try {
      setLoading(true);
      const [statsResponse, applicationsResponse] = await Promise.all([
        adminService.getDashboardStats(),
        adminService.getAllApplications({ pageSize: 5 }),
      ]);

      console.log("Dashboard stats response:", statsResponse);
      console.log("Applications response:", applicationsResponse);

      if (statsResponse.success && statsResponse.data) {
        console.log("Setting stats:", statsResponse.data);
        setStats(statsResponse.data);
      } else {
        console.error("Stats response failed:", statsResponse);
        setError(
          statsResponse.message || "Failed to load dashboard statistics"
        );
      }

      if (applicationsResponse.success && applicationsResponse.data) {
        // Sort by submission date and take the 5 most recent
        const apps = applicationsResponse.data as ApplicationSummary[];
        const sortedApps = apps
          .filter((app) => app.submittedOn) // Only show submitted applications
          .sort(
            (a, b) =>
              parseLocalDateTime(b.submittedOn).getTime() -
              parseLocalDateTime(a.submittedOn).getTime()
          )
          .slice(0, 5);
        setRecentApplications(sortedApps);
      }
    } catch (err) {
      console.error("Error loading dashboard:", err);
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Failed to load dashboard");
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <PageLoader message="Loading Dashboard..." />;
  }

  if (error || !stats) {
    return (
      <div className="pmc-fadeIn" style={{ padding: "24px" }}>
        <div
          className="pmc-card"
          style={{
            padding: "32px",
            textAlign: "center",
            background: "#fee2e2",
            border: "1px solid #fecaca",
          }}
        >
          <XCircle
            style={{
              width: "48px",
              height: "48px",
              color: "#dc2626",
              margin: "0 auto 16px",
            }}
          />
          <p
            className="pmc-font-semibold"
            style={{ color: "#dc2626", marginBottom: "16px" }}
          >
            {error || "Failed to load dashboard"}
          </p>
          <button
            onClick={loadDashboardStats}
            className="pmc-button pmc-button-primary"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="pmc-fadeIn">
      <div className="pmc-content-header pmc-fadeInDown">
        <h1
          className="pmc-content-title pmc-text-3xl pmc-font-bold"
          style={{ color: "var(--pmc-gray-900)" }}
        >
          Admin Dashboard 👨‍💼
        </h1>
        <p
          className="pmc-content-subtitle pmc-text-base"
          style={{ color: "var(--pmc-gray-600)" }}
        >
          Manage applications, officers, and system configuration
        </p>
      </div>

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
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/applications")}
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
                {stats.totalApplications}
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
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/applications?status=pending")}
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
                Pending Review
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {stats.pendingApplications}
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
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/applications?status=approved")}
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
                {stats.approvedApplications}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background: "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
            border: "none",
            color: "white",
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/applications?status=rejected")}
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
                {stats.rejectedApplications}
              </p>
            </div>
          </div>
        </div>
      </div>

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
            background: "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)",
            border: "none",
            color: "white",
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/officers")}
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
              <Users style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Total Officers
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {stats.totalOfficers}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background: "linear-gradient(135deg, #059669 0%, #047857 100%)",
            border: "none",
            color: "white",
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/officers")}
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
              <UserCog style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Active Officers
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {stats.activeOfficers}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background: "linear-gradient(135deg, #ea580c 0%, #c2410c 100%)",
            border: "none",
            color: "white",
            cursor: "pointer",
          }}
          onClick={() => navigate("/admin/officers")}
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
              <Mail style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Pending Invitations
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                {stats.pendingInvitations}
              </p>
            </div>
          </div>
        </div>

        <div
          className="pmc-card"
          style={{
            padding: "24px",
            background: "linear-gradient(135deg, #0891b2 0%, #0e7490 100%)",
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
              <DollarSign style={{ width: "28px", height: "28px" }} />
            </div>
            <div style={{ flex: 1 }}>
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ opacity: 0.9, marginBottom: "4px" }}
              >
                Total Revenue
              </p>
              <p className="pmc-text-3xl pmc-font-bold">
                ₹{stats.totalRevenueCollected.toLocaleString()}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div
        className="pmc-card pmc-slideInLeft"
        style={{ marginBottom: "32px" }}
      >
        <div className="pmc-card-header">
          <h2 className="pmc-card-title">Quick Actions</h2>
          <p className="pmc-card-subtitle">Common administrative tasks</p>
        </div>
        <div className="pmc-card-body">
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(3, 1fr)",
              gap: "16px",
            }}
          >
            <button
              className="pmc-button pmc-button-primary"
              onClick={() => navigate("/admin/officers")}
              style={{
                padding: "16px 20px",
                display: "flex",
                alignItems: "center",
                gap: "8px",
                justifyContent: "center",
              }}
            >
              <UserCog style={{ width: "20px", height: "20px" }} />
              <span className="pmc-font-semibold">Manage Officers</span>
            </button>
            <button
              className="pmc-button pmc-button-secondary"
              onClick={() => navigate("/admin/applications")}
              style={{
                padding: "16px 20px",
                display: "flex",
                alignItems: "center",
                gap: "8px",
                justifyContent: "center",
              }}
            >
              <FileText style={{ width: "20px", height: "20px" }} />
              <span className="pmc-font-semibold">View All Applications</span>
            </button>
            <button
              className="pmc-button pmc-button-secondary"
              onClick={() => navigate("/admin/forms")}
              style={{
                padding: "16px 20px",
                display: "flex",
                alignItems: "center",
                gap: "8px",
                justifyContent: "center",
              }}
            >
              <FileText style={{ width: "20px", height: "20px" }} />
              <span className="pmc-font-semibold">Manage Forms</span>
            </button>
          </div>
        </div>
      </div>

      {/* Recent Submitted Applications */}
      <div className="pmc-card pmc-slideInRight">
        <div className="pmc-card-header">
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <div>
              <h2 className="pmc-card-title">Recent Submitted Applications</h2>
              <p className="pmc-card-subtitle">Latest 5 applications</p>
            </div>
            <button
              className="pmc-button pmc-button-primary pmc-button-sm"
              onClick={() => navigate("/admin/applications")}
            >
              View All
            </button>
          </div>
        </div>
        <div className="pmc-card-body">
          <div className="pmc-table-container">
            <div className="pmc-table-responsive">
              {recentApplications.length > 0 ? (
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
                        Applicant
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Type
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Submitted
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
                    {recentApplications.map((app) => (
                      <tr key={app.applicationId}>
                        <td
                          className="pmc-text-sm pmc-font-semibold"
                          style={{ color: "var(--pmc-primary)" }}
                        >
                          #{app.applicationNumber}
                        </td>
                        <td
                          className="pmc-text-sm pmc-font-medium"
                          style={{ color: "var(--pmc-gray-800)" }}
                        >
                          {app.applicantName}
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {app.applicationType}
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {parseLocalDateTime(
                            app.submittedOn
                          ).toLocaleDateString()}
                        </td>
                        <td>
                          <span
                            className={`pmc-badge ${
                              app.status.toLowerCase() === "approved"
                                ? "pmc-status-approved"
                                : app.status.toLowerCase() === "rejected"
                                ? "pmc-status-rejected"
                                : "pmc-status-under-review"
                            }`}
                          >
                            <span
                              style={{
                                display: "flex",
                                alignItems: "center",
                                gap: "4px",
                              }}
                            >
                              {app.status.toLowerCase() === "approved" ? (
                                <CheckCircle
                                  style={{ width: "14px", height: "14px" }}
                                />
                              ) : app.status.toLowerCase() === "rejected" ? (
                                <XCircle
                                  style={{ width: "14px", height: "14px" }}
                                />
                              ) : (
                                <Clock
                                  style={{ width: "14px", height: "14px" }}
                                />
                              )}
                              {app.status}
                            </span>
                          </span>
                        </td>
                        <td>
                          <button
                            onClick={() =>
                              navigate(
                                `/admin/applications/${app.applicationId}`
                              )
                            }
                            className="pmc-button pmc-button-primary pmc-button-sm"
                            style={{
                              display: "inline-flex",
                              alignItems: "center",
                              gap: "6px",
                            }}
                          >
                            <Eye style={{ width: "14px", height: "14px" }} />
                            View
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div
                  style={{
                    padding: "48px 24px",
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
                    No applications found
                  </p>
                  <p className="pmc-text-sm">
                    Applications will appear here once users submit them
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
