import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { AuthCard } from "@/features/auth/auth-card";
import { RegisterForm } from "@/features/auth/register-form";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { sanitizeReturnUrl } from "@/lib/security/redirect";

type PageProps = {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "auth", "registerTitle");
}

export default async function RegisterPage({ params, searchParams }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const sp = await searchParams;
  const returnUrl = sanitizeReturnUrl(
    typeof sp.returnUrl === "string" ? sp.returnUrl : null,
    `/${locale}/account`,
  );
  const t = await getTranslations("auth");

  return (
    <AuthCard
      title={t("registerTitle")}
      subtitle={t("registerSubtitle")}
      footer={
        <>
          {t("haveAccount")}{" "}
          <Link href="/login" className="text-bronze hover:underline">
            {t("login")}
          </Link>
        </>
      }
    >
      <RegisterForm returnUrl={returnUrl} />
    </AuthCard>
  );
}
