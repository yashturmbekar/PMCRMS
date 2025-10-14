import React, { useState, useEffect, useCallback } from "react";
import {
  X,
  CreditCard,
  Clock,
  CheckCircle,
  XCircle,
  AlertCircle,
  History,
  Eye,
  Calendar,
  User,
  Mail,
  Phone,
  Monitor,
  MapPin,
} from "lucide-react";
import billdeskPaymentService from "../services/billdeskPaymentService";
import type {
  Transaction,
  PaymentStatusResponse,
  TransactionDetailsResponse,
} from "../services/billdeskPaymentService";

interface PaymentStatusModalProps {
  applicationId: number;
  isOpen: boolean;
  onClose: () => void;
}

const PaymentStatusModal: React.FC<PaymentStatusModalProps> = ({
  applicationId,
  isOpen,
  onClose,
}) => {
  const [loading, setLoading] = useState(true);
  const [paymentStatus, setPaymentStatus] =
    useState<PaymentStatusResponse | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"status" | "history">("status");
  const [selectedTransaction, setSelectedTransaction] = useState<
    TransactionDetailsResponse["data"] | null
  >(null);
  const [loadingDetails, setLoadingDetails] = useState(false);

  const fetchPaymentData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      // Fetch both status and history
      const [statusRes, historyRes] = await Promise.all([
        billdeskPaymentService.getPaymentStatus(applicationId),
        billdeskPaymentService.getPaymentHistory(applicationId),
      ]);

      if (statusRes.success) {
        setPaymentStatus(statusRes);
      }

      if (historyRes.success && historyRes.data) {
        setTransactions(historyRes.data);
      }
    } catch (err) {
      const error = err as Error;
      setError(error.message);
    } finally {
      setLoading(false);
    }
  }, [applicationId]);

  const fetchTransactionDetails = async (transactionId: string) => {
    console.log("=== fetchTransactionDetails START ===");
    console.log("fetchTransactionDetails called with ID:", transactionId);
    console.log(
      "Current selectedTransaction before fetch:",
      selectedTransaction
    );
    setLoadingDetails(true);
    try {
      const response = await billdeskPaymentService.getTransactionDetails(
        transactionId
      );
      console.log("Transaction details response:", response);
      console.log("Response.success:", response.success);
      console.log("Response.data:", response.data);
      if (response.success && response.data) {
        console.log("About to set selectedTransaction with:", response.data);
        setSelectedTransaction(response.data);
        console.log("setSelectedTransaction called");
      } else {
        console.log("Response not successful or no data");
      }
    } catch (err) {
      const error = err as Error;
      console.error("Error fetching transaction details:", error);
      setError(error.message);
    } finally {
      setLoadingDetails(false);
      console.log("=== fetchTransactionDetails END ===");
    }
  };

  const closeDetailView = () => {
    console.log("closeDetailView called - setting selectedTransaction to null");
    setSelectedTransaction(null);
  };

  useEffect(() => {
    if (isOpen) {
      fetchPaymentData();
    }
  }, [isOpen, fetchPaymentData]);

  useEffect(() => {
    console.log("selectedTransaction state changed:", selectedTransaction);
  }, [selectedTransaction]);

  useEffect(() => {
    console.log("activeTab changed to:", activeTab);
    console.log("transactions available:", transactions.length);
  }, [activeTab, transactions]);

  const getStatusIcon = (status: string) => {
    switch (status?.toUpperCase()) {
      case "SUCCESS":
        return <CheckCircle className="w-6 h-6 text-green-600" />;
      case "PENDING":
        return <Clock className="w-6 h-6 text-yellow-600" />;
      case "FAILED":
        return <XCircle className="w-6 h-6 text-red-600" />;
      default:
        return <AlertCircle className="w-6 h-6 text-gray-600" />;
    }
  };

  if (!isOpen) return null;

  return (
    <>
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
        <div className="bg-white rounded-xl shadow-2xl max-w-3xl w-full max-h-[90vh] overflow-hidden flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-200">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                <CreditCard className="w-6 h-6 text-blue-600" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-gray-900">
                  Payment Details
                </h2>
                <p className="text-sm text-gray-500">
                  Application #{applicationId}
                </p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <X className="w-5 h-5 text-gray-500" />
            </button>
          </div>

          {/* Tabs */}
          <div className="flex border-b border-gray-200">
            <button
              onClick={() => {
                console.log("Switching to status tab");
                setActiveTab("status");
              }}
              className={`flex-1 py-3 px-4 text-sm font-medium transition-colors ${
                activeTab === "status"
                  ? "text-blue-600 border-b-2 border-blue-600"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              Payment Status
            </button>
            <button
              onClick={() => {
                console.log("Switching to history tab");
                setActiveTab("history");
              }}
              className={`flex-1 py-3 px-4 text-sm font-medium transition-colors ${
                activeTab === "history"
                  ? "text-blue-600 border-b-2 border-blue-600"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              Transaction History ({transactions.length})
            </button>
          </div>

          {/* Content */}
          <div className="flex-1 overflow-y-auto p-6">
            {loading ? (
              <div className="flex items-center justify-center py-12">
                <div className="text-center">
                  <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
                  <p className="text-gray-600">Loading payment details...</p>
                </div>
              </div>
            ) : error ? (
              <div className="flex items-start p-4 bg-red-50 border border-red-200 rounded-lg">
                <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 mr-3 flex-shrink-0" />
                <div>
                  <p className="text-sm font-medium text-red-800">Error</p>
                  <p className="text-sm text-red-600 mt-1">{error}</p>
                </div>
              </div>
            ) : (
              <>
                {/* Status Tab */}
                {activeTab === "status" && paymentStatus && (
                  <div className="space-y-6">
                    {/* Payment Status Card */}
                    <div className="bg-gradient-to-br from-blue-50 to-purple-50 rounded-xl p-6 border border-blue-100">
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex items-center space-x-3">
                          {getStatusIcon(
                            paymentStatus.paymentStatus || "PENDING"
                          )}
                          <div>
                            <p className="text-sm text-gray-600 font-medium">
                              Payment Status
                            </p>
                            <p
                              className={`text-lg font-bold ${
                                paymentStatus.isPaymentComplete
                                  ? "text-green-600"
                                  : "text-yellow-600"
                              }`}
                            >
                              {paymentStatus.isPaymentComplete
                                ? "Completed"
                                : "Pending"}
                            </p>
                          </div>
                        </div>
                        {paymentStatus.isPaymentComplete && (
                          <span className="px-3 py-1 bg-green-100 text-green-800 text-xs font-semibold rounded-full">
                            ✓ Verified
                          </span>
                        )}
                      </div>

                      {/* Payment Amount */}
                      {paymentStatus.amountPaid && (
                        <div className="mt-4 pt-4 border-t border-blue-200">
                          <div className="flex items-baseline justify-between">
                            <span className="text-sm text-gray-600">
                              Amount Paid
                            </span>
                            <span className="text-2xl font-bold text-gray-900">
                              {billdeskPaymentService.formatCurrency(
                                paymentStatus.amountPaid
                              )}
                            </span>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Transaction Details */}
                    {(paymentStatus.transactionId ||
                      paymentStatus.bdOrderId) && (
                      <div className="bg-white border border-gray-200 rounded-xl p-6">
                        <h3 className="font-semibold text-gray-900 mb-4">
                          Transaction Details
                        </h3>
                        <div className="space-y-3">
                          {paymentStatus.transactionId && (
                            <div className="flex justify-between items-start">
                              <span className="text-sm text-gray-600">
                                Transaction ID
                              </span>
                              <span className="text-sm font-mono text-gray-900 text-right">
                                {paymentStatus.transactionId}
                              </span>
                            </div>
                          )}
                          {paymentStatus.bdOrderId && (
                            <div className="flex justify-between items-start">
                              <span className="text-sm text-gray-600">
                                BillDesk Order ID
                              </span>
                              <span className="text-sm font-mono text-gray-900 text-right">
                                {paymentStatus.bdOrderId}
                              </span>
                            </div>
                          )}
                          {paymentStatus.paymentDate && (
                            <div className="flex justify-between items-start">
                              <span className="text-sm text-gray-600">
                                Payment Date
                              </span>
                              <span className="text-sm text-gray-900">
                                {billdeskPaymentService.formatDate(
                                  paymentStatus.paymentDate
                                )}
                              </span>
                            </div>
                          )}
                        </div>
                      </div>
                    )}

                    {/* Help Section */}
                    <div className="bg-blue-50 border border-blue-200 rounded-xl p-4">
                      <div className="flex items-start">
                        <AlertCircle className="w-5 h-5 text-blue-600 mt-0.5 mr-3 flex-shrink-0" />
                        <div className="text-sm text-blue-800">
                          <p className="font-medium mb-1">
                            Need help with your payment?
                          </p>
                          <p>
                            Contact support at support@pmcrms.in with your
                            transaction ID.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* History Tab */}
                {activeTab === "history" && (
                  <div className="space-y-4">
                    {transactions.length === 0 ? (
                      <div className="text-center py-12">
                        <History className="w-12 h-12 text-gray-400 mx-auto mb-3" />
                        <p className="text-gray-600">
                          No payment history found
                        </p>
                      </div>
                    ) : (
                      transactions.map((transaction, index) => (
                        <div
                          key={transaction.id}
                          className="bg-white border border-gray-200 rounded-lg p-4 hover:border-blue-300 hover:shadow-md transition-all cursor-pointer"
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            console.log(
                              "=== TRANSACTION CARD CLICKED ===",
                              transaction.id,
                              "Index:",
                              index,
                              "Timestamp:",
                              new Date().toISOString()
                            );
                            fetchTransactionDetails(transaction.id);
                          }}
                        >
                          <div className="flex items-start justify-between mb-3">
                            <div className="flex items-center space-x-3">
                              {getStatusIcon(transaction.status)}
                              <div>
                                <p className="font-semibold text-gray-900">
                                  Transaction #{index + 1}
                                </p>
                                <p className="text-xs text-gray-500">
                                  {billdeskPaymentService.formatDate(
                                    transaction.createdAt
                                  )}
                                </p>
                              </div>
                            </div>
                            <div className="flex items-center gap-2">
                              <span
                                className={`px-3 py-1 text-xs font-semibold rounded-full ${billdeskPaymentService.getStatusColor(
                                  transaction.status
                                )}`}
                              >
                                {transaction.status}
                              </span>
                              <Eye className="w-4 h-4 text-blue-600" />
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-3 text-sm">
                            <div>
                              <p className="text-gray-600">Transaction ID</p>
                              <p className="font-mono text-gray-900">
                                {transaction.transactionId}
                              </p>
                            </div>
                            <div>
                              <p className="text-gray-600">Amount</p>
                              <p className="font-semibold text-gray-900">
                                {billdeskPaymentService.formatCurrency(
                                  transaction.price
                                )}
                              </p>
                            </div>
                            {transaction.bdOrderId && (
                              <div className="col-span-2">
                                <p className="text-gray-600">
                                  BillDesk Order ID
                                </p>
                                <p className="font-mono text-xs text-gray-900">
                                  {transaction.bdOrderId}
                                </p>
                              </div>
                            )}
                            {transaction.mode && (
                              <div>
                                <p className="text-gray-600">Payment Mode</p>
                                <p className="text-gray-900">
                                  {transaction.mode}
                                </p>
                              </div>
                            )}
                            {transaction.errorMessage && (
                              <div className="col-span-2">
                                <p className="text-gray-600">Error</p>
                                <p className="text-red-600 text-xs">
                                  {transaction.errorMessage}
                                </p>
                              </div>
                            )}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </>
            )}
          </div>

          {/* Footer */}
          <div className="border-t border-gray-200 p-4 bg-gray-50">
            <button
              onClick={onClose}
              className="w-full px-4 py-2 bg-gray-600 hover:bg-gray-700 text-white font-medium rounded-lg transition-colors"
            >
              Close
            </button>
          </div>
        </div>
      </div>

      {/* Transaction Details Modal */}
      {selectedTransaction && (
        <div
          className="fixed inset-0 z-[70] flex items-center justify-center bg-black bg-opacity-70 p-4"
          onClick={closeDetailView}
        >
          <div
            className="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-hidden flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="flex items-center justify-between p-6 border-b border-gray-200 bg-gradient-to-r from-blue-500 to-purple-600">
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 bg-white rounded-lg flex items-center justify-center">
                  <CreditCard className="w-6 h-6 text-blue-600" />
                </div>
                <div>
                  <h2 className="text-xl font-bold text-white">
                    Transaction Details
                  </h2>
                  <p className="text-sm text-blue-100">
                    Complete transaction information
                  </p>
                </div>
              </div>
              <button
                onClick={closeDetailView}
                className="p-2 hover:bg-white/20 rounded-lg transition-colors"
              >
                <X className="w-5 h-5 text-white" />
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6">
              {loadingDetails ? (
                <div className="flex items-center justify-center py-12">
                  <div className="text-center">
                    <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
                    <p className="text-gray-600">Loading details...</p>
                  </div>
                </div>
              ) : (
                <div className="space-y-6">
                  {/* Status Badge */}
                  <div className="flex items-center justify-center">
                    <div className="flex items-center space-x-3">
                      {getStatusIcon(selectedTransaction.status)}
                      <span
                        className={`px-4 py-2 text-sm font-bold rounded-full ${billdeskPaymentService.getStatusColor(
                          selectedTransaction.status
                        )}`}
                      >
                        {selectedTransaction.status}
                      </span>
                    </div>
                  </div>

                  {/* Transaction Amount */}
                  <div className="bg-gradient-to-br from-blue-50 to-purple-50 rounded-xl p-6 text-center border border-blue-100">
                    <p className="text-sm text-gray-600 mb-2">
                      Transaction Amount
                    </p>
                    <p className="text-3xl font-bold text-gray-900">
                      {billdeskPaymentService.formatCurrency(
                        selectedTransaction.amountPaid ||
                          selectedTransaction.price
                      )}
                    </p>
                  </div>

                  {/* Transaction IDs */}
                  <div className="bg-white border border-gray-200 rounded-xl p-6">
                    <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
                      <CreditCard className="w-5 h-5 text-blue-600" />
                      Transaction Identifiers
                    </h3>
                    <div className="space-y-3">
                      <div>
                        <p className="text-xs text-gray-600 mb-1">
                          Transaction ID
                        </p>
                        <p className="font-mono text-sm text-gray-900 bg-gray-50 p-2 rounded">
                          {selectedTransaction.transactionId}
                        </p>
                      </div>
                      {selectedTransaction.bdOrderId && (
                        <div>
                          <p className="text-xs text-gray-600 mb-1">
                            BillDesk Order ID
                          </p>
                          <p className="font-mono text-sm text-gray-900 bg-gray-50 p-2 rounded">
                            {selectedTransaction.bdOrderId}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Customer Information */}
                  {(selectedTransaction.firstName ||
                    selectedTransaction.email ||
                    selectedTransaction.phoneNumber) && (
                    <div className="bg-white border border-gray-200 rounded-xl p-6">
                      <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
                        <User className="w-5 h-5 text-blue-600" />
                        Customer Information
                      </h3>
                      <div className="space-y-3">
                        {selectedTransaction.firstName && (
                          <div className="flex items-center gap-3">
                            <User className="w-4 h-4 text-gray-400" />
                            <div>
                              <p className="text-xs text-gray-600">Name</p>
                              <p className="text-sm text-gray-900">
                                {selectedTransaction.firstName}{" "}
                                {selectedTransaction.lastName}
                              </p>
                            </div>
                          </div>
                        )}
                        {selectedTransaction.email && (
                          <div className="flex items-center gap-3">
                            <Mail className="w-4 h-4 text-gray-400" />
                            <div>
                              <p className="text-xs text-gray-600">Email</p>
                              <p className="text-sm text-gray-900">
                                {selectedTransaction.email}
                              </p>
                            </div>
                          </div>
                        )}
                        {selectedTransaction.phoneNumber && (
                          <div className="flex items-center gap-3">
                            <Phone className="w-4 h-4 text-gray-400" />
                            <div>
                              <p className="text-xs text-gray-600">Phone</p>
                              <p className="text-sm text-gray-900">
                                {selectedTransaction.phoneNumber}
                              </p>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {/* Payment Details */}
                  {(selectedTransaction.mode ||
                    selectedTransaction.cardType) && (
                    <div className="bg-white border border-gray-200 rounded-xl p-6">
                      <h3 className="font-semibold text-gray-900 mb-4">
                        Payment Method
                      </h3>
                      <div className="grid grid-cols-2 gap-4">
                        {selectedTransaction.mode && (
                          <div>
                            <p className="text-xs text-gray-600 mb-1">
                              Payment Mode
                            </p>
                            <p className="text-sm text-gray-900">
                              {selectedTransaction.mode}
                            </p>
                          </div>
                        )}
                        {selectedTransaction.cardType && (
                          <div>
                            <p className="text-xs text-gray-600 mb-1">
                              Card Type
                            </p>
                            <p className="text-sm text-gray-900">
                              {selectedTransaction.cardType}
                            </p>
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {/* Timestamps */}
                  <div className="bg-white border border-gray-200 rounded-xl p-6">
                    <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
                      <Calendar className="w-5 h-5 text-blue-600" />
                      Timeline
                    </h3>
                    <div className="space-y-3">
                      <div className="flex items-center gap-3">
                        <Clock className="w-4 h-4 text-gray-400" />
                        <div>
                          <p className="text-xs text-gray-600">Created</p>
                          <p className="text-sm text-gray-900">
                            {billdeskPaymentService.formatDate(
                              selectedTransaction.createdAt
                            )}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3">
                        <Clock className="w-4 h-4 text-gray-400" />
                        <div>
                          <p className="text-xs text-gray-600">Last Updated</p>
                          <p className="text-sm text-gray-900">
                            {billdeskPaymentService.formatDate(
                              selectedTransaction.updatedAt
                            )}
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Technical Details */}
                  {(selectedTransaction.clientIpAddress ||
                    selectedTransaction.userAgent) && (
                    <div className="bg-white border border-gray-200 rounded-xl p-6">
                      <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
                        <Monitor className="w-5 h-5 text-blue-600" />
                        Technical Information
                      </h3>
                      <div className="space-y-3">
                        {selectedTransaction.clientIpAddress && (
                          <div className="flex items-start gap-3">
                            <MapPin className="w-4 h-4 text-gray-400 mt-0.5" />
                            <div>
                              <p className="text-xs text-gray-600">
                                IP Address
                              </p>
                              <p className="text-sm font-mono text-gray-900">
                                {selectedTransaction.clientIpAddress}
                              </p>
                            </div>
                          </div>
                        )}
                        {selectedTransaction.userAgent && (
                          <div className="flex items-start gap-3">
                            <Monitor className="w-4 h-4 text-gray-400 mt-0.5" />
                            <div>
                              <p className="text-xs text-gray-600">
                                User Agent
                              </p>
                              <p className="text-xs text-gray-900 break-all">
                                {selectedTransaction.userAgent}
                              </p>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {/* Error Message */}
                  {selectedTransaction.errorMessage && (
                    <div className="bg-red-50 border border-red-200 rounded-xl p-4">
                      <div className="flex items-start gap-3">
                        <XCircle className="w-5 h-5 text-red-600 mt-0.5" />
                        <div>
                          <p className="text-sm font-medium text-red-800 mb-1">
                            Error Details
                          </p>
                          <p className="text-sm text-red-700">
                            {selectedTransaction.errorMessage}
                          </p>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Application Details */}
                  {selectedTransaction.applicationDetails && (
                    <div className="bg-white border border-gray-200 rounded-xl p-6">
                      <h3 className="font-semibold text-gray-900 mb-4">
                        Application Details
                      </h3>
                      <div className="space-y-2">
                        <div className="flex justify-between">
                          <span className="text-sm text-gray-600">
                            Applicant
                          </span>
                          <span className="text-sm text-gray-900 font-medium">
                            {
                              selectedTransaction.applicationDetails
                                .applicantName
                            }
                          </span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-sm text-gray-600">
                            Position
                          </span>
                          <span className="text-sm text-gray-900 font-medium">
                            {
                              selectedTransaction.applicationDetails
                                .positionType
                            }
                          </span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-sm text-gray-600">Status</span>
                          <span className="text-sm text-gray-900 font-medium">
                            {selectedTransaction.applicationDetails.status}
                          </span>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="border-t border-gray-200 p-4 bg-gray-50">
              <button
                onClick={closeDetailView}
                className="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors"
              >
                Close Details
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default PaymentStatusModal;
