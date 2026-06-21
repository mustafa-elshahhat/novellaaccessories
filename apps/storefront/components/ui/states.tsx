import type { ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

interface StateProps {
  title: string;
  description?: string;
  action?: ReactNode;
  icon?: ReactNode;
  className?: string;
}

export function EmptyState({ title, description, action, icon, className }: StateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-4 py-16 text-center",
        className,
      )}
    >
      {icon && <div className="text-champagne">{icon}</div>}
      <h2 className="text-xl font-semibold text-deepbrown">{title}</h2>
      {description && <p className="max-w-md text-taupe">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

export function ErrorState({ title, description, action, className }: StateProps) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-4 py-16 text-center",
        className,
      )}
    >
      <h2 className="text-xl font-semibold text-deepbrown">{title}</h2>
      {description && <p className="max-w-md text-taupe">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}
