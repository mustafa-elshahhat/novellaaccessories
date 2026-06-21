import "server-only";
import { apiFetch } from "./server";
import type {
  CreateOrderRequest,
  CreateOrderResponse,
  CustomerOrder,
  PagedResult,
} from "./types";

export const createOrder = (body: CreateOrderRequest) =>
  apiFetch<CreateOrderResponse>("/api/orders", {
    method: "POST",
    auth: true,
    body,
  });

/** The backend may return a bare array or a paged envelope; callers normalize. */
export const myOrders = () =>
  apiFetch<PagedResult<CustomerOrder> | CustomerOrder[]>("/api/orders/my", {
    auth: true,
  });

export const myOrder = (orderNumber: string) =>
  apiFetch<CustomerOrder>(
    `/api/orders/my/${encodeURIComponent(orderNumber)}`,
    { auth: true },
  );

export const cancelOrder = (orderNumber: string, reason?: string | null) =>
  apiFetch<CustomerOrder>(
    `/api/orders/my/${encodeURIComponent(orderNumber)}/cancel`,
    { method: "POST", auth: true, body: { reason: reason ?? null } },
  );
