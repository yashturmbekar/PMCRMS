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
    <div className="space-y-6">
      {/* Welcome Section */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-800 rounded-lg px-6 py-8 text-white">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">
              {getGreeting()}, {user?.name}!
            </h1>
            <p className="mt-2 text-blue-100">{getRoleBasedMessage()}</p>
          </div>
          <div className="hidden md:block">
            <Calendar className="h-16 w-16 text-blue-200" />
          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((stat, index) => (
          <div
            key={index}
            className="bg-white overflow-hidden shadow rounded-lg"
          >
            <div className="p-5">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className={`${stat.color} p-3 rounded-md`}>
                    <stat.icon className="h-6 w-6 text-white" />
                  </div>
                </div>
                <div className="ml-5 w-0 flex-1">
                  <dl>
                    <dt className="text-sm font-medium text-gray-500 truncate">
                      {stat.title}
                    </dt>
                    <dd className={`text-lg font-medium ${stat.textColor}`}>
                      {stat.displayValue}
                    </dd>
                  </dl>
                </div>
              </div>
            </div>
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
