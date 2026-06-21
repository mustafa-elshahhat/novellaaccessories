"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";
import { OtpInput } from "@/features/auth/otp-input";
import { useCooldown } from "@/features/auth/use-cooldown";
import { useToast } from "@/components/ui/toast";
import { apiForgotRequest, apiForgotReset } from "@/features/auth/auth-api";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { ApiError } from "@/lib/api/errors";
import {
  validatePhone,
  validatePasswordLength,
  isOtpComplete,
} from "@/lib/validation";

export function ForgotPasswordForm() {
  const t = useTranslations("auth");
  const to = useTranslations("otp");
  const tv = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const toast = useToast();
  const getError = useErrorMessage();
  const cooldown = useCooldown();

  const [step, setStep] = useState<"phone" | "reset">("phone");
  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  async function requestOtp(event: FormEvent) {
    event.preventDefault();
    if (!validatePhone(phone)) {
      setErrors({ phone: tv("phoneInvalid") });
      return;
    }
    setErrors({});
    setLoading(true);
    try {
      await apiForgotRequest({ phoneNumber: phone });
      setStep("reset");
      cooldown.start(60);
    } catch (err) {
      setErrors({ form: getError(err) });
    } finally {
      setLoading(false);
    }
  }

  async function resetPassword(event: FormEvent) {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (!isOtpComplete(code)) next.code = tv("codeLength");
    if (!validatePasswordLength(password)) next.password = tv("passwordMin");
    setErrors(next);
    if (Object.keys(next).length) return;

    setLoading(true);
    try {
      await apiForgotReset({ phoneNumber: phone, code, newPassword: password });
      toast.show(t("logoutDone"), "success");
      router.push(`/${locale}/login` as Route);
    } catch (err) {
      setErrors({ form: getError(err) });
      setLoading(false);
    }
  }

  async function resend() {
    if (cooldown.active) return;
    setErrors({});
    try {
      await apiForgotRequest({ phoneNumber: phone });
      cooldown.start(60);
    } catch (err) {
      const apiError = ApiError.from(err);
      const retry = Number(
        (apiError.details as { retryAfterSeconds?: unknown } | undefined)
          ?.retryAfterSeconds,
      );
      if (Number.isFinite(retry) && retry > 0) cooldown.start(retry);
      setErrors({ form: getError(err) });
    }
  }

  if (step === "reset") {
    return (
      <form onSubmit={resetPassword} noValidate className="space-y-5">
        {errors.form && <FormAlert>{errors.form}</FormAlert>}
        <p className="text-sm text-taupe">{to("subtitle", { phone })}</p>
        <OtpInput value={code} onChange={setCode} autoFocus />
        {errors.code && (
          <p role="alert" className="text-center text-xs text-red-600">
            {errors.code}
          </p>
        )}
        <FormField label={t("newPassword")} htmlFor="newPassword" error={errors.password}>
          <PasswordInput
            id="newPassword"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-invalid={errors.password ? true : undefined}
            aria-describedby={errors.password ? "newPassword-error" : undefined}
          />
        </FormField>
        <Button type="submit" fullWidth disabled={loading}>
          {t("resetPassword")}
        </Button>
        <div className="text-center text-sm">
          {cooldown.active ? (
            <span className="text-taupe">{to("resendIn", { seconds: cooldown.seconds })}</span>
          ) : (
            <button type="button" onClick={resend} className="text-bronze hover:underline">
              {to("resend")}
            </button>
          )}
        </div>
      </form>
    );
  }

  return (
    <form onSubmit={requestOtp} noValidate className="space-y-4">
      {errors.form && <FormAlert>{errors.form}</FormAlert>}
      <FormField label={t("phone")} htmlFor="phone" error={errors.phone}>
        <Input
          id="phone"
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
      <Button type="submit" fullWidth disabled={loading}>
        {t("requestCode")}
      </Button>
    </form>
  );
}
