"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { Link } from "@/lib/i18n/navigation";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";
import { useAuth } from "@/features/auth/auth-provider";
import { apiLogin } from "@/features/auth/auth-api";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { validatePhone } from "@/lib/validation";

export function LoginForm({ returnUrl }: { returnUrl: string }) {
  const t = useTranslations("auth");
  const tv = useTranslations("validation");
  const router = useRouter();
  const { setCustomer } = useAuth();
  const getError = useErrorMessage();

  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<{ phone?: string; password?: string; form?: string }>({});
  const [loading, setLoading] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const next: typeof errors = {};
    if (!validatePhone(phone)) next.phone = tv("phoneInvalid");
    if (!password) next.password = tv("passwordRequired");
    setErrors(next);
    if (next.phone || next.password) return;

    setLoading(true);
    try {
      const res = await apiLogin({ phoneNumber: phone, password });
      setCustomer(res.customer);
      router.push(returnUrl as Route);
      router.refresh();
    } catch (err) {
      setErrors({ form: getError(err) });
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {errors.form && <FormAlert>{errors.form}</FormAlert>}
      <FormField label={t("phone")} htmlFor="phone" error={errors.phone}>
        <Input
          id="phone"
          name="phone"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          dir="ltr"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          aria-invalid={errors.phone ? true : undefined}
          aria-describedby={errors.phone ? "phone-error" : undefined}
        />
      </FormField>
      <FormField label={t("password")} htmlFor="password" error={errors.password}>
        <PasswordInput
          id="password"
          name="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          aria-invalid={errors.password ? true : undefined}
          aria-describedby={errors.password ? "password-error" : undefined}
        />
      </FormField>
      <div className="text-end text-sm">
        <Link href="/forgot-password" className="text-bronze hover:underline">
          {t("forgotPassword")}
        </Link>
      </div>
      <Button type="submit" fullWidth disabled={loading}>
        {t("login")}
      </Button>
    </form>
  );
}
