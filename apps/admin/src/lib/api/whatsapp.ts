import { api, params } from "./client";
import type { PagedResult, ReminderSettings, WhatsAppHealth, WhatsAppMessage, WhatsAppQr, WhatsAppSettings, WhatsAppStatus } from "./types";

export const whatsappApi = {
  settings: () => api.get<WhatsAppSettings>("/api/admin/whatsapp/settings"),
  updateSettings: (body: Partial<WhatsAppSettings>) => api.put<WhatsAppSettings>("/api/admin/whatsapp/settings", body),
  status: () => api.get<WhatsAppStatus>("/api/admin/whatsapp/status"),
  qr: () => api.get<WhatsAppQr>("/api/admin/whatsapp/qr"),
  health: () => api.get<WhatsAppHealth>("/api/admin/whatsapp/health"),
  logout: () => api.post<WhatsAppStatus>("/api/admin/whatsapp/logout"),
  resetSession: () => api.post<WhatsAppStatus>("/api/admin/whatsapp/reset-session"),
  automations: () => api.get<ReminderSettings>("/api/admin/whatsapp/automations"),
  updateAutomations: (body: ReminderSettings) => api.put<ReminderSettings>("/api/admin/whatsapp/automations", body),
  messages: (query: { page?: number; pageSize?: number; status?: string; type?: string }) => api.get<PagedResult<WhatsAppMessage>>(`/api/admin/whatsapp/messages${params(query)}`),
  retry: (id: string) => api.post<WhatsAppMessage>(`/api/admin/whatsapp/messages/${id}/retry`),
  test: (body: { phone: string; message?: string }) => api.post<WhatsAppMessage>("/api/admin/whatsapp/test", body)
};
