import React, { createContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { apiService } from "../services/apiService";
import type {
  User,
  AuthResponse,
  LoginRequest,
  OtpVerificationRequest,
} from "../types";

export interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<AuthResponse>;
  register: (userData: Partial<User>) => Promise<AuthResponse>;
  sendOtp: (phoneNumber: string) => Promise<void>;
  verifyOtp: (data: OtpVerificationRequest) => Promise<AuthResponse>;
  logout: () => void;
  updateProfile: (userData: Partial<User>) => Promise<User>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if user is logged in on app start
    const initializeAuth = () => {
      try {
        const currentUser = apiService.getCurrentUser();
        if (currentUser && apiService.isAuthenticated()) {
          setUser(currentUser);
        }
      } catch (error) {
        console.error("Error initializing auth:", error);
        apiService.logout();
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  const login = async (data: LoginRequest): Promise<AuthResponse> => {
    try {
      setIsLoading(true);
      const response = await apiService.login(data);
      setUser(response.user);
      return response;
    } catch (error) {
      console.error("Login error:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (userData: Partial<User>): Promise<AuthResponse> => {
    try {
      setIsLoading(true);
      const response = await apiService.register(userData);
      setUser(response.user);
      return response;
    } catch (error) {
      console.error("Registration error:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const sendOtp = async (phoneNumber: string): Promise<void> => {
    try {
      await apiService.sendOtp(phoneNumber);
    } catch (error) {
      console.error("Send OTP error:", error);
      throw error;
    }
  };

  const verifyOtp = async (
    data: OtpVerificationRequest
  ): Promise<AuthResponse> => {
    try {
      setIsLoading(true);
      const response = await apiService.verifyOtp(data);
      setUser(response.user);
      return response;
    } catch (error) {
      console.error("OTP verification error:", error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = (): void => {
    apiService.logout();
    setUser(null);
  };

  const updateProfile = async (userData: Partial<User>): Promise<User> => {
    try {
      const updatedUser = await apiService.updateUserProfile(userData);
      setUser(updatedUser);
      return updatedUser;
    } catch (error) {
      console.error("Update profile error:", error);
      throw error;
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    sendOtp,
    verifyOtp,
    logout,
    updateProfile,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export default AuthContext;
