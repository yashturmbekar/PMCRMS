import { Calendar, Clock, CheckCircle, FileCheck } from "lucide-react";
import type { OfficerConfig } from "../components/dashboard/UnifiedOfficerDashboard";

// Junior Engineer Configuration
export const jeConfig: OfficerConfig = {
  officerType: "JE",
  dashboardTitle: "Junior Engineer Dashboard",
  roleCheck: (role: string) => role.includes("Junior"),
  tabs: [
    {
      id: "schedule",
      label: "SCHEDULE APPOINTMENT",
      icon: <Calendar style={{ width: "20px", height: "20px" }} />,
      badgeColor: "var(--pmc-primary)",
      filter: (applications) =>
        applications.filter(
          (app) =>
            app.currentStage !== "Appointment Scheduled" &&
            app.currentStage !== "APPOINTMENT_SCHEDULED"
        ),
      emptyMessage: "No appointments to schedule",
      emptySubMessage: "All assigned applications have scheduled appointments",
      showAutoBanner: false,
    },
    {
      id: "pending",
      label: "JUNIOR ENGINEER PENDING",
      icon: <Clock style={{ width: "20px", height: "20px" }} />,
      badgeColor: "var(--pmc-warning)",
      filter: (applications) =>
        applications.filter(
          (app) =>
            (app.currentStage === "Appointment Scheduled" ||
              app.currentStage === "APPOINTMENT_SCHEDULED") &&
            !(
              app.verificationInfo?.allVerified === true &&
              app.currentStatus === "ASSISTANT_ENGINEER_PENDING"
            )
        ),
      emptyMessage: "No pending applications",
      emptySubMessage: "All applications have been processed",
      showAutoBanner: true,
    },
  ],
  allowedActions: {
    schedule: true,
    approve: true,
    verify: false,
    reject: false,
  },
  autoForwardMessage:
    "Applications automatically move to Assistant Engineer after you complete document verification and digital signature. They will no longer appear in this pending list.",
};

// Assistant Engineer Configuration
export const aeConfig: OfficerConfig = {
  officerType: "AE",
  dashboardTitle: "Assistant Engineer Dashboard",
  roleCheck: (role: string) => role.includes("Assistant"),
  tabs: [
    {
      id: "pending",
      label: "PENDING APPROVAL",
      icon: <Clock style={{ width: "20px", height: "20px" }} />,
      badgeColor: "var(--pmc-primary)",
      filter: (applications) => applications,
      emptyMessage: "No pending applications",
      emptySubMessage:
        "Applications will appear here when forwarded by Junior Engineers",
      showAutoBanner: true,
    },
  ],
  allowedActions: {
    schedule: false,
    approve: false,
    verify: true,
    reject: true,
  },
  autoForwardMessage:
    "After you verify documents and apply digital signature, applications automatically forward to Executive Engineer and are removed from this list. Rejected applications return to the applicant for corrections.",
};

// Executive Engineer Configuration
export const eeConfig: OfficerConfig = {
  officerType: "EE",
  dashboardTitle: "Executive Engineer Dashboard",
  roleCheck: (role: string) =>
    role.includes("ExecutiveEngineer") || role === "EE",
  tabs: [
    {
      id: "stage1",
      label: "STAGE 1 PENDING APPROVAL",
      icon: <FileCheck style={{ width: "20px", height: "20px" }} />,
      badgeColor: "#7c3aed",
      filter: (applications) =>
        applications.filter(
          (app) =>
            app.currentStage === "Stage1" ||
            app.status === "EXECUTIVE_ENGINEER_PENDING_STAGE1"
        ),
      emptyMessage: "No Stage 1 applications pending",
      emptySubMessage: "All Stage 1 applications have been reviewed",
      showAutoBanner: true,
    },
    {
      id: "stage2",
      label: "STAGE 2 PENDING APPROVAL",
      icon: <CheckCircle style={{ width: "20px", height: "20px" }} />,
      badgeColor: "#0891b2",
      filter: (applications) =>
        applications.filter(
          (app) =>
            app.currentStage === "Stage2" ||
            app.status === "EXECUTIVE_ENGINEER_PENDING_STAGE2"
        ),
      emptyMessage: "No Stage 2 applications pending",
      emptySubMessage: "All Stage 2 applications have been reviewed",
      showAutoBanner: true,
    },
  ],
  allowedActions: {
    schedule: false,
    approve: false,
    verify: true,
    reject: true,
  },
  autoForwardMessage:
    "After verification and digital signature, applications automatically forward to City Engineer and are removed from this list.",
};

// City Engineer Configuration
export const ceConfig: OfficerConfig = {
  officerType: "CE",
  dashboardTitle: "City Engineer Dashboard",
  roleCheck: (role: string) => role.includes("CityEngineer") || role === "CE",
  tabs: [
    {
      id: "stage1",
      label: "STAGE 1 PENDING APPROVAL",
      icon: <FileCheck style={{ width: "20px", height: "20px" }} />,
      badgeColor: "#7c3aed",
      filter: (applications) =>
        applications.filter(
          (app) =>
            app.currentStage === "Stage1" ||
            app.status === "CITY_ENGINEER_PENDING_STAGE1"
        ),
      emptyMessage: "No Stage 1 applications pending",
      emptySubMessage: "All Stage 1 applications have been reviewed",
      showAutoBanner: true,
    },
    {
      id: "stage2",
      label: "STAGE 2 PENDING APPROVAL",
      icon: <CheckCircle style={{ width: "20px", height: "20px" }} />,
      badgeColor: "#0891b2",
      filter: (applications) =>
        applications.filter(
          (app) =>
            app.currentStage === "Stage2" ||
            app.status === "CITY_ENGINEER_PENDING_STAGE2"
        ),
      emptyMessage: "No Stage 2 applications pending",
      emptySubMessage: "All Stage 2 applications have been reviewed",
      showAutoBanner: true,
    },
  ],
  allowedActions: {
    schedule: false,
    approve: false,
    verify: true,
    reject: true,
  },
  autoForwardMessage:
    "After verification and digital signature, applications are marked as complete and approved.",
};

// Clerk Configuration
export const clerkConfig: OfficerConfig = {
  officerType: "Clerk",
  dashboardTitle: "Clerk Dashboard",
  roleCheck: (role: string) => role.includes("Clerk"),
  tabs: [
    {
      id: "pending",
      label: "PENDING APPROVAL",
      icon: <Clock style={{ width: "20px", height: "20px" }} />,
      badgeColor: "var(--pmc-primary)",
      filter: (applications) => applications,
      emptyMessage: "No pending applications",
      emptySubMessage: "All applications have been processed",
      showAutoBanner: true,
    },
  ],
  allowedActions: {
    schedule: false,
    approve: true,
    verify: false,
    reject: true,
  },
  autoForwardMessage:
    "After approval, applications automatically forward to Executive Engineer (Stage 2) for certificate signature and are removed from this list. No document verification or digital signature required.",
};
