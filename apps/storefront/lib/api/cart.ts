import "server-only";
import { apiFetch } from "./server";
import type { Cart, AddCartItemRequest } from "./types";

export const getCart = () => apiFetch<Cart>("/api/cart", { auth: true });

export const addItem = (body: AddCartItemRequest) =>
  apiFetch<Cart>("/api/cart/items", { method: "POST", auth: true, body });

export const updateItem = (itemId: string, quantity: number) =>
  apiFetch<Cart>(`/api/cart/items/${encodeURIComponent(itemId)}`, {
    method: "PATCH",
    auth: true,
    body: { quantity },
  });

export const removeItem = (itemId: string) =>
  apiFetch<Cart>(`/api/cart/items/${encodeURIComponent(itemId)}`, {
    method: "DELETE",
    auth: true,
  });

export const clearCart = () =>
  apiFetch<Cart>("/api/cart", { method: "DELETE", auth: true });

export const reprice = () =>
  apiFetch<Cart>("/api/cart/reprice", { method: "POST", auth: true });
