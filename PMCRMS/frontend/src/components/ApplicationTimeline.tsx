import React from "react";
import {
  CheckCircle2,
  XCircle,
  Clock,
  Circle,
  AlertCircle,
} from "lucide-react";

export interface TimelineEvent {
  id: number;
  status: string;
  statusDate: string;
  remarks?: string;
  actorName?: string;
  actorRole?: string;
  isApproval?: boolean;
  isRejection?: boolean;
}

interface ApplicationTimelineProps {
  events: TimelineEvent[];
}

const ApplicationTimeline: React.FC<ApplicationTimelineProps> = ({
  events,
}) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-IN", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getStatusIcon = (event: TimelineEvent, index: number) => {
    if (event.isRejection) {
      return (
        <div className="flex-shrink-0 w-10 h-10 rounded-full bg-red-100 flex items-center justify-center">
          <XCircle className="h-5 w-5 text-red-600" />
        </div>
      );
    }

    if (event.isApproval) {
      return (
        <div className="flex-shrink-0 w-10 h-10 rounded-full bg-green-100 flex items-center justify-center">
          <CheckCircle2 className="h-5 w-5 text-green-600" />
        </div>
      );
    }

    if (index === 0) {
      // Current/latest status
      return (
        <div className="flex-shrink-0 w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center">
          <Clock className="h-5 w-5 text-blue-600 animate-pulse" />
        </div>
      );
    }

    return (
      <div className="flex-shrink-0 w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center">
        <Circle className="h-5 w-5 text-gray-400" />
      </div>
    );
  };

  const getStatusColor = (event: TimelineEvent, index: number) => {
    if (event.isRejection) return "text-red-700 bg-red-50 border-red-200";
    if (event.isApproval) return "text-green-700 bg-green-50 border-green-200";
    if (index === 0) return "text-blue-700 bg-blue-50 border-blue-200";
    return "text-gray-700 bg-gray-50 border-gray-200";
  };

  if (!events || events.length === 0) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-8 text-center">
        <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-3" />
        <p className="text-sm text-gray-600">No status history available</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-6">
        Application Timeline
      </h3>

      <div className="space-y-6">
        {events.map((event, index) => (
          <div key={event.id} className="relative">
            {/* Connector Line */}
            {index < events.length - 1 && (
              <div className="absolute left-5 top-10 bottom-0 w-0.5 bg-gray-200"></div>
            )}

            <div className="flex gap-4">
              {/* Icon */}
              {getStatusIcon(event, index)}

              {/* Content */}
              <div className="flex-1 pb-6">
                <div
                  className={`rounded-lg border p-4 ${getStatusColor(
                    event,
                    index
                  )}`}
                >
                  {/* Status and Date */}
                  <div className="flex items-start justify-between gap-4 mb-2">
                    <h4 className="font-semibold text-base">{event.status}</h4>
                    {index === 0 && (
                      <span className="flex-shrink-0 px-2 py-1 text-xs font-medium bg-blue-200 text-blue-800 rounded">
                        Current
                      </span>
                    )}
                  </div>

                  <p className="text-sm text-gray-600 mb-2">
                    {formatDate(event.statusDate)}
                  </p>

                  {/* Actor Info */}
                  {event.actorName && (
                    <div className="flex items-center gap-2 mt-2 text-sm">
                      <span className="font-medium">{event.actorName}</span>
                      {event.actorRole && (
                        <>
                          <span className="text-gray-400">•</span>
                          <span className="text-gray-600">
                            {event.actorRole}
                          </span>
                        </>
                      )}
                    </div>
                  )}

                  {/* Remarks */}
                  {event.remarks && (
                    <div className="mt-3 pt-3 border-t border-current border-opacity-20">
                      <p className="text-sm italic">
                        <span className="font-medium not-italic">Remarks:</span>{" "}
                        {event.remarks}
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ApplicationTimeline;
