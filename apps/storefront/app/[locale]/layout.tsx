import type { ReactNode } from "react";
import { NextIntlClientProvider, hasLocale } from "next-intl";
import { getMessages, getTranslations, setRequestLocale } from "next-intl/server";
import { notFound } from "next/navigation";
import { routing, getDirection } from "@/lib/i18n/routing";
import { Providers } from "@/components/providers";
import { DesktopHeader } from "@/components/layout/desktop-header";
import { MobileHeader } from "@/components/layout/mobile-header";
import { BottomNav } from "@/components/layout/bottom-nav";
import { Footer } from "@/components/layout/footer";
import "@/styles/globals.css";

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) {
    notFound();
  }
  setRequestLocale(locale);

  const messages = await getMessages();
  const t = await getTranslations({ locale, namespace: "a11y" });
  const dir = getDirection(locale);

  return (
    <html lang={locale} dir={dir}>
      <body className="has-bottom-nav flex min-h-screen flex-col">
        <a href="#main-content" className="skip-link">
          {t("skipToContent")}
        </a>
        <NextIntlClientProvider messages={messages} locale={locale}>
          <Providers>
            <MobileHeader />
            <DesktopHeader />
            <main id="main-content" className="flex-1">
              {children}
            </main>
            <Footer />
            <BottomNav />
          </Providers>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
