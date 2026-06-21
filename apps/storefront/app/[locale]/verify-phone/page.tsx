import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AuthCard } from "@/features/auth/auth-card";
import { VerifyPhoneForm } from "@/features/auth/verify-phone-form";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { sanitizeReturnUrl } from "@/lib/security/redirect";

type PageProps = {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "otp", "title");
}

export default async function VerifyPhonePage({ params, searchParams }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const sp = await searchParams;
  const phone = typeof sp.phone === "string" ? sp.phone : "";
  const returnUrl = sanitizeReturnUrl(
    typeof sp.returnUrl === "string" ? sp.returnUrl : null,
    `/${locale}/account`,
  );
  const t = await getTranslations("otp");

  return (
    <AuthCard title={t("title")}>
      <VerifyPhoneForm phone={phone} returnUrl={returnUrl} />
    </AuthCard>
  );
}
