"use client";

import { useState } from "react";
import Image from "next/image";
import { useLocale, useTranslations } from "next-intl";
import { pick } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import type { PublicProductImage } from "@/lib/api/types";
import { ChevronLeftIcon, ChevronRightIcon } from "@/components/icons";
import { cn } from "@/lib/utils/cn";

export function ProductGallery({
  images,
  fallbackAlt,
}: {
  images: PublicProductImage[];
  fallbackAlt: string;
}) {
  const locale = useLocale() as Locale;
  const t = useTranslations("a11y");
  const [index, setIndex] = useState(0);

  if (images.length === 0) {
    return (
      <div className="flex aspect-square w-full items-center justify-center rounded-card border border-bordergold/40 bg-ivory text-taupe">
        novella
      </div>
    );
  }

  const current = images[index]!;
  const altFor = (img: PublicProductImage) =>
    pick(locale, img.altAr, img.altEn) || fallbackAlt;

  return (
    <div className="flex flex-col gap-3">
      <div className="relative aspect-square overflow-hidden rounded-card border border-bordergold/40 bg-ivory">
        <Image
          src={current.url}
          alt={altFor(current)}
          fill
          priority
          sizes="(max-width: 1024px) 100vw, 50vw"
          className="object-cover"
        />
        {images.length > 1 && (
          <>
            <button
              type="button"
              onClick={() => setIndex((i) => (i - 1 + images.length) % images.length)}
              aria-label={t("previousImage")}
              className="absolute start-2 top-1/2 -translate-y-1/2 rounded-full bg-cream/80 p-2 text-mocha hover:bg-cream"
            >
              <ChevronLeftIcon className="h-5 w-5 rtl:rotate-180" />
            </button>
            <button
              type="button"
              onClick={() => setIndex((i) => (i + 1) % images.length)}
              aria-label={t("nextImage")}
              className="absolute end-2 top-1/2 -translate-y-1/2 rounded-full bg-cream/80 p-2 text-mocha hover:bg-cream"
            >
              <ChevronRightIcon className="h-5 w-5 rtl:rotate-180" />
            </button>
          </>
        )}
      </div>

      {images.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-1">
          {images.map((img, i) => (
            <button
              key={img.id}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`${fallbackAlt} ${i + 1}`}
              aria-current={i === index}
              className={cn(
                "relative h-16 w-16 shrink-0 overflow-hidden rounded-lg border",
                i === index ? "border-bronze" : "border-bordergold/40",
              )}
            >
              <Image src={img.url} alt="" fill sizes="64px" className="object-cover" />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
