import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick } from "@/lib/i18n/localize";
import { formatPrice, formatDate } from "@/lib/format";
import { getCurrentCustomer } from "@/lib/auth/server";
import { myOrder } from "@/lib/api/orders";
import type { CustomerOrder } from "@/lib/api/types";
import { ApiError } from "@/lib/api/errors";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { Card } from "@/components/ui/card";
import { CancelOrderButton } from "@/features/account/cancel-order-button";

type PageProps = {
  params: Promise<{ locale: string; orderNumber: string }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "orders", "title");
}

const TIMELINE_STEPS: Array<{ status: CustomerOrder["status"]; field: keyof CustomerOrder }> = [
  { status: "Pending", field: "createdAt" },
  { status: "Confirmed", field: "confirmedAt" },
  { status: "Preparing", field: "preparingAt" },
  { status: "Shipped", field: "shippedAt" },
  { status: "Delivered", field: "deliveredAt" },
];

export default async function OrderDetailPage({ params }: PageProps) {
  const { locale, orderNumber } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/account/orders/${orderNumber}`);
  }

  let order: CustomerOrder;
  try {
    order = await myOrder(orderNumber);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  const t = await getTranslations("orders");
  const tp = await getTranslations("payment");

  const canCancel = order.status === "Pending" || order.status === "Confirmed";

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-deepbrown">
            {t("orderNumber", { number: order.orderNumber })}
          </h1>
          <p className="mt-1 text-sm text-bronze">{t(`status_${order.status}`)}</p>
        </div>
        {canCancel && <CancelOrderButton orderNumber={order.orderNumber} />}
      </div>

      <Card className="mb-6 p-5">
        <h2 className="mb-3 text-lg font-semibold text-deepbrown">{t("items")}</h2>
        <ul className="divide-y divide-bordergold/30">
          {order.items.map((item, index) => {
            const name = pick(locale, item.productNameAr, item.productNameEn);
            const variant = pick(locale, item.variantNameAr, item.variantNameEn);
            return (
              <li
                key={index}
                className="flex items-start justify-between gap-3 py-3"
              >
                <div>
                  <p className="text-mocha">{name}</p>
                  {variant && <p className="text-xs text-taupe">{variant}</p>}
                  <p className="text-xs text-taupe">× {item.quantity}</p>
                </div>
                <p className="font-medium text-mocha">{formatPrice(item.lineTotal, locale)}</p>
              </li>
            );
          })}
        </ul>

        <dl className="mt-4 space-y-2 border-t border-bordergold/40 pt-4 text-sm">
          <div className="flex justify-between">
            <dt className="text-taupe">{t("subtotal")}</dt>
            <dd>{formatPrice(order.productSubtotalBeforeDiscount, locale)}</dd>
          </div>
          {order.productDiscountTotal > 0 && (
            <div className="flex justify-between text-bronze">
              <dt>{t("productDiscount")}</dt>
              <dd>-{formatPrice(order.productDiscountTotal, locale)}</dd>
            </div>
          )}
          {order.couponDiscountTotal > 0 && (
            <div className="flex justify-between text-bronze">
              <dt>{t("couponDiscount")}</dt>
              <dd>-{formatPrice(order.couponDiscountTotal, locale)}</dd>
            </div>
          )}
          <div className="flex justify-between">
            <dt className="text-taupe">{t("shippingFee")}</dt>
            <dd>{formatPrice(order.shippingFee, locale)}</dd>
          </div>
          <div className="flex justify-between border-t border-bordergold/40 pt-2 font-semibold text-mocha">
            <dt>{t("total")}</dt>
            <dd>{formatPrice(order.grandTotal, locale)}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-taupe">{t("paymentMethod")}</dt>
            <dd>{tp(order.paymentMethod)}</dd>
          </div>
        </dl>
      </Card>

      <Card className="mb-6 p-5">
        <h2 className="mb-3 text-lg font-semibold text-deepbrown">{t("shippingAddress")}</h2>
        <p className="text-sm text-mocha">{order.customerName}</p>
        <p className="text-sm text-mocha" dir="ltr">
          {order.customerPhone}
        </p>
        <p className="text-sm text-taupe">
          {pick(locale, order.governorateNameAr, order.governorateNameEn)} — {order.cityDistrict}
        </p>
        <p className="text-sm text-taupe">{order.detailedAddress}</p>
        {order.notes && <p className="mt-1 text-sm text-taupe">{order.notes}</p>}
        {order.trackingNumber && (
          <p className="mt-2 text-sm text-mocha">
            {t("tracking")}: <span dir="ltr">{order.trackingNumber}</span>
          </p>
        )}
      </Card>

      <Card className="p-5">
        <h2 className="mb-4 text-lg font-semibold text-deepbrown">{t("timeline")}</h2>
        <ol className="space-y-3">
          {TIMELINE_STEPS.map((step) => {
            const at = order[step.field] as string | null;
            const done = Boolean(at);
            return (
              <li key={step.status} className="flex items-center gap-3">
                <span
                  className={
                    done
                      ? "h-2.5 w-2.5 rounded-full bg-bronze"
                      : "h-2.5 w-2.5 rounded-full border border-bordergold"
                  }
                />
                <span className={done ? "text-mocha" : "text-taupe"}>
                  {t(`status_${step.status}`)}
                </span>
                {at && <span className="ms-auto text-xs text-taupe">{formatDate(at, locale)}</span>}
              </li>
            );
          })}
          {order.status === "Cancelled" && order.cancelledAt && (
            <li className="flex items-center gap-3">
              <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
              <span className="text-red-600">{t("status_Cancelled")}</span>
              <span className="ms-auto text-xs text-taupe">
                {formatDate(order.cancelledAt, locale)}
              </span>
            </li>
          )}
        </ol>
      </Card>
    </div>
  );
}
