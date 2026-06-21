import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import { getFaq } from "@/lib/api/public";
import type { StaticPage } from "@/lib/api/types";
import { buildPublicMetadata } from "@/lib/seo/metadata";
import { SafeHtml } from "@/components/ui/safe-html";
import { ContentBlock } from "@/components/ui/content-block";
import { EmptyState } from "@/components/ui/states";

type PageProps = { params: Promise<{ locale: string }> };

async function loadFaq(): Promise<StaticPage | null> {
  try {
    return await getFaq();
  } catch {
    return null;
  }
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  const tf = await getTranslations({ locale, namespace: "footer" });
  const faq = await loadFaq();
  const title = faq ? pick(locale, faq.titleAr, faq.titleEn) : tf("faq");
  return buildPublicMetadata({ locale, title, pathAr: "/faq", pathEn: "/faq" });
}

export default async function FaqPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const tf = await getTranslations("footer");
  const faq = await loadFaq();

  const title = faq ? pick(locale, faq.titleAr, faq.titleEn) : tf("faq");
  const content = faq ? pick(locale, faq.contentAr, faq.contentEn) : "";

  return (
    <article className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown sm:text-3xl">{title}</h1>
      {content ? <SafeHtml html={content} /> : <EmptyState title={tf("faq")} />}
      {faq && (
        <ContentBlock
          ar={faq.aeoSummaryAr}
          en={faq.aeoSummaryEn}
          locale={locale as Locale}
        />
      )}
    </article>
  );
}
