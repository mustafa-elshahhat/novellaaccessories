import type { Locale } from "@/lib/i18n/routing";
import { pick } from "@/lib/i18n/localize";
import { cn } from "@/lib/utils/cn";

/**
 * Renders localized AEO/GEO/long-form text (plain text, never HTML — safe by construction).
 * Returns null when there is no content for the locale.
 */
export function ContentBlock({
  ar,
  en,
  locale,
  title,
  className,
}: {
  ar: string | null | undefined;
  en: string | null | undefined;
  locale: Locale;
  title?: string;
  className?: string;
}) {
  const text = pick(locale, ar, en);
  if (!text.trim()) return null;
  const paragraphs = text
    .split(/\n{2,}/)
    .map((p) => p.trim())
    .filter(Boolean);

  return (
    <section className={cn("mx-auto max-w-3xl px-1 py-6", className)}>
      {title && <h2 className="mb-3 text-lg font-semibold text-deepbrown">{title}</h2>}
      {paragraphs.map((paragraph, index) => (
        <p
          key={index}
          className="mb-3 whitespace-pre-line leading-relaxed text-taupe"
        >
          {paragraph}
        </p>
      ))}
    </section>
  );
}
