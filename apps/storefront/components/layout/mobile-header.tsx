"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { Brand } from "./brand";
import { LocaleSwitcher } from "@/components/ui/locale-switcher";
import { SearchIcon } from "@/components/icons";

export function MobileHeader() {
  const t = useTranslations("nav");
  return (
    <header className="sticky top-0 z-40 flex items-center justify-between border-b border-bordergold/40 bg-cream/90 px-3 py-2.5 backdrop-blur lg:hidden">
      <LocaleSwitcher className="px-2" />
      <Brand className="text-xl" />
      <Link
        href="/products"
        aria-label={t("products")}
        className="inline-flex h-10 w-10 items-center justify-center rounded-full text-mocha hover:bg-ivory"
      >
        <SearchIcon className="h-5 w-5" />
      </Link>
    </header>
  );
}
