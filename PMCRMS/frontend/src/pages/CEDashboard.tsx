import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { ceWorkflowService } from "../services/ceWorkflowService";
import { Eye, CheckCircle, XCircle, Award, AlertCircle } from "lucide-react";
import { PageLoader } from "../components";
import { OTPVerificationModal } from "../components/workflow";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import type { CEWorkflowStatusDto } from "../types/eeceWorkflow";

const CEDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [pendingApplications, setPendingApplications] = useState<
    CEWorkflowStatusDto[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedApplication, setSelectedApplication] =
    useState<CEWorkflowStatusDto | null>(null);
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
      console.log(`📊 Fetching CE Dashboard data - FINAL APPROVAL Queue`);

      const pending = await ceWorkflowService.getPendingApplications();
      setPendingApplications(pending);
      console.log(`✅ Loaded ${pending.length} pending applications`);
    } catch (error) {
      console.error("Error fetching CE dashboard data:", error);
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
    if (!user || !user.role.includes("City")) {
      navigate("/");
      return;
    }

    fetchData();
  }, [user, navigate, fetchData]);

  const handleFinalApproval = (application: CEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setShowOTPModal(true);
  };

  const handleRejectClick = (application: CEWorkflowStatusDto) => {
    setSelectedApplication(application);
    setRejectionComments("");
    setShowRejectModal(true);
  };

  const handleGenerateOtp = async (
    applicationId: number
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      return await ceWorkflowService.generateOtpForSignature(applicationId);
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
      return await ceWorkflowService.verifyAndSignDocuments(applicationId, {
        otp,
        comments,
      });
    } catch (error) {
      console.error("Error in final approval:", error);
      return {
        success: false,
        message: "Failed to approve application",
      };
    }
  };

  const handleRejectApplication = async () => {
    if (!rejectionComments.trim()) {
      setNotification({
        isOpen: true,
        message: "Rejection comments are mandatory for FINAL REJECTION",
        type: "warning",
        title: "Missing Comments",
        autoClose: false,
      });
      return;
    }

    if (!selectedApplication) return;

    try {
      setIsRejecting(true);
      const result = await ceWorkflowService.rejectApplication(
        selectedApplication.applicationId,
        { rejectionComments: rejectionComments }
      );

      if (result.success) {
        setNotification({
          isOpen: true,
          message: "Application FINALLY REJECTED",
          type: "error",
          title: "Final Rejection Applied",
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
      CITY_ENGINEER_PENDING: {
        color: "#dc2626",
        bg: "#fef2f2",
        label: "Final Approval Pending",
      },
      APPROVED: {
        color: "#10b981",
        bg: "#f0fdf4",
        label: "✓ APPROVED",
      },
      REJECTED: {
        color: "#dc2626",
        bg: "#fef2f2",
        label: "✗ FINALLY REJECTED",
      },
    };

    const config = statusConfig[status] || {
      color: "#64748b",
      bg: "#f1f5f9",
      label: status,
    };

    return (
      <span
        style={{
          padding: "6px 14px",
          borderRadius: "12px",
          fontSize: "13px",
          fontWeight: 700,
          color: config.color,
          background: config.bg,
          border: `2px solid ${config.color}`,
          textTransform: "uppercase",
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
          title="City Engineer - FINAL APPROVAL"
          officerType="CE"
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
            background: "rgba(0, 0, 0, 0.6)",
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
              border: "3px solid #dc2626",
            }}
          >
            <div
              style={{
                padding: "16px 20px",
                borderBottom: "2px solid #dc2626",
                background: "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
              }}
            >
              <h3
                style={{
                  color: "white",
                  margin: 0,
                  fontSize: "18px",
                  fontWeight: 700,
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <AlertCircle style={{ width: "22px", height: "22px" }} />
                FINAL REJECTION
              </h3>
            </div>

            <div style={{ padding: "20px" }}>
              <div
                style={{
                  padding: "12px",
                  background: "#fef2f2",
                  border: "2px solid #fca5a5",
                  borderRadius: "6px",
                  marginBottom: "16px",
                }}
              >
                <p
                  style={{
                    fontSize: "13px",
                    color: "#7f1d1d",
                    margin: 0,
                    fontWeight: 600,
                  }}
                >
                  ⚠️ WARNING: This is the FINAL stage of approval. Rejecting
                  this application will permanently mark it as REJECTED. This
                  action cannot be undone.
                </p>
              </div>

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
                  fontWeight: 700,
                  color: "#dc2626",
                  fontSize: "15px",
                }}
              >
                Final Rejection Comments{" "}
                <span style={{ color: "#dc2626" }}>*</span>
              </label>
              <textarea
                placeholder="Enter detailed rejection comments (MANDATORY) - Explain why this application is being finally rejected"
                value={rejectionComments}
                onChange={(e) => setRejectionComments(e.target.value)}
                style={{
                  width: "100%",
                  minHeight: "120px",
                  padding: "12px",
                  border: "2px solid #fca5a5",
                  borderRadius: "6px",
                  fontSize: "14px",
                  fontFamily: "inherit",
                  resize: "vertical",
                  background: "#fef2f2",
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
                  padding: "10px 20px",
                  fontSize: "14px",
                  background: "white",
                  border: "2px solid #d1d5db",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontWeight: 600,
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleRejectApplication}
                disabled={isRejecting || !rejectionComments.trim()}
                style={{
                  padding: "10px 24px",
                  fontSize: "14px",
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
                  color: "white",
                  border: "none",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontWeight: 700,
                  textTransform: "uppercase",
                }}
              >
                {isRejecting ? "Rejecting..." : "⚠️ Final Reject"}
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
              fontSize: "30px",
              fontWeight: 700,
              color: "#1e293b",
              marginBottom: "8px",
              display: "flex",
              alignItems: "center",
              gap: "12px",
            }}
          >
            <Award
              style={{
                width: "32px",
                height: "32px",
                color: "#dc2626",
              }}
            />
            City Engineer Dashboard
          </h1>
          <p style={{ color: "#64748b", fontSize: "15px", fontWeight: 500 }}>
            Final approval authority for all position registration applications
          </p>
        </div>

        {/* Workflow Info Banner */}
        <div
          style={{
            marginBottom: "20px",
            padding: "16px 20px",
            background: "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
            borderRadius: "8px",
            border: "2px solid #991b1b",
            display: "flex",
            alignItems: "flex-start",
            gap: "14px",
          }}
        >
          <CheckCircle
            style={{
              width: "24px",
              height: "24px",
              color: "white",
              flexShrink: 0,
              marginTop: "2px",
            }}
          />
          <div>
            <p
              style={{
                fontSize: "15px",
                color: "white",
                fontWeight: 700,
                margin: "0 0 6px 0",
              }}
            >
              ℹ️ Workflow Information - Final Stage
            </p>
            <p
              style={{
                fontSize: "14px",
                color: "rgba(255, 255, 255, 0.95)",
                margin: 0,
                lineHeight: "1.5",
              }}
            >
              After you apply final approval and digital signature, applications
              proceed to payment stage and are removed from this list. Rejected
              applications are permanently marked as REJECTED and returned to
              the applicant.
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
            <Award
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
              Applications will appear here when forwarded by Executive
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
                  border: "2px solid #fca5a5",
                  transition: "all 0.2s",
                  boxShadow: "0 2px 4px 0 rgba(220, 38, 38, 0.1)",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.boxShadow =
                    "0 4px 8px 0 rgba(220, 38, 38, 0.2)";
                  e.currentTarget.style.borderColor = "#dc2626";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.boxShadow =
                    "0 2px 4px 0 rgba(220, 38, 38, 0.1)";
                  e.currentTarget.style.borderColor = "#fca5a5";
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
                        fontSize: "18px",
                        fontWeight: 700,
                        color: "#1e293b",
                        marginBottom: "4px",
                      }}
                    >
                      {application.applicationNumber}
                    </h3>
                    <p
                      style={{
                        color: "#64748b",
                        fontSize: "14px",
                        fontWeight: 500,
                      }}
                    >
                      {application.applicantName}
                    </p>
                  </div>
                  {getStatusBadge(application.status)}
                </div>

                {/* Approval Hierarchy Display */}
                <div
                  style={{
                    padding: "12px",
                    background: "#f8fafc",
                    borderRadius: "6px",
                    marginBottom: "16px",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#64748b",
                      fontWeight: 600,
                      marginBottom: "8px",
                      textTransform: "uppercase",
                    }}
                  >
                    Approval Chain
                  </p>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                      flexWrap: "wrap",
                      fontSize: "13px",
                    }}
                  >
                    <span
                      style={{
                        color: "#10b981",
                        fontWeight: 600,
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <CheckCircle style={{ width: "14px", height: "14px" }} />
                      JE: {application.assignedJEName}
                    </span>
                    <span style={{ color: "#94a3b8" }}>→</span>
                    <span
                      style={{
                        color: "#10b981",
                        fontWeight: 600,
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <CheckCircle style={{ width: "14px", height: "14px" }} />
                      AE: {application.assignedAEName}
                    </span>
                    <span style={{ color: "#94a3b8" }}>→</span>
                    <span
                      style={{
                        color: "#10b981",
                        fontWeight: 600,
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <CheckCircle style={{ width: "14px", height: "14px" }} />
                      EE: {application.assignedExecutiveEngineerName}
                    </span>
                    <span style={{ color: "#94a3b8" }}>→</span>
                    <span
                      style={{
                        color: "#dc2626",
                        fontWeight: 700,
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Award style={{ width: "14px", height: "14px" }} />
                      CE: FINAL APPROVAL
                    </span>
                  </div>
                </div>

                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))",
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
                        fontWeight: 600,
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
                  {application.approvedDate && (
                    <div>
                      <p
                        style={{
                          fontSize: "12px",
                          color: "#94a3b8",
                          marginBottom: "4px",
                        }}
                      >
                        Approved Date
                      </p>
                      <p
                        style={{
                          fontSize: "14px",
                          color: "#10b981",
                          fontWeight: 600,
                        }}
                      >
                        {formatDate(application.approvedDate)}
                      </p>
                    </div>
                  )}
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
                      padding: "10px 18px",
                      background: "white",
                      border: "2px solid #d1d5db",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 600,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <Eye style={{ width: "16px", height: "16px" }} />
                    View Details
                  </button>
                  <button
                    onClick={() => handleFinalApproval(application)}
                    style={{
                      padding: "10px 24px",
                      background:
                        "linear-gradient(135deg, #dc2626 0%, #991b1b 100%)",
                      color: "white",
                      border: "none",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 700,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                      textTransform: "uppercase",
                    }}
                  >
                    <Award style={{ width: "18px", height: "18px" }} />
                    Final Approval
                  </button>
                  <button
                    onClick={() => handleRejectClick(application)}
                    style={{
                      padding: "10px 20px",
                      background: "white",
                      color: "#dc2626",
                      border: "2px solid #dc2626",
                      borderRadius: "6px",
                      cursor: "pointer",
                      fontSize: "14px",
                      fontWeight: 700,
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                      textTransform: "uppercase",
                    }}
                  >
                    <XCircle style={{ width: "16px", height: "16px" }} />
                    Final Reject
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

export default CEDashboard;
