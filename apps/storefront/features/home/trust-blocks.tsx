import { useTranslations } from "next-intl";

export function TrustBlocks() {
  const t = useTranslations("home");
  const items = [t("trustShipping"), t("trustAuthentic"), t("trustSupport")];
  return (
    <section className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <div className="grid gap-4 sm:grid-cols-3">
        {items.map((item) => (
          <div
            key={item}
            className="rounded-card border border-bordergold/40 bg-ivory/50 p-5 text-center text-sm text-mocha"
          >
            {item}
          </div>
        ))}
      </div>
    </section>
  );
}
