/* eslint-disable @typescript-eslint/no-explicit-any */
import axios from "axios";
import {
  API_TIMEOUT,
  AUTH_TOKEN_KEY,
  UNAUTH_ROUTES,
  API_BASE_URL,
  FILE_UPLOAD_TIMEOUT,
} from "../constants";

// Token helpers
export function getToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(AUTH_TOKEN_KEY, token);
}

export function removeToken(): void {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem("pmcrms_user");
}

const controllers: AbortController[] = [];

const instance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  timeout: API_TIMEOUT,
  headers: {
    "Content-Type": "application/json",
  },
});

// --- Request Interceptor ---
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

// --- Response Interceptor ---
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

// --- API Methods ---
const apiClient = {
  get: async (url: string, config = {}): Promise<any> => {
    return (await instance.get(url, config)).data;
  },
  post: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.post(url, data, config)).data;
  },
  // Specialized method for file uploads with extended timeout
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
  put: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.put(url, data, config)).data;
  },
  patch: async (url: string, data = {}, config = {}): Promise<any> => {
    return (await instance.patch(url, data, config)).data;
  },
  delete: async (url: string, config = {}): Promise<any> => {
    return (await instance.delete(url, config)).data;
  },
};

export default apiClient;
