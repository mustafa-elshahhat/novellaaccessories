"use client";

import { useLocale, useTranslations } from "next-intl";
import { usePathname, useRouter } from "@/lib/i18n/navigation";
import { GlobeIcon } from "@/components/icons";
import { cn } from "@/lib/utils/cn";

/**
 * Switches locale while preserving the current route and query string. The backend resolves
 * either localized slug, so detail routes remain valid; canonical/hreflang use locale-specific
 * slugs via server-rendered metadata.
 */
export function LocaleSwitcher({ className }: { className?: string }) {
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();
  const t = useTranslations("a11y");
  const other = locale === "ar" ? "en" : "ar";
  const label = other === "ar" ? "العربية" : "English";

  const handleSwitch = () => {
    // Read the query string directly (avoids useSearchParams, which would force a Suspense boundary).
    const qs = typeof window !== "undefined" ? window.location.search : "";
    const href = qs ? `${pathname}${qs}` : pathname;
    router.replace(href, { locale: other });
  };

  return (
    <button
      type="button"
      onClick={handleSwitch}
      aria-label={t("changeLanguage")}
      lang={other}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-pill px-3 py-1.5 text-sm text-mocha hover:bg-ivory",
        className,
      )}
    >
      <GlobeIcon className="h-4 w-4" />
      <span>{label}</span>
    </button>
  );
}
