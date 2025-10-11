import {
  Routes,
  Route,
  Navigate,
  BrowserRouter as Router,
} from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import OfficerLoginPage from "./pages/OfficerLoginPage";
import Dashboard from "./pages/Dashboard";
import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminApplicationsPage from "./pages/admin/AdminApplicationsPage";
import OfficerManagementPage from "./pages/admin/OfficerManagementPage";
import FormManagementPage from "./pages/admin/FormManagementPage";
import { PositionRegistrationPage } from "./pages/PositionRegistrationPage";
import ViewPositionApplication from "./pages/ViewPositionApplication";
import LoaderShowcase from "./components/LoaderShowcase";
import { AuthProvider } from "./contexts/AuthContext";
import { NotificationProvider } from "./contexts/NotificationContext";
import { useAuth } from "./hooks/useAuth";
import { Layout } from "./components/Layout";
import { PageLoader } from "./components";
import "./index.css";

// Protected route component with admin redirect
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <PageLoader message="Authenticating..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  // Redirect admins to admin dashboard
  if (user.role === "Admin" && window.location.pathname === "/dashboard") {
    return <Navigate to="/admin" replace />;
  }

  return <>{children}</>;
};

// Admin-only protected route
const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <PageLoader message="Authenticating..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (user.role !== "Admin") {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
};

// Smart redirect component for root path
const DefaultRedirect: React.FC = () => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <PageLoader message="Loading..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  // Redirect based on user role
  if (user.role === "Admin") {
    return <Navigate to="/admin" replace />;
  }

  return <Navigate to="/dashboard" replace />;
};

function App() {
  return (
    <AuthProvider>
      <NotificationProvider>
        <Router>
          <div className="App">
            <Routes>
              {/* Public routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/officer-login" element={<OfficerLoginPage />} />

              {/* Loader Showcase - For demo purposes */}
              <Route path="/loaders" element={<LoaderShowcase />} />

              {/* Admin routes with Layout */}
              <Route
                path="/admin"
                element={
                  <AdminRoute>
                    <Layout>
                      <AdminDashboard />
                    </Layout>
                  </AdminRoute>
                }
              />
              <Route
                path="/admin/applications"
                element={
                  <AdminRoute>
                    <Layout>
                      <AdminApplicationsPage />
                    </Layout>
                  </AdminRoute>
                }
              />
              <Route
                path="/admin/applications/:id"
                element={
                  <AdminRoute>
                    <Layout>
                      <ViewPositionApplication />
                    </Layout>
                  </AdminRoute>
                }
              />
              <Route
                path="/admin/officers"
                element={
                  <AdminRoute>
                    <Layout>
                      <OfficerManagementPage />
                    </Layout>
                  </AdminRoute>
                }
              />
              <Route
                path="/admin/forms"
                element={
                  <AdminRoute>
                    <Layout>
                      <FormManagementPage />
                    </Layout>
                  </AdminRoute>
                }
              />

              {/* Protected routes with Layout */}
              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <Dashboard />
                    </Layout>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/register/:positionType"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <PositionRegistrationPage />
                    </Layout>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/register/:positionType/:applicationId"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <PositionRegistrationPage />
                    </Layout>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/application/:id"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <ViewPositionApplication />
                    </Layout>
                  </ProtectedRoute>
                }
              />
              {/* Legacy route for backwards compatibility */}
              <Route
                path="/register-structural-engineer"
                element={
                  <ProtectedRoute>
                    <Layout>
                      <PositionRegistrationPage />
                    </Layout>
                  </ProtectedRoute>
                }
              />

              {/* Default redirect - role-based */}
              <Route path="/" element={<DefaultRedirect />} />

              {/* 404 fallback */}
              <Route
                path="*"
                element={
                  <div className="min-h-screen flex items-center justify-center bg-gray-50">
                    <div className="text-center pmc-fadeIn">
                      <div className="mb-8">
                        <div className="w-24 h-24 mx-auto mb-4 bg-blue-100 rounded-full flex items-center justify-center">
                          <svg
                            className="w-12 h-12 text-blue-600"
                            fill="currentColor"
                            viewBox="0 0 20 20"
                          >
                            <path
                              fillRule="evenodd"
                              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                              clipRule="evenodd"
                            />
                          </svg>
                        </div>
                        <h1 className="text-4xl font-bold text-gray-900 mb-2">
                          404
                        </h1>
                        <p className="text-gray-600 mb-6">Page not found</p>
                      </div>
                      <a
                        href="/dashboard"
                        className="pmc-button pmc-button-primary"
                      >
                        <svg
                          className="w-4 h-4 mr-2"
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path
                            fillRule="evenodd"
                            d="M9.707 14.707a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 1.414L7.414 9H15a1 1 0 110 2H7.414l2.293 2.293a1 1 0 010 1.414z"
                            clipRule="evenodd"
                          />
                        </svg>
                        Back to Dashboard
                      </a>
                    </div>
                  </div>
                }
              />
            </Routes>
          </div>
        </Router>
      </NotificationProvider>
    </AuthProvider>
  );
}

export default App;
