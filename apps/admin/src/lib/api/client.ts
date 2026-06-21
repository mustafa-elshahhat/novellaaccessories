import { env } from "@/lib/env";
import { ApiError, type ApiErrorEnvelope } from "./errors";

let accessToken: string | null = null;
let unauthorizedHandler: (() => void) | null = null;
let forbiddenHandler: (() => void) | null = null;

const timeoutMs = 20_000;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}

export function onUnauthorized(handler: () => void) {
  unauthorizedHandler = handler;
}

export function onForbidden(handler: () => void) {
  forbiddenHandler = handler;
}

export function params(values: Record<string, unknown>) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}

async function parseJson(response: Response) {
  const text = await response.text();
  if (!text) return undefined;
  try {
    return JSON.parse(text) as unknown;
  } catch {
    throw new ApiError(response.status, { code: "INVALID_JSON", message: "The server returned an invalid response." });
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  if (!path.startsWith("/api/")) throw new ApiError(400, { code: "ADMIN_API_ONLY", message: "Admin requests must use the API boundary." });

  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);
  const headers = new Headers(init.headers);
  if (!headers.has("Accept")) headers.set("Accept", "application/json");
  if (init.body && !(init.body instanceof FormData) && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);

  try {
    const response = await fetch(`${env.apiBaseUrl}${path}`, { ...init, headers, signal: init.signal ?? controller.signal, cache: "no-store" });
    const data = await parseJson(response);
    if (!response.ok) {
      const error = new ApiError(response.status, data as Partial<ApiErrorEnvelope> | undefined);
      if (response.status === 401) unauthorizedHandler?.();
      if (response.status === 403) forbiddenHandler?.();
      throw error;
    }
    return data as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (error instanceof DOMException && error.name === "AbortError") throw new ApiError(408, { code: "TIMEOUT", message: "The request timed out." });
    throw new ApiError(503, { code: "NETWORK_ERROR", message: "The API is unavailable." });
  } finally {
    window.clearTimeout(timeout);
  }
}

export const api = {
  get: <T>(path: string) => apiFetch<T>(path),
  post: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) => apiFetch<T>(path, { method: "PUT", body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) => apiFetch<T>(path, { method: "PATCH", body: JSON.stringify(body) }),
  delete: <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: "DELETE", body: body === undefined ? undefined : JSON.stringify(body) }),
  form: <T>(path: string, form: FormData) => apiFetch<T>(path, { method: "POST", body: form })
};
