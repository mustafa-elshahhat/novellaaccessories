"use client";

import { useEffect, useRef, useState } from "react";
import Image from "next/image";
import type { Route } from "next";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { pick, pickSlug } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import { formatPrice } from "@/lib/format";
import type { Cart, CartItem } from "@/lib/api/types";
import { bff } from "@/lib/api/bff-client";
import { useAuth } from "@/features/auth/auth-provider";
import { useCart } from "@/features/cart/cart-provider";
import { useToast } from "@/components/ui/toast";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { Button } from "@/components/ui/button";
import { QuantitySelector } from "@/components/ui/quantity-selector";
import { PriceDisplay } from "@/components/ui/price";
import { EmptyState } from "@/components/ui/states";
import { Skeleton } from "@/components/ui/skeleton";
import { CloseIcon } from "@/components/icons";

export function CartView() {
  const t = useTranslations("cart");
  const tAuth = useTranslations("auth");
  const tNav = useTranslations("nav");
  const locale = useLocale();
  const router = useRouter();
  const { customer, loading: authLoading } = useAuth();
  const { cart, setCart, refresh } = useCart();
  const toast = useToast();
  const getError = useErrorMessage();

  const [pendingId, setPendingId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [couponCode, setCouponCode] = useState("");
  const [checkingOut, setCheckingOut] = useState(false);
  const repriced = useRef(false);

  // Reprice once on load so displayed totals/availability are server-authoritative.
  useEffect(() => {
    if (!customer || repriced.current) return;
    repriced.current = true;
    void (async () => {
      try {
        const fresh = await bff<Cart>("/api/cart/reprice", { method: "POST" });
        setCart(fresh);
        if (fresh.hasUnavailableItems || fresh.items.some((i) => i.quantityAdjusted)) {
          setNotice(t("priceChanged"));
        }
      } catch {
        // keep existing cart on failure
      }
    })();
  }, [customer, setCart, t]);

  if (authLoading) {
    return (
      <div className="mx-auto max-w-3xl space-y-4 px-4 py-8">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  if (!customer) {
    return (
      <EmptyState
        title={tAuth("loginTitle")}
        action={
          <Link
            href="/login"
            className="inline-flex rounded-pill bg-bronze px-6 py-3 text-cream"
          >
            {tNav("login")}
          </Link>
        }
      />
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <EmptyState
        title={t("empty")}
        action={
          <Link
            href="/categories"
            className="inline-flex rounded-pill bg-bronze px-6 py-3 text-cream"
          >
            {t("emptyCta")}
          </Link>
        }
      />
    );
  }

  async function updateQty(item: CartItem, quantity: number) {
    if (quantity === item.quantity) return;
    setPendingId(item.itemId);
    try {
      const updated = await bff<Cart>(`/api/cart/items/${item.itemId}`, {
        method: "PATCH",
        body: JSON.stringify({ quantity }),
      });
      setCart(updated);
    } catch (err) {
      toast.show(getError(err), "error");
    } finally {
      setPendingId(null);
    }
  }

  async function removeItem(item: CartItem) {
    setPendingId(item.itemId);
    try {
      const updated = await bff<Cart>(`/api/cart/items/${item.itemId}`, {
        method: "DELETE",
      });
      setCart(updated);
    } catch (err) {
      toast.show(getError(err), "error");
    } finally {
      setPendingId(null);
    }
  }

  async function proceedToCheckout() {
    setCheckingOut(true);
    try {
      const fresh = await bff<Cart>("/api/cart/reprice", { method: "POST" });
      setCart(fresh);
      if (fresh.hasUnavailableItems) {
        setNotice(t("hasUnavailable"));
        setCheckingOut(false);
        return;
      }
      const qs = couponCode.trim() ? `?coupon=${encodeURIComponent(couponCode.trim())}` : "";
      router.push(`/${locale}/checkout${qs}` as Route);
    } catch (err) {
      toast.show(getError(err), "error");
      setCheckingOut(false);
    }
  }

  const hasUnavailable = cart.hasUnavailableItems;

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>

      {notice && (
        <p
          role="status"
          className="mb-4 rounded-lg bg-ivory px-4 py-3 text-sm text-bronze"
        >
          {notice}
        </p>
      )}

      <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
        <ul className="space-y-4">
          {cart.items.map((item) => {
            const name = pick(locale, item.productNameAr, item.productNameEn);
            const slug = pickSlug(locale as Locale, item.productSlugAr, item.productSlugEn);
            const imageAlt = pick(locale, item.primaryImageAltAr, item.primaryImageAltEn) || name;
            const variant = pick(locale, item.variantNameAr, item.variantNameEn);
            const busy = pendingId === item.itemId;
            return (
              <li
                key={item.itemId}
                className="flex gap-4 rounded-card border border-bordergold/40 bg-cream p-3"
              >
                <div className="relative h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-ivory">
                  {item.primaryImageUrl ? (
                    <Image
                      src={item.primaryImageUrl}
                      alt={imageAlt}
                      fill
                      sizes="96px"
                      className="object-cover"
                    />
                  ) : (
                    <div className="flex h-full items-center justify-center text-xs text-taupe">
                      novella
                    </div>
                  )}
                </div>
                <div className="flex flex-1 flex-col gap-2">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <Link
                        href={`/product/${slug}`}
                        className="font-medium text-mocha hover:text-bronze"
                      >
                        {name}
                      </Link>
                      {variant && <p className="text-xs text-taupe">{variant}</p>}
                      {!item.isAvailable && (
                        <p className="text-xs text-red-600">{t("itemUnavailable")}</p>
                      )}
                      {item.quantityAdjusted && (
                        <p className="text-xs text-bronze">{t("quantityAdjusted")}</p>
                      )}
                    </div>
                    <button
                      type="button"
                      onClick={() => removeItem(item)}
                      disabled={busy}
                      aria-label={t("remove")}
                      className="text-taupe hover:text-red-600 disabled:opacity-50"
                    >
                      <CloseIcon className="h-5 w-5" />
                    </button>
                  </div>
                  <div className="flex items-center justify-between gap-2">
                    <QuantitySelector
                      value={item.quantity}
                      onChange={(q) => updateQty(item, q)}
                      min={1}
                      disabled={busy || !item.isAvailable}
                    />
                    <div className="text-end">
                      <PriceDisplay
                        original={item.originalUnitPrice}
                        final={item.unitPrice}
                        size="sm"
                      />
                      <p className="text-sm font-semibold text-mocha">
                        {formatPrice(item.lineTotal, locale)}
                      </p>
                    </div>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>

        <aside className="h-fit rounded-card border border-bordergold/40 bg-ivory/40 p-5">
          <h2 className="mb-4 text-lg font-semibold text-deepbrown">{t("subtotal")}</h2>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-taupe">{t("subtotal")}</dt>
              <dd>{formatPrice(cart.productSubtotalBeforeDiscount, locale)}</dd>
            </div>
            {cart.productDiscountTotal > 0 && (
              <div className="flex justify-between text-bronze">
                <dt>{t("productDiscount")}</dt>
                <dd>-{formatPrice(cart.productDiscountTotal, locale)}</dd>
              </div>
            )}
            <div className="flex justify-between border-t border-bordergold/40 pt-2 font-semibold text-mocha">
              <dt>{t("subtotalAfterDiscount")}</dt>
              <dd>{formatPrice(cart.subtotalAfterProductDiscount, locale)}</dd>
            </div>
          </dl>
          <label className="mt-4 block text-sm font-medium text-mocha" htmlFor="cart-coupon">
            {t("couponLabel")}
          </label>
          <input
            id="cart-coupon"
            value={couponCode}
            onChange={(event) => setCouponCode(event.target.value)}
            placeholder={t("couponPlaceholder")}
            className="mt-2 w-full rounded-xl border border-bordergold bg-cream px-4 py-2.5 text-mocha placeholder:text-taupe/70 focus:border-bronze focus:outline-none focus:ring-2 focus:ring-bronze/30"
          />
          {hasUnavailable && (
            <p role="alert" className="mt-3 text-sm text-red-600">
              {t("hasUnavailable")}
            </p>
          )}
          <Button
            onClick={proceedToCheckout}
            fullWidth
            disabled={checkingOut || hasUnavailable}
            className="mt-4"
          >
            {t("checkout")}
          </Button>
        </aside>
      </div>
    </div>
  );
}
