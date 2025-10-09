// API Configuration
export const API_TIMEOUT = 30000; // 30 seconds
export const FILE_UPLOAD_TIMEOUT = 300000; // 5 minutes for file uploads

// Authentication
export const AUTH_TOKEN_KEY = "pmcrms_token";
export const AUTH_USER_KEY = "pmcrms_user";

// Routes that don't require authentication
export const UNAUTH_ROUTES = [
  "/auth/send-otp",
  "/auth/verify-otp",
  "/auth/login",
  "/auth/register",
];

// API Base URL
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5086";

// Default pagination
export const DEFAULT_PAGE_SIZE = 10;
export const DEFAULT_PAGE = 1;
