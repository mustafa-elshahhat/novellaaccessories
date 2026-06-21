import { Fragment } from "react";
import { Link } from "@/lib/i18n/navigation";

export interface Crumb {
  label: string;
  href?: string;
}

export function Breadcrumb({ items, label }: { items: Crumb[]; label: string }) {
  return (
    <nav aria-label={label} className="py-3 text-sm text-taupe">
      <ol className="flex flex-wrap items-center gap-1.5">
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <Fragment key={`${item.label}-${index}`}>
              <li>
                {item.href && !isLast ? (
                  <Link href={item.href} className="hover:text-bronze">
                    {item.label}
                  </Link>
                ) : (
                  <span aria-current={isLast ? "page" : undefined} className="text-mocha">
                    {item.label}
                  </span>
                )}
              </li>
              {!isLast && <li aria-hidden>/</li>}
            </Fragment>
          );
        })}
      </ol>
    </nav>
  );
}
