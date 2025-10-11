import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { jeWorkflowService } from "../services/jeWorkflowService";
import positionRegistrationService from "../services/positionRegistrationService";
import { Calendar, Clock, Eye, CheckCircle } from "lucide-react";
import { PageLoader } from "../components";
import { DocumentApprovalModal } from "../components/workflow";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import type { JEWorkflowStatusDto } from "../types/jeWorkflow";

interface ScheduleAppointment {
  applicationId: number;
  applicationNumber: string;
  firstName: string;
  lastName: string;
  status: string;
  createdDate: string;
  position: string;
  workflow?: JEWorkflowStatusDto;
}

const JEDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [scheduleAppointments, setScheduleAppointments] = useState<
    ScheduleAppointment[]
  >([]);
  const [pendingApplications, setPendingApplications] = useState<
    ScheduleAppointment[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"schedule" | "pending">(
    "schedule"
  );
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);
  const [scheduleError, setScheduleError] = useState("");
  const [selectedApplication, setSelectedApplication] =
    useState<ScheduleAppointment | null>(null);
  const [scheduleForm, setScheduleForm] = useState({
    comments: "",
    reviewDate: "",
    contactPerson: "",
    place: "",
    roomNumber: "",
  });
  const [showDocumentApprovalModal, setShowDocumentApprovalModal] =
    useState(false);
  const [selectedApplicationForApproval, setSelectedApplicationForApproval] =
    useState<{ id: number; documents: any[] } | null>(null);
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
  useEffect(() => {
    if (!user || !user.role.includes("Junior")) {
      navigate("/");
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        console.log("📊 Fetching JE Dashboard data for user:", user.id);

        // Fetch applications assigned to this JE officer
        const response = await jeWorkflowService.getOfficerApplications(
          user.id
        );

        console.log("✅ Assigned applications response:", response);
        console.log("📊 Response data:", response.data);
        console.log("📊 Response type:", typeof response);
        console.log("📊 Is Array:", Array.isArray(response));

        // Extract data from ApiResponse
        // The service already unwraps the response, so use response directly if it's an array
        const assignedApplications = Array.isArray(response)
          ? response
          : response.data || [];

        console.log("📋 Total applications:", assignedApplications.length);

        // Log each application's appointment info
        assignedApplications.forEach(
          (app: JEWorkflowStatusDto, index: number) => {
            console.log(`App ${index + 1}:`, {
              applicationId: app.applicationId,
              applicationNumber: app.applicationNumber,
              appointmentInfo: app.appointmentInfo,
              isScheduled: app.appointmentInfo?.isScheduled,
            });
          }
        );

        // Filter applications that need appointment scheduling
        // Show in "Schedule Appointment" tab if currentStage is NOT "Appointment Scheduled"
        const needsScheduling = assignedApplications.filter(
          (app: JEWorkflowStatusDto) => {
            const isAppointmentScheduled =
              app.currentStage === "Appointment Scheduled" ||
              app.currentStage === "APPOINTMENT_SCHEDULED";
            const needsSchedule = !isAppointmentScheduled;
            console.log(`🔍 App ${app.applicationId} needs scheduling:`, {
              currentStage: app.currentStage,
              needsSchedule,
            });
            return needsSchedule;
          }
        );

        // Filter applications for Junior Engineer Pending tab
        // Show only if currentStage is "Appointment Scheduled"
        const pending = assignedApplications.filter(
          (app: JEWorkflowStatusDto) => {
            const isAppointmentScheduled =
              app.currentStage === "Appointment Scheduled" ||
              app.currentStage === "APPOINTMENT_SCHEDULED";
            console.log(`🔍 App ${app.applicationId} in JE Pending:`, {
              currentStage: app.currentStage,
              shouldShow: isAppointmentScheduled,
            });
            return isAppointmentScheduled;
          }
        );

        // Transform to dashboard format
        const transformApplication = (
          app: JEWorkflowStatusDto
        ): ScheduleAppointment => ({
          applicationId: app.applicationId,
          applicationNumber:
            app.applicationNumber || `PMC_APP_${app.applicationId}`,
          firstName: app.firstName || "Unknown",
          lastName: app.lastName || "",
          status: app.currentStage || "JUNIOR_ENGINEER_PENDING",
          createdDate: app.lastUpdated || new Date().toISOString(),
          position: "Architect",
          workflow: app,
        });

        setScheduleAppointments(needsScheduling.map(transformApplication));
        setPendingApplications(pending.map(transformApplication));
      } catch (error) {
        console.error("❌ Error fetching JE dashboard data:", error);
        setScheduleAppointments([]);
        setPendingApplications([]);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user, navigate]);

  const handleScheduleAppointment = (application: ScheduleAppointment) => {
    setSelectedApplication(application);
    setShowScheduleModal(true);
    setScheduleError(""); // Clear any previous errors
    setScheduleForm({
      comments: "",
      reviewDate: "",
      contactPerson: "",
      place: "",
      roomNumber: "",
    });
  };

  const handleSubmitSchedule = async () => {
    // Clear previous error
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
      console.log("📅 Scheduling appointment:", {
        applicationId: selectedApplication.applicationId,
        ...scheduleForm,
      });

      await jeWorkflowService.scheduleAppointment({
        applicationId: selectedApplication.applicationId,
        reviewDate: scheduleForm.reviewDate,
        place: scheduleForm.place,
        contactPerson: scheduleForm.contactPerson,
        roomNumber: scheduleForm.roomNumber,
        remarks: scheduleForm.comments,
      });

      // Close schedule modal and show success popup
      setShowScheduleModal(false);
      setSelectedApplication(null);
      setShowSuccessPopup(true);

      // Refresh data after 2 seconds
      setTimeout(() => {
        window.location.reload();
      }, 2000);
    } catch (error) {
      console.error("❌ Error scheduling appointment:", error);
      setScheduleError("Failed to schedule appointment. Please try again.");
    }
  };

  const handleViewApplication = (applicationId: number) => {
    navigate(`/position-application/${applicationId}`);
  };

  const handleDocumentApprove = async (application: ScheduleAppointment) => {
    try {
      // Fetch full application details to get documents
      const fullApplication = await positionRegistrationService.getApplication(
        application.applicationId
      );

      setSelectedApplicationForApproval({
        id: fullApplication.id,
        documents: fullApplication.documents.map((doc) => ({
          id: doc.id,
          documentTypeName: doc.documentTypeName,
          fileName: doc.fileName,
          fileSize: doc.fileSize,
          isVerified: doc.isVerified,
        })),
      });
      setShowDocumentApprovalModal(true);
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
    // Refresh data
    window.location.reload();
  };

  const getPositionLabel = (position: string) => {
    const labels: Record<string, string> = {
      "0": "Architect",
      "1": "Licence Engineer",
      "2": "Structural Engineer",
      "3": "Supervisor 1",
      "4": "Supervisor 2",
      Architect: "Architect",
      LicenceEngineer: "Licence Engineer",
      StructuralEngineer: "Structural Engineer",
      Supervisor1: "Supervisor 1",
      Supervisor2: "Supervisor 2",
    };
    return labels[position] || position;
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return "N/A";
    const date = new Date(dateString);
    return date.toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
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
      />
      <div
        className="pmc-dashboard pmc-fadeIn"
        style={{ padding: "24px", maxWidth: "1400px", margin: "0 auto" }}
      >
        {/* Header */}
        <div style={{ marginBottom: "32px" }}>
          <h1
            className="pmc-text-3xl pmc-font-bold"
            style={{ color: "var(--pmc-text-primary)", marginBottom: "8px" }}
          >
            Junior Engineer Dashboard
          </h1>
          <p
            className="pmc-text-base"
            style={{ color: "var(--pmc-text-secondary)" }}
          >
            Welcome, {user?.name}! Manage your assigned applications and
            appointments.
          </p>
        </div>

        {/* Tabs */}
        <div
          className="pmc-card"
          style={{ marginBottom: "24px", overflow: "hidden" }}
        >
          <div
            style={{
              display: "flex",
              borderBottom: "2px solid var(--pmc-border)",
            }}
          >
            <button
              onClick={() => setActiveTab("schedule")}
              style={{
                flex: 1,
                padding: "16px 24px",
                background:
                  activeTab === "schedule"
                    ? "var(--pmc-primary)"
                    : "transparent",
                color:
                  activeTab === "schedule"
                    ? "white"
                    : "var(--pmc-text-secondary)",
                border: "none",
                borderBottom:
                  activeTab === "schedule"
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
                <Calendar style={{ width: "20px", height: "20px" }} />
                <span>SCHEDULE APPOINTMENT</span>
                <span
                  style={{
                    background:
                      activeTab === "schedule"
                        ? "rgba(255,255,255,0.2)"
                        : "var(--pmc-primary)",
                    color: activeTab === "schedule" ? "white" : "white",
                    padding: "2px 8px",
                    borderRadius: "12px",
                    fontSize: "12px",
                    fontWeight: "bold",
                  }}
                >
                  {scheduleAppointments.length}
                </span>
              </div>
            </button>

            <button
              onClick={() => setActiveTab("pending")}
              style={{
                flex: 1,
                padding: "16px 24px",
                background:
                  activeTab === "pending"
                    ? "var(--pmc-primary)"
                    : "transparent",
                color:
                  activeTab === "pending"
                    ? "white"
                    : "var(--pmc-text-secondary)",
                border: "none",
                borderBottom:
                  activeTab === "pending"
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
                <Clock style={{ width: "20px", height: "20px" }} />
                <span>JUNIOR ENGINEER PENDING</span>
                <span
                  style={{
                    background:
                      activeTab === "pending"
                        ? "rgba(255,255,255,0.2)"
                        : "var(--pmc-warning)",
                    color: "white",
                    padding: "2px 8px",
                    borderRadius: "12px",
                    fontSize: "12px",
                    fontWeight: "bold",
                  }}
                >
                  {pendingApplications.length}
                </span>
              </div>
            </button>
          </div>
        </div>

        {/* Schedule Appointment Tab */}
        {activeTab === "schedule" && (
          <div className="pmc-card pmc-slideInLeft">
            <div className="pmc-card-header">
              <h2 className="pmc-card-title">Schedule Appointment</h2>
              <p className="pmc-card-subtitle">
                Applications requiring appointment scheduling
              </p>
            </div>
            <div className="pmc-card-body">
              {scheduleAppointments.length === 0 ? (
                <div
                  style={{
                    textAlign: "center",
                    padding: "48px 24px",
                    color: "var(--pmc-text-secondary)",
                  }}
                >
                  <Calendar
                    style={{
                      width: "64px",
                      height: "64px",
                      margin: "0 auto 16px",
                      opacity: 0.3,
                    }}
                  />
                  <p className="pmc-text-lg pmc-font-medium">
                    No appointments to schedule
                  </p>
                  <p className="pmc-text-sm">
                    All assigned applications have scheduled appointments
                  </p>
                </div>
              ) : (
                <div style={{ overflowX: "auto" }}>
                  <table className="pmc-table">
                    <thead>
                      <tr>
                        <th>Application ID</th>
                        <th>First Name</th>
                        <th>Last Name</th>
                        <th>Status</th>
                        <th>Created Date</th>
                        <th>Position</th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {scheduleAppointments.map((app) => (
                        <tr key={app.applicationId}>
                          <td>
                            <span className="pmc-badge pmc-badge-primary">
                              {app.applicationNumber}
                            </span>
                          </td>
                          <td>{app.firstName}</td>
                          <td>{app.lastName}</td>
                          <td>
                            <span className="pmc-badge pmc-badge-warning">
                              JUNIOR ENGINEER PENDING
                            </span>
                          </td>
                          <td>{formatDate(app.createdDate)}</td>
                          <td>{getPositionLabel(app.position)}</td>
                          <td>
                            <div
                              style={{
                                display: "flex",
                                gap: "8px",
                                flexWrap: "wrap",
                              }}
                            >
                              <button
                                className="pmc-button pmc-button-sm pmc-button-primary"
                                onClick={() =>
                                  handleViewApplication(app.applicationId)
                                }
                                style={{
                                  display: "flex",
                                  alignItems: "center",
                                  gap: "4px",
                                }}
                              >
                                <Eye
                                  style={{ width: "16px", height: "16px" }}
                                />
                                View
                              </button>
                              <button
                                className="pmc-button pmc-button-sm pmc-button-success"
                                onClick={() => handleScheduleAppointment(app)}
                                style={{
                                  display: "flex",
                                  alignItems: "center",
                                  gap: "4px",
                                }}
                              >
                                <Calendar
                                  style={{ width: "16px", height: "16px" }}
                                />
                                Schedule
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Junior Engineer Pending Tab */}
        {activeTab === "pending" && (
          <div className="pmc-card pmc-slideInLeft">
            <div className="pmc-card-header">
              <h2 className="pmc-card-title">Junior Engineer Pending</h2>
              <p className="pmc-card-subtitle">
                Applications under review by Junior Engineer
              </p>
            </div>
            <div className="pmc-card-body">
              {pendingApplications.length === 0 ? (
                <div
                  style={{
                    textAlign: "center",
                    padding: "48px 24px",
                    color: "var(--pmc-text-secondary)",
                  }}
                >
                  <CheckCircle
                    style={{
                      width: "64px",
                      height: "64px",
                      margin: "0 auto 16px",
                      opacity: 0.3,
                    }}
                  />
                  <p className="pmc-text-lg pmc-font-medium">
                    No pending applications
                  </p>
                  <p className="pmc-text-sm">
                    All applications have been processed
                  </p>
                </div>
              ) : (
                <div style={{ overflowX: "auto" }}>
                  <table className="pmc-table">
                    <thead>
                      <tr>
                        <th>Application ID</th>
                        <th>First Name</th>
                        <th>Last Name</th>
                        <th>Created Date</th>
                        <th>Stage</th>
                        <th>Position</th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {pendingApplications.map((app) => (
                        <tr key={app.applicationId}>
                          <td>
                            <span className="pmc-badge pmc-badge-primary">
                              {app.applicationNumber}
                            </span>
                          </td>
                          <td>{app.firstName}</td>
                          <td>{app.lastName}</td>
                          <td>{formatDate(app.createdDate)}</td>
                          <td>
                            <span className="pmc-badge pmc-badge-warning">
                              {jeWorkflowService.getStageName(app.status)}
                            </span>
                          </td>
                          <td>{getPositionLabel(app.position)}</td>
                          <td>
                            <div style={{ display: "flex", gap: "8px" }}>
                              <button
                                className="pmc-button pmc-button-sm pmc-button-primary"
                                onClick={() =>
                                  handleViewApplication(app.applicationId)
                                }
                                style={{
                                  display: "flex",
                                  alignItems: "center",
                                  gap: "4px",
                                }}
                              >
                                <Eye
                                  style={{ width: "16px", height: "16px" }}
                                />
                                View
                              </button>
                              <button
                                className="pmc-button pmc-button-sm pmc-button-success"
                                onClick={() => handleDocumentApprove(app)}
                                style={{
                                  display: "flex",
                                  alignItems: "center",
                                  gap: "4px",
                                }}
                              >
                                <CheckCircle
                                  style={{ width: "16px", height: "16px" }}
                                />
                                Document Approve
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Schedule Appointment Modal */}
        {showScheduleModal && selectedApplication && (
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
                boxShadow:
                  "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)",
                position: "relative",
                maxHeight: "95vh",
                display: "flex",
                flexDirection: "column",
              }}
            >
              <div
                className="pmc-modal-header"
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

              {/* Error Message */}
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
                className="pmc-modal-body"
                style={{
                  padding: "20px",
                  overflowY: "auto",
                  flexGrow: 1,
                }}
              >
                <div style={{ marginBottom: "16px" }}>
                  <label
                    className="pmc-label"
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
                      outline: "none",
                      transition: "all 0.2s",
                      cursor: "pointer",
                      backgroundColor: "white",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                    required
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
                      className="pmc-label"
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
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>

                  <div>
                    <label
                      className="pmc-label"
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
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>
                </div>

                <div style={{ marginBottom: "16px" }}>
                  <label
                    className="pmc-label"
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
                      outline: "none",
                      transition: "all 0.2s",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>

                <div>
                  <label
                    className="pmc-label"
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
                      outline: "none",
                      transition: "all 0.2s",
                      fontFamily: "inherit",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>
              </div>

              <div
                className="pmc-modal-footer"
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
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowScheduleModal(false)}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                  }}
                >
                  Cancel
                </button>
                <button
                  className="pmc-button pmc-button-success"
                  onClick={handleSubmitSchedule}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
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
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
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
                Appointment Scheduled Successfully!
              </h2>
              <p
                style={{
                  fontSize: "16px",
                  color: "#64748b",
                  marginBottom: "24px",
                }}
              >
                The appointment has been scheduled and the applicant will be
                notified.
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
              <p
                style={{
                  marginTop: "16px",
                  fontSize: "14px",
                  color: "#64748b",
                }}
              >
                Refreshing dashboard...
              </p>
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
      </div>
    </>
  );
};

export default JEDashboard;
