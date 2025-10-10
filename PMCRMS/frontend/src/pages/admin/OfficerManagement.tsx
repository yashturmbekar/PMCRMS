import React, { useEffect, useState, useCallback } from "react";
import {
  adminService,
  type Officer,
  type OfficerInvitation,
} from "../../services/adminService";
import { OfficerRoles } from "../../types/admin";
import {
  TrashIcon,
  ArrowPathIcon,
  XCircleIcon,
  UserPlusIcon,
} from "@heroicons/react/24/outline";

const OfficerManagement: React.FC = () => {
  const [officers, setOfficers] = useState<Officer[]>([]);
  const [invitations, setInvitations] = useState<OfficerInvitation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [activeTab, setActiveTab] = useState<"officers" | "invitations">(
    "officers"
  );

  // Invite form state
  const [inviteForm, setInviteForm] = useState({
    name: "",
    email: "",
    role: "",
    employeeId: "",
    phoneNumber: "",
    department: "",
  });
  const [inviteLoading, setInviteLoading] = useState(false);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError("");

      if (activeTab === "officers") {
        const response = await adminService.getOfficers();
        if (response.success && response.data) {
          setOfficers(response.data);
        }
      } else {
        const response = await adminService.getInvitations();
        if (response.success && response.data) {
          setInvitations(response.data);
        }
      }
    } catch (err) {
      console.error("Error loading data:", err);
      setError("Failed to load data");
    } finally {
      setLoading(false);
    }
  }, [activeTab]);

  const handleInviteOfficer = async (e: React.FormEvent) => {
    e.preventDefault();
    setInviteLoading(true);
    setError("");
    setSuccess("");

    try {
      const response = await adminService.inviteOfficer({
        name: inviteForm.name,
        email: inviteForm.email,
        role: inviteForm.role,
        employeeId: inviteForm.employeeId,
        phoneNumber: inviteForm.phoneNumber || undefined,
        department: inviteForm.department || undefined,
      });

      if (response.success) {
        setSuccess("Officer invitation sent successfully!");
        setShowInviteModal(false);
        setInviteForm({
          name: "",
          email: "",
          role: "",
          employeeId: "",
          phoneNumber: "",
          department: "",
        });
        loadData();
      } else {
        setError(response.message || "Failed to send invitation");
      }
    } catch (err) {
      console.error("Error inviting officer:", err);
      setError("Failed to send invitation");
    } finally {
      setInviteLoading(false);
    }
  };

  const handleResendInvitation = async (invitationId: number) => {
    try {
      const response = await adminService.resendInvitation(invitationId);
      if (response.success) {
        setSuccess("Invitation resent successfully!");
        loadData();
      } else {
        setError(response.message || "Failed to resend invitation");
      }
    } catch (err) {
      console.error("Error resending invitation:", err);
      setError("Failed to resend invitation");
    }
  };

  const handleRevokeInvitation = async (invitationId: number) => {
    if (!confirm("Are you sure you want to revoke this invitation?")) return;

    try {
      const response = await adminService.revokeInvitation(invitationId);
      if (response.success) {
        setSuccess("Invitation revoked successfully!");
        loadData();
      } else {
        setError(response.message || "Failed to revoke invitation");
      }
    } catch (err) {
      console.error("Error revoking invitation:", err);
      setError("Failed to revoke invitation");
    }
  };

  const handleDeleteOfficer = async (officerId: number) => {
    if (
      !confirm(
        "Are you sure you want to delete this officer? This action cannot be undone."
      )
    ) {
      return;
    }

    try {
      const response = await adminService.deleteOfficer(officerId);
      if (response.success) {
        setSuccess("Officer deleted successfully!");
        loadData();
      } else {
        setError(response.message || "Failed to delete officer");
      }
    } catch (err) {
      console.error("Error deleting officer:", err);
      setError("Failed to delete officer");
    }
  };

  const getRoleLabel = (roleValue: string): string => {
    const role = OfficerRoles.find((r) => r.value === roleValue);
    return role ? role.label : roleValue;
  };

  return (
    <div className="p-6 bg-gray-50">
      {/* Header */}
      <div className="mb-6">
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-3xl font-bold text-gray-800">
              Officer Management
            </h1>
            <p className="text-gray-600 mt-1">
              Manage officer invitations and accounts
            </p>
          </div>
          <button
            onClick={() => setShowInviteModal(true)}
            className="flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors"
          >
            <UserPlusIcon className="h-5 w-5" />
            Invite Officer
          </button>
        </div>
      </div>

      {/* Success/Error Messages */}
      {success && (
        <div className="mb-4 bg-green-50 border border-green-200 text-green-800 px-4 py-3 rounded-lg">
          {success}
          <button
            onClick={() => setSuccess("")}
            className="float-right font-bold"
          >
            ×
          </button>
        </div>
      )}
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-lg">
          {error}
          <button
            onClick={() => setError("")}
            className="float-right font-bold"
          >
            ×
          </button>
        </div>
      )}

      {/* Tabs */}
      <div className="mb-6 border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab("officers")}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === "officers"
                ? "border-indigo-500 text-indigo-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            }`}
          >
            Active Officers ({officers.length})
          </button>
          <button
            onClick={() => setActiveTab("invitations")}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === "invitations"
                ? "border-indigo-500 text-indigo-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            }`}
          >
            Pending Invitations ({invitations.length})
          </button>
        </nav>
      </div>

      {/* Loading State */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
        </div>
      ) : (
        <>
          {/* Officers Table */}
          {activeTab === "officers" && (
            <div className="bg-white shadow-md rounded-lg overflow-hidden">
              {officers.length === 0 ? (
                <div className="text-center py-12 text-gray-500">
                  No officers found. Invite officers to get started.
                </div>
              ) : (
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Email
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Phone
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Role
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {officers.map((officer) => (
                      <tr key={officer.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="font-medium text-gray-900">
                            {officer.name}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">
                            {officer.email}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">
                            {officer.phoneNumber || "-"}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                            {getRoleLabel(officer.role)}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span
                            className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
                              officer.isActive
                                ? "bg-green-100 text-green-800"
                                : "bg-gray-100 text-gray-800"
                            }`}
                          >
                            {officer.isActive ? "Active" : "Inactive"}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <button
                            onClick={() => handleDeleteOfficer(officer.id)}
                            className="text-red-600 hover:text-red-900"
                            title="Delete Officer"
                          >
                            <TrashIcon className="h-5 w-5" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}

          {/* Invitations Table */}
          {activeTab === "invitations" && (
            <div className="bg-white shadow-md rounded-lg overflow-hidden">
              {invitations.length === 0 ? (
                <div className="text-center py-12 text-gray-500">
                  No pending invitations.
                </div>
              ) : (
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Email
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Role
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Sent Date
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Expires
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {invitations.map((invitation) => (
                      <tr key={invitation.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">
                            {invitation.email}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className="px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                            {getRoleLabel(invitation.role)}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span
                            className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
                              invitation.status === "Accepted"
                                ? "bg-green-100 text-green-800"
                                : invitation.status === "Revoked"
                                ? "bg-red-100 text-red-800"
                                : "bg-yellow-100 text-yellow-800"
                            }`}
                          >
                            {invitation.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {new Date(invitation.invitedAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {new Date(invitation.expiresAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                          {invitation.status === "Pending" && (
                            <>
                              <button
                                onClick={() =>
                                  handleResendInvitation(invitation.id)
                                }
                                className="text-blue-600 hover:text-blue-900"
                                title="Resend Invitation"
                              >
                                <ArrowPathIcon className="h-5 w-5 inline" />
                              </button>
                              <button
                                onClick={() =>
                                  handleRevokeInvitation(invitation.id)
                                }
                                className="text-red-600 hover:text-red-900"
                                title="Revoke Invitation"
                              >
                                <XCircleIcon className="h-5 w-5 inline" />
                              </button>
                            </>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}
        </>
      )}

      {/* Invite Modal */}
      {showInviteModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <div className="mt-3">
              <h3 className="text-lg font-medium leading-6 text-gray-900 mb-4">
                Invite Officer
              </h3>
              <form onSubmit={handleInviteOfficer} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Full Name
                  </label>
                  <input
                    type="text"
                    required
                    value={inviteForm.name}
                    onChange={(e) =>
                      setInviteForm({
                        ...inviteForm,
                        name: e.target.value,
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Employee ID
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
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Email
                  </label>
                  <input
                    type="email"
                    required
                    value={inviteForm.email}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, email: e.target.value })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Phone Number
                  </label>
                  <input
                    type="tel"
                    value={inviteForm.phoneNumber}
                    onChange={(e) =>
                      setInviteForm({
                        ...inviteForm,
                        phoneNumber: e.target.value,
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Role
                  </label>
                  <select
                    required
                    value={inviteForm.role}
                    onChange={(e) =>
                      setInviteForm({ ...inviteForm, role: e.target.value })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  >
                    <option value="">Select a role</option>
                    {OfficerRoles.map((role) => (
                      <option key={role.value} value={role.value}>
                        {role.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Department (Optional)
                  </label>
                  <input
                    type="text"
                    value={inviteForm.department}
                    onChange={(e) =>
                      setInviteForm({
                        ...inviteForm,
                        department: e.target.value,
                      })
                    }
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
                  />
                </div>
                <div className="flex gap-2 pt-4">
                  <button
                    type="submit"
                    disabled={inviteLoading}
                    className="flex-1 bg-indigo-600 text-white px-4 py-2 rounded-md hover:bg-indigo-700 disabled:bg-gray-400"
                  >
                    {inviteLoading ? "Sending..." : "Send Invitation"}
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowInviteModal(false)}
                    className="flex-1 bg-gray-200 text-gray-800 px-4 py-2 rounded-md hover:bg-gray-300"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default OfficerManagement;
