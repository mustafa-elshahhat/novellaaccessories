import { api } from "./client";
import type { PaymentMethodReadiness } from "./types";

export const paymentsApi = {
  readiness: () => api.get<PaymentMethodReadiness[]>("/api/admin/payments/readiness")
};
