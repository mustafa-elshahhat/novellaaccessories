"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Input } from "@/components/ui/input";
import { IconButton } from "@/components/ui/button";
import { SearchIcon } from "@/components/icons";

export function SearchBar({ initialQuery = "" }: { initialQuery?: string }) {
  const t = useTranslations("products");
  const locale = useLocale();
  const router = useRouter();
  const [query, setQuery] = useState(initialQuery);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    const term = query.trim();
    const href = term
      ? `/${locale}/products?q=${encodeURIComponent(term)}`
      : `/${locale}/products`;
    router.push(href as Route);
  }

  return (
    <form onSubmit={onSubmit} role="search" className="mb-6 flex gap-2">
      <Input
        type="search"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder={t("searchPlaceholder")}
        aria-label={t("searchPlaceholder")}
      />
      <IconButton
        type="submit"
        label={t("searchPlaceholder")}
        className="shrink-0 bg-bronze text-cream hover:bg-mocha"
      >
        <SearchIcon className="h-5 w-5" />
      </IconButton>
    </form>
  );
}
