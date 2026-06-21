/** Formats a price in EGP for the given locale (Arabic-Egypt or English-Egypt). */
export function formatPrice(value: number, locale: string): string {
  try {
    return new Intl.NumberFormat(locale === "ar" ? "ar-EG" : "en-EG", {
      style: "currency",
      currency: "EGP",
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${value} EGP`;
  }
}

/** Formats an ISO date for display. */
export function formatDate(iso: string, locale: string): string {
  try {
    return new Intl.DateTimeFormat(locale === "ar" ? "ar-EG" : "en-GB", {
      year: "numeric",
      month: "short",
      day: "numeric",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

/** Rounds a discount percentage to a whole number for badges. */
export function formatDiscountPercent(value: number | null | undefined): number {
  if (value == null) return 0;
  return Math.round(value);
}
