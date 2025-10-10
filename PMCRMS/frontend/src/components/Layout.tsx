import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import {
  Home,
  FileText,
  Users,
  Upload,
  BarChart3,
  Settings,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import NotificationBell from "./NotificationBell";

interface LayoutProps {
  children: React.ReactNode;
}

interface NavItem {
  name: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  roles?: string[];
}

const navigation: NavItem[] = [
  { name: "Dashboard", href: "/dashboard", icon: Home },
  {
    name: "Admin Dashboard",
    href: "/admin",
    icon: Settings,
    roles: ["Admin"],
  },
  { name: "Applications", href: "/applications", icon: FileText },
  { name: "Documents", href: "/documents", icon: Upload },
  {
    name: "Users",
    href: "/users",
    icon: Users,
    roles: [
      "Admin",
      "JuniorArchitect",
      "AssistantArchitect",
      "JuniorLicenceEngineer",
      "AssistantLicenceEngineer",
      "JuniorStructuralEngineer",
      "AssistantStructuralEngineer",
      "JuniorSupervisor1",
      "AssistantSupervisor1",
      "JuniorSupervisor2",
      "AssistantSupervisor2",
      "ExecutiveEngineer",
      "CityEngineer",
    ],
  },
  {
    name: "Reports",
    href: "/reports",
    icon: BarChart3,
    roles: ["Admin", "ExecutiveEngineer", "CityEngineer"],
  },
  { name: "Settings", href: "/settings", icon: Settings },
];

export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const filteredNavigation = navigation.filter(
    (item) => !item.roles || (user && item.roles.includes(user.role))
  );

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        background: "var(--pmc-bg-secondary)",
      }}
    >
      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div
            className="fixed inset-0"
            style={{
              background: "rgba(0, 0, 0, 0.5)",
              backdropFilter: "blur(4px)",
            }}
            onClick={() => setSidebarOpen(false)}
          />
          <div
            className="relative flex-1 flex flex-col max-w-xs w-full pmc-slideInLeft"
            style={{ background: "white", height: "100vh" }}
          >
            <div className="absolute top-0 right-0 -mr-12 pt-2">
              <button
                type="button"
                className="ml-1 flex items-center justify-center h-10 w-10 rounded-full pmc-focus-visible"
                style={{ background: "rgba(255, 255, 255, 0.1)" }}
                onClick={() => setSidebarOpen(false)}
              >
                <X className="h-6 w-6" style={{ color: "white" }} />
              </button>
            </div>
            <SidebarContent
              navigation={filteredNavigation}
              currentPath={location.pathname}
              onNavigate={() => setSidebarOpen(false)}
            />
          </div>
        </div>
      )}

      {/* Desktop sidebar */}
      <div className="hidden lg:flex lg:flex-shrink-0">
        <div className="flex flex-col" style={{ width: "280px" }}>
          <SidebarContent
            navigation={filteredNavigation}
            currentPath={location.pathname}
          />
        </div>
      </div>

      {/* Main content */}
      <div className="flex flex-col flex-1" style={{ overflow: "hidden" }}>
        {/* Top navigation - PMC Header */}
        <div
          className="pmc-header pmc-fadeInDown"
          style={{
            background: "linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%)",
            boxShadow: "0 4px 20px rgba(0, 0, 0, 0.15)",
            borderBottom: "3px solid #fbbf24",
            position: "sticky",
            top: 0,
            zIndex: 30,
          }}
        >
          <div
            className="pmc-header-content"
            style={{
              padding: "16px 32px",
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <button
                type="button"
                className="lg:hidden pmc-button pmc-button-icon pmc-button-sm"
                style={{
                  background: "rgba(255, 255, 255, 0.15)",
                  border: "1px solid rgba(255, 255, 255, 0.3)",
                  backdropFilter: "blur(10px)",
                  transition: "all 0.3s ease",
                }}
                onClick={() => setSidebarOpen(true)}
                onMouseEnter={(e) => {
                  e.currentTarget.style.background =
                    "rgba(255, 255, 255, 0.25)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background =
                    "rgba(255, 255, 255, 0.15)";
                }}
              >
                <Menu className="h-5 w-5" style={{ color: "white" }} />
              </button>

              <div
                className="pmc-header-brand"
                style={{ display: "flex", alignItems: "center", gap: "12px" }}
              >
                <div
                  className="pmc-header-logo"
                  style={{
                    width: "52px",
                    height: "52px",
                    background: "white",
                    borderRadius: "12px",
                    padding: "8px",
                    boxShadow: "0 4px 12px rgba(0, 0, 0, 0.2)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  <img
                    src="/pmc-logo.png"
                    alt="PMC Logo"
                    style={{
                      width: "100%",
                      height: "100%",
                      objectFit: "contain",
                    }}
                  />
                </div>
                <div>
                  <h1
                    className="pmc-header-title"
                    style={{
                      fontSize: "20px",
                      fontWeight: "700",
                      color: "white",
                      margin: 0,
                      lineHeight: "1.2",
                      letterSpacing: "0.5px",
                    }}
                  >
                    PMCRMS
                  </h1>
                  <p
                    className="pmc-header-subtitle"
                    style={{
                      fontSize: "12px",
                      color: "rgba(255, 255, 255, 0.9)",
                      margin: 0,
                      fontWeight: "500",
                    }}
                  >
                    Pune Municipal Corporation
                  </p>
                </div>
              </div>
            </div>

            <div
              className="pmc-header-actions"
              style={{ display: "flex", alignItems: "center", gap: "16px" }}
            >
              {/* Notification Bell */}
              <div style={{ position: "relative" }}>
                <NotificationBell />
              </div>

              {/* Profile section */}
              <div
                className="pmc-user-menu"
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  padding: "8px 16px",
                  background: "rgba(255, 255, 255, 0.15)",
                  borderRadius: "12px",
                  backdropFilter: "blur(10px)",
                  border: "1px solid rgba(255, 255, 255, 0.2)",
                  transition: "all 0.3s ease",
                  cursor: "pointer",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.background =
                    "rgba(255, 255, 255, 0.25)";
                  e.currentTarget.style.transform = "translateY(-2px)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background =
                    "rgba(255, 255, 255, 0.15)";
                  e.currentTarget.style.transform = "translateY(0)";
                }}
              >
                <div
                  className="pmc-user-avatar"
                  style={{
                    width: "36px",
                    height: "36px",
                    borderRadius: "10px",
                    background:
                      "linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: "16px",
                    fontWeight: "700",
                    color: "white",
                    boxShadow: "0 2px 8px rgba(0, 0, 0, 0.2)",
                  }}
                >
                  {user?.name?.charAt(0).toUpperCase() || "U"}
                </div>
                <div className="pmc-user-info" style={{ textAlign: "left" }}>
                  <p
                    className="pmc-user-name"
                    style={{
                      fontSize: "14px",
                      fontWeight: "600",
                      color: "white",
                      margin: 0,
                      lineHeight: "1.2",
                    }}
                  >
                    {user?.name || "User"}
                  </p>
                  <p
                    className="pmc-user-role"
                    style={{
                      fontSize: "11px",
                      color: "rgba(255, 255, 255, 0.8)",
                      margin: 0,
                      fontWeight: "500",
                    }}
                  >
                    {user?.role || "Applicant"}
                  </p>
                </div>
              </div>

              {/* Logout button */}
              <button
                onClick={handleLogout}
                className="pmc-button pmc-button-danger pmc-button-sm pmc-button-icon"
                style={{
                  background: "rgba(220, 38, 38, 0.9)",
                  border: "1px solid rgba(255, 255, 255, 0.3)",
                  padding: "10px",
                  borderRadius: "10px",
                  transition: "all 0.3s ease",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  boxShadow: "0 2px 8px rgba(0, 0, 0, 0.2)",
                }}
                title="Logout"
                onMouseEnter={(e) => {
                  e.currentTarget.style.background = "#b91c1c";
                  e.currentTarget.style.transform = "translateY(-2px)";
                  e.currentTarget.style.boxShadow =
                    "0 4px 12px rgba(0, 0, 0, 0.3)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background = "rgba(220, 38, 38, 0.9)";
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 2px 8px rgba(0, 0, 0, 0.2)";
                }}
              >
                <LogOut className="h-5 w-5" />
              </button>
            </div>
          </div>
        </div>

        {/* Page content */}
        <main
          className="flex-1 relative pmc-content"
          style={{
            marginLeft: 0,
            overflowY: "auto",
            padding: "32px",
            background: "var(--pmc-bg-secondary)",
          }}
        >
          <div style={{ maxWidth: "1400px", margin: "0 auto" }}>{children}</div>
        </main>
      </div>
    </div>
  );
};

interface SidebarContentProps {
  navigation: NavItem[];
  currentPath: string;
  onNavigate?: () => void;
}

const SidebarContent: React.FC<SidebarContentProps> = ({
  navigation,
  currentPath,
  onNavigate,
}) => (
  <div
    className="pmc-sidebar pmc-fadeIn"
    style={{
      position: "static",
      width: "100%",
      height: "100vh",
      overflowY: "auto",
    }}
  >
    {/* Logo Section - Professional Header */}
    <div
      className="pmc-sidebar-header"
      style={{ padding: "24px", textAlign: "center" }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "12px",
          justifyContent: "center",
        }}
      >
        <div
          style={{
            width: "48px",
            height: "48px",
            background: "white",
            borderRadius: "12px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "8px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
          }}
        >
          <img
            src="/pmc-logo.png"
            alt="PMC Logo"
            style={{ width: "100%", height: "100%", objectFit: "contain" }}
          />
        </div>
        <div style={{ textAlign: "left" }}>
          <div
            className="pmc-font-bold"
            style={{ fontSize: "16px", lineHeight: "1.2" }}
          >
            PMC RMS
          </div>
          <div className="pmc-text-xs" style={{ opacity: 0.9 }}>
            Record Management
          </div>
        </div>
      </div>
    </div>

    {/* Navigation */}
    <nav className="pmc-sidebar-nav" style={{ padding: "16px" }}>
      <div className="pmc-nav-section">
        <h3 className="pmc-nav-section-title">Main Menu</h3>
        <div style={{ marginTop: "8px" }}>
          {navigation.map((item) => {
            const isActive = currentPath === item.href;
            const IconComponent = item.icon;
            return (
              <Link
                key={item.name}
                to={item.href}
                onClick={onNavigate}
                className={`pmc-nav-item ${isActive ? "active" : ""}`}
                style={{
                  display: "flex",
                  alignItems: "center",
                  textDecoration: "none",
                }}
              >
                <IconComponent className="pmc-nav-icon" />
                <span className="pmc-font-medium">{item.name}</span>
              </Link>
            );
          })}
        </div>
      </div>

      {/* Footer Info */}
      <div
        style={{
          marginTop: "32px",
          padding: "16px",
          borderTop: "1px solid var(--pmc-gray-200)",
          background: "var(--pmc-gray-50)",
          borderRadius: "12px",
        }}
      >
        <p
          className="pmc-text-xs pmc-font-medium"
          style={{ color: "var(--pmc-gray-600)", marginBottom: "4px" }}
        >
          Version 1.0.0
        </p>
        <p className="pmc-text-xs" style={{ color: "var(--pmc-gray-500)" }}>
          © 2025 PMC. All rights reserved.
        </p>
      </div>
    </nav>
  </div>
);
