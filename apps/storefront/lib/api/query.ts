import type { ProductListQuery } from "./types";

/** Builds a query string for product/category list endpoints (only defined params included). */
export function buildProductListQuery(query: ProductListQuery = {}): string {
  const params = new URLSearchParams();
  if (query.page !== undefined) params.set("page", String(query.page));
  if (query.pageSize !== undefined) params.set("pageSize", String(query.pageSize));
  if (query.search) params.set("search", query.search);
  if (query.categorySlug) params.set("categorySlug", query.categorySlug);
  if (query.featured !== undefined) params.set("featured", String(query.featured));
  if (query.hasDiscount !== undefined)
    params.set("hasDiscount", String(query.hasDiscount));
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

/** Parses search params into a typed, clamped ProductListQuery. */
export function parseProductListQuery(
  searchParams: Record<string, string | string[] | undefined>,
): ProductListQuery {
  const get = (key: string): string | undefined => {
    const v = searchParams[key];
    return Array.isArray(v) ? v[0] : v;
  };
  const pageRaw = Number(get("page"));
  const pageSizeRaw = Number(get("pageSize"));
  const page = Number.isFinite(pageRaw) && pageRaw > 0 ? Math.floor(pageRaw) : 1;
  const pageSize =
    Number.isFinite(pageSizeRaw) && pageSizeRaw > 0
      ? Math.min(Math.floor(pageSizeRaw), 100)
      : 20;
  return {
    page,
    pageSize,
    search: get("search") || undefined,
  };
}
