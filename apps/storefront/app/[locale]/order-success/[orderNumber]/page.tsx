import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { formatPrice } from "@/lib/format";
import { getCurrentCustomer } from "@/lib/auth/server";
import { myOrder } from "@/lib/api/orders";
import type { CustomerOrder } from "@/lib/api/types";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { CheckIcon } from "@/components/icons";
import { WhatsAppLink } from "@/components/ui/whatsapp-link";

type PageProps = {
  params: Promise<{ locale: string; orderNumber: string }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "orderSuccess", "title");
}

export default async function OrderSuccessPage({ params }: PageProps) {
  const { locale, orderNumber } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/order-success/${orderNumber}`);
  }

  const t = await getTranslations("orderSuccess");
  const tp = await getTranslations("payment");
  const to = await getTranslations("orders");
  const tc = await getTranslations("common");

  // Validate against the backend (never trust the route param blindly).
  let order: CustomerOrder | null = null;
  try {
    order = await myOrder(orderNumber);
  } catch {
    order = null;
  }

  return (
    <div className="mx-auto flex max-w-xl flex-col items-center px-4 py-16 text-center">
      <span className="flex h-16 w-16 items-center justify-center rounded-full bg-bronze text-cream">
        <CheckIcon className="h-8 w-8" />
      </span>
      <h1 className="mt-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>
      <p className="mt-2 text-taupe">{t("subtitle")}</p>

      <dl className="mt-6 w-full max-w-sm space-y-2 rounded-card border border-bordergold/40 bg-cream p-5 text-sm">
        <div className="flex justify-between">
          <dt className="text-taupe">{t("orderNumber")}</dt>
          <dd className="font-semibold text-mocha">{order?.orderNumber ?? orderNumber}</dd>
        </div>
        {order && (
          <>
            <div className="flex justify-between">
              <dt className="text-taupe">{t("status")}</dt>
              <dd className="text-mocha">{to(`status_${order.status}`)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-taupe">{t("paymentMethod")}</dt>
              <dd className="text-mocha">{tp(order.paymentMethod)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-taupe">{t("total")}</dt>
              <dd className="font-semibold text-mocha">
                {formatPrice(order.grandTotal, locale)}
              </dd>
            </div>
          </>
        )}
      </dl>

      <div className="mt-6 flex flex-col items-center gap-3">
        <Link
          href={`/account/orders/${order?.orderNumber ?? orderNumber}`}
          className="text-bronze hover:underline"
        >
          {t("viewOrder")}
        </Link>
        <Link
          href="/"
          className="inline-flex rounded-pill bg-bronze px-6 py-3 text-cream"
        >
          {tc("continueShopping")}
        </Link>
        <WhatsAppLink className="mt-2 text-sm text-bronze hover:text-mocha">
          {t("support")}
        </WhatsAppLink>
      </div>
    </div>
  );
}
