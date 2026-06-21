import type { Locale } from "@/lib/i18n/routing";
import type { PublicProductVariant } from "@/lib/api/types";
import { pick } from "@/lib/i18n/localize";

/** Builds a human-readable label for a variant from its localized attributes. */
export function variantLabel(locale: Locale, variant: PublicProductVariant): string {
  const named = pick(locale, variant.nameAr, variant.nameEn);
  if (named) return named;
  const parts = [
    variant.size ?? "",
    pick(locale, variant.colorAr, variant.colorEn),
    pick(locale, variant.materialAr, variant.materialEn),
    pick(locale, variant.customOptionValueAr, variant.customOptionValueEn),
  ]
    .map((p) => p.trim())
    .filter(Boolean);
  return parts.join(" · ") || (variant.size ?? "—");
}
