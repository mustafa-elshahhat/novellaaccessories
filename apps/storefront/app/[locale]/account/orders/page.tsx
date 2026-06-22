import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { formatPrice, formatDate } from "@/lib/format";
import { getCurrentCustomer } from "@/lib/auth/server";
import { myOrders } from "@/lib/api/orders";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/states";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "orders", "title");
}

export default async function OrdersPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/account/orders`);
  }

  const t = await getTranslations("orders");
  const tp = await getTranslations("payment");

  const res = await myOrders();
  const orders = Array.isArray(res) ? res : res.items;

  if (orders.length === 0) {
    return <EmptyState title={t("empty")} />;
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>
      <ul className="space-y-4">
        {orders.map((order) => (
          <li key={order.orderNumber}>
            <Card className="flex flex-wrap items-center justify-between gap-4 p-5">
              <div>
                <p className="font-semibold text-mocha">
                  {t("orderNumber", { number: order.orderNumber })}
                </p>
                <p className="text-sm text-taupe">{formatDate(order.createdAt, locale)}</p>
                <p className="mt-1 text-sm text-bronze">{t(`status_${order.status}`)}</p>
              </div>
              <div className="text-end">
                <p className="font-semibold text-mocha">
                  {formatPrice(order.grandTotal, locale)}
                </p>
                <p className="text-xs text-taupe">{tp(order.paymentMethod)}</p>
                <Link
                  href={`/account/orders/${order.orderNumber}`}
                  className="mt-1 inline-block text-sm text-bronze hover:underline"
                >
                  {t("view")}
                </Link>
              </div>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
