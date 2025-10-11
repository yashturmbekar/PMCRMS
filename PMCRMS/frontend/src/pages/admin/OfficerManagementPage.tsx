import React, { useEffect, useState } from "react";
import {
  adminService,
  type Officer,
  type OfficerInvitation,
} from "../../services/adminService";
import {
  UserPlus,
  Mail,
  UserCheck,
  UserX,
  Search,
  Shield,
  Trash2,
  CheckCircle,
  XCircle,
} from "lucide-react";
import { PageLoader } from "../../components";

const OfficerManagementPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<"officers" | "invitations">(
    "officers"
  );
  const [officers, setOfficers] = useState<Officer[]>([]);
  const [invitations, setInvitations] = useState<OfficerInvitation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteForm, setInviteForm] = useState({
    name: "",
    email: "",
    role: "",
    employeeId: "",
    department: "",
  });
  const [submitting, setSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    try {
      setLoading(true);
      if (activeTab === "officers") {
        const response = await adminService.getOfficers();
        if (response.success && response.data) {
          setOfficers(response.data as Officer[]);
        }
      } else {
        const response = await adminService.getInvitations();
        if (response.success && response.data) {
          setInvitations(response.data as OfficerInvitation[]);
        }
      }
    } catch (err) {
      console.error("Error loading data:", err);
      setError("Failed to load data");
    } finally {
      setLoading(false);
    }
  };

  const handleInviteOfficer = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      const response = await adminService.inviteOfficer(inviteForm);
      if (response.success) {
        setSuccessMessage("Invitation sent successfully!");
        setShowInviteModal(false);
        setInviteForm({
          name: "",
          email: "",
          role: "",
          employeeId: "",
          department: "",
        });
        setTimeout(() => setSuccessMessage(""), 3000);
        loadData();
      } else {
        setError(response.message || "Failed to send invitation");
      }
    } catch (err) {
      console.error("Error sending invitation:", err);
      setError("Failed to send invitation");
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleOfficerStatus = async (
    officerId: number,
    currentStatus: boolean
  ) => {
    try {
      const response = await adminService.updateOfficer(officerId, {
        userId: officerId,
        isActive: !currentStatus,
      });
      if (response.success) {
        setSuccessMessage(
          `Officer ${
            !currentStatus ? "activated" : "deactivated"
          } successfully!`
        );
        setTimeout(() => setSuccessMessage(""), 3000);
        loadData();
      } else {
        setError(response.message || "Failed to update officer status");
      }
    } catch (err) {
      console.error("Error updating officer:", err);
      setError("Failed to update officer status");
    }
  };

  const handleCancelInvitation = async (invitationId: number) => {
    if (!confirm("Are you sure you want to cancel this invitation?")) return;

    try {
      // TODO: Implement backend endpoint for cancelling invitations
      console.log("Cancelling invitation:", invitationId);
      setSuccessMessage("Invitation cancelled successfully (not implemented)!");
      setTimeout(() => setSuccessMessage(""), 3000);
      loadData();
    } catch (err) {
      console.error("Error cancelling invitation:", err);
      setError("Failed to cancel invitation");
    }
  };

  const filteredOfficers = officers.filter(
    (officer) =>
      officer.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      officer.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      officer.role.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredInvitations = invitations.filter(
    (inv) =>
      inv.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      inv.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      inv.role.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (loading && !officers.length && !invitations.length) {
    return <PageLoader message="Loading Officers..." />;
  }

  return (
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
              Officer Management 👥
            </h1>
            <p
              className="pmc-content-subtitle pmc-text-base"
              style={{ color: "var(--pmc-gray-600)" }}
            >
              Manage officers and invitations
            </p>
          </div>
          <button
            onClick={() => setShowInviteModal(true)}
            className="pmc-button pmc-button-primary"
            style={{
              padding: "12px 24px",
              display: "flex",
              alignItems: "center",
              gap: "8px",
            }}
          >
            <UserPlus style={{ width: "18px", height: "18px" }} />
            <span className="pmc-font-semibold">Invite Officer</span>
          </button>
        </div>
      </div>

      <div
        className="pmc-card pmc-fadeInUp"
        style={{ marginBottom: "24px", padding: "20px" }}
      >
        <div style={{ position: "relative" }}>
          <Search
            style={{
              position: "absolute",
              left: "12px",
              top: "50%",
              transform: "translateY(-50%)",
              width: "18px",
              height: "18px",
              color: "var(--pmc-gray-400)",
            }}
          />
          <input
            type="text"
            placeholder="Search officers or invitations..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pmc-input"
            style={{
              paddingLeft: "40px",
              width: "100%",
            }}
          />
        </div>
      </div>

      <div className="pmc-card pmc-slideInLeft">
        <div
          className="pmc-card-header"
          style={{ borderBottom: "1px solid var(--pmc-gray-200)" }}
        >
          <div style={{ display: "flex", gap: "8px" }}>
            <button
              onClick={() => setActiveTab("officers")}
              style={{
                padding: "12px 24px",
                background:
                  activeTab === "officers"
                    ? "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)"
                    : "transparent",
                color:
                  activeTab === "officers" ? "white" : "var(--pmc-gray-700)",
                border: "none",
                borderRadius: "6px",
                cursor: "pointer",
                fontWeight: 600,
                fontSize: "14px",
                transition: "all 0.3s ease",
              }}
            >
              Active Officers ({officers.length})
            </button>
            <button
              onClick={() => setActiveTab("invitations")}
              style={{
                padding: "12px 24px",
                background:
                  activeTab === "invitations"
                    ? "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)"
                    : "transparent",
                color:
                  activeTab === "invitations" ? "white" : "var(--pmc-gray-700)",
                border: "none",
                borderRadius: "6px",
                cursor: "pointer",
                fontWeight: 600,
                fontSize: "14px",
                transition: "all 0.3s ease",
              }}
            >
              Pending Invitations ({invitations.length})
            </button>
          </div>
        </div>

        <div className="pmc-card-body">
          {activeTab === "officers" ? (
            filteredOfficers.length === 0 ? (
              <div
                style={{
                  padding: "48px 24px",
                  textAlign: "center",
                  color: "var(--pmc-gray-500)",
                }}
              >
                <UserCheck
                  style={{
                    width: "48px",
                    height: "48px",
                    margin: "0 auto 16px",
                    opacity: 0.3,
                  }}
                />
                <p className="pmc-text-base pmc-font-medium">
                  No officers found
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
                          Name
                        </th>
                        <th
                          className="pmc-text-xs pmc-font-semibold"
                          style={{
                            textTransform: "uppercase",
                            letterSpacing: "0.05em",
                            color: "var(--pmc-gray-700)",
                          }}
                        >
                          Email
                        </th>
                        <th
                          className="pmc-text-xs pmc-font-semibold"
                          style={{
                            textTransform: "uppercase",
                            letterSpacing: "0.05em",
                            color: "var(--pmc-gray-700)",
                          }}
                        >
                          Role
                        </th>
                        <th
                          className="pmc-text-xs pmc-font-semibold"
                          style={{
                            textTransform: "uppercase",
                            letterSpacing: "0.05em",
                            color: "var(--pmc-gray-700)",
                          }}
                        >
                          Department
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
                      {filteredOfficers.map((officer) => (
                        <tr key={officer.id}>
                          <td
                            className="pmc-text-sm pmc-font-medium"
                            style={{ color: "var(--pmc-gray-800)" }}
                          >
                            {officer.name}
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {officer.email}
                          </td>
                          <td>
                            <span className="pmc-badge pmc-status-under-review">
                              <Shield
                                style={{
                                  width: "12px",
                                  height: "12px",
                                  marginRight: "4px",
                                }}
                              />
                              {officer.role}
                            </span>
                          </td>
                          <td
                            className="pmc-text-sm"
                            style={{ color: "var(--pmc-gray-600)" }}
                          >
                            {officer.employeeId}
                          </td>
                          <td>
                            <span
                              className={`pmc-badge ${
                                officer.isActive
                                  ? "pmc-status-approved"
                                  : "pmc-status-rejected"
                              }`}
                            >
                              {officer.isActive ? "Active" : "Inactive"}
                            </span>
                          </td>
                          <td>
                            <button
                              onClick={() =>
                                handleToggleOfficerStatus(
                                  officer.id,
                                  officer.isActive
                                )
                              }
                              className={`pmc-button ${
                                officer.isActive
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
                              {officer.isActive ? (
                                <>
                                  <UserX
                                    style={{ width: "14px", height: "14px" }}
                                  />
                                  Deactivate
                                </>
                              ) : (
                                <>
                                  <UserCheck
                                    style={{ width: "14px", height: "14px" }}
                                  />
                                  Activate
                                </>
                              )}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )
          ) : filteredInvitations.length === 0 ? (
            <div
              style={{
                padding: "48px 24px",
                textAlign: "center",
                color: "var(--pmc-gray-500)",
              }}
            >
              <Mail
                style={{
                  width: "48px",
                  height: "48px",
                  margin: "0 auto 16px",
                  opacity: 0.3,
                }}
              />
              <p className="pmc-text-base pmc-font-medium">
                No pending invitations
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
                        Email
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Role
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Department
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Invited By
                      </th>
                      <th
                        className="pmc-text-xs pmc-font-semibold"
                        style={{
                          textTransform: "uppercase",
                          letterSpacing: "0.05em",
                          color: "var(--pmc-gray-700)",
                        }}
                      >
                        Expires
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
                    {filteredInvitations.map((invitation) => (
                      <tr key={invitation.id}>
                        <td
                          className="pmc-text-sm pmc-font-medium"
                          style={{ color: "var(--pmc-gray-800)" }}
                        >
                          {invitation.email}
                        </td>
                        <td>
                          <span className="pmc-badge pmc-status-under-review">
                            {invitation.role}
                          </span>
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {invitation.department || "N/A"}
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {invitation.invitedByName}
                        </td>
                        <td
                          className="pmc-text-sm"
                          style={{ color: "var(--pmc-gray-600)" }}
                        >
                          {new Date(invitation.expiresAt).toLocaleDateString()}
                        </td>
                        <td>
                          <button
                            onClick={() =>
                              handleCancelInvitation(invitation.id)
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
                            <Trash2 style={{ width: "14px", height: "14px" }} />
                            Cancel
                          </button>
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

      {showInviteModal && (
        <div
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
          }}
          className="pmc-fadeIn"
          onClick={() => setShowInviteModal(false)}
        >
          <div
            className="pmc-card"
            style={{
              width: "90%",
              maxWidth: "500px",
              padding: "0",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="pmc-card-header">
              <h2 className="pmc-card-title">Invite New Officer</h2>
              <p className="pmc-card-subtitle">
                Send an invitation email to a new officer
              </p>
            </div>
            <form onSubmit={handleInviteOfficer}>
              <div
                className="pmc-card-body"
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "16px",
                }}
              >
                <div>
                  <label className="pmc-label">
                    Full Name{" "}
                    <span style={{ color: "var(--pmc-danger)" }}>*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={inviteForm.name}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, name: e.target.value })
                    }
                    className="pmc-input"
                    placeholder="John Doe"
                  />
                </div>
                <div>
                  <label className="pmc-label">
                    Email Address{" "}
                    <span style={{ color: "var(--pmc-danger)" }}>*</span>
                  </label>
                  <input
                    type="email"
                    required
                    value={inviteForm.email}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, email: e.target.value })
                    }
                    className="pmc-input"
                    placeholder="officer@pmc.gov"
                  />
                </div>
                <div>
                  <label className="pmc-label">
                    Employee ID{" "}
                    <span style={{ color: "var(--pmc-danger)" }}>*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={inviteForm.employeeId}
                    onChange={(e) =>
                      setInviteForm({
                        ...inviteForm,
                        employeeId: e.target.value,
                      })
                    }
                    className="pmc-input"
                    placeholder="EMP-12345"
                  />
                </div>
                <div>
                  <label className="pmc-label">
                    Role <span style={{ color: "var(--pmc-danger)" }}>*</span>
                  </label>
                  <select
                    required
                    value={inviteForm.role}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, role: e.target.value })
                    }
                    className="pmc-input"
                  >
                    <option value="">Select a role</option>
                    <option value="Reviewer">Reviewer</option>
                    <option value="Approver">Approver</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <div>
                  <label className="pmc-label">
                    Department{" "}
                    <span style={{ color: "var(--pmc-danger)" }}>*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={inviteForm.department}
                    onChange={(e) =>
                      setInviteForm({
                        ...inviteForm,
                        department: e.target.value,
                      })
                    }
                    className="pmc-input"
                    placeholder="e.g., Building Department"
                  />
                </div>
              </div>
              <div
                style={{
                  padding: "16px 24px",
                  borderTop: "1px solid var(--pmc-gray-200)",
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="button"
                  onClick={() => setShowInviteModal(false)}
                  className="pmc-button pmc-button-secondary"
                  disabled={submitting}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="pmc-button pmc-button-primary"
                  disabled={submitting}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <Mail style={{ width: "16px", height: "16px" }} />
                  {submitting ? "Sending..." : "Send Invitation"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default OfficerManagementPage;
