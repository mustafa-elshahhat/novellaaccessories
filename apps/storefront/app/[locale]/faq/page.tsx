import type { Metadata } from "next";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick } from "@/lib/i18n/localize";
import type { Locale } from "@/lib/i18n/routing";
import { getFaq } from "@/lib/api/public";
import type { StaticPage } from "@/lib/api/types";
import { buildPublicMetadata } from "@/lib/seo/metadata";
import { JsonLd, faqJsonLd } from "@/lib/seo/jsonld";
import { SafeHtml } from "@/components/ui/safe-html";
import { ContentBlock } from "@/components/ui/content-block";

type PageProps = { params: Promise<{ locale: string }> };

async function loadFaq(): Promise<StaticPage> {
  return getFaq();
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;
  const tf = await getTranslations({ locale, namespace: "footer" });
  const faq = await loadFaq();
  const title = pick(locale, faq.titleAr, faq.titleEn) || tf("faq");
  return buildPublicMetadata({ locale, title, pathAr: "/faq", pathEn: "/faq" });
}

export default async function FaqPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale);
  const faq = await loadFaq();

  const title = pick(locale, faq.titleAr, faq.titleEn);
  const content = pick(locale, faq.contentAr, faq.contentEn);
  const faqItems = extractFaqItems(content);

  return (
    <article className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      {faqItems.length > 0 && <JsonLd data={faqJsonLd(faqItems)} />}
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown sm:text-3xl">{title}</h1>
      <SafeHtml html={content} />
      <ContentBlock ar={faq.aeoSummaryAr} en={faq.aeoSummaryEn} locale={locale as Locale} />
      <ContentBlock ar={faq.geoContentAr} en={faq.geoContentEn} locale={locale as Locale} />
    </article>
  );
}

function extractFaqItems(content: string): Array<{ question: string; answer: string }> {
  const lines = content
    .replace(/<br\s*\/?\s*>/gi, "\n")
    .replace(/<\/p>/gi, "\n")
    .replace(/<[^>]+>/g, "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  const items: Array<{ question: string; answer: string }> = [];
  for (let i = 0; i < lines.length; i += 1) {
    const line = lines[i];
    if (!line) continue;
    const question = line.replace(/^(Q|س)\s*[:：]\s*/i, "");
    const answerLine = lines[i + 1];
    if (question === line || !answerLine) continue;
    const answer = answerLine.replace(/^(A|ج)\s*[:：]\s*/i, "");
    if (answer !== answerLine) items.push({ question, answer });
  }
  return items;
}
