import React from "react";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  itemsPerPage: number;
  onPageChange: (page: number) => void;
  showFirstLast?: boolean;
  showPageInfo?: boolean;
}

const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  totalItems,
  itemsPerPage,
  onPageChange,
  showFirstLast = true,
  showPageInfo = true,
}) => {
  const startItem = (currentPage - 1) * itemsPerPage + 1;
  const endItem = Math.min(currentPage * itemsPerPage, totalItems);

  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisiblePages = 5;

    if (totalPages <= maxVisiblePages) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (currentPage <= 3) {
        for (let i = 1; i <= 4; i++) {
          pages.push(i);
        }
        pages.push("...");
        pages.push(totalPages);
      } else if (currentPage >= totalPages - 2) {
        pages.push(1);
        pages.push("...");
        for (let i = totalPages - 3; i <= totalPages; i++) {
          pages.push(i);
        }
      } else {
        pages.push(1);
        pages.push("...");
        for (let i = currentPage - 1; i <= currentPage + 1; i++) {
          pages.push(i);
        }
        pages.push("...");
        pages.push(totalPages);
      }
    }

    return pages;
  };

  if (totalPages <= 1) return null;

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "16px 24px",
        borderTop: "1px solid var(--pmc-border)",
        background: "var(--pmc-gray-50)",
      }}
    >
      {/* Page Info */}
      {showPageInfo && (
        <div
          className="pmc-text-sm"
          style={{ color: "var(--pmc-gray-600)", fontWeight: 500 }}
        >
          Showing{" "}
          <span style={{ fontWeight: 700, color: "var(--pmc-gray-900)" }}>
            {startItem}
          </span>{" "}
          to{" "}
          <span style={{ fontWeight: 700, color: "var(--pmc-gray-900)" }}>
            {endItem}
          </span>{" "}
          of{" "}
          <span style={{ fontWeight: 700, color: "var(--pmc-gray-900)" }}>
            {totalItems}
          </span>{" "}
          entries
        </div>
      )}

      {/* Pagination Controls */}
      <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
        {/* First Page */}
        {showFirstLast && (
          <button
            onClick={() => onPageChange(1)}
            disabled={currentPage === 1}
            className="pmc-button pmc-button-sm"
            style={{
              padding: "8px",
              minWidth: "36px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              background: "white",
              border: "1px solid var(--pmc-border)",
              opacity: currentPage === 1 ? 0.4 : 1,
              cursor: currentPage === 1 ? "not-allowed" : "pointer",
            }}
            title="First page"
          >
            <ChevronsLeft style={{ width: "16px", height: "16px" }} />
          </button>
        )}

        {/* Previous Page */}
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="pmc-button pmc-button-sm"
          style={{
            padding: "8px 12px",
            display: "flex",
            alignItems: "center",
            gap: "4px",
            background: "white",
            border: "1px solid var(--pmc-border)",
            opacity: currentPage === 1 ? 0.4 : 1,
            cursor: currentPage === 1 ? "not-allowed" : "pointer",
          }}
          title="Previous page"
        >
          <ChevronLeft style={{ width: "16px", height: "16px" }} />
          <span className="pmc-text-sm pmc-font-medium">Previous</span>
        </button>

        {/* Page Numbers */}
        <div style={{ display: "flex", gap: "4px" }}>
          {getPageNumbers().map((page, index) => {
            if (page === "...") {
              return (
                <span
                  key={`ellipsis-${index}`}
                  style={{
                    padding: "8px 12px",
                    color: "var(--pmc-gray-400)",
                    fontSize: "14px",
                    fontWeight: 500,
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  ...
                </span>
              );
            }

            return (
              <button
                key={page}
                onClick={() => onPageChange(page as number)}
                className="pmc-button pmc-button-sm"
                style={{
                  padding: "8px 12px",
                  minWidth: "40px",
                  background:
                    currentPage === page
                      ? "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)"
                      : "white",
                  color: currentPage === page ? "white" : "var(--pmc-gray-700)",
                  border:
                    currentPage === page
                      ? "none"
                      : "1px solid var(--pmc-border)",
                  fontWeight: currentPage === page ? 700 : 500,
                  fontSize: "14px",
                  boxShadow:
                    currentPage === page
                      ? "0 2px 4px rgba(37, 99, 235, 0.2)"
                      : "none",
                  transition: "all 0.2s ease",
                }}
                onMouseEnter={(e) => {
                  if (currentPage !== page) {
                    e.currentTarget.style.background = "var(--pmc-gray-100)";
                    e.currentTarget.style.borderColor = "var(--pmc-primary)";
                  }
                }}
                onMouseLeave={(e) => {
                  if (currentPage !== page) {
                    e.currentTarget.style.background = "white";
                    e.currentTarget.style.borderColor = "var(--pmc-border)";
                  }
                }}
              >
                {page}
              </button>
            );
          })}
        </div>

        {/* Next Page */}
        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className="pmc-button pmc-button-sm"
          style={{
            padding: "8px 12px",
            display: "flex",
            alignItems: "center",
            gap: "4px",
            background: "white",
            border: "1px solid var(--pmc-border)",
            opacity: currentPage === totalPages ? 0.4 : 1,
            cursor: currentPage === totalPages ? "not-allowed" : "pointer",
          }}
          title="Next page"
        >
          <span className="pmc-text-sm pmc-font-medium">Next</span>
          <ChevronRight style={{ width: "16px", height: "16px" }} />
        </button>

        {/* Last Page */}
        {showFirstLast && (
          <button
            onClick={() => onPageChange(totalPages)}
            disabled={currentPage === totalPages}
            className="pmc-button pmc-button-sm"
            style={{
              padding: "8px",
              minWidth: "36px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              background: "white",
              border: "1px solid var(--pmc-border)",
              opacity: currentPage === totalPages ? 0.4 : 1,
              cursor: currentPage === totalPages ? "not-allowed" : "pointer",
            }}
            title="Last page"
          >
            <ChevronsRight style={{ width: "16px", height: "16px" }} />
          </button>
        )}
      </div>
    </div>
  );
};

export default Pagination;
