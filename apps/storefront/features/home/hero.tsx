import Image from "next/image";
import { useLocale } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { pick } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import type { Hero as HeroType } from "@/lib/api/types";

function isInternal(href: string): boolean {
  return href.startsWith("/") && !href.startsWith("//");
}
function isExternal(href: string): boolean {
  return /^https?:\/\//i.test(href);
}

export function Hero({ hero }: { hero: HeroType }) {
  const locale = useLocale() as Locale;
  const title = pick(locale, hero.titleAr, hero.titleEn);
  const subtitle = pick(locale, hero.subtitleAr, hero.subtitleEn);
  const cta = pick(locale, hero.ctaTextAr, hero.ctaTextEn);
  const link = hero.ctaLink ?? "";
  const showCta = Boolean(cta) && (isInternal(link) || isExternal(link));

  return (
    <section className="relative overflow-hidden">
      <div className="relative aspect-[16/11] w-full sm:aspect-[21/9]">
        <Image
          src={hero.imageUrl}
          alt={title}
          fill
          priority
          sizes="100vw"
          className="object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-deepbrown/50 via-deepbrown/10 to-transparent" />
        <div className="absolute inset-0 flex flex-col items-center justify-end gap-3 p-6 text-center sm:justify-center sm:p-12">
          <h1 className="max-w-2xl text-2xl font-semibold text-cream drop-shadow sm:text-4xl">
            {title}
          </h1>
          {subtitle && <p className="max-w-xl text-cream/90 drop-shadow">{subtitle}</p>}
          {showCta &&
            (isInternal(link) ? (
              <Link
                href={link}
                className="mt-1 rounded-pill bg-cream px-6 py-2.5 font-medium text-mocha hover:bg-ivory"
              >
                {cta}
              </Link>
            ) : (
              <a
                href={link}
                target="_blank"
                rel="noopener noreferrer"
                className="mt-1 rounded-pill bg-cream px-6 py-2.5 font-medium text-mocha hover:bg-ivory"
              >
                {cta}
              </a>
            ))}
        </div>
      </div>
    </section>
  );
}
