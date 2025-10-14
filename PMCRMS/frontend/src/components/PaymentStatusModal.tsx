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
    setLoadingDetails(true);
    try {
      const response = await billdeskPaymentService.getTransactionDetails(
        transactionId
      );
      if (response.success && response.data) {
        setSelectedTransaction(response.data);
      }
    } catch (err) {
      const error = err as Error;
      console.error("Error fetching transaction details:", error);
      setError(error.message);
    } finally {
      setLoadingDetails(false);
    }
  };

  const closeDetailView = () => {
    setSelectedTransaction(null);
  };

  useEffect(() => {
    if (isOpen) {
      fetchPaymentData();
      setSelectedTransaction(null); // Reset detail view when main modal opens
    }
  }, [isOpen, fetchPaymentData]);

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

  // If detail modal is open, render only the detail modal
  if (selectedTransaction) {
    return (
      <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black bg-opacity-70 p-4">
        <div className="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-hidden flex flex-col">
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

                {/* Transaction Information */}
                <div className="bg-gradient-to-r from-blue-50 to-purple-50 rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                    <CreditCard className="w-4 h-4" />
                    Transaction Information
                  </h3>
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">
                        Transaction ID
                      </span>
                      <span className="text-sm text-gray-900 font-mono font-medium">
                        {selectedTransaction.transactionId}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">BD Order ID</span>
                      <span className="text-sm text-gray-900 font-mono">
                        {selectedTransaction.bdOrderId || "N/A"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">Price</span>
                      <span className="text-sm text-gray-900 font-bold">
                        ₹{selectedTransaction.price.toFixed(2)}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">Amount Paid</span>
                      <span className="text-sm text-gray-900 font-bold">
                        ₹{selectedTransaction.amountPaid.toFixed(2)}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">Created At</span>
                      <span className="text-sm text-gray-900">
                        {billdeskPaymentService.formatDate(
                          selectedTransaction.createdAt
                        )}
                      </span>
                    </div>
                    {selectedTransaction.updatedAt && (
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">
                          Updated At
                        </span>
                        <span className="text-sm text-gray-900">
                          {billdeskPaymentService.formatDate(
                            selectedTransaction.updatedAt
                          )}
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Customer Information */}
                <div className="bg-green-50 rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                    <User className="w-4 h-4" />
                    Customer Information
                  </h3>
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span className="text-sm text-gray-600">Name</span>
                      <span className="text-sm text-gray-900 font-medium">
                        {selectedTransaction.firstName}{" "}
                        {selectedTransaction.lastName}
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600 flex items-center gap-1">
                        <Mail className="w-3 h-3" />
                        Email
                      </span>
                      <span className="text-sm text-gray-900">
                        {selectedTransaction.email}
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-gray-600 flex items-center gap-1">
                        <Phone className="w-3 h-3" />
                        Mobile
                      </span>
                      <span className="text-sm text-gray-900 font-mono">
                        {selectedTransaction.phoneNumber}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Payment Method */}
                {selectedTransaction.mode && (
                  <div className="bg-yellow-50 rounded-lg p-4">
                    <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                      <CreditCard className="w-4 h-4" />
                      Payment Method
                    </h3>
                    <div className="space-y-2">
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">Mode</span>
                        <span className="text-sm text-gray-900 font-medium uppercase">
                          {selectedTransaction.mode}
                        </span>
                      </div>
                      {selectedTransaction.cardType && (
                        <div className="flex justify-between">
                          <span className="text-sm text-gray-600">
                            Card Type
                          </span>
                          <span className="text-sm text-gray-900">
                            {selectedTransaction.cardType}
                          </span>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Technical Details */}
                <div className="bg-gray-50 rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                    <Monitor className="w-4 h-4" />
                    Technical Details
                  </h3>
                  <div className="space-y-2">
                    {selectedTransaction.clientIpAddress && (
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">
                          IP Address
                        </span>
                        <span className="text-sm text-gray-900 font-mono">
                          {selectedTransaction.clientIpAddress}
                        </span>
                      </div>
                    )}
                    {selectedTransaction.userAgent && (
                      <div className="flex flex-col gap-1">
                        <span className="text-sm text-gray-600">
                          User Agent
                        </span>
                        <span className="text-xs text-gray-900 font-mono bg-white p-2 rounded border">
                          {selectedTransaction.userAgent}
                        </span>
                      </div>
                    )}
                    {selectedTransaction.errorMessage && (
                      <div className="flex flex-col gap-1">
                        <span className="text-sm text-red-600">
                          Error Message
                        </span>
                        <span className="text-xs text-red-900 bg-red-50 p-2 rounded border border-red-200">
                          {selectedTransaction.errorMessage}
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Application Details */}
                {selectedTransaction.applicationDetails && (
                  <div className="bg-purple-50 rounded-lg p-4">
                    <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                      <MapPin className="w-4 h-4" />
                      Application Details
                    </h3>
                    <div className="space-y-2">
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">
                          Application ID
                        </span>
                        <span className="text-sm text-gray-900 font-medium">
                          #{selectedTransaction.applicationDetails.id}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">
                          Application ID
                        </span>
                        <span className="text-sm text-gray-900 font-medium">
                          #{selectedTransaction.applicationDetails.id}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm text-gray-600">
                          Position Type
                        </span>
                        <span className="text-sm text-gray-900 font-medium">
                          {selectedTransaction.applicationDetails.positionType}
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
              Back to Transactions
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Main modal (payment status and history)
  return (
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
            onClick={() => setActiveTab("status")}
            className={`flex-1 py-3 px-4 text-sm font-medium transition-colors ${
              activeTab === "status"
                ? "text-blue-600 border-b-2 border-blue-600"
                : "text-gray-600 hover:text-gray-900"
            }`}
          >
            Payment Status
          </button>
          <button
            onClick={() => setActiveTab("history")}
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
            <div className="text-center py-12">
              <XCircle className="w-12 h-12 text-red-500 mx-auto mb-3" />
              <p className="text-red-600">{error}</p>
            </div>
          ) : (
            <>
              {/* Status Tab */}
              {activeTab === "status" && paymentStatus && (
                <div className="space-y-6">
                  <div className="bg-gradient-to-r from-blue-50 to-purple-50 rounded-lg p-6">
                    <div className="flex items-center space-x-4 mb-4">
                      {paymentStatus.paymentStatus &&
                        getStatusIcon(paymentStatus.paymentStatus)}
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900">
                          {paymentStatus.paymentStatus || "Unknown"}
                        </h3>
                        <p className="text-sm text-gray-600">
                          Current payment status
                        </p>
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-4 mt-4">
                      <div>
                        <p className="text-xs text-gray-600">Amount</p>
                        <p className="text-lg font-bold text-gray-900">
                          ₹{paymentStatus.amountPaid?.toFixed(2) || "0.00"}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-gray-600">Transaction ID</p>
                        <p className="text-sm font-mono text-gray-900">
                          {paymentStatus.transactionId || "N/A"}
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
                      <p className="text-gray-600">No payment history found</p>
                    </div>
                  ) : (
                    transactions.map((transaction, index) => (
                      <div
                        key={transaction.id}
                        className="bg-white border border-gray-200 rounded-lg p-4 hover:border-blue-400 hover:shadow-lg transition-all cursor-pointer"
                        onClick={() => fetchTransactionDetails(transaction.id)}
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
                            <Eye className="w-5 h-5 text-blue-600" />
                          </div>
                        </div>

                        <div className="grid grid-cols-2 gap-3 text-sm">
                          <div>
                            <p className="text-gray-600">Transaction ID</p>
                            <p className="font-mono text-gray-900 text-xs">
                              {transaction.transactionId}
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-600">Amount</p>
                            <p className="font-bold text-gray-900">
                              ₹{transaction.amountPaid.toFixed(2)}
                            </p>
                          </div>
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
  );
};

export default PaymentStatusModal;
