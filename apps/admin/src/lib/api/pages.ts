import { api } from "./client";
import type { StaticPage } from "./types";

export const pagesApi = {
  list: () => api.get<StaticPage[]>("/api/admin/pages"),
  get: (id: string) => api.get<StaticPage>(`/api/admin/pages/${id}`),
  update: (id: string, body: Partial<StaticPage>) => api.put<StaticPage>(`/api/admin/pages/${id}`, body)
};
