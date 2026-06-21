import "server-only";
import { apiFetch } from "./server";
import { buildProductListQuery } from "./query";
import type {
  Home,
  SiteSettings,
  Hero,
  PublicCategory,
  PublicProduct,
  PublicProductListItem,
  PagedResult,
  StaticPage,
  PublicGovernorate,
  ProductListQuery,
} from "./types";

const CATALOG_REVALIDATE = 60;
const CONTENT_REVALIDATE = 300;

export const getHome = () =>
  apiFetch<Home>("/api/public/home", { revalidate: CATALOG_REVALIDATE });

export const getSiteSettings = () =>
  apiFetch<SiteSettings>("/api/public/site-settings", {
    revalidate: CONTENT_REVALIDATE,
  });

export const getHeroes = () =>
  apiFetch<Hero[]>("/api/public/hero", { revalidate: CATALOG_REVALIDATE });

export const getCategories = () =>
  apiFetch<PublicCategory[]>("/api/public/categories", {
    revalidate: CATALOG_REVALIDATE,
  });

export const getCategory = (slug: string) =>
  apiFetch<PublicCategory>(
    `/api/public/categories/${encodeURIComponent(slug)}`,
    { revalidate: CATALOG_REVALIDATE },
  );

export const getCategoryProducts = (slug: string, query: ProductListQuery = {}) =>
  apiFetch<PagedResult<PublicProductListItem>>(
    `/api/public/categories/${encodeURIComponent(slug)}/products${buildProductListQuery(query)}`,
    { revalidate: CATALOG_REVALIDATE },
  );

export const getProducts = (query: ProductListQuery = {}) =>
  apiFetch<PagedResult<PublicProductListItem>>(
    `/api/public/products${buildProductListQuery(query)}`,
    { revalidate: CATALOG_REVALIDATE },
  );

export const getFeaturedProducts = () =>
  apiFetch<PublicProductListItem[]>("/api/public/products/featured", {
    revalidate: CATALOG_REVALIDATE,
  });

export const searchProducts = (term: string, query: ProductListQuery = {}) => {
  const qs = buildProductListQuery(query).replace(/^\?/, "");
  const url = `/api/public/products/search?q=${encodeURIComponent(term)}${qs ? `&${qs}` : ""}`;
  return apiFetch<PagedResult<PublicProductListItem>>(url, {
    revalidate: CATALOG_REVALIDATE,
  });
};

export const getProduct = (slug: string) =>
  apiFetch<PublicProduct>(`/api/public/products/${encodeURIComponent(slug)}`, {
    revalidate: CATALOG_REVALIDATE,
  });

export const getPage = (slug: string) =>
  apiFetch<StaticPage>(`/api/public/pages/${encodeURIComponent(slug)}`, {
    revalidate: CONTENT_REVALIDATE,
  });

export const getFaq = () =>
  apiFetch<StaticPage>("/api/public/faq", { revalidate: CONTENT_REVALIDATE });

export const getGovernorates = () =>
  apiFetch<PublicGovernorate[]>("/api/public/shipping/governorates", {
    revalidate: CONTENT_REVALIDATE,
  });
