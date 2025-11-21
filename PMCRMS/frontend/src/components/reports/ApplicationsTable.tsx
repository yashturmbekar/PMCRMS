import React, { useState, useEffect } from "react";
import { Search, ChevronUp, ChevronDown, FileText, Eye } from "lucide-react";
import { useNavigate } from "react-router-dom";
import type { ReportApplication } from "../../types/reports";
import { PositionDisplayNames, StageDisplayNames } from "../../types/reports";
import { formatDisplayDate } from "../../utils/dateUtils";

interface ApplicationsTableProps {
  applications: ReportApplication[];
  positionType: string;
  stageName: string;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onSearch: (searchTerm: string) => void;
  onSort: (sortBy: string, sortDirection: "asc" | "desc") => void;
  isLoading?: boolean;
}

const ApplicationsTable: React.FC<ApplicationsTableProps> = ({
  applications,
  positionType,
  stageName,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  onSearch,
  onSort,
  isLoading = false,
}) => {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState<string>("createdDate");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");

  const totalPages = Math.ceil(totalCount / pageSize);

  const handleSort = (column: string) => {
    let newDirection: "asc" | "desc" = "desc";
    if (sortBy === column) {
      newDirection = sortDirection === "asc" ? "desc" : "asc";
    }
    setSortBy(column);
    setSortDirection(newDirection);
    onSort(column, newDirection);
  };

  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
  };

  // Debounced search
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      onSearch(searchTerm);
    }, 500);
    return () => clearTimeout(timeoutId);
  }, [searchTerm, onSearch]);

  const SortIcon: React.FC<{ column: string }> = ({ column }) => {
    if (sortBy !== column) {
      return (
        <div style={{ opacity: 0.3 }}>
          <ChevronUp style={{ width: "14px", height: "14px" }} />
        </div>
      );
    }
    return sortDirection === "asc" ? (
      <ChevronUp style={{ width: "14px", height: "14px", color: "#3b82f6" }} />
    ) : (
      <ChevronDown
        style={{ width: "14px", height: "14px", color: "#3b82f6" }}
      />
    );
  };

  const positionDisplayName =
    PositionDisplayNames[positionType] || positionType;
  const stageDisplayName = StageDisplayNames[stageName] || stageName;

  return (
    <div>
      {/* Header */}
      <div className="pmc-card-header">
        <h2 className="pmc-card-title">{positionDisplayName}</h2>
        <p className="pmc-card-subtitle">
          {stageDisplayName} • {totalCount}{" "}
          {totalCount === 1 ? "application" : "applications"}
        </p>
      </div>

      {/* Search Bar */}
      <div style={{ marginBottom: "24px" }}>
        <div style={{ position: "relative" }}>
          <Search
            style={{
              position: "absolute",
              left: "16px",
              top: "50%",
              transform: "translateY(-50%)",
              width: "20px",
              height: "20px",
              color: "var(--pmc-gray-400)",
              zIndex: 10,
            }}
          />
          <input
            type="text"
            placeholder="Search by Application ID or Name..."
            value={searchTerm}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="pmc-input"
            style={{ paddingLeft: "48px", width: "100%" }}
          />
        </div>
      </div>

      {/* Table */}
      <div className="pmc-card-body">
        {isLoading ? (
          <div style={{ padding: "48px", textAlign: "center" }}>
            <div className="pmc-loader" style={{ margin: "0 auto 16px" }}></div>
            <p style={{ color: "var(--pmc-gray-600)" }}>
              Loading applications...
            </p>
          </div>
        ) : applications.length === 0 ? (
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
                ? "Try adjusting your search criteria"
                : "There are no applications at this stage"}
            </p>
          </div>
        ) : (
          <>
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
                          cursor: "pointer",
                        }}
                        onClick={() => handleSort("applicationNumber")}
                      >
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          Application #
                          <SortIcon column="applicationNumber" />
                        </div>
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                          cursor: "pointer",
                        }}
                        onClick={() => handleSort("firstName")}
                      >
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          Applicant
                          <SortIcon column="firstName" />
                        </div>
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
                          cursor: "pointer",
                        }}
                        onClick={() => handleSort("createdDate")}
                      >
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          Submitted
                          <SortIcon column="createdDate" />
                        </div>
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
                      <tr key={app.applicationId}>
                        <td
                          className="pmc-text-sm pmc-font-medium"
                          style={{ color: "var(--pmc-primary)" }}
                        >
                          {app.applicationNumber}
                        </td>
                        <td className="pmc-text-sm">
                          {app.firstName} {app.lastName}
                        </td>
                        <td className="pmc-text-sm">
                          {PositionDisplayNames[app.positionType] ||
                            app.positionType}
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {formatDisplayDate(app.createdDate)}
                        </td>
                        <td>
                          <span className="pmc-status-badge">
                            {StageDisplayNames[app.currentStage] ||
                              app.currentStage}
                          </span>
                        </td>
                        <td>
                          <button
                            onClick={() =>
                              navigate(
                                `/admin/applications/${app.applicationId}`,
                                {
                                  state: {
                                    from: "reports",
                                    positionType,
                                    stageName,
                                    stageDisplayName,
                                  },
                                }
                              )
                            }
                            className="pmc-button pmc-button-secondary pmc-button-sm"
                            style={{
                              display: "flex",
                              alignItems: "center",
                              gap: "6px",
                            }}
                          >
                            <Eye style={{ width: "16px", height: "16px" }} />
                            View
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="pmc-table-footer">
                <div
                  className="pmc-text-sm"
                  style={{ color: "var(--pmc-gray-600)" }}
                >
                  Showing {(currentPage - 1) * pageSize + 1} to{" "}
                  {Math.min(currentPage * pageSize, totalCount)} of {totalCount}{" "}
                  applications
                </div>
                <div style={{ display: "flex", gap: "8px" }}>
                  <button
                    onClick={() => onPageChange(currentPage - 1)}
                    disabled={currentPage === 1}
                    className="pmc-button pmc-button-secondary pmc-button-sm"
                  >
                    Previous
                  </button>
                  {Array.from({ length: totalPages }, (_, i) => i + 1)
                    .filter(
                      (page) =>
                        page === 1 ||
                        page === totalPages ||
                        Math.abs(page - currentPage) <= 1
                    )
                    .map((page, idx, arr) => (
                      <React.Fragment key={page}>
                        {idx > 0 && arr[idx - 1] !== page - 1 && (
                          <span
                            style={{
                              padding: "0 4px",
                              color: "var(--pmc-gray-400)",
                            }}
                          >
                            ...
                          </span>
                        )}
                        <button
                          onClick={() => onPageChange(page)}
                          className={
                            page === currentPage
                              ? "pmc-button pmc-button-primary pmc-button-sm"
                              : "pmc-button pmc-button-secondary pmc-button-sm"
                          }
                        >
                          {page}
                        </button>
                      </React.Fragment>
                    ))}
                  <button
                    onClick={() => onPageChange(currentPage + 1)}
                    disabled={currentPage === totalPages}
                    className="pmc-button pmc-button-secondary pmc-button-sm"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default ApplicationsTable;
