/**
 * PMCRMS Application Constants
 * Centralized configuration values for the application
 */

// ==================== API CONFIGURATION ====================

/**
 * API timeout for standard requests (30 seconds)
 */
export const API_TIMEOUT = 30000;

/**
 * Extended timeout for file upload operations (5 minutes)
 */
export const FILE_UPLOAD_TIMEOUT = 300000;

/**
 * Base URL for API requests
 * Uses environment variable or defaults to local development server
 */
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5086";

// ==================== AUTHENTICATION ====================

/**
 * Local storage key for authentication token
 */
export const AUTH_TOKEN_KEY = "pmcrms_token";

/**
 * Local storage key for user data
 */
export const AUTH_USER_KEY = "pmcrms_user";

/**
 * API routes that don't require authentication
 */
export const UNAUTH_ROUTES = [
  "/auth/send-otp",
  "/auth/verify-otp",
  "/auth/login",
  "/auth/officer-login",
  "/auth/register",
];

// ==================== PAGINATION ====================

/**
 * Default number of items per page
 */
export const DEFAULT_PAGE_SIZE = 10;

/**
 * Default starting page number
 */
export const DEFAULT_PAGE = 1;
