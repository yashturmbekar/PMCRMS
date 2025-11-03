import React, { useEffect, useState } from "react";
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
} from "lucide-react";
import { PageLoader } from "../../components";
import NotificationModal from "../../components/common/NotificationModal";
import type { NotificationType } from "../../components/common/NotificationModal";

const FormManagementPage: React.FC = () => {
  const [forms, setForms] = useState<FormConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
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

  const handleDeleteForm = async (_formId: number, formName: string) => {
    if (!confirm(`Are you sure you want to delete the form "${formName}"?`))
      return;

    try {
      setSuccessMessage(
        `Form "${formName}" deleted successfully (not implemented)!`
      );
      setTimeout(() => setSuccessMessage(""), SUCCESS_MESSAGE_TIMEOUT);
      loadForms();
    } catch (err) {
      console.error("Error deleting form:", err);
      setError("Failed to delete form");
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
      <div className="pmc-fadeIn">
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
                                onClick={() =>
                                  setNotification({
                                    isOpen: true,
                                    message:
                                      "Form details viewing functionality will be available soon!",
                                    type: "info",
                                    title: "Coming Soon",
                                    autoClose: false,
                                  })
                                }
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
                                onClick={() =>
                                  setNotification({
                                    isOpen: true,
                                    message:
                                      "Form editing functionality will be available soon!",
                                    type: "info",
                                    title: "Coming Soon",
                                    autoClose: false,
                                  })
                                }
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
      </div>
    </>
  );
};

export default FormManagementPage;
