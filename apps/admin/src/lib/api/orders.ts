import { api, params } from "./client";
import type { Order, OrderListItem, PagedResult } from "./types";

export type OrderQuery = { page?: number; pageSize?: number; status?: string; search?: string; from?: string; to?: string; paymentMethod?: string; governorate?: string };

export const ordersApi = {
  list: (query: OrderQuery) => api.get<PagedResult<OrderListItem>>(`/api/admin/orders${params(query)}`),
  get: (id: string) => api.get<Order>(`/api/admin/orders/${id}`),
  status: (id: string, status: string) => api.patch<Order>(`/api/admin/orders/${id}/status`, { status }),
  cancel: (id: string, reason: string) => api.post<Order>(`/api/admin/orders/${id}/cancel`, { reason }),
  shipping: (id: string, body: { shippingProviderName?: string; externalTrackingNumber?: string; externalShippingStatus?: string }) => api.patch<Order>(`/api/admin/orders/${id}/shipping`, body)
};
