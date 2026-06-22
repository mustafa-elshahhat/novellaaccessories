"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { pick } from "@/lib/i18n/localize";
import { formatPrice } from "@/lib/format";
import type {
  CheckoutPreview,
  CreateOrderResponse,
  CustomerProfile,
  PaymentMethod,
  PaymentMethodInfo,
  PublicGovernorate,
} from "@/lib/api/types";
import { bff } from "@/lib/api/bff-client";
import { useCart } from "@/features/cart/cart-provider";
import { useAnalytics } from "@/features/analytics/analytics-provider";
import { useToast } from "@/components/ui/toast";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { FormField } from "@/components/ui/form-field";
import { Input, Textarea } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";

export function CheckoutForm({
  customer,
  governorates,
  paymentMethods,
  initialCoupon = "",
}: {
  customer: CustomerProfile;
  governorates: PublicGovernorate[];
  paymentMethods: PaymentMethodInfo[];
  initialCoupon?: string;
}) {
  const t = useTranslations("checkout");
  const tp = useTranslations("payment");
  const tv = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const { refresh: refreshCart } = useCart();
  const analytics = useAnalytics();
  const toast = useToast();
  const getError = useErrorMessage();

  const activeMethods = paymentMethods.filter((m) => m.isActive);

  const [governorateId, setGovernorateId] = useState("");
  const [cityDistrict, setCityDistrict] = useState("");
  const [detailedAddress, setDetailedAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [coupon, setCoupon] = useState(initialCoupon);
  const [method, setMethod] = useState<PaymentMethod | "">(
    activeMethods[0]?.method ?? "",
  );

  const [preview, setPreview] = useState<CheckoutPreview | null>(null);
  const [idempotencyKey, setIdempotencyKey] = useState(() => newIdempotencyKey());
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [reviewing, setReviewing] = useState(false);
  const [placing, setPlacing] = useState(false);

  function validate(): boolean {
    const next: Record<string, string> = {};
    if (!governorateId) next.governorate = tv("governorateRequired");
    if (!cityDistrict.trim()) next.cityDistrict = tv("addressRequired");
    if (!detailedAddress.trim()) next.detailedAddress = tv("addressRequired");
    if (!method) next.method = tv("paymentRequired");
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  // Changing inputs invalidates a stale preview so totals never drift from what is ordered.
  function invalidatePreview() {
    if (preview) {
      setPreview(null);
      setIdempotencyKey(newIdempotencyKey());
    }
  }

  async function review(event: FormEvent) {
    event.preventDefault();
    if (!validate()) return;
    setReviewing(true);
    try {
      const result = await bff<CheckoutPreview>("/api/checkout/preview", {
        method: "POST",
        body: JSON.stringify({
          governorateId,
          couponCode: coupon.trim() || null,
        }),
      });
      setPreview(result);
      analytics.track("CheckoutStarted");
    } catch (err) {
      setErrors({ form: getError(err) });
    } finally {
      setReviewing(false);
    }
  }

  async function placeOrder() {
    if (placing || !preview) return;
    if (preview.warnings.length > 0) {
      setErrors({ form: t("warnings") });
      return;
    }
    setPlacing(true);
    try {
      const order = await bff<CreateOrderResponse>("/api/orders", {
        method: "POST",
        body: JSON.stringify({
          governorateId,
          cityDistrict: cityDistrict.trim(),
          detailedAddress: detailedAddress.trim(),
          notes: notes.trim() || null,
          paymentMethod: method,
          couponCode: coupon.trim() || null,
          idempotencyKey,
        }),
      });
      analytics.track("OrderPlaced", { orderId: order.orderId });
      await refreshCart();
      router.push(`/${locale}/order-success/${order.orderNumber}` as Route);
    } catch (err) {
      setErrors({ form: getError(err) });
      toast.show(getError(err), "error");
      setPlacing(false);
      // Force a fresh review (stock/price may have changed between preview and submit).
      setPreview(null);
      setIdempotencyKey(newIdempotencyKey());
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>
      {errors.form && (
        <div className="mb-4">
          <FormAlert>{errors.form}</FormAlert>
        </div>
      )}

      <form onSubmit={review} noValidate className="space-y-6">
        <section className="rounded-card border border-bordergold/40 bg-cream p-5">
          <h2 className="mb-3 text-lg font-semibold text-deepbrown">{t("contact")}</h2>
          <p className="text-sm text-mocha">
            {t("name")}: {customer.fullName}
          </p>
          <p className="mt-1 text-sm text-mocha">
            {t("phone")}: <span dir="ltr">{customer.phoneNumber}</span>{" "}
            <Link href="/change-phone" className="text-bronze hover:underline">
              ({t("changePhone")})
            </Link>
          </p>
        </section>

        <section className="space-y-4 rounded-card border border-bordergold/40 bg-cream p-5">
          <h2 className="text-lg font-semibold text-deepbrown">{t("shippingAddress")}</h2>
          <FormField label={t("governorate")} htmlFor="governorate" error={errors.governorate}>
            <Select
              id="governorate"
              value={governorateId}
              onChange={(e) => {
                setGovernorateId(e.target.value);
                invalidatePreview();
              }}
              aria-invalid={errors.governorate ? true : undefined}
            >
              <option value="">{t("selectGovernorate")}</option>
              {governorates.map((g) => (
                <option key={g.id} value={g.id}>
                  {pick(locale, g.nameAr, g.nameEn)}
                </option>
              ))}
            </Select>
          </FormField>
          <FormField label={t("cityDistrict")} htmlFor="cityDistrict" error={errors.cityDistrict}>
            <Input
              id="cityDistrict"
              value={cityDistrict}
              onChange={(e) => setCityDistrict(e.target.value)}
              aria-invalid={errors.cityDistrict ? true : undefined}
            />
          </FormField>
          <FormField
            label={t("detailedAddress")}
            htmlFor="detailedAddress"
            error={errors.detailedAddress}
          >
            <Textarea
              id="detailedAddress"
              value={detailedAddress}
              onChange={(e) => setDetailedAddress(e.target.value)}
              aria-invalid={errors.detailedAddress ? true : undefined}
            />
          </FormField>
          <FormField label={t("notes")} htmlFor="notes">
            <Textarea
              id="notes"
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </FormField>
        </section>

        <section className="space-y-3 rounded-card border border-bordergold/40 bg-cream p-5">
          <h2 className="text-lg font-semibold text-deepbrown">{t("paymentMethod")}</h2>
          {activeMethods.length === 0 ? (
            <p className="text-sm text-taupe">{tp("unavailable")}</p>
          ) : (
            <fieldset>
              <legend className="sr-only">{t("paymentMethod")}</legend>
              <div className="space-y-2">
                {activeMethods.map((m) => (
                  <label
                    key={m.method}
                    className="flex cursor-pointer items-center gap-3 rounded-xl border border-bordergold/60 px-4 py-3"
                  >
                    <input
                      type="radio"
                      name="paymentMethod"
                      value={m.method}
                      checked={method === m.method}
                      onChange={() => setMethod(m.method)}
                    />
                    <span className="text-mocha">{tp(m.method)}</span>
                  </label>
                ))}
              </div>
            </fieldset>
          )}
          {errors.method && (
            <p role="alert" className="text-xs text-red-600">
              {errors.method}
            </p>
          )}
        </section>

        <section className="space-y-3 rounded-card border border-bordergold/40 bg-cream p-5">
          <FormField label={t("couponDiscount")} htmlFor="coupon">
            <Input
              id="coupon"
              value={coupon}
              onChange={(e) => {
                setCoupon(e.target.value);
                invalidatePreview();
              }}
            />
          </FormField>
        </section>

        {!preview && (
          <Button type="submit" fullWidth disabled={reviewing}>
            {t("summary")}
          </Button>
        )}
      </form>

      {preview && (
        <section className="mt-6 space-y-4 rounded-card border border-bordergold/40 bg-ivory/40 p-5">
          <h2 className="text-lg font-semibold text-deepbrown">{t("summary")}</h2>
          {preview.warnings.length > 0 && (
            <ul className="space-y-1 rounded-lg bg-cream px-3 py-2 text-sm text-bronze">
              {preview.warnings.map((w, i) => (
                <li key={i}>{w}</li>
              ))}
            </ul>
          )}
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-taupe">{t("subtotal")}</dt>
              <dd>{formatPrice(preview.productSubtotalBeforeDiscount, locale)}</dd>
            </div>
            {preview.productDiscountTotal > 0 && (
              <div className="flex justify-between text-bronze">
                <dt>{t("productDiscount")}</dt>
                <dd>-{formatPrice(preview.productDiscountTotal, locale)}</dd>
              </div>
            )}
            {preview.couponDiscountTotal > 0 && (
              <div className="flex justify-between text-bronze">
                <dt>{t("couponDiscount")}</dt>
                <dd>-{formatPrice(preview.couponDiscountTotal, locale)}</dd>
              </div>
            )}
            <div className="flex justify-between">
              <dt className="text-taupe">{t("shippingFee")}</dt>
              <dd>{formatPrice(preview.shippingFee, locale)}</dd>
            </div>
            <div className="flex justify-between border-t border-bordergold/40 pt-2 text-base font-semibold text-mocha">
              <dt>{t("grandTotal")}</dt>
              <dd>{formatPrice(preview.grandTotal, locale)}</dd>
            </div>
          </dl>
          {preview.warnings.length > 0 && <FormAlert>{t("warnings")}</FormAlert>}
          <Button onClick={placeOrder} fullWidth disabled={placing || preview.warnings.length > 0}>
            {placing ? t("placing") : t("placeOrder")}
          </Button>
        </section>
      )}
    </div>
  );
}

function newIdempotencyKey() {
  return globalThis.crypto?.randomUUID?.() ?? `checkout-${Date.now()}-${Math.random()}`;
}
