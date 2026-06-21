import Image from "next/image";
import { useLocale } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { pick, pickSlug } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import type { PublicProductListItem } from "@/lib/api/types";
import { PriceDisplay } from "./price";
import { DiscountBadge, AvailabilityBadge } from "./badges";

export function ProductCard({ product }: { product: PublicProductListItem }) {
  const locale = useLocale() as Locale;
  const name = pick(locale, product.nameAr, product.nameEn);
  const slug = pickSlug(locale, product.slugAr, product.slugEn);

  return (
    <Link href={`/product/${slug}`} className="group flex flex-col gap-2">
      <div className="relative aspect-square overflow-hidden rounded-card border border-bordergold/40 bg-ivory">
        {product.primaryImageUrl ? (
          <Image
            src={product.primaryImageUrl}
            alt={name}
            fill
            sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
            className="object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-taupe">
            novella
          </div>
        )}
        {product.hasDiscount && (
          <div className="absolute start-2 top-2">
            <DiscountBadge percent={product.discountPercentage} />
          </div>
        )}
      </div>
      <h3 className="line-clamp-2 text-sm font-medium text-mocha">{name}</h3>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <PriceDisplay original={product.originalPrice} final={product.finalPrice} size="sm" />
        <AvailabilityBadge available={product.isAvailable} />
      </div>
    </Link>
  );
}
