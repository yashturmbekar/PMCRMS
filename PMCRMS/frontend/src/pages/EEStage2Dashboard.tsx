/**
 * EE Stage 2 Dashboard
 * Executive Engineer Stage 2 workflow for applying digital signature to certificates
 * After clerk processing (status 18), EE applies digital signature via HSM
 */

import React, { useState, useEffect } from "react";
import eeStage2WorkflowService, {
  type EEStage2ApplicationDto,
  type EEStage2ApplicationDetailDto,
  type EEStage2Statistics,
} from "../services/eeStage2WorkflowService";

const EEStage2Dashboard: React.FC = () => {
  // State management
  const [activeTab, setActiveTab] = useState<"pending" | "completed">(
    "pending"
  );
  const [pendingApplications, setPendingApplications] = useState<
    EEStage2ApplicationDto[]
  >([]);
  const [completedApplications, setCompletedApplications] = useState<
    EEStage2ApplicationDto[]
  >([]);
  const [statistics, setStatistics] = useState<EEStage2Statistics>({
    pendingCount: 0,
    completedCount: 0,
    todayProcessed: 0,
    weekProcessed: 0,
    monthProcessed: 0,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modal states
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [showSignatureModal, setShowSignatureModal] = useState(false);
  const [selectedApplication, setSelectedApplication] =
    useState<EEStage2ApplicationDetailDto | null>(null);
  const [otpCode, setOtpCode] = useState("");
  const [otpReference, setOtpReference] = useState<string | null>(null);
  const [isGeneratingOtp, setIsGeneratingOtp] = useState(false);
  const [isApplyingSignature, setIsApplyingSignature] = useState(false);

  /**
   * Load statistics
   */
  const loadStatistics = async () => {
    try {
      const stats = await eeStage2WorkflowService.getStatistics();
      setStatistics(stats);
    } catch (err) {
      console.error("Error loading statistics:", err);
    }
  };

  /**
   * Load applications based on active tab
   */
  const loadApplications = React.useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (activeTab === "pending") {
        const apps = await eeStage2WorkflowService.getPendingApplications();
        setPendingApplications(apps);
      } else {
        const apps = await eeStage2WorkflowService.getCompletedApplications();
        setCompletedApplications(apps);
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load applications"
      );
    } finally {
      setLoading(false);
    }
  }, [activeTab]);

  // Load data on mount and when active tab changes
  useEffect(() => {
    loadStatistics();
    loadApplications();
  }, [activeTab, loadApplications]);

  // Load data on mount and when active tab changes
  useEffect(() => {
    loadStatistics();
    loadApplications();
  }, [activeTab, loadApplications]);

  // Auto-dismiss messages
  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => setSuccessMessage(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [successMessage]);

  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => setError(null), 7000);
      return () => clearTimeout(timer);
    }
  }, [error]);
  const handleViewDetails = async (applicationId: number) => {
    try {
      setLoading(true);
      const details = await eeStage2WorkflowService.getApplicationDetails(
        applicationId
      );
      setSelectedApplication(details);
      setShowDetailsModal(true);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load application details"
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Generate OTP for digital signature
   */
  const handleGenerateOtp = async (applicationId: number) => {
    try {
      setIsGeneratingOtp(true);
      setError(null);
      const result = await eeStage2WorkflowService.generateOtp(applicationId);
      if (result.success) {
        setOtpReference(result.otpReference || null);
        setSuccessMessage(result.message);
        // Open signature modal
        const details = await eeStage2WorkflowService.getApplicationDetails(
          applicationId
        );
        setSelectedApplication(details);
        setShowSignatureModal(true);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate OTP");
    } finally {
      setIsGeneratingOtp(false);
    }
  };

  /**
   * Apply digital signature with OTP
   */
  const handleApplySignature = async () => {
    if (!selectedApplication) return;
    if (!otpCode.trim()) {
      setError("Please enter the OTP code");
      return;
    }

    try {
      setIsApplyingSignature(true);
      setError(null);
      const result = await eeStage2WorkflowService.applyDigitalSignature(
        selectedApplication.applicationId,
        otpCode
      );
      if (result.success) {
        setSuccessMessage(result.message);
        setShowSignatureModal(false);
        setOtpCode("");
        setOtpReference(null);
        setSelectedApplication(null);
        // Reload data
        loadStatistics();
        loadApplications();
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to apply digital signature"
      );
    } finally {
      setIsApplyingSignature(false);
    }
  };

  /**
   * Format date to readable format
   */
  const formatDate = (dateString?: string): string => {
    if (!dateString) return "N/A";
    const date = new Date(dateString);
    return date.toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  };

  /**
   * Format currency
   */
  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: "INR",
    }).format(amount);
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">
          Executive Engineer - Certificate Digital Signature
        </h1>
        <p className="text-gray-600 mt-2">
          Apply digital signature to certificates after clerk processing
        </p>
      </div>

      {/* Success Message */}
      {successMessage && (
        <div className="mb-6 bg-green-50 border-l-4 border-green-500 p-4 rounded-md shadow">
          <div className="flex items-center">
            <svg
              className="w-5 h-5 text-green-500 mr-3"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
            <p className="text-green-800 font-medium">{successMessage}</p>
          </div>
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="mb-6 bg-red-50 border-l-4 border-red-500 p-4 rounded-md shadow">
          <div className="flex items-center">
            <svg
              className="w-5 h-5 text-red-500 mr-3"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
            <p className="text-red-800 font-medium">{error}</p>
          </div>
        </div>
      )}

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-6 mb-8">
        <div className="bg-white p-6 rounded-lg shadow-md border-l-4 border-yellow-500">
          <h3 className="text-gray-500 text-sm font-medium">
            Pending Signatures
          </h3>
          <p className="text-3xl font-bold text-yellow-600 mt-2">
            {statistics.pendingCount}
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md border-l-4 border-green-500">
          <h3 className="text-gray-500 text-sm font-medium">Total Processed</h3>
          <p className="text-3xl font-bold text-green-600 mt-2">
            {statistics.completedCount}
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md border-l-4 border-blue-500">
          <h3 className="text-gray-500 text-sm font-medium">This Week</h3>
          <p className="text-3xl font-bold text-blue-600 mt-2">
            {statistics.weekProcessed}
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md border-l-4 border-purple-500">
          <h3 className="text-gray-500 text-sm font-medium">Today</h3>
          <p className="text-3xl font-bold text-purple-600 mt-2">
            {statistics.todayProcessed}
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md border-l-4 border-indigo-500">
          <h3 className="text-gray-500 text-sm font-medium">This Month</h3>
          <p className="text-3xl font-bold text-indigo-600 mt-2">
            {statistics.monthProcessed}
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white rounded-lg shadow-md mb-6">
        <div className="border-b border-gray-200">
          <nav className="flex -mb-px">
            <button
              onClick={() => setActiveTab("pending")}
              className={`py-4 px-6 text-sm font-medium border-b-2 transition-colors ${
                activeTab === "pending"
                  ? "border-blue-500 text-blue-600"
                  : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
              }`}
            >
              Pending Signatures ({statistics.pendingCount})
            </button>
            <button
              onClick={() => setActiveTab("completed")}
              className={`py-4 px-6 text-sm font-medium border-b-2 transition-colors ${
                activeTab === "completed"
                  ? "border-blue-500 text-blue-600"
                  : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
              }`}
            >
              Completed Signatures ({statistics.completedCount})
            </button>
          </nav>
        </div>

        {/* Applications Table */}
        <div className="p-6">
          {loading ? (
            <div className="flex justify-center items-center py-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Application No.
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Applicant Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Building Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Plot Area
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Payment Amount
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Clerk Processed
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {activeTab === "pending" &&
                    pendingApplications.length === 0 && (
                      <tr>
                        <td
                          colSpan={7}
                          className="px-6 py-8 text-center text-gray-500"
                        >
                          No pending applications for digital signature
                        </td>
                      </tr>
                    )}
                  {activeTab === "completed" &&
                    completedApplications.length === 0 && (
                      <tr>
                        <td
                          colSpan={7}
                          className="px-6 py-8 text-center text-gray-500"
                        >
                          No completed signatures yet
                        </td>
                      </tr>
                    )}
                  {(activeTab === "pending"
                    ? pendingApplications
                    : completedApplications
                  ).map((app) => (
                    <tr key={app.applicationId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-blue-600">
                        {app.applicationNumber}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {app.applicantName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {app.buildingType}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {app.plotArea} sq.m
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-medium">
                        {formatCurrency(app.paymentAmount)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {formatDate(app.clerkProcessedDate)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm space-x-2">
                        <button
                          onClick={() => handleViewDetails(app.applicationId)}
                          className="text-blue-600 hover:text-blue-800 font-medium"
                        >
                          View Details
                        </button>
                        {activeTab === "pending" && (
                          <button
                            onClick={() => handleGenerateOtp(app.applicationId)}
                            disabled={isGeneratingOtp}
                            className="ml-2 bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
                          >
                            {isGeneratingOtp
                              ? "Generating..."
                              : "Apply Signature"}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Details Modal */}
      {showDetailsModal && selectedApplication && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-2xl font-bold text-gray-900">
                Application Details
              </h2>
              <p className="text-gray-600 mt-1">
                {selectedApplication.applicationNumber}
              </p>
            </div>
            <div className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <h3 className="font-semibold text-gray-700">
                    Applicant Name:
                  </h3>
                  <p className="text-gray-900">
                    {selectedApplication.applicantName}
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">Email:</h3>
                  <p className="text-gray-900">
                    {selectedApplication.applicantEmail}
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">Contact:</h3>
                  <p className="text-gray-900">
                    {selectedApplication.applicantContact}
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">
                    Building Type:
                  </h3>
                  <p className="text-gray-900">
                    {selectedApplication.buildingType}
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">Plot Area:</h3>
                  <p className="text-gray-900">
                    {selectedApplication.plotArea} sq.m
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">
                    Building Area:
                  </h3>
                  <p className="text-gray-900">
                    {selectedApplication.buildingArea} sq.m
                  </p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">Floors:</h3>
                  <p className="text-gray-900">{selectedApplication.floors}</p>
                </div>
                <div>
                  <h3 className="font-semibold text-gray-700">Location:</h3>
                  <p className="text-gray-900">
                    {selectedApplication.village}, {selectedApplication.taluka},{" "}
                    {selectedApplication.district}
                  </p>
                </div>
              </div>

              <div className="border-t pt-4">
                <h3 className="font-semibold text-gray-700 mb-2">
                  Payment Details
                </h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h4 className="text-sm text-gray-500">Payment Amount:</h4>
                    <p className="text-gray-900 font-medium">
                      {formatCurrency(selectedApplication.paymentAmount)}
                    </p>
                  </div>
                  <div>
                    <h4 className="text-sm text-gray-500">Payment Date:</h4>
                    <p className="text-gray-900">
                      {formatDate(selectedApplication.paymentDate)}
                    </p>
                  </div>
                  <div>
                    <h4 className="text-sm text-gray-500">
                      Payment Reference:
                    </h4>
                    <p className="text-gray-900">
                      {selectedApplication.paymentReference}
                    </p>
                  </div>
                </div>
              </div>

              <div className="border-t pt-4">
                <h3 className="font-semibold text-gray-700 mb-2">
                  Clerk Processing
                </h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h4 className="text-sm text-gray-500">Processed Date:</h4>
                    <p className="text-gray-900">
                      {formatDate(selectedApplication.clerkProcessedDate)}
                    </p>
                  </div>
                  <div>
                    <h4 className="text-sm text-gray-500">Remarks:</h4>
                    <p className="text-gray-900">
                      {selectedApplication.clerkRemarks || "No remarks"}
                    </p>
                  </div>
                </div>
              </div>

              <div className="border-t pt-4">
                <h3 className="font-semibold text-gray-700 mb-2">
                  Approval History
                </h3>
                <div className="space-y-2">
                  {selectedApplication.jeOfficerName && (
                    <div className="flex justify-between">
                      <span className="text-gray-600">Junior Engineer:</span>
                      <span className="text-gray-900">
                        {selectedApplication.jeOfficerName} (
                        {formatDate(selectedApplication.jeApprovedDate)})
                      </span>
                    </div>
                  )}
                  {selectedApplication.aeOfficerName && (
                    <div className="flex justify-between">
                      <span className="text-gray-600">Assistant Engineer:</span>
                      <span className="text-gray-900">
                        {selectedApplication.aeOfficerName} (
                        {formatDate(selectedApplication.aeApprovedDate)})
                      </span>
                    </div>
                  )}
                  {selectedApplication.ee1OfficerName && (
                    <div className="flex justify-between">
                      <span className="text-gray-600">
                        Executive Engineer (Stage 1):
                      </span>
                      <span className="text-gray-900">
                        {selectedApplication.ee1OfficerName} (
                        {formatDate(selectedApplication.ee1ApprovedDate)})
                      </span>
                    </div>
                  )}
                  {selectedApplication.ce1OfficerName && (
                    <div className="flex justify-between">
                      <span className="text-gray-600">
                        City Engineer (Stage 1):
                      </span>
                      <span className="text-gray-900">
                        {selectedApplication.ce1OfficerName} (
                        {formatDate(selectedApplication.ce1ApprovedDate)})
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="p-6 border-t border-gray-200 flex justify-end">
              <button
                onClick={() => {
                  setShowDetailsModal(false);
                  setSelectedApplication(null);
                }}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400 transition-colors"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Digital Signature Modal */}
      {showSignatureModal && selectedApplication && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-2xl font-bold text-gray-900">
                Apply Digital Signature
              </h2>
              <p className="text-gray-600 mt-1">
                Application: {selectedApplication.applicationNumber}
              </p>
            </div>
            <div className="p-6 space-y-4">
              <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
                <p className="text-sm text-blue-900">
                  <strong>
                    OTP has been sent to your registered mobile/email.
                  </strong>
                  <br />
                  Please enter the OTP to apply your digital signature via HSM.
                </p>
                {otpReference && (
                  <p className="text-xs text-blue-700 mt-2">
                    Reference: {otpReference}
                  </p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Enter OTP <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={otpCode}
                  onChange={(e) => setOtpCode(e.target.value)}
                  placeholder="Enter 6-digit OTP"
                  maxLength={6}
                  className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {error && (
                <div className="bg-red-50 border-l-4 border-red-500 p-3 rounded">
                  <p className="text-sm text-red-800">{error}</p>
                </div>
              )}
            </div>
            <div className="p-6 border-t border-gray-200 flex justify-end space-x-3">
              <button
                onClick={() => {
                  setShowSignatureModal(false);
                  setOtpCode("");
                  setOtpReference(null);
                  setSelectedApplication(null);
                }}
                className="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400 transition-colors"
                disabled={isApplyingSignature}
              >
                Cancel
              </button>
              <button
                onClick={handleApplySignature}
                disabled={isApplyingSignature || !otpCode.trim()}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
              >
                {isApplyingSignature
                  ? "Applying Signature..."
                  : "Apply Signature"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default EEStage2Dashboard;
