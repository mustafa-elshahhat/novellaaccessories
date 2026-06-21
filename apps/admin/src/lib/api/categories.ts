import { api } from "./client";
import type { Category, Success } from "./types";

export const categoriesApi = {
  list: () => api.get<Category[]>("/api/admin/categories"),
  get: (id: string) => api.get<Category>(`/api/admin/categories/${id}`),
  create: (body: Partial<Category>) => api.post<Category>("/api/admin/categories", body),
  update: (id: string, body: Partial<Category>) => api.put<Category>(`/api/admin/categories/${id}`, body),
  remove: (id: string) => api.delete<Success>(`/api/admin/categories/${id}`),
  status: (id: string, isActive: boolean) => api.patch<Success>(`/api/admin/categories/${id}/status`, { isActive }),
  reorder: (items: { id: string; sortOrder: number }[]) => api.patch<Success>("/api/admin/categories/reorder", { items })
};
