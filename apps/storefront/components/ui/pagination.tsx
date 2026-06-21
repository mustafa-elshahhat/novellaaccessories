import { useTranslations } from "next-intl";
import { Link } from "@/lib/i18n/navigation";
import { ChevronLeftIcon, ChevronRightIcon } from "@/components/icons";
import { cn } from "@/lib/utils/cn";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  createHref: (page: number) => string;
}

export function Pagination({ currentPage, totalPages, createHref }: PaginationProps) {
  const t = useTranslations("common");
  if (totalPages <= 1) return null;

  const pages: number[] = [];
  const start = Math.max(1, currentPage - 2);
  const end = Math.min(totalPages, currentPage + 2);
  for (let p = start; p <= end; p++) pages.push(p);

  const linkClass = "flex h-10 min-w-10 items-center justify-center rounded-full px-3 text-sm";

  return (
    <nav aria-label={t("search")} className="mt-8 flex items-center justify-center gap-1">
      {currentPage > 1 && (
        <Link
          href={createHref(currentPage - 1)}
          aria-label="Previous"
          className={cn(linkClass, "text-mocha hover:bg-ivory")}
        >
          <ChevronLeftIcon className="h-4 w-4 rtl:rotate-180" />
        </Link>
      )}
      {pages.map((p) => (
        <Link
          key={p}
          href={createHref(p)}
          aria-current={p === currentPage ? "page" : undefined}
          className={cn(
            linkClass,
            p === currentPage
              ? "bg-bronze text-cream"
              : "text-mocha hover:bg-ivory",
          )}
        >
          {p}
        </Link>
      ))}
      {currentPage < totalPages && (
        <Link
          href={createHref(currentPage + 1)}
          aria-label="Next"
          className={cn(linkClass, "text-mocha hover:bg-ivory")}
        >
          <ChevronRightIcon className="h-4 w-4 rtl:rotate-180" />
        </Link>
      )}
    </nav>
  );
}
