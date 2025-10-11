import React, { useState } from "react";
import { X, CheckCircle, FileText } from "lucide-react";
import { jeWorkflowService } from "../../services/jeWorkflowService";
import NotificationModal from "../common/NotificationModal";
import type { NotificationType } from "../common/NotificationModal";

interface Document {
  id: number;
  documentTypeName: string;
  fileName: string;
  fileSize?: number;
  isVerified: boolean;
}

interface DocumentApprovalModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId: number;
  documents: Document[];
  onApprovalComplete?: () => void;
}

const DocumentApprovalModal: React.FC<DocumentApprovalModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  documents,
  onApprovalComplete,
}) => {
  const [comments, setComments] = useState("");
  const [selectedDocuments, setSelectedDocuments] = useState<Set<number>>(
    new Set()
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notification, setNotification] = useState<{
    isOpen: boolean;
    message: string;
    type: NotificationType;
    title: string;
    autoClose?: boolean;
  }>({
    isOpen: false,
    message: "",
    type: "success",
    title: "",
  });

  if (!isOpen) return null;

  const handleDocumentToggle = (documentId: number) => {
    const newSelected = new Set(selectedDocuments);
    if (newSelected.has(documentId)) {
      newSelected.delete(documentId);
    } else {
      newSelected.add(documentId);
    }
    setSelectedDocuments(newSelected);
  };

  const handleApproveAll = () => {
    const allDocIds = documents
      .filter((doc) => doc.documentTypeName !== "RecommendedForm")
      .map((doc) => doc.id);
    setSelectedDocuments(new Set(allDocIds));
  };

  const handleSubmit = async () => {
    if (selectedDocuments.size === 0) {
      setNotification({
        isOpen: true,
        message: "Please select at least one document to approve",
        type: "warning",
        title: "No Documents Selected",
        autoClose: false,
      });
      return;
    }

    try {
      setIsSubmitting(true);

      // Approve each selected document
      for (const docId of Array.from(selectedDocuments)) {
        await jeWorkflowService.verifyDocument({
          applicationId,
          documentId: docId,
          isApproved: true,
          remarks: comments,
        });
      }

      setNotification({
        isOpen: true,
        message: "The selected documents have been approved successfully!",
        type: "success",
        title: "Documents Approved Successfully!",
        autoClose: true,
      });

      // Close modal and refresh after notification
      setTimeout(() => {
        onApprovalComplete?.();
        onClose();
      }, 2000);
    } catch (error) {
      console.error("Error approving documents:", error);
      setNotification({
        isOpen: true,
        message: "Failed to approve documents. Please try again.",
        type: "error",
        title: "Approval Failed",
        autoClose: false,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <NotificationModal
        isOpen={notification.isOpen}
        onClose={() => setNotification({ ...notification, isOpen: false })}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        autoClose={notification.autoClose}
        autoCloseDuration={2000}
      />
      <div
        className="pmc-modal-overlay"
        onClick={onClose}
        style={{
          position: "fixed",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: "rgba(0, 0, 0, 0.5)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          zIndex: 1000,
          padding: "20px",
        }}
      >
        <div
          className="pmc-modal pmc-slideInUp"
          onClick={(e) => e.stopPropagation()}
          style={{
            background: "white",
            borderRadius: "8px",
            maxWidth: "520px",
            width: "100%",
            maxHeight: "90vh",
            overflow: "hidden",
            display: "flex",
            flexDirection: "column",
            boxShadow:
              "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)",
          }}
        >
          {/* Header */}
          <div
            className="pmc-modal-header"
            style={{
              padding: "14px 18px",
              borderBottom: "1px solid #e5e7eb",
              background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              flexShrink: 0,
            }}
          >
            <h3
              style={{
                color: "white",
                margin: 0,
                fontSize: "17px",
                fontWeight: "600",
              }}
            >
              Document Approve
            </h3>
            <button
              onClick={onClose}
              style={{
                background: "transparent",
                border: "none",
                cursor: "pointer",
                padding: "4px",
              }}
            >
              <X style={{ width: "20px", height: "20px", color: "white" }} />
            </button>
          </div>

          {/* Body - Scrollable */}
          <div
            className="pmc-modal-body"
            style={{
              padding: "16px 18px",
              overflowY: "auto",
              flexGrow: 1,
            }}
          >
            {/* Comments */}
            <div style={{ marginBottom: "16px" }}>
              <label
                className="pmc-label"
                style={{
                  display: "block",
                  marginBottom: "6px",
                  fontWeight: 600,
                  color: "#334155",
                  fontSize: "13px",
                }}
              >
                Comments
              </label>
              <textarea
                placeholder="Enter comments"
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                style={{
                  width: "100%",
                  minHeight: "60px",
                  padding: "8px 10px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "13px",
                  fontFamily: "inherit",
                  resize: "vertical",
                }}
              />
            </div>

            {/* Document List */}
            <div>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  marginBottom: "8px",
                }}
              >
                <label
                  className="pmc-label"
                  style={{
                    fontWeight: 600,
                    color: "#334155",
                    margin: 0,
                    fontSize: "13px",
                  }}
                >
                  Select Documents to Approve
                </label>
                <button
                  onClick={handleApproveAll}
                  className="pmc-button pmc-button-sm pmc-button-outline"
                  style={{
                    fontSize: "11px",
                    padding: "4px 8px",
                  }}
                >
                  Select All
                </button>
              </div>

              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(2, 1fr)",
                  gap: "6px",
                  marginBottom: "10px",
                }}
              >
                {documents
                  .filter((doc) => doc.documentTypeName !== "RecommendedForm")
                  .map((doc) => (
                    <div
                      key={doc.id}
                      onClick={() => handleDocumentToggle(doc.id)}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        padding: "8px 10px",
                        background: selectedDocuments.has(doc.id)
                          ? "#f0f9ff"
                          : "#f8fafc",
                        border: selectedDocuments.has(doc.id)
                          ? "2px solid #3b82f6"
                          : "1px solid #e2e8f0",
                        borderRadius: "6px",
                        cursor: "pointer",
                        transition: "all 0.2s ease",
                      }}
                    >
                      <div
                        style={{
                          width: "16px",
                          height: "16px",
                          border: "2px solid #cbd5e1",
                          borderRadius: "3px",
                          marginRight: "8px",
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          background: selectedDocuments.has(doc.id)
                            ? "#3b82f6"
                            : "white",
                          flexShrink: 0,
                        }}
                      >
                        {selectedDocuments.has(doc.id) && (
                          <CheckCircle
                            style={{
                              width: "12px",
                              height: "12px",
                              color: "white",
                            }}
                          />
                        )}
                      </div>

                      <FileText
                        style={{
                          width: "16px",
                          height: "16px",
                          color: "#3b82f6",
                          marginRight: "8px",
                          flexShrink: 0,
                        }}
                      />

                      <div style={{ flex: 1, minWidth: 0 }}>
                        <p
                          style={{
                            fontWeight: 500,
                            color: "#1e293b",
                            marginBottom: "1px",
                            fontSize: "13px",
                          }}
                        >
                          {doc.documentTypeName}
                        </p>
                        <p
                          style={{
                            fontSize: "11px",
                            color: "#64748b",
                            whiteSpace: "nowrap",
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                          }}
                        >
                          {doc.fileName}
                        </p>
                      </div>

                      {doc.isVerified && (
                        <span
                          className="pmc-badge pmc-badge-success"
                          style={{
                            fontSize: "9px",
                            padding: "2px 5px",
                            flexShrink: 0,
                          }}
                        >
                          Verified
                        </span>
                      )}
                    </div>
                  ))}
              </div>

              {/* Selected Count */}
              {selectedDocuments.size > 0 && (
                <div
                  style={{
                    padding: "8px 10px",
                    background: "#eff6ff",
                    border: "1px solid #bfdbfe",
                    borderRadius: "6px",
                  }}
                >
                  <p style={{ fontSize: "12px", color: "#1e40af", margin: 0 }}>
                    <strong>{selectedDocuments.size}</strong> document
                    {selectedDocuments.size !== 1 ? "s" : ""} selected for
                    approval
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* Footer */}
          <div
            className="pmc-modal-footer"
            style={{
              padding: "12px 18px",
              borderTop: "1px solid #e5e7eb",
              display: "flex",
              gap: "8px",
              justifyContent: "flex-end",
              background: "#f9fafb",
              flexShrink: 0,
            }}
          >
            <button
              className="pmc-button pmc-button-outline"
              onClick={onClose}
              disabled={isSubmitting}
              style={{
                minWidth: "80px",
                padding: "7px 14px",
                fontSize: "13px",
              }}
            >
              Cancel
            </button>
            <button
              className="pmc-button pmc-button-success"
              onClick={handleSubmit}
              disabled={isSubmitting || selectedDocuments.size === 0}
              style={{
                minWidth: "100px",
                padding: "7px 14px",
                fontSize: "13px",
              }}
            >
              {isSubmitting ? "Submitting..." : "SUBMIT"}
            </button>
          </div>
        </div>
      </div>
    </>
  );
};

export default DocumentApprovalModal;
