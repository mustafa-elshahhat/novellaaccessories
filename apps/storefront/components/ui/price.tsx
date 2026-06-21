import { useLocale } from "next-intl";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils/cn";

interface PriceDisplayProps {
  original: number;
  final: number;
  className?: string;
  size?: "sm" | "md" | "lg";
}

const sizes = {
  sm: "text-sm",
  md: "text-base",
  lg: "text-xl",
};

export function PriceDisplay({ original, final, className, size = "md" }: PriceDisplayProps) {
  const locale = useLocale();
  const discounted = final < original;
  return (
    <span className={cn("inline-flex flex-wrap items-baseline gap-2", className)}>
      <span className={cn("font-semibold text-mocha", sizes[size])}>
        {formatPrice(final, locale)}
      </span>
      {discounted && (
        <span className="text-sm text-taupe line-through">
          {formatPrice(original, locale)}
        </span>
      )}
    </span>
  );
}
