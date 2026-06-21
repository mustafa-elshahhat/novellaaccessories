import { api } from "./client";
import type { Coupon, CouponUsage, Success, TwoOrderSettings } from "./types";

export const couponsApi = {
  list: () => api.get<Coupon[]>("/api/admin/coupons"),
  get: (id: string) => api.get<Coupon>(`/api/admin/coupons/${id}`),
  create: (body: Partial<Coupon>) => api.post<Coupon>("/api/admin/coupons", body),
  update: (id: string, body: Partial<Coupon>) => api.put<Coupon>(`/api/admin/coupons/${id}`, body),
  remove: (id: string) => api.delete<Success>(`/api/admin/coupons/${id}`),
  status: (id: string, isActive: boolean) => api.patch<Success>(`/api/admin/coupons/${id}/status`, { isActive }),
  usage: (id: string) => api.get<CouponUsage[]>(`/api/admin/coupons/${id}/usage`),
  twoOrder: () => api.get<TwoOrderSettings>("/api/admin/coupons/two-order/settings"),
  updateTwoOrder: (body: TwoOrderSettings) => api.put<TwoOrderSettings>("/api/admin/coupons/two-order/settings", body)
};
