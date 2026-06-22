import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { pick } from "@/lib/i18n/localize";
import { getHome, getSiteSettings } from "@/lib/api/public";
import type { SiteSettings } from "@/lib/api/types";
import { buildPublicMetadata } from "@/lib/seo/metadata";
import { JsonLd, organizationJsonLd, websiteJsonLd } from "@/lib/seo/jsonld";
import { Hero } from "@/features/home/hero";
import { TrustBlocks } from "@/features/home/trust-blocks";
import { Section } from "@/components/ui/section";
import { ProductGrid } from "@/components/ui/product-grid";
import { CategoryCard } from "@/components/ui/category-card";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  let settings: SiteSettings | null = null;
  try {
    settings = await getSiteSettings();
  } catch {
    settings = null;
  }
  const t = await getTranslations({ locale, namespace: "home" });
  const title =
    (settings && pick(locale, settings.defaultSeoTitleAr, settings.defaultSeoTitleEn)) ||
    (settings && pick(locale, settings.siteNameAr, settings.siteNameEn)) ||
    "Novella";
  const description =
    (settings &&
      pick(locale, settings.defaultSeoDescriptionAr, settings.defaultSeoDescriptionEn)) ||
    t("brandStoryBody");
  return buildPublicMetadata({
    locale,
    title,
    description,
    pathAr: "/",
    pathEn: "/",
  });
}

export default async function HomePage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("home");

  const home = await getHome();

  const heroes = home.heroes.filter((h) => h.isActive && Boolean(h.imageUrl));
  const categories = home.categories;
  const featured = home.featuredProducts;
  const discounted = featured.filter((p) => p.hasDiscount).slice(0, 8);
  const siteName = pick(locale, home.siteSettings.siteNameAr, home.siteSettings.siteNameEn);

  return (
    <>
      <JsonLd data={organizationJsonLd(siteName || "Novella")} />
      <JsonLd data={websiteJsonLd(siteName || "Novella")} />

      {heroes[0] && <Hero hero={heroes[0]} />}

      {categories.length > 0 && (
        <Section
          title={t("featuredCategories")}
          viewAllHref="/categories"
          viewAllLabel={t("viewAll")}
        >
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {categories.slice(0, 8).map((category) => (
              <CategoryCard key={category.id} category={category} />
            ))}
          </div>
        </Section>
      )}

      {discounted.length > 0 && (
        <Section title={t("discounted")} viewAllHref="/offers" viewAllLabel={t("viewAll")}>
          <ProductGrid products={discounted} />
        </Section>
      )}

      {featured.length > 0 && (
        <Section
          title={t("featuredProducts")}
          viewAllHref="/products"
          viewAllLabel={t("viewAll")}
        >
          <ProductGrid products={featured.slice(0, 8)} />
        </Section>
      )}

      <section className="mx-auto max-w-3xl px-4 py-12 text-center">
        <p className="text-sm uppercase tracking-[0.3em] text-champagne">
          {t("brandStoryEyebrow")}
        </p>
        <h2 className="mt-3 text-2xl font-semibold sm:text-3xl">{t("brandStoryTitle")}</h2>
        <p className="mt-3 text-taupe">{t("brandStoryBody")}</p>
      </section>

      <TrustBlocks />

      <section className="mx-auto max-w-3xl px-4 pb-12 text-center">
        <h2 className="text-xl font-semibold text-deepbrown">{t("faqPreviewTitle")}</h2>
        <Link href="/faq" className="mt-3 inline-block text-bronze hover:underline">
          {t("viewAll")}
        </Link>
      </section>
    </>
  );
}
