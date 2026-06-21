"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const t = useTranslations("error");

  useEffect(() => {
    // Log to console only; never surface raw error details to the user.
    console.error(error);
  }, [error]);

  return (
    <section className="mx-auto flex max-w-xl flex-col items-center px-4 py-24 text-center">
      <h1 className="text-2xl font-semibold">{t("title")}</h1>
      <p className="mt-2 text-taupe">{t("description")}</p>
      <button
        type="button"
        onClick={reset}
        className="mt-8 inline-flex items-center rounded-pill bg-bronze px-6 py-3 text-cream"
      >
        {t("retry")}
      </button>
    </section>
  );
}
