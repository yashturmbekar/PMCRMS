import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  adminService,
  type OfficerInvitation,
  type Officer,
  type InviteOfficerRequest,
} from "../services/adminService";
import { useAuth } from "../hooks/useAuth";
import { SUCCESS_MESSAGE_TIMEOUT } from "../constants";
import {
  Users,
  Mail,
  UserPlus,
  RefreshCw,
  Trash2,
  Edit,
  X,
  CheckCircle,
  XCircle,
  ArrowLeft,
  Search,
  Filter,
} from "lucide-react";

const OfficerManagement: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<"officers" | "invitations">(
    "officers"
  );
  const [officers, setOfficers] = useState<Officer[]>([]);
  const [invitations, setInvitations] = useState<OfficerInvitation[]>([]);
  const [filteredOfficers, setFilteredOfficers] = useState<Officer[]>([]);
  const [filteredInvitations, setFilteredInvitations] = useState<
    OfficerInvitation[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");
  const [success, setSuccess] = useState<string>("");
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");

  useEffect(() => {
    if (user?.role !== "Admin") {
      navigate("/dashboard");
      return;
    }
    loadData();
  }, [user, navigate]);

  useEffect(() => {
    filterOfficers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, roleFilter, officers]);

  useEffect(() => {
    filterInvitations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, statusFilter, invitations]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError("");
      const [officersRes, invitationsRes] = await Promise.all([
        adminService.getOfficers(),
        adminService.getInvitations(),
      ]);

      if (officersRes.success && officersRes.data) {
        setOfficers(officersRes.data);
      }
      if (invitationsRes.success && invitationsRes.data) {
        setInvitations(invitationsRes.data);
      }
    } catch (err) {
      console.error("Error loading data:", err);
      setError("Failed to load officer data");
    } finally {
      setLoading(false);
    }
  };

  const filterOfficers = () => {
    let filtered = [...officers];

    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(
        (o) =>
          o.name.toLowerCase().includes(term) ||
          o.email.toLowerCase().includes(term) ||
          o.employeeId.toLowerCase().includes(term)
      );
    }

    if (roleFilter) {
      filtered = filtered.filter((o) => o.role === roleFilter);
    }

    setFilteredOfficers(filtered);
  };

  const filterInvitations = () => {
    let filtered = [...invitations];

    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(
        (i) =>
          i.name.toLowerCase().includes(term) ||
          i.email.toLowerCase().includes(term) ||
          i.employeeId.toLowerCase().includes(term)
      );
    }

    if (statusFilter) {
      filtered = filtered.filter((i) => i.status === statusFilter);
    }

    setFilteredInvitations(filtered);
  };

  const handleResendInvitation = async (invitationId: number) => {
    try {
      const response = await adminService.resendInvitation(invitationId);
      if (response.success) {
        setSuccess("Invitation resent successfully!");
        loadData();
        setTimeout(() => setSuccess(""), SUCCESS_MESSAGE_TIMEOUT);
      } else {
        setError(response.message || "Failed to resend invitation");
      }
    } catch {
      setError("Failed to resend invitation");
    }
  };

  const handleDeleteInvitation = async (invitationId: number) => {
    if (!confirm("Are you sure you want to delete this invitation?")) {
      return;
    }

    try {
      const response = await adminService.deleteInvitation(invitationId);
      if (response.success) {
        setSuccess("Invitation deleted successfully!");
        loadData();
        setTimeout(() => setSuccess(""), SUCCESS_MESSAGE_TIMEOUT);
      } else {
        setError(response.message || "Failed to delete invitation");
      }
    } catch {
      setError("Failed to delete invitation");
    }
  };

  const handleDeactivateOfficer = async (officerId: number) => {
    if (!confirm("Are you sure you want to deactivate this officer?")) {
      return;
    }

    try {
      const response = await adminService.deleteOfficer(officerId);
      if (response.success) {
        setSuccess("Officer deactivated successfully!");
        loadData();
        setTimeout(() => setSuccess(""), SUCCESS_MESSAGE_TIMEOUT);
      } else {
        setError(response.message || "Failed to deactivate officer");
      }
    } catch {
      setError("Failed to deactivate officer");
    }
  };

  const getRoleLabel = (role: string) => {
    const roleLabels: Record<string, string> = {
      Admin: "Admin",
      Clerk: "Clerk",
      JuniorArchitect: "Junior Architect",
      AssistantArchitect: "Assistant Architect",
      JuniorLicenceEngineer: "Junior Licence Engineer",
      AssistantLicenceEngineer: "Assistant Licence Engineer",
      JuniorStructuralEngineer: "Junior Structural Engineer",
      AssistantStructuralEngineer: "Assistant Structural Engineer",
      JuniorSupervisor1: "Junior Supervisor 1",
      AssistantSupervisor1: "Assistant Supervisor 1",
      JuniorSupervisor2: "Junior Supervisor 2",
      AssistantSupervisor2: "Assistant Supervisor 2",
      ExecutiveEngineer: "Executive Engineer",
      CityEngineer: "City Engineer",
    };
    return roleLabels[role] || role;
  };

  const getStatusBadge = (status: string, isExpired?: boolean) => {
    if (isExpired) {
      return (
        <span className="px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800">
          Expired
        </span>
      );
    }

    switch (status) {
      case "Pending":
        return (
          <span className="px-2 py-1 text-xs font-semibold rounded-full bg-yellow-100 text-yellow-800">
            Pending
          </span>
        );
      case "Accepted":
        return (
          <span className="px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800">
            Accepted
          </span>
        );
      case "Revoked":
        return (
          <span className="px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800">
            Revoked
          </span>
        );
      default:
        return (
          <span className="px-2 py-1 text-xs font-semibold rounded-full bg-gray-100 text-gray-800">
            {status}
          </span>
        );
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-pmc-primary mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex justify-between items-center">
            <div className="flex items-center space-x-4">
              <button
                onClick={() => navigate("/admin")}
                className="p-2 hover:bg-gray-100 rounded-lg"
              >
                <ArrowLeft className="w-5 h-5 text-gray-600" />
              </button>
              <div>
                <h1 className="text-3xl font-bold text-gray-900">
                  Officer Management
                </h1>
                <p className="mt-1 text-sm text-gray-500">
                  Invite and manage PMC officers
                </p>
              </div>
            </div>
            <button
              onClick={() => setShowInviteModal(true)}
              className="px-4 py-2 bg-pmc-primary text-white rounded-lg hover:bg-pmc-primary-dark flex items-center space-x-2"
            >
              <UserPlus className="w-4 h-4" />
              <span>Invite Officer</span>
            </button>
          </div>
        </div>
      </div>

      {/* Alerts */}
      {error && (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-6">
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <XCircle className="w-5 h-5 text-red-600" />
              <p className="text-red-800">{error}</p>
            </div>
            <button onClick={() => setError("")}>
              <X className="w-5 h-5 text-red-600" />
            </button>
          </div>
        </div>
      )}

      {success && (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-6">
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <CheckCircle className="w-5 h-5 text-green-600" />
              <p className="text-green-800">{success}</p>
            </div>
            <button onClick={() => setSuccess("")}>
              <X className="w-5 h-5 text-green-600" />
            </button>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Tabs */}
        <div className="bg-white rounded-lg shadow mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex -mb-px">
              <button
                onClick={() => setActiveTab("officers")}
                className={`px-6 py-4 text-sm font-medium border-b-2 ${
                  activeTab === "officers"
                    ? "border-pmc-primary text-pmc-primary"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                }`}
              >
                <div className="flex items-center space-x-2">
                  <Users className="w-4 h-4" />
                  <span>Officers ({officers.length})</span>
                </div>
              </button>
              <button
                onClick={() => setActiveTab("invitations")}
                className={`px-6 py-4 text-sm font-medium border-b-2 ${
                  activeTab === "invitations"
                    ? "border-pmc-primary text-pmc-primary"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                }`}
              >
                <div className="flex items-center space-x-2">
                  <Mail className="w-4 h-4" />
                  <span>Invitations ({invitations.length})</span>
                </div>
              </button>
            </nav>
          </div>

          {/* Search and Filters */}
          <div className="p-6 border-b border-gray-200">
            <div className="flex flex-col md:flex-row md:items-center md:space-x-4 space-y-4 md:space-y-0">
              <div className="flex-1 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search by name, email, or employee ID..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                />
              </div>
              {activeTab === "officers" && (
                <div className="flex items-center space-x-2">
                  <Filter className="w-5 h-5 text-gray-400" />
                  <select
                    value={roleFilter}
                    onChange={(e) => setRoleFilter(e.target.value)}
                    className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                  >
                    <option value="">All Roles</option>
                    <option value="Clerk">Clerk</option>
                    <option value="JuniorArchitect">Junior Architect</option>
                    <option value="AssistantArchitect">
                      Assistant Architect
                    </option>
                    <option value="JuniorLicenceEngineer">
                      Junior Licence Engineer
                    </option>
                    <option value="AssistantLicenceEngineer">
                      Assistant Licence Engineer
                    </option>
                    <option value="JuniorStructuralEngineer">
                      Junior Structural Engineer
                    </option>
                    <option value="AssistantStructuralEngineer">
                      Assistant Structural Engineer
                    </option>
                    <option value="JuniorSupervisor1">
                      Junior Supervisor 1
                    </option>
                    <option value="AssistantSupervisor1">
                      Assistant Supervisor 1
                    </option>
                    <option value="JuniorSupervisor2">
                      Junior Supervisor 2
                    </option>
                    <option value="AssistantSupervisor2">
                      Assistant Supervisor 2
                    </option>
                    <option value="ExecutiveEngineer">
                      Executive Engineer
                    </option>
                    <option value="CityEngineer">City Engineer</option>
                  </select>
                </div>
              )}
              {activeTab === "invitations" && (
                <div className="flex items-center space-x-2">
                  <Filter className="w-5 h-5 text-gray-400" />
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                  >
                    <option value="">All Status</option>
                    <option value="Pending">Pending</option>
                    <option value="Accepted">Accepted</option>
                    <option value="Expired">Expired</option>
                    <option value="Revoked">Revoked</option>
                  </select>
                </div>
              )}
            </div>
          </div>

          {/* Tab Content */}
          {activeTab === "officers" ? (
            <OfficersTable
              officers={filteredOfficers}
              onDeactivate={handleDeactivateOfficer}
              getRoleLabel={getRoleLabel}
            />
          ) : (
            <InvitationsTable
              invitations={filteredInvitations}
              onResend={handleResendInvitation}
              onDelete={handleDeleteInvitation}
              getRoleLabel={getRoleLabel}
              getStatusBadge={getStatusBadge}
            />
          )}
        </div>
      </div>

      {/* Invite Officer Modal */}
      {showInviteModal && (
        <InviteOfficerModal
          onClose={() => setShowInviteModal(false)}
          onSuccess={() => {
            setShowInviteModal(false);
            setSuccess("Officer invitation sent successfully!");
            loadData();
            setTimeout(() => setSuccess(""), SUCCESS_MESSAGE_TIMEOUT);
          }}
          onError={(msg) => setError(msg)}
        />
      )}
    </div>
  );
};

// Officers Table Component
const OfficersTable: React.FC<{
  officers: Officer[];
  onDeactivate: (id: number) => void;
  getRoleLabel: (role: string) => string;
}> = ({ officers, onDeactivate, getRoleLabel }) => {
  if (officers.length === 0) {
    return (
      <div className="p-12 text-center">
        <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">No officers found</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Officer
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Employee ID
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Role
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Last Login
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Applications
            </th>
            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {officers.map((officer) => (
            <tr key={officer.id} className="hover:bg-gray-50">
              <td className="px-6 py-4 whitespace-nowrap">
                <div>
                  <div className="text-sm font-medium text-gray-900">
                    {officer.name}
                  </div>
                  <div className="text-sm text-gray-500">{officer.email}</div>
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {officer.employeeId}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {getRoleLabel(officer.role)}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                {officer.isActive ? (
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-green-100 text-green-800">
                    Active
                  </span>
                ) : (
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800">
                    Inactive
                  </span>
                )}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {officer.lastLoginAt
                  ? new Date(officer.lastLoginAt).toLocaleDateString()
                  : "Never"}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {officer.applicationsProcessed}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <div className="flex justify-end space-x-2">
                  <button
                    className="text-blue-600 hover:text-blue-900"
                    title="Edit Officer"
                  >
                    <Edit className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => onDeactivate(officer.id)}
                    className="text-red-600 hover:text-red-900"
                    title="Deactivate Officer"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

// Invitations Table Component
const InvitationsTable: React.FC<{
  invitations: OfficerInvitation[];
  onResend: (id: number) => void;
  onDelete: (id: number) => void;
  getRoleLabel: (role: string) => string;
  getStatusBadge: (status: string, isExpired?: boolean) => React.ReactElement;
}> = ({ invitations, onResend, onDelete, getRoleLabel, getStatusBadge }) => {
  if (invitations.length === 0) {
    return (
      <div className="p-12 text-center">
        <Mail className="w-12 h-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">No invitations found</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Invitee
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Employee ID
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Role
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Invited
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Expires
            </th>
            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {invitations.map((invitation) => (
            <tr key={invitation.id} className="hover:bg-gray-50">
              <td className="px-6 py-4 whitespace-nowrap">
                <div>
                  <div className="text-sm font-medium text-gray-900">
                    {invitation.name}
                  </div>
                  <div className="text-sm text-gray-500">
                    {invitation.email}
                  </div>
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {invitation.employeeId}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                <div className="text-sm text-gray-900">
                  {getRoleLabel(invitation.role)}
                </div>
              </td>
              <td className="px-6 py-4 whitespace-nowrap">
                {getStatusBadge(invitation.status, invitation.isExpired)}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {new Date(invitation.invitedAt).toLocaleDateString()}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {new Date(invitation.expiresAt).toLocaleDateString()}
              </td>
              <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <div className="flex justify-end space-x-2">
                  {invitation.status === "Pending" && !invitation.isExpired && (
                    <button
                      onClick={() => onResend(invitation.id)}
                      className="text-blue-600 hover:text-blue-900"
                      title="Resend Invitation"
                    >
                      <RefreshCw className="w-4 h-4" />
                    </button>
                  )}
                  <button
                    onClick={() => onDelete(invitation.id)}
                    className="text-red-600 hover:text-red-900"
                    title="Delete Invitation"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

// Invite Officer Modal Component
const InviteOfficerModal: React.FC<{
  onClose: () => void;
  onSuccess: () => void;
  onError: (msg: string) => void;
}> = ({ onClose, onSuccess, onError }) => {
  const [formData, setFormData] = useState<InviteOfficerRequest>({
    name: "",
    email: "",
    phoneNumber: "",
    role: "",
    employeeId: "",
    department: "",
    expiryDays: 7,
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await adminService.inviteOfficer(formData);
      if (response.success) {
        onSuccess();
      } else {
        onError(response.message || "Failed to send invitation");
      }
    } catch {
      onError("Failed to send invitation");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="p-6 border-b border-gray-200 flex justify-between items-center">
          <h2 className="text-2xl font-bold text-gray-900">Invite Officer</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Full Name *
              </label>
              <input
                type="text"
                required
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                placeholder="Enter officer's full name"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Email Address *
              </label>
              <input
                type="email"
                required
                value={formData.email}
                onChange={(e) =>
                  setFormData({ ...formData, email: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                placeholder="officer@pmc.gov.in"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Phone Number
              </label>
              <input
                type="tel"
                value={formData.phoneNumber}
                onChange={(e) =>
                  setFormData({ ...formData, phoneNumber: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                placeholder="9876543210"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Employee ID *
              </label>
              <input
                type="text"
                required
                value={formData.employeeId}
                onChange={(e) =>
                  setFormData({ ...formData, employeeId: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                placeholder="EMP001"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Role *
              </label>
              <select
                required
                value={formData.role}
                onChange={(e) =>
                  setFormData({ ...formData, role: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
              >
                <option value="">Select Role</option>
                <option value="Clerk">Clerk</option>
                <option value="JuniorArchitect">Junior Architect</option>
                <option value="AssistantArchitect">Assistant Architect</option>
                <option value="JuniorLicenceEngineer">
                  Junior Licence Engineer
                </option>
                <option value="AssistantLicenceEngineer">
                  Assistant Licence Engineer
                </option>
                <option value="JuniorStructuralEngineer">
                  Junior Structural Engineer
                </option>
                <option value="AssistantStructuralEngineer">
                  Assistant Structural Engineer
                </option>
                <option value="JuniorSupervisor1">Junior Supervisor 1</option>
                <option value="AssistantSupervisor1">
                  Assistant Supervisor 1
                </option>
                <option value="JuniorSupervisor2">Junior Supervisor 2</option>
                <option value="AssistantSupervisor2">
                  Assistant Supervisor 2
                </option>
                <option value="ExecutiveEngineer">Executive Engineer</option>
                <option value="CityEngineer">City Engineer</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Department
              </label>
              <input
                type="text"
                value={formData.department}
                onChange={(e) =>
                  setFormData({ ...formData, department: e.target.value })
                }
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-pmc-primary focus:border-transparent"
                placeholder="Building Department"
              />
            </div>
          </div>

          <div className="flex justify-end space-x-4 pt-6 border-t border-gray-200">
            <button
              type="button"
              onClick={onClose}
              className="px-6 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-6 py-2 bg-pmc-primary text-white rounded-lg hover:bg-pmc-primary-dark disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? "Sending..." : "Send Invitation"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default OfficerManagement;
