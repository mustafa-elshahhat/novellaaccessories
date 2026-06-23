import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { publicEnv } from "@/lib/env";

// Pure text builders for automatically generated metadata live in a server-free module so they
// can be unit-tested in isolation. Re-exported here as the single metadata entry point.
export {
  brandName,
  entityTitle,
  excerpt,
  productMetaDescription,
  categoryMetaDescription,
  pageMetaDescription,
} from "./metadata-text";

/** Robots directive for private/transactional pages (cart, checkout, account, auth, …). */
export const NOINDEX: Metadata["robots"] = {
  index: false,
  follow: false,
};

export function absoluteUrl(path: string): string {
  const p = path.startsWith("/") ? path : `/${path}`;
  return `${publicEnv.siteUrl}${p}`;
}

/**
 * Builds full public-page metadata: localized title/description, canonical for the current
 * locale, hreflang alternates (using locale-specific slugs), Open Graph and Twitter cards.
 * `pathAr`/`pathEn` are locale-relative paths WITHOUT the locale prefix (e.g. "/product/slug").
 */
export function buildPublicMetadata(opts: {
  locale: string;
  title: string;
  description?: string;
  pathAr: string;
  pathEn: string;
  images?: string[];
  type?: "website" | "article";
}): Metadata {
  const { locale, title, description, pathAr, pathEn, images, type = "website" } = opts;
  const canonical = absoluteUrl(`/${locale}${locale === "ar" ? pathAr : pathEn}`);
  const ogImages = images?.length ? images.map((url) => ({ url })) : undefined;

  return {
    title,
    description: description ?? undefined,
    alternates: {
      canonical,
      languages: {
        ar: absoluteUrl(`/ar${pathAr}`),
        en: absoluteUrl(`/en${pathEn}`),
        "x-default": absoluteUrl(`/ar${pathAr}`),
      },
    },
    openGraph: {
      title,
      description: description ?? undefined,
      url: canonical,
      type,
      siteName: "Novella",
      locale: locale === "ar" ? "ar_EG" : "en_US",
      images: ogImages,
    },
    twitter: {
      card: "summary_large_image",
      title,
      description: description ?? undefined,
      images,
    },
  };
}

/** Builds title + noindex metadata for a private page from a translation key. */
export async function privatePageMetadata(
  locale: string,
  namespace: string,
  key: string,
): Promise<Metadata> {
  const t = await getTranslations({ locale, namespace });
  return {
    title: t(key),
    robots: NOINDEX,
  };
}
