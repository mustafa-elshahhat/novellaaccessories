import { api, params } from "./client";
import type { Expense, Success } from "./types";

export const expensesApi = {
  list: (query: { from?: string; to?: string; category?: string }) => api.get<Expense[]>(`/api/admin/expenses${params(query)}`),
  get: (id: string) => api.get<Expense>(`/api/admin/expenses/${id}`),
  create: (body: Partial<Expense>) => api.post<Expense>("/api/admin/expenses", body),
  update: (id: string, body: Partial<Expense>) => api.put<Expense>(`/api/admin/expenses/${id}`, body),
  remove: (id: string) => api.delete<Success>(`/api/admin/expenses/${id}`)
};
