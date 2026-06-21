import { api, params } from "./client";
import type { ImageDto, PagedResult, Product, Success, UploadedImageDto } from "./types";

export type ProductQuery = { page?: number; pageSize?: number; search?: string; categoryId?: string; isActive?: boolean | string; isFeatured?: boolean | string };

export const productsApi = {
  list: (query: ProductQuery) => api.get<PagedResult<Product>>(`/api/admin/products${params(query)}`),
  get: (id: string) => api.get<Product>(`/api/admin/products/${id}`),
  create: (body: Partial<Product>) => api.post<Product>("/api/admin/products", body),
  update: (id: string, body: Partial<Product>) => api.put<Product>(`/api/admin/products/${id}`, body),
  remove: (id: string) => api.delete<Success>(`/api/admin/products/${id}`),
  status: (id: string, isActive: boolean) => api.patch<Success>(`/api/admin/products/${id}/status`, { isActive }),
  addImage: (id: string, body: { url: string; publicId: string; altAr?: string; altEn?: string; isPrimary: boolean }) => api.post<ImageDto>(`/api/admin/products/${id}/images`, body),
  removeImage: (id: string, imageId: string) => api.delete<Success & { publicId?: string }>(`/api/admin/products/${id}/images/${imageId}`),
  reorderImages: (id: string, items: { id: string; sortOrder: number }[]) => api.patch<Success>(`/api/admin/products/${id}/images/reorder`, { items }),
  uploadImage: (file: File, entityType = "products", entityId?: string) => {
    const form = new FormData();
    form.set("file", file);
    form.set("entityType", entityType);
    if (entityId) form.set("entityId", entityId);
    return api.form<UploadedImageDto>("/api/admin/uploads/image", form);
  }
};
