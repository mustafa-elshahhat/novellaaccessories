"use client";

import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/lib/i18n/navigation";
import { Brand } from "./brand";
import { LocaleSwitcher } from "@/components/ui/locale-switcher";
import { useAuth } from "@/features/auth/auth-provider";
import { useCart } from "@/features/cart/cart-provider";
import { BagIcon, UserIcon } from "@/components/icons";
import { isActivePath } from "@/lib/utils/active";
import { cn } from "@/lib/utils/cn";

const NAV = [
  { key: "home", href: "/" },
  { key: "categories", href: "/categories" },
  { key: "offers", href: "/offers" },
  { key: "about", href: "/page/about" },
  { key: "contact", href: "/page/contact" },
] as const;

export function DesktopHeader() {
  const t = useTranslations("nav");
  const pathname = usePathname();
  const { customer } = useAuth();
  const { count } = useCart();

  return (
    <header className="sticky top-0 z-40 hidden border-b border-bordergold/40 bg-cream/90 backdrop-blur lg:block">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-6 px-6 py-4">
        <Brand />
        <nav aria-label={t("menu")} className="flex items-center gap-6">
          {NAV.map((item) => {
            const active = isActivePath(pathname, item.href);
            return (
              <Link
                key={item.key}
                href={item.href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "text-sm transition-colors hover:text-bronze",
                  active ? "font-medium text-bronze" : "text-mocha",
                )}
              >
                {t(item.key)}
              </Link>
            );
          })}
        </nav>
        <div className="flex items-center gap-1">
          <LocaleSwitcher />
          <Link
            href={customer ? "/account" : "/login"}
            aria-label={t("account")}
            className="inline-flex h-10 w-10 items-center justify-center rounded-full text-mocha hover:bg-ivory"
          >
            <UserIcon className="h-5 w-5" />
          </Link>
          <Link
            href="/cart"
            aria-label={t("cart")}
            className="relative inline-flex h-10 w-10 items-center justify-center rounded-full text-mocha hover:bg-ivory"
          >
            <BagIcon className="h-5 w-5" />
            {count > 0 && (
              <span className="absolute -end-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-rosegold px-1 text-[10px] font-semibold text-cream">
                {count}
              </span>
            )}
          </Link>
        </div>
      </div>
    </header>
  );
}
