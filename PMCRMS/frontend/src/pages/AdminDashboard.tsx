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
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header with gradient */}
      <div className="bg-gradient-to-r from-blue-900 via-blue-800 to-blue-900 shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-white flex items-center gap-3">
                <div className="bg-white/10 p-2 rounded-lg backdrop-blur-sm">
                  <Settings className="w-8 h-8 text-white" />
                </div>
                Admin Dashboard
              </h1>
              <p className="mt-2 text-blue-200 font-medium">
                Pune Municipal Corporation - PMCRMS Management
              </p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => navigate("/admin/officers")}
                className="pmc-button pmc-button-primary flex items-center gap-2 shadow-lg hover:shadow-xl transition-all"
              >
                <UserPlus className="w-4 h-4" />
                <span>Invite Officer</span>
              </button>
              <button
                onClick={() => navigate("/admin/forms")}
                className="px-4 py-2 bg-white/10 backdrop-blur-sm text-white rounded-lg border border-white/20 hover:bg-white/20 transition-all flex items-center gap-2"
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
        {/* Statistics Cards with enhanced styling */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {/* Total Applications */}
          <div className="pmc-card group hover:shadow-xl transition-all duration-300 border-l-4 border-blue-500">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                  Total Applications
                </p>
                <p className="text-4xl font-bold text-gray-900 mt-3">
                  {stats.totalApplications}
                </p>
              </div>
              <div className="bg-gradient-to-br from-blue-500 to-blue-600 p-4 rounded-xl shadow-lg group-hover:scale-110 transition-transform">
                <FileText className="w-8 h-8 text-white" />
              </div>
            </div>
          </div>

          {/* Pending Applications */}
          <div className="pmc-card group hover:shadow-xl transition-all duration-300 border-l-4 border-orange-500">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                  Pending Applications
                </p>
                <p className="text-4xl font-bold text-orange-600 mt-3">
                  {stats.pendingApplications}
                </p>
              </div>
              <div className="bg-gradient-to-br from-orange-500 to-orange-600 p-4 rounded-xl shadow-lg group-hover:scale-110 transition-transform">
                <Clock className="w-8 h-8 text-white" />
              </div>
            </div>
          </div>

          {/* Approved Applications */}
          <div className="pmc-card group hover:shadow-xl transition-all duration-300 border-l-4 border-green-500">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                  Approved Applications
                </p>
                <p className="text-4xl font-bold text-green-600 mt-3">
                  {stats.approvedApplications}
                </p>
              </div>
              <div className="bg-gradient-to-br from-green-500 to-green-600 p-4 rounded-xl shadow-lg group-hover:scale-110 transition-transform">
                <CheckCircle className="w-8 h-8 text-white" />
              </div>
            </div>
          </div>

          {/* Rejected Applications */}
          <div className="pmc-card group hover:shadow-xl transition-all duration-300 border-l-4 border-red-500">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                  Rejected Applications
                </p>
                <p className="text-4xl font-bold text-red-600 mt-3">
                  {stats.rejectedApplications}
                </p>
              </div>
              <div className="bg-gradient-to-br from-red-500 to-red-600 p-4 rounded-xl shadow-lg group-hover:scale-110 transition-transform">
                <XCircle className="w-8 h-8 text-white" />
              </div>
            </div>
          </div>
        </div>

        {/* Officers & Invitations Row with enhanced styling */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          {/* Total Officers */}
          <div className="pmc-card hover:shadow-xl transition-all duration-300">
            <div className="flex items-center justify-between mb-4">
              <div className="bg-gradient-to-br from-purple-500 to-purple-600 p-3 rounded-xl shadow-lg">
                <Users className="w-7 h-7 text-white" />
              </div>
            </div>
            <div>
              <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide mb-2">
                Total Officers
              </p>
              <p className="text-4xl font-bold text-gray-900">
                {stats.totalOfficers}
              </p>
              <div className="mt-3 flex items-center gap-2">
                <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold bg-green-100 text-green-700">
                  {stats.activeOfficers} Active
                </span>
              </div>
            </div>
          </div>

          {/* Pending Invitations */}
          <div className="pmc-card hover:shadow-xl transition-all duration-300">
            <div className="flex items-center justify-between mb-4">
              <div className="bg-gradient-to-br from-indigo-500 to-indigo-600 p-3 rounded-xl shadow-lg">
                <Mail className="w-7 h-7 text-white" />
              </div>
            </div>
            <div>
              <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide mb-2">
                Pending Invitations
              </p>
              <p className="text-4xl font-bold text-indigo-600">
                {stats.pendingInvitations}
              </p>
            </div>
          </div>

          {/* Revenue This Month */}
          <div className="pmc-card hover:shadow-xl transition-all duration-300 bg-gradient-to-br from-green-50 to-emerald-50">
            <div className="flex items-center justify-between mb-4">
              <div className="bg-gradient-to-br from-green-500 to-green-600 p-3 rounded-xl shadow-lg">
                <DollarSign className="w-7 h-7 text-white" />
              </div>
            </div>
            <div>
              <p className="text-sm font-semibold text-gray-600 uppercase tracking-wide mb-2">
                Revenue This Month
              </p>
              <p className="text-3xl font-bold text-green-700">
                {formatCurrency(stats.revenueThisMonth)}
              </p>
              <p className="text-sm text-gray-600 mt-2 font-medium">
                Total: {formatCurrency(stats.totalRevenueCollected)}
              </p>
            </div>
          </div>
        </div>

        {/* Charts Row with enhanced styling */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {/* Application Trends */}
          <div className="pmc-card hover:shadow-xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-200">
              <div className="bg-gradient-to-br from-blue-500 to-blue-600 p-2 rounded-lg shadow-md">
                <TrendingUp className="w-6 h-6 text-white" />
              </div>
              <h3 className="text-xl font-bold text-gray-900">
                Application Trends (Last 7 Days)
              </h3>
            </div>
            <div className="space-y-3">
              {stats.applicationTrends.length > 0 ? (
                stats.applicationTrends.map((trend, index) => (
                  <div
                    key={index}
                    className="group flex items-center justify-between p-4 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-xl hover:from-blue-100 hover:to-indigo-100 transition-all border border-blue-200 hover:border-blue-400 hover:shadow-md"
                  >
                    <span className="text-sm font-bold text-gray-800">
                      {new Date(trend.date).toLocaleDateString("en-IN", {
                        weekday: "short",
                        month: "short",
                        day: "numeric",
                      })}
                    </span>
                    <div className="flex items-center gap-4">
                      <span className="text-xs font-semibold text-gray-600 bg-white px-3 py-1 rounded-full uppercase tracking-wide">
                        {trend.status}
                      </span>
                      <span className="text-2xl font-bold text-blue-700 bg-blue-200 px-4 py-1 rounded-lg group-hover:scale-110 transition-transform">
                        {trend.count}
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 bg-gradient-to-br from-gray-50 to-gray-100 rounded-xl border-2 border-dashed border-gray-300">
                  <TrendingUp className="w-12 h-12 text-gray-400 mx-auto mb-3" />
                  <p className="text-gray-500 font-medium">No data available</p>
                </div>
              )}
            </div>
          </div>

          {/* Role Distribution */}
          <div className="pmc-card hover:shadow-xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-200">
              <div className="bg-gradient-to-br from-purple-500 to-purple-600 p-2 rounded-lg shadow-md">
                <Users className="w-6 h-6 text-white" />
              </div>
              <h3 className="text-xl font-bold text-gray-900">
                Officer Role Distribution
              </h3>
            </div>
            <div className="space-y-4">
              {stats.roleDistribution.length > 0 ? (
                stats.roleDistribution.map((role, index) => (
                  <div key={index} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-bold text-gray-800">
                        {getRoleLabel(role.role)}
                      </span>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold text-green-600 bg-green-100 px-3 py-1 rounded-full">
                          {role.activeCount} Active
                        </span>
                        <span className="text-sm font-semibold text-gray-600 bg-gray-200 px-3 py-1 rounded-full">
                          {role.count} Total
                        </span>
                      </div>
                    </div>
                    <div className="relative w-full bg-gray-200 rounded-full h-3 overflow-hidden shadow-inner">
                      <div
                        className="absolute top-0 left-0 h-3 bg-gradient-to-r from-purple-500 to-purple-600 rounded-full transition-all duration-500 ease-out shadow-md"
                        style={{
                          width: `${
                            role.count > 0
                              ? (role.activeCount / role.count) * 100
                              : 0
                          }%`,
                        }}
                      ></div>
                    </div>
                    <p className="text-xs text-gray-600 font-medium">
                      {role.count > 0
                        ? ((role.activeCount / role.count) * 100).toFixed(1)
                        : 0}
                      % Active Rate
                    </p>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 bg-gradient-to-br from-gray-50 to-gray-100 rounded-xl border-2 border-dashed border-gray-300">
                  <Users className="w-12 h-12 text-gray-400 mx-auto mb-3" />
                  <p className="text-gray-500 font-medium">
                    No officers registered yet
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Quick Actions with enhanced styling */}
        <div className="pmc-card hover:shadow-xl transition-all duration-300">
          <div className="flex items-center gap-3 mb-6 pb-4 border-b border-gray-200">
            <div className="bg-gradient-to-br from-orange-500 to-orange-600 p-2 rounded-lg shadow-md">
              <Settings className="w-6 h-6 text-white" />
            </div>
            <h3 className="text-xl font-bold text-gray-900">Quick Actions</h3>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <button
              onClick={() => navigate("/admin/applications")}
              className="group bg-gradient-to-br from-blue-50 to-blue-100 border-2 border-blue-200 rounded-xl p-6 hover:from-blue-500 hover:to-blue-600 hover:border-blue-600 transition-all duration-300 hover:shadow-xl hover:scale-105 text-left"
            >
              <div className="bg-blue-500 p-3 rounded-lg mb-4 inline-block group-hover:bg-white transition-colors shadow-md">
                <FileText className="w-7 h-7 text-white group-hover:text-blue-600" />
              </div>
              <h4 className="font-bold text-lg text-blue-900 group-hover:text-white transition-colors mb-2">
                All Applications
              </h4>
              <p className="text-sm text-blue-700 group-hover:text-blue-100 transition-colors">
                View and manage all applications
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/officers")}
              className="group bg-gradient-to-br from-purple-50 to-purple-100 border-2 border-purple-200 rounded-xl p-6 hover:from-purple-500 hover:to-purple-600 hover:border-purple-600 transition-all duration-300 hover:shadow-xl hover:scale-105 text-left"
            >
              <div className="bg-purple-500 p-3 rounded-lg mb-4 inline-block group-hover:bg-white transition-colors shadow-md">
                <Users className="w-7 h-7 text-white group-hover:text-purple-600" />
              </div>
              <h4 className="font-bold text-lg text-purple-900 group-hover:text-white transition-colors mb-2">
                Officer Management
              </h4>
              <p className="text-sm text-purple-700 group-hover:text-purple-100 transition-colors">
                Invite and manage officers
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/forms")}
              className="group bg-gradient-to-br from-green-50 to-green-100 border-2 border-green-200 rounded-xl p-6 hover:from-green-500 hover:to-green-600 hover:border-green-600 transition-all duration-300 hover:shadow-xl hover:scale-105 text-left"
            >
              <div className="bg-green-500 p-3 rounded-lg mb-4 inline-block group-hover:bg-white transition-colors shadow-md">
                <Settings className="w-7 h-7 text-white group-hover:text-green-600" />
              </div>
              <h4 className="font-bold text-lg text-green-900 group-hover:text-white transition-colors mb-2">
                Form Configuration
              </h4>
              <p className="text-sm text-green-700 group-hover:text-green-100 transition-colors">
                Manage forms, fees, and fields
              </p>
            </button>

            <button
              onClick={() => navigate("/admin/reports")}
              className="group bg-gradient-to-br from-orange-50 to-orange-100 border-2 border-orange-200 rounded-xl p-6 hover:from-orange-500 hover:to-orange-600 hover:border-orange-600 transition-all duration-300 hover:shadow-xl hover:scale-105 text-left"
            >
              <div className="bg-orange-500 p-3 rounded-lg mb-4 inline-block group-hover:bg-white transition-colors shadow-md">
                <TrendingUp className="w-7 h-7 text-white group-hover:text-orange-600" />
              </div>
              <h4 className="font-bold text-lg text-orange-900 group-hover:text-white transition-colors mb-2">
                Reports
              </h4>
              <p className="text-sm text-orange-700 group-hover:text-orange-100 transition-colors">
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
