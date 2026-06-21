import { bff } from "@/lib/api/bff-client";
import type { CustomerProfile } from "@/lib/api/types";

const json = (body: unknown): RequestInit => ({
  method: "POST",
  body: JSON.stringify(body),
});

export const apiRegister = (body: {
  fullName: string;
  phoneNumber: string;
  password: string;
}) =>
  bff<{ requiresVerification: boolean; phoneNumber: string }>(
    "/api/auth/register",
    json(body),
  );

export const apiVerifyPhone = (body: { phoneNumber: string; code: string }) =>
  bff<{ customer: CustomerProfile }>("/api/auth/verify-phone", json(body));

export const apiLogin = (body: { phoneNumber: string; password: string }) =>
  bff<{ customer: CustomerProfile }>("/api/auth/login", json(body));

export const apiForgotRequest = (body: { phoneNumber: string }) =>
  bff<{ success: boolean }>("/api/auth/forgot-password/request-otp", json(body));

export const apiForgotReset = (body: {
  phoneNumber: string;
  code: string;
  newPassword: string;
}) => bff<{ success: boolean }>("/api/auth/forgot-password/reset", json(body));

export const apiChangePhoneRequest = (body: { newPhoneNumber: string }) =>
  bff<{ success: boolean }>("/api/auth/change-phone/request-otp", json(body));

export const apiChangePhoneVerify = (body: {
  newPhoneNumber: string;
  code: string;
}) => bff<{ success: boolean }>("/api/auth/change-phone/verify", json(body));
