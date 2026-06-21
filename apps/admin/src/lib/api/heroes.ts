import { api } from "./client";
import type { Hero, Success } from "./types";

export const heroesApi = {
  list: () => api.get<Hero[]>("/api/admin/heroes"),
  create: (body: Partial<Hero>) => api.post<Hero>("/api/admin/heroes", body),
  update: (id: string, body: Partial<Hero>) => api.put<Hero>(`/api/admin/heroes/${id}`, body),
  remove: (id: string) => api.delete<Success>(`/api/admin/heroes/${id}`),
  status: (id: string, isActive: boolean) => api.patch<Success>(`/api/admin/heroes/${id}/status`, { isActive }),
  reorder: (items: { id: string; sortOrder: number }[]) => api.patch<Success>("/api/admin/heroes/reorder", { items })
};
