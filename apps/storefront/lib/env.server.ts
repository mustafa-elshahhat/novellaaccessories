import "server-only";

/**
 * Server-only access to the Novella API base URL. This value MUST NOT be exposed to the
 * browser (no `NEXT_PUBLIC_` prefix). Only Server Components, Route Handlers, Server Actions,
 * and server-only API utilities may read it. Throwing here is caught by the API fetch wrapper
 * and surfaced as an "API unavailable" state so the build and runtime degrade gracefully.
 */
export function getApiBaseUrl(): string {
  const url = process.env.API_BASE_URL;
  if (!url || !url.trim()) {
    throw new Error("API_BASE_URL is not configured");
  }
  return url.trim().replace(/\/$/, "");
}

export function isApiConfigured(): boolean {
  return Boolean(process.env.API_BASE_URL && process.env.API_BASE_URL.trim());
}
