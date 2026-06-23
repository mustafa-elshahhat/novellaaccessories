import "server-only";
import { tryApiFetch } from "./server";
import type { SitemapData } from "./types";

// Sitemap data is the only backend SEO read. Per-entity metadata (title/description/canonical/
// hreflang/JSON-LD) is generated automatically from the normal Product, Category, and Page APIs.
export const getSitemapData = () =>
  tryApiFetch<SitemapData>("/api/public/seo/sitemap-data", { revalidate: 300 });
