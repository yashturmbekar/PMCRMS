/* eslint-disable @typescript-eslint/no-explicit-any */
import axios from "axios";
import {
  API_TIMEOUT,
  AUTH_TOKEN_KEY,
  UNAUTH_ROUTES,
  API_BASE_URL,
  FILE_UPLOAD_TIMEOUT,
} from "../constants";

// ==================== CONFIGURATION ====================

/**
 * Get the base URL for API requests
 * Centralized configuration to avoid hardcoding URLs across the application
 */
export function getApiBaseUrl(): string {
  return API_BASE_URL;
}

/**
 * Get the full API URL with /api suffix
 */
export function getApiUrl(): string {
  return `${API_BASE_URL}/api`;
}

// ==================== TOKEN MANAGEMENT ====================

/**
 * Retrieve authentication token from local storage
 */
export function getToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_KEY);
}

/**
 * Store authentication token in local storage
 */
export function setToken(token: string): void {
  localStorage.setItem(AUTH_TOKEN_KEY, token);
}

/**
 * Clear authentication token and user data from local storage
 */
export function removeToken(): void {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem("pmcrms_user");
}

// ==================== AXIOS INSTANCE CONFIGURATION ====================

const controllers: AbortController[] = [];

/**
 * Create axios instance with centralized configuration
 * All API requests should use this instance
 */
const instance = axios.create({
  baseURL: getApiUrl(),
  timeout: API_TIMEOUT,
  headers: {
    "Content-Type": "application/json",
  },
});

// ==================== REQUEST INTERCEPTOR ====================

instance.interceptors.request.use(
  (config: any) => {
    const url = config.url ?? "";
    const isTokenRequired = !UNAUTH_ROUTES.includes(url);
    const token = getToken();

    const controller = new AbortController();
    controllers.push(controller);

    return {
      ...config,
      signal: controller.signal,
      timeout: config.timeout || API_TIMEOUT,
      headers: {
        ...config.headers,
        ...(isTokenRequired && token
          ? { Authorization: `Bearer ${token}` }
          : {}),
      },
    };
  },
  (error) => Promise.reject(error)
);

// ==================== RESPONSE INTERCEPTOR ====================

instance.interceptors.response.use(
  (response: any) => Promise.resolve(response),
  (error) => {
    if (error?.response?.status === 401 || error?.response?.status === 403) {
      // Don't redirect on login/auth endpoints - let the login page handle the error
      const url = error?.config?.url ?? "";
      const isAuthEndpoint =
        url.includes("/auth/login") ||
        url.includes("/auth/officer-login") ||
        url.includes("/auth/admin-login") ||
        url.includes("/auth/send-otp") ||
        url.includes("/auth/verify-otp");

      if (isAuthEndpoint) {
        // Let the login page handle the error display
        return Promise.reject(error);
      }

      // Get user info before clearing
      const userStr = localStorage.getItem("pmcrms_user");
      const user = userStr ? JSON.parse(userStr) : null;
      // Regular users/applicants have role "Applicant", all other roles are officers
      const isOfficer = user && user.role !== "Applicant";

      removeToken();
      controllers.forEach((controller) => controller.abort());
      controllers.length = 0;

      // Redirect to appropriate login page based on user role
      window.location.href = isOfficer ? "/officer-login" : "/login";
    }
    return Promise.reject(error);
  }
);

// ==================== API CLIENT METHODS ====================

/**
 * Centralized API client with standardized HTTP methods
 * All application services should use this client for API requests
 */
const apiClient = {
  /**
   * Perform GET request
   */
  get: async (url: string, config = {}): Promise<any> => {
    return (await instance.get(url, config)).data;
  },

  /**
   * Perform POST request
   */
  post: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.post(url, data, config)).data;
  },

  /**
   * Specialized method for file uploads with extended timeout
   */
  postWithFiles: async (url: string, data = {}, config = {}): Promise<any> => {
    const uploadConfig = {
      ...config,
      timeout: FILE_UPLOAD_TIMEOUT,
      headers: {
        "Content-Type": "multipart/form-data",
        ...(config as any).headers,
      },
    };
    return (await instance.post(url, data, uploadConfig)).data;
  },

  /**
   * Perform PUT request
   */
  put: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.put(url, data, config)).data;
  },

  /**
   * Perform PATCH request
   */
  patch: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.patch(url, data, config)).data;
  },

  /**
   * Perform DELETE request
   */
  delete: async (url: string, config = {}): Promise<any> => {
    return (await instance.delete(url, config)).data;
  },
};

export default apiClient;
