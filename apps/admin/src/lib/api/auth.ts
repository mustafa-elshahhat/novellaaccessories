import { api } from "./client";
import type { AdminLoginResponse, AdminProfile, Success } from "./types";

export const authApi = {
  login: (body: { username: string; password: string }) => api.post<AdminLoginResponse>("/api/admin/auth/login", body),
  logout: () => api.post<Success>("/api/admin/auth/logout"),
  me: () => api.get<AdminProfile>("/api/admin/auth/me")
};
