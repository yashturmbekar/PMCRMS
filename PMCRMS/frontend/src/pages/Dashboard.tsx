import React from "react";
import { Link, useNavigate } from "react-router-dom";

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const user = JSON.parse(localStorage.getItem("user") || "{}");

  const handleLogout = () => {
    localStorage.removeItem("auth-token");
    localStorage.removeItem("user");
    navigate("/login");
  };

  const stats = [
    {
      title: "Total Registrations",
      value: "1,234",
      change: "+12%",
      changeType: "positive",
    },
    {
      title: "Pending Applications",
      value: "56",
      change: "-8%",
      changeType: "negative",
    },
    {
      title: "Approved Today",
      value: "23",
      change: "+15%",
      changeType: "positive",
    },
    {
      title: "Active Users",
      value: "89",
      change: "+5%",
      changeType: "positive",
    },
  ];

  const recentApplications = [
    {
      id: "001",
      name: "Rajesh Kumar",
      type: "New Registration",
      status: "Pending",
      date: "2024-01-15",
    },
    {
      id: "002",
      name: "Priya Sharma",
      type: "Renewal",
      status: "Approved",
      date: "2024-01-15",
    },
    {
      id: "003",
      name: "Amit Patel",
      type: "Amendment",
      status: "Under Review",
      date: "2024-01-14",
    },
    {
      id: "004",
      name: "Sunita Devi",
      type: "New Registration",
      status: "Approved",
      date: "2024-01-14",
    },
    {
      id: "005",
      name: "Vikram Singh",
      type: "Renewal",
      status: "Rejected",
      date: "2024-01-13",
    },
  ];

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Approved":
        return "pmc-badge pmc-badge-success";
      case "Pending":
        return "pmc-badge pmc-badge-warning";
      case "Under Review":
        return "pmc-badge pmc-badge-info";
      case "Rejected":
        return "pmc-badge pmc-badge-error";
      default:
        return "pmc-badge pmc-badge-info";
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="pmc-header">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center">
              <img
                src="/pmc-logo.png"
                alt="PMC Logo"
                className="h-10 w-10 mr-3"
                onError={(e) => {
                  const target = e.target as HTMLImageElement;
                  target.style.display = "none";
                  target.parentElement!.innerHTML = `
                    <div class="h-10 w-10 mr-3 bg-blue-100 rounded-lg flex items-center justify-center border border-blue-200">
                      <span class="text-blue-600 font-bold text-xs">PMC</span>
                    </div>
                  `;
                }}
              />
              <div>
                <h1 className="text-xl font-bold text-gray-900">PMC RMS</h1>
                <p className="text-sm text-gray-600">
                  Registration Management System
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-4">
              <div className="text-right">
                <p className="text-sm font-medium text-gray-900">
                  {user.name || "Admin User"}
                </p>
                <p className="text-xs text-gray-500">
                  {user.role || "Administrator"}
                </p>
              </div>
              <button
                onClick={handleLogout}
                className="pmc-button pmc-button-secondary text-sm"
              >
                <svg
                  className="w-4 h-4 mr-2"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fillRule="evenodd"
                    d="M3 3a1 1 0 00-1 1v12a1 1 0 102 0V4a1 1 0 00-1-1zm10.293 9.293a1 1 0 001.414 1.414l3-3a1 1 0 000-1.414l-3-3a1 1 0 10-1.414 1.414L14.586 9H7a1 1 0 100 2h7.586l-1.293 1.293z"
                    clipRule="evenodd"
                  />
                </svg>
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Welcome Section */}
        <div className="mb-8">
          <h2 className="text-2xl font-bold text-gray-900 mb-2">
            Welcome back, {user.name || "Admin"}! 👋
          </h2>
          <p className="text-gray-600">
            Here's what's happening with your registrations today.
          </p>
        </div>

        {/* Statistics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {stats.map((stat, index) => (
            <div key={index} className="pmc-card p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600 mb-1">
                    {stat.title}
                  </p>
                  <p className="text-2xl font-bold text-gray-900">
                    {stat.value}
                  </p>
                </div>
                <div
                  className={`flex items-center text-sm font-medium ${
                    stat.changeType === "positive"
                      ? "text-green-600"
                      : "text-red-600"
                  }`}
                >
                  {stat.changeType === "positive" ? (
                    <svg
                      className="w-4 h-4 mr-1"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M3.293 9.707a1 1 0 010-1.414l6-6a1 1 0 011.414 0l6 6a1 1 0 01-1.414 1.414L11 5.414V17a1 1 0 11-2 0V5.414L4.707 9.707a1 1 0 01-1.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  ) : (
                    <svg
                      className="w-4 h-4 mr-1"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.707 10.293a1 1 0 010 1.414l-6 6a1 1 0 01-1.414 0l-6-6a1 1 0 111.414-1.414L9 14.586V3a1 1 0 012 0v11.586l4.293-4.293a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                  {stat.change}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Quick Actions */}
        <div className="mb-8">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Quick Actions
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Link
              to="/applications/new"
              className="pmc-card p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex items-center">
                <div className="bg-blue-100 rounded-lg p-3 mr-4">
                  <svg
                    className="w-6 h-6 text-blue-600"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div>
                  <h4 className="font-semibold text-gray-900">
                    New Registration
                  </h4>
                  <p className="text-sm text-gray-600">
                    Create a new application
                  </p>
                </div>
              </div>
            </Link>

            <Link
              to="/applications"
              className="pmc-card p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex items-center">
                <div className="bg-green-100 rounded-lg p-3 mr-4">
                  <svg
                    className="w-6 h-6 text-green-600"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M4 4a2 2 0 00-2 2v8a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2H4zm0 2v8h12V6H4z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
                <div>
                  <h4 className="font-semibold text-gray-900">
                    View Applications
                  </h4>
                  <p className="text-sm text-gray-600">
                    Manage all applications
                  </p>
                </div>
              </div>
            </Link>

            <Link
              to="/reports"
              className="pmc-card p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex items-center">
                <div className="bg-purple-100 rounded-lg p-3 mr-4">
                  <svg
                    className="w-6 h-6 text-purple-600"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
                  </svg>
                </div>
                <div>
                  <h4 className="font-semibold text-gray-900">
                    Generate Reports
                  </h4>
                  <p className="text-sm text-gray-600">
                    View analytics & reports
                  </p>
                </div>
              </div>
            </Link>
          </div>
        </div>

        {/* Recent Applications */}
        <div className="pmc-card">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900">
              Recent Applications
            </h3>
          </div>
          <div className="overflow-x-auto">
            <table className="pmc-table">
              <thead>
                <tr>
                  <th>Application ID</th>
                  <th>Applicant Name</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {recentApplications.map((app) => (
                  <tr key={app.id}>
                    <td className="font-mono font-medium">#{app.id}</td>
                    <td className="font-medium">{app.name}</td>
                    <td>{app.type}</td>
                    <td>
                      <span className={getStatusBadge(app.status)}>
                        {app.status}
                      </span>
                    </td>
                    <td className="text-gray-600">{app.date}</td>
                    <td>
                      <div className="flex space-x-2">
                        <button className="text-blue-600 hover:text-blue-800 text-sm font-medium">
                          View
                        </button>
                        <button className="text-gray-600 hover:text-gray-800 text-sm font-medium">
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-4 border-t border-gray-200">
            <Link
              to="/applications"
              className="text-blue-600 hover:text-blue-800 text-sm font-medium"
            >
              View all applications →
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
};

export default Dashboard;
