import "server-only";
import { tryApiFetch } from "./server";
import type { SitemapData, ProductSeo, SeoMetadata } from "./types";

// SEO reads are graceful (null on failure) so metadata generation never breaks a page.
export const getSitemapData = () =>
  tryApiFetch<SitemapData>("/api/public/seo/sitemap-data", { revalidate: 300 });

export const getProductSeo = (slug: string) =>
  tryApiFetch<ProductSeo>(
    `/api/public/seo/product/${encodeURIComponent(slug)}`,
    { revalidate: 300 },
  );

export const getCategorySeo = (slug: string) =>
  tryApiFetch<SeoMetadata>(
    `/api/public/seo/category/${encodeURIComponent(slug)}`,
    { revalidate: 300 },
  );

export const getPageSeo = (slug: string) =>
  tryApiFetch<SeoMetadata>(`/api/public/seo/page/${encodeURIComponent(slug)}`, {
    revalidate: 300,
  });
