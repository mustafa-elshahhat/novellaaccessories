import { api } from "./client";
import type { SeoMetadata } from "./types";

export const seoApi = {
  content: () => api.get<SeoMetadata[]>("/api/admin/seo/content"),
  update: (entityType: string, entityId: string, body: Partial<SeoMetadata>) => api.put<SeoMetadata>(`/api/admin/seo/content/${entityType}/${entityId}`, body)
};
