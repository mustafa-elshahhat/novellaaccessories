import { cn } from "@/lib/utils/cn";

export function Spinner({ className, label }: { className?: string; label?: string }) {
  return (
    <span role="status" className="inline-flex items-center gap-2">
      <span
        className={cn(
          "h-5 w-5 animate-spin rounded-full border-2 border-bordergold border-t-bronze",
          className,
        )}
      />
      {label ? <span>{label}</span> : <span className="sr-only">Loading</span>}
    </span>
  );
}
