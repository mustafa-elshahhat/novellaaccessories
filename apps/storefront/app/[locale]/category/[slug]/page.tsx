import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick, pickSlug } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import { getCategory, getCategoryProducts } from "@/lib/api/public";
import type { PublicCategory } from "@/lib/api/types";
import { ApiError } from "@/lib/api/errors";
import { buildPublicMetadata } from "@/lib/seo/metadata";
import { JsonLd, breadcrumbJsonLd, collectionJsonLd } from "@/lib/seo/jsonld";
import { absoluteUrl } from "@/lib/seo/metadata";
import { ProductGrid } from "@/components/ui/product-grid";
import { Pagination } from "@/components/ui/pagination";
import { Breadcrumb } from "@/components/ui/breadcrumb";
import { EmptyState } from "@/components/ui/states";
import { ContentBlock } from "@/components/ui/content-block";

type PageProps = {
  params: Promise<{ locale: string; slug: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

function parsePage(value: string | string[] | undefined): number {
  const n = Number(Array.isArray(value) ? value[0] : value);
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : 1;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale, slug } = await params;
  let category: PublicCategory | null = null;
  try {
    category = await getCategory(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return {};
    throw error;
  }
  if (!category) return {};
  const title =
    pick(locale, category.seoTitleAr, category.seoTitleEn) ||
    pick(locale, category.nameAr, category.nameEn);
  const description = pick(locale, category.seoDescriptionAr, category.seoDescriptionEn);
  return buildPublicMetadata({
    locale,
    title,
    description: description || undefined,
    pathAr: `/category/${category.slugAr}`,
    pathEn: `/category/${category.slugEn}`,
    images: category.imageUrl ? [category.imageUrl] : undefined,
  });
}

export default async function CategoryPage({ params, searchParams }: PageProps) {
  const { locale, slug } = await params;
  setRequestLocale(locale);
  const sp = await searchParams;
  const page = parsePage(sp.page);

  let category: PublicCategory | null = null;
  try {
    category = await getCategory(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  const tNav = await getTranslations("nav");
  const tCat = await getTranslations("categories");
  const te = await getTranslations("empty");
  const ta = await getTranslations("a11y");

  const localizedSlug = pickSlug(locale as Locale, category.slugAr, category.slugEn);
  const name = pick(locale as Locale, category.nameAr, category.nameEn);

  const result = await getCategoryProducts(localizedSlug, { page, pageSize: 20 });
  const products = result.items;

  return (
    <div className="mx-auto max-w-6xl px-4 py-4 sm:px-6">
      <JsonLd
        data={breadcrumbJsonLd([
          { name: tNav("home"), url: absoluteUrl(`/${locale}`) },
          { name: tNav("categories"), url: absoluteUrl(`/${locale}/categories`) },
          { name, url: absoluteUrl(`/${locale}/category/${localizedSlug}`) },
        ])}
      />
      <JsonLd
        data={collectionJsonLd({
          name,
          url: absoluteUrl(`/${locale}/category/${localizedSlug}`),
          description: pick(locale, category.seoDescriptionAr, category.seoDescriptionEn),
        })}
      />
      <Breadcrumb
        label={ta("loading")}
        items={[
          { label: tNav("home"), href: "/" },
          { label: tNav("categories"), href: "/categories" },
          { label: name },
        ]}
      />
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{name}</h1>

      {products.length === 0 ? (
        <EmptyState title={tCat("empty") || te("noProducts")} />
      ) : (
        <>
          <ProductGrid products={products} />
          <Pagination
            currentPage={result.page}
            totalPages={result.totalPages}
            createHref={(p) => `/category/${localizedSlug}?page=${p}`}
          />
        </>
      )}

      <ContentBlock
        ar={category.seoDescriptionAr}
        en={category.seoDescriptionEn}
        locale={locale as Locale}
      />
    </div>
  );
}
