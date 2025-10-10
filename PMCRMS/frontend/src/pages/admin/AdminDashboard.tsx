import React, { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  adminService,
  type AdminDashboardStats,
} from "../../services/adminService";
import {
  UsersIcon,
  DocumentTextIcon,
  BanknotesIcon,
  CheckCircleIcon,
  ClockIcon,
  XCircleIcon,
  ArrowTrendingUpIcon,
} from "@heroicons/react/24/outline";

const AdminDashboard: React.FC = () => {
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
        setError(response.message || "Failed to load dashboard stats");
      }
    } catch (err) {
      console.error("Error loading dashboard stats:", err);
      setError("Failed to load dashboard statistics");
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          minHeight: "400px",
          padding: "24px",
        }}
      >
        <div style={{ textAlign: "center" }}>
          <div className="pmc-spinner" style={{ margin: "0 auto" }}></div>
          <p
            className="pmc-text-base"
            style={{ marginTop: "16px", color: "var(--pmc-gray-600)" }}
          >
            Loading dashboard...
          </p>
        </div>
      </div>
    );
  }

  if (error || !stats) {
    return (
      <div style={{ padding: "24px" }}>
        <div className="pmc-alert pmc-alert-error">
          <p>{error || "Failed to load data"}</p>
          <button
            onClick={loadDashboardStats}
            className="pmc-button pmc-button-error"
            style={{ marginTop: "8px" }}
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
      maximumFractionDigits: 0,
    }).format(amount);
  };

  return (
    <div style={{ padding: "32px" }}>
      {/* Header */}
      <div style={{ marginBottom: "32px" }}>
        <h1
          className="pmc-text-3xl"
          style={{ fontWeight: 700, color: "var(--pmc-gray-900)" }}
        >
          Admin Dashboard
        </h1>
        <p
          className="pmc-text-base"
          style={{ marginTop: "8px", color: "var(--pmc-gray-600)" }}
        >
          Manage officers, applications, and system configuration
        </p>
      </div>

      {/* Stats Grid */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
          gap: "24px",
          marginBottom: "32px",
        }}
      >
        {/* Total Applications */}
        <div className="pmc-card" style={{ borderLeft: "4px solid #3B82F6" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <div>
              <p
                className="pmc-text-sm"
                style={{ fontWeight: 500, color: "var(--pmc-gray-600)" }}
              >
                Total Applications
              </p>
              <p
                className="pmc-text-3xl"
                style={{
                  fontWeight: 700,
                  color: "var(--pmc-gray-900)",
                  marginTop: "8px",
                }}
              >
                {stats.totalApplications}
              </p>
            </div>
            <DocumentTextIcon
              style={{ height: "48px", width: "48px", color: "#3B82F6" }}
            />
          </div>
        </div>

        {/* Pending Applications */}
        <div className="pmc-card" style={{ borderLeft: "4px solid #EAB308" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <div>
              <p
                className="pmc-text-sm"
                style={{ fontWeight: 500, color: "var(--pmc-gray-600)" }}
              >
                Pending
              </p>
              <p
                className="pmc-text-3xl"
                style={{
                  fontWeight: 700,
                  color: "var(--pmc-gray-900)",
                  marginTop: "8px",
                }}
              >
                {stats.pendingApplications}
              </p>
            </div>
            <ClockIcon
              style={{ height: "48px", width: "48px", color: "#EAB308" }}
            />
          </div>
        </div>

        {/* Approved Applications */}
        <div className="pmc-card" style={{ borderLeft: "4px solid #22C55E" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <div>
              <p
                className="pmc-text-sm"
                style={{ fontWeight: 500, color: "var(--pmc-gray-600)" }}
              >
                Approved
              </p>
              <p
                className="pmc-text-3xl"
                style={{
                  fontWeight: 700,
                  color: "var(--pmc-gray-900)",
                  marginTop: "8px",
                }}
              >
                {stats.approvedApplications}
              </p>
            </div>
            <CheckCircleIcon
              style={{ height: "48px", width: "48px", color: "#22C55E" }}
            />
          </div>
        </div>

        {/* Rejected Applications */}
        <div className="pmc-card" style={{ borderLeft: "4px solid #EF4444" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <div>
              <p
                className="pmc-text-sm"
                style={{ fontWeight: 500, color: "var(--pmc-gray-600)" }}
              >
                Rejected
              </p>
              <p
                className="pmc-text-3xl"
                style={{
                  fontWeight: 700,
                  color: "var(--pmc-gray-900)",
                  marginTop: "8px",
                }}
              >
                {stats.rejectedApplications}
              </p>
            </div>
            <XCircleIcon
              style={{ height: "48px", width: "48px", color: "#EF4444" }}
            />
          </div>
        </div>
      </div>

      {/* Officer and Revenue Stats */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
          gap: "24px",
          marginBottom: "32px",
        }}
      >
        {/* Officers */}
        <div className="pmc-card">
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: "16px",
            }}
          >
            <h3
              className="pmc-text-lg"
              style={{ fontWeight: 600, color: "var(--pmc-gray-900)" }}
            >
              Officers
            </h3>
            <UsersIcon
              style={{
                height: "32px",
                width: "32px",
                color: "var(--pmc-primary)",
              }}
            />
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: "8px" }}>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span
                className="pmc-text-base"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                Total:
              </span>
              <span className="pmc-text-base" style={{ fontWeight: 600 }}>
                {stats.totalOfficers}
              </span>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span
                className="pmc-text-base"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                Active:
              </span>
              <span
                className="pmc-text-base"
                style={{ fontWeight: 600, color: "#22C55E" }}
              >
                {stats.activeOfficers}
              </span>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span
                className="pmc-text-base"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                Pending Invitations:
              </span>
              <span
                className="pmc-text-base"
                style={{ fontWeight: 600, color: "#EAB308" }}
              >
                {stats.pendingInvitations}
              </span>
            </div>
          </div>
          <Link
            to="/admin/officers"
            className="pmc-button pmc-button-primary"
            style={{
              marginTop: "16px",
              display: "block",
              textAlign: "center",
              textDecoration: "none",
            }}
          >
            Manage Officers
          </Link>
        </div>

        {/* Revenue */}
        <div className="pmc-card">
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              marginBottom: "16px",
            }}
          >
            <h3
              className="pmc-text-lg"
              style={{ fontWeight: 600, color: "var(--pmc-gray-900)" }}
            >
              Revenue
            </h3>
            <BanknotesIcon
              style={{ height: "32px", width: "32px", color: "#22C55E" }}
            />
          </div>
          <div
            style={{ display: "flex", flexDirection: "column", gap: "12px" }}
          >
            <div>
              <p
                className="pmc-text-sm"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                Total Collected
              </p>
              <p
                className="pmc-text-2xl"
                style={{ fontWeight: 700, color: "var(--pmc-gray-900)" }}
              >
                {formatCurrency(stats.totalRevenueCollected)}
              </p>
            </div>
            <div>
              <p
                className="pmc-text-sm"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                This Month
              </p>
              <p
                className="pmc-text-xl"
                style={{ fontWeight: 600, color: "#22C55E" }}
              >
                {formatCurrency(stats.revenueThisMonth)}
              </p>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="pmc-card">
          <h3
            className="pmc-text-lg"
            style={{
              fontWeight: 600,
              color: "var(--pmc-gray-900)",
              marginBottom: "16px",
            }}
          >
            Quick Actions
          </h3>
          <div style={{ display: "flex", flexDirection: "column", gap: "8px" }}>
            <Link
              to="/admin/officers/invite"
              className="pmc-button pmc-button-secondary"
              style={{ textAlign: "center", textDecoration: "none" }}
            >
              Invite Officer
            </Link>
            <Link
              to="/admin/forms"
              className="pmc-button pmc-button-success"
              style={{ textAlign: "center", textDecoration: "none" }}
            >
              Manage Forms
            </Link>
            <Link
              to="/admin/applications"
              className="pmc-button"
              style={{
                textAlign: "center",
                textDecoration: "none",
                background: "#A855F7",
                color: "#fff",
              }}
            >
              View All Applications
            </Link>
          </div>
        </div>
      </div>

      {/* Role Distribution */}
      {stats.roleDistribution && stats.roleDistribution.length > 0 && (
        <div className="pmc-card" style={{ marginBottom: "32px" }}>
          <h3 className="pmc-card-title">Officer Distribution by Role</h3>
          <div className="pmc-card-body">
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
                gap: "16px",
              }}
            >
              {stats.roleDistribution.map((role) => (
                <div
                  key={role.role}
                  style={{
                    border: "1px solid var(--pmc-gray-200)",
                    borderRadius: "8px",
                    padding: "16px",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <div>
                      <p
                        className="pmc-text-base"
                        style={{
                          fontWeight: 500,
                          color: "var(--pmc-gray-900)",
                        }}
                      >
                        {role.role}
                      </p>
                      <p
                        className="pmc-text-sm"
                        style={{
                          color: "var(--pmc-gray-600)",
                          marginTop: "4px",
                        }}
                      >
                        {role.activeCount} active / {role.count} total
                      </p>
                    </div>
                    <div style={{ textAlign: "right" }}>
                      <p
                        className="pmc-text-2xl"
                        style={{ fontWeight: 700, color: "var(--pmc-primary)" }}
                      >
                        {role.count}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Application Trends */}
      {stats.applicationTrends && stats.applicationTrends.length > 0 && (
        <div className="pmc-card">
          <div className="pmc-card-header">
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
              }}
            >
              <h3 className="pmc-card-title">
                Application Trends (Last 7 Days)
              </h3>
              <ArrowTrendingUpIcon
                style={{
                  height: "24px",
                  width: "24px",
                  color: "var(--pmc-gray-400)",
                }}
              />
            </div>
          </div>
          <div className="pmc-card-body">
            <div
              style={{ display: "flex", flexDirection: "column", gap: "8px" }}
            >
              {stats.applicationTrends.map((trend, index) => (
                <div
                  key={index}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    paddingTop: "8px",
                    paddingBottom: "8px",
                    borderBottom:
                      index < stats.applicationTrends.length - 1
                        ? "1px solid var(--pmc-gray-200)"
                        : "none",
                  }}
                >
                  <span
                    className="pmc-text-base"
                    style={{ color: "var(--pmc-gray-600)" }}
                  >
                    {new Date(trend.date).toLocaleDateString()}
                  </span>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "16px",
                    }}
                  >
                    <span
                      className="pmc-text-sm"
                      style={{ color: "var(--pmc-gray-500)" }}
                    >
                      {trend.status}
                    </span>
                    <span
                      className="pmc-text-base"
                      style={{ fontWeight: 600, color: "var(--pmc-gray-900)" }}
                    >
                      {trend.count} applications
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;
