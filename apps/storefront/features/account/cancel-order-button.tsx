"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Dialog } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import { bff } from "@/lib/api/bff-client";
import { useToast } from "@/components/ui/toast";
import { useErrorMessage } from "@/features/shared/use-error-message";

export function CancelOrderButton({ orderNumber }: { orderNumber: string }) {
  const t = useTranslations("orders");
  const tc = useTranslations("common");
  const router = useRouter();
  const toast = useToast();
  const getError = useErrorMessage();

  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [loading, setLoading] = useState(false);

  async function confirm() {
    setLoading(true);
    try {
      await bff(`/api/orders/my/${encodeURIComponent(orderNumber)}/cancel`, {
        method: "POST",
        body: JSON.stringify({ reason: reason.trim() || null }),
      });
      toast.show(t("cancelled"), "success");
      setOpen(false);
      router.refresh();
    } catch (err) {
      toast.show(getError(err), "error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <Button variant="secondary" onClick={() => setOpen(true)}>
        {t("cancel")}
      </Button>
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        title={t("cancelConfirmTitle")}
        footer={
          <>
            <Button variant="ghost" onClick={() => setOpen(false)}>
              {tc("back")}
            </Button>
            <Button onClick={confirm} disabled={loading}>
              {t("cancel")}
            </Button>
          </>
        }
      >
        <p className="text-sm text-taupe">{t("cancelConfirmBody")}</p>
        <div className="mt-3">
          <Textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t("cancelReason")}
            rows={2}
            aria-label={t("cancelReason")}
          />
        </div>
      </Dialog>
    </>
  );
}
