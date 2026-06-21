import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";
import { AuthCard } from "@/features/auth/auth-card";
import { LoginForm } from "@/features/auth/login-form";
import { privatePageMetadata } from "@/lib/seo/metadata";
import { sanitizeReturnUrl } from "@/lib/security/redirect";

type PageProps = {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  return privatePageMetadata(locale, "auth", "loginTitle");
}

export default async function LoginPage({ params, searchParams }: PageProps) {
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
      title={t("loginTitle")}
      subtitle={t("loginSubtitle")}
      footer={
        <>
          {t("noAccount")}{" "}
          <Link href="/register" className="text-bronze hover:underline">
            {t("register")}
          </Link>
        </>
      }
    >
      <LoginForm returnUrl={returnUrl} />
    </AuthCard>
  );
}
