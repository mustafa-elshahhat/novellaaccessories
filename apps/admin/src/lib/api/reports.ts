import { api, params } from "./client";
import type { AnalyticsReport, Expense, ProfitReport, RowReport, SalesReport } from "./types";

export type ReportQuery = { range: "Today" | "ThisWeek" | "ThisMonth" | "Custom"; from?: string; to?: string };

export const reportsApi = {
  sales: (query: ReportQuery) => api.get<SalesReport>(`/api/admin/reports/sales${params(query)}`),
  profit: (query: ReportQuery) => api.get<ProfitReport>(`/api/admin/reports/profit${params(query)}`),
  products: (query: ReportQuery) => api.get<RowReport[]>(`/api/admin/reports/products${params(query)}`),
  categories: (query: ReportQuery) => api.get<RowReport[]>(`/api/admin/reports/categories${params(query)}`),
  coupons: (query: ReportQuery) => api.get<RowReport[]>(`/api/admin/reports/coupons${params(query)}`),
  payments: (query: ReportQuery) => api.get<RowReport[]>(`/api/admin/reports/payments${params(query)}`),
  governorates: (query: ReportQuery) => api.get<RowReport[]>(`/api/admin/reports/governorates${params(query)}`),
  analytics: (query: ReportQuery) => api.get<AnalyticsReport>(`/api/admin/reports/analytics${params(query)}`),
  expenses: (query: ReportQuery) => api.get<Expense[]>(`/api/admin/reports/expenses${params(query)}`)
};
