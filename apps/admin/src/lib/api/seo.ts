import { api } from "./client";
import type { SeoMetadata } from "./types";

export const seoApi = {
  content: () => api.get<SeoMetadata[]>("/api/admin/seo/content")
};
