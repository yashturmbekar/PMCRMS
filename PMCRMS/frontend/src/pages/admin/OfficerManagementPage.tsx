import React, { useEffect, useState } from "react";
import { adminService, type Officer } from "../../services/adminService";
import { UserPlus, Search, Edit2, Mail, User, Shield } from "lucide-react";
import { PageLoader } from "../../components";

// Available officer designations
const OFFICER_DESIGNATIONS = [
  "JuniorArchitect",
  "AssistantArchitect",
  "JuniorLicenceEngineer",
  "AssistantLicenceEngineer",
  "JuniorStructuralEngineer",
  "AssistantStructuralEngineer",
  "JuniorSupervisor1",
  "AssistantSupervisor1",
  "JuniorSupervisor2",
  "AssistantSupervisor2",
  "ExecutiveEngineer",
  "CityEngineer",
  "Clerk",
];

// Helper function to format designation names with spaces
const formatDesignation = (designation: string): string => {
  // Add space before capital letters and numbers
  return designation
    .replace(/([A-Z])/g, " $1") // Add space before capital letters
    .replace(/([0-9])/g, " $1") // Add space before numbers
    .trim(); // Remove leading space
};

const OfficerManagementPage: React.FC = () => {
  const [officers, setOfficers] = useState<Officer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [selectedOfficer, setSelectedOfficer] = useState<Officer | null>(null);
  const [inviteForm, setInviteForm] = useState({
    name: "",
    email: "",
    role: "",
    employeeId: "",
  });
  const [editForm, setEditForm] = useState({
    name: "",
    email: "",
  });
  const [submitting, setSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    loadOfficers();
  }, []);

  const loadOfficers = async () => {
    try {
      setLoading(true);
      const response = await adminService.getOfficers();
      if (response.success && response.data) {
        setOfficers(response.data as Officer[]);
      }
    } catch (err) {
      console.error("Error loading officers:", err);
      setError("Failed to load officers");
    } finally {
      setLoading(false);
    }
  };

  // Get available designations (exclude already assigned ones)
  const getAvailableDesignations = () => {
    const assignedDesignations = officers.map((officer) => officer.role);
    return OFFICER_DESIGNATIONS.filter(
      (designation) => !assignedDesignations.includes(designation)
    );
  };

  const handleInviteOfficer = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!inviteForm.name || !inviteForm.email || !inviteForm.role) {
      setError("Please fill all required fields");
      return;
    }

    try {
      setSubmitting(true);

      // Backend will auto-generate employeeId if not provided
      const response = await adminService.inviteOfficer({
        name: inviteForm.name,
        email: inviteForm.email,
        role: inviteForm.role,
      });

      if (response.success) {
        setSuccessMessage(
          "Officer invited successfully! Password sent to email."
        );
        setShowInviteModal(false);
        setInviteForm({
          name: "",
          email: "",
          role: "",
          employeeId: "",
        });
        setTimeout(() => setSuccessMessage(""), 3000);
        loadOfficers();
      } else {
        setError(response.message || "Failed to invite officer");
      }
    } catch (err) {
      console.error("Error inviting officer:", err);
      setError("Failed to invite officer");
    } finally {
      setSubmitting(false);
    }
  };

  const handleEditOfficer = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedOfficer || !editForm.name) {
      setError("Please fill all required fields");
      return;
    }

    try {
      setSubmitting(true);
      // TODO: Backend needs to support email updates with password reset
      const response = await adminService.updateOfficer(selectedOfficer.id, {
        userId: selectedOfficer.id,
        name: editForm.name,
        // email: editForm.email, // Not supported in backend yet
        role: selectedOfficer.role, // Keep same role/designation
        isActive: selectedOfficer.isActive,
      });

      if (response.success) {
        setSuccessMessage("Officer updated successfully!");
        setShowEditModal(false);
        setSelectedOfficer(null);
        setEditForm({
          name: "",
          email: "",
        });
        setTimeout(() => setSuccessMessage(""), 3000);
        loadOfficers();
      } else {
        setError(response.message || "Failed to update officer");
      }
    } catch (err) {
      console.error("Error updating officer:", err);
      setError("Failed to update officer");
    } finally {
      setSubmitting(false);
    }
  };

  const openEditModal = (officer: Officer) => {
    setSelectedOfficer(officer);
    setEditForm({
      name: officer.name,
      email: officer.email,
    });
    setShowEditModal(true);
  };

  const filteredOfficers = officers.filter(
    (officer) =>
      officer.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      officer.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      officer.role.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (loading) {
    return <PageLoader message="Loading officers..." />;
  }

  return (
    <div className="pmc-fadeIn" style={{ padding: "24px" }}>
      {/* Header */}
      <div
        style={{
          marginBottom: "32px",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
        }}
      >
        <div>
          <h1
            style={{
              fontSize: "28px",
              fontWeight: "700",
              color: "var(--pmc-gray-900)",
              marginBottom: "8px",
            }}
          >
            Manage Officers
          </h1>
          <p style={{ color: "var(--pmc-gray-600)", fontSize: "14px" }}>
            Manage officer accounts and designations
          </p>
        </div>
        <button
          className="pmc-button pmc-button-primary"
          onClick={() => setShowInviteModal(true)}
          style={{
            display: "flex",
            alignItems: "center",
            gap: "8px",
            padding: "12px 24px",
          }}
          disabled={getAvailableDesignations().length === 0}
        >
          <UserPlus style={{ width: "20px", height: "20px" }} />
          <span>Invite Officer</span>
        </button>
      </div>

      {/* Success Message */}
      {successMessage && (
        <div
          className="pmc-fadeIn"
          style={{
            position: "fixed",
            top: "24px",
            right: "24px",
            backgroundColor: "var(--pmc-success)",
            color: "white",
            padding: "16px 24px",
            borderRadius: "8px",
            boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
            zIndex: 1000,
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <Mail style={{ width: "20px", height: "20px" }} />
          {successMessage}
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div
          className="pmc-card"
          style={{
            marginBottom: "24px",
            padding: "16px",
            backgroundColor: "#FEE2E2",
            borderLeft: "4px solid #EF4444",
          }}
        >
          <p style={{ color: "#DC2626", margin: 0 }}>{error}</p>
        </div>
      )}

      {/* Search Bar */}
      <div
        className="pmc-card"
        style={{ marginBottom: "24px", padding: "16px" }}
      >
        <div style={{ position: "relative" }}>
          <Search
            style={{
              position: "absolute",
              left: "12px",
              top: "50%",
              transform: "translateY(-50%)",
              color: "var(--pmc-gray-400)",
              width: "20px",
              height: "20px",
            }}
          />
          <input
            type="text"
            placeholder="Search by name, email, or designation..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{
              width: "100%",
              padding: "12px 12px 12px 44px",
              border: "1px solid var(--pmc-gray-300)",
              borderRadius: "6px",
              fontSize: "14px",
            }}
          />
        </div>
      </div>

      {/* Officers Table */}
      <div className="pmc-card">
        <div style={{ overflowX: "auto" }}>
          <table className="pmc-table">
            <thead>
              <tr>
                <th style={{ width: "30%" }}>NAME</th>
                <th style={{ width: "35%" }}>EMAIL ADDRESS</th>
                <th style={{ width: "25%" }}>DESIGNATION</th>
                <th style={{ width: "10%", textAlign: "center" }}>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredOfficers.length === 0 ? (
                <tr>
                  <td
                    colSpan={4}
                    style={{ textAlign: "center", padding: "48px" }}
                  >
                    <div className="pmc-empty-state">
                      <User
                        style={{
                          width: "48px",
                          height: "48px",
                          color: "var(--pmc-gray-400)",
                          margin: "0 auto 16px",
                        }}
                      />
                      <p style={{ color: "var(--pmc-gray-600)", margin: 0 }}>
                        {searchTerm ? "No officers found" : "No officers yet"}
                      </p>
                      {!searchTerm && (
                        <p
                          style={{
                            color: "var(--pmc-gray-500)",
                            fontSize: "14px",
                            marginTop: "8px",
                          }}
                        >
                          Click "Invite Officer" to add your first officer
                        </p>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                filteredOfficers.map((officer) => (
                  <tr key={officer.id}>
                    <td>
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <div
                          style={{
                            width: "32px",
                            height: "32px",
                            borderRadius: "50%",
                            background:
                              "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            color: "white",
                            fontWeight: "600",
                            fontSize: "14px",
                          }}
                        >
                          {officer.name.charAt(0).toUpperCase()}
                        </div>
                        <span style={{ fontWeight: "500" }}>
                          {officer.name}
                        </span>
                      </div>
                    </td>
                    <td>
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "6px",
                        }}
                      >
                        <Mail
                          style={{
                            width: "16px",
                            height: "16px",
                            color: "var(--pmc-gray-400)",
                          }}
                        />
                        <span style={{ color: "var(--pmc-gray-600)" }}>
                          {officer.email}
                        </span>
                      </div>
                    </td>
                    <td>
                      <div
                        className="pmc-badge"
                        style={{
                          display: "inline-flex",
                          alignItems: "center",
                          gap: "6px",
                          padding: "6px 12px",
                          background:
                            "linear-gradient(135deg, var(--pmc-primary) 0%, var(--pmc-primary-dark) 100%)",
                          color: "white",
                          borderRadius: "6px",
                          fontSize: "12px",
                          fontWeight: "600",
                        }}
                      >
                        <Shield style={{ width: "14px", height: "14px" }} />
                        {formatDesignation(officer.role)}
                      </div>
                    </td>
                    <td style={{ textAlign: "center" }}>
                      <button
                        className="pmc-button pmc-button-secondary"
                        onClick={() => openEditModal(officer)}
                        style={{
                          padding: "8px 16px",
                          fontSize: "14px",
                          display: "inline-flex",
                          alignItems: "center",
                          gap: "6px",
                        }}
                      >
                        <Edit2 style={{ width: "16px", height: "16px" }} />
                        Edit
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Invite Officer Modal */}
      {showInviteModal && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.5)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 1000,
            padding: "20px",
          }}
          onClick={() => setShowInviteModal(false)}
        >
          <div
            className="pmc-card pmc-fadeIn"
            style={{
              maxWidth: "500px",
              width: "100%",
              maxHeight: "90vh",
              overflowY: "auto",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                padding: "24px",
                borderBottom: "1px solid var(--pmc-gray-200)",
              }}
            >
              <h2
                style={{
                  fontSize: "20px",
                  fontWeight: "700",
                  color: "var(--pmc-gray-900)",
                  margin: 0,
                }}
              >
                Invite Officer
              </h2>
              <p
                style={{
                  color: "var(--pmc-gray-600)",
                  fontSize: "14px",
                  marginTop: "4px",
                  marginBottom: 0,
                }}
              >
                Send invitation with auto-generated password
              </p>
            </div>

            <form onSubmit={handleInviteOfficer} style={{ padding: "24px" }}>
              {/* Name Field */}
              <div style={{ marginBottom: "20px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Full Name *
                </label>
                <div style={{ position: "relative" }}>
                  <User
                    style={{
                      position: "absolute",
                      left: "12px",
                      top: "50%",
                      transform: "translateY(-50%)",
                      color: "var(--pmc-gray-400)",
                      width: "18px",
                      height: "18px",
                    }}
                  />
                  <input
                    type="text"
                    required
                    value={inviteForm.name}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, name: e.target.value })
                    }
                    placeholder="Enter officer's full name"
                    style={{
                      width: "100%",
                      padding: "12px 12px 12px 40px",
                      border: "1px solid var(--pmc-gray-300)",
                      borderRadius: "6px",
                      fontSize: "14px",
                    }}
                  />
                </div>
              </div>

              {/* Email Field */}
              <div style={{ marginBottom: "20px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Email Address *
                </label>
                <div style={{ position: "relative" }}>
                  <Mail
                    style={{
                      position: "absolute",
                      left: "12px",
                      top: "50%",
                      transform: "translateY(-50%)",
                      color: "var(--pmc-gray-400)",
                      width: "18px",
                      height: "18px",
                    }}
                  />
                  <input
                    type="email"
                    required
                    value={inviteForm.email}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, email: e.target.value })
                    }
                    placeholder="officer@example.com"
                    style={{
                      width: "100%",
                      padding: "12px 12px 12px 40px",
                      border: "1px solid var(--pmc-gray-300)",
                      borderRadius: "6px",
                      fontSize: "14px",
                    }}
                  />
                </div>
              </div>

              {/* Designation Field */}
              <div style={{ marginBottom: "24px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Designation *
                </label>
                <div style={{ position: "relative" }}>
                  <Shield
                    style={{
                      position: "absolute",
                      left: "12px",
                      top: "50%",
                      transform: "translateY(-50%)",
                      color: "var(--pmc-gray-400)",
                      width: "18px",
                      height: "18px",
                    }}
                  />
                  <select
                    required
                    value={inviteForm.role}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, role: e.target.value })
                    }
                    style={{
                      width: "100%",
                      padding: "12px 12px 12px 40px",
                      border: "1px solid var(--pmc-gray-300)",
                      borderRadius: "6px",
                      fontSize: "14px",
                      backgroundColor: "white",
                    }}
                  >
                    <option value="">Select a designation</option>
                    {getAvailableDesignations().map((designation) => (
                      <option key={designation} value={designation}>
                        {formatDesignation(designation)}
                      </option>
                    ))}
                  </select>
                </div>
                {getAvailableDesignations().length === 0 && (
                  <p
                    style={{
                      color: "var(--pmc-warning)",
                      fontSize: "12px",
                      marginTop: "4px",
                    }}
                  >
                    All designations have been assigned
                  </p>
                )}
              </div>

              {/* Action Buttons */}
              <div
                style={{
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="button"
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowInviteModal(false)}
                  disabled={submitting}
                  style={{ padding: "10px 20px" }}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="pmc-button pmc-button-primary"
                  disabled={
                    submitting || getAvailableDesignations().length === 0
                  }
                  style={{ padding: "10px 20px" }}
                >
                  {submitting ? "Sending..." : "Send Invitation"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Officer Modal */}
      {showEditModal && selectedOfficer && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(0, 0, 0, 0.5)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 1000,
            padding: "20px",
          }}
          onClick={() => setShowEditModal(false)}
        >
          <div
            className="pmc-card pmc-fadeIn"
            style={{
              maxWidth: "500px",
              width: "100%",
              maxHeight: "90vh",
              overflowY: "auto",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              style={{
                padding: "24px",
                borderBottom: "1px solid var(--pmc-gray-200)",
              }}
            >
              <h2
                style={{
                  fontSize: "20px",
                  fontWeight: "700",
                  color: "var(--pmc-gray-900)",
                  margin: 0,
                }}
              >
                Edit Officer
              </h2>
              <p
                style={{
                  color: "var(--pmc-gray-600)",
                  fontSize: "14px",
                  marginTop: "4px",
                  marginBottom: 0,
                }}
              >
                Update officer name
              </p>
            </div>

            <form onSubmit={handleEditOfficer} style={{ padding: "24px" }}>
              {/* Designation Display (Read-only) */}
              <div style={{ marginBottom: "20px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Designation
                </label>
                <div
                  style={{
                    padding: "12px",
                    backgroundColor: "var(--pmc-gray-100)",
                    borderRadius: "6px",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <Shield
                    style={{
                      width: "18px",
                      height: "18px",
                      color: "var(--pmc-primary)",
                    }}
                  />
                  <span
                    style={{ fontWeight: "600", color: "var(--pmc-gray-700)" }}
                  >
                    {formatDesignation(selectedOfficer.role)}
                  </span>
                </div>
              </div>

              {/* Name Field */}
              <div style={{ marginBottom: "20px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Full Name *
                </label>
                <div style={{ position: "relative" }}>
                  <User
                    style={{
                      position: "absolute",
                      left: "12px",
                      top: "50%",
                      transform: "translateY(-50%)",
                      color: "var(--pmc-gray-400)",
                      width: "18px",
                      height: "18px",
                    }}
                  />
                  <input
                    type="text"
                    required
                    value={editForm.name}
                    onChange={(e) =>
                      setEditForm({ ...editForm, name: e.target.value })
                    }
                    placeholder="Enter officer's full name"
                    style={{
                      width: "100%",
                      padding: "12px 12px 12px 40px",
                      border: "1px solid var(--pmc-gray-300)",
                      borderRadius: "6px",
                      fontSize: "14px",
                    }}
                  />
                </div>
              </div>

              {/* Email Field (Read-only) */}
              <div style={{ marginBottom: "24px" }}>
                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: "600",
                    color: "var(--pmc-gray-700)",
                    fontSize: "14px",
                  }}
                >
                  Email Address
                </label>
                <div
                  style={{
                    padding: "12px",
                    backgroundColor: "var(--pmc-gray-100)",
                    borderRadius: "6px",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <Mail
                    style={{
                      width: "18px",
                      height: "18px",
                      color: "var(--pmc-primary)",
                    }}
                  />
                  <span style={{ color: "var(--pmc-gray-600)" }}>
                    {selectedOfficer.email}
                  </span>
                </div>
                <p
                  style={{
                    color: "var(--pmc-gray-500)",
                    fontSize: "12px",
                    marginTop: "4px",
                  }}
                >
                  Email address cannot be changed
                </p>
              </div>

              {/* Warning Message */}
              <div
                style={{
                  padding: "12px",
                  backgroundColor: "#FEF3C7",
                  borderLeft: "4px solid #F59E0B",
                  borderRadius: "6px",
                  marginBottom: "20px",
                  display: "none", // Hidden for now
                }}
              >
                <p style={{ color: "#92400E", fontSize: "13px", margin: 0 }}>
                  ⚠️ A new password will be generated and sent to the updated
                  email address
                </p>
              </div>

              {/* Action Buttons */}
              <div
                style={{
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="button"
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowEditModal(false)}
                  disabled={submitting}
                  style={{ padding: "10px 20px" }}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="pmc-button pmc-button-primary"
                  disabled={submitting}
                  style={{ padding: "10px 20px" }}
                >
                  {submitting ? "Updating..." : "Update Officer"}
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
