import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick, pickSlug } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import { getProduct, getFeaturedProducts } from "@/lib/api/public";
import type { PublicProduct, PublicProductListItem } from "@/lib/api/types";
import { ApiError } from "@/lib/api/errors";
import { buildPublicMetadata, absoluteUrl } from "@/lib/seo/metadata";
import { JsonLd, productJsonLd, breadcrumbJsonLd } from "@/lib/seo/jsonld";
import { ProductGallery } from "@/features/catalog/product-gallery";
import { ProductPurchase } from "@/features/catalog/product-purchase";
import { Breadcrumb } from "@/components/ui/breadcrumb";
import { Section } from "@/components/ui/section";
import { ProductGrid } from "@/components/ui/product-grid";
import { ContentBlock } from "@/components/ui/content-block";

type PageProps = {
  params: Promise<{ locale: string; slug: string }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale, slug } = await params;
  let product: PublicProduct | null = null;
  try {
    product = await getProduct(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return {};
    throw error;
  }
  if (!product) return {};
  const title =
    pick(locale, product.seoTitleAr, product.seoTitleEn) ||
    pick(locale, product.nameAr, product.nameEn);
  const description =
    pick(locale, product.seoDescriptionAr, product.seoDescriptionEn) ||
    pick(locale, product.descriptionAr, product.descriptionEn);
  return buildPublicMetadata({
    locale,
    title,
    description: description || undefined,
    pathAr: `/product/${product.slugAr}`,
    pathEn: `/product/${product.slugEn}`,
    images: product.images.map((i) => i.url),
    type: "article",
  });
}

export default async function ProductPage({ params }: PageProps) {
  const { locale, slug } = await params;
  setRequestLocale(locale);

  let product: PublicProduct | null = null;
  try {
    product = await getProduct(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  const tNav = await getTranslations("nav");
  const tp = await getTranslations("products");
  const ta = await getTranslations("a11y");

  const name = pick(locale, product.nameAr, product.nameEn);
  const description = pick(locale, product.descriptionAr, product.descriptionEn);
  const localizedSlug = pickSlug(locale, product.slugAr, product.slugEn);
  const url = absoluteUrl(`/${locale}/product/${localizedSlug}`);

  const featured = await getFeaturedProducts();
  const related: PublicProductListItem[] = featured.filter((p) => p.id !== product.id).slice(0, 8);

  return (
    <div className="mx-auto max-w-6xl px-4 py-4 sm:px-6">
      <JsonLd
        data={productJsonLd({
          name,
          description: description || undefined,
          images: product.images.map((i) => i.url),
          url,
          price: product.finalPrice,
          isAvailable: product.isAvailable,
        })}
      />
      <JsonLd
        data={breadcrumbJsonLd([
          { name: tNav("home"), url: absoluteUrl(`/${locale}`) },
          { name: tNav("products"), url: absoluteUrl(`/${locale}/products`) },
          { name, url },
        ])}
      />
      <Breadcrumb
        label={ta("loading")}
        items={[
          { label: tNav("home"), href: "/" },
          { label: tNav("products"), href: "/products" },
          { label: name },
        ]}
      />

      <div className="grid gap-8 lg:grid-cols-2">
        <ProductGallery images={product.images} fallbackAlt={name} />
        <div>
          <h1 className="mb-4 text-2xl font-semibold text-deepbrown sm:text-3xl">{name}</h1>
          <ProductPurchase product={product} />
        </div>
      </div>

      {description && (
        <section className="mx-auto max-w-3xl py-8">
          <h2 className="mb-3 text-lg font-semibold text-deepbrown">{tp("description")}</h2>
          <p className="whitespace-pre-line leading-relaxed text-taupe">{description}</p>
        </section>
      )}

      <ContentBlock
        ar={product.aeoSummaryAr}
        en={product.aeoSummaryEn}
        locale={locale as Locale}
        title={tp("questionsTitle")}
      />

      {related.length > 0 && (
        <Section title={tp("relatedTitle")}>
          <ProductGrid products={related} />
        </Section>
      )}
    </div>
  );
}
