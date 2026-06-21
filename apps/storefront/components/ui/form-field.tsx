import type { ReactNode } from "react";
import { cn } from "@/lib/utils/cn";

interface FormFieldProps {
  label: string;
  htmlFor: string;
  error?: string | null;
  hint?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
}

/**
 * Accessible field wrapper: associates a label and validation error with a control.
 * Pass the matching `id`, `aria-invalid` and `aria-describedby` to the inner control:
 *   aria-describedby={error ? `${htmlFor}-error` : undefined}
 */
export function FormField({
  label,
  htmlFor,
  error,
  hint,
  required,
  children,
  className,
}: FormFieldProps) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <label htmlFor={htmlFor} className="text-sm font-medium text-mocha">
        {label}
        {required && <span className="text-rosegold"> *</span>}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-taupe">{hint}</p>}
      {error && (
        <p id={`${htmlFor}-error`} role="alert" className="text-xs text-red-600">
          {error}
        </p>
      )}
    </div>
  );
}
