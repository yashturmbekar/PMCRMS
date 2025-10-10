import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  adminService,
  type AdminDashboardStats,
} from "../services/adminService";
import { useAuth } from "../hooks/useAuth";
import {
  Users,
  FileText,
  Clock,
  CheckCircle,
  XCircle,
  TrendingUp,
  Mail,
  DollarSign,
  Settings,
  UserPlus,
} from "lucide-react";

const AdminDashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");

  useEffect(() => {
    // Verify admin access
    if (user?.role !== "Admin") {
      navigate("/dashboard");
      return;
    }
    loadDashboardData();
  }, [user, navigate]);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      setError("");
      const response = await adminService.getDashboardStats();
      if (response.success && response.data) {
        setStats(response.data);
      } else {
        setError(response.message || "Failed to load dashboard data");
      }
    } catch (err) {
      console.error("Error loading admin dashboard:", err);
      setError(
        err instanceof Error ? err.message : "Failed to load dashboard data"
      );
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(amount);
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

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-pmc-primary mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading dashboard...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6">
            <h3 className="text-red-800 font-semibold mb-2">Error</h3>
            <p className="text-red-600">{error}</p>
            <button
              onClick={loadDashboardData}
              className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (!stats) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">
                Admin Dashboard
              </h1>
              <p className="mt-1 text-sm text-gray-500">
                Pune Municipal Corporation - PMCRMS Management
              </p>
            </div>
            <div className="flex space-x-3">
              <button
                onClick={() => navigate("/admin/officers")}
                className="px-4 py-2 bg-pmc-primary text-white rounded-lg hover:bg-pmc-primary-dark flex items-center space-x-2"
              >
                <UserPlus className="w-4 h-4" />
                <span>Invite Officer</span>
              </button>
              <button
                onClick={() => navigate("/admin/forms")}
                className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 flex items-center space-x-2"
              >
                <Settings className="w-4 h-4" />
                <span>Manage Forms</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Statistics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {/* Total Applications */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Total Applications
                </p>
                <p className="text-3xl font-bold text-gray-900 mt-2">
                  {stats.totalApplications}
                </p>
              </div>
              <div className="bg-blue-100 p-3 rounded-full">
                <FileText className="w-6 h-6 text-blue-600" />
              </div>
            </div>
          </div>

          {/* Pending Applications */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Pending Applications
                </p>
                <p className="text-3xl font-bold text-orange-600 mt-2">
                  {stats.pendingApplications}
                </p>
              </div>
              <div className="bg-orange-100 p-3 rounded-full">
                <Clock className="w-6 h-6 text-orange-600" />
              </div>
            </div>
          </div>

          {/* Approved Applications */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Approved Applications
                </p>
                <p className="text-3xl font-bold text-green-600 mt-2">
                  {stats.approvedApplications}
                </p>
              </div>
              <div className="bg-green-100 p-3 rounded-full">
                <CheckCircle className="w-6 h-6 text-green-600" />
              </div>
            </div>
          </div>

          {/* Rejected Applications */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Rejected Applications
                </p>
                <p className="text-3xl font-bold text-red-600 mt-2">
                  {stats.rejectedApplications}
                </p>
              </div>
              <div className="bg-red-100 p-3 rounded-full">
                <XCircle className="w-6 h-6 text-red-600" />
              </div>
            </div>
          </div>
        </div>

        {/* Officers & Invitations Row */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          {/* Total Officers */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Total Officers
                </p>
                <p className="text-3xl font-bold text-gray-900 mt-2">
                  {stats.totalOfficers}
                </p>
                <p className="text-sm text-green-600 mt-1">
                  {stats.activeOfficers} Active
                </p>
              </div>
              <div className="bg-purple-100 p-3 rounded-full">
                <Users className="w-6 h-6 text-purple-600" />
              </div>
            </div>
          </div>

          {/* Pending Invitations */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Pending Invitations
                </p>
                <p className="text-3xl font-bold text-blue-600 mt-2">
                  {stats.pendingInvitations}
                </p>
              </div>
              <div className="bg-blue-100 p-3 rounded-full">
                <Mail className="w-6 h-6 text-blue-600" />
              </div>
            </div>
          </div>

          {/* Revenue This Month */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">
                  Revenue This Month
                </p>
                <p className="text-2xl font-bold text-green-600 mt-2">
                  {formatCurrency(stats.revenueThisMonth)}
                </p>
                <p className="text-sm text-gray-500 mt-1">
                  Total: {formatCurrency(stats.totalRevenueCollected)}
                </p>
              </div>
              <div className="bg-green-100 p-3 rounded-full">
                <DollarSign className="w-6 h-6 text-green-600" />
              </div>
            </div>
          </div>
        </div>

        {/* Charts Row */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {/* Application Trends */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <TrendingUp className="w-5 h-5 mr-2 text-pmc-primary" />
              Application Trends (Last 7 Days)
            </h3>
            <div className="space-y-3">
              {stats.applicationTrends.length > 0 ? (
                stats.applicationTrends.map((trend, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                  >
                    <span className="text-sm font-medium text-gray-700">
                      {new Date(trend.date).toLocaleDateString("en-IN", {
                        weekday: "short",
                        month: "short",
                        day: "numeric",
                      })}
                    </span>
                    <div className="flex items-center space-x-3">
                      <span className="text-xs text-gray-500">
                        {trend.status}
                      </span>
                      <span className="text-lg font-bold text-pmc-primary">
                        {trend.count}
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-center text-gray-500 py-8">
                  No data available
                </p>
              )}
            </div>
          </div>

          {/* Role Distribution */}
          <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
              <Users className="w-5 h-5 mr-2 text-pmc-primary" />
              Officer Role Distribution
            </h3>
            <div className="space-y-3">
              {stats.roleDistribution.length > 0 ? (
                stats.roleDistribution.map((role, index) => (
                  <div key={index} className="space-y-1">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-gray-700">
                        {getRoleLabel(role.role)}
                      </span>
                      <span className="text-sm text-gray-600">
                        {role.activeCount} / {role.count}
                      </span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-pmc-primary h-2 rounded-full transition-all duration-300"
                        style={{
                          width: `${
                            role.count > 0
                              ? (role.activeCount / role.count) * 100
                              : 0
                          }%`,
                        }}
                      ></div>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-center text-gray-500 py-8">
                  No officers registered yet
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="bg-white rounded-lg shadow p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Quick Actions
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <button
              onClick={() => navigate("/admin/applications")}
              className="p-4 border border-gray-300 rounded-lg hover:border-pmc-primary hover:bg-blue-50 transition-colors text-left"
            >
              <FileText className="w-6 h-6 text-pmc-primary mb-2" />
              <h4 className="font-semibold text-gray-900">All Applications</h4>
              <p className="text-sm text-gray-600 mt-1">
                View and manage all applications
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/officers")}
              className="p-4 border border-gray-300 rounded-lg hover:border-pmc-primary hover:bg-blue-50 transition-colors text-left"
            >
              <Users className="w-6 h-6 text-pmc-primary mb-2" />
              <h4 className="font-semibold text-gray-900">
                Officer Management
              </h4>
              <p className="text-sm text-gray-600 mt-1">
                Invite and manage officers
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/forms")}
              className="p-4 border border-gray-300 rounded-lg hover:border-pmc-primary hover:bg-blue-50 transition-colors text-left"
            >
              <Settings className="w-6 h-6 text-pmc-primary mb-2" />
              <h4 className="font-semibold text-gray-900">
                Form Configuration
              </h4>
              <p className="text-sm text-gray-600 mt-1">
                Manage forms, fees, and fields
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/reports")}
              className="p-4 border border-gray-300 rounded-lg hover:border-pmc-primary hover:bg-blue-50 transition-colors text-left"
            >
              <TrendingUp className="w-6 h-6 text-pmc-primary mb-2" />
              <h4 className="font-semibold text-gray-900">Reports</h4>
              <p className="text-sm text-gray-600 mt-1">
                Generate and view reports
              </p>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
