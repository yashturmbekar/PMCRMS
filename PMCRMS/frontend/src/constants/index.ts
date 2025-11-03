/**
 * PMCRMS Application Constants
 * Centralized configuration values for the application
 */

// ==================== API CONFIGURATION ====================

/**
 * API timeout for standard requests (30 seconds)
 * Can be overridden via VITE_API_TIMEOUT environment variable
 */
export const API_TIMEOUT = parseInt(
  import.meta.env.VITE_API_TIMEOUT || "30000",
  10
);

/**
 * Extended timeout for file upload operations (5 minutes)
 * Can be overridden via VITE_FILE_UPLOAD_TIMEOUT environment variable
 */
export const FILE_UPLOAD_TIMEOUT = parseInt(
  import.meta.env.VITE_FILE_UPLOAD_TIMEOUT || "300000",
  10
);

/**
 * Base URL for API requests
 * Uses environment variable or throws error if not configured
 */
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ||
  (() => {
    throw new Error("VITE_API_BASE_URL environment variable is not configured");
  })();

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
 * Can be overridden via VITE_DEFAULT_PAGE_SIZE environment variable
 */
export const DEFAULT_PAGE_SIZE = parseInt(
  import.meta.env.VITE_DEFAULT_PAGE_SIZE || "10",
  10
);

/**
 * Default starting page number
 */
export const DEFAULT_PAGE = 1;

// ==================== UI TIMEOUTS ====================

/**
 * Success message auto-dismiss timeout (3 seconds)
 * Can be overridden via VITE_SUCCESS_MESSAGE_TIMEOUT environment variable
 */
export const SUCCESS_MESSAGE_TIMEOUT = parseInt(
  import.meta.env.VITE_SUCCESS_MESSAGE_TIMEOUT || "3000",
  10
);

/**
 * Error message auto-dismiss timeout (5 seconds)
 * Can be overridden via VITE_ERROR_MESSAGE_TIMEOUT environment variable
 */
export const ERROR_MESSAGE_TIMEOUT = parseInt(
  import.meta.env.VITE_ERROR_MESSAGE_TIMEOUT || "5000",
  10
);

/**
 * Extended error message timeout (7 seconds)
 * Can be overridden via VITE_EXTENDED_ERROR_TIMEOUT environment variable
 */
export const EXTENDED_ERROR_TIMEOUT = parseInt(
  import.meta.env.VITE_EXTENDED_ERROR_TIMEOUT || "7000",
  10
);

/**
 * Redirect delay timeout (1.5 seconds)
 * Can be overridden via VITE_REDIRECT_DELAY environment variable
 */
export const REDIRECT_DELAY = parseInt(
  import.meta.env.VITE_REDIRECT_DELAY || "1500",
  10
);

/**
 * Notification polling interval (30 seconds)
 * Can be overridden via VITE_NOTIFICATION_POLL_INTERVAL environment variable
 */
export const NOTIFICATION_POLL_INTERVAL = parseInt(
  import.meta.env.VITE_NOTIFICATION_POLL_INTERVAL || "30000",
  10
);

/**
 * Maximum certificate polling attempts (40 attempts = 2 minutes at 3 seconds each)
 * Can be overridden via VITE_MAX_POLL_ATTEMPTS environment variable
 */
export const MAX_POLL_ATTEMPTS = parseInt(
  import.meta.env.VITE_MAX_POLL_ATTEMPTS || "40",
  10
);

// ==================== ADMIN CONFIGURATION ====================

/**
 * Admin email addresses for login
 * Can be overridden via VITE_ADMIN_EMAILS environment variable (comma-separated)
 */
export const ADMIN_EMAILS = (
  import.meta.env.VITE_ADMIN_EMAILS || "admin@gmail.com,pmc@mailinator.com"
)
  .split(",")
  .map((email: string) => email.trim());
