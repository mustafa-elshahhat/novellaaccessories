import "server-only";
import { NextResponse } from "next/server";
import { ApiError } from "./errors";
import { isTrustedOrigin } from "@/lib/security/csrf";
import { clearAuthCookie } from "./cookies";

/**
 * CSRF guard for state-changing BFF routes. Returns a 403 response when the request origin
 * is not trusted, or `null` when the request may proceed.
 */
export function csrfGuard(request: Request): NextResponse | null {
  if (!isTrustedOrigin(request)) {
    return NextResponse.json(
      { code: "FORBIDDEN", message: "Invalid request origin." },
      { status: 403 },
    );
  }
  return null;
}

/** Converts any error into a sanitized JSON error response, clearing auth on 401. */
export async function errorResponse(error: unknown): Promise<NextResponse> {
  const err = ApiError.from(error);
  if (err.status === 401) {
    await clearAuthCookie();
  }
  return NextResponse.json(
    { code: err.code, message: err.message, details: err.details },
    { status: err.status },
  );
}

/** Runs an API call and returns its JSON, or a sanitized error response on failure. */
export async function jsonResult<T>(fn: () => Promise<T>): Promise<NextResponse> {
  try {
    const data = await fn();
    return NextResponse.json(data ?? { success: true });
  } catch (error) {
    return errorResponse(error);
  }
}

/** Safely parses a JSON request body, returning `{}` on empty/invalid input. */
export async function readJson<T = Record<string, unknown>>(
  request: Request,
): Promise<T> {
  try {
    const text = await request.text();
    return text ? (JSON.parse(text) as T) : ({} as T);
  } catch {
    return {} as T;
  }
}
