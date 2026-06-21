import { useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { cn } from "@/lib/utils/cn";

export function Brand({ className }: { className?: string }) {
  const t = useTranslations("common");
  return (
    <Link
      href="/"
      aria-label={t("brand")}
      className={cn(
        "font-[family-name:var(--font-heading)] text-2xl lowercase tracking-[0.2em] text-deepbrown",
        className,
      )}
    >
      novella
    </Link>
  );
}
