import { forwardRef, type SelectHTMLAttributes } from "react";
import { cn } from "@/lib/utils/cn";

export const Select = forwardRef<
  HTMLSelectElement,
  SelectHTMLAttributes<HTMLSelectElement>
>(function Select({ className, children, ...props }, ref) {
  return (
    <select
      ref={ref}
      className={cn(
        "w-full appearance-none rounded-xl border border-bordergold bg-cream px-4 py-2.5 text-mocha focus:border-bronze focus:outline-none focus:ring-2 focus:ring-bronze/30 disabled:opacity-60 aria-[invalid=true]:border-red-500",
        className,
      )}
      {...props}
    >
      {children}
    </select>
  );
});
