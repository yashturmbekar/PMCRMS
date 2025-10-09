import React, { useState, useEffect } from "react";
import { useAuth } from "../hooks/useAuth";
import { apiService } from "../services/apiService";
import {
  FileText,
  TrendingUp,
  Clock,
  CheckCircle,
  XCircle,
  DollarSign,
  Calendar,
  BarChart3,
} from "lucide-react";
import type { DashboardStats, Application } from "../types";

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentApplications, setRecentApplications] = useState<Application[]>(
    []
  );
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setIsLoading(true);

        // Fetch dashboard stats
        const dashboardStats = await apiService.getDashboardStats();
        setStats(dashboardStats);

        // Fetch recent applications based on user role
        const applications = await apiService.getApplications(1, 5);
        setRecentApplications(applications.data);
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return "Good Morning";
    if (hour < 17) return "Good Afternoon";
    return "Good Evening";
  };

  const getRoleBasedMessage = () => {
    switch (user?.role) {
      case "Applicant":
        return "Track your building permit applications and manage documents.";
      case "JuniorEngineer":
        return "Review and process applications assigned to you.";
      case "AssistantEngineer":
        return "Monitor application workflow and provide technical reviews.";
      case "ExecutiveEngineer":
        return "Oversee application approvals and digital signatures.";
      case "CityEngineer":
        return "Final approvals and certificate issuance oversight.";
      case "Clerk":
        return "Process payments and manage administrative tasks.";
      case "Admin":
        return "System administration and user management.";
      default:
        return "Welcome to the PMCRMS system.";
    }
  };

  const statCards = [
    {
      title: "Total Applications",
      value: stats?.totalApplications || 0,
      displayValue: (stats?.totalApplications || 0).toString(),
      icon: FileText,
      color: "bg-blue-500",
      textColor: "text-blue-600",
    },
    {
      title: "Pending Applications",
      value: stats?.pendingApplications || 0,
      displayValue: (stats?.pendingApplications || 0).toString(),
      icon: Clock,
      color: "bg-yellow-500",
      textColor: "text-yellow-600",
    },
    {
      title: "Approved Applications",
      value: stats?.approvedApplications || 0,
      displayValue: (stats?.approvedApplications || 0).toString(),
      icon: CheckCircle,
      color: "bg-green-500",
      textColor: "text-green-600",
    },
    {
      title: "Rejected Applications",
      value: stats?.rejectedApplications || 0,
      displayValue: (stats?.rejectedApplications || 0).toString(),
      icon: XCircle,
      color: "bg-red-500",
      textColor: "text-red-600",
    },
  ];

  // Add financial stats for relevant roles
  if (
    ["Admin", "Clerk", "ExecutiveEngineer", "CityEngineer"].includes(
      user?.role || ""
    )
  ) {
    statCards.push(
      {
        title: "Payments Completed",
        value: stats?.paymentsCompleted || 0,
        displayValue: (stats?.paymentsCompleted || 0).toString(),
        icon: DollarSign,
        color: "bg-purple-500",
        textColor: "text-purple-600",
      },
      {
        title: "Total Revenue",
        value: stats?.totalRevenue || 0,
        displayValue: `₹${(stats?.totalRevenue || 0).toLocaleString()}`,
        icon: TrendingUp,
        color: "bg-indigo-500",
        textColor: "text-indigo-600",
      }
    );
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case "Completed":
        return "bg-green-100 text-green-800";
      case "Submitted":
        return "bg-blue-100 text-blue-800";
      case "UnderReviewByJE":
      case "UnderReviewByAE":
      case "UnderReviewByEE1":
      case "UnderReviewByCE1":
        return "bg-yellow-100 text-yellow-800";
      case "RejectedByJE":
      case "RejectedByAE":
      case "RejectedByEE1":
      case "RejectedByCE1":
        return "bg-red-100 text-red-800";
      case "PaymentPending":
        return "bg-orange-100 text-orange-800";
      case "PaymentCompleted":
        return "bg-green-100 text-green-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  const formatStatus = (status: string) => {
    return status
      .replace(/([A-Z])/g, " $1")
      .replace(/^./, (str) => str.toUpperCase());
  };

  return (
    <div className="space-y-8 pmc-fadeIn">
      {/* Welcome Section */}
      <div className="relative overflow-hidden bg-gradient-to-br from-slate-800 via-slate-700 to-indigo-900 rounded-2xl shadow-2xl">
        {/* Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="w-full h-full"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='40' height='40' viewBox='0 0 40 40' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='%23ffffff' fill-opacity='0.1'%3E%3Cpath d='M20 20c0-5.5-4.5-10-10-10s-10 4.5-10 10 4.5 10 10 10 10-4.5 10-10zm10 0c0 5.5 4.5 10 10 10s10-4.5 10-10-4.5-10-10-10-10 4.5-10 10z'/%3E%3C/g%3E%3C/svg%3E")`,
              backgroundSize: "40px 40px",
            }}
          />
        </div>

        {/* Gradient Accents */}
        <div className="absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl from-blue-400/20 to-transparent rounded-full blur-2xl"></div>
        <div className="absolute bottom-0 left-0 w-24 h-24 bg-gradient-to-tr from-indigo-400/20 to-transparent rounded-full blur-xl"></div>

        <div className="relative px-8 py-10 text-white">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-4 mb-4">
                <div className="w-14 h-14 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-xl flex items-center justify-center shadow-lg">
                  <span className="text-white font-bold text-lg">
                    {user?.name?.charAt(0).toUpperCase()}
                  </span>
                </div>
                <div>
                  <h1 className="text-3xl font-bold leading-tight">
                    {getGreeting()}, {user?.name}!
                  </h1>
                  <div className="flex items-center gap-2 mt-1">
                    <div className="w-2 h-2 bg-emerald-400 rounded-full"></div>
                    <span className="text-blue-200 font-medium">
                      {user?.role}
                    </span>
                  </div>
                </div>
              </div>
              <p className="text-lg text-slate-200 max-w-2xl leading-relaxed">
                {getRoleBasedMessage()}
              </p>
            </div>
            <div className="hidden lg:block">
              <div className="w-24 h-24 bg-white/10 backdrop-blur-sm rounded-2xl flex items-center justify-center border border-white/20">
                <Calendar className="h-12 w-12 text-blue-200" />
              </div>
            </div>
          </div>

          {/* Quick Stats Preview */}
          <div className="mt-8 grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4 border border-white/20">
              <div className="text-sm text-blue-200">Total</div>
              <div className="text-2xl font-bold text-white">
                {stats?.totalApplications || 0}
              </div>
            </div>
            <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4 border border-white/20">
              <div className="text-sm text-yellow-200">Pending</div>
              <div className="text-2xl font-bold text-white">
                {stats?.pendingApplications || 0}
              </div>
            </div>
            <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4 border border-white/20">
              <div className="text-sm text-emerald-200">Approved</div>
              <div className="text-2xl font-bold text-white">
                {stats?.approvedApplications || 0}
              </div>
            </div>
            <div className="bg-white/10 backdrop-blur-sm rounded-xl p-4 border border-white/20">
              <div className="text-sm text-red-200">Rejected</div>
              <div className="text-2xl font-bold text-white">
                {stats?.rejectedApplications || 0}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Enhanced Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {statCards.map((stat, index) => (
          <div
            key={index}
            className="group bg-white/80 backdrop-blur-sm rounded-2xl shadow-lg hover:shadow-xl border border-slate-200/50 overflow-hidden transition-all duration-300 hover:scale-105 pmc-slideInLeft"
            style={{ animationDelay: `${index * 100}ms` }}
          >
            <div className="p-6">
              <div className="flex items-center justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-4">
                    <div
                      className={`${stat.color} p-3 rounded-xl shadow-lg group-hover:scale-110 transition-transform duration-300`}
                    >
                      <stat.icon className="h-6 w-6 text-white" />
                    </div>
                    <div>
                      <dt className="text-sm font-semibold text-slate-600 uppercase tracking-wide">
                        {stat.title}
                      </dt>
                      <dd
                        className={`text-2xl font-bold ${stat.textColor} group-hover:scale-105 transition-transform duration-300`}
                      >
                        {stat.displayValue}
                      </dd>
                    </div>
                  </div>
                </div>
              </div>

              {/* Progress indicator */}
              <div className="mt-4">
                <div className="w-full bg-slate-100 rounded-full h-1.5">
                  <div
                    className={`${stat.color.replace(
                      "bg-",
                      "bg-"
                    )} h-1.5 rounded-full transition-all duration-1000 ease-out`}
                    style={{
                      width: `${Math.min(
                        (stat.value /
                          Math.max(...statCards.map((s) => s.value))) *
                          100,
                        100
                      )}%`,
                      animationDelay: `${index * 200 + 500}ms`,
                    }}
                  ></div>
                </div>
              </div>
            </div>

            {/* Hover Effect Overlay */}
            <div className="absolute inset-0 bg-gradient-to-br from-transparent to-slate-50/50 opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none"></div>
          </div>
        ))}
      </div>

      {/* Recent Applications */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg leading-6 font-medium text-gray-900">
              Recent Applications
            </h3>
            <a
              href="/applications"
              className="text-sm text-blue-600 hover:text-blue-500"
            >
              View all
            </a>
          </div>

          {recentApplications.length > 0 ? (
            <div className="overflow-hidden">
              <ul className="divide-y divide-gray-200">
                {recentApplications.map((application) => (
                  <li key={application.id} className="py-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center">
                        <div className="flex-shrink-0">
                          <FileText className="h-8 w-8 text-gray-400" />
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {application.applicationNumber}
                          </div>
                          <div className="text-sm text-gray-500">
                            {application.propertyDetails.location}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center space-x-4">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(
                            application.currentStatus
                          )}`}
                        >
                          {formatStatus(application.currentStatus)}
                        </span>
                        <div className="text-sm text-gray-500">
                          {new Date(
                            application.submissionDate
                          ).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          ) : (
            <div className="text-center py-8">
              <FileText className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-sm font-medium text-gray-900">
                No applications
              </h3>
              <p className="mt-1 text-sm text-gray-500">
                {user?.role === "Applicant"
                  ? "Get started by creating a new application."
                  : "No applications to review at this time."}
              </p>
              {user?.role === "Applicant" && (
                <div className="mt-6">
                  <a
                    href="/applications/new"
                    className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                  >
                    Create New Application
                  </a>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
            Quick Actions
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {user?.role === "Applicant" && (
              <a
                href="/applications/new"
                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
              >
                <FileText className="mr-2 h-4 w-4" />
                New Application
              </a>
            )}

            <a
              href="/applications"
              className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
            >
              <FileText className="mr-2 h-4 w-4" />
              View Applications
            </a>

            {["Admin", "ExecutiveEngineer", "CityEngineer"].includes(
              user?.role || ""
            ) && (
              <a
                href="/reports"
                className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                <BarChart3 className="mr-2 h-4 w-4" />
                View Reports
              </a>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
