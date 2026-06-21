import { useTranslations } from "next-intl";
import { formatDiscountPercent } from "@/lib/format";
import { cn } from "@/lib/utils/cn";

export function DiscountBadge({
  percent,
  className,
}: {
  percent: number | null;
  className?: string;
}) {
  const t = useTranslations("products");
  const value = formatDiscountPercent(percent);
  if (!value) return null;
  return (
    <span
      className={cn(
        "rounded-pill bg-rosegold px-2 py-0.5 text-xs font-semibold text-cream",
        className,
      )}
    >
      {t("discountBadge", { percent: value })}
    </span>
  );
}

export function AvailabilityBadge({
  available,
  className,
}: {
  available: boolean;
  className?: string;
}) {
  const t = useTranslations("products");
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-pill px-2 py-0.5 text-xs font-medium",
        available ? "bg-ivory text-bronze" : "bg-shadowbeige text-taupe",
        className,
      )}
    >
      {available ? t("available") : t("unavailable")}
    </span>
  );
}
