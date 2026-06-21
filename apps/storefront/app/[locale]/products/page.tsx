import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getProducts, searchProducts } from "@/lib/api/public";
import type { PagedResult, PublicProductListItem } from "@/lib/api/types";
import { buildPublicMetadata, NOINDEX } from "@/lib/seo/metadata";
import { ProductGrid } from "@/components/ui/product-grid";
import { Pagination } from "@/components/ui/pagination";
import { EmptyState } from "@/components/ui/states";
import { SearchBar } from "@/features/catalog/search-bar";

type PageProps = {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
function parsePage(value: string | string[] | undefined): number {
  const n = Number(first(value));
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : 1;
}

export async function generateMetadata({ params, searchParams }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  const sp = await searchParams;
  const q = first(sp.q)?.trim();
  const t = await getTranslations({ locale, namespace: "products" });
  const base = buildPublicMetadata({
    locale,
    title: q ? t("searchResults", { query: q }) : t("title"),
    pathAr: "/products",
    pathEn: "/products",
  });
  return q ? { ...base, robots: NOINDEX } : base;
}

export default async function ProductsPage({ params, searchParams }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const sp = await searchParams;
  const q = first(sp.q)?.trim();
  const page = parsePage(sp.page);
  const t = await getTranslations("products");
  const te = await getTranslations("empty");

  let result: PagedResult<PublicProductListItem> | null = null;
  try {
    result = q
      ? await searchProducts(q, { page, pageSize: 20 })
      : await getProducts({ page, pageSize: 20 });
  } catch {
    result = null;
  }
  const products = result?.items ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <SearchBar initialQuery={q ?? ""} />
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">
        {q ? t("searchResults", { query: q }) : t("title")}
      </h1>
      {products.length === 0 ? (
        <EmptyState title={q ? te("noResults") : te("noProducts")} />
      ) : (
        <>
          <ProductGrid products={products} />
          {result && (
            <Pagination
              currentPage={result.page}
              totalPages={result.totalPages}
              createHref={(p) =>
                q
                  ? `/products?q=${encodeURIComponent(q)}&page=${p}`
                  : `/products?page=${p}`
              }
            />
          )}
        </>
      )}
    </div>
  );
}
