import React, { useState, useEffect, useContext, useRef } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  FileText,
  CheckCircle,
  XCircle,
  Calendar,
  Ban,
  X,
  Eye,
  CreditCard,
  Info,
  AlertCircle,
} from "lucide-react";
import positionRegistrationService, {
  type PositionRegistrationResponse,
} from "../services/positionRegistrationService";
import { PageLoader, FullScreenLoader } from "../components";
import {
  DocumentApprovalModal,
  OTPVerificationModal,
} from "../components/workflow";
import AuthContext from "../contexts/AuthContext";
import { jeWorkflowService } from "../services/jeWorkflowService";
import { aeWorkflowService } from "../services/aeWorkflowService";
import { eeWorkflowService } from "../services/eeWorkflowService";
import eeStage2WorkflowService from "../services/eeStage2WorkflowService";
import { ceWorkflowService } from "../services/ceWorkflowService";
import ceStage2WorkflowService from "../services/ceStage2WorkflowService";
import { clerkWorkflowService } from "../services/clerkWorkflowService";
import type { PositionType } from "../types/aeWorkflow";
import NotificationModal from "../components/common/NotificationModal";
import type { NotificationType } from "../components/common/NotificationModal";
import PaymentButton from "../components/PaymentButton";
import PaymentStatusModal from "../components/PaymentStatusModal";
import DateTimePicker from "../components/DateTimePicker";
import ModalLoader from "../components/ModalLoader";
import { getApiUrl, getToken } from "../services/apiClient";
import { parseLocalDateTime } from "../utils/dateUtils";

const ViewPositionApplication: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const authContext = useContext(AuthContext);
  const user = authContext?.user;
  const [application, setApplication] =
    useState<PositionRegistrationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showDocumentApprovalModal, setShowDocumentApprovalModal] =
    useState(false);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showRescheduleModal, setShowRescheduleModal] = useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);
  const [scheduleError, setScheduleError] = useState("");
  const [rescheduleError, setRescheduleError] = useState("");
  const [isScheduling, setIsScheduling] = useState(false);
  const [isRescheduling, setIsRescheduling] = useState(false);
  const [scheduleForm, setScheduleForm] = useState({
    comments: "",
    reviewDate: "",
    contactPerson: "",
    place: "",
    roomNumber: "",
  });
  const [rescheduleForm, setRescheduleForm] = useState({
    newReviewDate: "",
    rescheduleReason: "",
    place: "",
    contactPerson: "",
    roomNumber: "",
  });
  const [notification, setNotification] = useState<{
    isOpen: boolean;
    message: string;
    type: NotificationType;
    title: string;
    autoClose?: boolean;
  }>({
    isOpen: false,
    message: "",
    type: "success",
    title: "",
  });
  const [selectedDocument, setSelectedDocument] = useState<{
    fileName: string;
    filePath?: string; // Make filePath optional
    documentTypeName: string;
    id?: number; // Add optional ID for API endpoint
    pdfBase64?: string; // Add base64 for direct display (recommendation form)
    fileBase64?: string; // Add base64 for all documents
  } | null>(null);
  const [pdfBlobUrl, setPdfBlobUrl] = useState<string | null>(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showClerkApprovalModal, setShowClerkApprovalModal] = useState(false);
  const [clerkRemarks, setClerkRemarks] = useState("");
  const [isApprovingClerk, setIsApprovingClerk] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectionComments, setRejectionComments] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);

  // Certificate state
  const [certificateStatus, setCertificateStatus] = useState<{
    exists: boolean;
    certificateId?: number;
    generatedDate?: string;
    fileName?: string;
    fileSize?: number;
  } | null>(null);
  const [loadingCertificate, setLoadingCertificate] = useState(false);

  // Track if certificate was previously unavailable (for notification)
  const certificateWasPendingRef = useRef(false);

  // Determine if accessed from admin context
  const isAdminView = user?.role === "Admin" || location.state?.fromAdmin;
  const isJEOfficer = user?.role && user.role.includes("Junior");
  const isClerkOfficer = user?.role && user.role.includes("Clerk");
  const isAEOfficer = user?.role && user.role.includes("Assistant");
  const isEEOfficer = user?.role && user.role.includes("Executive");
  const isCEOfficer = user?.role && user.role.includes("City");

  const getOfficerType = (): "AE" | "EE" | "CE" => {
    if (isAEOfficer) return "AE";
    if (isEEOfficer) return "EE";
    if (isCEOfficer) return "CE";
    return "AE"; // Default
  };

  const backPath = isAdminView
    ? "/admin/applications"
    : isJEOfficer
    ? "/je-dashboard"
    : isClerkOfficer
    ? "/clerk-dashboard"
    : "/dashboard";

  // Determine the correct dashboard route based on officer role
  const getDashboardRoute = () => {
    if (!user?.role) return "/dashboard";

    if (user.role.includes("Junior")) {
      return "/je-dashboard";
    } else if (user.role.includes("Assistant")) {
      return "/ae-dashboard";
    } else if (user.role.includes("Executive")) {
      return "/ee-dashboard";
    } else if (user.role.includes("City")) {
      return "/ce-dashboard";
    } else if (user.role.includes("Clerk")) {
      return "/clerk-dashboard";
    }

    return "/dashboard";
  };

  useEffect(() => {
    const fetchApplication = async () => {
      if (!id) {
        setError("Application ID is required");
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        console.log("📋 Application Data:", {
          status: response.status,
          statusName: response.statusName,
          workflowCurrentStage: response.workflowInfo?.currentStage,
          hasAppointment: response.workflowInfo?.hasAppointment,
        });
        setApplication(response);
      } catch (err) {
        console.error("Error fetching application:", err);
        setError("Failed to load application details");
      } finally {
        setLoading(false);
      }
    };

    fetchApplication();
  }, [id]);

  // Fetch certificate status after application loads and poll if pending
  useEffect(() => {
    // Don't poll certificate status for workflow officers (JE, AE, EE, CE)
    // This is only needed for applicants and clerks
    const workflowOfficerRoles = [
      "JuniorArchitect",
      "JuniorLicenceEngineer",
      "JuniorStructuralEngineer",
      "JuniorSupervisor1",
      "JuniorSupervisor2",
      "AssistantArchitect",
      "AssistantLicenceEngineer",
      "AssistantStructuralEngineer",
      "AssistantSupervisor1",
      "AssistantSupervisor2",
      "ExecutiveEngineer",
      "CityEngineer",
    ];

    if (user?.role && workflowOfficerRoles.includes(user.role)) {
      console.log(
        `🚫 Skipping certificate status check for workflow officer role: ${user.role}`
      );
      return;
    }

    const fetchCertificateStatus = async () => {
      if (!id || !application || !application.isPaymentComplete) {
        console.log("⏸️ Skipping certificate check - payment not complete");
        return;
      }

      try {
        setLoadingCertificate(true);
        const token = localStorage.getItem("pmcrms_token");
        const userStr = localStorage.getItem("pmcrms_user");
        const user = userStr ? JSON.parse(userStr) : null;

        console.log("🔐 Certificate Status Request:");
        console.log(
          "  App ID:",
          id,
          "| Token:",
          !!token,
          "| User:",
          user?.id,
          user?.role
        );

        if (!token) {
          console.error("❌ No token - Please login!");
          return;
        }

        const response = await fetch(
          `${getApiUrl()}/Certificate/status/${id}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (response.ok) {
          const data = await response.json();
          setCertificateStatus(data);
          console.log("✅ Certificate status:", data);
        } else if (response.status === 401) {
          const errorData = await response.json().catch(() => ({}));
          console.error("❌ 401 UNAUTHORIZED");
          console.error(
            "  Logged in:",
            user?.fullName,
            `(ID: ${user?.id}, Role: ${user?.role})`
          );
          console.error("  Trying to access Application ID:", id);
          console.error(
            "  Server says:",
            errorData.message || "User not authenticated"
          );
          console.error(
            "  Fix: Logout and login again, or check if you own this application"
          );
          return;
        } else {
          const errorData = await response.json().catch(() => ({}));
          console.error("❌ HTTP", response.status, errorData);
        }
      } catch (error) {
        console.error("❌ Error fetching certificate status:", error);
      } finally {
        setLoadingCertificate(false);
      }
    };

    // Fetch certificate status only once
    fetchCertificateStatus();

    // No cleanup needed since we're not using intervals
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    id,
    application?.isPaymentComplete,
    certificateStatus?.exists,
    user?.role,
  ]);

  // Show notification when certificate becomes available
  useEffect(() => {
    // If certificate just became available (was pending, now exists)
    if (certificateStatus?.exists && certificateWasPendingRef.current) {
      setNotification({
        isOpen: true,
        message:
          "Your license certificate has been generated successfully. You can now view and download it.",
        type: "success",
        title: "Certificate Ready! 🎉",
        autoClose: true,
      });
      certificateWasPendingRef.current = false; // Reset tracking
    }

    // Track that certificate was pending if it doesn't exist yet and payment is complete
    if (
      certificateStatus !== null &&
      !certificateStatus.exists &&
      application?.isPaymentComplete
    ) {
      certificateWasPendingRef.current = true;
    }
  }, [certificateStatus, application?.isPaymentComplete]);

  // Create blob URL from base64 when selectedDocument changes
  useEffect(() => {
    // Cleanup previous blob URL
    if (pdfBlobUrl) {
      URL.revokeObjectURL(pdfBlobUrl);
      setPdfBlobUrl(null);
    }

    // Create new blob URL if we have base64 data (either pdfBase64 or fileBase64)
    const base64Data =
      selectedDocument?.pdfBase64 || selectedDocument?.fileBase64;

    if (base64Data) {
      try {
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);

        // Determine content type based on file name
        let contentType = "application/pdf";
        if (selectedDocument?.fileName) {
          const fileName = selectedDocument.fileName.toLowerCase();
          if (fileName.endsWith(".jpg") || fileName.endsWith(".jpeg")) {
            contentType = "image/jpeg";
          } else if (fileName.endsWith(".png")) {
            contentType = "image/png";
          } else if (fileName.endsWith(".gif")) {
            contentType = "image/gif";
          } else if (fileName.endsWith(".webp")) {
            contentType = "image/webp";
          }
        }

        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);
        setPdfBlobUrl(url);
        console.log("✅ Created blob URL:", url, "Type:", contentType);
      } catch (error) {
        console.error("❌ Error creating blob URL:", error);
      }
    }

    // Cleanup on unmount
    return () => {
      if (pdfBlobUrl) {
        URL.revokeObjectURL(pdfBlobUrl);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDocument]);

  const handleDocumentApprovalComplete = async () => {
    // Navigate to appropriate dashboard after successful document verification
    // The form will be removed from the officer's list after digital signature is added
    const dashboardRoute = getDashboardRoute();
    navigate(dashboardRoute);
  };

  const handleDownloadCertificate = async () => {
    if (!id) return;

    try {
      const token = getToken();
      const response = await fetch(
        `${getApiUrl()}/Certificate/download/${id}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        throw new Error("Failed to download certificate");
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `LicenceCertificate_${id}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error("Error downloading certificate:", error);
      setNotification({
        isOpen: true,
        message: "Failed to download certificate. Please try again.",
        type: "error",
        title: "Download Failed",
      });
    }
  };

  const handleViewCertificate = async () => {
    if (!id) return;

    try {
      const token = getToken();
      const response = await fetch(
        `${getApiUrl()}/Certificate/download/${id}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        throw new Error("Failed to load certificate");
      }

      const blob = await response.blob();
      const arrayBuffer = await blob.arrayBuffer();
      const base64 = btoa(
        new Uint8Array(arrayBuffer).reduce(
          (data, byte) => data + String.fromCharCode(byte),
          ""
        )
      );

      setSelectedDocument({
        fileName: certificateStatus?.fileName || "LicenceCertificate.pdf",
        documentTypeName: "Licence Certificate",
        pdfBase64: base64,
      });
    } catch (error) {
      console.error("Error viewing certificate:", error);
      setNotification({
        isOpen: true,
        message: "Failed to load certificate. Please try again.",
        type: "error",
        title: "View Failed",
      });
    }
  };

  const handleScheduleAppointment = () => {
    setShowScheduleModal(true);
    setScheduleError(""); // Clear any previous errors
    setScheduleForm({
      comments: "",
      reviewDate: "",
      contactPerson: "",
      place: "",
      roomNumber: "",
    });
  };

  const handleSubmitSchedule = async () => {
    // Clear previous error
    setScheduleError("");

    // Validate all required fields
    if (
      !application ||
      !scheduleForm.reviewDate ||
      !scheduleForm.contactPerson ||
      !scheduleForm.place ||
      !scheduleForm.roomNumber
    ) {
      setScheduleError("Please fill in all required fields");
      return;
    }

    // Trim and validate non-empty strings
    if (
      !scheduleForm.contactPerson.trim() ||
      !scheduleForm.place.trim() ||
      !scheduleForm.roomNumber.trim()
    ) {
      setScheduleError("All fields must contain valid values");
      return;
    }

    try {
      setIsScheduling(true);
      console.log("📅 Scheduling appointment:", {
        applicationId: application.id,
        ...scheduleForm,
      });

      await jeWorkflowService.scheduleAppointment({
        applicationId: application.id,
        reviewDate: scheduleForm.reviewDate,
        place: scheduleForm.place.trim(),
        contactPerson: scheduleForm.contactPerson.trim(),
        roomNumber: scheduleForm.roomNumber.trim(),
        remarks: scheduleForm.comments.trim() || undefined,
      });

      // Close schedule modal and show success popup
      setShowScheduleModal(false);
      setShowSuccessPopup(true);

      // Redirect to dashboard after 2 seconds
      setTimeout(() => {
        navigate(backPath);
      }, 2000);
    } catch (error: unknown) {
      console.error("❌ Error scheduling appointment:", error);
      // Extract error message from API response if available
      let errorMessage = "Failed to schedule appointment. Please try again.";
      if (error && typeof error === "object" && "response" in error) {
        const response = (
          error as {
            response?: { data?: { message?: string; Message?: string } };
          }
        ).response;
        errorMessage =
          response?.data?.message || response?.data?.Message || errorMessage;
      }
      setScheduleError(errorMessage);
    } finally {
      setIsScheduling(false);
    }
  };

  const handleRescheduleSubmit = async () => {
    // Validate all required fields
    if (
      !rescheduleForm.newReviewDate ||
      !rescheduleForm.rescheduleReason ||
      !rescheduleForm.place ||
      !rescheduleForm.contactPerson ||
      !rescheduleForm.roomNumber
    ) {
      setRescheduleError("Please fill in all required fields");
      return;
    }

    // Trim and validate non-empty strings
    if (
      !rescheduleForm.rescheduleReason.trim() ||
      !rescheduleForm.place.trim() ||
      !rescheduleForm.contactPerson.trim() ||
      !rescheduleForm.roomNumber.trim()
    ) {
      setRescheduleError("All fields must contain valid values");
      return;
    }

    if (!application?.workflowInfo?.appointmentId) {
      setRescheduleError("Appointment ID not found");
      return;
    }

    try {
      setRescheduleError("");
      setIsRescheduling(true);
      await jeWorkflowService.rescheduleAppointment({
        appointmentId: application.workflowInfo.appointmentId,
        newReviewDate: rescheduleForm.newReviewDate,
        rescheduleReason: rescheduleForm.rescheduleReason.trim(),
        place: rescheduleForm.place.trim(),
        contactPerson: rescheduleForm.contactPerson.trim(),
        roomNumber: rescheduleForm.roomNumber.trim(),
      });

      setShowRescheduleModal(false);
      setNotification({
        isOpen: true,
        message: `Appointment rescheduled successfully! User will be notified via email.`,
        type: "success",
        title: "Appointment Rescheduled",
        autoClose: true,
      });

      // Refresh application data
      if (id) {
        const response = await positionRegistrationService.getApplication(
          parseInt(id)
        );
        setApplication(response);
      }
    } catch (error: unknown) {
      console.error("❌ Error rescheduling appointment:", error);
      // Extract error message from API response if available
      let errorMessage = "Failed to reschedule appointment. Please try again.";
      if (error && typeof error === "object" && "response" in error) {
        const response = (
          error as {
            response?: { data?: { message?: string; Message?: string } };
          }
        ).response;
        errorMessage =
          response?.data?.message || response?.data?.Message || errorMessage;
      }
      setRescheduleError(errorMessage);
    } finally {
      setIsRescheduling(false);
    }
  };

  const handleClerkApprove = async () => {
    if (!application) return;
    setShowClerkApprovalModal(true);
  };

  const handleClerkApprovalSubmit = async () => {
    if (!application) return;

    try {
      setIsApprovingClerk(true);
      const result = await clerkWorkflowService.approveApplication(
        application.id,
        clerkRemarks
      );

      if (result.success) {
        setShowClerkApprovalModal(false);
        setNotification({
          isOpen: true,
          message:
            "Application approved successfully and forwarded to Executive Engineer (Stage 2)!",
          type: "success",
          title: "Approval Successful",
          autoClose: true,
        });

        setTimeout(() => {
          navigate(getDashboardRoute());
        }, 2000);
      } else {
        setNotification({
          isOpen: true,
          message: result.message || "Failed to approve application",
          type: "error",
          title: "Approval Failed",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error approving application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to approve application. Please try again.",
        type: "error",
        title: "Approval Failed",
        autoClose: false,
      });
    } finally {
      setIsApprovingClerk(false);
    }
  };

  const handleRejectApplication = async () => {
    if (!application) return;

    // Validate rejection comments
    if (!rejectionComments.trim()) {
      setNotification({
        isOpen: true,
        message: "Rejection comments are mandatory",
        type: "error",
        title: "Validation Error",
        autoClose: false,
      });
      return;
    }

    setIsRejecting(true);

    try {
      let result;

      // Clerk rejection
      if (user?.role.includes("Clerk")) {
        result = await clerkWorkflowService.rejectApplication(
          application.id,
          rejectionComments
        );

        if (result.success) {
          setShowRejectModal(false);
          setNotification({
            isOpen: true,
            message: "Application rejected successfully!",
            type: "success",
            title: "Rejection Successful",
            autoClose: true,
          });

          setTimeout(() => {
            navigate(getDashboardRoute());
          }, 2000);
        } else {
          setNotification({
            isOpen: true,
            message: result.message || "Failed to reject application",
            type: "error",
            title: "Rejection Failed",
            autoClose: false,
          });
        }
        setIsRejecting(false);
        return;
      }

      // Junior Engineer rejection
      if (user?.role.includes("Junior")) {
        result = await jeWorkflowService.rejectApplication({
          applicationId: application.id,
          rejectionComments: rejectionComments,
        });

        if (result.success) {
          setShowRejectModal(false);
          setNotification({
            isOpen: true,
            message: "Application rejected successfully!",
            type: "success",
            title: "Rejection Successful",
            autoClose: true,
          });

          setTimeout(() => {
            navigate(getDashboardRoute());
          }, 2000);
        } else {
          setNotification({
            isOpen: true,
            message: result.message || "Failed to reject application",
            type: "error",
            title: "Rejection Failed",
            autoClose: false,
          });
        }
        setIsRejecting(false);
        return;
      }

      // Assistant Engineer rejection
      if (user?.role.includes("Assistant")) {
        result = await aeWorkflowService.rejectApplication({
          applicationId: application.id,
          positionType: application.positionType as PositionType,
          rejectionComments: rejectionComments,
        });

        if (result.success) {
          setShowRejectModal(false);
          setNotification({
            isOpen: true,
            message: "Application rejected successfully!",
            type: "success",
            title: "Rejection Successful",
            autoClose: true,
          });

          setTimeout(() => {
            navigate(getDashboardRoute());
          }, 2000);
        } else {
          setNotification({
            isOpen: true,
            message: result.message || "Failed to reject application",
            type: "error",
            title: "Rejection Failed",
            autoClose: false,
          });
        }
        setIsRejecting(false);
        return;
      }

      // Executive Engineer rejection
      if (user?.role.includes("Executive")) {
        result = await eeWorkflowService.rejectApplication({
          applicationId: application.id,
          rejectionComments: rejectionComments,
        });

        if (result.success) {
          setShowRejectModal(false);
          setNotification({
            isOpen: true,
            message: "Application rejected successfully!",
            type: "success",
            title: "Rejection Successful",
            autoClose: true,
          });

          setTimeout(() => {
            navigate(getDashboardRoute());
          }, 2000);
        } else {
          setNotification({
            isOpen: true,
            message: result.message || "Failed to reject application",
            type: "error",
            title: "Rejection Failed",
            autoClose: false,
          });
        }
        setIsRejecting(false);
        return;
      }

      // City Engineer rejection
      if (user?.role.includes("City")) {
        result = await ceWorkflowService.rejectApplication({
          applicationId: application.id,
          rejectionComments: rejectionComments,
        });

        if (result.success) {
          setShowRejectModal(false);
          setNotification({
            isOpen: true,
            message: "Application rejected successfully!",
            type: "success",
            title: "Rejection Successful",
            autoClose: true,
          });

          setTimeout(() => {
            navigate(getDashboardRoute());
          }, 2000);
        } else {
          setNotification({
            isOpen: true,
            message: result.message || "Failed to reject application",
            type: "error",
            title: "Rejection Failed",
            autoClose: false,
          });
        }
        setIsRejecting(false);
        return;
      }

      // Fallback for unrecognized roles
      console.log("🚫 Rejecting application:", {
        applicationId: application.id,
        remarks: rejectionComments,
      });

      setNotification({
        isOpen: true,
        message: "Rejection not supported for this role.",
        type: "error",
        title: "Rejection Failed",
        autoClose: false,
      });
      setIsRejecting(false);
    } catch (error) {
      console.error("❌ Error rejecting application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to reject application. Please try again.",
        type: "error",
        title: "Rejection Failed",
        autoClose: false,
      });
      setIsRejecting(false);
    }
  };

  const openRejectModal = () => {
    setRejectionComments("");
    setShowRejectModal(true);
  };

  // OTP Verification Functions for AE, EE, CE Officers
  const handleVerifyDocuments = () => {
    setShowOTPModal(true);
  };

  const handleGenerateOtp = async (
    applicationId: number
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      if (user?.role.includes("Assistant")) {
        return await aeWorkflowService.generateOtpForSignature(applicationId);
      } else if (user?.role.includes("Executive")) {
        // Check if this is a Stage 2 application (license certificate signature)
        if (
          application?.status === "EXECUTIVE_ENGINEER_SIGN_PENDING" ||
          application?.status === 32
        ) {
          // Use EE Stage 2 service for license certificate signature
          const result = await eeStage2WorkflowService.generateOtp(
            applicationId
          );
          return { success: result.success, message: result.message };
        }
        // Use EE Stage 1 service for recommendation form signature
        return await eeWorkflowService.generateOtpForSignature(applicationId);
      } else if (user?.role.includes("City")) {
        // Check if this is a Stage 2 application (final license certificate signature)
        if (
          application?.status === "CITY_ENGINEER_SIGN_PENDING" ||
          application?.status === 34
        ) {
          // Use CE Stage 2 service for final license certificate signature
          const result = await ceStage2WorkflowService.generateOtp(
            applicationId
          );
          return { success: result.success, message: result.message };
        }
        // Use CE Stage 1 service for recommendation form signature
        return await ceWorkflowService.generateOtpForSignature(applicationId);
      }
      return { success: false, message: "OTP generation not supported" };
    } catch (error) {
      console.error("Error generating OTP:", error);
      return { success: false, message: "Failed to generate OTP" };
    }
  };

  const handleVerifyAndSign = async (
    applicationId: number,
    otp: string,
    comments?: string
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      if (user?.role.includes("Assistant")) {
        const getDefaultPositionType = (role: string): PositionType => {
          const roleToPositionType: Record<string, PositionType> = {
            AssistantArchitect: 0 as PositionType,
            AssistantStructuralEngineer: 2 as PositionType,
            AssistantLicenceEngineer: 1 as PositionType,
            AssistantSupervisor1: 3 as PositionType,
            AssistantSupervisor2: 4 as PositionType,
          };
          return roleToPositionType[role] ?? (0 as PositionType);
        };
        const positionType = getDefaultPositionType(user!.role);
        return await aeWorkflowService.verifyAndSignDocuments({
          applicationId,
          positionType,
          otp,
          comments,
        });
      } else if (user?.role.includes("Executive")) {
        // Check if this is a Stage 2 application (license certificate signature)
        if (
          application?.status === "EXECUTIVE_ENGINEER_SIGN_PENDING" ||
          application?.status === 32
        ) {
          // Use EE Stage 2 service for license certificate signature → CE Stage 2
          const result = await eeStage2WorkflowService.applyDigitalSignature(
            applicationId,
            otp,
            comments
          );
          return { success: result.success, message: result.message };
        }
        // Use EE Stage 1 service for recommendation form signature
        return await eeWorkflowService.verifyAndSignDocuments({
          applicationId,
          otp,
          comments,
        });
      } else if (user?.role.includes("City")) {
        // Check if this is a Stage 2 application (final license certificate signature)
        if (
          application?.status === "CITY_ENGINEER_SIGN_PENDING" ||
          application?.status === 34
        ) {
          // Use CE Stage 2 service for final license certificate signature → APPROVED status
          const result = await ceStage2WorkflowService.applyFinalSignature(
            applicationId,
            otp,
            comments
          );
          return { success: result.success, message: result.message };
        }
        // Use CE Stage 1 service for recommendation form signature → Payment stage
        return await ceWorkflowService.verifyAndSignDocuments({
          applicationId,
          otp,
          comments,
        });
      }
      return { success: false, message: "Verification not supported" };
    } catch (error) {
      console.error("Error verifying and signing:", error);
      return { success: false, message: "Failed to verify and sign documents" };
    }
  };

  const handleOTPVerificationComplete = async () => {
    setShowOTPModal(false);
    setNotification({
      isOpen: true,
      message: "Documents verified and signed successfully!",
      type: "success",
      title: "Verification Successful",
      autoClose: true,
    });
    setTimeout(() => {
      navigate(getDashboardRoute());
    }, 2000);
  };

  if (loading) {
    return <PageLoader message="Loading application details..." />;
  }

  if (error || !application) {
    return (
      <div className="pmc-container" style={{ padding: "40px 20px" }}>
        <div className="pmc-card">
          <div
            className="pmc-card-body"
            style={{ textAlign: "center", padding: "40px" }}
          >
            <XCircle size={48} color="#dc2626" style={{ margin: "0 auto" }} />
            <h2 style={{ marginTop: "16px", color: "#dc2626" }}>Error</h2>
            <p style={{ color: "#64748b", marginTop: "8px" }}>{error}</p>
            <button
              onClick={() => navigate(backPath)}
              className="pmc-button pmc-button-primary"
              style={{
                marginTop: "24px",
                color: "#fff",
                backgroundColor: "#1e40af",
              }}
            >
              <ArrowLeft size={16} style={{ marginRight: "8px" }} />
              {isAdminView ? "Back to Applications" : "Back to Dashboard"}
            </button>
          </div>
        </div>
      </div>
    );
  }

  const getStatusBadge = (status: number | string, statusName: string) => {
    // Use statusName from backend for display
    const displayText = statusName || "Under Review";

    // Convert status to number for comparison
    const statusNum = typeof status === "string" ? parseInt(status) : status;

    // Determine badge class based on status code
    let badgeClass = "pmc-badge pmc-status-under-review";

    if (statusNum === 1) {
      badgeClass = "pmc-badge pmc-status-pending"; // Draft
    } else if (statusNum === 23) {
      badgeClass = "pmc-badge pmc-status-approved"; // Completed
    }

    return <span className={badgeClass}>{displayText}</span>;
  };

  return (
    <>
      <NotificationModal
        isOpen={notification.isOpen}
        onClose={() => setNotification({ ...notification, isOpen: false })}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        autoClose={notification.autoClose}
        autoCloseDuration={2000}
      />
      <div className="pmc-container" style={{ padding: "20px" }}>
        {/* Header */}
        <div style={{ marginBottom: "24px" }}>
          <button
            onClick={() => navigate(backPath)}
            className="pmc-button pmc-button-secondary pmc-button-sm"
            style={{
              marginBottom: "16px",
              color: "#1e40af",
              backgroundColor: "#fff",
              border: "1px solid #cbd5e1",
            }}
          >
            <ArrowLeft size={16} style={{ marginRight: "8px" }} />
            {isAdminView ? "Back to Applications" : "Back to Dashboard"}
          </button>

          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <div>
              <h1 className="pmc-page-title">Application Details</h1>
              {application.applicationNumber && (
                <p
                  style={{
                    color: "#64748b",
                    fontSize: "14px",
                    marginTop: "4px",
                  }}
                >
                  Application #:{" "}
                  <strong>{application.applicationNumber}</strong>
                </p>
              )}
            </div>
            {getStatusBadge(application.status, application.statusName)}
          </div>
        </div>

        {/* Rejection Banner - Only show if application is rejected */}
        {(application.status === 37 || application.status === "REJECTED") && (
          <div
            className="pmc-fadeIn"
            style={{
              padding: "16px 20px",
              marginBottom: "20px",
              background: "linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)",
              border: "2px solid #fca5a5",
              borderRadius: "10px",
              boxShadow: "0 4px 12px rgba(220, 38, 38, 0.1)",
            }}
          >
            <div
              style={{
                display: "flex",
                gap: "12px",
                alignItems: "flex-start",
                marginBottom: "12px",
              }}
            >
              <AlertCircle
                size={24}
                style={{ color: "#dc2626", flexShrink: 0, marginTop: "2px" }}
              />
              <div style={{ flex: 1 }}>
                <h3
                  className="pmc-text-lg pmc-font-bold"
                  style={{ color: "#dc2626", marginBottom: "8px" }}
                >
                  Application Rejected
                </h3>
                <p
                  className="pmc-text-sm pmc-font-semibold"
                  style={{ color: "#7f1d1d", marginBottom: "6px" }}
                >
                  Rejection Reason:
                </p>
                <p
                  className="pmc-text-sm"
                  style={{
                    color: "#7f1d1d",
                    lineHeight: "1.7",
                    background: "rgba(127, 29, 29, 0.05)",
                    padding: "10px 12px",
                    borderRadius: "6px",
                    border: "1px solid rgba(127, 29, 29, 0.15)",
                  }}
                >
                  {application.jeRejectionStatus &&
                  application.jeRejectionComments
                    ? `Junior Engineer: ${application.jeRejectionComments}`
                    : application.aeArchitectRejectionStatus &&
                      application.aeArchitectRejectionComments
                    ? `Assistant Engineer (Architect): ${application.aeArchitectRejectionComments}`
                    : application.aeStructuralRejectionStatus &&
                      application.aeStructuralRejectionComments
                    ? `Assistant Engineer (Structural): ${application.aeStructuralRejectionComments}`
                    : application.executiveEngineerRejectionStatus &&
                      application.executiveEngineerRejectionComments
                    ? `Executive Engineer: ${application.executiveEngineerRejectionComments}`
                    : application.cityEngineerRejectionStatus &&
                      application.cityEngineerRejectionComments
                    ? `City Engineer: ${application.cityEngineerRejectionComments}`
                    : application.remarks || "No rejection comments provided"}
                </p>
              </div>
            </div>
            <div
              style={{
                paddingTop: "12px",
                borderTop: "1px solid rgba(220, 38, 38, 0.2)",
              }}
            >
              <p
                className="pmc-text-sm pmc-font-medium"
                style={{ color: "#991b1b" }}
              >
                📝 Please review the rejection comments above and make necessary
                corrections. You can resubmit your application from the
                dashboard.
              </p>
            </div>
          </div>
        )}

        {/* Basic Information */}
        <div className="pmc-card" style={{ marginBottom: "16px" }}>
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Basic Information
            </h2>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-form-grid pmc-form-grid-3">
              <div>
                <label className="pmc-label">Position Type</label>
                <p className="pmc-value">{application.positionTypeName}</p>
              </div>
              <div>
                <label className="pmc-label">Full Name</label>
                <p className="pmc-value">{application.fullName}</p>
              </div>
              <div>
                <label className="pmc-label">Mother's Name</label>
                <p className="pmc-value">{application.motherName}</p>
              </div>
              <div>
                <label className="pmc-label">Mobile Number</label>
                <p className="pmc-value">{application.mobileNumber}</p>
              </div>
              <div>
                <label className="pmc-label">Email Address</label>
                <p className="pmc-value">{application.emailAddress}</p>
              </div>
              <div>
                <label className="pmc-label">Gender</label>
                <p className="pmc-value">{application.genderName}</p>
              </div>
              <div>
                <label className="pmc-label">Date of Birth</label>
                <p className="pmc-value">
                  {new Date(application.dateOfBirth).toLocaleDateString()} (
                  {application.age} years)
                </p>
              </div>
              {application.bloodGroup && (
                <div>
                  <label className="pmc-label">Blood Group</label>
                  <p className="pmc-value">{application.bloodGroup}</p>
                </div>
              )}
              {application.height && (
                <div>
                  <label className="pmc-label">Height</label>
                  <p className="pmc-value">{application.height} cm</p>
                </div>
              )}
              <div>
                <label className="pmc-label">PAN Card Number</label>
                <p className="pmc-value">{application.panCardNumber}</p>
              </div>
              <div>
                <label className="pmc-label">Aadhar Card Number</label>
                <p className="pmc-value">{application.aadharCardNumber}</p>
              </div>
              {application.coaCardNumber && (
                <div>
                  <label className="pmc-label">COA Card Number</label>
                  <p className="pmc-value">{application.coaCardNumber}</p>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Addresses */}
        {application.addresses.map((address) => (
          <div
            className="pmc-card"
            style={{ marginBottom: "16px" }}
            key={address.id}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                {address.addressType} Address
              </h2>
            </div>
            <div className="pmc-card-body">
              <p className="pmc-value">{address.fullAddress}</p>
            </div>
          </div>
        ))}

        {/* Qualifications */}
        {application.qualifications.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Educational Qualifications
              </h2>
            </div>
            <div className="pmc-card-body">
              {application.qualifications.map((qual, index) => (
                <div
                  key={qual.id}
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    marginBottom:
                      index < application.qualifications.length - 1
                        ? "12px"
                        : "0",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <div className="pmc-form-grid pmc-form-grid-3">
                    <div>
                      <label className="pmc-label">Institute Name</label>
                      <p className="pmc-value">{qual.instituteName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">University Name</label>
                      <p className="pmc-value">{qual.universityName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Degree/Specialization</label>
                      <p className="pmc-value">
                        {qual.degreeName} ({qual.specializationName})
                      </p>
                    </div>
                    <div>
                      <label className="pmc-label">Passing Month & Year</label>
                      <p className="pmc-value">
                        {qual.passingMonthName} {qual.yearOfPassing}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Experiences */}
        {application.experiences.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Work Experience
              </h2>
            </div>
            <div className="pmc-card-body">
              {application.experiences.map((exp, index) => (
                <div
                  key={exp.id}
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    marginBottom:
                      index < application.experiences.length - 1 ? "12px" : "0",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <div className="pmc-form-grid pmc-form-grid-3">
                    <div>
                      <label className="pmc-label">Company Name</label>
                      <p className="pmc-value">{exp.companyName}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Position</label>
                      <p className="pmc-value">{exp.position}</p>
                    </div>
                    <div>
                      <label className="pmc-label">Duration</label>
                      <p className="pmc-value">
                        {new Date(exp.fromDate).toLocaleDateString()} -{" "}
                        {new Date(exp.toDate).toLocaleDateString()}
                      </p>
                    </div>
                    <div>
                      <label className="pmc-label">Total Experience</label>
                      <p className="pmc-value">{exp.yearsOfExperience} years</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Documents */}
        {application.documents.length > 0 && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Uploaded Documents
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                {application.documents
                  .filter(
                    (doc) =>
                      doc.documentTypeName !== "RecommendedForm" &&
                      doc.documentTypeName !== "LicenceCertificate" &&
                      doc.documentTypeName !== "PaymentChallan"
                  )
                  .map((doc) => (
                    <div
                      key={doc.id}
                      style={{
                        padding: "16px",
                        background: "#f8fafc",
                        borderRadius: "8px",
                        border: "1px solid #e2e8f0",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "12px",
                        }}
                      >
                        <FileText size={24} color="#3b82f6" />
                        <div>
                          <p
                            className="pmc-value"
                            style={{ marginBottom: "4px" }}
                          >
                            {doc.documentTypeName}
                          </p>
                          <p style={{ fontSize: "12px", color: "#64748b" }}>
                            {doc.fileName}
                          </p>
                          {doc.fileSize && (
                            <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                              {(doc.fileSize / 1024).toFixed(2)} KB
                            </p>
                          )}
                        </div>
                      </div>
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        {doc.isVerified ? (
                          <span title="Verified">
                            <CheckCircle size={20} color="#10b981" />
                          </span>
                        ) : (
                          <span title="Not Verified">
                            <XCircle size={20} color="#94a3b8" />
                          </span>
                        )}
                        <button
                          className="pmc-button pmc-button-primary pmc-button-sm"
                          title="View Document"
                          onClick={() =>
                            setSelectedDocument({
                              fileName: doc.fileName,
                              filePath: doc.filePath,
                              documentTypeName: doc.documentTypeName,
                              fileBase64: doc.fileBase64, // Add base64 data
                            })
                          }
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Eye size={14} />
                          View
                        </button>
                        <button
                          className="pmc-button pmc-button-secondary pmc-button-sm"
                          title="Download Document"
                          onClick={() => {
                            if (doc.fileBase64) {
                              try {
                                // Download from base64 binary data stored in database
                                const byteCharacters = atob(doc.fileBase64);
                                const byteNumbers = new Array(
                                  byteCharacters.length
                                );
                                for (
                                  let i = 0;
                                  i < byteCharacters.length;
                                  i++
                                ) {
                                  byteNumbers[i] = byteCharacters.charCodeAt(i);
                                }
                                const byteArray = new Uint8Array(byteNumbers);
                                const blob = new Blob([byteArray], {
                                  type:
                                    doc.contentType ||
                                    "application/octet-stream",
                                });
                                const url = URL.createObjectURL(blob);
                                const link = document.createElement("a");
                                link.href = url;
                                link.download = doc.fileName;
                                document.body.appendChild(link);
                                link.click();
                                document.body.removeChild(link);
                                URL.revokeObjectURL(url);
                              } catch (error) {
                                console.error(
                                  "Error downloading document:",
                                  error
                                );
                                setNotification({
                                  isOpen: true,
                                  message:
                                    "Failed to download document. Please try again.",
                                  type: "error",
                                  title: "Download Failed",
                                });
                              }
                            } else {
                              // No binary data available
                              console.error(
                                "Document binary data not available:",
                                doc.fileName
                              );
                              setNotification({
                                isOpen: true,
                                message:
                                  "Document data not available. Please contact support.",
                                type: "error",
                                title: "Download Failed",
                              });
                            }
                          }}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                            color: "#1e40af",
                            backgroundColor: "#fff",
                            border: "1px solid #cbd5e1",
                          }}
                        >
                          <Download size={14} />
                          Download
                        </button>
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          </div>
        )}

        {/* Recommendation Form - Separate Section */}
        {application.recommendationForm && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
                color: "#334155",
                padding: "12px 16px",
                borderBottom: "2px solid #cbd5e1",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "#334155", margin: 0 }}
              >
                Recommendation Form
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div
                  style={{
                    padding: "16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    border: "1px solid #e2e8f0",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                    }}
                  >
                    <FileText size={24} color="#3b82f6" />
                    <div>
                      <p className="pmc-value" style={{ marginBottom: "4px" }}>
                        Recommendation Form
                      </p>
                      <p style={{ fontSize: "12px", color: "#64748b" }}>
                        {application.recommendationForm.fileName}
                      </p>
                      {application.recommendationForm.fileSize && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          {(
                            application.recommendationForm.fileSize / 1024
                          ).toFixed(2)}{" "}
                          KB
                        </p>
                      )}
                    </div>
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    <button
                      className="pmc-button pmc-button-primary pmc-button-sm"
                      onClick={() =>
                        setSelectedDocument({
                          fileName: application.recommendationForm!.fileName,
                          filePath: "",
                          documentTypeName: "Recommendation Form",
                          pdfBase64: application.recommendationForm!.pdfBase64,
                        })
                      }
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Eye size={14} />
                      View
                    </button>
                    <button
                      className="pmc-button pmc-button-secondary pmc-button-sm"
                      onClick={() => {
                        // Download from base64 data
                        const byteCharacters = atob(
                          application.recommendationForm!.pdfBase64
                        );
                        const byteNumbers = new Array(byteCharacters.length);
                        for (let i = 0; i < byteCharacters.length; i++) {
                          byteNumbers[i] = byteCharacters.charCodeAt(i);
                        }
                        const byteArray = new Uint8Array(byteNumbers);
                        const blob = new Blob([byteArray], {
                          type: "application/pdf",
                        });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download =
                          application.recommendationForm!.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(url);
                      }}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Download size={14} />
                      Download
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* License Certificate - Show if it exists in database */}
        {application.licenseCertificate && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                color: "white",
                padding: "12px 16px",
                borderBottom: "2px solid #059669",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "white", margin: 0 }}
              >
                License Certificate
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div
                  style={{
                    padding: "16px",
                    background: "#f0fdf4",
                    borderRadius: "8px",
                    border: "1px solid #86efac",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                    }}
                  >
                    <CheckCircle size={24} color="#10b981" />
                    <div>
                      <p
                        className="pmc-value"
                        style={{ marginBottom: "4px", color: "#059669" }}
                      >
                        License Certificate Generated
                      </p>
                      <p style={{ fontSize: "12px", color: "#64748b" }}>
                        {application.licenseCertificate.fileName}
                      </p>
                      {application.licenseCertificate.fileSize && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          {(
                            application.licenseCertificate.fileSize / 1024
                          ).toFixed(2)}{" "}
                          KB
                        </p>
                      )}
                      {application.licenseCertificate.lastSignedDate && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          Last signed:{" "}
                          {new Date(
                            application.licenseCertificate.lastSignedDate
                          ).toLocaleDateString("en-IN")}
                        </p>
                      )}
                    </div>
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    <button
                      className="pmc-button pmc-button-success pmc-button-sm"
                      onClick={() =>
                        setSelectedDocument({
                          fileName: application.licenseCertificate!.fileName,
                          filePath: "",
                          documentTypeName: "License Certificate",
                          pdfBase64: application.licenseCertificate!.pdfBase64,
                        })
                      }
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Eye size={14} />
                      View
                    </button>
                    <button
                      className="pmc-button pmc-button-secondary pmc-button-sm"
                      onClick={() => {
                        const byteCharacters = atob(
                          application.licenseCertificate!.pdfBase64
                        );
                        const byteNumbers = new Array(byteCharacters.length);
                        for (let i = 0; i < byteCharacters.length; i++) {
                          byteNumbers[i] = byteCharacters.charCodeAt(i);
                        }
                        const byteArray = new Uint8Array(byteNumbers);
                        const blob = new Blob([byteArray], {
                          type: "application/pdf",
                        });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download =
                          application.licenseCertificate!.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(url);
                      }}
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Download size={14} />
                      Download
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Challan (Payment Receipt) - Show if it exists */}
        {application.challan && (
          <div className="pmc-card" style={{ marginBottom: "16px" }}>
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                color: "white",
                padding: "12px 16px",
                borderBottom: "2px solid #2563eb",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{ color: "white", margin: 0 }}
              >
                Payment Challan
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                <div
                  style={{
                    padding: "16px",
                    background: "#eff6ff",
                    borderRadius: "8px",
                    border: "1px solid #93c5fd",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                    }}
                  >
                    <CreditCard size={24} color="#3b82f6" />
                    <div>
                      <p
                        className="pmc-value"
                        style={{ marginBottom: "4px", color: "#2563eb" }}
                      >
                        Challan #{application.challan.challanNumber}
                      </p>
                      <p style={{ fontSize: "12px", color: "#64748b" }}>
                        Amount: ₹{application.challan.amount.toFixed(2)}
                      </p>
                      {application.challan.createdDate && (
                        <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                          Generated:{" "}
                          {new Date(
                            application.challan.createdDate
                          ).toLocaleDateString("en-IN")}
                        </p>
                      )}
                      {application.challan.paidDate && (
                        <p style={{ fontSize: "11px", color: "#10b981" }}>
                          Paid:{" "}
                          {new Date(
                            application.challan.paidDate
                          ).toLocaleDateString("en-IN")}
                        </p>
                      )}
                    </div>
                  </div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    {application.challan.pdfBase64 && (
                      <>
                        <button
                          className="pmc-button pmc-button-primary pmc-button-sm"
                          onClick={() =>
                            setSelectedDocument({
                              fileName: `Challan_${
                                application.challan!.challanNumber
                              }.pdf`,
                              filePath: "",
                              documentTypeName: "Payment Challan",
                              pdfBase64: application.challan!.pdfBase64,
                            })
                          }
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Eye size={14} />
                          View
                        </button>
                        <button
                          className="pmc-button pmc-button-secondary pmc-button-sm"
                          onClick={() => {
                            const byteCharacters = atob(
                              application.challan!.pdfBase64
                            );
                            const byteNumbers = new Array(
                              byteCharacters.length
                            );
                            for (let i = 0; i < byteCharacters.length; i++) {
                              byteNumbers[i] = byteCharacters.charCodeAt(i);
                            }
                            const byteArray = new Uint8Array(byteNumbers);
                            const blob = new Blob([byteArray], {
                              type: "application/pdf",
                            });
                            const url = URL.createObjectURL(blob);
                            const link = document.createElement("a");
                            link.href = url;
                            link.download = `Challan_${
                              application.challan!.challanNumber
                            }.pdf`;
                            document.body.appendChild(link);
                            link.click();
                            document.body.removeChild(link);
                            URL.revokeObjectURL(url);
                          }}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Download size={14} />
                          Download
                        </button>
                      </>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* License Certificate - Separate Section */}
        {/* Show for: (1) Payment complete OR (2) Architect position at Clerk stage or beyond */}
        {(application.isPaymentComplete ||
          (application.positionType === 0 &&
            (typeof application.status === "number"
              ? application.status
              : parseInt(application.status)) >= 35)) && // Architect at CLERK_PENDING (35) or beyond
          (isClerkOfficer ||
            (isEEOfficer &&
              (typeof application.status === "number"
                ? application.status
                : parseInt(application.status)) >= 19) || // EE Stage 2 (Digital Signature stages)
            (isCEOfficer &&
              (typeof application.status === "number"
                ? application.status
                : parseInt(application.status)) >= 21) || // CE Stage 2 (Final Approval stages)
            (!isJEOfficer &&
              !isAEOfficer &&
              !isEEOfficer &&
              !isCEOfficer &&
              !isClerkOfficer)) && ( // Regular user
            <div className="pmc-card" style={{ marginBottom: "16px" }}>
              <div
                className="pmc-card-header"
                style={{
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  color: "white",
                  padding: "12px 16px",
                  borderBottom: "2px solid #059669",
                }}
              >
                <h2
                  className="pmc-card-title"
                  style={{ color: "white", margin: 0 }}
                >
                  License Certificate
                </h2>
              </div>
              <div className="pmc-card-body">
                <div className="pmc-form-grid pmc-form-grid-2">
                  {loadingCertificate ? (
                    <div
                      style={{
                        padding: "16px",
                        background: "#f8fafc",
                        borderRadius: "8px",
                        border: "1px solid #e2e8f0",
                        textAlign: "center",
                      }}
                    >
                      <p style={{ color: "#64748b" }}>
                        Loading certificate status...
                      </p>
                    </div>
                  ) : certificateStatus?.exists ? (
                    <div
                      style={{
                        padding: "16px",
                        background: "#f0fdf4",
                        borderRadius: "8px",
                        border: "1px solid #86efac",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "12px",
                        }}
                      >
                        <CheckCircle size={24} color="#10b981" />
                        <div>
                          <p
                            className="pmc-value"
                            style={{ marginBottom: "4px", color: "#059669" }}
                          >
                            License Certificate Generated
                          </p>
                          <p style={{ fontSize: "12px", color: "#64748b" }}>
                            {certificateStatus.fileName ||
                              "LicenceCertificate.pdf"}
                          </p>
                          {certificateStatus.fileSize && (
                            <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                              {(certificateStatus.fileSize / 1024).toFixed(2)}{" "}
                              KB
                            </p>
                          )}
                          {certificateStatus.generatedDate && (
                            <p style={{ fontSize: "11px", color: "#94a3b8" }}>
                              Generated on:{" "}
                              {new Date(
                                certificateStatus.generatedDate
                              ).toLocaleDateString("en-IN")}
                            </p>
                          )}
                        </div>
                      </div>
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                        }}
                      >
                        <button
                          className="pmc-button pmc-button-success pmc-button-sm"
                          onClick={handleViewCertificate}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Eye size={14} />
                          View
                        </button>
                        <button
                          className="pmc-button pmc-button-secondary pmc-button-sm"
                          onClick={handleDownloadCertificate}
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "4px",
                          }}
                        >
                          <Download size={14} />
                          Download
                        </button>
                      </div>
                    </div>
                  ) : (
                    <div
                      style={{
                        padding: "16px",
                        background: "#fef3c7",
                        borderRadius: "8px",
                        border: "1px solid #fbbf24",
                        display: "flex",
                        alignItems: "center",
                        gap: "12px",
                      }}
                    >
                      <Info size={24} color="#f59e0b" />
                      <div>
                        <p
                          className="pmc-value"
                          style={{ marginBottom: "4px", color: "#d97706" }}
                        >
                          Certificate Generation Pending
                        </p>
                        <p style={{ fontSize: "12px", color: "#92400e" }}>
                          Your license certificate is being generated. This may
                          take a few moments after payment completion.
                        </p>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

        {/* Payment Section - Only show for regular users after CE Stage 1, or for Stage 2 officers (EE/CE), Clerk */}
        {/* Don't show for Architect position (positionType = 0) as it has no fees */}
        {!isJEOfficer &&
          !user?.role.includes("Assistant") &&
          !user?.role.includes("Executive") &&
          !user?.role.includes("City") &&
          application.positionType !== 0 &&
          (application.challanAmount ?? 0) > 0 && (
            <div
              className="pmc-card"
              style={{
                marginBottom: "16px",
                border: application.isPaymentComplete
                  ? "2px solid #10b981"
                  : "2px solid #f59e0b",
                boxShadow: application.isPaymentComplete
                  ? "0 2px 8px rgba(16, 185, 129, 0.15)"
                  : "0 2px 8px rgba(245, 158, 11, 0.15)",
              }}
            >
              <div
                className="pmc-card-header"
                style={{
                  background: application.isPaymentComplete
                    ? "linear-gradient(135deg, #10b981 0%, #059669 100%)"
                    : "linear-gradient(135deg, #f59e0b 0%, #d97706 100%)",
                  color: "white",
                  padding: "12px 16px",
                  borderBottom: application.isPaymentComplete
                    ? "2px solid #059669"
                    : "2px solid #d97706",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                  }}
                >
                  <h2
                    className="pmc-card-title"
                    style={{
                      color: "white",
                      margin: 0,
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                    }}
                  >
                    {application.isPaymentComplete ? (
                      <CheckCircle size={20} />
                    ) : (
                      <CreditCard size={20} />
                    )}
                    {application.isPaymentComplete
                      ? "Payment Completed"
                      : "Payment Required"}
                  </h2>
                  {application.isPaymentComplete && (
                    <span
                      style={{
                        padding: "4px 12px",
                        background: "rgba(255, 255, 255, 0.25)",
                        color: "white",
                        fontSize: "12px",
                        fontWeight: "600",
                        borderRadius: "9999px",
                        display: "flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      ✓ Verified
                    </span>
                  )}
                </div>
              </div>
              <div className="pmc-card-body">
                {!application.isPaymentComplete ? (
                  <>
                    {/* Payment Pending Notice */}
                    <div
                      style={{
                        background:
                          "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
                        padding: "16px",
                        borderRadius: "8px",
                        border: "1px solid #fcd34d",
                        marginBottom: "20px",
                        display: "flex",
                        gap: "12px",
                      }}
                    >
                      <Info
                        size={24}
                        color="#d97706"
                        style={{ flexShrink: 0 }}
                      />
                      <div>
                        <p
                          style={{
                            margin: 0,
                            fontWeight: 600,
                            color: "#78350f",
                            marginBottom: "4px",
                          }}
                        >
                          Application Under Review - Payment Required
                        </p>
                        <p
                          style={{
                            margin: 0,
                            fontSize: "14px",
                            color: "#92400e",
                          }}
                        >
                          Your application has been reviewed by City Engineer
                          (Stage 1). Please complete the payment to proceed with
                          final processing.
                        </p>
                      </div>
                    </div>

                    {/* Payment Info Card */}
                    <div
                      style={{
                        background: "#fffbeb",
                        padding: "20px",
                        borderRadius: "8px",
                        border: "2px solid #fcd34d",
                        marginBottom: "20px",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "12px",
                          marginBottom: "16px",
                        }}
                      >
                        <div
                          style={{
                            width: "48px",
                            height: "48px",
                            borderRadius: "50%",
                            background:
                              "linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%)",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            color: "white",
                          }}
                        >
                          <CreditCard size={24} />
                        </div>
                        <div>
                          <h3
                            style={{
                              margin: 0,
                              fontSize: "18px",
                              color: "#78350f",
                              marginBottom: "4px",
                            }}
                          >
                            Payment Details
                          </h3>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "14px",
                              color: "#92400e",
                            }}
                          >
                            Complete payment to proceed with application
                            processing
                          </p>
                        </div>
                      </div>

                      <div
                        className="pmc-form-grid pmc-form-grid-2"
                        style={{ marginBottom: "16px" }}
                      >
                        <div
                          style={{
                            background: "white",
                            padding: "12px",
                            borderRadius: "6px",
                            border: "1px solid #fde68a",
                          }}
                        >
                          <label
                            className="pmc-label"
                            style={{ color: "#92400e", marginBottom: "4px" }}
                          >
                            Payment Amount
                          </label>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "24px",
                              fontWeight: 700,
                              color: "#78350f",
                            }}
                          >
                            ₹
                            {application?.challanAmount?.toLocaleString(
                              "en-IN"
                            ) || "0"}
                          </p>
                        </div>
                        <div
                          style={{
                            background: "white",
                            padding: "12px",
                            borderRadius: "6px",
                            border: "1px solid #fde68a",
                          }}
                        >
                          <label
                            className="pmc-label"
                            style={{ color: "#92400e", marginBottom: "4px" }}
                          >
                            Payment Gateway
                          </label>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "16px",
                              fontWeight: 600,
                              color: "#78350f",
                            }}
                          >
                            BillDesk (Secure)
                          </p>
                        </div>
                      </div>

                      <div
                        style={{
                          background: "white",
                          padding: "12px",
                          borderRadius: "6px",
                          border: "1px solid #fde68a",
                        }}
                      >
                        <p
                          style={{
                            margin: 0,
                            fontSize: "13px",
                            color: "#92400e",
                            display: "flex",
                            alignItems: "center",
                            gap: "6px",
                          }}
                        >
                          <Info size={16} />
                          <strong>Next Step:</strong> After successful payment,
                          your application will be automatically forwarded to
                          the Clerk for verification and further processing.
                        </p>
                      </div>
                    </div>

                    {/* Payment Button */}
                    <PaymentButton
                      applicationId={application.id}
                      applicationStatus={application.status}
                      isPaymentComplete={application.isPaymentComplete || false}
                      challanAmount={application.challanAmount}
                      onPaymentInitiated={() => {
                        console.log(
                          "Payment initiated for application:",
                          application.id
                        );
                      }}
                      onPaymentSuccess={async () => {
                        // Refresh application data after payment success
                        if (id) {
                          const response =
                            await positionRegistrationService.getApplication(
                              parseInt(id)
                            );
                          setApplication(response);
                        }
                      }}
                    />
                  </>
                ) : (
                  <>
                    {/* Payment Completed - Show Transaction Details */}
                    <div
                      style={{
                        background:
                          "linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%)",
                        padding: "20px",
                        borderRadius: "8px",
                        border: "2px solid #10b981",
                        marginBottom: "20px",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "12px",
                          marginBottom: "16px",
                        }}
                      >
                        <div
                          style={{
                            width: "48px",
                            height: "48px",
                            borderRadius: "50%",
                            background: "#10b981",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            color: "white",
                          }}
                        >
                          <CheckCircle size={28} />
                        </div>
                        <div>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "18px",
                              fontWeight: 700,
                              color: "#065f46",
                              marginBottom: "2px",
                            }}
                          >
                            Payment Completed Successfully
                          </p>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "14px",
                              color: "#059669",
                            }}
                          >
                            Your application has been forwarded to the Clerk for
                            verification
                          </p>
                        </div>
                      </div>

                      {/* Transaction & Challan Details Grid */}
                      <div
                        className="pmc-form-grid pmc-form-grid-3"
                        style={{ marginBottom: "16px" }}
                      >
                        <div
                          style={{
                            background: "white",
                            padding: "14px",
                            borderRadius: "6px",
                            border: "1px solid #bbf7d0",
                          }}
                        >
                          <label
                            className="pmc-label"
                            style={{ color: "#065f46", marginBottom: "4px" }}
                          >
                            Payment Amount
                          </label>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "20px",
                              fontWeight: 700,
                              color: "#047857",
                            }}
                          >
                            ₹
                            {application?.challanAmount?.toLocaleString(
                              "en-IN"
                            ) || "0"}
                          </p>
                        </div>

                        <div
                          style={{
                            background: "white",
                            padding: "14px",
                            borderRadius: "6px",
                            border: "1px solid #bbf7d0",
                          }}
                        >
                          <label
                            className="pmc-label"
                            style={{ color: "#065f46", marginBottom: "4px" }}
                          >
                            Payment Status
                          </label>
                          <p
                            style={{
                              margin: 0,
                              fontSize: "15px",
                              fontWeight: 700,
                              color: "#047857",
                              display: "flex",
                              alignItems: "center",
                              gap: "6px",
                            }}
                          >
                            <CheckCircle size={18} />
                            Success
                          </p>
                        </div>

                        {application.paymentCompletedDate && (
                          <div
                            style={{
                              background: "white",
                              padding: "14px",
                              borderRadius: "6px",
                              border: "1px solid #bbf7d0",
                            }}
                          >
                            <label
                              className="pmc-label"
                              style={{ color: "#065f46", marginBottom: "4px" }}
                            >
                              Completed On
                            </label>
                            <p
                              style={{
                                margin: 0,
                                fontSize: "13px",
                                fontWeight: 600,
                                color: "#047857",
                              }}
                            >
                              {new Date(
                                application.paymentCompletedDate
                              ).toLocaleString("en-IN", {
                                dateStyle: "medium",
                                timeStyle: "short",
                              })}
                            </p>
                          </div>
                        )}
                      </div>

                      {/* Action Buttons */}
                      <div style={{ display: "flex", gap: "12px" }}>
                        <button
                          className="pmc-button pmc-button-primary"
                          onClick={() => setShowPaymentModal(true)}
                          style={{
                            flex: 1,
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            gap: "8px",
                          }}
                        >
                          <Eye size={16} />
                          View Transaction Details
                        </button>

                        {/* Download Challan Button */}
                        <button
                          className="pmc-button pmc-button-success"
                          onClick={async () => {
                            try {
                              // Find the challan document in the documents array
                              const challanDoc = application.documents.find(
                                (doc) =>
                                  doc.documentTypeName === "PaymentChallan"
                              );

                              if (challanDoc && challanDoc.fileBase64) {
                                // Download from base64 data
                                const byteCharacters = atob(
                                  challanDoc.fileBase64
                                );
                                const byteNumbers = new Array(
                                  byteCharacters.length
                                );
                                for (
                                  let i = 0;
                                  i < byteCharacters.length;
                                  i++
                                ) {
                                  byteNumbers[i] = byteCharacters.charCodeAt(i);
                                }
                                const byteArray = new Uint8Array(byteNumbers);
                                const blob = new Blob([byteArray], {
                                  type: "application/pdf",
                                });
                                const url = URL.createObjectURL(blob);
                                const link = document.createElement("a");
                                link.href = url;
                                link.download = challanDoc.fileName;
                                document.body.appendChild(link);
                                link.click();
                                document.body.removeChild(link);
                                URL.revokeObjectURL(url);
                              } else {
                                setNotification({
                                  isOpen: true,
                                  message:
                                    "Challan not found. Please contact support.",
                                  type: "error",
                                  title: "Download Failed",
                                  autoClose: false,
                                });
                              }
                            } catch (error) {
                              console.error(
                                "Error downloading challan:",
                                error
                              );
                              setNotification({
                                isOpen: true,
                                message:
                                  "Failed to download challan. Please try again.",
                                type: "error",
                                title: "Download Failed",
                                autoClose: false,
                              });
                            }
                          }}
                          style={{
                            flex: 1,
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            gap: "8px",
                            background:
                              "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                          }}
                        >
                          <Download size={16} />
                          Download Challan
                        </button>
                      </div>
                    </div>
                  </>
                )}
              </div>
            </div>
          )}

        {/* Application Timeline */}
        <div className="pmc-card">
          <div
            className="pmc-card-header"
            style={{
              background: "linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%)",
              color: "#334155",
              padding: "12px 16px",
              borderBottom: "2px solid #cbd5e1",
            }}
          >
            <h2
              className="pmc-card-title"
              style={{ color: "#334155", margin: 0 }}
            >
              Application Timeline
            </h2>
          </div>
          <div className="pmc-card-body">
            <div className="pmc-form-grid pmc-form-grid-3">
              <div>
                <label className="pmc-label">Created Date</label>
                <p className="pmc-value">
                  {new Date(application.createdDate).toLocaleString()}
                </p>
              </div>
              {application.submittedDate && (
                <div>
                  <label className="pmc-label">Submitted Date</label>
                  <p className="pmc-value">
                    {new Date(application.submittedDate).toLocaleString()}
                  </p>
                </div>
              )}
              {application.approvedDate && (
                <div>
                  <label className="pmc-label">Approved Date</label>
                  <p className="pmc-value">
                    {new Date(application.approvedDate).toLocaleString()}
                  </p>
                </div>
              )}
            </div>
            {application.remarks && (
              <div style={{ marginTop: "16px" }}>
                <label className="pmc-label">Remarks</label>
                <p className="pmc-value">{application.remarks}</p>
              </div>
            )}
          </div>
        </div>

        {/* Appointment Details Card */}
        {application.workflowInfo?.hasAppointment && (
          <div
            className="pmc-card"
            style={{
              marginTop: "24px",
              border: "2px solid #3b82f6",
              boxShadow: "0 2px 8px rgba(59, 130, 246, 0.1)",
            }}
          >
            <div
              className="pmc-card-header"
              style={{
                background: "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                color: "white",
                padding: "12px 16px",
                borderBottom: "2px solid #2563eb",
              }}
            >
              <h2
                className="pmc-card-title"
                style={{
                  color: "white",
                  margin: 0,
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <Calendar size={20} />
                Scheduled Appointment Details
              </h2>
            </div>
            <div className="pmc-card-body">
              <div className="pmc-form-grid pmc-form-grid-2">
                {application.workflowInfo.appointmentDate && (
                  <div>
                    <label className="pmc-label">Appointment Date & Time</label>
                    <p
                      className="pmc-value"
                      style={{ fontWeight: 600, color: "#1e40af" }}
                    >
                      {parseLocalDateTime(
                        application.workflowInfo.appointmentDate
                      ).toLocaleString("en-IN", {
                        dateStyle: "full",
                        timeStyle: "short",
                      })}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentPlace && (
                  <div>
                    <label className="pmc-label">Location</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentPlace}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentRoomNumber && (
                  <div>
                    <label className="pmc-label">Room Number</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentRoomNumber}
                    </p>
                  </div>
                )}
                {application.workflowInfo.appointmentContactPerson && (
                  <div>
                    <label className="pmc-label">Contact Person</label>
                    <p className="pmc-value">
                      {application.workflowInfo.appointmentContactPerson}
                    </p>
                  </div>
                )}
              </div>
              {application.workflowInfo.appointmentComments && (
                <div style={{ marginTop: "16px" }}>
                  <label className="pmc-label">Additional Instructions</label>
                  <p
                    className="pmc-value"
                    style={{
                      backgroundColor: "#f0f9ff",
                      padding: "12px",
                      borderRadius: "6px",
                      border: "1px solid #bae6fd",
                    }}
                  >
                    {application.workflowInfo.appointmentComments}
                  </p>
                </div>
              )}

              {/* Reschedule Button for Junior Engineers */}
              {isJEOfficer && (
                <div style={{ marginTop: "16px", textAlign: "right" }}>
                  <button
                    className="pmc-button pmc-button-secondary"
                    onClick={() => {
                      const currentDate = application.workflowInfo
                        ?.appointmentDate
                        ? new Date(
                            application.workflowInfo.appointmentDate
                          ).toISOString()
                        : "";
                      setRescheduleForm({
                        newReviewDate: currentDate,
                        rescheduleReason: "",
                        place: application.workflowInfo?.appointmentPlace || "",
                        contactPerson:
                          application.workflowInfo?.appointmentContactPerson ||
                          "",
                        roomNumber:
                          application.workflowInfo?.appointmentRoomNumber || "",
                      });
                      setShowRescheduleModal(true);
                    }}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "8px",
                      marginLeft: "auto",
                    }}
                  >
                    <Calendar size={18} />
                    Reschedule Appointment
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Action Buttons for JE Officers */}
        {isJEOfficer && (
          <div
            style={{
              marginTop: "24px",
              display: "flex",
              gap: "12px",
              justifyContent: "flex-end",
            }}
          >
            <button
              className="pmc-button pmc-button-secondary"
              title="Back to Dashboard"
              onClick={() => navigate(getDashboardRoute())}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#1e40af",
                backgroundColor: "#fff",
                border: "1px solid #cbd5e1",
              }}
            >
              <ArrowLeft size={18} />
              Back to Dashboard
            </button>
            <button
              className="pmc-button pmc-button-danger"
              title="Reject Application"
              onClick={openRejectModal}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#fff",
                backgroundColor: "#dc2626",
              }}
            >
              <Ban size={18} />
              Reject Application
            </button>
            {application.workflowInfo?.currentStage ===
            "Appointment Scheduled" ? (
              <button
                className="pmc-button pmc-button-success"
                title="Document Approve"
                onClick={() => setShowDocumentApprovalModal(true)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  color: "#fff",
                  backgroundColor: "#16a34a",
                }}
              >
                <CheckCircle size={18} />
                Document Approve
              </button>
            ) : (
              <button
                className="pmc-button pmc-button-success"
                title="Schedule Appointment"
                onClick={handleScheduleAppointment}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                  color: "#fff",
                  backgroundColor: "#16a34a",
                }}
              >
                <Calendar size={18} />
                Schedule Appointment
              </button>
            )}
          </div>
        )}

        {/* Action Buttons for AE, EE, CE Officers */}
        {(user?.role.includes("Assistant") ||
          user?.role.includes("Executive") ||
          user?.role.includes("City")) && (
          <div
            style={{
              marginTop: "24px",
              display: "flex",
              gap: "12px",
              justifyContent: "flex-end",
            }}
          >
            <button
              className="pmc-button pmc-button-secondary"
              title="Back to Dashboard"
              onClick={() => navigate(getDashboardRoute())}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#1e40af",
                backgroundColor: "#fff",
                border: "1px solid #cbd5e1",
              }}
            >
              <ArrowLeft size={18} />
              Back to Dashboard
            </button>
            {/* Reject button hidden for AE/EE/CE officers as per requirement */}
            <button
              className="pmc-button pmc-button-success"
              title="Verify & Approve Documents"
              onClick={handleVerifyDocuments}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#fff",
                backgroundColor: "#16a34a",
              }}
            >
              <CheckCircle size={18} />
              Verify & Approve
            </button>
          </div>
        )}

        {/* Action Buttons for Clerk */}
        {user?.role.includes("Clerk") && (
          <div
            style={{
              marginTop: "24px",
              display: "flex",
              gap: "12px",
              justifyContent: "flex-end",
            }}
          >
            <button
              className="pmc-button pmc-button-secondary"
              title="Back to Dashboard"
              onClick={() => navigate(getDashboardRoute())}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#1e40af",
                backgroundColor: "#fff",
                border: "1px solid #cbd5e1",
              }}
            >
              <ArrowLeft size={18} />
              Back to Dashboard
            </button>
            {/* Clerk cannot reject applications - only Stage 1 officers can reject */}
            <button
              className="pmc-button pmc-button-success"
              title="Approve Application"
              onClick={handleClerkApprove}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#fff",
                backgroundColor: "#16a34a",
              }}
            >
              <CheckCircle size={18} />
              Approve
            </button>
          </div>
        )}

        {/* Clerk Approval Modal */}
        {showClerkApprovalModal && (
          <div
            onClick={() => setShowClerkApprovalModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0,0,0,0.5)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
            }}
          >
            <div
              className="pmc-modal pmc-slideInUp"
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "12px",
                maxWidth: "520px",
                width: "100%",
                boxShadow:
                  "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)",
              }}
            >
              {/* Modal Header */}
              <div
                style={{
                  padding: "24px 24px 20px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  borderRadius: "12px 12px 0 0",
                }}
              >
                <div
                  style={{ display: "flex", alignItems: "center", gap: "12px" }}
                >
                  <div
                    style={{
                      width: "48px",
                      height: "48px",
                      borderRadius: "50%",
                      background: "rgba(255, 255, 255, 0.2)",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                    }}
                  >
                    <CheckCircle size={28} style={{ color: "white" }} />
                  </div>
                  <div>
                    <h3
                      style={{
                        color: "white",
                        margin: 0,
                        fontSize: "20px",
                        fontWeight: "600",
                      }}
                    >
                      Approve Application
                    </h3>
                    <p
                      style={{
                        color: "rgba(255,255,255,0.9)",
                        fontSize: "13px",
                        margin: "4px 0 0 0",
                      }}
                    >
                      Forward to Executive Engineer (Stage 2)
                    </p>
                  </div>
                </div>
              </div>

              {/* Modal Body */}
              <div style={{ padding: "24px" }}>
                <div
                  style={{
                    marginBottom: "20px",
                    padding: "14px 16px",
                    background:
                      "linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)",
                    borderRadius: "8px",
                    border: "1px solid #3b82f6",
                  }}
                >
                  <div style={{ display: "flex", gap: "12px" }}>
                    <Info
                      size={20}
                      style={{
                        color: "#1e40af",
                        flexShrink: 0,
                        marginTop: "2px",
                      }}
                    />
                    <div>
                      <p
                        style={{
                          margin: 0,
                          fontSize: "14px",
                          color: "#1e3a8a",
                          lineHeight: "1.5",
                        }}
                      >
                        <strong>
                          Application #{application?.applicationNumber}
                        </strong>
                      </p>
                      <p
                        style={{
                          margin: "4px 0 0 0",
                          fontSize: "13px",
                          color: "#1e40af",
                          lineHeight: "1.5",
                        }}
                      >
                        This application will be forwarded to Executive Engineer
                        for certificate signature (Stage 2).
                      </p>
                    </div>
                  </div>
                </div>

                <div style={{ marginBottom: "4px" }}>
                  <label
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Remarks / Comments{" "}
                    <span style={{ color: "#9ca3af" }}>(Optional)</span>
                  </label>
                  <textarea
                    placeholder="Add any remarks or comments about this approval..."
                    value={clerkRemarks}
                    onChange={(e) => setClerkRemarks(e.target.value)}
                    rows={4}
                    style={{
                      width: "100%",
                      padding: "12px 14px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "8px",
                      fontSize: "14px",
                      fontFamily: "inherit",
                      resize: "vertical",
                      outline: "none",
                      transition: "all 0.2s",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>
              </div>

              {/* Modal Footer */}
              <div
                style={{
                  padding: "16px 24px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                  borderRadius: "0 0 12px 12px",
                }}
              >
                <button
                  onClick={() => setShowClerkApprovalModal(false)}
                  disabled={isApprovingClerk}
                  style={{
                    padding: "10px 20px",
                    fontSize: "14px",
                    fontWeight: 600,
                    background: "white",
                    color: "#374151",
                    border: "1.5px solid #d1d5db",
                    borderRadius: "8px",
                    cursor: isApprovingClerk ? "not-allowed" : "pointer",
                    opacity: isApprovingClerk ? 0.6 : 1,
                    transition: "all 0.2s",
                  }}
                >
                  Cancel
                </button>
                <button
                  onClick={handleClerkApprovalSubmit}
                  disabled={isApprovingClerk}
                  style={{
                    padding: "10px 24px",
                    fontSize: "14px",
                    fontWeight: 600,
                    background: isApprovingClerk
                      ? "#9ca3af"
                      : "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                    color: "white",
                    border: "none",
                    borderRadius: "8px",
                    cursor: isApprovingClerk ? "not-allowed" : "pointer",
                    transition: "all 0.2s",
                  }}
                >
                  <CheckCircle
                    size={18}
                    style={{ display: "inline", marginRight: "6px" }}
                  />
                  Approve & Forward
                </button>
              </div>

              {/* Spinner Animation */}
              <style>
                {`
                  @keyframes spin {
                    to { transform: rotate(360deg); }
                  }
                `}
              </style>
            </div>
          </div>
        )}

        {/* Schedule Appointment Modal */}
        {showScheduleModal && (
          <div
            onClick={() => setShowScheduleModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0,0,0,0.6)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
              backdropFilter: "blur(4px)",
            }}
          >
            <div
              className="pmc-modal pmc-slideInUp"
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "12px",
                maxWidth: "900px",
                width: "100%",
                boxShadow:
                  "0 25px 50px -12px rgba(0, 0, 0, 0.25), 0 0 0 1px rgba(0, 0, 0, 0.05)",
                position: "relative",
                maxHeight: "90vh",
                display: "flex",
                flexDirection: "column",
              }}
            >
              {/* Header */}
              <div
                className="pmc-modal-header"
                style={{
                  padding: "20px 24px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  borderRadius: "12px 12px 0 0",
                  flexShrink: 0,
                }}
              >
                <h3
                  style={{
                    color: "white",
                    marginBottom: "4px",
                    fontSize: "20px",
                    fontWeight: "700",
                    letterSpacing: "-0.01em",
                  }}
                >
                  Schedule Appointment
                </h3>
                <p
                  style={{
                    color: "rgba(255,255,255,0.95)",
                    fontSize: "14px",
                    margin: 0,
                    fontWeight: "500",
                  }}
                >
                  Application: {application.applicationNumber}
                </p>
              </div>

              {/* Error Message */}
              {scheduleError && (
                <div
                  style={{
                    margin: "20px 24px 0",
                    padding: "14px 16px",
                    background: "#fef2f2",
                    border: "1px solid #fecaca",
                    borderRadius: "8px",
                    color: "#991b1b",
                    fontSize: "14px",
                    fontWeight: "500",
                    display: "flex",
                    alignItems: "center",
                    gap: "10px",
                  }}
                >
                  <span style={{ fontSize: "18px" }}>⚠️</span>
                  {scheduleError}
                </div>
              )}

              {/* Modal Body - No Scroll */}
              <div
                className="pmc-modal-body"
                style={{
                  padding: "24px",
                  flexGrow: 1,
                  overflow: "visible",
                }}
              >
                {/* Date/Time and Form Fields Container */}
                <div style={{ marginBottom: "20px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Select Date & Time{" "}
                    <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <DateTimePicker
                    value={scheduleForm.reviewDate}
                    onChange={(value) =>
                      setScheduleForm({
                        ...scheduleForm,
                        reviewDate: value,
                      })
                    }
                    minDate={new Date()}
                  />
                </div>

                {/* Form Fields Grid */}
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 1fr",
                    gap: "16px",
                    marginBottom: "20px",
                  }}
                >
                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: 600,
                        fontSize: "14px",
                        color: "#374151",
                      }}
                    >
                      Contact Person <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Enter contact person name"
                      value={scheduleForm.contactPerson}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          contactPerson: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "12px 14px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "8px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>

                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: 600,
                        fontSize: "14px",
                        color: "#374151",
                      }}
                    >
                      Room Number <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Enter room number"
                      value={scheduleForm.roomNumber}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          roomNumber: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "12px 14px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "8px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>
                </div>

                <div style={{ marginBottom: "20px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Place <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="Enter location/place"
                    value={scheduleForm.place}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        place: e.target.value,
                      })
                    }
                    style={{
                      width: "100%",
                      padding: "12px 14px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "8px",
                      fontSize: "14px",
                      outline: "none",
                      transition: "all 0.2s",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>

                <div>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Comments
                  </label>
                  <textarea
                    placeholder="Additional comments or instructions (optional)"
                    value={scheduleForm.comments}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        comments: e.target.value,
                      })
                    }
                    rows={3}
                    style={{
                      width: "100%",
                      padding: "12px 14px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "8px",
                      fontSize: "14px",
                      resize: "vertical",
                      outline: "none",
                      transition: "all 0.2s",
                      fontFamily: "inherit",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>
              </div>

              {/* Footer */}
              <div
                className="pmc-modal-footer"
                style={{
                  padding: "16px 24px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                  borderRadius: "0 0 12px 12px",
                  flexShrink: 0,
                }}
              >
                <button
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowScheduleModal(false)}
                  disabled={isScheduling}
                  style={{
                    padding: "10px 24px",
                    fontSize: "14px",
                    fontWeight: "600",
                    borderRadius: "8px",
                  }}
                >
                  Cancel
                </button>
                <button
                  className="pmc-button pmc-button-success"
                  onClick={handleSubmitSchedule}
                  disabled={isScheduling}
                  style={{
                    padding: "10px 28px",
                    fontSize: "14px",
                    fontWeight: "600",
                    borderRadius: "8px",
                    boxShadow: "0 2px 8px rgba(16, 185, 129, 0.25)",
                  }}
                >
                  Schedule Appointment
                </button>
              </div>

              {/* Modal Loader Overlay */}
              <ModalLoader
                isVisible={isScheduling}
                message="Scheduling appointment..."
              />
            </div>
          </div>
        )}

        {/* Reschedule Appointment Modal */}
        {showRescheduleModal && (
          <div
            onClick={() => setShowRescheduleModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0,0,0,0.6)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
              backdropFilter: "blur(4px)",
            }}
          >
            <div
              className="pmc-modal pmc-slideInUp"
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "12px",
                maxWidth: "900px",
                width: "100%",
                boxShadow:
                  "0 25px 50px -12px rgba(0, 0, 0, 0.25), 0 0 0 1px rgba(0, 0, 0, 0.05)",
                position: "relative",
                maxHeight: "90vh",
                display: "flex",
                flexDirection: "column",
              }}
            >
              {/* Header */}
              <div
                className="pmc-modal-header"
                style={{
                  padding: "20px 24px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  borderRadius: "12px 12px 0 0",
                  flexShrink: 0,
                }}
              >
                <h3
                  style={{
                    color: "white",
                    marginBottom: "4px",
                    fontSize: "20px",
                    fontWeight: "700",
                    letterSpacing: "-0.01em",
                  }}
                >
                  Reschedule Appointment
                </h3>
                <p
                  style={{
                    color: "rgba(255,255,255,0.95)",
                    fontSize: "14px",
                    margin: 0,
                    fontWeight: "500",
                  }}
                >
                  Application: {application.applicationNumber}
                </p>
              </div>

              {/* Error Message */}
              {rescheduleError && (
                <div
                  style={{
                    margin: "20px 24px 0",
                    padding: "14px 16px",
                    background: "#fef2f2",
                    border: "1px solid #fecaca",
                    borderRadius: "8px",
                    color: "#991b1b",
                    fontSize: "14px",
                    fontWeight: "500",
                    display: "flex",
                    alignItems: "center",
                    gap: "10px",
                  }}
                >
                  <span style={{ fontSize: "18px" }}>⚠️</span>
                  {rescheduleError}
                </div>
              )}

              {/* Modal Body - No Scroll */}
              <div
                className="pmc-modal-body"
                style={{
                  padding: "24px",
                  flexGrow: 1,
                  overflow: "visible",
                }}
              >
                {/* Date/Time Picker */}
                <div style={{ marginBottom: "20px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    New Review Date <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <DateTimePicker
                    value={rescheduleForm.newReviewDate}
                    onChange={(value) =>
                      setRescheduleForm({
                        ...rescheduleForm,
                        newReviewDate: value,
                      })
                    }
                    minDate={new Date()}
                  />
                </div>

                {/* Form Fields Grid */}
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 1fr",
                    gap: "16px",
                    marginBottom: "20px",
                  }}
                >
                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: 600,
                        fontSize: "14px",
                        color: "#374151",
                      }}
                    >
                      Contact Person <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Enter contact person name"
                      value={rescheduleForm.contactPerson}
                      onChange={(e) =>
                        setRescheduleForm({
                          ...rescheduleForm,
                          contactPerson: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "12px 14px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "8px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>

                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "8px",
                        fontWeight: 600,
                        fontSize: "14px",
                        color: "#374151",
                      }}
                    >
                      Room Number <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Enter room number"
                      value={rescheduleForm.roomNumber}
                      onChange={(e) =>
                        setRescheduleForm({
                          ...rescheduleForm,
                          roomNumber: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "12px 14px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "8px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>
                </div>

                <div style={{ marginBottom: "20px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Place <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="Enter location/place"
                    value={rescheduleForm.place}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
                        place: e.target.value,
                      })
                    }
                    style={{
                      width: "100%",
                      padding: "12px 14px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "8px",
                      fontSize: "14px",
                      outline: "none",
                      transition: "all 0.2s",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>

                <div>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "8px",
                      fontWeight: 600,
                      fontSize: "14px",
                      color: "#374151",
                    }}
                  >
                    Reschedule Reason{" "}
                    <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <textarea
                    placeholder="Enter reason for rescheduling"
                    value={rescheduleForm.rescheduleReason}
                    onChange={(e) =>
                      setRescheduleForm({
                        ...rescheduleForm,
                        rescheduleReason: e.target.value,
                      })
                    }
                    rows={3}
                    style={{
                      width: "100%",
                      padding: "12px 14px",
                      border: `1.5px solid ${
                        rescheduleError && !rescheduleForm.rescheduleReason
                          ? "#ef4444"
                          : "#d1d5db"
                      }`,
                      borderRadius: "8px",
                      fontSize: "14px",
                      resize: "vertical",
                      outline: "none",
                      transition: "all 0.2s",
                      fontFamily: "inherit",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor =
                        rescheduleError && !rescheduleForm.rescheduleReason
                          ? "#ef4444"
                          : "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                  {rescheduleError && !rescheduleForm.rescheduleReason && (
                    <p
                      style={{
                        marginTop: "6px",
                        fontSize: "13px",
                        color: "#ef4444",
                        fontWeight: 500,
                      }}
                    >
                      Reschedule reason is required
                    </p>
                  )}
                </div>
              </div>

              {/* Footer */}
              <div
                className="pmc-modal-footer"
                style={{
                  padding: "16px 24px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "12px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                  borderRadius: "0 0 12px 12px",
                  flexShrink: 0,
                }}
              >
                <button
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowRescheduleModal(false)}
                  style={{
                    padding: "10px 24px",
                    fontSize: "14px",
                    fontWeight: "600",
                    borderRadius: "8px",
                  }}
                  disabled={isRescheduling}
                >
                  Cancel
                </button>
                <button
                  className="pmc-button pmc-button-success"
                  onClick={handleRescheduleSubmit}
                  disabled={isRescheduling}
                  style={{
                    padding: "10px 28px",
                    fontSize: "14px",
                    fontWeight: "600",
                    borderRadius: "8px",
                    boxShadow: "0 2px 8px rgba(16, 185, 129, 0.25)",
                  }}
                >
                  Reschedule Appointment
                </button>
              </div>

              {/* Modal Loader Overlay */}
              <ModalLoader
                isVisible={isRescheduling}
                message="Rescheduling appointment..."
              />
            </div>
          </div>
        )}

        {/* Success Popup */}
        {showSuccessPopup && (
          <div
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundColor: "rgba(0, 0, 0, 0.7)",
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
              zIndex: 10000,
              padding: "20px",
            }}
          >
            <div
              style={{
                backgroundColor: "white",
                borderRadius: "16px",
                maxWidth: "500px",
                width: "100%",
                padding: "40px",
                textAlign: "center",
                boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
              }}
            >
              <div
                style={{
                  width: "80px",
                  height: "80px",
                  borderRadius: "50%",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  flexShrink: 0,
                }}
              >
                <h3
                  style={{
                    color: "white",
                    marginBottom: "2px",
                    fontSize: "18px",
                    fontWeight: "600",
                  }}
                >
                  Schedule Appointment
                </h3>
                <p
                  style={{
                    color: "rgba(255,255,255,0.9)",
                    fontSize: "13px",
                    margin: 0,
                  }}
                >
                  Application: {application.applicationNumber}
                </p>
              </div>

              {/* Error Message */}
              {scheduleError && (
                <div
                  style={{
                    margin: "16px 20px 0",
                    padding: "12px 16px",
                    background:
                      "linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)",
                    border: "1.5px solid #ef4444",
                    borderRadius: "6px",
                    color: "#991b1b",
                    fontSize: "13px",
                    fontWeight: "500",
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  <span style={{ fontSize: "16px" }}>⚠️</span>
                  {scheduleError}
                </div>
              )}

              <div
                className="pmc-modal-body"
                style={{
                  padding: "20px",
                  overflowY: "auto",
                  flexGrow: 1,
                }}
              >
                <div style={{ marginBottom: "16px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Review Date <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <DateTimePicker
                    value={scheduleForm.reviewDate}
                    onChange={(value) =>
                      setScheduleForm({
                        ...scheduleForm,
                        reviewDate: value,
                      })
                    }
                    minDate={new Date()}
                  />
                </div>

                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns: "1fr 1fr",
                    gap: "12px",
                    marginBottom: "16px",
                  }}
                >
                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "6px",
                        fontWeight: 500,
                        fontSize: "13px",
                        color: "#374151",
                      }}
                    >
                      Contact Person <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Contact Person"
                      value={scheduleForm.contactPerson}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          contactPerson: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "10px 12px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>

                  <div>
                    <label
                      className="pmc-label"
                      style={{
                        display: "block",
                        marginBottom: "6px",
                        fontWeight: 500,
                        fontSize: "13px",
                        color: "#374151",
                      }}
                    >
                      Room Number <span style={{ color: "#dc2626" }}>*</span>
                    </label>
                    <input
                      type="text"
                      placeholder="Room Number"
                      value={scheduleForm.roomNumber}
                      onChange={(e) =>
                        setScheduleForm({
                          ...scheduleForm,
                          roomNumber: e.target.value,
                        })
                      }
                      style={{
                        width: "100%",
                        padding: "10px 12px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "14px",
                        outline: "none",
                        transition: "all 0.2s",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                        e.target.style.boxShadow =
                          "0 0 0 3px rgba(16, 185, 129, 0.1)";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                        e.target.style.boxShadow = "none";
                      }}
                    />
                  </div>
                </div>

                <div style={{ marginBottom: "16px" }}>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Place <span style={{ color: "#dc2626" }}>*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="Place"
                    value={scheduleForm.place}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        place: e.target.value,
                      })
                    }
                    style={{
                      width: "100%",
                      padding: "10px 12px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                      outline: "none",
                      transition: "all 0.2s",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>

                <div>
                  <label
                    className="pmc-label"
                    style={{
                      display: "block",
                      marginBottom: "6px",
                      fontWeight: 500,
                      fontSize: "13px",
                      color: "#374151",
                    }}
                  >
                    Comments
                  </label>
                  <textarea
                    placeholder="Additional comments or instructions"
                    value={scheduleForm.comments}
                    onChange={(e) =>
                      setScheduleForm({
                        ...scheduleForm,
                        comments: e.target.value,
                      })
                    }
                    rows={3}
                    style={{
                      width: "100%",
                      padding: "10px 12px",
                      border: "1.5px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                      resize: "vertical",
                      outline: "none",
                      transition: "all 0.2s",
                      fontFamily: "inherit",
                    }}
                    onFocus={(e) => {
                      e.target.style.borderColor = "#10b981";
                      e.target.style.boxShadow =
                        "0 0 0 3px rgba(16, 185, 129, 0.1)";
                    }}
                    onBlur={(e) => {
                      e.target.style.borderColor = "#d1d5db";
                      e.target.style.boxShadow = "none";
                    }}
                  />
                </div>
              </div>

              <div
                className="pmc-modal-footer"
                style={{
                  padding: "12px 20px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "10px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                  flexShrink: 0,
                }}
              >
                <button
                  className="pmc-button pmc-button-secondary"
                  onClick={() => setShowScheduleModal(false)}
                  disabled={isScheduling}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                  }}
                >
                  Cancel
                </button>
                <button
                  className="pmc-button pmc-button-success"
                  onClick={handleSubmitSchedule}
                  disabled={isScheduling}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                  }}
                >
                  Schedule Appointment
                </button>
              </div>

              {/* Modal Loader Overlay */}
              <ModalLoader
                isVisible={isScheduling}
                message="Scheduling appointment..."
              />
            </div>
          </div>
        )}

        {/* Success Popup */}
        {showSuccessPopup && (
          <div
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundColor: "rgba(0, 0, 0, 0.7)",
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
              zIndex: 10000,
              padding: "20px",
            }}
          >
            <div
              style={{
                backgroundColor: "white",
                borderRadius: "16px",
                maxWidth: "500px",
                width: "100%",
                padding: "40px",
                textAlign: "center",
                boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
              }}
            >
              <div
                style={{
                  width: "80px",
                  height: "80px",
                  borderRadius: "50%",
                  background:
                    "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                  margin: "0 auto 24px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  fontSize: "40px",
                }}
              >
                ✓
              </div>
              <h2
                style={{
                  fontSize: "28px",
                  fontWeight: "700",
                  color: "#10b981",
                  marginBottom: "16px",
                }}
              >
                Appointment Scheduled Successfully!
              </h2>
              <p
                style={{
                  fontSize: "16px",
                  color: "#64748b",
                  marginBottom: "24px",
                }}
              >
                The appointment has been scheduled and the applicant will be
                notified.
              </p>
              <div
                style={{
                  width: "40px",
                  height: "40px",
                  border: "4px solid #10b981",
                  borderTopColor: "transparent",
                  borderRadius: "50%",
                  margin: "0 auto",
                  animation: "spin 1s linear infinite",
                }}
              />
              <p
                style={{
                  marginTop: "16px",
                  fontSize: "14px",
                  color: "#64748b",
                }}
              >
                Redirecting to dashboard...
              </p>
              <style>
                {`
                @keyframes spin {
                  to { transform: rotate(360deg); }
                }
              `}
              </style>
            </div>
          </div>
        )}

        {/* Document Approval Modal */}
        <DocumentApprovalModal
          isOpen={showDocumentApprovalModal}
          onClose={() => setShowDocumentApprovalModal(false)}
          applicationId={application.id}
          documents={application.documents.map((doc) => ({
            id: doc.id,
            documentTypeName: doc.documentTypeName,
            fileName: doc.fileName,
            fileSize: doc.fileSize,
            isVerified: doc.isVerified,
          }))}
          onApprovalComplete={handleDocumentApprovalComplete}
        />

        {/* Document Preview Modal */}
        {selectedDocument && (
          <div
            className="pmc-modal-overlay"
            onClick={() => setSelectedDocument(null)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0, 0, 0, 0.8)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
            }}
          >
            <div
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "8px",
                width: "90%",
                maxWidth: "1200px",
                height: "90vh",
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
              }}
            >
              {/* Header */}
              <div
                style={{
                  padding: "16px 20px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                }}
              >
                <div>
                  <h3
                    style={{
                      color: "white",
                      margin: 0,
                      fontSize: "18px",
                      fontWeight: "600",
                    }}
                  >
                    {selectedDocument.documentTypeName}
                  </h3>
                  <p
                    style={{
                      color: "rgba(255,255,255,0.9)",
                      fontSize: "13px",
                      margin: "4px 0 0 0",
                    }}
                  >
                    {selectedDocument.fileName}
                  </p>
                </div>
                <div
                  style={{ display: "flex", gap: "8px", alignItems: "center" }}
                >
                  <button
                    className="pmc-button pmc-button-sm"
                    onClick={() => {
                      const base64Data =
                        selectedDocument.pdfBase64 ||
                        selectedDocument.fileBase64;

                      if (base64Data) {
                        // Download from base64 data
                        const byteCharacters = atob(base64Data);
                        const byteNumbers = new Array(byteCharacters.length);
                        for (let i = 0; i < byteCharacters.length; i++) {
                          byteNumbers[i] = byteCharacters.charCodeAt(i);
                        }
                        const byteArray = new Uint8Array(byteNumbers);

                        // Determine content type
                        let contentType = "application/pdf";
                        const fileName =
                          selectedDocument.fileName.toLowerCase();
                        if (
                          fileName.endsWith(".jpg") ||
                          fileName.endsWith(".jpeg")
                        ) {
                          contentType = "image/jpeg";
                        } else if (fileName.endsWith(".png")) {
                          contentType = "image/png";
                        } else if (fileName.endsWith(".gif")) {
                          contentType = "image/gif";
                        } else if (fileName.endsWith(".webp")) {
                          contentType = "image/webp";
                        }

                        const blob = new Blob([byteArray], {
                          type: contentType,
                        });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download = selectedDocument.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        URL.revokeObjectURL(url);
                      } else {
                        // No binary data available - this should not happen as all documents are stored in database
                        console.error(
                          "Document binary data not available:",
                          selectedDocument.fileName
                        );
                        setNotification({
                          isOpen: true,
                          message:
                            "Document data not available. Please contact support.",
                          type: "error",
                          title: "Download Failed",
                        });
                      }
                    }}
                    style={{
                      background: "rgba(255,255,255,0.2)",
                      color: "white",
                      border: "1px solid rgba(255,255,255,0.3)",
                      display: "flex",
                      alignItems: "center",
                      gap: "6px",
                    }}
                  >
                    <Download size={14} />
                    Download
                  </button>
                  <button
                    onClick={() => setSelectedDocument(null)}
                    style={{
                      background: "transparent",
                      border: "none",
                      cursor: "pointer",
                      padding: "4px",
                      color: "white",
                    }}
                  >
                    <X size={24} />
                  </button>
                </div>
              </div>

              {/* Document Preview */}
              <div
                style={{
                  flex: 1,
                  overflow: "auto",
                  background: "#f3f4f6",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {selectedDocument.fileName.toLowerCase().endsWith(".pdf") ? (
                  pdfBlobUrl ? (
                    <iframe
                      src={pdfBlobUrl}
                      style={{
                        width: "100%",
                        height: "100%",
                        border: "none",
                      }}
                      title={selectedDocument.fileName}
                    />
                  ) : (
                    <div
                      style={{
                        textAlign: "center",
                        padding: "40px",
                        color: "#dc2626",
                      }}
                    >
                      <FileText size={64} style={{ margin: "0 auto 16px" }} />
                      <p style={{ fontSize: "16px", marginBottom: "8px" }}>
                        Document data not available
                      </p>
                      <p style={{ fontSize: "14px", color: "#64748b" }}>
                        The document binary data could not be loaded. Please
                        contact support.
                      </p>
                    </div>
                  )
                ) : selectedDocument.fileName.match(
                    /\.(jpg|jpeg|png|gif|webp)$/i
                  ) ? (
                  pdfBlobUrl ? (
                    <img
                      src={pdfBlobUrl}
                      alt={selectedDocument.fileName}
                      style={{
                        maxWidth: "100%",
                        maxHeight: "100%",
                        objectFit: "contain",
                      }}
                    />
                  ) : (
                    <div
                      style={{
                        textAlign: "center",
                        padding: "40px",
                        color: "#dc2626",
                      }}
                    >
                      <FileText size={64} style={{ margin: "0 auto 16px" }} />
                      <p style={{ fontSize: "16px", marginBottom: "8px" }}>
                        Image data not available
                      </p>
                      <p style={{ fontSize: "14px", color: "#64748b" }}>
                        The image binary data could not be loaded. Please
                        contact support.
                      </p>
                    </div>
                  )
                ) : (
                  <div
                    style={{
                      textAlign: "center",
                      padding: "40px",
                      color: "#64748b",
                    }}
                  >
                    <FileText size={64} style={{ margin: "0 auto 16px" }} />
                    <p style={{ fontSize: "16px", marginBottom: "8px" }}>
                      Preview not available for this file type
                    </p>
                    <p style={{ fontSize: "14px", marginBottom: "16px" }}>
                      Click the download button in the header to save the file
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* OTP Verification Modal for AE, EE, CE Officers */}
        {application && (
          <OTPVerificationModal
            isOpen={showOTPModal}
            onClose={() => setShowOTPModal(false)}
            applicationId={application.id}
            officerType={getOfficerType()}
            onGenerateOtp={handleGenerateOtp}
            onVerifyAndSign={handleVerifyAndSign}
            onSuccess={handleOTPVerificationComplete}
          />
        )}

        {/* Payment Status Modal */}
        <PaymentStatusModal
          applicationId={application.id}
          isOpen={showPaymentModal}
          onClose={() => setShowPaymentModal(false)}
        />

        {/* Reject Modal */}
        {showRejectModal && application && (
          <div
            className="pmc-modal-overlay"
            onClick={() => setShowRejectModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0, 0, 0, 0.5)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              zIndex: 1000,
              padding: "20px",
            }}
          >
            <div
              className="pmc-modal pmc-slideInUp"
              onClick={(e) => e.stopPropagation()}
              style={{
                background: "white",
                borderRadius: "8px",
                maxWidth: "500px",
                width: "100%",
              }}
            >
              <div
                style={{
                  padding: "16px 20px",
                  borderBottom: "1px solid #e5e7eb",
                  background:
                    "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                }}
              >
                <h3 style={{ color: "white", margin: 0, fontSize: "17px" }}>
                  Reject Application
                </h3>
              </div>

              <div style={{ padding: "20px" }}>
                <p style={{ marginBottom: "12px", color: "#475569" }}>
                  <strong>Application:</strong> {application.applicationNumber}
                </p>
                <p style={{ marginBottom: "16px", color: "#475569" }}>
                  <strong>Applicant:</strong>{" "}
                  {`${application.firstName || ""} ${
                    application.lastName || ""
                  }`.trim()}
                </p>

                <label
                  style={{
                    display: "block",
                    marginBottom: "8px",
                    fontWeight: 600,
                    color: "#334155",
                  }}
                >
                  Rejection Comments <span style={{ color: "#dc2626" }}>*</span>
                </label>
                <textarea
                  placeholder="Enter detailed rejection comments (mandatory)"
                  value={rejectionComments}
                  onChange={(e) => setRejectionComments(e.target.value)}
                  style={{
                    width: "100%",
                    minHeight: "100px",
                    padding: "10px 12px",
                    border: "1px solid #d1d5db",
                    borderRadius: "6px",
                    fontSize: "14px",
                    fontFamily: "inherit",
                    resize: "vertical",
                  }}
                />
              </div>

              <div
                style={{
                  padding: "14px 20px",
                  borderTop: "1px solid #e5e7eb",
                  display: "flex",
                  gap: "10px",
                  justifyContent: "flex-end",
                  background: "#f9fafb",
                }}
              >
                <button
                  onClick={() => setShowRejectModal(false)}
                  disabled={isRejecting}
                  style={{
                    padding: "8px 16px",
                    fontSize: "14px",
                    background: "white",
                    border: "1px solid #d1d5db",
                    borderRadius: "6px",
                    cursor: isRejecting ? "not-allowed" : "pointer",
                  }}
                >
                  Cancel
                </button>
                <button
                  onClick={handleRejectApplication}
                  disabled={isRejecting || !rejectionComments.trim()}
                  style={{
                    padding: "8px 20px",
                    fontSize: "14px",
                    fontWeight: 600,
                    background:
                      isRejecting || !rejectionComments.trim()
                        ? "#9ca3af"
                        : "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    cursor:
                      isRejecting || !rejectionComments.trim()
                        ? "not-allowed"
                        : "pointer",
                    transition: "all 0.2s",
                  }}
                >
                  Reject Application
                </button>
              </div>

              {/* Modal Loader Overlay */}
              <ModalLoader
                isVisible={isRejecting}
                message="Rejecting Application"
              />
            </div>
          </div>
        )}
      </div>

      {/* Full Screen Loader */}
      {isApprovingClerk && (
        <FullScreenLoader
          message="Approving Application"
          submessage="Please wait while we process the approval..."
        />
      )}
    </>
  );
};

export default ViewPositionApplication;
