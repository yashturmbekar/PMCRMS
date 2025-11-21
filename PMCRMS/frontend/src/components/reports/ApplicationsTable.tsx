import React, { useState, useEffect } from "react";
import {
  ArrowLeft,
  Search,
  ChevronUp,
  ChevronDown,
  Calendar,
  User,
  FileText,
  Eye,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import type { ReportApplication } from "../../types/reports";
import { PositionDisplayNames, StageDisplayNames } from "../../types/reports";
import { formatLocalDateTime, parseLocalDateTime } from "../../utils/dateUtils";

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
  onBack: () => void;
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
  onBack,
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
      <div className="mb-6">
        <button
          onClick={onBack}
          className="pmc-button pmc-button-secondary flex items-center gap-2 mb-4"
        >
          <ArrowLeft style={{ width: "16px", height: "16px" }} />
          Back to Stages
        </button>
        <div className="flex items-center justify-between">
          <div>
            <h2
              style={{ fontSize: "24px", fontWeight: "700", color: "#1f2937" }}
            >
              {positionDisplayName}
            </h2>
            <p style={{ fontSize: "14px", color: "#6b7280", marginTop: "4px" }}>
              {stageDisplayName} • {totalCount}{" "}
              {totalCount === 1 ? "application" : "applications"}
            </p>
          </div>
        </div>
      </div>

      {/* Search Bar */}
      <div className="pmc-card mb-6" style={{ padding: "16px" }}>
        <div style={{ position: "relative" }}>
          <Search
            style={{
              position: "absolute",
              left: "12px",
              top: "50%",
              transform: "translateY(-50%)",
              width: "20px",
              height: "20px",
              color: "#9ca3af",
            }}
          />
          <input
            type="text"
            placeholder="Search by Application ID or Name..."
            value={searchTerm}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="pmc-input"
            style={{ paddingLeft: "44px", width: "100%" }}
          />
        </div>
      </div>

      {/* Table */}
      <div className="pmc-card" style={{ overflow: "hidden" }}>
        {isLoading ? (
          <div style={{ padding: "48px", textAlign: "center" }}>
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p style={{ marginTop: "16px", color: "#6b7280" }}>
              Loading applications...
            </p>
          </div>
        ) : applications.length === 0 ? (
          <div style={{ padding: "48px", textAlign: "center" }}>
            <FileText
              style={{
                width: "48px",
                height: "48px",
                margin: "0 auto 16px",
                color: "#9ca3af",
              }}
            />
            <h3
              style={{ fontSize: "18px", fontWeight: "600", color: "#374151" }}
            >
              No Applications Found
            </h3>
            <p style={{ color: "#6b7280", marginTop: "8px" }}>
              {searchTerm
                ? "No applications match your search criteria."
                : "There are no applications at this stage."}
            </p>
          </div>
        ) : (
          <>
            <div style={{ overflowX: "auto" }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr
                    style={{
                      background: "#f9fafb",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                        cursor: "pointer",
                        userSelect: "none",
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
                        <Calendar style={{ width: "14px", height: "14px" }} />
                        Created Date
                        <SortIcon column="createdDate" />
                      </div>
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                        cursor: "pointer",
                        userSelect: "none",
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
                        <FileText style={{ width: "14px", height: "14px" }} />
                        Application ID
                        <SortIcon column="applicationNumber" />
                      </div>
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                        cursor: "pointer",
                        userSelect: "none",
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
                        <User style={{ width: "14px", height: "14px" }} />
                        First Name
                        <SortIcon column="firstName" />
                      </div>
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                        cursor: "pointer",
                        userSelect: "none",
                      }}
                      onClick={() => handleSort("lastName")}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "4px",
                        }}
                      >
                        Last Name
                        <SortIcon column="lastName" />
                      </div>
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                      }}
                    >
                      Position
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "left",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                      }}
                    >
                      Stage
                    </th>
                    <th
                      style={{
                        padding: "12px 16px",
                        textAlign: "center",
                        fontSize: "12px",
                        fontWeight: "600",
                        color: "#6b7280",
                        textTransform: "uppercase",
                      }}
                    >
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {applications.map((app) => (
                    <tr
                      key={app.applicationId}
                      style={{
                        borderBottom: "1px solid #e5e7eb",
                        transition: "background 0.15s",
                      }}
                      className="hover:bg-gray-50"
                    >
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span style={{ color: "#374151" }}>
                          {formatLocalDateTime(
                            parseLocalDateTime(app.createdDate)
                          )}
                        </span>
                      </td>
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span
                          style={{
                            fontFamily: "monospace",
                            fontWeight: "600",
                            color: "#3b82f6",
                          }}
                        >
                          {app.applicationNumber}
                        </span>
                      </td>
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span style={{ color: "#374151", fontWeight: "500" }}>
                          {app.firstName}
                        </span>
                      </td>
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span style={{ color: "#374151", fontWeight: "500" }}>
                          {app.lastName}
                        </span>
                      </td>
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span
                          style={{
                            display: "inline-block",
                            padding: "4px 12px",
                            borderRadius: "6px",
                            background: "#dbeafe",
                            color: "#1e40af",
                            fontSize: "12px",
                            fontWeight: "500",
                          }}
                        >
                          {PositionDisplayNames[app.positionType] ||
                            app.positionType}
                        </span>
                      </td>
                      <td style={{ padding: "16px", fontSize: "14px" }}>
                        <span style={{ color: "#6b7280" }}>
                          {StageDisplayNames[app.currentStage] ||
                            app.currentStage}
                        </span>
                      </td>
                      <td
                        style={{
                          padding: "16px",
                          fontSize: "14px",
                          textAlign: "center",
                        }}
                      >
                        <button
                          onClick={() =>
                            navigate(`/admin/applications/${app.applicationId}`)
                          }
                          className="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium transition-colors"
                          style={{
                            background: "#eff6ff",
                            color: "#3b82f6",
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.background = "#dbeafe";
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.background = "#eff6ff";
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

            {/* Pagination */}
            {totalPages > 1 && (
              <div
                style={{
                  padding: "16px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                }}
              >
                <div style={{ fontSize: "14px", color: "#6b7280" }}>
                  Showing {(currentPage - 1) * pageSize + 1} to{" "}
                  {Math.min(currentPage * pageSize, totalCount)} of {totalCount}{" "}
                  applications
                </div>
                <div style={{ display: "flex", gap: "8px" }}>
                  <button
                    onClick={() => onPageChange(currentPage - 1)}
                    disabled={currentPage === 1}
                    className="pmc-button pmc-button-secondary"
                    style={{
                      padding: "8px 16px",
                      fontSize: "14px",
                      opacity: currentPage === 1 ? 0.5 : 1,
                    }}
                  >
                    Previous
                  </button>
                  <div
                    style={{
                      display: "flex",
                      gap: "4px",
                      alignItems: "center",
                    }}
                  >
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
                              style={{ padding: "0 4px", color: "#9ca3af" }}
                            >
                              ...
                            </span>
                          )}
                          <button
                            onClick={() => onPageChange(page)}
                            className={
                              page === currentPage
                                ? "pmc-button pmc-button-primary"
                                : "pmc-button pmc-button-secondary"
                            }
                            style={{
                              padding: "8px 12px",
                              fontSize: "14px",
                              minWidth: "40px",
                            }}
                          >
                            {page}
                          </button>
                        </React.Fragment>
                      ))}
                  </div>
                  <button
                    onClick={() => onPageChange(currentPage + 1)}
                    disabled={currentPage === totalPages}
                    className="pmc-button pmc-button-secondary"
                    style={{
                      padding: "8px 16px",
                      fontSize: "14px",
                      opacity: currentPage === totalPages ? 0.5 : 1,
                    }}
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
