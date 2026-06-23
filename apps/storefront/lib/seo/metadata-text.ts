import { BRAND } from "@/lib/constants";
import { pick } from "@/lib/i18n/localize";

/**
 * Pure, framework-free helpers that build the text of automatically generated metadata
 * (titles, descriptions, excerpts and localized fallbacks) from normal business content.
 * Kept separate from metadata.ts so they can be unit-tested without server-only imports.
 */

export const META_DESCRIPTION_MAX = 160;

/** Localized brand/site name used as the title template suffix. */
export function brandName(locale: string): string {
  return pick(locale, BRAND.nameAr, BRAND.nameEn) || BRAND.nameEn;
}

/** Entity page title: localized name joined with the brand title template ("Name | Brand"). */
export function entityTitle(locale: string, name: string): string {
  const trimmed = name.trim();
  return trimmed ? `${trimmed} | ${brandName(locale)}` : brandName(locale);
}

/**
 * Plain-text metadata excerpt: removes HTML (tolerating unmatched/malformed markup), normalizes
 * whitespace, and truncates to a metadata-friendly length at a word boundary.
 */
export function excerpt(input: string | null | undefined, maxLength = META_DESCRIPTION_MAX): string {
  if (!input) return "";
  const text = input
    .replace(/<[^>]*>/g, " ") // well-formed tags
    .replace(/<[^>]*$/g, " ") // a trailing, unclosed tag
    .replace(/[<>]/g, " ") // any stray angle brackets
    .replace(/&nbsp;/gi, " ")
    .replace(/&amp;/gi, "&")
    .replace(/\s+/g, " ")
    .replace(/\s+([.,;:!?؟،])/g, "$1") // drop whitespace introduced before punctuation by tag removal
    .trim();
  if (text.length <= maxLength) return text;
  const slice = text.slice(0, maxLength);
  const lastSpace = slice.lastIndexOf(" ");
  return `${(lastSpace > 0 ? slice.slice(0, lastSpace) : slice).trim()}…`;
}

/** Product metadata description: the localized product description, or a localized fallback. */
export function productMetaDescription(
  locale: string,
  name: string,
  description: string | null | undefined,
): string {
  const d = (description ?? "").trim();
  if (d) return d;
  return locale === "ar"
    ? `${name} من ${brandName(locale)} — إكسسوار أنيق مع توصيل داخل مصر والدفع عند الاستلام.`
    : `${name} from ${brandName(locale)} — an elegant accessory with delivery across Egypt and cash on delivery.`;
}

/** Category metadata description: the localized category description, or a localized fallback. */
export function categoryMetaDescription(
  locale: string,
  name: string,
  description: string | null | undefined,
): string {
  const d = (description ?? "").trim();
  if (d) return d;
  return locale === "ar"
    ? `تسوّقي تشكيلة ${name} من ${brandName(locale)} مع توصيل داخل مصر.`
    : `Shop ${name} from ${brandName(locale)} with delivery across Egypt.`;
}

/** Static-page metadata description: a plain-text excerpt of the content, or a localized fallback. */
export function pageMetaDescription(
  locale: string,
  content: string | null | undefined,
  title: string,
): string {
  const ex = excerpt(content);
  if (ex) return ex;
  return `${title} — ${brandName(locale)}.`;
}
