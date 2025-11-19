import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { jeWorkflowService } from "../services/jeWorkflowService";
import { aeWorkflowService } from "../services/aeWorkflowService";
import { eeWorkflowService } from "../services/eeWorkflowService";
import eeStage2WorkflowService from "../services/eeStage2WorkflowService";
import { ceWorkflowService } from "../services/ceWorkflowService";
import ceStage2WorkflowService from "../services/ceStage2WorkflowService";
import { clerkWorkflowService } from "../services/clerkWorkflowService";
import positionRegistrationService from "../services/positionRegistrationService";
import { REDIRECT_DELAY } from "../constants";
import { Calendar, Clock, Eye, CheckCircle, XCircle, Info } from "lucide-react";
import {
  PageLoader,
  ModalLoader,
  Pagination,
  FullScreenLoader,
} from "../components";
import {
  DocumentApprovalModal,
  OTPVerificationModal,
} from "../components/workflow";
import NotificationModal from "../components/common/NotificationModal";
import DateTimePicker from "../components/DateTimePicker";
import { parseLocalDateTime } from "../utils/dateUtils";
import type { NotificationType } from "../components/common/NotificationModal";
import type { JEWorkflowStatusDto } from "../types/jeWorkflow";
import type { AEWorkflowStatusDto, PositionType } from "../types/aeWorkflow";

interface Application {
  applicationId: number;
  applicationNumber: string;
  firstName?: string;
  lastName?: string;
  applicantName?: string;
  status: string;
  createdDate: string;
  position?: string;
  positionType?: string;
  currentStage?: string;
  currentStatus?: string;
  assignedJEName?: string;
  assignedAEName?: string | null;
  assignedToAEName?: string;
  verificationInfo?: {
    allVerified?: boolean;
  } | null;
  workflow?: {
    hasAppointment?: boolean;
    appointmentDate?: string;
    isAppointmentCompleted?: boolean;
    allDocumentsVerified?: boolean;
    digitalSignatureApplied?: boolean;
    currentStage?: string;
  };
  documents?: any[];
  id?: number;
  jeApprovalStatus?: boolean;
  aeApprovalStatus?: boolean;
  eeApprovalStatus?: boolean;
  ceApprovalStatus?: boolean;
  isStage2?: boolean;
  stage2Data?: any;
}

const OfficerDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [applications, setApplications] = useState<Application[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<string>("tab1");
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [showSuccessPopup, setShowSuccessPopup] = useState(false);
  const [scheduleError, setScheduleError] = useState("");
  const [isScheduling, setIsScheduling] = useState(false);
  const [selectedApplication, setSelectedApplication] =
    useState<Application | null>(null);
  const [scheduleForm, setScheduleForm] = useState({
    comments: "",
    reviewDate: "",
    contactPerson: "",
    place: "",
    roomNumber: "",
  });
  const [showDocumentApprovalModal, setShowDocumentApprovalModal] =
    useState(false);
  const [selectedApplicationForApproval, setSelectedApplicationForApproval] =
    useState<{ id: number; documents: any[] } | null>(null);
  const [showOTPModal, setShowOTPModal] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectionComments, setRejectionComments] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);
  const [showClerkApprovalModal, setShowClerkApprovalModal] = useState(false);
  const [clerkRemarks, setClerkRemarks] = useState("");
  const [isApprovingClerk, setIsApprovingClerk] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(5);
  const [notification, setNotification] = useState<{
    isOpen: boolean;
    message: string;
    type: NotificationType;
    title: string;
    autoClose?: boolean;
  }>({
    isOpen: false,
    message: "",
    type: "info",
    title: "",
    autoClose: false,
  });

  // Determine officer type and configuration
  const getOfficerType = (): {
    type: "JE" | "AE" | "EE" | "CE" | "Clerk";
    title: string;
    subtitle: string;
    tabs: Array<{ id: string; label: string; icon: any; badge?: string }>;
  } => {
    if (!user) return { type: "JE", title: "", subtitle: "", tabs: [] };

    if (user.role.includes("Junior")) {
      return {
        type: "JE",
        title: "Junior Engineer Dashboard",
        subtitle: `Welcome, ${user.name}! Manage your assigned applications and appointments.`,
        tabs: [
          {
            id: "tab1",
            label: "SCHEDULE APPOINTMENT",
            icon: Calendar,
            badge: "primary",
          },
          {
            id: "tab2",
            label: "JUNIOR ENGINEER PENDING",
            icon: Clock,
            badge: "warning",
          },
        ],
      };
    }

    if (user.role.includes("Assistant")) {
      return {
        type: "AE",
        title: "Assistant Engineer Dashboard",
        subtitle: `Welcome, ${user.name}! Review and approve applications.`,
        tabs: [
          {
            id: "tab1",
            label: "PENDING APPROVAL",
            icon: Clock,
            badge: "warning",
          },
        ],
      };
    }

    if (user.role.includes("ExecutiveEngineer")) {
      return {
        type: "EE",
        title: "Executive Engineer Dashboard",
        subtitle: "Review and approve applications from all position types",
        tabs: [
          {
            id: "tab1",
            label: "STAGE 1 PENDING APPROVAL",
            icon: Clock,
            badge: "warning",
          },
          {
            id: "tab2",
            label: "STAGE 2 PENDING APPROVAL",
            icon: CheckCircle,
            badge: "success",
          },
        ],
      };
    }

    if (user.role.includes("CityEngineer")) {
      return {
        type: "CE",
        title: "City Engineer Dashboard",
        subtitle: "Final review and approval of applications",
        tabs: [
          {
            id: "tab1",
            label: "STAGE 1 PENDING APPROVAL",
            icon: Clock,
            badge: "warning",
          },
          {
            id: "tab2",
            label: "STAGE 2 PENDING APPROVAL",
            icon: CheckCircle,
            badge: "success",
          },
        ],
      };
    }

    if (user.role.includes("Clerk")) {
      return {
        type: "Clerk",
        title: "Clerk Dashboard",
        subtitle: "Manage and process applications",
        tabs: [
          {
            id: "tab1",
            label: "PENDING APPROVAL",
            icon: Clock,
            badge: "warning",
          },
        ],
      };
    }

    return { type: "JE", title: "", subtitle: "", tabs: [] };
  };

  const officerConfig = getOfficerType();

  // Fetch dashboard data
  useEffect(() => {
    if (!user) {
      navigate("/");
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        let fetchedApplications: Application[] = [];

        if (officerConfig.type === "JE") {
          const response = await jeWorkflowService.getOfficerApplications(
            user.id
          );
          const assignedApplications = Array.isArray(response)
            ? response
            : response.data || [];

          fetchedApplications = assignedApplications.map(
            (app: JEWorkflowStatusDto) => ({
              applicationId: app.applicationId,
              applicationNumber:
                app.applicationNumber || `PMC_APP_${app.applicationId}`,
              firstName: app.firstName || "Unknown",
              lastName: app.lastName || "",
              applicantName: `${app.firstName || ""} ${
                app.lastName || ""
              }`.trim(),
              status: app.currentStage || "JUNIOR_ENGINEER_PENDING",
              currentStage: app.currentStage,
              currentStatus: app.currentStatus,
              createdDate: app.lastUpdated || new Date().toISOString(),
              position: "Architect",
              verificationInfo: app.verificationInfo,
              workflow: app,
            })
          );
        } else if (officerConfig.type === "AE") {
          const getPositionTypeString = (role: string): string => {
            const roleToPositionString: Record<string, string> = {
              AssistantArchitect: "Architect",
              AssistantStructuralEngineer: "StructuralEngineer",
              AssistantLicenceEngineer: "LicenceEngineer",
              AssistantSupervisor1: "Supervisor1",
              AssistantSupervisor2: "Supervisor2",
            };
            return roleToPositionString[role] || "Architect";
          };

          const positionTypeString = getPositionTypeString(user.role);
          const pending = await aeWorkflowService.getPendingApplications(
            positionTypeString
          );

          fetchedApplications = pending.map((app: AEWorkflowStatusDto) => ({
            ...app,
            assignedAEName: app.assignedToAEName,
          }));
        } else if (officerConfig.type === "EE") {
          // Fetch both Stage 1 and Stage 2 applications
          const stage1Pending =
            await eeWorkflowService.getPendingApplications();
          const stage2Pending =
            await eeStage2WorkflowService.getPendingApplications();

          // Map Stage 1 applications
          const stage1Apps = stage1Pending.map((app) => ({
            ...app,
            status: app.status || "EXECUTIVE_ENGINEER_PENDING",
            createdDate: app.createdDate || new Date().toISOString(),
            isStage2: false,
          }));

          // Map Stage 2 applications
          const stage2Apps = stage2Pending.map((app: any) => ({
            applicationId: app.id || app.applicationId, // Backend uses 'id', frontend expects 'applicationId'
            applicationNumber: app.applicationNumber,
            applicantName: app.applicantName,
            firstName: app.applicantName?.split(" ")[0] || "",
            lastName: app.applicantName?.split(" ").slice(1).join(" ") || "",
            status: "EXECUTIVE_ENGINEER_SIGN_PENDING",
            createdDate:
              app.processedByClerkDate ||
              app.clerkProcessedDate ||
              new Date().toISOString(),
            positionType: app.positionType || app.buildingType,
            position: app.positionType || app.buildingType,
            assignedAEName: "Clerk",
            isStage2: true,
            stage2Data: app,
          })); // Combine both stages
          fetchedApplications = [...stage1Apps, ...stage2Apps];
        } else if (officerConfig.type === "CE") {
          // Fetch both Stage 1 and Stage 2 applications
          const stage1Pending =
            await ceWorkflowService.getPendingApplications();
          const stage2Pending =
            await ceStage2WorkflowService.getPendingApplications();

          // Map Stage 1 applications
          const stage1Apps = stage1Pending.map((app) => ({
            ...app,
            status: app.status || "CITY_ENGINEER_PENDING",
            createdDate: app.createdDate || new Date().toISOString(),
            isStage2: false,
          }));

          // Map Stage 2 applications
          const stage2Apps = stage2Pending.map((app) => ({
            applicationId: app.id, // Backend returns 'id', map it to 'applicationId'
            applicationNumber: app.applicationNumber,
            applicantName: app.applicantName,
            firstName: app.applicantName?.split(" ")[0] || "",
            lastName: app.applicantName?.split(" ").slice(1).join(" ") || "",
            status: "CITY_ENGINEER_SIGN_PENDING",
            createdDate: app.eeStage2SignedDate || app.createdAt,
            positionType: app.positionType,
            position: app.positionType,
            assignedAEName: "EE Stage 2",
            isStage2: true,
            stage2Data: app,
          }));

          // Combine both stages
          fetchedApplications = [...stage1Apps, ...stage2Apps];
        } else if (officerConfig.type === "Clerk") {
          const pending = await clerkWorkflowService.getPendingApplications();
          fetchedApplications = pending.map((app) => ({
            ...app,
            applicationId: app.id,
            applicationNumber: app.applicationNumber,
            applicantName: app.applicantName,
            firstName: app.applicantName?.split(" ")[0] || "",
            lastName: app.applicantName?.split(" ").slice(1).join(" ") || "",
            positionType: app.positionType,
            assignedAEName: app.assignedAEName,
            status: "CLERK_PENDING",
            createdDate: app.createdAt || new Date().toISOString(),
          }));
        }

        setApplications(fetchedApplications);
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
        setApplications([]);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user, navigate, officerConfig.type]);

  // Filter applications based on active tab
  const getFilteredApplications = () => {
    if (officerConfig.type === "JE") {
      if (activeTab === "tab1") {
        // Schedule Appointment tab - only show applications WITHOUT scheduled appointments
        return applications.filter((app) => {
          // Check if appointment is scheduled using the workflow data
          const hasScheduledAppointment = app.workflow?.hasAppointment === true;
          // Exclude applications that already have appointments or have completed payment/moved forward
          const hasCompletedPayment =
            app.status === "PaymentCompleted" ||
            app.status === "CLERK_PENDING" ||
            app.currentStage === "Payment Completed" ||
            app.currentStage?.includes("Clerk") ||
            app.currentStage?.includes("PAYMENT") ||
            app.currentStage?.includes("EXECUTIVE_ENGINEER_SIGN");

          return !hasScheduledAppointment && !hasCompletedPayment;
        });
      } else {
        // JE Pending tab - appointment scheduled but JE workflow not completed
        // Show if: appointment scheduled AND (JE hasn't approved OR digital signature not applied)
        return applications.filter((app) => {
          const hasScheduledAppointment = app.workflow?.hasAppointment === true;
          const jeWorkflowCompleted =
            app.workflow?.digitalSignatureApplied === true &&
            app.currentStatus === "ASSISTANT_ENGINEER_PENDING";

          // Show applications where appointment is scheduled and JE work is not complete
          return hasScheduledAppointment && !jeWorkflowCompleted;
        });
      }
    } else if (officerConfig.type === "EE" || officerConfig.type === "CE") {
      if (activeTab === "tab1") {
        // Stage 1 - filter logic based on status
        return applications.filter(
          (app) =>
            !app.isStage2 &&
            app.status?.includes("PENDING") &&
            !app.status?.includes("STAGE2")
        );
      } else {
        // Stage 2 - filter logic based on isStage2 flag
        return applications.filter((app) => app.isStage2 === true);
      }
    }

    // AE and Clerk - single tab, show all
    return applications;
  };

  const filteredApplications = getFilteredApplications();

  // Pagination logic
  const getCurrentPageData = () => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    return filteredApplications.slice(startIndex, endIndex);
  };

  const getTotalPages = () => {
    return Math.ceil(filteredApplications.length / itemsPerPage);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    // Scroll to top of dashboard
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  // Reset to first page when tab changes or applications change
  useEffect(() => {
    setCurrentPage(1);
  }, [activeTab, applications]);

  // Event handlers
  const handleScheduleAppointment = (application: Application) => {
    setSelectedApplication(application);
    setShowScheduleModal(true);
    setScheduleError("");
    setScheduleForm({
      comments: "",
      reviewDate: "",
      contactPerson: "",
      place: "",
      roomNumber: "",
    });
  };

  const handleSubmitSchedule = async () => {
    setScheduleError("");

    if (
      !selectedApplication ||
      !scheduleForm.reviewDate ||
      !scheduleForm.contactPerson ||
      !scheduleForm.place ||
      !scheduleForm.roomNumber
    ) {
      setScheduleError("Please fill in all required fields");
      return;
    }

    try {
      setIsScheduling(true);
      await jeWorkflowService.scheduleAppointment({
        applicationId: selectedApplication.applicationId,
        reviewDate: scheduleForm.reviewDate,
        place: scheduleForm.place,
        contactPerson: scheduleForm.contactPerson,
        roomNumber: scheduleForm.roomNumber,
        remarks: scheduleForm.comments,
      });

      setShowScheduleModal(false);
      setSelectedApplication(null);
      setShowSuccessPopup(true);

      setTimeout(() => {
        window.location.reload();
      }, 2000);
    } catch (error: unknown) {
      console.error("Error scheduling appointment:", error);
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

  const handleViewApplication = (applicationId: number) => {
    // All officers use the same officer view page
    navigate(`/position-application/${applicationId}`);
  };

  const handleDocumentApprove = async (application: Application) => {
    try {
      const fullApplication = await positionRegistrationService.getApplication(
        application.applicationId
      );
      setSelectedApplicationForApproval({
        id: fullApplication.id,
        documents: fullApplication.documents.map((doc) => ({
          id: doc.id,
          documentTypeName: doc.documentTypeName,
          fileName: doc.fileName,
          fileSize: doc.fileSize,
          isVerified: doc.isVerified,
        })),
      });
      setShowDocumentApprovalModal(true);
    } catch (error) {
      console.error("Error fetching application details:", error);
      setNotification({
        isOpen: true,
        message: "Failed to load application details. Please try again.",
        type: "error",
        title: "Error",
        autoClose: false,
      });
    }
  };

  const handleDocumentApprovalComplete = () => {
    setShowDocumentApprovalModal(false);
    setSelectedApplicationForApproval(null);
    window.location.reload();
  };

  const handleVerifyDocuments = (application: Application) => {
    setSelectedApplication(application);
    // Directly show OTP modal for all officers (Stage 1 and Stage 2)
    // The OTP modal has a built-in comments field
    setShowOTPModal(true);
  };

  const handleClerkApprove = (application: Application) => {
    setSelectedApplication(application);
    setClerkRemarks("");
    setShowClerkApprovalModal(true);
  };

  const handleClerkApprovalSubmit = async () => {
    if (!selectedApplication) return;

    setIsApprovingClerk(true);

    try {
      const result = await clerkWorkflowService.approveApplication(
        selectedApplication.applicationId,
        clerkRemarks || ""
      );

      if (result.success) {
        setShowClerkApprovalModal(false);
        setNotification({
          isOpen: true,
          message: result.message || "Application approved successfully",
          type: "success",
          title: "Success",
          autoClose: true,
        });
        // Refresh the application list
        setTimeout(() => window.location.reload(), REDIRECT_DELAY);
      } else {
        setNotification({
          isOpen: true,
          message: result.message || "Failed to approve application",
          type: "error",
          title: "Error",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error approving application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to approve application. Please try again.",
        type: "error",
        title: "Error",
        autoClose: false,
      });
    } finally {
      setIsApprovingClerk(false);
    }
  };

  const handleRejectClick = (application: Application) => {
    setSelectedApplication(application);
    setRejectionComments("");
    setShowRejectModal(true);
  };

  const handleGenerateOtp = async (
    applicationId: number
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      if (officerConfig.type === "AE") {
        return await aeWorkflowService.generateOtpForSignature(applicationId);
      } else if (officerConfig.type === "EE") {
        // Check if this is a Stage 2 application (license certificate signature)
        const application = applications.find(
          (app) => app.applicationId === applicationId
        );
        if (
          application?.isStage2 ||
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
      } else if (officerConfig.type === "CE") {
        // Check if this is a Stage 2 application (final license certificate signature)
        const application = applications.find(
          (app) => app.applicationId === applicationId
        );
        if (
          application?.isStage2 ||
          application?.status === "CITY_ENGINEER_SIGN_PENDING"
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
      if (officerConfig.type === "AE") {
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
      } else if (officerConfig.type === "EE") {
        // Check if this is a Stage 2 application (license certificate signature)
        const application = applications.find(
          (app) => app.applicationId === applicationId
        );
        if (
          application?.isStage2 ||
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
      } else if (officerConfig.type === "CE") {
        // Check if this is a Stage 2 application (final license certificate signature)
        const application = applications.find(
          (app) => app.applicationId === applicationId
        );
        if (
          application?.isStage2 ||
          application?.status === "CITY_ENGINEER_SIGN_PENDING"
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

  const handleRejectApplication = async () => {
    if (!rejectionComments.trim()) {
      setNotification({
        isOpen: true,
        message: "Rejection comments are mandatory",
        type: "warning",
        title: "Missing Comments",
        autoClose: false,
      });
      return;
    }

    if (!selectedApplication) return;

    try {
      setIsRejecting(true);
      let result;

      if (officerConfig.type === "JE") {
        result = await jeWorkflowService.rejectApplication({
          applicationId: selectedApplication.applicationId,
          rejectionComments,
        });
      } else if (officerConfig.type === "AE") {
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
        result = await aeWorkflowService.rejectApplication({
          applicationId: selectedApplication.applicationId,
          positionType,
          rejectionComments,
        });
      } else if (officerConfig.type === "EE") {
        result = await eeWorkflowService.rejectApplication({
          applicationId: selectedApplication.applicationId,
          rejectionComments,
        });
      } else if (officerConfig.type === "CE") {
        result = await ceWorkflowService.rejectApplication({
          applicationId: selectedApplication.applicationId,
          rejectionComments,
        });
      } else if (officerConfig.type === "Clerk") {
        result = await clerkWorkflowService.rejectApplication(
          selectedApplication.applicationId,
          rejectionComments
        );
      }

      if (result?.success) {
        setNotification({
          isOpen: true,
          message: "Application rejected successfully",
          type: "success",
          title: "Application Rejected",
          autoClose: true,
        });
        setShowRejectModal(false);
        setRejectionComments("");
        setSelectedApplication(null);
        window.location.reload();
      } else {
        setNotification({
          isOpen: true,
          message: result?.message || "Failed to reject application",
          type: "error",
          title: "Rejection Failed",
          autoClose: false,
        });
      }
    } catch (error) {
      console.error("Error rejecting application:", error);
      setNotification({
        isOpen: true,
        message: "Failed to reject application. Please try again.",
        type: "error",
        title: "Rejection Failed",
        autoClose: false,
      });
    } finally {
      setIsRejecting(false);
    }
  };

  const getPositionLabel = (position: string) => {
    const labels: Record<string, string> = {
      "0": "Architect",
      "1": "Licence Engineer",
      "2": "Structural Engineer",
      "3": "Supervisor 1",
      "4": "Supervisor 2",
      Architect: "Architect",
      LicenceEngineer: "Licence Engineer",
      StructuralEngineer: "Structural Engineer",
      Supervisor1: "Supervisor 1",
      Supervisor2: "Supervisor 2",
    };
    return labels[position] || position;
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return "N/A";
    const date = parseLocalDateTime(dateString);
    return date.toLocaleDateString("en-IN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  if (loading) {
    return <PageLoader />;
  }

  return (
    <>
      <NotificationModal
        isOpen={notification.isOpen}
        onClose={() => setNotification({ ...notification, isOpen: false })}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        autoClose={notification.autoClose}
      />
      <div
        className="pmc-dashboard pmc-fadeIn"
        style={{ padding: "24px", maxWidth: "1400px", margin: "0 auto" }}
      >
        {/* Header */}
        <div style={{ marginBottom: "32px" }}>
          <h1
            className="pmc-text-3xl pmc-font-bold"
            style={{ color: "var(--pmc-text-primary)", marginBottom: "8px" }}
          >
            {officerConfig.title}
          </h1>
          <p
            className="pmc-text-base"
            style={{ color: "var(--pmc-text-secondary)" }}
          >
            {officerConfig.subtitle}
          </p>
        </div>

        {/* Tabs */}
        {officerConfig.tabs.length > 1 && (
          <div
            className="pmc-card"
            style={{ marginBottom: "24px", overflow: "hidden" }}
          >
            <div
              style={{
                display: "flex",
                borderBottom: "2px solid var(--pmc-border)",
              }}
            >
              {officerConfig.tabs.map((tab) => {
                const Icon = tab.icon;

                // Calculate count for each tab independently - MUST match getFilteredApplications() logic
                let tabCount = 0;
                if (officerConfig.type === "JE") {
                  if (tab.id === "tab1") {
                    // Schedule Appointment tab - same logic as getFilteredApplications()
                    tabCount = applications.filter((app) => {
                      const hasScheduledAppointment =
                        app.workflow?.hasAppointment === true;
                      const hasCompletedPayment =
                        app.status === "PaymentCompleted" ||
                        app.status === "CLERK_PENDING" ||
                        app.currentStage === "Payment Completed" ||
                        app.currentStage?.includes("Clerk") ||
                        app.currentStage?.includes("PAYMENT") ||
                        app.currentStage?.includes("EXECUTIVE_ENGINEER_SIGN");
                      return !hasScheduledAppointment && !hasCompletedPayment;
                    }).length;
                  } else if (tab.id === "tab2") {
                    // JE Pending tab - same logic as getFilteredApplications()
                    tabCount = applications.filter((app) => {
                      const hasScheduledAppointment =
                        app.workflow?.hasAppointment === true;
                      const jeWorkflowCompleted =
                        app.workflow?.digitalSignatureApplied === true &&
                        app.currentStatus === "ASSISTANT_ENGINEER_PENDING";
                      return hasScheduledAppointment && !jeWorkflowCompleted;
                    }).length;
                  }
                } else if (
                  officerConfig.type === "EE" ||
                  officerConfig.type === "CE"
                ) {
                  if (tab.id === "tab1") {
                    // Stage 1 - count non-Stage2 applications
                    tabCount = applications.filter(
                      (app) =>
                        !app.isStage2 &&
                        app.status?.includes("PENDING") &&
                        !app.status?.includes("STAGE2")
                    ).length;
                  } else if (tab.id === "tab2") {
                    // Stage 2 - count applications with isStage2 flag
                    tabCount = applications.filter(
                      (app) => app.isStage2 === true
                    ).length;
                  }
                } else {
                  // AE and Clerk - single tab, show all
                  tabCount = applications.length;
                }

                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    style={{
                      flex: 1,
                      padding: "16px 24px",
                      background:
                        activeTab === tab.id
                          ? "var(--pmc-primary)"
                          : "transparent",
                      color:
                        activeTab === tab.id
                          ? "white"
                          : "var(--pmc-text-secondary)",
                      border: "none",
                      borderBottom:
                        activeTab === tab.id
                          ? "3px solid var(--pmc-primary)"
                          : "3px solid transparent",
                      cursor: "pointer",
                      fontWeight: 600,
                      fontSize: "16px",
                      transition: "all 0.3s ease",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        gap: "8px",
                      }}
                    >
                      <Icon style={{ width: "20px", height: "20px" }} />
                      <span>{tab.label}</span>
                      <span
                        style={{
                          background:
                            activeTab === tab.id
                              ? "rgba(255,255,255,0.2)"
                              : tab.badge === "warning"
                              ? "var(--pmc-warning)"
                              : "var(--pmc-primary)",
                          color: "white",
                          padding: "2px 8px",
                          borderRadius: "12px",
                          fontSize: "12px",
                          fontWeight: "bold",
                        }}
                      >
                        {tabCount}
                      </span>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
        )}

        {/* Application List */}
        <div className="pmc-card pmc-slideInLeft">
          <div className="pmc-card-header">
            <h2 className="pmc-card-title">
              {officerConfig.tabs.find((t) => t.id === activeTab)?.label ||
                "Applications"}
            </h2>
            <p className="pmc-card-subtitle">
              {officerConfig.type === "JE" &&
                activeTab === "tab1" &&
                "Applications requiring appointment scheduling"}
              {officerConfig.type === "JE" &&
                activeTab === "tab2" &&
                "Applications under review by Junior Engineer"}
              {officerConfig.type === "AE" &&
                "Applications pending your approval"}
              {(officerConfig.type === "EE" || officerConfig.type === "CE") &&
                activeTab === "tab1" &&
                "Stage 1 pending applications"}
              {(officerConfig.type === "EE" || officerConfig.type === "CE") &&
                activeTab === "tab2" &&
                "Stage 2 pending applications"}
              {officerConfig.type === "Clerk" &&
                "Applications pending approval"}
            </p>
          </div>

          <div className="pmc-card-body">
            {filteredApplications.length === 0 ? (
              <div
                style={{
                  textAlign: "center",
                  padding: "48px 24px",
                  color: "var(--pmc-text-secondary)",
                }}
              >
                {officerConfig.type === "JE" && activeTab === "tab1" ? (
                  <>
                    <Calendar
                      style={{
                        width: "64px",
                        height: "64px",
                        margin: "0 auto 16px",
                        opacity: 0.3,
                      }}
                    />
                    <p className="pmc-text-lg pmc-font-medium">
                      No appointments to schedule
                    </p>
                    <p className="pmc-text-sm">
                      All assigned applications have scheduled appointments
                    </p>
                  </>
                ) : (
                  <>
                    <CheckCircle
                      style={{
                        width: "64px",
                        height: "64px",
                        margin: "0 auto 16px",
                        opacity: 0.3,
                      }}
                    />
                    <p className="pmc-text-lg pmc-font-medium">
                      No pending applications
                    </p>
                    <p className="pmc-text-sm">
                      All applications have been processed
                    </p>
                  </>
                )}
              </div>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table className="pmc-table">
                  <thead>
                    <tr>
                      <th>Application ID</th>
                      <th>Applicant Name</th>
                      <th>Created Date</th>
                      {officerConfig.type === "JE" && activeTab === "tab1" && (
                        <th>Status</th>
                      )}
                      {officerConfig.type === "JE" && activeTab === "tab2" && (
                        <th>Stage</th>
                      )}
                      {(officerConfig.type === "AE" ||
                        officerConfig.type === "EE" ||
                        officerConfig.type === "CE" ||
                        officerConfig.type === "Clerk") && (
                        <>
                          <th>Position Type</th>
                          <th>Approved by</th>
                        </>
                      )}
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {getCurrentPageData().map((app) => (
                      <tr key={app.applicationId || app.applicationNumber}>
                        <td>
                          <span className="pmc-badge pmc-badge-primary">
                            {app.applicationNumber}
                          </span>
                        </td>
                        <td>
                          {app.applicantName ||
                            `${app.firstName || ""} ${
                              app.lastName || ""
                            }`.trim() ||
                            "N/A"}
                        </td>
                        <td>{formatDate(app.createdDate)}</td>
                        {officerConfig.type === "JE" &&
                          activeTab === "tab1" && (
                            <td>
                              <span className="pmc-badge pmc-badge-warning">
                                JUNIOR ENGINEER PENDING
                              </span>
                            </td>
                          )}
                        {officerConfig.type === "JE" &&
                          activeTab === "tab2" && (
                            <td>
                              <span className="pmc-badge pmc-badge-warning">
                                {jeWorkflowService.getStageName(app.status)}
                              </span>
                            </td>
                          )}
                        {(officerConfig.type === "AE" ||
                          officerConfig.type === "EE" ||
                          officerConfig.type === "CE" ||
                          officerConfig.type === "Clerk") && (
                          <>
                            <td>
                              {getPositionLabel(
                                app.positionType || app.position || ""
                              )}
                            </td>
                            <td>
                              {app.assignedAEName ||
                                app.assignedToAEName ||
                                "N/A"}
                            </td>
                          </>
                        )}
                        <td>
                          <div
                            style={{
                              display: "flex",
                              gap: "8px",
                              flexWrap: "wrap",
                            }}
                          >
                            <button
                              className="pmc-button pmc-button-sm pmc-button-primary"
                              onClick={() =>
                                handleViewApplication(app.applicationId)
                              }
                              style={{
                                display: "flex",
                                alignItems: "center",
                                gap: "4px",
                              }}
                            >
                              <Eye style={{ width: "16px", height: "16px" }} />
                              View
                            </button>
                            {officerConfig.type === "JE" &&
                              activeTab === "tab1" && (
                                <>
                                  <button
                                    className="pmc-button pmc-button-sm pmc-button-success"
                                    onClick={() =>
                                      handleScheduleAppointment(app)
                                    }
                                    style={{
                                      display: "flex",
                                      alignItems: "center",
                                      gap: "4px",
                                    }}
                                  >
                                    <Calendar
                                      style={{ width: "16px", height: "16px" }}
                                    />
                                    Schedule
                                  </button>
                                  <button
                                    className="pmc-button pmc-button-sm"
                                    onClick={() => handleRejectClick(app)}
                                    style={{
                                      display: "flex",
                                      alignItems: "center",
                                      gap: "4px",
                                      background:
                                        "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                                      color: "white",
                                    }}
                                  >
                                    <XCircle
                                      style={{ width: "16px", height: "16px" }}
                                    />
                                    Reject
                                  </button>
                                </>
                              )}
                            {officerConfig.type === "JE" &&
                              activeTab === "tab2" && (
                                <>
                                  <button
                                    className="pmc-button pmc-button-sm pmc-button-success"
                                    onClick={() => handleDocumentApprove(app)}
                                    style={{
                                      display: "flex",
                                      alignItems: "center",
                                      gap: "4px",
                                    }}
                                  >
                                    <CheckCircle
                                      style={{ width: "16px", height: "16px" }}
                                    />
                                    Document Approve
                                  </button>
                                  <button
                                    className="pmc-button pmc-button-sm"
                                    onClick={() => handleRejectClick(app)}
                                    style={{
                                      display: "flex",
                                      alignItems: "center",
                                      gap: "4px",
                                      background:
                                        "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                                      color: "white",
                                    }}
                                  >
                                    <XCircle
                                      style={{ width: "16px", height: "16px" }}
                                    />
                                    Reject
                                  </button>
                                </>
                              )}
                            {(officerConfig.type === "AE" ||
                              officerConfig.type === "EE" ||
                              officerConfig.type === "CE") && (
                              <>
                                <button
                                  className="pmc-button pmc-button-sm pmc-button-success"
                                  onClick={() => handleVerifyDocuments(app)}
                                  style={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: "4px",
                                  }}
                                >
                                  <CheckCircle
                                    style={{ width: "16px", height: "16px" }}
                                  />
                                  Verify
                                </button>
                                {/* Only Stage 1 officers can reject */}
                                {!app.isStage2 && (
                                  <button
                                    className="pmc-button pmc-button-sm"
                                    onClick={() => handleRejectClick(app)}
                                    style={{
                                      display: "flex",
                                      alignItems: "center",
                                      gap: "4px",
                                      background:
                                        "linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)",
                                      color: "white",
                                    }}
                                  >
                                    <XCircle
                                      style={{ width: "16px", height: "16px" }}
                                    />
                                    Reject
                                  </button>
                                )}
                              </>
                            )}
                            {/* Clerk cannot reject applications */}
                            {officerConfig.type === "Clerk" && (
                              <>
                                <button
                                  className="pmc-button pmc-button-sm pmc-button-success"
                                  onClick={() => handleClerkApprove(app)}
                                  style={{
                                    display: "flex",
                                    alignItems: "center",
                                    gap: "4px",
                                  }}
                                >
                                  <CheckCircle
                                    style={{ width: "16px", height: "16px" }}
                                  />
                                  Approve
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Pagination */}
                {filteredApplications.length > 0 && (
                  <Pagination
                    currentPage={currentPage}
                    totalPages={getTotalPages()}
                    totalItems={filteredApplications.length}
                    itemsPerPage={itemsPerPage}
                    onPageChange={handlePageChange}
                    showFirstLast={true}
                    showPageInfo={true}
                  />
                )}
              </div>
            )}
          </div>
        </div>

        {/* Schedule Appointment Modal (JE only) */}
        {showScheduleModal && selectedApplication && (
          <div
            className="pmc-modal-overlay"
            onClick={() => setShowScheduleModal(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: "rgba(0, 0, 0, 0.6)",
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
                  Application: {selectedApplication.applicationNumber}
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
                {/* Date/Time Picker */}
                <div style={{ marginBottom: "20px" }}>
                  <DateTimePicker
                    label="Select Date & Time"
                    value={scheduleForm.reviewDate}
                    onChange={(value) =>
                      setScheduleForm({
                        ...scheduleForm,
                        reviewDate: value,
                      })
                    }
                    minDate={new Date()}
                    required
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
                Refreshing dashboard...
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

        {/* Document Approval Modal (JE only) */}
        {showDocumentApprovalModal && selectedApplicationForApproval && (
          <DocumentApprovalModal
            isOpen={showDocumentApprovalModal}
            onClose={() => setShowDocumentApprovalModal(false)}
            applicationId={selectedApplicationForApproval.id}
            documents={selectedApplicationForApproval.documents}
            onApprovalComplete={handleDocumentApprovalComplete}
          />
        )}

        {/* OTP Verification Modal (AE, EE, CE only) */}
        {showOTPModal &&
          selectedApplication &&
          (officerConfig.type === "AE" ||
            officerConfig.type === "EE" ||
            officerConfig.type === "CE") && (
            <OTPVerificationModal
              isOpen={showOTPModal}
              onClose={() => {
                setShowOTPModal(false);
                setSelectedApplication(null);
              }}
              applicationId={selectedApplication.applicationId}
              officerType={officerConfig.type}
              onGenerateOtp={handleGenerateOtp}
              onVerifyAndSign={handleVerifyAndSign}
              onSuccess={() => window.location.reload()}
            />
          )}

        {/* Clerk Approval Modal */}
        {showClerkApprovalModal && selectedApplication && (
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
                          Application #{selectedApplication.applicationNumber}
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
                    size={16}
                    style={{ display: "inline", marginRight: "6px" }}
                  />
                  Approve Application
                </button>
              </div>
            </div>
          </div>
        )}

        {/* EE Stage 2 Confirmation Modal */}

        {/* Reject Modal (AE, EE, CE) */}
        {showRejectModal && selectedApplication && (
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
                  <strong>Application:</strong>{" "}
                  {selectedApplication.applicationNumber}
                </p>
                <p style={{ marginBottom: "16px", color: "#475569" }}>
                  <strong>Applicant:</strong>{" "}
                  {selectedApplication.applicantName ||
                    `${selectedApplication.firstName || ""} ${
                      selectedApplication.lastName || ""
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
                    cursor: "pointer",
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
            </div>
          </div>
        )}
      </div>

      {/* Full Screen Loaders */}
      {isApprovingClerk && (
        <FullScreenLoader
          message="Approving Application"
          submessage="Please wait while we process the approval..."
        />
      )}

      {isRejecting && (
        <FullScreenLoader
          message="Rejecting Application"
          submessage="Please wait while we process the rejection..."
        />
      )}
    </>
  );
};

export default OfficerDashboard;
