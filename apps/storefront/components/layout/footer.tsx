import { useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { LocaleSwitcher } from "@/components/ui/locale-switcher";
import { WhatsAppLink } from "@/components/ui/whatsapp-link";
import { PAGE_SLUGS } from "@/lib/constants";

export function Footer() {
  const t = useTranslations();
  const year = new Date().getFullYear();

  return (
    <footer className="mt-16 border-t border-bordergold/40 bg-ivory/60">
      <div className="mx-auto grid max-w-6xl gap-8 px-6 py-10 sm:grid-cols-2 lg:grid-cols-4">
        <div>
          <p className="font-[family-name:var(--font-heading)] text-xl lowercase tracking-[0.2em] text-deepbrown">
            novella
          </p>
          <p className="mt-2 text-sm text-taupe">{t("footer.tagline")}</p>
        </div>

        <nav aria-label={t("footer.shop")}>
          <h2 className="mb-3 text-sm font-semibold text-mocha">{t("footer.shop")}</h2>
          <ul className="space-y-2 text-sm text-taupe">
            <li><Link href="/categories" className="hover:text-bronze">{t("nav.categories")}</Link></li>
            <li><Link href="/products" className="hover:text-bronze">{t("nav.products")}</Link></li>
            <li><Link href="/offers" className="hover:text-bronze">{t("nav.offers")}</Link></li>
          </ul>
        </nav>

        <nav aria-label={t("footer.help")}>
          <h2 className="mb-3 text-sm font-semibold text-mocha">{t("footer.help")}</h2>
          <ul className="space-y-2 text-sm text-taupe">
            <li><Link href={`/page/${PAGE_SLUGS.contact}`} className="hover:text-bronze">{t("nav.contact")}</Link></li>
            <li><Link href="/faq" className="hover:text-bronze">{t("footer.faq")}</Link></li>
            <li><Link href={`/page/${PAGE_SLUGS.shipping}`} className="hover:text-bronze">{t("footer.shippingPolicy")}</Link></li>
            <li><Link href={`/page/${PAGE_SLUGS.returns}`} className="hover:text-bronze">{t("footer.returnsPolicy")}</Link></li>
          </ul>
        </nav>

        <nav aria-label={t("footer.policies")}>
          <h2 className="mb-3 text-sm font-semibold text-mocha">{t("footer.policies")}</h2>
          <ul className="space-y-2 text-sm text-taupe">
            <li><Link href={`/page/${PAGE_SLUGS.privacy}`} className="hover:text-bronze">{t("footer.privacyPolicy")}</Link></li>
            <li><Link href={`/page/${PAGE_SLUGS.terms}`} className="hover:text-bronze">{t("footer.termsPolicy")}</Link></li>
            <li><Link href={`/page/${PAGE_SLUGS.about}`} className="hover:text-bronze">{t("nav.about")}</Link></li>
          </ul>
          <div className="mt-4">
            <WhatsAppLink className="text-sm text-bronze hover:text-mocha">
              {t("common.whatsappSupport")}
            </WhatsAppLink>
          </div>
        </nav>
      </div>

      <div className="border-t border-bordergold/30">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-6 py-4 text-xs text-taupe sm:flex-row">
          <p>© {year} novella. {t("footer.rights")}</p>
          <LocaleSwitcher />
        </div>
      </div>
    </footer>
  );
}
