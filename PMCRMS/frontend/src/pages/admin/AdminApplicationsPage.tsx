import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { adminService } from "../../services/adminService";
import {
  FileText,
  Search,
  Eye,
  CheckCircle,
  XCircle,
  Clock,
  ArrowLeft,
  Home,
  ChevronRight,
} from "lucide-react";
import { PageLoader, Pagination } from "../../components";
import { parseLocalDateTime } from "../../utils/dateUtils";

interface ApplicationSummary {
  applicationId: number;
  applicationNumber: string;
  applicantName: string;
  applicationType: string;
  status: string;
  submittedOn: string;
}

const AdminApplicationsPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [applications, setApplications] = useState<ApplicationSummary[]>([]);
  const [filteredApplications, setFilteredApplications] = useState<
    ApplicationSummary[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>(
    searchParams.get("status") || "all"
  );
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(5);

  useEffect(() => {
    loadApplications();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    filterApplications();
    setCurrentPage(1); // Reset to first page when filters change
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [applications, searchTerm, statusFilter]);

  const loadApplications = async () => {
    try {
      setLoading(true);
      const response = await adminService.getAllApplications();
      if (response.success && response.data) {
        setApplications(response.data as ApplicationSummary[]);
      } else {
        setError(response.message || "Failed to load applications");
      }
    } catch (err) {
      console.error("Error loading applications:", err);
      setError("Failed to load applications");
    } finally {
      setLoading(false);
    }
  };

  const filterApplications = () => {
    let filtered = [...applications];

    if (statusFilter !== "all") {
      filtered = filtered.filter(
        (app) => app.status.toLowerCase() === statusFilter.toLowerCase()
      );
    }

    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(
        (app) =>
          app.applicantName.toLowerCase().includes(term) ||
          app.applicationNumber.toLowerCase().includes(term) ||
          app.applicationType.toLowerCase().includes(term)
      );
    }

    setFilteredApplications(filtered);
  };

  const handleStatusFilterChange = (status: string) => {
    setStatusFilter(status);
    if (status === "all") {
      searchParams.delete("status");
    } else {
      searchParams.set("status", status);
    }
    setSearchParams(searchParams);
  };

  const getStatusBadgeClass = (status: string) => {
    const statusLower = status.toLowerCase();
    if (statusLower === "approved") return "pmc-status-approved";
    if (statusLower === "rejected") return "pmc-status-rejected";
    if (statusLower === "pending" || statusLower === "under review")
      return "pmc-status-under-review";
    if (statusLower === "draft") return "pmc-status-draft";
    return "pmc-status-draft";
  };

  const getStatusIcon = (status: string) => {
    const statusLower = status.toLowerCase();
    if (statusLower === "approved")
      return <CheckCircle style={{ width: "14px", height: "14px" }} />;
    if (statusLower === "rejected")
      return <XCircle style={{ width: "14px", height: "14px" }} />;
    return <Clock style={{ width: "14px", height: "14px" }} />;
  };

  // Pagination logic
  const getCurrentPageData = () => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    return filteredApplications.slice(startIndex, endIndex);
  };

  const getTotalPages = () => {
    return Math.ceil(filteredApplications.length / itemsPerPage);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  if (loading) {
    return <PageLoader message="Loading Applications..." />;
  }

  return (
    <div className="pmc-fadeIn">
      {/* Breadcrumbs */}
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
          Applications
        </span>
      </div>

      {/* Header with Back Button */}
      <div className="pmc-content-header pmc-fadeInDown">
        <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
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
          <div>
            <h1
              className="pmc-content-title pmc-text-3xl pmc-font-bold"
              style={{ color: "var(--pmc-gray-900)", marginBottom: "4px" }}
            >
              Applications Management 📋
            </h1>
            <p
              className="pmc-content-subtitle pmc-text-base"
              style={{ color: "var(--pmc-gray-600)", margin: 0 }}
            >
              Review and manage all position applications
            </p>
          </div>
        </div>
      </div>

      <div
        className="pmc-card pmc-fadeInUp"
        style={{ marginBottom: "24px", padding: "20px" }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "16px",
            marginBottom: "16px",
          }}
        >
          <div style={{ position: "relative", flex: 1 }}>
            <Search
              style={{
                position: "absolute",
                left: "12px",
                top: "50%",
                transform: "translateY(-50%)",
                width: "18px",
                height: "18px",
                color: "var(--pmc-gray-400)",
              }}
            />
            <input
              type="text"
              placeholder="Search by applicant name, application number, or type..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pmc-input"
              style={{
                paddingLeft: "40px",
                width: "100%",
              }}
            />
          </div>
        </div>

        <div style={{ display: "flex", gap: "8px", flexWrap: "wrap" }}>
          <button
            onClick={() => handleStatusFilterChange("all")}
            className={`pmc-button ${
              statusFilter === "all"
                ? "pmc-button-primary"
                : "pmc-button-secondary"
            }`}
            style={{
              padding: "8px 16px",
              fontSize: "14px",
            }}
          >
            All ({applications.length})
          </button>
          <button
            onClick={() => handleStatusFilterChange("pending")}
            className={`pmc-button ${
              statusFilter === "pending"
                ? "pmc-button-primary"
                : "pmc-button-secondary"
            }`}
            style={{
              padding: "8px 16px",
              fontSize: "14px",
            }}
          >
            Pending (
            {
              applications.filter((a) => a.status.toLowerCase() === "pending")
                .length
            }
            )
          </button>
          <button
            onClick={() => handleStatusFilterChange("under review")}
            className={`pmc-button ${
              statusFilter === "under review"
                ? "pmc-button-primary"
                : "pmc-button-secondary"
            }`}
            style={{
              padding: "8px 16px",
              fontSize: "14px",
            }}
          >
            Under Review (
            {
              applications.filter(
                (a) => a.status.toLowerCase() === "under review"
              ).length
            }
            )
          </button>
          <button
            onClick={() => handleStatusFilterChange("approved")}
            className={`pmc-button ${
              statusFilter === "approved"
                ? "pmc-button-primary"
                : "pmc-button-secondary"
            }`}
            style={{
              padding: "8px 16px",
              fontSize: "14px",
            }}
          >
            Approved (
            {
              applications.filter((a) => a.status.toLowerCase() === "approved")
                .length
            }
            )
          </button>
          <button
            onClick={() => handleStatusFilterChange("rejected")}
            className={`pmc-button ${
              statusFilter === "rejected"
                ? "pmc-button-primary"
                : "pmc-button-secondary"
            }`}
            style={{
              padding: "8px 16px",
              fontSize: "14px",
            }}
          >
            Rejected (
            {
              applications.filter((a) => a.status.toLowerCase() === "rejected")
                .length
            }
            )
          </button>
        </div>
      </div>

      <div className="pmc-card pmc-slideInLeft">
        <div className="pmc-card-header">
          <h2 className="pmc-card-title">
            {statusFilter === "all"
              ? "All Applications"
              : `${
                  statusFilter.charAt(0).toUpperCase() + statusFilter.slice(1)
                } Applications`}
          </h2>
          <p className="pmc-card-subtitle">
            {filteredApplications.length} application
            {filteredApplications.length !== 1 ? "s" : ""} found
          </p>
        </div>
        <div className="pmc-card-body">
          {error ? (
            <div
              style={{
                padding: "32px",
                textAlign: "center",
                background: "#fee2e2",
                borderRadius: "8px",
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
                {error}
              </p>
              <button
                onClick={loadApplications}
                className="pmc-button pmc-button-primary"
              >
                Retry
              </button>
            </div>
          ) : filteredApplications.length === 0 ? (
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
                {searchTerm
                  ? "Try adjusting your search or filters"
                  : statusFilter !== "all"
                  ? "No applications with this status"
                  : "Applications will appear here once users submit them"}
              </p>
            </div>
          ) : (
            <div className="pmc-table-container">
              <div className="pmc-table-responsive">
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
                    {getCurrentPageData().map((app) => (
                      <tr key={app.applicationId}>
                        <td
                          className="pmc-text-sm pmc-font-medium"
                          style={{ color: "var(--pmc-primary)" }}
                        >
                          {app.applicationNumber}
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
                            className={`pmc-badge ${getStatusBadgeClass(
                              app.status
                            )}`}
                          >
                            <span
                              style={{
                                display: "flex",
                                alignItems: "center",
                                gap: "4px",
                              }}
                            >
                              {getStatusIcon(app.status)}
                              {app.status}
                            </span>
                          </span>
                        </td>
                        <td>
                          <div style={{ display: "flex", gap: "8px" }}>
                            <button
                              onClick={() =>
                                navigate(
                                  `/admin/applications/${app.applicationId}`
                                )
                              }
                              className="pmc-button pmc-button-primary"
                              title="View Application Details"
                              style={{
                                padding: "6px 12px",
                                fontSize: "13px",
                                display: "inline-flex",
                                alignItems: "center",
                                gap: "6px",
                              }}
                            >
                              <Eye style={{ width: "14px", height: "14px" }} />
                              View
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Pagination */}
                {filteredApplications.length > 0 && (
                  <Pagination
                    currentPage={currentPage}
                    totalPages={getTotalPages()}
                    totalItems={filteredApplications.length}
                    itemsPerPage={itemsPerPage}
                    onPageChange={handlePageChange}
                    showFirstLast={true}
                    showPageInfo={true}
                  />
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminApplicationsPage;
