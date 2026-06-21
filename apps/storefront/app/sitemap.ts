import type { MetadataRoute } from "next";
import { publicEnv } from "@/lib/env";
import { getSitemapData } from "@/lib/api/seo";

export const revalidate = 3600;

/** Maps a backend sitemap entry type to its localized storefront route segment. */
const TYPE_SEGMENT: Record<string, string> = {
  category: "category",
  product: "product",
  page: "page",
};

function url(path: string): string {
  return `${publicEnv.siteUrl}${path}`;
}

/** Builds a sitemap entry with ar/en + x-default hreflang alternates. */
function entry(
  pathAr: string,
  pathEn: string,
  locale: "ar" | "en",
  lastModified?: string | Date,
): MetadataRoute.Sitemap[number] {
  return {
    url: url(locale === "ar" ? pathAr : pathEn),
    lastModified,
    alternates: {
      languages: {
        ar: url(pathAr),
        en: url(pathEn),
        "x-default": url(pathAr),
      },
    },
  };
}

/**
 * Public sitemap: static catalog/content routes for both locales plus indexable
 * category/product/page entries from the backend (localized slugs). Private/transactional
 * routes are excluded entirely (also blocked in robots.ts).
 */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const staticPaths = ["", "/categories", "/products", "/offers", "/faq"];
  const items: MetadataRoute.Sitemap = [];

  for (const path of staticPaths) {
    items.push(entry(`/ar${path}`, `/en${path}`, "ar"));
    items.push(entry(`/ar${path}`, `/en${path}`, "en"));
  }

  const data = await getSitemapData();
  if (data) {
    for (const e of data.entries) {
      const segment = TYPE_SEGMENT[e.type];
      if (!segment || !e.indexable) continue;
      const pathAr = `/ar/${segment}/${e.slugAr}`;
      const pathEn = `/en/${segment}/${e.slugEn}`;
      items.push(entry(pathAr, pathEn, "ar", e.lastModified));
      items.push(entry(pathAr, pathEn, "en", e.lastModified));
    }
  }

  return items;
}
