"use client";

import { useTranslations } from "next-intl";
import { ApiError, errorTranslationKey } from "@/lib/api/errors";

/**
 * Returns a function that maps any thrown error (an ApiError code or generic) to a localized,
 * user-facing message. Never surfaces raw backend/exception text.
 */
export function useErrorMessage() {
  const t = useTranslations("errors");
  return (error: unknown): string => {
    const err = ApiError.from(error);
    if (err.code === "NETWORK" || err.code === "UNAVAILABLE") {
      return t("network");
    }
    const key = errorTranslationKey(err.code);
    return t.has(key) ? t(key) : t("generic");
  };
}
