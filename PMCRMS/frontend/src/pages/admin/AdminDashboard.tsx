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
} from "lucide-react";
import { PageLoader } from "../../components";

const AdminDashboard: React.FC = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    loadDashboardStats();
  }, []);

  const loadDashboardStats = async () => {
    try {
      setLoading(true);
      const response = await adminService.getDashboardStats();
      if (response.success && response.data) {
        setStats(response.data);
      } else {
        setError(response.message || "Failed to load dashboard statistics");
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
              <span className="pmc-font-semibold">View Applications</span>
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

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(2, 1fr)",
          gap: "24px",
        }}
        className="pmc-slideInRight"
      >
        <div className="pmc-card">
          <div className="pmc-card-header">
            <h2 className="pmc-card-title">Officer Distribution</h2>
            <p className="pmc-card-subtitle">Officers by role</p>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-table-container">
              <div className="pmc-table-responsive">
                {stats.roleDistribution && stats.roleDistribution.length > 0 ? (
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
                          Role
                        </th>
                        <th
                          className="pmc-text-xs pmc-font-semibold"
                          style={{
                            textTransform: "uppercase",
                            letterSpacing: "0.05em",
                            color: "var(--pmc-gray-700)",
                          }}
                        >
                          Total
                        </th>
                        <th
                          className="pmc-text-xs pmc-font-semibold"
                          style={{
                            textTransform: "uppercase",
                            letterSpacing: "0.05em",
                            color: "var(--pmc-gray-700)",
                          }}
                        >
                          Active
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {stats.roleDistribution.map((role) => (
                        <tr key={role.role}>
                          <td
                            className="pmc-text-sm pmc-font-medium"
                            style={{ color: "var(--pmc-gray-800)" }}
                          >
                            {role.role}
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {role.count}
                          </td>
                          <td>
                            <span className="pmc-badge pmc-status-approved">
                              {role.activeCount}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div
                    style={{
                      padding: "32px 16px",
                      textAlign: "center",
                      color: "var(--pmc-gray-500)",
                    }}
                  >
                    <Users
                      style={{
                        width: "32px",
                        height: "32px",
                        margin: "0 auto 12px",
                        opacity: 0.3,
                      }}
                    />
                    <p className="pmc-text-sm">No officers yet</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="pmc-card">
          <div className="pmc-card-header">
            <h2 className="pmc-card-title">Recent Trends</h2>
            <p className="pmc-card-subtitle">Last 7 days activity</p>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-table-container">
              <div className="pmc-table-responsive">
                {stats.applicationTrends &&
                stats.applicationTrends.length > 0 ? (
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
                          Date
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
                          Count
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {stats.applicationTrends.map((trend, index) => (
                        <tr key={index}>
                          <td
                            className="pmc-text-sm pmc-font-medium"
                            style={{ color: "var(--pmc-gray-800)" }}
                          >
                            {new Date(trend.date).toLocaleDateString()}
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {trend.status}
                          </td>
                          <td>
                            <span className="pmc-badge pmc-status-under-review">
                              {trend.count}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div
                    style={{
                      padding: "32px 16px",
                      textAlign: "center",
                      color: "var(--pmc-gray-500)",
                    }}
                  >
                    <Clock
                      style={{
                        width: "32px",
                        height: "32px",
                        margin: "0 auto 12px",
                        opacity: 0.3,
                      }}
                    />
                    <p className="pmc-text-sm">No recent activity</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
