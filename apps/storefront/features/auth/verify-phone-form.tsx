"use client";

import { useState, type FormEvent } from "react";
import type { Route } from "next";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { Link } from "@/lib/i18n/navigation";
import { Button } from "@/components/ui/button";
import { FormAlert } from "@/components/ui/form-alert";
import { OtpInput } from "@/features/auth/otp-input";
import { useAuth } from "@/features/auth/auth-provider";
import { apiVerifyPhone } from "@/features/auth/auth-api";
import { useErrorMessage } from "@/features/shared/use-error-message";
import { isOtpComplete } from "@/lib/validation";

export function VerifyPhoneForm({
  phone,
  returnUrl,
}: {
  phone: string;
  returnUrl: string;
}) {
  const to = useTranslations("otp");
  const tv = useTranslations("validation");
  const router = useRouter();
  const { setCustomer } = useAuth();
  const getError = useErrorMessage();

  const [code, setCode] = useState("");
  const [error, setError] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!isOtpComplete(code)) {
      setError(tv("codeLength"));
      return;
    }
    setLoading(true);
    try {
      const res = await apiVerifyPhone({ phoneNumber: phone, code });
      setCustomer(res.customer);
      router.push(returnUrl as Route);
      router.refresh();
    } catch (err) {
      setError(getError(err));
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-5">
      {error && <FormAlert>{error}</FormAlert>}
      <p className="text-sm text-taupe">{to("subtitle", { phone })}</p>
      <OtpInput value={code} onChange={setCode} autoFocus />
      <Button type="submit" fullWidth disabled={loading}>
        {to("verify")}
      </Button>
      <div className="text-center text-sm text-taupe">
        <Link href="/register" className="text-bronze hover:underline">
          {to("resend")}
        </Link>
      </div>
    </form>
  );
}
