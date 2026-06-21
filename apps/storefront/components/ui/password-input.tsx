"use client";

import { forwardRef, useState, type InputHTMLAttributes } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils/cn";
import { fieldClasses } from "./input";
import { EyeIcon, EyeOffIcon } from "@/components/icons";

export const PasswordInput = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement>
>(function PasswordInput({ className, ...props }, ref) {
  const [visible, setVisible] = useState(false);
  const t = useTranslations("auth");
  return (
    <div className="relative">
      <input
        ref={ref}
        type={visible ? "text" : "password"}
        className={cn(fieldClasses, "pe-11", className)}
        {...props}
      />
      <button
        type="button"
        onClick={() => setVisible((v) => !v)}
        aria-label={visible ? t("password") : t("password")}
        aria-pressed={visible}
        className="absolute inset-y-0 end-0 flex w-11 items-center justify-center text-taupe hover:text-bronze"
      >
        {visible ? (
          <EyeOffIcon className="h-5 w-5" />
        ) : (
          <EyeIcon className="h-5 w-5" />
        )}
      </button>
    </div>
  );
});
