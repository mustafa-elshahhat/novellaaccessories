import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils/cn";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-card border border-bordergold/60 bg-cream shadow-[var(--shadow-card)]",
        className,
      )}
      {...props}
    />
  );
}
