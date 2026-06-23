import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { pick } from "@/lib/i18n/localize";
import { getPage } from "@/lib/api/public";
import type { StaticPage } from "@/lib/api/types";
import { ApiError } from "@/lib/api/errors";
import { buildPublicMetadata, entityTitle, pageMetaDescription } from "@/lib/seo/metadata";
import { SafeHtml } from "@/components/ui/safe-html";
import { WhatsAppLink } from "@/components/ui/whatsapp-link";
import { PAGE_SLUGS } from "@/lib/constants";

type PageProps = {
  params: Promise<{ locale: string; slug: string }>;
};

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale, slug } = await params;
  let page: StaticPage | null = null;
  try {
    page = await getPage(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return {};
    throw error;
  }
  if (!page) return {};
  const title = pick(locale, page.titleAr, page.titleEn);
  const description = pageMetaDescription(
    locale,
    pick(locale, page.contentAr, page.contentEn),
    title,
  );
  return buildPublicMetadata({
    locale,
    title: entityTitle(locale, title),
    description,
    pathAr: `/page/${page.slugAr}`,
    pathEn: `/page/${page.slugEn}`,
  });
}

export default async function StaticContentPage({ params }: PageProps) {
  const { locale, slug } = await params;
  setRequestLocale(locale);

  let page: StaticPage | null = null;
  try {
    page = await getPage(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound();
    throw error;
  }

  const tc = await getTranslations("common");
  const title = pick(locale, page.titleAr, page.titleEn);
  const content = pick(locale, page.contentAr, page.contentEn);
  const isReturns = page.key === PAGE_SLUGS.returns || slug === PAGE_SLUGS.returns;

  return (
    <article className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <h1 className="mb-6 text-2xl font-semibold text-deepbrown sm:text-3xl">{title}</h1>
      {content && <SafeHtml html={content} />}

      {isReturns && (
        <div className="mt-8 rounded-card border border-bordergold/40 bg-ivory/50 p-6 text-center">
          <WhatsAppLink className="justify-center rounded-pill bg-bronze px-6 py-3 text-cream hover:bg-mocha">
            {tc("whatsappSupport")}
          </WhatsAppLink>
        </div>
      )}
    </article>
  );
}
