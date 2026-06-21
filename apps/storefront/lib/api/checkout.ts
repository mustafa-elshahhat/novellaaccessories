import "server-only";
import { apiFetch } from "./server";
import type { CheckoutPreview, CheckoutPreviewRequest } from "./types";

export const preview = (body: CheckoutPreviewRequest) =>
  apiFetch<CheckoutPreview>("/api/checkout/preview", {
    method: "POST",
    auth: true,
    body,
  });
