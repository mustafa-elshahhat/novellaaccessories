/**
 * Picks the localized variant of a dual-field backend value (`*Ar` / `*En`),
 * falling back to the other locale when one side is missing.
 */
export function pick(
  locale: string,
  ar: string | null | undefined,
  en: string | null | undefined,
): string {
  const primary = locale === "ar" ? ar : en;
  const secondary = locale === "ar" ? en : ar;
  return (primary ?? secondary ?? "").trim();
}

/** Picks the localized slug for the current locale (slugs are never transliterated client-side). */
export function pickSlug(locale: string, slugAr: string, slugEn: string): string {
  return locale === "ar" ? slugAr : slugEn;
}
