"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Locale } from "@/lib/i18n/routing";
import type { Cart, PublicProduct } from "@/lib/api/types";
import { variantLabel } from "./variant-label";
import { PriceDisplay } from "@/components/ui/price";
import { DiscountBadge, AvailabilityBadge } from "@/components/ui/badges";
import { QuantitySelector } from "@/components/ui/quantity-selector";
import { Button } from "@/components/ui/button";
import { bff } from "@/lib/api/bff-client";
import { useCart } from "@/features/cart/cart-provider";
import { useToast } from "@/components/ui/toast";
import { useAnalytics } from "@/features/analytics/analytics-provider";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { cn } from "@/lib/utils/cn";

export function ProductPurchase({ product }: { product: PublicProduct }) {
  const locale = useLocale() as Locale;
  const t = useTranslations("products");
  const tc = useTranslations("common");
  const { setCart } = useCart();
  const toast = useToast();
  const analytics = useAnalytics();
  const getError = useErrorMessage();

  const variants = product.variants;
  const [selectedId, setSelectedId] = useState<string | null>(
    variants.length === 1 ? variants[0]!.id : null,
  );
  const [qty, setQty] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>();

  const selected = useMemo(
    () => variants.find((v) => v.id === selectedId) ?? null,
    [variants, selectedId],
  );

  // Fire ProductView once per mounted product (guards StrictMode double-invocation).
  const viewedId = useRef<string | null>(null);
  useEffect(() => {
    if (viewedId.current === product.id) return;
    viewedId.current = product.id;
    analytics.track("ProductView", { productId: product.id });
  }, [product.id, analytics]);

  const displayOriginal = selected?.originalPrice ?? product.originalPrice;
  const displayFinal = selected?.finalPrice ?? product.finalPrice;
  const available = selected ? selected.isAvailable : product.isAvailable;
  const canAdd =
    product.isAvailable && variants.length > 0 && (selected?.isAvailable ?? false);

  async function add() {
    setError(undefined);
    if (variants.length > 0 && !selected) {
      setError(t("selectVariant"));
      return;
    }
    if (!selected) return;
    setLoading(true);
    try {
      const cart = await bff<Cart>("/api/cart/items", {
        method: "POST",
        body: JSON.stringify({ productVariantId: selected.id, quantity: qty }),
      });
      setCart(cart);
      analytics.track("AddToCart", {
        productId: product.id,
        metadata: { quantity: qty },
      });
      toast.show(tc("addToCart"), "success");
    } catch (err) {
      const message = getError(err);
      setError(message);
      toast.show(message, "error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center gap-3">
        <PriceDisplay original={displayOriginal} final={displayFinal} size="lg" />
        {displayFinal < displayOriginal && (
          <DiscountBadge percent={product.discountPercentage} />
        )}
        <AvailabilityBadge available={available} />
      </div>

      {variants.length > 0 && (
        <div>
          <p className="mb-2 text-sm font-medium text-mocha">{t("selectVariant")}</p>
          <div className="flex flex-wrap gap-2">
            {variants.map((variant) => (
              <button
                key={variant.id}
                type="button"
                disabled={!variant.isAvailable}
                onClick={() => setSelectedId(variant.id)}
                aria-pressed={selectedId === variant.id}
                className={cn(
                  "rounded-pill border px-4 py-2 text-sm transition-colors",
                  !variant.isAvailable && "cursor-not-allowed opacity-40 line-through",
                  selectedId === variant.id
                    ? "border-bronze bg-ivory text-bronze"
                    : "border-bordergold text-mocha hover:bg-ivory",
                )}
              >
                {variantLabel(locale, variant)}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="flex items-center gap-4">
        <QuantitySelector value={qty} onChange={setQty} min={1} disabled={!canAdd} />
        <Button onClick={add} disabled={!canAdd || loading} className="flex-1">
          {tc("addToCart")}
        </Button>
      </div>

      {!available && <p className="text-sm text-taupe">{t("outOfStock")}</p>}
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}

      <div className="space-y-1 border-t border-bordergold/40 pt-4">
        <p className="text-xs text-taupe">{t("shippingNote")}</p>
        <p className="text-xs text-taupe">{t("returnNote")}</p>
      </div>
    </div>
  );
}
