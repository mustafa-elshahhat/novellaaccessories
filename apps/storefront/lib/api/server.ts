import "server-only";
import { cookies } from "next/headers";
import { getApiBaseUrl } from "@/lib/env.server";
import { ApiError, mapStatusToCode } from "./errors";

const AUTH_COOKIE = "novella_token";
const DEFAULT_TIMEOUT_MS = 12_000;

export interface ApiFetchOptions {
  method?: string;
  /** Attach the bearer token from the HttpOnly cookie (if present). Forces dynamic rendering. */
  auth?: boolean;
  body?: unknown;
  /** Cache mode for public, cacheable reads. Omit for authenticated calls (defaults to no-store). */
  revalidate?: number;
  cache?: RequestCache;
  timeoutMs?: number;
  headers?: Record<string, string>;
}

/**
 * Server-only fetch wrapper to the Novella API. Reads the bearer token from the HttpOnly
 * cookie only when `auth` is set (so public calls stay statically cacheable). Throws a
 * typed {@link ApiError} on failure; never leaks raw responses.
 */
export async function apiFetch<T>(
  path: string,
  opts: ApiFetchOptions = {},
): Promise<T> {
  const {
    method = "GET",
    auth = false,
    body,
    revalidate,
    cache,
    timeoutMs = DEFAULT_TIMEOUT_MS,
    headers = {},
  } = opts;

  let baseUrl: string;
  try {
    baseUrl = getApiBaseUrl();
  } catch {
    throw new ApiError("UNAVAILABLE", "The service is temporarily unavailable.", 503);
  }

  const finalHeaders: Record<string, string> = { Accept: "application/json", ...headers };
  if (body !== undefined) finalHeaders["Content-Type"] = "application/json";

  if (auth) {
    const store = await cookies();
    const token = store.get(AUTH_COOKIE)?.value;
    if (token) finalHeaders["Authorization"] = `Bearer ${token}`;
  }

  const init: RequestInit & { next?: { revalidate?: number } } = {
    method,
    headers: finalHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  };

  if (auth || cache === "no-store") {
    init.cache = "no-store";
  } else if (revalidate !== undefined) {
    init.next = { revalidate };
  } else if (cache) {
    init.cache = cache;
  }

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  init.signal = controller.signal;

  let res: Response;
  try {
    res = await fetch(`${baseUrl}${path}`, init);
  } catch {
    clearTimeout(timer);
    throw new ApiError("NETWORK", "Could not reach the server.", 503);
  }
  clearTimeout(timer);

  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  let json: unknown = null;
  if (text) {
    try {
      json = JSON.parse(text);
    } catch {
      json = null;
    }
  }

  if (!res.ok) {
    const obj = (json ?? {}) as {
      code?: string;
      message?: string;
      details?: Record<string, unknown>;
    };
    throw new ApiError(
      obj.code ?? mapStatusToCode(res.status),
      obj.message ?? "Request failed.",
      res.status,
      obj.details,
    );
  }

  return json as T;
}

/** Like {@link apiFetch} but returns `null` instead of throwing — for graceful public reads. */
export async function tryApiFetch<T>(
  path: string,
  opts: ApiFetchOptions = {},
): Promise<T | null> {
  try {
    return await apiFetch<T>(path, opts);
  } catch {
    return null;
  }
}
