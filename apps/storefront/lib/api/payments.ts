import "server-only";
import { apiFetch } from "./server";
import type { PaymentMethodInfo } from "./types";

export const getPaymentMethods = () =>
  apiFetch<PaymentMethodInfo[]>("/api/payments/methods", { revalidate: 60 });
