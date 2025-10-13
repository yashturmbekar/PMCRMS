import React, { useState, useEffect, useContext } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  FileText,
  CheckCircle,
  XCircle,
  Calendar,
  Ban,
  X,
  Eye,
} from "lucide-react";
import positionRegistrationService, {
  type PositionRegistrationResponse,
} from "../services/positionRegistrationService";
import { PageLoader } from "../components";
import { DocumentApprovalModal } from "../components/workflow";
import AuthContext from "../contexts/AuthContext";
import { jeWorkflowService } from "../services/jeWorkflowService";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import PaymentButton from "../components/PaymentButton";
import PaymentStatusModal from "../components/PaymentStatusModal";

const ViewPositionApplication: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const authContext = useContext(AuthContext);
  const user = authContext?.user;
  const [application, setApplication] =
    useState<PositionRegistrationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showDocumentApprovalModal, setShowDocumentApprovalModal] =
    useState(false);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showRescheduleModal, setShowRescheduleModal] = useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);
  const [scheduleError, setScheduleError] = useState("");
  const [rescheduleError, setRescheduleError] = useState("");
  const [scheduleForm, setScheduleForm] = useState({
    comments: "",
    reviewDate: "",
    contactPerson: "",
    place: "",
    roomNumber: "",
  });
  const [rescheduleForm, setRescheduleForm] = useState({
    newReviewDate: "",
    rescheduleReason: "",
    place: "",
    contactPerson: "",
    roomNumber: "",
  });
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
  const [selectedDocument, setSelectedDocument] = useState<{
    fileName: string;
    filePath?: string; // Make filePath optional
    documentTypeName: string;
    id?: number; // Add optional ID for API endpoint
    pdfBase64?: string; // Add base64 for direct display (recommendation form)
    fileBase64?: string; // Add base64 for all documents
  } | null>(null);
  const [pdfBlobUrl, setPdfBlobUrl] = useState<string | null>(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  // Determine if accessed from admin context
  const isAdminView = user?.role === "Admin" || location.state?.fromAdmin;
  const isJEOfficer = user?.role && user.role.includes("Junior");
  const backPath = isAdminView
    ? "/admin/applications"
    : isJEOfficer
    ? "/je-dashboard"
    : "/dashboard";

  useEffect(() => {
    const fetchApplication = async () => {
      if (!id) {
        setError("Application ID is required");
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        console.log("📋 Application Data:", {
          status: response.status,
          statusName: response.statusName,
          workflowCurrentStage: response.workflowInfo?.currentStage,
          hasAppointment: response.workflowInfo?.hasAppointment,
        });
        setApplication(response);
      } catch (err) {
        console.error("Error fetching application:", err);
        setError("Failed to load application details");
      } finally {
        setLoading(false);
      }
    };

    fetchApplication();
  }, [id]);

  // Create blob URL from base64 when selectedDocument changes
  useEffect(() => {
    // Cleanup previous blob URL
    if (pdfBlobUrl) {
      URL.revokeObjectURL(pdfBlobUrl);
      setPdfBlobUrl(null);
    }

    // Create new blob URL if we have base64 data (either pdfBase64 or fileBase64)
    const base64Data =
      selectedDocument?.pdfBase64 || selectedDocument?.fileBase64;

    if (base64Data) {
      try {
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);

        // Determine content type based on file name
        let contentType = "application/pdf";
        if (selectedDocument?.fileName) {
          const fileName = selectedDocument.fileName.toLowerCase();
          if (fileName.endsWith(".jpg") || fileName.endsWith(".jpeg")) {
            contentType = "image/jpeg";
          } else if (fileName.endsWith(".png")) {
            contentType = "image/png";
          } else if (fileName.endsWith(".gif")) {
            contentType = "image/gif";
          } else if (fileName.endsWith(".webp")) {
            contentType = "image/webp";
          }
        }

        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);
        setPdfBlobUrl(url);
        console.log("✅ Created blob URL:", url, "Type:", contentType);
      } catch (error) {
        console.error("❌ Error creating blob URL:", error);
      }
    }

    // Cleanup on unmount
    return () => {
      if (pdfBlobUrl) {
        URL.revokeObjectURL(pdfBlobUrl);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDocument]);

  const handleDocumentApprovalComplete = async () => {
    // Reload application data
    if (id) {
      try {
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        setApplication(response);
      } catch (err) {
        console.error("Error reloading application:", err);
      }
    }
  };

  const handleScheduleAppointment = () => {
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
      !application ||
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
        applicationId: application.id,
        ...scheduleForm,
      });

      await jeWorkflowService.scheduleAppointment({
        applicationId: application.id,
        reviewDate: scheduleForm.reviewDate,
        place: scheduleForm.place,
        contactPerson: scheduleForm.contactPerson,
        roomNumber: scheduleForm.roomNumber,
        remarks: scheduleForm.comments,
      });

      // Close schedule modal and show success popup
      setShowScheduleModal(false);
      setShowSuccessPopup(true);

      // Redirect to dashboard after 2 seconds
      setTimeout(() => {
        navigate(backPath);
      }, 2000);
    } catch (error) {
      console.error("❌ Error scheduling appointment:", error);
      setScheduleError("Failed to schedule appointment. Please try again.");
    }
  };

  const handleRescheduleSubmit = async () => {
    if (!rescheduleForm.newReviewDate || !rescheduleForm.rescheduleReason) {
      setRescheduleError("Please fill in all required fields");
      return;
    }

    if (!application?.workflowInfo?.appointmentId) {
      setRescheduleError("Appointment ID not found");
      return;
    }

    try {
      setRescheduleError("");
      await jeWorkflowService.rescheduleAppointment({
        appointmentId: application.workflowInfo.appointmentId,
        newReviewDate: rescheduleForm.newReviewDate,
        rescheduleReason: rescheduleForm.rescheduleReason,
        place: rescheduleForm.place || undefined,
        contactPerson: rescheduleForm.contactPerson || undefined,
        roomNumber: rescheduleForm.roomNumber || undefined,
      });

      setShowRescheduleModal(false);
      setNotification({
        isOpen: true,
        message: `Appointment rescheduled successfully! User will be notified via email.`,
        type: "success",
        title: "Appointment Rescheduled",
        autoClose: true,
      });

      // Refresh application data
      if (id) {
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        setApplication(response);
      }
    } catch (error) {
      console.error("❌ Error rescheduling appointment:", error);
      setRescheduleError("Failed to reschedule appointment. Please try again.");
    }
  };

  const handleRejectApplication = async () => {
    if (!application) return;

    const remarks = prompt("Please provide a reason for rejection:");
    if (!remarks) {
      return; // User cancelled
    }

    try {
      // TODO: Implement reject API call
      console.log("🚫 Rejecting application:", {
        applicationId: application.id,
        remarks,
      });

      setNotification({
        isOpen: true,
        message: "The application has been rejected successfully!",
        type: "success",
        title: "Application Rejected Successfully!",
        autoClose: true,
      });

      setTimeout(() => {
        navigate(backPath);
      }, 2000);
    } catch (error) {
      console.error("❌ Error rejecting application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to reject application. Please try again.",
        type: "error",
        title: "Rejection Failed",
        autoClose: false,
      });
    }
  };

  if (loading) {
    return <PageLoader message="Loading application details..." />;
  }

  if (error || !application) {
    return (
      <div className="pmc-container" style={{ padding: "40px 20px" }}>
        <div className="pmc-card">
          <div
            className="pmc-card-body"
            style={{ textAlign: "center", padding: "40px" }}
          >
            <XCircle size={48} color="#dc2626" style={{ margin: "0 auto" }} />
            <h2 style={{ marginTop: "16px", color: "#dc2626" }}>Error</h2>
            <p style={{ color: "#64748b", marginTop: "8px" }}>{error}</p>
            <button
              onClick={() => navigate(backPath)}
              className="pmc-button pmc-button-primary"
              style={{ marginTop: "24px" }}
            >
              <ArrowLeft size={16} style={{ marginRight: "8px" }} />
              {isAdminView ? "Back to Applications" : "Back to Dashboard"}
            </button>
          </div>
        </div>
      </div>
    );
  }

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 1: // Draft
        return <span className="pmc-badge pmc-status-pending">Draft</span>;
      case 2: // Submitted
        return (
          <span className="pmc-badge pmc-status-under-review">Submitted</span>
        );
      case 23: // Completed
        return <span className="pmc-badge pmc-status-approved">Completed</span>;
      default:
        return (
          <span className="pmc-badge pmc-status-under-review">
            Under Review
          </span>
        );
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
      <div className="pmc-container" style={{ padding: "20px" }}>
        {/* Header */}
        <div style={{ marginBottom: "24px" }}>
          <button
            onClick={() => navigate(backPath)}
            className="pmc-button pmc-button-secondary pmc-button-sm"
            style={{ marginBottom: "16px" }}
          >
            <ArrowLeft size={16} style={{ marginRight: "8px" }} />
            {isAdminView ? "Back to Applications" : "Back to Dashboard"}
          </button>

          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <div>
              <h1 className="pmc-page-title">Application Details</h1>
              {application.applicationNumber && (
                <p
                  style={{
                    color: "#64748b",
                    fontSize: "14px",
                    marginTop: "4px",
                  }}
                >
                  Application #:{" "}
                  <strong>{application.applicationNumber}</strong>
                </p>
              )}
            </div>
            {getStatusBadge(application.status)}
          </div>
        </div>

        {/* Basic Information */}
        <div className="pmc-card" style={{ marginBottom: "16px" }}>
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Basic Information
            </h2>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-form-grid pmc-form-grid-3">
              <div>
                <label className="pmc-label">Position Type</label>
                <p className="pmc-value">{application.positionTypeName}</p>
              </div>
              <div>
                <label className="pmc-label">Full Name</label>
                <p className="pmc-value">{application.fullName}</p>
              </div>
              <div>
                <label className="pmc-label">Mother's Name</label>
                <p className="pmc-value">{application.motherName}</p>
              </div>
              <div>
                <label className="pmc-label">Mobile Number</label>
                <p className="pmc-value">{application.mobileNumber}</p>
              </div>
              <div>
                <label className="pmc-label">Email Address</label>
                <p className="pmc-value">{application.emailAddress}</p>
              </div>
              <div>
                <label className="pmc-label">Gender</label>
                <p className="pmc-value">{application.genderName}</p>
              </div>
              <div>
                <label className="pmc-label">Date of Birth</label>
                <p className="pmc-value">
                  {new Date(application.dateOfBirth).toLocaleDateString()} (
                  {application.age} years)
                </p>
              </div>
              {application.bloodGroup && (
                <div>
                  <label className="pmc-label">Blood Group</label>
                  <p className="pmc-value">{application.bloodGroup}</p>
                </div>
              )}
              {application.height && (
                <div>
                  <label className="pmc-label">Height</label>
                  <p className="pmc-value">{application.height} cm</p>
                </div>
              )}
              <div>
                <label className="pmc-label">PAN Card Number</label>
                <p className="pmc-value">{application.panCardNumber}</p>
              </div>
              <div>
                <label className="pmc-label">Aadhar Card Number</label>
                <p className="pmc-value">{application.aadharCardNumber}</p>
              </div>
              {application.coaCardNumber && (
                <div>
                  <label className="pmc-label">COA Card Number</label>
                  <p className="pmc-value">{application.coaCardNumber}</p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Addresses */}
        {application.addresses.map((address) => (
          <div
            className="pmc-card"
            style={{ marginBottom: "16px" }}
            key={address.id}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                {address.addressType} Address
              </h2>
            </div>
            <div className="pmc-card-body">
              <p className="pmc-value">{address.fullAddress}</p>
            </div>
          </div>
        ))}

        {/* Qualifications */}
        {application.qualifications.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Educational Qualifications
              </h2>
            </div>
            <div className="pmc-card-body">
              {application.qualifications.map((qual, index) => (
                <div
                  key={qual.id}
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    marginBottom:
                      index < application.qualifications.length - 1
                        ? "12px"
                        : "0",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <div className="pmc-form-grid pmc-form-grid-3">
                    <div>
                      <label className="pmc-label">Institute Name</label>
                      <p className="pmc-value">{qual.instituteName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">University Name</label>
                      <p className="pmc-value">{qual.universityName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Degree/Specialization</label>
                      <p className="pmc-value">
                        {qual.degreeName} ({qual.specializationName})
                      </p>
                    </div>
                    <div>
                      <label className="pmc-label">Passing Month & Year</label>
                      <p className="pmc-value">
                        {qual.passingMonthName} {qual.yearOfPassing}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Experiences */}
        {application.experiences.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Work Experience
              </h2>
            </div>
            <div className="pmc-card-body">
              {application.experiences.map((exp, index) => (
                <div
                  key={exp.id}
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    marginBottom:
                      index < application.experiences.length - 1 ? "12px" : "0",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <div className="pmc-form-grid pmc-form-grid-3">
                    <div>
                      <label className="pmc-label">Company Name</label>
                      <p className="pmc-value">{exp.companyName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Position</label>
                      <p className="pmc-value">{exp.position}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Duration</label>
                      <p className="pmc-value">
                        {new Date(exp.fromDate).toLocaleDateString()} -{" "}
                        {new Date(exp.toDate).toLocaleDateString()}
                      </p>
                    </div>
                    <div>
                      <label className="pmc-label">Total Experience</label>
                      <p className="pmc-value">{exp.yearsOfExperience} years</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Documents */}
        {application.documents.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Uploaded Documents
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                {application.documents
                  .filter((doc) => doc.documentTypeName !== "RecommendedForm")
                  .map((doc) => (
                    <div
                      key={doc.id}
                      style={{
                        padding: "16px",
                        background: "#f8fafc",
                        borderRadius: "8px",
                        border: "1px solid #e2e8f0",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "12px",
                        }}
                      >
                        <FileText size={24} color="#3b82f6" />
                        <div>
                          <p
                            className="pmc-value"
                            style={{ marginBottom: "4px" }}
                          >
                            {doc.documentTypeName}
                          </p>
                          <p style={{ fontSize: "12px", color: "#64748b" }}>
                            {doc.fileName}
                          </p>
                          {doc.fileSize && (
                            <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                              {(doc.fileSize / 1024).toFixed(2)} KB
                            </p>
                          )}
                        </div>
                      </div>
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        {doc.isVerified ? (
                          <span title="Verified">
                            <CheckCircle size={20} color="#10b981" />
                          </span>
                        ) : (
                          <span title="Not Verified">
                            <XCircle size={20} color="#94a3b8" />
                          </span>
                        )}
                        <button
                          className="pmc-button pmc-button-primary pmc-button-sm"
                          onClick={() =>
                            setSelectedDocument({
                              fileName: doc.fileName,
                              filePath: doc.filePath,
                              documentTypeName: doc.documentTypeName,
                              fileBase64: doc.fileBase64, // Add base64 data
                            })
                          }
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Eye size={14} />
                          View
                        </button>
                        <button
                          className="pmc-button pmc-button-secondary pmc-button-sm"
                          onClick={() => {
                            if (doc.fileBase64) {
                              // Download from base64 data
                              const byteCharacters = atob(doc.fileBase64);
                              const byteNumbers = new Array(
                                byteCharacters.length
                              );
                              for (let i = 0; i < byteCharacters.length; i++) {
                                byteNumbers[i] = byteCharacters.charCodeAt(i);
                              }
                              const byteArray = new Uint8Array(byteNumbers);
                              const blob = new Blob([byteArray], {
                                type:
                                  doc.contentType || "application/octet-stream",
                              });
                              const url = URL.createObjectURL(blob);
                              const link = document.createElement("a");
                              link.href = url;
                              link.download = doc.fileName;
                              document.body.appendChild(link);
                              link.click();
                              document.body.removeChild(link);
                              URL.revokeObjectURL(url);
                            } else {
                              // Fallback to file path
                              const link = document.createElement("a");
                              link.href = `http://localhost:5062/${doc.filePath}`;
                              link.download = doc.fileName;
                              document.body.appendChild(link);
                              link.click();
                              document.body.removeChild(link);
                            }
                          }}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Download size={14} />
                          Download
                        </button>
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          </div>
        )}

        {/* Recommendation Form - Separate Section */}
        {application.recommendationForm && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Recommendation Form
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    border: "1px solid #e2e8f0",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                    }}
                  >
                    <FileText size={24} color="#3b82f6" />
                    <div>
                      <p className="pmc-value" style={{ marginBottom: "4px" }}>
                        Recommendation Form
                      </p>
                      <p style={{ fontSize: "12px", color: "#64748b" }}>
                        {application.recommendationForm.fileName}
                      </p>
                      {application.recommendationForm.fileSize && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          {(
                            application.recommendationForm.fileSize / 1024
                          ).toFixed(2)}{" "}
                          KB
                        </p>
                      )}
                    </div>
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    <button
                      className="pmc-button pmc-button-primary pmc-button-sm"
                      onClick={() =>
                        setSelectedDocument({
                          fileName: application.recommendationForm!.fileName,
                          filePath: "",
                          documentTypeName: "Recommendation Form",
                          pdfBase64: application.recommendationForm!.pdfBase64,
                        })
                      }
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Eye size={14} />
                      View
                    </button>
                    <button
                      className="pmc-button pmc-button-secondary pmc-button-sm"
                      onClick={() => {
                        // Download from base64 data
                        const byteCharacters = atob(
                          application.recommendationForm!.pdfBase64
                        );
                        const byteNumbers = new Array(byteCharacters.length);
                        for (let i = 0; i < byteCharacters.length; i++) {
                          byteNumbers[i] = byteCharacters.charCodeAt(i);
                        }
                        const byteArray = new Uint8Array(byteNumbers);
                        const blob = new Blob([byteArray], {
                          type: "application/pdf",
                        });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download =
                          application.recommendationForm!.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(url);
                      }}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Download size={14} />
                      Download
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Payment Section - Shows when CE approved (status 13) */}
        {application.status === 13 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                color: "white",
                padding: "12px 16px",
                borderBottom: "2px solid #1d4ed8",
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                }}
              >
                <h2
                  className="pmc-card-title"
                  style={{ color: "white", margin: 0 }}
                >
                  Payment Required
                </h2>
                {application.isPaymentComplete && (
                  <span
                    style={{
                      padding: "4px 12px",
                      background: "#10b981",
                      color: "white",
                      fontSize: "12px",
                      fontWeight: "600",
                      borderRadius: "9999px",
                    }}
                  >
                    ✓ Payment Completed
                  </span>
                )}
              </div>
            </div>
            <div className="pmc-card-body">
              {!application.isPaymentComplete ? (
                <>
                  <div style={{ marginBottom: "16px" }}>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#64748b",
                        marginBottom: "8px",
                      }}
                    >
                      Your application has been approved by the City Engineer.
                      Please complete the payment of <strong>₹3,000</strong> to
                      proceed to the next stage.
                    </p>
                    <div
                      style={{
                        padding: "12px",
                        background: "#eff6ff",
                        borderLeft: "4px solid #3b82f6",
                        borderRadius: "4px",
                        fontSize: "13px",
                        color: "#1e40af",
                      }}
                    >
                      <strong>Note:</strong> After successful payment, your
                      application will be forwarded to the Clerk for further
                      processing.
                    </div>
                  </div>
                  <PaymentButton
                    applicationId={application.id}
                    applicationStatus={application.status}
                    isPaymentComplete={application.isPaymentComplete || false}
                    onPaymentInitiated={() => {
                      console.log(
                        "Payment initiated for application:",
                        application.id
                      );
                    }}
                  />
                </>
              ) : (
                <>
                  <div style={{ marginBottom: "16px" }}>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#64748b",
                        marginBottom: "8px",
                      }}
                    >
                      Your payment has been successfully completed. Your
                      application will now proceed to the Clerk for further
                      processing.
                    </p>
                    {application.paymentCompletedDate && (
                      <p style={{ fontSize: "13px", color: "#94a3b8" }}>
                        Payment completed on:{" "}
                        {new Date(
                          application.paymentCompletedDate
                        ).toLocaleString()}
                      </p>
                    )}
                  </div>
                  <button
                    className="pmc-button pmc-button-primary"
                    onClick={() => setShowPaymentModal(true)}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    <FileText size={18} />
                    View Payment Details
                  </button>
                </>
              )}
            </div>
          </div>
        )}

        {/* Application Timeline */}
        <div className="pmc-card">
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Application Timeline
            </h2>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-form-grid pmc-form-grid-3">
              <div>
                <label className="pmc-label">Created Date</label>
                <p className="pmc-value">
                  {new Date(application.createdDate).toLocaleString()}
                </p>
              </div>
              {application.submittedDate && (
                <div>
                  <label className="pmc-label">Submitted Date</label>
                  <p className="pmc-value">
                    {new Date(application.submittedDate).toLocaleString()}
                  </p>
                </div>
              )}
              {application.approvedDate && (
                <div>
                  <label className="pmc-label">Approved Date</label>
                  <p className="pmc-value">
                    {new Date(application.approvedDate).toLocaleString()}
                  </p>
                </div>
              )}
            </div>
            {application.remarks && (
              <div style={{ marginTop: "16px" }}>
                <label className="pmc-label">Remarks</label>
                <p className="pmc-value">{application.remarks}</p>
              </div>
            )}
          </div>
        </div>

        {/* Appointment Details Card */}
        {application.workflowInfo?.hasAppointment && (
          <div
            className="pmc-card"
            style={{
              marginTop: "24px",
              border: "2px solid #3b82f6",
              boxShadow: "0 2px 8px rgba(59, 130, 246, 0.1)",
            }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                color: "white",
                padding: "12px 16px",
                borderBottom: "2px solid #2563eb",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "white",
                  margin: 0,
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <Calendar size={20} />
                Scheduled Appointment Details
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                {application.workflowInfo.appointmentDate && (
                  <div>
                    <label className="pmc-label">Appointment Date & Time</label>
                    <p
                      className="pmc-value"
                      style={{ fontWeight: 600, color: "#1e40af" }}
                    >
                      {new Date(
                        application.workflowInfo.appointmentDate
                      ).toLocaleString("en-IN", {
                        dateStyle: "full",
                        timeStyle: "short",
                      })}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentPlace && (
                  <div>
                    <label className="pmc-label">Location</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentPlace}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentRoomNumber && (
                  <div>
                    <label className="pmc-label">Room Number</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentRoomNumber}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentContactPerson && (
                  <div>
                    <label className="pmc-label">Contact Person</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentContactPerson}
                    </p>
                  </div>
                )}
              </div>
              {application.workflowInfo.appointmentComments && (
                <div style={{ marginTop: "16px" }}>
                  <label className="pmc-label">Additional Instructions</label>
                  <p
                    className="pmc-value"
                    style={{
                      backgroundColor: "#f0f9ff",
                      padding: "12px",
                      borderRadius: "6px",
                      border: "1px solid #bae6fd",
                    }}
                  >
                    {application.workflowInfo.appointmentComments}
                  </p>
                </div>
              )}

              {/* Reschedule Button for Junior Engineers */}
              {isJEOfficer && (
                <div style={{ marginTop: "16px", textAlign: "right" }}>
                  <button
                    className="pmc-button pmc-button-secondary"
                    onClick={() => {
                      const currentDate = application.workflowInfo
                        ?.appointmentDate
                        ? new Date(application.workflowInfo.appointmentDate)
                            .toISOString()
                            .slice(0, 16)
                        : "";
                      setRescheduleForm({
                        newReviewDate: currentDate,
                        rescheduleReason: "",
                        place: application.workflowInfo?.appointmentPlace || "",
                        contactPerson:
                          application.workflowInfo?.appointmentContactPerson ||
                          "",
                        roomNumber:
                          application.workflowInfo?.appointmentRoomNumber || "",
                      });
                      setShowRescheduleModal(true);
                    }}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                      marginLeft: "auto",
                    }}
                  >
                    <Calendar size={18} />
                    Reschedule Appointment
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Action Buttons for JE Officers */}
        {isJEOfficer && (
          <div
            style={{
              marginTop: "24px",
              display: "flex",
              gap: "12px",
              justifyContent: "flex-end",
            }}
          >
            <button
              className="pmc-button pmc-button-danger"
              onClick={handleRejectApplication}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
              }}
            >
              <Ban size={18} />
              Reject Application
            </button>
            {application.workflowInfo?.currentStage ===
            "Appointment Scheduled" ? (
              <button
                className="pmc-button pmc-button-success"
                onClick={() => setShowDocumentApprovalModal(true)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <CheckCircle size={18} />
                Document Approve
              </button>
            ) : (
              <button
                className="pmc-button pmc-button-success"
                onClick={handleScheduleAppointment}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <Calendar size={18} />
                Schedule Appointment
              </button>
            )}
          </div>
        )}

        {/* Schedule Appointment Modal */}
        {showScheduleModal && (
          <div
            onClick={() => setShowScheduleModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0,0,0,0.5)",
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
                  Application: {application.applicationNumber}
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

        {/* Reschedule Appointment Modal */}
        {showRescheduleModal && (
          <div
            onClick={() => setShowRescheduleModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0,0,0,0.5)",
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
                    "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                  flexShrink: 0,
                }}
              >
                <h3
                  style={{
                    color: "white",
                    marginBottom: "2px",
                    fontSize: "18px",
                    fontWeight: "600",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <Calendar size={20} />
                  Reschedule Appointment
                </h3>
                <p
                  style={{
                    color: "rgba(255,255,255,0.9)",
                    fontSize: "13px",
                    margin: 0,
                  }}
                >
                  Application: {application.applicationNumber}
                </p>
              </div>

              {/* Error Message */}
              {rescheduleError && (
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
                  {rescheduleError}
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
                {/* Current Appointment Info */}
                {application.workflowInfo?.appointmentDate && (
                  <div
                    style={{
                      marginBottom: "20px",
                      padding: "12px",
                      background: "#f0f9ff",
                      borderRadius: "6px",
                      border: "1px solid #bae6fd",
                    }}
                  >
                    <p
                      style={{
                        fontSize: "12px",
                        color: "#0369a1",
                        fontWeight: "600",
                        marginBottom: "4px",
                      }}
                    >
                      Current Appointment
                    </p>
                    <p
                      style={{
                        fontSize: "14px",
                        color: "#1e40af",
                        fontWeight: "500",
                        margin: 0,
                      }}
                    >
                      {new Date(
                        application.workflowInfo.appointmentDate
                      ).toLocaleString("en-IN", {
                        dateStyle: "full",
                        timeStyle: "short",
                      })}
                    </p>
                  </div>
                )}

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
                    New Date & Time <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="datetime-local"
                    value={rescheduleForm.newReviewDate}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
                        newReviewDate: e.target.value,
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
                      e.target.style.borderColor = "#3b82f6";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(59, 130, 246, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                    required
                  />
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#6b7280",
                      marginTop: "4px",
                      marginBottom: 0,
                    }}
                  >
                    Select a date and time after the current date
                  </p>
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
                    Reason for Rescheduling{" "}
                    <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <textarea
                    placeholder="Please provide a reason for rescheduling this appointment..."
                    value={rescheduleForm.rescheduleReason}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
                        rescheduleReason: e.target.value,
                      })
                    }
                    rows={4}
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
                      e.target.style.borderColor = "#3b82f6";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(59, 130, 246, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                    required
                  />
                </div>

                {/* Place Field */}
                <div style={{ marginTop: "16px" }}>
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
                    Place
                  </label>
                  <input
                    type="text"
                    placeholder="e.g., PMC Main Office, Kharadi Office"
                    value={rescheduleForm.place}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
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
                      e.target.style.borderColor = "#3b82f6";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(59, 130, 246, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                  <p
                    style={{
                      fontSize: "11px",
                      color: "#6b7280",
                      marginTop: "4px",
                      marginBottom: 0,
                    }}
                  >
                    Leave empty to keep current location
                  </p>
                </div>

                {/* Contact Person Field */}
                <div style={{ marginTop: "16px" }}>
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
                    Contact Person
                  </label>
                  <input
                    type="text"
                    placeholder="e.g., Junior Engineer Name"
                    value={rescheduleForm.contactPerson}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
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
                      e.target.style.borderColor = "#3b82f6";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(59, 130, 246, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                  <p
                    style={{
                      fontSize: "11px",
                      color: "#6b7280",
                      marginTop: "4px",
                      marginBottom: 0,
                    }}
                  >
                    Leave empty to keep current contact person
                  </p>
                </div>

                {/* Room Number Field */}
                <div style={{ marginTop: "16px" }}>
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
                    Room Number
                  </label>
                  <input
                    type="text"
                    placeholder="e.g., Room 301, 2nd Floor"
                    value={rescheduleForm.roomNumber}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
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
                      e.target.style.borderColor = "#3b82f6";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(59, 130, 246, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                  <p
                    style={{
                      fontSize: "11px",
                      color: "#6b7280",
                      marginTop: "4px",
                      marginBottom: 0,
                    }}
                  >
                    Leave empty to keep current room number
                  </p>
                </div>

                {/* Info Notice */}
                <div
                  style={{
                    marginTop: "16px",
                    padding: "10px 12px",
                    background: "#fef3c7",
                    border: "1px solid #fbbf24",
                    borderRadius: "6px",
                    display: "flex",
                    alignItems: "flex-start",
                    gap: "8px",
                  }}
                >
                  <span style={{ fontSize: "16px" }}>ℹ️</span>
                  <p
                    style={{
                      fontSize: "12px",
                      color: "#92400e",
                      margin: 0,
                      lineHeight: "1.5",
                    }}
                  >
                    The applicant will receive an email notification with the
                    new appointment date and time.
                  </p>
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
                  onClick={() => {
                    setShowRescheduleModal(false);
                    setRescheduleError("");
                  }}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                  }}
                >
                  Cancel
                </button>
                <button
                  className="pmc-button pmc-button-primary"
                  onClick={handleRescheduleSubmit}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                    display: "flex",
                    alignItems: "center",
                    gap: "6px",
                  }}
                >
                  <Calendar size={16} />
                  Reschedule Appointment
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
                Redirecting to dashboard...
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
        <DocumentApprovalModal
          isOpen={showDocumentApprovalModal}
          onClose={() => setShowDocumentApprovalModal(false)}
          applicationId={application.id}
          documents={application.documents.map((doc) => ({
            id: doc.id,
            documentTypeName: doc.documentTypeName,
            fileName: doc.fileName,
            fileSize: doc.fileSize,
            isVerified: doc.isVerified,
          }))}
          onApprovalComplete={handleDocumentApprovalComplete}
        />

        {/* Document Preview Modal */}
        {selectedDocument && (
          <div
            className="pmc-modal-overlay"
            onClick={() => setSelectedDocument(null)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0, 0, 0, 0.8)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
            }}
          >
            <div
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "8px",
                width: "90%",
                maxWidth: "1200px",
                height: "90vh",
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
              }}
            >
              {/* Header */}
              <div
                style={{
                  padding: "16px 20px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                }}
              >
                <div>
                  <h3
                    style={{
                      color: "white",
                      margin: 0,
                      fontSize: "18px",
                      fontWeight: "600",
                    }}
                  >
                    {selectedDocument.documentTypeName}
                  </h3>
                  <p
                    style={{
                      color: "rgba(255,255,255,0.9)",
                      fontSize: "13px",
                      margin: "4px 0 0 0",
                    }}
                  >
                    {selectedDocument.fileName}
                  </p>
                </div>
                <div
                  style={{ display: "flex", gap: "8px", alignItems: "center" }}
                >
                  <button
                    className="pmc-button pmc-button-sm"
                    onClick={() => {
                      const base64Data =
                        selectedDocument.pdfBase64 ||
                        selectedDocument.fileBase64;

                      if (base64Data) {
                        // Download from base64 data
                        const byteCharacters = atob(base64Data);
                        const byteNumbers = new Array(byteCharacters.length);
                        for (let i = 0; i < byteCharacters.length; i++) {
                          byteNumbers[i] = byteCharacters.charCodeAt(i);
                        }
                        const byteArray = new Uint8Array(byteNumbers);

                        // Determine content type
                        let contentType = "application/pdf";
                        const fileName =
                          selectedDocument.fileName.toLowerCase();
                        if (
                          fileName.endsWith(".jpg") ||
                          fileName.endsWith(".jpeg")
                        ) {
                          contentType = "image/jpeg";
                        } else if (fileName.endsWith(".png")) {
                          contentType = "image/png";
                        } else if (fileName.endsWith(".gif")) {
                          contentType = "image/gif";
                        } else if (fileName.endsWith(".webp")) {
                          contentType = "image/webp";
                        }

                        const blob = new Blob([byteArray], {
                          type: contentType,
                        });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download = selectedDocument.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(url);
                      } else {
                        // Use API endpoint or file path
                        const link = document.createElement("a");
                        link.href = selectedDocument.id
                          ? `http://localhost:5062/api/StructuralEngineer/documents/${selectedDocument.id}/download`
                          : `http://localhost:5062/${selectedDocument.filePath}`;
                        link.download = selectedDocument.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                      }
                    }}
                    style={{
                      background: "rgba(255,255,255,0.2)",
                      color: "white",
                      border: "1px solid rgba(255,255,255,0.3)",
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <Download size={14} />
                    Download
                  </button>
                  <button
                    onClick={() => setSelectedDocument(null)}
                    style={{
                      background: "transparent",
                      border: "none",
                      cursor: "pointer",
                      padding: "4px",
                      color: "white",
                    }}
                  >
                    <X size={24} />
                  </button>
                </div>
              </div>

              {/* Document Preview */}
              <div
                style={{
                  flex: 1,
                  overflow: "auto",
                  background: "#f3f4f6",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {selectedDocument.fileName.toLowerCase().endsWith(".pdf") ? (
                  <iframe
                    src={
                      pdfBlobUrl ||
                      (selectedDocument.id
                        ? `http://localhost:5062/api/StructuralEngineer/documents/${selectedDocument.id}/download`
                        : `http://localhost:5062/${selectedDocument.filePath}`)
                    }
                    style={{
                      width: "100%",
                      height: "100%",
                      border: "none",
                    }}
                    title={selectedDocument.fileName}
                  />
                ) : selectedDocument.fileName.match(
                    /\.(jpg|jpeg|png|gif|webp)$/i
                  ) ? (
                  <img
                    src={
                      pdfBlobUrl ||
                      (selectedDocument.id
                        ? `http://localhost:5062/api/StructuralEngineer/documents/${selectedDocument.id}/download`
                        : `http://localhost:5062/${selectedDocument.filePath}`)
                    }
                    alt={selectedDocument.fileName}
                    style={{
                      maxWidth: "100%",
                      maxHeight: "100%",
                      objectFit: "contain",
                    }}
                  />
                ) : (
                  <div
                    style={{
                      textAlign: "center",
                      padding: "40px",
                      color: "#64748b",
                    }}
                  >
                    <FileText size={64} style={{ margin: "0 auto 16px" }} />
                    <p style={{ fontSize: "16px", marginBottom: "8px" }}>
                      Preview not available for this file type
                    </p>
                    <p style={{ fontSize: "14px", marginBottom: "16px" }}>
                      Click the download button to view the file
                    </p>
                    <button
                      className="pmc-button pmc-button-primary"
                      onClick={() => {
                        const link = document.createElement("a");
                        link.href = selectedDocument.id
                          ? `http://localhost:5062/api/StructuralEngineer/documents/${selectedDocument.id}/download`
                          : `http://localhost:5062/${selectedDocument.filePath}`;
                        link.download = selectedDocument.fileName;
                        link.click();
                      }}
                      style={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: "8px",
                      }}
                    >
                      <Download size={16} />
                      Download File
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Payment Status Modal */}
        <PaymentStatusModal
          applicationId={application.id}
          isOpen={showPaymentModal}
          onClose={() => setShowPaymentModal(false)}
        />
      </div>
    </>
  );
};

export default ViewPositionApplication;
