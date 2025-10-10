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
  { name: "Applications", href: "/applications", icon: FileText },
  { name: "Documents", href: "/documents", icon: Upload },
  {
    name: "Users",
    href: "/users",
    icon: Users,
    roles: [
      "Admin",
      "JuniorEngineer",
      "AssistantEngineer",
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
        <div className="pmc-header pmc-fadeInDown">
          <div className="pmc-header-content" style={{ padding: "16px 32px" }}>
            <button
              type="button"
              className="lg:hidden pmc-button pmc-button-icon pmc-button-sm"
              style={{
                marginRight: "16px",
                background: "rgba(255, 255, 255, 0.1)",
                border: "1px solid rgba(255, 255, 255, 0.2)",
              }}
              onClick={() => setSidebarOpen(true)}
            >
              <Menu className="h-5 w-5" style={{ color: "white" }} />
            </button>

            <div className="pmc-header-brand">
              <div className="pmc-header-logo">
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
                <h1 className="pmc-header-title">PMCRMS</h1>
                <p className="pmc-header-subtitle">
                  Pune Municipal Corporation
                </p>
              </div>
            </div>

            <div className="pmc-header-actions">
              {/* Notification Bell */}
              <NotificationBell />

              {/* Profile section */}
              <div className="pmc-user-menu">
                <div className="pmc-user-avatar">
                  {user?.name?.charAt(0).toUpperCase() || "U"}
                </div>
                <div className="pmc-user-info">
                  <p className="pmc-user-name">{user?.name || "User"}</p>
                  <p className="pmc-user-role">{user?.role || "Member"}</p>
                </div>
              </div>

              {/* Logout button */}
              <button
                onClick={handleLogout}
                className="pmc-button pmc-button-danger pmc-button-sm pmc-button-icon"
                style={{ background: "rgba(220, 38, 38, 0.9)" }}
                title="Logout"
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
