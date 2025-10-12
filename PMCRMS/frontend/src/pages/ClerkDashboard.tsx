import React, { useState, useEffect } from "react";
import {
  clerkWorkflowService,
  type ClerkApplicationDto,
  type ClerkApplicationDetailDto,
  type ClerkStatistics,
} from "../services/clerkWorkflowService";

const ClerkDashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<"pending" | "completed">(
    "pending"
  );
  const [pendingApplications, setPendingApplications] = useState<
    ClerkApplicationDto[]
  >([]);
  const [completedApplications, setCompletedApplications] = useState<
    ClerkApplicationDto[]
  >([]);
  const [statistics, setStatistics] = useState<ClerkStatistics | null>(null);
  const [selectedApplication, setSelectedApplication] =
    useState<ClerkApplicationDetailDto | null>(null);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);
  const [isRejectModalOpen, setIsRejectModalOpen] = useState(false);
  const [approveRemarks, setApproveRemarks] = useState("");
  const [rejectReason, setRejectReason] = useState("");
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [stats, pending, completed] = await Promise.all([
        clerkWorkflowService.getStatistics(),
        clerkWorkflowService.getPendingApplications(),
        clerkWorkflowService.getCompletedApplications(),
      ]);

      setStatistics(stats);
      setPendingApplications(pending);
      setCompletedApplications(completed);
    } catch (err) {
      setError(
        (err as { response?: { data?: { message?: string } } })?.response?.data
          ?.message || "Failed to load applications"
      );
      console.error("Error loading clerk dashboard:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetails = async (id: number) => {
    try {
      setActionLoading(true);
      const details = await clerkWorkflowService.getApplicationDetails(id);
      setSelectedApplication(details);
      setIsDetailsModalOpen(true);
    } catch (err) {
      setError(
        (err as { response?: { data?: { message?: string } } })?.response?.data
          ?.message || "Failed to load application details"
      );
    } finally {
      setActionLoading(false);
    }
  };

  const handleOpenApproveModal = (application: ClerkApplicationDto) => {
    setSelectedApplication(application as ClerkApplicationDetailDto);
    setApproveRemarks("");
    setIsApproveModalOpen(true);
  };

  const handleOpenRejectModal = (application: ClerkApplicationDto) => {
    setSelectedApplication(application as ClerkApplicationDetailDto);
    setRejectReason("");
    setIsRejectModalOpen(true);
  };

  const handleApprove = async () => {
    if (!selectedApplication) return;

    try {
      setActionLoading(true);
      const result = await clerkWorkflowService.approveApplication(
        selectedApplication.id,
        approveRemarks
      );

      if (result.success) {
        setSuccessMessage(result.message);
        setIsApproveModalOpen(false);
        setApproveRemarks("");
        loadData();
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError(
        (err as { response?: { data?: { message?: string } } })?.response?.data
          ?.message || "Failed to approve application"
      );
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    if (!selectedApplication || !rejectReason.trim()) {
      setError("Rejection reason is required");
      return;
    }

    try {
      setActionLoading(true);
      const result = await clerkWorkflowService.rejectApplication(
        selectedApplication.id,
        rejectReason
      );

      if (result.success) {
        setSuccessMessage(result.message);
        setIsRejectModalOpen(false);
        setRejectReason("");
        loadData();
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError(
        (err as { response?: { data?: { message?: string } } })?.response?.data
          ?.message || "Failed to reject application"
      );
    } finally {
      setActionLoading(false);
    }
  };

  const formatDate = (dateString: string | null) => {
    if (!dateString) return "N/A";
    return new Date(dateString).toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  };

  const formatAmount = (amount: number | null) => {
    if (!amount) return "N/A";
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(amount);
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Clerk Dashboard</h1>
          <p className="text-gray-600 mt-2">
            Post-Payment Application Processing
          </p>
        </div>

        {/* Statistics Cards */}
        {statistics && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Pending</p>
                  <p className="text-3xl font-bold text-blue-600">
                    {statistics.pendingCount}
                  </p>
                </div>
                <div className="bg-blue-100 p-3 rounded-full">
                  <svg
                    className="w-6 h-6 text-blue-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">
                    Total Processed
                  </p>
                  <p className="text-3xl font-bold text-green-600">
                    {statistics.totalProcessed}
                  </p>
                </div>
                <div className="bg-green-100 p-3 rounded-full">
                  <svg
                    className="w-6 h-6 text-green-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">This Week</p>
                  <p className="text-3xl font-bold text-purple-600">
                    {statistics.weekProcessed}
                  </p>
                </div>
                <div className="bg-purple-100 p-3 rounded-full">
                  <svg
                    className="w-6 h-6 text-purple-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                    />
                  </svg>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Today</p>
                  <p className="text-3xl font-bold text-orange-600">
                    {statistics.todayProcessed}
                  </p>
                </div>
                <div className="bg-orange-100 p-3 rounded-full">
                  <svg
                    className="w-6 h-6 text-orange-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Success Message */}
        {successMessage && (
          <div className="mb-4 bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded relative">
            <span className="block sm:inline">{successMessage}</span>
            <button
              onClick={() => setSuccessMessage(null)}
              className="absolute top-0 bottom-0 right-0 px-4 py-3"
            >
              <svg
                className="fill-current h-6 w-6 text-green-500"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
              >
                <path d="M14.348 14.849a1.2 1.2 0 0 1-1.697 0L10 11.819l-2.651 3.029a1.2 1.2 0 1 1-1.697-1.697l2.758-3.15-2.759-3.152a1.2 1.2 0 1 1 1.697-1.697L10 8.183l2.651-3.031a1.2 1.2 0 1 1 1.697 1.697l-2.758 3.152 2.758 3.15a1.2 1.2 0 0 1 0 1.698z" />
              </svg>
            </button>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="mb-4 bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative">
            <span className="block sm:inline">{error}</span>
            <button
              onClick={() => setError(null)}
              className="absolute top-0 bottom-0 right-0 px-4 py-3"
            >
              <svg
                className="fill-current h-6 w-6 text-red-500"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
              >
                <path d="M14.348 14.849a1.2 1.2 0 0 1-1.697 0L10 11.819l-2.651 3.029a1.2 1.2 0 1 1-1.697-1.697l2.758-3.15-2.759-3.152a1.2 1.2 0 1 1 1.697-1.697L10 8.183l2.651-3.031a1.2 1.2 0 1 1 1.697 1.697l-2.758 3.152 2.758 3.15a1.2 1.2 0 0 1 0 1.698z" />
              </svg>
            </button>
          </div>
        )}

        {/* Tabs */}
        <div className="bg-white rounded-lg shadow mb-6">
          <div className="border-b border-gray-200">
            <nav className="flex -mb-px">
              <button
                onClick={() => setActiveTab("pending")}
                className={`${
                  activeTab === "pending"
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                } flex-1 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
              >
                Pending Applications ({pendingApplications.length})
              </button>
              <button
                onClick={() => setActiveTab("completed")}
                className={`${
                  activeTab === "completed"
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                } flex-1 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
              >
                Completed Applications ({completedApplications.length})
              </button>
            </nav>
          </div>

          {/* Content */}
          <div className="p-6">
            {loading ? (
              <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Application
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Applicant
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Payment
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Payment Date
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {(activeTab === "pending"
                      ? pendingApplications
                      : completedApplications
                    ).map((app) => (
                      <tr key={app.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm font-medium text-gray-900">
                            {app.applicationNumber}
                          </div>
                          <div className="text-sm text-gray-500">
                            {app.applicationType}
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="text-sm font-medium text-gray-900">
                            {app.applicantName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {app.applicantEmail}
                          </div>
                          <div className="text-sm text-gray-500">
                            {app.applicantMobile}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center">
                            <span
                              className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                                app.isPaymentComplete
                                  ? "bg-green-100 text-green-800"
                                  : "bg-red-100 text-red-800"
                              }`}
                            >
                              {app.isPaymentComplete ? "Paid" : "Pending"}
                            </span>
                            <span className="ml-2 text-sm text-gray-900">
                              {formatAmount(app.paymentAmount)}
                            </span>
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {formatDate(app.paymentCompletedDate)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <div className="flex justify-end gap-2">
                            <button
                              onClick={() => handleViewDetails(app.id)}
                              className="text-blue-600 hover:text-blue-900"
                            >
                              View
                            </button>
                            {activeTab === "pending" && (
                              <>
                                <button
                                  onClick={() => handleOpenApproveModal(app)}
                                  className="text-green-600 hover:text-green-900"
                                >
                                  Approve
                                </button>
                                <button
                                  onClick={() => handleOpenRejectModal(app)}
                                  className="text-red-600 hover:text-red-900"
                                >
                                  Reject
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                    {(activeTab === "pending"
                      ? pendingApplications
                      : completedApplications
                    ).length === 0 && (
                      <tr>
                        <td
                          colSpan={5}
                          className="px-6 py-12 text-center text-gray-500"
                        >
                          No {activeTab} applications found
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Details Modal */}
      {isDetailsModalOpen && selectedApplication && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-3/4 lg:w-1/2 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-bold text-gray-900">
                Application Details
              </h3>
              <button
                onClick={() => setIsDetailsModalOpen(false)}
                className="text-gray-400 hover:text-gray-500"
              >
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Application Number
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.applicationNumber}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Type
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.applicationType}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Applicant Name
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.applicantName}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Email
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.applicantEmail}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Mobile
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.applicantMobile}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Current Status
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.currentStatus}
                  </p>
                </div>
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700">
                    Property Address
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.propertyAddress}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Payment Amount
                  </label>
                  <p className="mt-1 text-sm text-gray-900 font-semibold">
                    {formatAmount(selectedApplication.paymentAmount)}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Payment Date
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {formatDate(selectedApplication.paymentCompletedDate)}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Transaction ID
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.transactionId || "N/A"}
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    BillDesk Order ID
                  </label>
                  <p className="mt-1 text-sm text-gray-900">
                    {selectedApplication.bdOrderId || "N/A"}
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-6 flex justify-end">
              <button
                onClick={() => setIsDetailsModalOpen(false)}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Approve Modal */}
      {isApproveModalOpen && selectedApplication && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-1/2 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-bold text-gray-900">
                Approve Application
              </h3>
              <button
                onClick={() => setIsApproveModalOpen(false)}
                className="text-gray-400 hover:text-gray-500"
              >
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                Application:{" "}
                <span className="font-semibold">
                  {selectedApplication.applicationNumber}
                </span>
              </p>
              <p className="text-sm text-gray-600">
                Applicant:{" "}
                <span className="font-semibold">
                  {selectedApplication.applicantName}
                </span>
              </p>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Remarks (Optional)
                </label>
                <textarea
                  value={approveRemarks}
                  onChange={(e) => setApproveRemarks(e.target.value)}
                  rows={4}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                  placeholder="Add any remarks for approval..."
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setIsApproveModalOpen(false)}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400"
                disabled={actionLoading}
              >
                Cancel
              </button>
              <button
                onClick={handleApprove}
                disabled={actionLoading}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {actionLoading ? "Processing..." : "Approve & Forward to EE"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Reject Modal */}
      {isRejectModalOpen && selectedApplication && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-1/2 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-bold text-gray-900">
                Reject Application
              </h3>
              <button
                onClick={() => setIsRejectModalOpen(false)}
                className="text-gray-400 hover:text-gray-500"
              >
                <svg
                  className="h-6 w-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            <div className="space-y-4">
              <p className="text-sm text-gray-600">
                Application:{" "}
                <span className="font-semibold">
                  {selectedApplication.applicationNumber}
                </span>
              </p>
              <p className="text-sm text-gray-600">
                Applicant:{" "}
                <span className="font-semibold">
                  {selectedApplication.applicantName}
                </span>
              </p>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Rejection Reason <span className="text-red-500">*</span>
                </label>
                <textarea
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  rows={4}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500"
                  placeholder="Provide detailed reason for rejection..."
                  required
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setIsRejectModalOpen(false)}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400"
                disabled={actionLoading}
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                disabled={actionLoading || !rejectReason.trim()}
                className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {actionLoading ? "Processing..." : "Reject Application"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ClerkDashboard;
