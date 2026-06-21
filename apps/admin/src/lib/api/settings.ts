import { api } from "./client";
import type { ReminderSettings, SiteSettings } from "./types";

export const settingsApi = {
  site: () => api.get<SiteSettings>("/api/admin/site-settings"),
  updateSite: (body: SiteSettings) => api.put<SiteSettings>("/api/admin/site-settings", body),
  reminders: () => api.get<ReminderSettings>("/api/admin/reminders/settings"),
  updateReminders: (body: ReminderSettings) => api.put<ReminderSettings>("/api/admin/reminders/settings", body),
  runReminders: () => api.post<{ abandonedSent: number; inactiveSent: number; skipped: number }>("/api/admin/reminders/run")
};
