import { api } from "./client";
import type { AdminLoginResponse, AdminProfile, Success } from "./types";

export const authApi = {
  login: (body: { username: string; password: string }) => api.post<AdminLoginResponse>("/api/admin/auth/login", body),
  logout: () => api.post<Success>("/api/admin/auth/logout"),
  me: async () => {
    const admin = await api.get<Omit<AdminProfile, "role">>("/api/admin/auth/me");
    return { ...admin, role: "Admin" as const };
  }
};
