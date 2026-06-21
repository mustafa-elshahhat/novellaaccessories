import type { ReactNode } from "react";
import { Link } from "@/lib/i18n/navigation";

export function Section({
  title,
  viewAllHref,
  viewAllLabel,
  children,
}: {
  title: string;
  viewAllHref?: string;
  viewAllLabel?: string;
  children: ReactNode;
}) {
  return (
    <section className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <div className="mb-5 flex items-end justify-between gap-4">
        <h2 className="text-xl font-semibold text-deepbrown sm:text-2xl">{title}</h2>
        {viewAllHref && viewAllLabel && (
          <Link href={viewAllHref} className="shrink-0 text-sm text-bronze hover:underline">
            {viewAllLabel}
          </Link>
        )}
      </div>
      {children}
    </section>
  );
}
