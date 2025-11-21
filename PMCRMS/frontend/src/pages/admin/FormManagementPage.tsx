import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  adminService,
  type FormConfiguration,
} from "../../services/adminService";
import { SUCCESS_MESSAGE_TIMEOUT } from "../../constants";
import {
  FileText,
  Plus,
  Edit,
  Eye,
  Trash2,
  CheckCircle,
  XCircle,
  Power,
  ArrowLeft,
  Home,
  ChevronRight,
} from "lucide-react";
import { PageLoader } from "../../components";
import NotificationModal from "../../components/common/NotificationModal";
import type { NotificationType } from "../../components/common/NotificationModal";

const FormManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const [forms, setForms] = useState<FormConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [showEditModal, setShowEditModal] = useState(false);
  const [showViewModal, setShowViewModal] = useState(false);
  const [selectedForm, setSelectedForm] = useState<FormConfiguration | null>(
    null
  );
  const [editForm, setEditForm] = useState({
    formName: "",
    description: "",
    baseFee: 0,
    processingFee: 0,
    lateFee: 0,
    processingDays: 0,
    maxFileSizeMB: 10,
    maxFilesAllowed: 10,
    allowOnlineSubmission: true,
    changeReason: "",
  });
  const [submitting, setSubmitting] = useState(false);
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

  useEffect(() => {
    loadForms();
  }, []);

  const loadForms = async () => {
    try {
      setLoading(true);
      const response = await adminService.getFormConfigurations();
      if (response.success && response.data) {
        setForms(response.data as FormConfiguration[]);
      } else {
        setError(response.message || "Failed to load forms");
      }
    } catch (err) {
      console.error("Error loading forms:", err);
      setError("Failed to load forms");
    } finally {
      setLoading(false);
    }
  };

  const handleToggleFormStatus = async (
    formId: number,
    currentStatus: boolean
  ) => {
    try {
      const response = await adminService.updateFormConfiguration(formId, {
        formId: formId,
        isActive: !currentStatus,
      });
      if (response.success) {
        setSuccessMessage(
          `Form ${!currentStatus ? "activated" : "deactivated"} successfully!`
        );
        setTimeout(() => setSuccessMessage(""), SUCCESS_MESSAGE_TIMEOUT);
        loadForms();
      } else {
        setError(response.message || "Failed to update form status");
      }
    } catch (err) {
      console.error("Error updating form:", err);
      setError("Failed to update form status");
    }
  };

  const handleDeleteForm = async (formId: number, formName: string) => {
    if (!confirm(`Are you sure you want to delete the form "${formName}"?`))
      return;

    try {
      const response = await adminService.deleteFormConfiguration(formId);
      if (response.success) {
        setSuccessMessage(`Form "${formName}" deleted successfully!`);
        setTimeout(() => setSuccessMessage(""), SUCCESS_MESSAGE_TIMEOUT);
        loadForms();
      } else {
        setError(response.message || "Failed to delete form");
      }
    } catch (err) {
      console.error("Error deleting form:", err);
      setError("Failed to delete form");
    }
  };

  const handleViewForm = (form: FormConfiguration) => {
    setSelectedForm(form);
    setShowViewModal(true);
  };

  const handleEditForm = (form: FormConfiguration) => {
    setSelectedForm(form);
    setEditForm({
      formName: form.formName,
      description: form.description || "",
      baseFee: form.baseFee,
      processingFee: form.processingFee,
      lateFee: form.lateFee,
      processingDays: form.processingDays,
      maxFileSizeMB: form.maxFileSizeMB || 10,
      maxFilesAllowed: form.maxFilesAllowed || 10,
      allowOnlineSubmission: form.allowOnlineSubmission,
      changeReason: "",
    });
    setShowEditModal(true);
  };

  const handleSubmitEdit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedForm) return;

    if (!editForm.changeReason.trim()) {
      setError("Please provide a reason for this change");
      return;
    }

    try {
      setSubmitting(true);
      setError("");

      const response = await adminService.updateFormConfiguration(
        selectedForm.id,
        {
          formId: selectedForm.id,
          formName: editForm.formName,
          description: editForm.description,
          baseFee: editForm.baseFee,
          processingFee: editForm.processingFee,
          lateFee: editForm.lateFee,
          processingDays: editForm.processingDays,
          maxFileSizeMB: editForm.maxFileSizeMB,
          maxFilesAllowed: editForm.maxFilesAllowed,
          allowOnlineSubmission: editForm.allowOnlineSubmission,
          changeReason: editForm.changeReason,
        }
      );

      if (response.success) {
        setSuccessMessage("Form updated successfully!");
        setShowEditModal(false);
        setSelectedForm(null);
        setTimeout(() => setSuccessMessage(""), SUCCESS_MESSAGE_TIMEOUT);
        loadForms();
      } else {
        setError(response.message || "Failed to update form");
      }
    } catch (err) {
      console.error("Error updating form:", err);
      setError("Failed to update form");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <PageLoader message="Loading Forms..." />;
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
      <div className="pmc-fadeIn" style={{ padding: "24px" }}>
        {/* Breadcrumbs */}
        <div
          className="pmc-fadeInDown"
          style={{
            marginBottom: "16px",
            display: "flex",
            alignItems: "center",
            gap: "8px",
            fontSize: "14px",
            color: "var(--pmc-gray-600)",
          }}
        >
          <button
            onClick={() => navigate("/admin")}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "4px",
              background: "none",
              border: "none",
              cursor: "pointer",
              color: "var(--pmc-primary)",
              padding: "4px 8px",
              borderRadius: "4px",
              transition: "background 0.2s",
            }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.background = "var(--pmc-gray-100)")
            }
            onMouseLeave={(e) => (e.currentTarget.style.background = "none")}
          >
            <Home style={{ width: "16px", height: "16px" }} />
            Dashboard
          </button>
          <ChevronRight style={{ width: "16px", height: "16px" }} />
          <span style={{ color: "var(--pmc-gray-900)", fontWeight: "600" }}>
            Form Management
          </span>
        </div>

        {/* Header with Back Button */}
        <div style={{ marginBottom: "24px" }}>
          <button
            onClick={() => navigate("/admin")}
            className="pmc-button pmc-button-secondary"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              padding: "10px 16px",
              marginBottom: "12px",
            }}
          >
            <ArrowLeft style={{ width: "18px", height: "18px" }} />
            Back
          </button>
        </div>

        {successMessage && (
          <div
            className="pmc-fadeInDown"
            style={{
              position: "fixed",
              top: "80px",
              right: "24px",
              zIndex: 1000,
              background:
                "linear-gradient(135deg, var(--pmc-success) 0%, #15803d 100%)",
              color: "white",
              padding: "16px 20px",
              borderRadius: "8px",
              boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
              display: "flex",
              alignItems: "center",
              gap: "12px",
            }}
          >
            <CheckCircle style={{ width: "20px", height: "20px" }} />
            <span className="pmc-font-medium">{successMessage}</span>
          </div>
        )}

        {error && (
          <div
            className="pmc-fadeInDown"
            style={{
              marginBottom: "24px",
              background: "#fee2e2",
              border: "1px solid #fecaca",
              color: "#dc2626",
              padding: "16px 20px",
              borderRadius: "8px",
              display: "flex",
              alignItems: "center",
              gap: "12px",
            }}
          >
            <XCircle style={{ width: "20px", height: "20px" }} />
            <span className="pmc-font-medium">{error}</span>
            <button
              onClick={() => setError("")}
              style={{
                marginLeft: "auto",
                background: "transparent",
                border: "none",
                color: "#dc2626",
                cursor: "pointer",
                padding: "4px",
              }}
            >
              ✕
            </button>
          </div>
        )}

        <div className="pmc-content-header pmc-fadeInDown">
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <div>
              <h1
                className="pmc-content-title pmc-text-3xl pmc-font-bold"
                style={{ color: "var(--pmc-gray-900)" }}
              >
                Form Management 📝
              </h1>
              <p
                className="pmc-content-subtitle pmc-text-base"
                style={{ color: "var(--pmc-gray-600)" }}
              >
                Configure and manage application forms
              </p>
            </div>
            <button
              className="pmc-button pmc-button-primary"
              style={{
                padding: "12px 24px",
                display: "flex",
                alignItems: "center",
                gap: "8px",
              }}
              onClick={() =>
                setNotification({
                  isOpen: true,
                  message: "Create form functionality will be available soon!",
                  type: "info",
                  title: "Coming Soon",
                  autoClose: false,
                })
              }
            >
              <Plus style={{ width: "18px", height: "18px" }} />
              <span className="pmc-font-semibold">Create Form</span>
            </button>
          </div>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(3, 1fr)",
            gap: "24px",
            marginBottom: "32px",
          }}
          className="pmc-fadeInUp"
        >
          <div
            className="pmc-card"
            style={{
              padding: "24px",
              background:
                "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)",
              border: "none",
              color: "white",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <div
                style={{
                  width: "56px",
                  height: "56px",
                  background: "rgba(255, 255, 255, 0.2)",
                  borderRadius: "12px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <FileText style={{ width: "28px", height: "28px" }} />
              </div>
              <div style={{ flex: 1 }}>
                <p
                  className="pmc-text-sm pmc-font-medium"
                  style={{ opacity: 0.9, marginBottom: "4px" }}
                >
                  Total Forms
                </p>
                <p className="pmc-text-3xl pmc-font-bold">{forms.length}</p>
              </div>
            </div>
          </div>

          <div
            className="pmc-card"
            style={{
              padding: "24px",
              background:
                "linear-gradient(135deg, var(--pmc-success) 0%, #15803d 100%)",
              border: "none",
              color: "white",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <div
                style={{
                  width: "56px",
                  height: "56px",
                  background: "rgba(255, 255, 255, 0.2)",
                  borderRadius: "12px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <CheckCircle style={{ width: "28px", height: "28px" }} />
              </div>
              <div style={{ flex: 1 }}>
                <p
                  className="pmc-text-sm pmc-font-medium"
                  style={{ opacity: 0.9, marginBottom: "4px" }}
                >
                  Active Forms
                </p>
                <p className="pmc-text-3xl pmc-font-bold">
                  {forms.filter((f) => f.isActive).length}
                </p>
              </div>
            </div>
          </div>

          <div
            className="pmc-card"
            style={{
              padding: "24px",
              background:
                "linear-gradient(135deg, var(--pmc-gray-500) 0%, var(--pmc-gray-700) 100%)",
              border: "none",
              color: "white",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <div
                style={{
                  width: "56px",
                  height: "56px",
                  background: "rgba(255, 255, 255, 0.2)",
                  borderRadius: "12px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <XCircle style={{ width: "28px", height: "28px" }} />
              </div>
              <div style={{ flex: 1 }}>
                <p
                  className="pmc-text-sm pmc-font-medium"
                  style={{ opacity: 0.9, marginBottom: "4px" }}
                >
                  Inactive Forms
                </p>
                <p className="pmc-text-3xl pmc-font-bold">
                  {forms.filter((f) => !f.isActive).length}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="pmc-card pmc-slideInLeft">
          <div className="pmc-card-header">
            <h2 className="pmc-card-title">All Forms</h2>
            <p className="pmc-card-subtitle">
              {forms.length} form{forms.length !== 1 ? "s" : ""} configured
            </p>
          </div>
          <div className="pmc-card-body">
            {forms.length === 0 ? (
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
                  No forms configured
                </p>
                <p className="pmc-text-sm">
                  Create your first form to start collecting applications
                </p>
              </div>
            ) : (
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
                          }}
                        >
                          Form Name
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
                          }}
                        >
                          Description
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
                      {forms.map((form) => (
                        <tr key={form.id}>
                          <td
                            className="pmc-text-sm pmc-font-medium"
                            style={{ color: "var(--pmc-gray-800)" }}
                          >
                            {form.formName}
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {form.formType}
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {form.description || "No description"}
                          </td>
                          <td>
                            <span
                              className={`pmc-badge ${
                                form.isActive
                                  ? "pmc-status-approved"
                                  : "pmc-status-rejected"
                              }`}
                            >
                              {form.isActive ? "Active" : "Inactive"}
                            </span>
                          </td>
                          <td>
                            <div style={{ display: "flex", gap: "8px" }}>
                              <button
                                onClick={() => handleViewForm(form)}
                                className="pmc-button pmc-button-secondary"
                                style={{
                                  padding: "6px 12px",
                                  fontSize: "13px",
                                  display: "inline-flex",
                                  alignItems: "center",
                                  gap: "6px",
                                }}
                              >
                                <Eye
                                  style={{ width: "14px", height: "14px" }}
                                />
                                View
                              </button>
                              <button
                                onClick={() => handleEditForm(form)}
                                className="pmc-button pmc-button-secondary"
                                style={{
                                  padding: "6px 12px",
                                  fontSize: "13px",
                                  display: "inline-flex",
                                  alignItems: "center",
                                  gap: "6px",
                                }}
                              >
                                <Edit
                                  style={{ width: "14px", height: "14px" }}
                                />
                                Edit
                              </button>
                              <button
                                onClick={() =>
                                  handleToggleFormStatus(form.id, form.isActive)
                                }
                                className={`pmc-button ${
                                  form.isActive
                                    ? "pmc-button-secondary"
                                    : "pmc-button-primary"
                                }`}
                                style={{
                                  padding: "6px 12px",
                                  fontSize: "13px",
                                  display: "inline-flex",
                                  alignItems: "center",
                                  gap: "6px",
                                }}
                              >
                                <Power
                                  style={{ width: "14px", height: "14px" }}
                                />
                                {form.isActive ? "Deactivate" : "Activate"}
                              </button>
                              <button
                                onClick={() =>
                                  handleDeleteForm(form.id, form.formName)
                                }
                                className="pmc-button pmc-button-secondary"
                                style={{
                                  padding: "6px 12px",
                                  fontSize: "13px",
                                  display: "inline-flex",
                                  alignItems: "center",
                                  gap: "6px",
                                  color: "var(--pmc-danger)",
                                }}
                              >
                                <Trash2
                                  style={{ width: "14px", height: "14px" }}
                                />
                                Delete
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* View Form Modal */}
        {showViewModal && selectedForm && (
          <div
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
            }}
            onClick={() => setShowViewModal(false)}
          >
            <div
              className="pmc-card pmc-fadeIn"
              style={{
                maxWidth: "700px",
                width: "100%",
                maxHeight: "90vh",
                overflow: "auto",
              }}
              onClick={(e) => e.stopPropagation()}
            >
              <div className="pmc-card-header">
                <h2 className="pmc-card-title">Form Details</h2>
                <p className="pmc-card-subtitle">View form configuration</p>
              </div>
              <div className="pmc-card-body" style={{ padding: "24px" }}>
                <div style={{ display: "grid", gap: "20px" }}>
                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: "600",
                        color: "var(--pmc-gray-700)",
                      }}
                    >
                      Form Name
                    </label>
                    <p style={{ color: "var(--pmc-gray-900)" }}>
                      {selectedForm.formName}
                    </p>
                  </div>
                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: "600",
                        color: "var(--pmc-gray-700)",
                      }}
                    >
                      Form Type
                    </label>
                    <p style={{ color: "var(--pmc-gray-900)" }}>
                      {selectedForm.formType}
                    </p>
                  </div>
                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: "600",
                        color: "var(--pmc-gray-700)",
                      }}
                    >
                      Description
                    </label>
                    <p style={{ color: "var(--pmc-gray-900)" }}>
                      {selectedForm.description || "No description"}
                    </p>
                  </div>
                  <div
                    style={{
                      display: "grid",
                      gridTemplateColumns: "1fr 1fr",
                      gap: "16px",
                    }}
                  >
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Base Fee
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        ₹{selectedForm.baseFee}
                      </p>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Processing Fee
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        ₹{selectedForm.processingFee}
                      </p>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Late Fee
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        ₹{selectedForm.lateFee}
                      </p>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Total Fee
                      </label>
                      <p
                        style={{
                          color: "var(--pmc-gray-900)",
                          fontWeight: "600",
                        }}
                      >
                        ₹{selectedForm.totalFee}
                      </p>
                    </div>
                  </div>
                  <div
                    style={{
                      display: "grid",
                      gridTemplateColumns: "1fr 1fr",
                      gap: "16px",
                    }}
                  >
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Processing Days
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        {selectedForm.processingDays} days
                      </p>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Status
                      </label>
                      <span
                        className={`pmc-badge ${
                          selectedForm.isActive
                            ? "pmc-status-approved"
                            : "pmc-status-rejected"
                        }`}
                      >
                        {selectedForm.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Max File Size
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        {selectedForm.maxFileSizeMB} MB
                      </p>
                    </div>
                    <div>
                      <label
                        style={{
                          display: "block",
                          marginBottom: "8px",
                          fontWeight: "600",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Max Files
                      </label>
                      <p style={{ color: "var(--pmc-gray-900)" }}>
                        {selectedForm.maxFilesAllowed} files
                      </p>
                    </div>
                  </div>
                  <div>
                    <label
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: "600",
                        color: "var(--pmc-gray-700)",
                      }}
                    >
                      Online Submission
                    </label>
                    <span
                      className={`pmc-badge ${
                        selectedForm.allowOnlineSubmission
                          ? "pmc-status-approved"
                          : "pmc-status-rejected"
                      }`}
                    >
                      {selectedForm.allowOnlineSubmission
                        ? "Enabled"
                        : "Disabled"}
                    </span>
                  </div>
                </div>
                <div
                  style={{
                    marginTop: "24px",
                    display: "flex",
                    justifyContent: "flex-end",
                    gap: "12px",
                  }}
                >
                  <button
                    onClick={() => setShowViewModal(false)}
                    className="pmc-button pmc-button-secondary"
                  >
                    Close
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Edit Form Modal */}
        {showEditModal && selectedForm && (
          <div
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
            }}
            onClick={() => !submitting && setShowEditModal(false)}
          >
            <div
              className="pmc-card pmc-fadeIn"
              style={{
                maxWidth: "800px",
                width: "100%",
                maxHeight: "90vh",
                overflow: "auto",
              }}
              onClick={(e) => e.stopPropagation()}
            >
              <div className="pmc-card-header">
                <h2 className="pmc-card-title">Edit Form Configuration</h2>
                <p className="pmc-card-subtitle">
                  Update form details and fees
                </p>
              </div>
              <div className="pmc-card-body" style={{ padding: "24px" }}>
                <form onSubmit={handleSubmitEdit}>
                  <div style={{ display: "grid", gap: "20px" }}>
                    <div>
                      <label className="pmc-form-label" htmlFor="formName">
                        Form Name{" "}
                        <span style={{ color: "var(--pmc-danger)" }}>*</span>
                      </label>
                      <input
                        id="formName"
                        type="text"
                        className="pmc-form-input"
                        value={editForm.formName}
                        onChange={(e) =>
                          setEditForm({ ...editForm, formName: e.target.value })
                        }
                        required
                      />
                    </div>

                    <div>
                      <label className="pmc-form-label" htmlFor="description">
                        Description
                      </label>
                      <textarea
                        id="description"
                        className="pmc-form-input"
                        rows={3}
                        value={editForm.description}
                        onChange={(e) =>
                          setEditForm({
                            ...editForm,
                            description: e.target.value,
                          })
                        }
                      />
                    </div>

                    <div
                      style={{
                        display: "grid",
                        gridTemplateColumns: "1fr 1fr",
                        gap: "16px",
                      }}
                    >
                      <div>
                        <label className="pmc-form-label" htmlFor="baseFee">
                          Base Fee (₹){" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="baseFee"
                          type="number"
                          min="0"
                          step="0.01"
                          className="pmc-form-input"
                          value={editForm.baseFee}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              baseFee: parseFloat(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                      <div>
                        <label
                          className="pmc-form-label"
                          htmlFor="processingFee"
                        >
                          Processing Fee (₹){" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="processingFee"
                          type="number"
                          min="0"
                          step="0.01"
                          className="pmc-form-input"
                          value={editForm.processingFee}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              processingFee: parseFloat(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                      <div>
                        <label className="pmc-form-label" htmlFor="lateFee">
                          Late Fee (₹){" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="lateFee"
                          type="number"
                          min="0"
                          step="0.01"
                          className="pmc-form-input"
                          value={editForm.lateFee}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              lateFee: parseFloat(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                      <div>
                        <label
                          className="pmc-form-label"
                          htmlFor="processingDays"
                        >
                          Processing Days{" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="processingDays"
                          type="number"
                          min="1"
                          className="pmc-form-input"
                          value={editForm.processingDays}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              processingDays: parseInt(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                    </div>

                    <div
                      style={{
                        display: "grid",
                        gridTemplateColumns: "1fr 1fr",
                        gap: "16px",
                      }}
                    >
                      <div>
                        <label
                          className="pmc-form-label"
                          htmlFor="maxFileSizeMB"
                        >
                          Max File Size (MB){" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="maxFileSizeMB"
                          type="number"
                          min="1"
                          max="100"
                          className="pmc-form-input"
                          value={editForm.maxFileSizeMB}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              maxFileSizeMB: parseInt(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                      <div>
                        <label
                          className="pmc-form-label"
                          htmlFor="maxFilesAllowed"
                        >
                          Max Files Allowed{" "}
                          <span style={{ color: "var(--pmc-danger)" }}>*</span>
                        </label>
                        <input
                          id="maxFilesAllowed"
                          type="number"
                          min="1"
                          max="50"
                          className="pmc-form-input"
                          value={editForm.maxFilesAllowed}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              maxFilesAllowed: parseInt(e.target.value),
                            })
                          }
                          required
                        />
                      </div>
                    </div>

                    <div>
                      <label
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                          cursor: "pointer",
                        }}
                      >
                        <input
                          type="checkbox"
                          checked={editForm.allowOnlineSubmission}
                          onChange={(e) =>
                            setEditForm({
                              ...editForm,
                              allowOnlineSubmission: e.target.checked,
                            })
                          }
                          style={{ width: "18px", height: "18px" }}
                        />
                        <span
                          className="pmc-form-label"
                          style={{ marginBottom: 0 }}
                        >
                          Allow Online Submission
                        </span>
                      </label>
                    </div>

                    <div>
                      <label className="pmc-form-label" htmlFor="changeReason">
                        Reason for Change{" "}
                        <span style={{ color: "var(--pmc-danger)" }}>*</span>
                      </label>
                      <textarea
                        id="changeReason"
                        className="pmc-form-input"
                        rows={2}
                        placeholder="Please explain why you're making these changes..."
                        value={editForm.changeReason}
                        onChange={(e) =>
                          setEditForm({
                            ...editForm,
                            changeReason: e.target.value,
                          })
                        }
                        required
                      />
                      <p
                        style={{
                          fontSize: "12px",
                          color: "var(--pmc-gray-600)",
                          marginTop: "4px",
                        }}
                      >
                        This will be recorded in the audit log
                      </p>
                    </div>

                    {error && (
                      <div
                        style={{
                          background: "#fee2e2",
                          border: "1px solid #fecaca",
                          color: "#dc2626",
                          padding: "12px",
                          borderRadius: "6px",
                          fontSize: "14px",
                        }}
                      >
                        {error}
                      </div>
                    )}

                    <div
                      style={{
                        display: "flex",
                        justifyContent: "flex-end",
                        gap: "12px",
                        marginTop: "8px",
                      }}
                    >
                      <button
                        type="button"
                        onClick={() => setShowEditModal(false)}
                        className="pmc-button pmc-button-secondary"
                        disabled={submitting}
                      >
                        Cancel
                      </button>
                      <button
                        type="submit"
                        className="pmc-button pmc-button-primary"
                        disabled={submitting}
                      >
                        {submitting ? "Saving..." : "Save Changes"}
                      </button>
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}
      </div>
    </>
  );
};

export default FormManagementPage;
