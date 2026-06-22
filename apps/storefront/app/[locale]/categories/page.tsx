import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { getCategories } from "@/lib/api/public";
import { buildPublicMetadata, absoluteUrl } from "@/lib/seo/metadata";
import { JsonLd, collectionJsonLd } from "@/lib/seo/jsonld";
import { CategoryCard } from "@/components/ui/category-card";
import { EmptyState } from "@/components/ui/states";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "categories" });
  return buildPublicMetadata({
    locale,
    title: t("title"),
    pathAr: "/categories",
    pathEn: "/categories",
  });
}

export default async function CategoriesPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("categories");
  const te = await getTranslations("empty");

  const categories = await getCategories();

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <JsonLd
        data={collectionJsonLd({
          name: t("title"),
          url: absoluteUrl(`/${locale}/categories`),
        })}
      />
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>
      {categories.length === 0 ? (
        <EmptyState title={te("noCategories")} />
      ) : (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {categories.map((category) => (
            <CategoryCard key={category.id} category={category} />
          ))}
        </div>
      )}
    </div>
  );
}
