"use client";

import type { Route } from "next";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useAuth } from "@/features/auth/auth-provider";
import { Button } from "@/components/ui/button";

export function LogoutButton() {
  const t = useTranslations("account");
  const locale = useLocale();
  const router = useRouter();
  const { logout } = useAuth();

  async function onClick() {
    await logout();
    router.push(`/${locale}` as Route);
    router.refresh();
  }

  return (
    <Button variant="secondary" onClick={onClick}>
      {t("logout")}
    </Button>
  );
}
