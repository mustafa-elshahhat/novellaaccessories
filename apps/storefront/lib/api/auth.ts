import "server-only";
import { apiFetch } from "./server";
import type { AuthTokenResponse, RegisterResponse } from "./types";

export interface RegisterBody {
  fullName: string;
  phoneNumber: string;
  password: string;
}
export interface LoginBody {
  phoneNumber: string;
  password: string;
}
export interface VerifyPhoneBody {
  phoneNumber: string;
  code: string;
}
export interface ForgotPasswordBody {
  phoneNumber: string;
}
export interface ResetPasswordBody {
  phoneNumber: string;
  code: string;
  newPassword: string;
}
export interface ChangePhoneBody {
  newPhoneNumber: string;
}
export interface ChangePhoneVerifyBody {
  newPhoneNumber: string;
  code: string;
}

export const register = (body: RegisterBody) =>
  apiFetch<RegisterResponse>("/api/auth/register", { method: "POST", body });

export const verifyPhone = (body: VerifyPhoneBody) =>
  apiFetch<AuthTokenResponse>("/api/auth/verify-phone", { method: "POST", body });

export const login = (body: LoginBody) =>
  apiFetch<AuthTokenResponse>("/api/auth/login", { method: "POST", body });

export const me = () =>
  apiFetch<AuthTokenResponse>("/api/auth/me", { auth: true });

export const logout = () =>
  apiFetch<{ success: boolean }>("/api/auth/logout", {
    method: "POST",
    auth: true,
  });

export const forgotPasswordRequest = (body: ForgotPasswordBody) =>
  apiFetch<{ success: boolean }>("/api/auth/forgot-password/request-otp", {
    method: "POST",
    body,
  });

export const forgotPasswordReset = (body: ResetPasswordBody) =>
  apiFetch<{ success: boolean }>("/api/auth/forgot-password/reset", {
    method: "POST",
    body,
  });

export const changePhoneRequest = (body: ChangePhoneBody) =>
  apiFetch<{ success: boolean }>("/api/auth/change-phone/request-otp", {
    method: "POST",
    auth: true,
    body,
  });

export const changePhoneVerify = (body: ChangePhoneVerifyBody) =>
  apiFetch<{ success: boolean }>("/api/auth/change-phone/verify", {
    method: "POST",
    auth: true,
    body,
  });
