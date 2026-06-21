import { ApiError, mapStatusToCode } from "./errors";

/**
 * Client-side helper to call same-origin BFF route handlers (`/api/*`). The HttpOnly auth
 * cookie is attached automatically by the browser; the token is never read in JS.
 */
export async function bff<T>(path: string, init: RequestInit = {}): Promise<T> {
  let res: Response;
  try {
    res = await fetch(path, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(init.headers ?? {}),
      },
    });
  } catch {
    throw new ApiError("NETWORK", "Could not reach the server.", 503);
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
