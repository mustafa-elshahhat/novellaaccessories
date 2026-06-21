"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";
import { OtpInput } from "@/features/auth/otp-input";
import { useCooldown } from "@/features/auth/use-cooldown";
import { useAuth } from "@/features/auth/auth-provider";
import { apiRegister, apiVerifyPhone } from "@/features/auth/auth-api";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { ApiError } from "@/lib/api/errors";
import {
  validatePhone,
  validatePasswordLength,
  isOtpComplete,
} from "@/lib/validation";

export function RegisterForm({ returnUrl }: { returnUrl: string }) {
  const t = useTranslations("auth");
  const to = useTranslations("otp");
  const tv = useTranslations("validation");
  const router = useRouter();
  const { setCustomer } = useAuth();
  const getError = useErrorMessage();
  const cooldown = useCooldown();

  const [step, setStep] = useState<"details" | "otp">("details");
  const [form, setForm] = useState({ name: "", phone: "", password: "", confirm: "" });
  const [code, setCode] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const update = (key: keyof typeof form, value: string) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  async function submitDetails(event: FormEvent) {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (!form.name.trim()) next.name = tv("nameRequired");
    if (!validatePhone(form.phone)) next.phone = tv("phoneInvalid");
    if (!validatePasswordLength(form.password)) next.password = tv("passwordMin");
    if (form.password !== form.confirm) next.confirm = tv("passwordMismatch");
    setErrors(next);
    if (Object.keys(next).length) return;

    setLoading(true);
    try {
      await apiRegister({
        fullName: form.name.trim(),
        phoneNumber: form.phone,
        password: form.password,
      });
      setStep("otp");
      cooldown.start(60);
    } catch (err) {
      setErrors({ form: getError(err) });
    } finally {
      setLoading(false);
    }
  }

  async function submitOtp(event: FormEvent) {
    event.preventDefault();
    if (!isOtpComplete(code)) {
      setErrors({ code: tv("codeLength") });
      return;
    }
    setLoading(true);
    try {
      const res = await apiVerifyPhone({ phoneNumber: form.phone, code });
      setCustomer(res.customer);
      router.push(returnUrl as Route);
      router.refresh();
    } catch (err) {
      setErrors({ form: getError(err) });
      setLoading(false);
    }
  }

  async function resend() {
    if (cooldown.active) return;
    setErrors({});
    try {
      await apiRegister({
        fullName: form.name.trim(),
        phoneNumber: form.phone,
        password: form.password,
      });
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

  if (step === "otp") {
    return (
      <form onSubmit={submitOtp} noValidate className="space-y-5">
        {errors.form && <FormAlert>{errors.form}</FormAlert>}
        <p className="text-sm text-taupe">{to("subtitle", { phone: form.phone })}</p>
        <OtpInput value={code} onChange={setCode} autoFocus />
        {errors.code && (
          <p role="alert" className="text-center text-xs text-red-600">
            {errors.code}
          </p>
        )}
        <Button type="submit" fullWidth disabled={loading}>
          {to("verify")}
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
    <form onSubmit={submitDetails} noValidate className="space-y-4">
      {errors.form && <FormAlert>{errors.form}</FormAlert>}
      <FormField label={t("name")} htmlFor="name" error={errors.name} required>
        <Input
          id="name"
          autoComplete="name"
          value={form.name}
          onChange={(e) => update("name", e.target.value)}
          aria-invalid={errors.name ? true : undefined}
          aria-describedby={errors.name ? "name-error" : undefined}
        />
      </FormField>
      <FormField label={t("phone")} htmlFor="phone" error={errors.phone} required>
        <Input
          id="phone"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          dir="ltr"
          value={form.phone}
          onChange={(e) => update("phone", e.target.value)}
          aria-invalid={errors.phone ? true : undefined}
          aria-describedby={errors.phone ? "phone-error" : undefined}
        />
      </FormField>
      <FormField label={t("password")} htmlFor="password" error={errors.password} required>
        <PasswordInput
          id="password"
          autoComplete="new-password"
          value={form.password}
          onChange={(e) => update("password", e.target.value)}
          aria-invalid={errors.password ? true : undefined}
          aria-describedby={errors.password ? "password-error" : undefined}
        />
      </FormField>
      <FormField
        label={t("confirmPassword")}
        htmlFor="confirm"
        error={errors.confirm}
        required
      >
        <PasswordInput
          id="confirm"
          autoComplete="new-password"
          value={form.confirm}
          onChange={(e) => update("confirm", e.target.value)}
          aria-invalid={errors.confirm ? true : undefined}
          aria-describedby={errors.confirm ? "confirm-error" : undefined}
        />
      </FormField>
      <Button type="submit" fullWidth disabled={loading}>
        {t("register")}
      </Button>
    </form>
  );
}
