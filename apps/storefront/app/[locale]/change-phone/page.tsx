import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthCard } from "@/features/auth/auth-card";
import { ChangePhoneForm } from "@/features/auth/change-phone-form";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { getCurrentCustomer } from "@/lib/auth/server";

type PageProps = {
  params: Promise<{ locale: string }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "auth", "changePhoneTitle");
}

export default async function ChangePhonePage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  const customer = await getCurrentCustomer();
  if (!customer) {
    redirect(`/${locale}/login?returnUrl=/${locale}/change-phone`);
  }

  const t = await getTranslations("auth");

  return (
    <AuthCard title={t("changePhoneTitle")} subtitle={t("changePhoneSubtitle")}>
      <ChangePhoneForm currentPhone={customer.phoneNumber} />
    </AuthCard>
  );
}
