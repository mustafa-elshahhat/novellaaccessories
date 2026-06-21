import { api, params } from "./client";
import type { PagedResult, WhatsAppMessage, WhatsAppSettings, WhatsAppStatus } from "./types";

export const whatsappApi = {
  settings: () => api.get<WhatsAppSettings>("/api/admin/whatsapp/settings"),
  updateSettings: (body: Partial<WhatsAppSettings>) => api.put<WhatsAppSettings>("/api/admin/whatsapp/settings", body),
  status: () => api.get<WhatsAppStatus>("/api/admin/whatsapp/status"),
  messages: (query: { page?: number; pageSize?: number; status?: string; type?: string }) => api.get<PagedResult<WhatsAppMessage>>(`/api/admin/whatsapp/messages${params(query)}`),
  retry: (id: string) => api.post<WhatsAppMessage>(`/api/admin/whatsapp/messages/${id}/retry`),
  test: (body: { phone: string; message?: string }) => api.post<WhatsAppMessage>("/api/admin/whatsapp/test", body)
};
