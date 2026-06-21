"use client";

import { useTranslations } from "next-intl";
import { MinusIcon, PlusIcon } from "@/components/icons";
import { cn } from "@/lib/utils/cn";

interface QuantitySelectorProps {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
  disabled?: boolean;
  className?: string;
}

export function QuantitySelector({
  value,
  onChange,
  min = 1,
  max,
  disabled,
  className,
}: QuantitySelectorProps) {
  const t = useTranslations("a11y");
  const canDecrease = !disabled && value > min;
  const canIncrease = !disabled && (max === undefined || value < max);

  return (
    <div
      className={cn(
        "inline-flex items-center rounded-pill border border-bordergold bg-cream",
        className,
      )}
      role="group"
    >
      <button
        type="button"
        aria-label={t("decreaseQuantity")}
        onClick={() => onChange(Math.max(min, value - 1))}
        disabled={!canDecrease}
        className="flex h-10 w-10 items-center justify-center text-mocha disabled:opacity-40"
      >
        <MinusIcon className="h-4 w-4" />
      </button>
      <span aria-live="polite" className="w-10 text-center text-mocha">
        {value}
      </span>
      <button
        type="button"
        aria-label={t("increaseQuantity")}
        onClick={() => onChange(max ? Math.min(max, value + 1) : value + 1)}
        disabled={!canIncrease}
        className="flex h-10 w-10 items-center justify-center text-mocha disabled:opacity-40"
      >
        <PlusIcon className="h-4 w-4" />
      </button>
    </div>
  );
}
