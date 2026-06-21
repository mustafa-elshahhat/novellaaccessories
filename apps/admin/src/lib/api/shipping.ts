import { api } from "./client";
import type { Governorate, Success } from "./types";

export const shippingApi = {
  list: () => api.get<Governorate[]>("/api/admin/shipping/governorates"),
  create: (body: Partial<Governorate>) => api.post<Governorate>("/api/admin/shipping/governorates", body),
  update: (id: string, body: Partial<Governorate>) => api.put<Governorate>(`/api/admin/shipping/governorates/${id}`, body),
  status: (id: string, isActive: boolean) => api.patch<Success>(`/api/admin/shipping/governorates/${id}/status`, { isActive })
};
