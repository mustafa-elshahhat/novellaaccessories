import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { getCurrentCustomer } from "@/lib/auth/server";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { Card } from "@/components/ui/card";
import { LogoutButton } from "@/features/account/logout-button";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "account", "title");
}

export default async function AccountPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/account`);
  }

  const t = await getTranslations("account");

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown">{t("title")}</h1>

      <Card className="p-5">
        <h2 className="mb-3 text-lg font-semibold text-deepbrown">{t("profile")}</h2>
        <dl className="space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-taupe">{t("name")}</dt>
            <dd className="text-mocha">{customer.fullName}</dd>
          </div>
          <div className="flex items-center justify-between">
            <dt className="text-taupe">{t("phone")}</dt>
            <dd className="flex items-center gap-2 text-mocha">
              <span dir="ltr">{customer.phoneNumber}</span>
              <span className="rounded-pill bg-ivory px-2 py-0.5 text-xs text-bronze">
                {customer.isPhoneVerified ? t("verified") : t("notVerified")}
              </span>
            </dd>
          </div>
        </dl>
        <Link
          href="/change-phone"
          className="mt-4 inline-block text-sm text-bronze hover:underline"
        >
          {t("changePhone")}
        </Link>
      </Card>

      <div className="mt-6 flex flex-wrap items-center justify-between gap-4">
        <Link
          href="/account/orders"
          className="inline-flex rounded-pill border border-champagne px-6 py-3 text-bronze hover:bg-ivory"
        >
          {t("myOrders")}
        </Link>
        <LogoutButton />
      </div>
    </div>
  );
}
