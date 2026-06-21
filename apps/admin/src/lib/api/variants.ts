import { api } from "./client";
import type { InventoryMovement, Success, Variant } from "./types";

export const variantsApi = {
  list: (productId: string) => api.get<Variant[]>(`/api/admin/products/${productId}/variants`),
  create: (productId: string, body: Partial<Variant>) => api.post<Variant>(`/api/admin/products/${productId}/variants`, body),
  update: (variantId: string, body: Partial<Variant>) => api.put<Variant>(`/api/admin/variants/${variantId}`, body),
  remove: (variantId: string) => api.delete<Success>(`/api/admin/variants/${variantId}`),
  status: (variantId: string, isActive: boolean) => api.patch<Success>(`/api/admin/variants/${variantId}/status`, { isActive }),
  stock: (variantId: string, newStockQuantity: number, reason: string) => api.patch<Variant>(`/api/admin/variants/${variantId}/stock`, { newStockQuantity, reason }),
  movements: (variantId: string) => api.get<InventoryMovement[]>(`/api/admin/variants/${variantId}/movements`)
};
