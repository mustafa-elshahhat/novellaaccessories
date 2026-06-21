import createMiddleware from "next-intl/middleware";
import { NextResponse, type NextRequest } from "next/server";
import { routing } from "./lib/i18n/routing";
import { sanitizeReturnUrl } from "./lib/security/redirect";

// Next.js 16 "proxy" convention (replaces middleware.ts). Kept intentionally lightweight:
// locale routing + a fast cookie-presence check for protected routes. No API/DB calls,
// no full token verification — final auth/verified-phone checks happen in server layouts.
const handleI18n = createMiddleware(routing);

const AUTH_COOKIE = "novella_token";
const PROTECTED_PREFIXES = ["/checkout", "/account", "/change-phone"];

function stripLocale(pathname: string): { locale: string; rest: string } {
  const segments = pathname.split("/").filter(Boolean);
  const first = segments[0];
  if (first && (routing.locales as readonly string[]).includes(first)) {
    return { locale: first, rest: "/" + segments.slice(1).join("/") };
  }
  return { locale: routing.defaultLocale, rest: pathname };
}

function isProtected(rest: string): boolean {
  return PROTECTED_PREFIXES.some((p) => rest === p || rest.startsWith(p + "/"));
}

export function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;
  const { locale, rest } = stripLocale(pathname);

  if (isProtected(rest) && !request.cookies.has(AUTH_COOKIE)) {
    const returnUrl = sanitizeReturnUrl(pathname + search, `/${locale}`);
    const loginUrl = new URL(`/${locale}/login`, request.url);
    loginUrl.searchParams.set("returnUrl", returnUrl);
    return NextResponse.redirect(loginUrl);
  }

  return handleI18n(request);
}

export const config = {
  // Run on all paths except API (BFF), Next internals, and static/public files (anything with a dot).
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml|.*\\..*).*)",
  ],
};
