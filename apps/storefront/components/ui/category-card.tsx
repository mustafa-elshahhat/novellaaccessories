import Image from "next/image";
import { useLocale } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { pick, pickSlug } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import type { PublicCategory } from "@/lib/api/types";

export function CategoryCard({ category }: { category: PublicCategory }) {
  const locale = useLocale() as Locale;
  const name = pick(locale, category.nameAr, category.nameEn);
  const slug = pickSlug(locale, category.slugAr, category.slugEn);

  return (
    <Link
      href={`/category/${slug}`}
      className="group block overflow-hidden rounded-card border border-bordergold/40 bg-ivory"
    >
      <div className="relative aspect-[4/3]">
        {category.imageUrl ? (
          <Image
            src={category.imageUrl}
            alt={name}
            fill
            sizes="(max-width: 640px) 50vw, 25vw"
            className="object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-taupe">{name}</div>
        )}
      </div>
      <div className="p-3 text-center">
        <h3 className="font-medium text-mocha">{name}</h3>
      </div>
    </Link>
  );
}
