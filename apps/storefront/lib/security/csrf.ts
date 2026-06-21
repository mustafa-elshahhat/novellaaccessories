import { getSiteHost } from "@/lib/env";

/**
 * CSRF defense for cookie-authenticated BFF mutation routes. A request is trusted only when
 * its `Origin` header host matches the storefront's own host (the request `Host` header) or
 * the configured public site host. Arbitrary `X-Forwarded-Host` values are NOT trusted.
 *
 * In all environments the Origin must be present and match — this is strict in Production and
 * naturally permits same-origin local development (where Origin host === Host host === localhost).
 */
export function isTrustedOrigin(request: Request): boolean {
  const origin = request.headers.get("origin");
  if (!origin) return false;

  let originHost: string;
  try {
    originHost = new URL(origin).host;
  } catch {
    return false;
  }

  const allowed = new Set<string>();
  const host = request.headers.get("host");
  if (host) allowed.add(host);
  const siteHost = getSiteHost();
  if (siteHost) allowed.add(siteHost);

  return allowed.has(originHost);
}
