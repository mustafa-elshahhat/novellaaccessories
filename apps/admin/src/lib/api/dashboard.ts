import { api } from "./client";
import type { DashboardAlert, DashboardSummary, OrderListItem } from "./types";

export const dashboardApi = {
  summary: () => api.get<DashboardSummary>("/api/admin/dashboard/summary"),
  recentOrders: (take = 10) => api.get<OrderListItem[]>(`/api/admin/dashboard/recent-orders?take=${take}`),
  alerts: () => api.get<DashboardAlert[]>("/api/admin/dashboard/alerts")
};
