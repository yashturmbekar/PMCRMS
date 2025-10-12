import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { eeWorkflowService } from "../services/eeWorkflowService";
import { Eye, CheckCircle, XCircle, Clock } from "lucide-react";
import { PageLoader } from "../components";
import { OTPVerificationModal } from "../components/workflow";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import type { EEWorkflowStatusDto } from "../types/eeceWorkflow";

const EEDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [pendingApplications, setPendingApplications] = useState<
    EEWorkflowStatusDto[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedApplication, setSelectedApplication] =
    useState<EEWorkflowStatusDto | null>(null);
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

  // Fetch dashboard data
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      console.log(`📊 Fetching EE Dashboard data - All position types`);

      const pending = await eeWorkflowService.getPendingApplications();
      setPendingApplications(pending);
      console.log(`✅ Loaded ${pending.length} pending applications`);
    } catch (error) {
      console.error("Error fetching EE dashboard data:", error);
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
  }, []);

  useEffect(() => {
    if (!user || !user.role.includes("Executive")) {
      navigate("/");
      return;
    }

    fetchData();
  }, [user, navigate, fetchData]);

  const handleVerifyDocuments = (application: EEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setShowOTPModal(true);
  };

  const handleRejectClick = (application: EEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setRejectionComments("");
    setShowRejectModal(true);
  };

  const handleGenerateOtp = async (
    applicationId: number
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      return await eeWorkflowService.generateOtpForSignature(applicationId);
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
      return await eeWorkflowService.verifyAndSignDocuments(applicationId, {
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
      const result = await eeWorkflowService.rejectApplication(
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
      EXECUTIVE_ENGINEER_PENDING: {
        color: "#7c3aed",
        bg: "#f5f3ff",
        label: "Pending Review",
      },
      CITY_ENGINEER_PENDING: {
        color: "#dc2626",
        bg: "#fef2f2",
        label: "Forwarded to CE",
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
          officerType="EE"
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
            Executive Engineer Dashboard
          </h1>
          <p style={{ color: "#64748b", fontSize: "15px" }}>
            Review and approve applications from all position types
          </p>
        </div>

        {/* Workflow Info Banner */}
        <div
          style={{
            marginBottom: "20px",
            padding: "14px 18px",
            background: "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)",
            borderRadius: "8px",
            border: "1px solid #6d28d9",
            display: "flex",
            alignItems: "center",
            gap: "12px",
          }}
        >
          <CheckCircle
            style={{ width: "22px", height: "22px", color: "white" }}
          />
          <div>
            <p
              style={{
                fontSize: "14px",
                color: "white",
                fontWeight: 600,
                margin: "0 0 4px 0",
              }}
            >
              ℹ️ Workflow Information
            </p>
            <p
              style={{
                fontSize: "13px",
                color: "rgba(255, 255, 255, 0.95)",
                margin: 0,
                lineHeight: "1.5",
              }}
            >
              After you verify documents and apply digital signature,
              applications automatically forward to City Engineer and are
              removed from this list. Rejected applications return to the
              applicant for corrections.
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
            <Clock
              style={{
                width: "48px",
                height: "48px",
                color: "#cbd5e1",
                margin: "0 auto 16px",
              }}
            />
            <p style={{ color: "#64748b", fontSize: "16px" }}>
              No pending applications
            </p>
            <p
              style={{
                color: "#94a3b8",
                fontSize: "14px",
                marginTop: "8px",
              }}
            >
              Applications will appear here when forwarded by Assistant
              Engineers
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
                  e.currentTarget.style.borderColor = "#7c3aed";
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
                      Approved by JE
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#334155",
                        fontWeight: 500,
                      }}
                    >
                      {application.assignedJEName || "N/A"}
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
                      Approved by AE
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#334155",
                        fontWeight: 500,
                      }}
                    >
                      {application.assignedAEName || "N/A"}
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
                        "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)",
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

export default EEDashboard;
