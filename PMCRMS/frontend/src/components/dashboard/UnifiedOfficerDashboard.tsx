import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";
import { Calendar, Clock, Eye, CheckCircle, XCircle } from "lucide-react";
import { PageLoader, FullScreenLoader } from "../../components";
import { OTPVerificationModal, DocumentApprovalModal } from "../workflow";
import NotificationModal from "../common/NotificationModal";
import type { NotificationType } from "../common/NotificationModal";

// Officer Configuration Types
export interface OfficerConfig {
  officerType: "JE" | "AE" | "EE" | "CE" | "Clerk";
  dashboardTitle: string;
  roleCheck: (role: string) => boolean;
  tabs: DashboardTab[];
  allowedActions: {
    schedule?: boolean;
    approve?: boolean;
    verify?: boolean;
    reject?: boolean;
  };
  autoForwardMessage?: string;
}

export interface DashboardTab {
  id: string;
  label: string;
  icon: React.ReactNode;
  badgeColor: string;
  filter: (applications: UnifiedApplication[]) => UnifiedApplication[];
  emptyMessage: string;
  emptySubMessage: string;
  showAutoBanner?: boolean;
}

export interface UnifiedApplication {
  applicationId: number;
  applicationNumber: string;
  applicantName?: string;
  firstName?: string;
  lastName?: string;
  status: string;
  currentStage?: string;
  createdDate: string;
  position?: string;
  positionType?: string;
  assignedToName?: string;
  verificationInfo?: {
    allVerified?: boolean;
  } | null;
  currentStatus?: string;
  [key: string]: unknown;
}

interface UnifiedOfficerDashboardProps {
  config: OfficerConfig;
  fetchApplications: () => Promise<UnifiedApplication[]>;
  scheduleAppointment?: (data: Record<string, unknown>) => Promise<void>;
  verifyAndSign?: (
    applicationId: number,
    otp: string,
    comments?: string
  ) => Promise<{ success: boolean; message?: string }>;
  rejectApplication?: (
    applicationId: number,
    data: Record<string, unknown>
  ) => Promise<{ success: boolean; message?: string }>;
  generateOtp?: (
    applicationId: number
  ) => Promise<{ success: boolean; message?: string }>;
  getApplicationDetails?: (
    applicationId: number
  ) => Promise<Record<string, unknown>>;
}

const UnifiedOfficerDashboard: React.FC<UnifiedOfficerDashboardProps> = ({
  config,
  fetchApplications,
  scheduleAppointment,
  verifyAndSign,
  rejectApplication,
  generateOtp,
  getApplicationDetails,
}) => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [applications, setApplications] = useState<UnifiedApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(config.tabs[0]?.id || "");

  // Modal States
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [showDocumentApprovalModal, setShowDocumentApprovalModal] =
    useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);

  // Form States
  const [selectedApplication, setSelectedApplication] =
    useState<UnifiedApplication | null>(null);
  const [scheduleForm, setScheduleForm] = useState({
    comments: "",
    reviewDate: "",
    contactPerson: "",
    place: "",
    roomNumber: "",
  });
  const [rejectionComments, setRejectionComments] = useState("");
  const [scheduleError, setScheduleError] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);

  const [selectedApplicationForApproval, setSelectedApplicationForApproval] =
    useState<{
      id: number;
      documents: Array<{
        id: number;
        documentTypeName: string;
        fileName: string;
        fileSize: number;
        isVerified: boolean;
      }>;
    } | null>(null);

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
      console.log(`📊 Fetching ${config.officerType} Dashboard data`);

      const apps = await fetchApplications();
      setApplications(apps);

      console.log(`✅ Loaded ${apps.length} applications`);
    } catch (error) {
      console.error(
        `❌ Error fetching ${config.officerType} dashboard data:`,
        error
      );
      setApplications([]);
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
  }, [config.officerType, fetchApplications]);

  useEffect(() => {
    if (!user || !config.roleCheck(user.role)) {
      navigate("/");
      return;
    }

    fetchData();
  }, [user, navigate, fetchData, config]);

  // Handlers
  const handleScheduleAppointment = (application: UnifiedApplication) => {
    setSelectedApplication(application);
    setShowScheduleModal(true);
    setScheduleError("");
    setScheduleForm({
      comments: "",
      reviewDate: "",
      contactPerson: "",
      place: "",
      roomNumber: "",
    });
  };

  const handleSubmitSchedule = async () => {
    setScheduleError("");

    if (
      !selectedApplication ||
      !scheduleForm.reviewDate ||
      !scheduleForm.contactPerson ||
      !scheduleForm.place ||
      !scheduleForm.roomNumber
    ) {
      setScheduleError("Please fill in all required fields");
      return;
    }

    try {
      if (scheduleAppointment) {
        await scheduleAppointment({
          applicationId: selectedApplication.applicationId,
          reviewDate: scheduleForm.reviewDate,
          place: scheduleForm.place,
          contactPerson: scheduleForm.contactPerson,
          roomNumber: scheduleForm.roomNumber,
          remarks: scheduleForm.comments,
        });

        setShowScheduleModal(false);
        setSelectedApplication(null);
        setShowSuccessPopup(true);

        setTimeout(() => {
          window.location.reload();
        }, 2000);
      }
    } catch (error) {
      console.error("❌ Error scheduling appointment:", error);
      setScheduleError("Failed to schedule appointment. Please try again.");
    }
  };

  const handleVerifyDocuments = (application: UnifiedApplication) => {
    setSelectedApplication(application);
    setShowOTPModal(true);
  };

  const handleRejectClick = (application: UnifiedApplication) => {
    setSelectedApplication(application);
    setRejectionComments("");
    setShowRejectModal(true);
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

    if (!selectedApplication || !rejectApplication) return;

    try {
      setIsRejecting(true);
      const result = await rejectApplication(
        selectedApplication.applicationId,
        {
          rejectionComments: rejectionComments,
        }
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
        fetchData();
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

  const handleDocumentApprove = async (application: UnifiedApplication) => {
    try {
      if (getApplicationDetails) {
        const fullApplication = (await getApplicationDetails(
          application.applicationId
        )) as {
          id: number;
          documents: Array<{
            id: number;
            documentTypeName: string;
            fileName: string;
            fileSize: number;
            isVerified: boolean;
          }>;
        };

        setSelectedApplicationForApproval({
          id: fullApplication.id,
          documents: fullApplication.documents,
        });
        setShowDocumentApprovalModal(true);
      }
    } catch (error) {
      console.error("Error fetching application details:", error);
      setNotification({
        isOpen: true,
        message: "Failed to load application details. Please try again.",
        type: "error",
        title: "Error",
        autoClose: false,
      });
    }
  };

  const handleDocumentApprovalComplete = () => {
    setShowDocumentApprovalModal(false);
    setSelectedApplicationForApproval(null);
    window.location.reload();
  };

  const handleViewApplication = (applicationId: number) => {
    navigate(`/position-registration-application-details/${applicationId}`);
  };

  // Utility Functions
  const formatDate = (dateString: string) => {
    if (!dateString) return "N/A";
    return parseLocalDateTime(dateString).toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  const getApplicantName = (app: UnifiedApplication) => {
    if (app.applicantName) return app.applicantName;
    return `${app.firstName || ""} ${app.lastName || ""}`.trim() || "N/A";
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<
      string,
      { color: string; bg: string; label: string }
    > = {
      ASSISTANT_ENGINEER_PENDING: {
        color: "#2563eb",
        bg: "#eff6ff",
        label: "AE Pending",
      },
      EXECUTIVE_ENGINEER_PENDING: {
        color: "#7c3aed",
        bg: "#f5f3ff",
        label: "EE Pending",
      },
      CITY_ENGINEER_PENDING: {
        color: "#0891b2",
        bg: "#ecfeff",
        label: "CE Pending",
      },
      PAYMENT_PENDING: {
        color: "#f59e0b",
        bg: "#fffbeb",
        label: "Payment Pending",
      },
      PaymentPending: {
        color: "#f59e0b",
        bg: "#fffbeb",
        label: "Payment Pending",
      },
      PAYMENT_COMPLETED: {
        color: "#10b981",
        bg: "#f0fdf4",
        label: "Payment Completed",
      },
      PaymentCompleted: {
        color: "#10b981",
        bg: "#f0fdf4",
        label: "Payment Completed",
      },
      REJECTED: { color: "#dc2626", bg: "#fef2f2", label: "Rejected" },
      APPROVED: { color: "#16a34a", bg: "#f0fdf4", label: "Approved" },
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

  // Get filtered applications for active tab
  const activeTabConfig = config.tabs.find((t) => t.id === activeTab);
  const filteredApplications = activeTabConfig
    ? activeTabConfig.filter(applications)
    : [];

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

      {/* OTP Modal */}
      {showOTPModal &&
        selectedApplication &&
        config.allowedActions.verify &&
        config.officerType !== "JE" &&
        config.officerType !== "Clerk" && (
          <OTPVerificationModal
            isOpen={showOTPModal}
            onClose={() => {
              setShowOTPModal(false);
              setSelectedApplication(null);
            }}
            applicationId={selectedApplication.applicationId}
            officerType={config.officerType as "AE" | "EE" | "CE"}
            positionType={
              selectedApplication.positionType || selectedApplication.position
            }
            onGenerateOtp={
              generateOtp || (() => Promise.resolve({ success: false }))
            }
            onVerifyAndSign={
              verifyAndSign || (() => Promise.resolve({ success: false }))
            }
            onSuccess={() => fetchData()}
          />
        )}

      {/* Reject Modal */}
      {showRejectModal &&
        selectedApplication &&
        config.allowedActions.reject && (
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
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
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
                  <strong>Applicant:</strong>{" "}
                  {getApplicantName(selectedApplication)}
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
                      isRejecting || !rejectionComments.trim()
                        ? "#9ca3af"
                        : "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    cursor:
                      isRejecting || !rejectionComments.trim()
                        ? "not-allowed"
                        : "pointer",
                    fontWeight: 600,
                  }}
                >
                  Reject Application
                </button>
              </div>
            </div>
          </div>
        )}

      {/* Schedule Modal */}
      {showScheduleModal &&
        selectedApplication &&
        config.allowedActions.schedule && (
          <div
            className="pmc-modal-overlay"
            onClick={() => setShowScheduleModal(false)}
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
              overflow: "auto",
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
                maxHeight: "95vh",
                display: "flex",
                flexDirection: "column",
              }}
            >
              <div
                style={{
                  padding: "16px 20px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  flexShrink: 0,
                }}
              >
                <h3
                  style={{
                    color: "white",
                    marginBottom: "2px",
                    fontSize: "18px",
                    fontWeight: "600",
                  }}
                >
                  Schedule Appointment
                </h3>
                <p
                  style={{
                    color: "rgba(255,255,255,0.9)",
                    fontSize: "13px",
                    margin: 0,
                  }}
                >
                  Application: {selectedApplication.applicationNumber}
                </p>
              </div>

              {scheduleError && (
                <div
                  style={{
                    margin: "16px 20px 0",
                    padding: "12px 16px",
                    background:
                      "linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)",
                    border: "1.5px solid #ef4444",
                    borderRadius: "6px",
                    color: "#991b1b",
                    fontSize: "13px",
                    fontWeight: "500",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <span style={{ fontSize: "16px" }}>⚠️</span>
                  {scheduleError}
                </div>
              )}

              <div
                style={{
                  padding: "20px",
                  overflowY: "auto",
                  flexGrow: 1,
                }}
              >
                <div style={{ marginBottom: "16px" }}>
                  <label
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Review Date <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="datetime-local"
                    value={scheduleForm.reviewDate}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        reviewDate: e.target.value,
                      })
                    }
                    min={new Date().toISOString().slice(0, 16)}
                    style={{
                      width: "100%",
                      padding: "10px 12px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                    }}
                  />
                </div>

                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 1fr",
                    gap: "12px",
                    marginBottom: "16px",
                  }}
                >
                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "6px",
                        fontWeight: 500,
                        fontSize: "13px",
                        color: "#374151",
                      }}
                    >
                      Contact Person <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Contact Person"
                      value={scheduleForm.contactPerson}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          contactPerson: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "10px 12px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "14px",
                      }}
                    />
                  </div>

                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "6px",
                        fontWeight: 500,
                        fontSize: "13px",
                        color: "#374151",
                      }}
                    >
                      Room Number <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Room Number"
                      value={scheduleForm.roomNumber}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          roomNumber: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "10px 12px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "14px",
                      }}
                    />
                  </div>
                </div>

                <div style={{ marginBottom: "16px" }}>
                  <label
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Place <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="Place"
                    value={scheduleForm.place}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        place: e.target.value,
                      })
                    }
                    style={{
                      width: "100%",
                      padding: "10px 12px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                    }}
                  />
                </div>

                <div>
                  <label
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Comments
                  </label>
                  <textarea
                    placeholder="Additional comments or instructions"
                    value={scheduleForm.comments}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        comments: e.target.value,
                      })
                    }
                    rows={3}
                    style={{
                      width: "100%",
                      padding: "10px 12px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                      resize: "vertical",
                      fontFamily: "inherit",
                    }}
                  />
                </div>
              </div>

              <div
                style={{
                  padding: "12px 20px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "10px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                  flexShrink: 0,
                }}
              >
                <button
                  onClick={() => setShowScheduleModal(false)}
                  style={{
                    padding: "8px 20px",
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
                  onClick={handleSubmitSchedule}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                    background:
                      "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    cursor: "pointer",
                    fontWeight: 600,
                  }}
                >
                  Schedule Appointment
                </button>
              </div>
            </div>
          </div>
        )}

      {/* Success Popup */}
      {showSuccessPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.7)",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            zIndex: 10000,
            padding: "20px",
          }}
        >
          <div
            style={{
              backgroundColor: "white",
              borderRadius: "16px",
              maxWidth: "500px",
              width: "100%",
              padding: "40px",
              textAlign: "center",
              boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
            }}
          >
            <div
              style={{
                width: "80px",
                height: "80px",
                borderRadius: "50%",
                background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                margin: "0 auto 24px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: "40px",
              }}
            >
              ✓
            </div>
            <h2
              style={{
                fontSize: "28px",
                fontWeight: "700",
                color: "#10b981",
                marginBottom: "16px",
              }}
            >
              Success!
            </h2>
            <p
              style={{
                fontSize: "16px",
                color: "#64748b",
                marginBottom: "24px",
              }}
            >
              Operation completed successfully. Refreshing dashboard...
            </p>
            <div
              style={{
                width: "40px",
                height: "40px",
                border: "4px solid #10b981",
                borderTopColor: "transparent",
                borderRadius: "50%",
                margin: "0 auto",
                animation: "spin 1s linear infinite",
              }}
            />
            <style>
              {`
                @keyframes spin {
                  to { transform: rotate(360deg); }
                }
              `}
            </style>
          </div>
        </div>
      )}

      {/* Document Approval Modal */}
      {showDocumentApprovalModal && selectedApplicationForApproval && (
        <DocumentApprovalModal
          isOpen={showDocumentApprovalModal}
          onClose={() => setShowDocumentApprovalModal(false)}
          applicationId={selectedApplicationForApproval.id}
          documents={selectedApplicationForApproval.documents}
          onApprovalComplete={handleDocumentApprovalComplete}
        />
      )}

      {/* Main Dashboard */}
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
            {config.dashboardTitle}
          </h1>
          <p style={{ color: "#64748b", fontSize: "15px" }}>
            Welcome, {user?.name}! Manage your assigned applications
          </p>
        </div>

        {/* Tabs */}
        {config.tabs.length > 1 && (
          <div
            className="pmc-card"
            style={{ marginBottom: "24px", overflow: "hidden" }}
          >
            <div
              style={{
                display: "flex",
                borderBottom: "2px solid #e5e7eb",
              }}
            >
              {config.tabs.map((tab) => {
                const tabApplications = tab.filter(applications);
                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    style={{
                      flex: 1,
                      padding: "16px 24px",
                      background:
                        activeTab === tab.id
                          ? "var(--pmc-primary)"
                          : "transparent",
                      color: activeTab === tab.id ? "white" : "#64748b",
                      border: "none",
                      borderBottom:
                        activeTab === tab.id
                          ? "3px solid var(--pmc-primary)"
                          : "3px solid transparent",
                      cursor: "pointer",
                      fontWeight: 600,
                      fontSize: "16px",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        gap: "8px",
                      }}
                    >
                      {tab.icon}
                      <span>{tab.label}</span>
                      <span
                        style={{
                          background:
                            activeTab === tab.id
                              ? "rgba(255,255,255,0.2)"
                              : tab.badgeColor,
                          color: "white",
                          padding: "2px 8px",
                          borderRadius: "12px",
                          fontSize: "12px",
                          fontWeight: "bold",
                        }}
                      >
                        {tabApplications.length}
                      </span>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
        )}

        {/* Tab Content */}
        <div className="pmc-card pmc-slideInLeft">
          <div className="pmc-card-header">
            <h2 className="pmc-card-title">{activeTabConfig?.label}</h2>
            <p className="pmc-card-subtitle">
              {activeTabConfig?.emptySubMessage}
            </p>
          </div>

          {/* Auto-Forward Banner */}
          {activeTabConfig?.showAutoBanner && config.autoForwardMessage && (
            <div
              style={{
                margin: "16px 24px",
                padding: "14px 18px",
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                borderRadius: "8px",
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
                  ℹ️ {config.autoForwardMessage}
                </p>
              </div>
            </div>
          )}

          <div className="pmc-card-body">
            {filteredApplications.length === 0 ? (
              <div
                style={{
                  textAlign: "center",
                  padding: "60px 20px",
                  color: "#64748b",
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
                <p style={{ fontSize: "16px", fontWeight: 500 }}>
                  {activeTabConfig?.emptyMessage}
                </p>
                <p
                  style={{
                    fontSize: "14px",
                    marginTop: "8px",
                    color: "#94a3b8",
                  }}
                >
                  {activeTabConfig?.emptySubMessage}
                </p>
              </div>
            ) : (
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "12px",
                }}
              >
                {filteredApplications.map((application) => (
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
                          {getApplicantName(application)}
                        </p>
                      </div>
                      {getStatusBadge(application.status)}
                    </div>

                    <div
                      style={{
                        display: "grid",
                        gridTemplateColumns:
                          "repeat(auto-fit, minmax(200px, 1fr))",
                        gap: "12px",
                        marginBottom: "16px",
                      }}
                    >
                      {application.positionType && (
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
                      )}
                      {application.assignedToName && (
                        <div>
                          <p
                            style={{
                              fontSize: "12px",
                              color: "#94a3b8",
                              marginBottom: "4px",
                            }}
                          >
                            Assigned To
                          </p>
                          <p
                            style={{
                              fontSize: "14px",
                              color: "#334155",
                              fontWeight: 500,
                            }}
                          >
                            {application.assignedToName}
                          </p>
                        </div>
                      )}
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
                      style={{
                        display: "flex",
                        gap: "10px",
                        marginTop: "16px",
                        flexWrap: "wrap",
                      }}
                    >
                      <button
                        onClick={() =>
                          handleViewApplication(application.applicationId)
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

                      {config.allowedActions.schedule &&
                        activeTab === "schedule" && (
                          <button
                            onClick={() =>
                              handleScheduleAppointment(application)
                            }
                            style={{
                              padding: "8px 20px",
                              background:
                                "linear-gradient(135deg, #10b981 0%, #059669 100%)",
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
                            <Calendar
                              style={{ width: "16px", height: "16px" }}
                            />
                            Schedule
                          </button>
                        )}

                      {config.allowedActions.approve && (
                        <button
                          onClick={() => handleDocumentApprove(application)}
                          style={{
                            padding: "8px 20px",
                            background:
                              "linear-gradient(135deg, #10b981 0%, #059669 100%)",
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
                          <CheckCircle
                            style={{ width: "16px", height: "16px" }}
                          />
                          Document Approve
                        </button>
                      )}

                      {config.allowedActions.verify && (
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
                          <CheckCircle
                            style={{ width: "16px", height: "16px" }}
                          />
                          Verify Documents
                        </button>
                      )}

                      {config.allowedActions.reject && (
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
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Full Screen Loader */}
      {isRejecting && (
        <FullScreenLoader
          message="Rejecting Application"
          submessage="Please wait while we process the rejection..."
        />
      )}
    </>
  );
};

export default UnifiedOfficerDashboard;
