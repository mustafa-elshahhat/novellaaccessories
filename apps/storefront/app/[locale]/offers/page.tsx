import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getProducts } from "@/lib/api/public";
import type { PagedResult, PublicProductListItem } from "@/lib/api/types";
import { buildPublicMetadata, absoluteUrl } from "@/lib/seo/metadata";
import { JsonLd, collectionJsonLd } from "@/lib/seo/jsonld";
import { ProductGrid } from "@/components/ui/product-grid";
import { Pagination } from "@/components/ui/pagination";
import { EmptyState } from "@/components/ui/states";

type PageProps = {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

function parsePage(value: string | string[] | undefined): number {
  const n = Number(Array.isArray(value) ? value[0] : value);
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : 1;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "offers" });
  return buildPublicMetadata({
    locale,
    title: t("title"),
    description: t("subtitle"),
    pathAr: "/offers",
    pathEn: "/offers",
  });
}

export default async function OffersPage({ params, searchParams }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const sp = await searchParams;
  const page = parsePage(sp.page);
  const t = await getTranslations("offers");
  const te = await getTranslations("empty");

  // Server-side active-discount filter (backend ProductListQuery.hasDiscount — see backend change #2).
  let result: PagedResult<PublicProductListItem> | null = null;
  try {
    result = await getProducts({ hasDiscount: true, page, pageSize: 20 });
  } catch {
    result = null;
  }
  const products = result?.items ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <JsonLd
        data={collectionJsonLd({
          name: t("title"),
          url: absoluteUrl(`/${locale}/offers`),
          description: t("subtitle"),
        })}
      />
      <h1 className="mb-2 text-2xl font-semibold text-deepbrown">{t("title")}</h1>
      <p className="mb-6 text-taupe">{t("subtitle")}</p>
      {products.length === 0 ? (
        <EmptyState title={te("noProducts")} />
      ) : (
        <>
          <ProductGrid products={products} />
          {result && (
            <Pagination
              currentPage={result.page}
              totalPages={result.totalPages}
              createHref={(p) => `/offers?page=${p}`}
            />
          )}
        </>
      )}
    </div>
  );
}
