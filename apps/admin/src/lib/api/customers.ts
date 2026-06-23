import { api, params } from "./client";
import type { CustomerDetail, CustomerListItem, PagedResult } from "./types";

export const customersApi = {
  list: (query: { page?: number; pageSize?: number; search?: string }) => api.get<PagedResult<CustomerListItem>>(`/api/admin/customers${params(query)}`),
  get: (id: string) => api.get<CustomerDetail>(`/api/admin/customers/${id}`),
  status: (id: string, isActive: boolean) => api.patch<CustomerDetail>(`/api/admin/customers/${id}/status`, { isActive })
};
