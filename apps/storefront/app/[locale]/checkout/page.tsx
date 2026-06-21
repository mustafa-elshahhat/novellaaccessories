import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { getCurrentCustomer } from "@/lib/auth/server";
import { getGovernorates } from "@/lib/api/public";
import { getPaymentMethods } from "@/lib/api/payments";
import type { PublicGovernorate, PaymentMethodInfo } from "@/lib/api/types";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { CheckoutForm } from "@/features/checkout/checkout-form";
import { EmptyState } from "@/components/ui/states";

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "checkout", "title");
}

export default async function CheckoutPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/checkout`);
  }

  const t = await getTranslations("checkout");

  if (!customer.isPhoneVerified) {
    return (
      <EmptyState
        title={t("verifyRequired")}
        action={
          <Link
            href="/verify-phone"
            className="inline-flex rounded-pill bg-bronze px-6 py-3 text-cream"
          >
            {t("verifyCta")}
          </Link>
        }
      />
    );
  }

  let governorates: PublicGovernorate[] = [];
  let paymentMethods: PaymentMethodInfo[] = [];
  try {
    governorates = await getGovernorates();
  } catch {
    governorates = [];
  }
  try {
    paymentMethods = await getPaymentMethods();
  } catch {
    paymentMethods = [];
  }

  return (
    <CheckoutForm
      customer={customer}
      governorates={governorates}
      paymentMethods={paymentMethods}
    />
  );
}
