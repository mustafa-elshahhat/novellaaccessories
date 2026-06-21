/**
 * Public, client-safe configuration. Only `NEXT_PUBLIC_*` values appear here so it can be
 * imported from client components. The server-only API base URL lives in `lib/env.server.ts`.
 */

function parseLocales(raw: string | undefined): string[] {
  return (raw ?? "ar,en")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

export const publicEnv = {
  siteUrl: (process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000").replace(
    /\/$/,
    "",
  ),
  defaultLocale: process.env.NEXT_PUBLIC_DEFAULT_LOCALE ?? "ar",
  supportedLocales: parseLocales(process.env.NEXT_PUBLIC_SUPPORTED_LOCALES),
  analyticsEnabled: (process.env.NEXT_PUBLIC_ANALYTICS_ENABLED ?? "false") === "true",
  whatsappSupportUrl: process.env.NEXT_PUBLIC_WHATSAPP_SUPPORT_URL ?? "",
} as const;

/** The host (without protocol) of the configured public site URL — used for CSRF origin checks. */
export function getSiteHost(): string | null {
  try {
    return new URL(publicEnv.siteUrl).host;
  } catch {
    return null;
  }
}
