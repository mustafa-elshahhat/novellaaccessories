import type { MetadataRoute } from "next";
import { publicEnv } from "@/lib/env";
import { NOINDEX_PREFIXES } from "@/lib/constants";
import { locales } from "@/lib/i18n/routing";

/**
 * robots.txt: allow public catalog/content; disallow transactional, auth, account, and the
 * same-origin BFF API. Private prefixes are blocked for every supported locale.
 */
export default function robots(): MetadataRoute.Robots {
  const disallow = [
    "/api/",
    ...locales.flatMap((locale) =>
      NOINDEX_PREFIXES.map((prefix) => `/${locale}${prefix}`),
    ),
  ];

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow,
    },
    sitemap: `${publicEnv.siteUrl}/sitemap.xml`,
    host: publicEnv.siteUrl,
  };
}
