import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils/cn";

const fieldClasses =
  "w-full rounded-xl border border-bordergold bg-cream px-4 py-2.5 text-mocha placeholder:text-taupe/70 focus:border-bronze focus:outline-none focus:ring-2 focus:ring-bronze/30 disabled:opacity-60 aria-[invalid=true]:border-red-500";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  function Input({ className, ...props }, ref) {
    return <input ref={ref} className={cn(fieldClasses, className)} {...props} />;
  },
);

export const Textarea = forwardRef<
  HTMLTextAreaElement,
  TextareaHTMLAttributes<HTMLTextAreaElement>
>(function Textarea({ className, rows = 4, ...props }, ref) {
  return (
    <textarea ref={ref} rows={rows} className={cn(fieldClasses, className)} {...props} />
  );
});

export { fieldClasses };
