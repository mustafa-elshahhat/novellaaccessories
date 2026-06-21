"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/lib/i18n/navigation";
import { useAuth } from "@/features/auth/auth-provider";
import { useCart } from "@/features/cart/cart-provider";
import {
  HomeIcon,
  GridIcon,
  BagIcon,
  UserIcon,
  MenuIcon,
  CloseIcon,
} from "@/components/icons";
import { LocaleSwitcher } from "@/components/ui/locale-switcher";
import { WhatsAppLink } from "@/components/ui/whatsapp-link";
import { isActivePath } from "@/lib/utils/active";
import { cn } from "@/lib/utils/cn";
import { PAGE_SLUGS } from "@/lib/constants";

export function BottomNav() {
  const t = useTranslations("nav");
  const pathname = usePathname();
  const { customer } = useAuth();
  const { count } = useCart();
  const [menuOpen, setMenuOpen] = useState(false);

  const tabs = [
    { key: "home", href: "/", Icon: HomeIcon, badge: 0 },
    { key: "categories", href: "/categories", Icon: GridIcon, badge: 0 },
    { key: "cart", href: "/cart", Icon: BagIcon, badge: count },
    { key: "account", href: customer ? "/account" : "/login", Icon: UserIcon, badge: 0 },
  ] as const;

  return (
    <>
      <nav
        aria-label={t("menu")}
        className="fixed inset-x-0 bottom-0 z-40 border-t border-bordergold/50 bg-cream/95 pb-[env(safe-area-inset-bottom)] backdrop-blur lg:hidden"
      >
        <ul className="flex items-stretch justify-around">
          {tabs.map(({ key, href, Icon, badge }) => {
            const active = isActivePath(pathname, href);
            return (
              <li key={key} className="flex-1">
                <Link
                  href={href}
                  aria-current={active ? "page" : undefined}
                  className={cn(
                    "flex flex-col items-center gap-0.5 py-2 text-[11px]",
                    active ? "text-bronze" : "text-taupe",
                  )}
                >
                  <span className="relative">
                    <Icon className="h-6 w-6" />
                    {badge > 0 && (
                      <span className="absolute -end-2 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-rosegold px-1 text-[9px] font-semibold text-cream">
                        {badge}
                      </span>
                    )}
                  </span>
                  {t(key)}
                </Link>
              </li>
            );
          })}
          <li className="flex-1">
            <button
              type="button"
              onClick={() => setMenuOpen(true)}
              aria-haspopup="dialog"
              aria-expanded={menuOpen}
              className="flex w-full flex-col items-center gap-0.5 py-2 text-[11px] text-taupe"
            >
              <MenuIcon className="h-6 w-6" />
              {t("menu")}
            </button>
          </li>
        </ul>
      </nav>
      {menuOpen && <MobileMenu onClose={() => setMenuOpen(false)} loggedIn={!!customer} />}
    </>
  );
}

function MobileMenu({
  onClose,
  loggedIn,
}: {
  onClose: () => void;
  loggedIn: boolean;
}) {
  const t = useTranslations();

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prev;
    };
  }, [onClose]);

  const links: Array<{ href: string; label: string }> = [
    { href: "/products", label: t("nav.products") },
    { href: "/offers", label: t("nav.offers") },
    { href: `/page/${PAGE_SLUGS.about}`, label: t("nav.about") },
    { href: `/page/${PAGE_SLUGS.contact}`, label: t("nav.contact") },
    { href: "/faq", label: t("footer.faq") },
    { href: `/page/${PAGE_SLUGS.shipping}`, label: t("footer.shippingPolicy") },
    { href: `/page/${PAGE_SLUGS.returns}`, label: t("footer.returnsPolicy") },
    { href: `/page/${PAGE_SLUGS.privacy}`, label: t("footer.privacyPolicy") },
    { href: `/page/${PAGE_SLUGS.terms}`, label: t("footer.termsPolicy") },
  ];

  return (
    <div className="fixed inset-0 z-50 lg:hidden" role="dialog" aria-modal="true" aria-label={t("nav.menu")}>
      <div className="absolute inset-0 bg-deepbrown/40" onClick={onClose} aria-hidden />
      <div className="absolute inset-x-0 bottom-0 max-h-[80vh] overflow-y-auto rounded-t-3xl border-t border-bordergold bg-cream p-6 pb-[calc(1.5rem+env(safe-area-inset-bottom))]">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-deepbrown">{t("nav.menu")}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={t("common.close")}
            className="inline-flex h-10 w-10 items-center justify-center rounded-full text-mocha hover:bg-ivory"
          >
            <CloseIcon className="h-5 w-5" />
          </button>
        </div>
        <ul className="space-y-1">
          {links.map((link) => (
            <li key={link.href}>
              <Link
                href={link.href}
                onClick={onClose}
                className="block rounded-xl px-3 py-2.5 text-mocha hover:bg-ivory"
              >
                {link.label}
              </Link>
            </li>
          ))}
          <li>
            <Link
              href={loggedIn ? "/account" : "/login"}
              onClick={onClose}
              className="block rounded-xl px-3 py-2.5 text-mocha hover:bg-ivory"
            >
              {loggedIn ? t("nav.account") : t("nav.login")}
            </Link>
          </li>
        </ul>
        <div className="mt-4 flex items-center justify-between border-t border-bordergold/40 pt-4">
          <LocaleSwitcher />
          <WhatsAppLink className="text-sm text-bronze">
            {t("common.whatsappSupport")}
          </WhatsAppLink>
        </div>
      </div>
    </div>
  );
}
