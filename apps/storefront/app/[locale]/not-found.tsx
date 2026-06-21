import { getTranslations } from "next-intl/server";
import { Link } from "@/lib/i18n/navigation";

export default async function LocaleNotFound() {
  const t = await getTranslations("notFound");
  return (
    <section className="mx-auto flex max-w-xl flex-col items-center px-4 py-24 text-center">
      <p className="text-6xl font-semibold text-champagne">404</p>
      <h1 className="mt-4 text-2xl font-semibold">{t("title")}</h1>
      <p className="mt-2 text-taupe">{t("description")}</p>
      <Link
        href="/"
        className="mt-8 inline-flex items-center rounded-pill bg-bronze px-6 py-3 text-cream"
      >
        {t("backHome")}
      </Link>
    </section>
  );
}
