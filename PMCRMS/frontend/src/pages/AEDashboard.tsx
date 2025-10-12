import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { aeWorkflowService } from "../services/aeWorkflowService";
import { Eye, CheckCircle, XCircle, Clock, Filter } from "lucide-react";
import { PageLoader } from "../components";
import { OTPVerificationModal } from "../components/workflow";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import type { AEWorkflowStatusDto } from "../types/aeWorkflow";

type PositionType =
  | "Architect"
  | "StructuralEngineer"
  | "LicenceEngineer"
  | "Supervisor1"
  | "Supervisor2";

const AEDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [pendingApplications, setPendingApplications] = useState<
    AEWorkflowStatusDto[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [selectedPositionType, setSelectedPositionType] =
    useState<PositionType>("Architect");
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedApplication, setSelectedApplication] =
    useState<AEWorkflowStatusDto | null>(null);
  const [rejectionComments, setRejectionComments] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);
  const [notification, setNotification] = useState<{
    isOpen: boolean;
    message: string;
    type: NotificationType;
    title: string;
    autoClose?: boolean;
  }>({
    isOpen: false,
    message: "",
    type: "info",
    title: "",
    autoClose: false,
  });

  // Position type options
  const positionTypes: PositionType[] = [
    "Architect",
    "StructuralEngineer",
    "LicenceEngineer",
    "Supervisor1",
    "Supervisor2",
  ];

  // Position type display labels
  const getPositionLabel = (positionType: PositionType): string => {
    const labels: Record<PositionType, string> = {
      Architect: "Architect",
      StructuralEngineer: "Structural Engineer",
      LicenceEngineer: "Licence Engineer",
      Supervisor1: "Supervisor 1",
      Supervisor2: "Supervisor 2",
    };
    return labels[positionType];
  };

  // Fetch dashboard data
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      console.log(
        `📊 Fetching AE Dashboard pending applications for position: ${selectedPositionType}`
      );

      const pending = await aeWorkflowService.getPendingApplications(
        selectedPositionType
      );
      setPendingApplications(pending);
      console.log(`✅ Loaded ${pending.length} pending applications`);
    } catch (error) {
      console.error("Error fetching AE dashboard data:", error);
      setNotification({
        isOpen: true,
        message: "Failed to load applications. Please try again.",
        type: "error",
        title: "Loading Failed",
        autoClose: false,
      });
    } finally {
      setLoading(false);
    }
  }, [selectedPositionType]);

  useEffect(() => {
    if (!user || !user.role.includes("Assistant")) {
      navigate("/");
      return;
    }

    fetchData();
  }, [user, navigate, fetchData]);

  const handleVerifyDocuments = (application: AEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setShowOTPModal(true);
  };

  const handleRejectClick = (application: AEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setRejectionComments("");
    setShowRejectModal(true);
  };

  const handleGenerateOtp = async (
    applicationId: number
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      return await aeWorkflowService.generateOtpForSignature(applicationId);
    } catch (error) {
      console.error("Error generating OTP:", error);
      return {
        success: false,
        message: "Failed to generate OTP",
      };
    }
  };

  const handleVerifyAndSign = async (
    applicationId: number,
    otp: string,
    comments?: string
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      return await aeWorkflowService.verifyAndSignDocuments(applicationId, {
        positionType: selectedPositionType,
        otp,
        comments,
      });
    } catch (error) {
      console.error("Error verifying and signing:", error);
      return {
        success: false,
        message: "Failed to verify and sign documents",
      };
    }
  };

  const handleRejectApplication = async () => {
    if (!rejectionComments.trim()) {
      setNotification({
        isOpen: true,
        message: "Rejection comments are mandatory",
        type: "warning",
        title: "Missing Comments",
        autoClose: false,
      });
      return;
    }

    if (!selectedApplication) return;

    try {
      setIsRejecting(true);
      const result = await aeWorkflowService.rejectApplication(
        selectedApplication.applicationId,
        { rejectionComments: rejectionComments }
      );

      if (result.success) {
        setNotification({
          isOpen: true,
          message: "Application rejected successfully",
          type: "success",
          title: "Application Rejected",
          autoClose: true,
        });

        setShowRejectModal(false);
        setRejectionComments("");
        setSelectedApplication(null);
        fetchData(); // Refresh list
      } else {
        setNotification({
          isOpen: true,
          message: result.message || "Failed to reject application",
          type: "error",
          title: "Rejection Failed",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error rejecting application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to reject application. Please try again.",
        type: "error",
        title: "Rejection Failed",
        autoClose: false,
      });
    } finally {
      setIsRejecting(false);
    }
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return "N/A";
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<
      string,
      { color: string; bg: string; label: string }
    > = {
      ASSISTANT_ENGINEER_PENDING: {
        color: "#2563eb",
        bg: "#eff6ff",
        label: "Pending Review",
      },
      EXECUTIVE_ENGINEER_PENDING: {
        color: "#7c3aed",
        bg: "#f5f3ff",
        label: "Forwarded to EE",
      },
      REJECTED: { color: "#dc2626", bg: "#fef2f2", label: "Rejected" },
    };

    const config = statusConfig[status] || {
      color: "#64748b",
      bg: "#f1f5f9",
      label: status,
    };

    return (
      <span
        style={{
          padding: "4px 12px",
          borderRadius: "12px",
          fontSize: "13px",
          fontWeight: 600,
          color: config.color,
          background: config.bg,
        }}
      >
        {config.label}
      </span>
    );
  };

  if (loading) {
    return <PageLoader />;
  }

  return (
    <>
      <NotificationModal
        isOpen={notification.isOpen}
        onClose={() => setNotification({ ...notification, isOpen: false })}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        autoClose={notification.autoClose}
        autoCloseDuration={3000}
      />

      {showOTPModal && selectedApplication && (
        <OTPVerificationModal
          isOpen={showOTPModal}
          onClose={() => {
            setShowOTPModal(false);
            setSelectedApplication(null);
          }}
          applicationId={selectedApplication.applicationId}
          officerType="AE"
          positionType={selectedPositionType}
          onGenerateOtp={handleGenerateOtp}
          onVerifyAndSign={handleVerifyAndSign}
          onSuccess={() => fetchData()}
        />
      )}

      {/* Reject Modal */}
      {showRejectModal && selectedApplication && (
        <div
          className="pmc-modal-overlay"
          onClick={() => setShowRejectModal(false)}
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
              maxWidth: "500px",
              width: "100%",
            }}
          >
            <div
              style={{
                padding: "16px 20px",
                borderBottom: "1px solid #e5e7eb",
                background: "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
              }}
            >
              <h3 style={{ color: "white", margin: 0, fontSize: "17px" }}>
                Reject Application
              </h3>
            </div>

            <div style={{ padding: "20px" }}>
              <p style={{ marginBottom: "12px", color: "#475569" }}>
                <strong>Application:</strong>{" "}
                {selectedApplication.applicationNumber}
              </p>
              <p style={{ marginBottom: "16px", color: "#475569" }}>
                <strong>Applicant:</strong> {selectedApplication.applicantName}
              </p>

              <label
                style={{
                  display: "block",
                  marginBottom: "8px",
                  fontWeight: 600,
                  color: "#334155",
                }}
              >
                Rejection Comments <span style={{ color: "#dc2626" }}>*</span>
              </label>
              <textarea
                placeholder="Enter detailed rejection comments (mandatory)"
                value={rejectionComments}
                onChange={(e) => setRejectionComments(e.target.value)}
                style={{
                  width: "100%",
                  minHeight: "100px",
                  padding: "10px 12px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  fontSize: "14px",
                  fontFamily: "inherit",
                  resize: "vertical",
                }}
              />
            </div>

            <div
              style={{
                padding: "14px 20px",
                borderTop: "1px solid #e5e7eb",
                display: "flex",
                gap: "10px",
                justifyContent: "flex-end",
                background: "#f9fafb",
              }}
            >
              <button
                onClick={() => setShowRejectModal(false)}
                disabled={isRejecting}
                style={{
                  padding: "8px 16px",
                  fontSize: "14px",
                  background: "white",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  cursor: "pointer",
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleRejectApplication}
                disabled={isRejecting || !rejectionComments.trim()}
                style={{
                  padding: "8px 20px",
                  fontSize: "14px",
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                  color: "white",
                  border: "none",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontWeight: 600,
                }}
              >
                {isRejecting ? "Rejecting..." : "Reject Application"}
              </button>
            </div>
          </div>
        </div>
      )}

      <div style={{ padding: "24px", maxWidth: "1400px", margin: "0 auto" }}>
        {/* Header */}
        <div style={{ marginBottom: "24px" }}>
          <h1
            style={{
              fontSize: "28px",
              fontWeight: 700,
              color: "#1e293b",
              marginBottom: "8px",
            }}
          >
            Assistant Engineer Dashboard
          </h1>
          <p style={{ color: "#64748b", fontSize: "15px" }}>
            Review and approve applications for position-based verification
          </p>
        </div>

        {/* Position Type Filter */}
        <div
          style={{
            marginBottom: "20px",
            padding: "16px 20px",
            background: "white",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            gap: "12px",
          }}
        >
          <Filter style={{ width: "20px", height: "20px", color: "#64748b" }} />
          <label
            style={{ fontWeight: 600, color: "#334155", fontSize: "14px" }}
          >
            Filter by Position Type:
          </label>
          <select
            value={selectedPositionType}
            onChange={(e) =>
              setSelectedPositionType(e.target.value as PositionType)
            }
            style={{
              padding: "8px 12px",
              border: "1px solid #d1d5db",
              borderRadius: "6px",
              fontSize: "14px",
              cursor: "pointer",
              background: "white",
            }}
          >
            {positionTypes.map((type) => (
              <option key={type} value={type}>
                {getPositionLabel(type)}
              </option>
            ))}
          </select>
        </div>

        {/* Auto-Forward Information Banner */}
        <div
          style={{
            padding: "14px 18px",
            background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
            borderRadius: "8px",
            marginBottom: "20px",
            display: "flex",
            alignItems: "center",
            gap: "12px",
            boxShadow: "0 2px 4px rgba(59, 130, 246, 0.1)",
          }}
        >
          <div
            style={{
              width: "40px",
              height: "40px",
              background: "rgba(255, 255, 255, 0.2)",
              borderRadius: "50%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            <CheckCircle
              style={{ width: "24px", height: "24px", color: "white" }}
            />
          </div>
          <div style={{ flex: 1 }}>
            <p
              style={{
                color: "white",
                fontSize: "14px",
                fontWeight: 500,
                margin: 0,
                lineHeight: 1.5,
              }}
            >
              ℹ️ <strong>Workflow:</strong> After you verify documents and apply
              digital signature, applications automatically forward to{" "}
              <strong>Executive Engineer</strong> and are removed from this
              list. Rejected applications return to the applicant for
              corrections.
            </p>
          </div>
        </div>

        {/* Applications List */}
        {pendingApplications.length === 0 ? (
          <div
            style={{
              padding: "60px 20px",
              textAlign: "center",
              background: "white",
              borderRadius: "8px",
              border: "1px solid #e5e7eb",
            }}
          >
            <div style={{ marginBottom: "12px" }}>
              <Clock
                style={{
                  width: "48px",
                  height: "48px",
                  color: "#cbd5e1",
                  margin: "0 auto",
                }}
              />
            </div>
            <p style={{ color: "#64748b", fontSize: "16px", fontWeight: 500 }}>
              No pending applications for{" "}
              {getPositionLabel(selectedPositionType)} position
            </p>
            <p style={{ color: "#94a3b8", fontSize: "14px", marginTop: "8px" }}>
              Applications will appear here when forwarded by Junior Engineers
            </p>
          </div>
        ) : (
          <div
            style={{ display: "flex", flexDirection: "column", gap: "12px" }}
          >
            {pendingApplications.map((application) => (
              <div
                key={application.applicationId}
                style={{
                  background: "white",
                  padding: "20px",
                  borderRadius: "8px",
                  border: "1px solid #e5e7eb",
                  transition: "all 0.2s",
                  boxShadow: "0 1px 3px 0 rgba(0, 0, 0, 0.1)",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.boxShadow =
                    "0 4px 6px -1px rgba(0, 0, 0, 0.1)";
                  e.currentTarget.style.borderColor = "#3b82f6";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.boxShadow =
                    "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
                  e.currentTarget.style.borderColor = "#e5e7eb";
                }}
              >
                <div
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "flex-start",
                    marginBottom: "16px",
                  }}
                >
                  <div>
                    <h3
                      style={{
                        fontSize: "17px",
                        fontWeight: 600,
                        color: "#1e293b",
                        marginBottom: "4px",
                      }}
                    >
                      {application.applicationNumber}
                    </h3>
                    <p style={{ color: "#64748b", fontSize: "14px" }}>
                      {application.applicantName}
                    </p>
                  </div>
                  {getStatusBadge(application.status)}
                </div>

                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
                    gap: "12px",
                    marginBottom: "16px",
                  }}
                >
                  <div>
                    <p
                      style={{
                        fontSize: "12px",
                        color: "#94a3b8",
                        marginBottom: "4px",
                      }}
                    >
                      Position Type
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#334155",
                        fontWeight: 500,
                      }}
                    >
                      {application.positionType}
                    </p>
                  </div>
                  <div>
                    <p
                      style={{
                        fontSize: "12px",
                        color: "#94a3b8",
                        marginBottom: "4px",
                      }}
                    >
                      Assigned to AE
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#334155",
                        fontWeight: 500,
                      }}
                    >
                      {application.assignedToAEName || "N/A"}
                    </p>
                  </div>
                  <div>
                    <p
                      style={{
                        fontSize: "12px",
                        color: "#94a3b8",
                        marginBottom: "4px",
                      }}
                    >
                      Created Date
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#334155",
                        fontWeight: 500,
                      }}
                    >
                      {formatDate(application.createdDate)}
                    </p>
                  </div>
                </div>

                {/* Action Buttons */}
                <div
                  style={{ display: "flex", gap: "10px", marginTop: "16px" }}
                >
                  <button
                    onClick={() =>
                      navigate(
                        `/position-registration-application-details/${application.applicationId}`
                      )
                    }
                    style={{
                      padding: "8px 16px",
                      background: "white",
                      border: "1px solid #d1d5db",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 500,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <Eye style={{ width: "16px", height: "16px" }} />
                    View Details
                  </button>
                  <button
                    onClick={() => handleVerifyDocuments(application)}
                    style={{
                      padding: "8px 20px",
                      background:
                        "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                      color: "white",
                      border: "none",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 600,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <CheckCircle style={{ width: "16px", height: "16px" }} />
                    Verify Documents
                  </button>
                  <button
                    onClick={() => handleRejectClick(application)}
                    style={{
                      padding: "8px 16px",
                      background:
                        "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                      color: "white",
                      border: "none",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 600,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <XCircle style={{ width: "16px", height: "16px" }} />
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </>
  );
};

export default AEDashboard;
