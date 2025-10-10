import React from "react";
import { User, Calendar, Clock } from "lucide-react";

interface AssignedOfficerInfoProps {
  assignedOfficerName?: string;
  assignedOfficerDesignation?: string;
  assignedDate?: string;
  currentStatus?: string;
}

const AssignedOfficerInfo: React.FC<AssignedOfficerInfoProps> = ({
  assignedOfficerName,
  assignedOfficerDesignation,
  assignedDate,
  currentStatus,
}) => {
  if (!assignedOfficerName) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-6">
        <div className="flex items-center gap-3 text-gray-500">
          <User className="h-5 w-5" />
          <p className="text-sm font-medium">No officer assigned yet</p>
        </div>
      </div>
    );
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-IN", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div className="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-6">
      <div className="flex items-start gap-4">
        {/* Officer Avatar */}
        <div className="flex-shrink-0">
          <div className="w-12 h-12 rounded-full bg-pmc-primary text-white flex items-center justify-center font-bold text-lg">
            {assignedOfficerName.charAt(0).toUpperCase()}
          </div>
        </div>

        {/* Officer Details */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <User className="h-4 w-4 text-gray-600" />
            <h3 className="text-lg font-semibold text-gray-900">
              Assigned Officer
            </h3>
          </div>

          <p className="text-base font-bold text-pmc-primary">
            {assignedOfficerName}
          </p>

          {assignedOfficerDesignation && (
            <p className="text-sm text-gray-600 mt-1">
              {assignedOfficerDesignation}
            </p>
          )}

          {assignedDate && (
            <div className="flex items-center gap-2 mt-3 text-sm text-gray-600">
              <Calendar className="h-4 w-4" />
              <span>Assigned on {formatDate(assignedDate)}</span>
            </div>
          )}

          {currentStatus && (
            <div className="flex items-center gap-2 mt-2">
              <Clock className="h-4 w-4 text-gray-600" />
              <span className="text-sm text-gray-700">
                Current Status:{" "}
                <span className="font-semibold">{currentStatus}</span>
              </span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default AssignedOfficerInfo;
