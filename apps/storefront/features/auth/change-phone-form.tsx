"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";
import { OtpInput } from "@/features/auth/otp-input";
import { useCooldown } from "@/features/auth/use-cooldown";
import { useToast } from "@/components/ui/toast";
import { useAuth } from "@/features/auth/auth-provider";
import { apiChangePhoneRequest, apiChangePhoneVerify } from "@/features/auth/auth-api";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { ApiError } from "@/lib/api/errors";
import { validatePhone, isOtpComplete } from "@/lib/validation";

export function ChangePhoneForm({ currentPhone }: { currentPhone: string }) {
  const t = useTranslations("auth");
  const to = useTranslations("otp");
  const tv = useTranslations("validation");
  const ta = useTranslations("account");
  const locale = useLocale();
  const router = useRouter();
  const toast = useToast();
  const { refresh } = useAuth();
  const getError = useErrorMessage();
  const cooldown = useCooldown();

  const [step, setStep] = useState<"phone" | "otp">("phone");
  const [newPhone, setNewPhone] = useState("");
  const [code, setCode] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  async function requestOtp(event: FormEvent) {
    event.preventDefault();
    if (!validatePhone(newPhone)) {
      setErrors({ newPhone: tv("phoneInvalid") });
      return;
    }
    setErrors({});
    setLoading(true);
    try {
      await apiChangePhoneRequest({ newPhoneNumber: newPhone });
      setStep("otp");
      cooldown.start(60);
    } catch (err) {
      setErrors({ form: getError(err) });
    } finally {
      setLoading(false);
    }
  }

  async function verify(event: FormEvent) {
    event.preventDefault();
    if (!isOtpComplete(code)) {
      setErrors({ code: tv("codeLength") });
      return;
    }
    setLoading(true);
    try {
      await apiChangePhoneVerify({ newPhoneNumber: newPhone, code });
      await refresh();
      toast.show(ta("changePhone"), "success");
      router.push(`/${locale}/account` as Route);
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
      await apiChangePhoneRequest({ newPhoneNumber: newPhone });
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
      <form onSubmit={verify} noValidate className="space-y-5">
        {errors.form && <FormAlert>{errors.form}</FormAlert>}
        <p className="text-sm text-taupe">{to("subtitle", { phone: newPhone })}</p>
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
    <form onSubmit={requestOtp} noValidate className="space-y-4">
      {errors.form && <FormAlert>{errors.form}</FormAlert>}
      <p className="text-sm text-taupe">
        {t("phone")}: <span dir="ltr">{currentPhone}</span>
      </p>
      <FormField label={t("newPhone")} htmlFor="newPhone" error={errors.newPhone}>
        <Input
          id="newPhone"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          dir="ltr"
          value={newPhone}
          onChange={(e) => setNewPhone(e.target.value)}
          aria-invalid={errors.newPhone ? true : undefined}
          aria-describedby={errors.newPhone ? "newPhone-error" : undefined}
        />
      </FormField>
      <Button type="submit" fullWidth disabled={loading}>
        {t("requestCode")}
      </Button>
    </form>
  );
}
