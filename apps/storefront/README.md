# apps/storefront — Novella Accessories Storefront

Customer-facing storefront for Novella Accessories. Fully localized (Arabic `ar` RTL default,
English `en` LTR), mobile-first, and built on the Next.js App Router. The storefront talks **only**
to `apps/api` and is never the pricing authority — all totals are computed server-side by the
backend and the storefront only displays them.

## Stack

| Concern      | Choice                                                        |
| ------------ | ------------------------------------------------------------ |
| Framework    | Next.js **16.2.9** (App Router, Turbopack, typed routes)     |
| Runtime      | React **19.2.7**                                             |
| Language     | TypeScript 5 (`strict`, `noUncheckedIndexedAccess`)         |
| i18n         | next-intl **4.x** (`[locale]` routing, `ar` default / RTL)  |
| Styling      | Tailwind CSS **4.x** (CSS-first `@theme`, logical props)    |
| Sanitization | isomorphic-dompurify (backend rich content)                |
| Tests        | Vitest 4 + @testing-library/react + jsdom                   |

## Getting started

```bash
npm install
cp .env.example .env.local   # set API_BASE_URL to your running apps/api
npm run dev                  # http://localhost:3000 -> /ar
```

## Scripts

| Script              | Purpose                                          |
| ------------------- | ------------------------------------------------ |
| `npm run dev`       | Dev server (Turbopack)                           |
| `npm run build`     | Production build                                 |
| `npm run start`     | Serve the production build                       |
| `npm run lint`      | ESLint (flat config; **not** `next lint`)        |
| `npm run typecheck` | `next typegen && tsc --noEmit`                   |
| `npm run test`      | Vitest unit/component tests                      |

## Environment

The backend base URL is **server-only** — the browser never calls `apps/api` directly.

```
API_BASE_URL=                       # server-only (no NEXT_PUBLIC_); used by BFF + server code
NEXT_PUBLIC_SITE_URL=               # canonical/OG/sitemap base
NEXT_PUBLIC_DEFAULT_LOCALE=ar
NEXT_PUBLIC_SUPPORTED_LOCALES=ar,en
NEXT_PUBLIC_ANALYTICS_ENABLED=true
NEXT_PUBLIC_WHATSAPP_SUPPORT_URL=   # public wa.me support link (returns/exchanges + support)
```

Never place server secrets (API URL, JWT keys, WhatsApp sidecar URL/keys, DB or Cloudinary
secrets, payment webhook secrets) in `NEXT_PUBLIC_*`. The storefront calls **only `apps/api`** —
never `apps/whatsapp`.

## Architecture

- **BFF (Backend-for-Frontend).** The browser calls same-origin `/api/*` route handlers, which
  forward to `apps/api` using a Bearer token read from an **HttpOnly** cookie (`novella_token`).
  The JWT never reaches client JS, `localStorage`, URLs, or logs.
- **CSRF.** Every state-changing BFF route enforces an `Origin`-host check (`lib/security/csrf.ts`)
  on top of the `SameSite=Lax` / `Secure` / `HttpOnly` cookie.
- **`proxy.ts`** (Next.js 16 convention, replaces `middleware.ts`) handles locale routing plus a
  fast cookie-presence gate for protected routes. Final auth / verified-phone checks live in
  server layouts and pages via `/api/auth/me`.
- **Caching.** Public catalog/content pages use timed revalidation; authenticated routes (cart,
  checkout, account, orders) are always `no-store`.
- **Pricing safety.** The cart is repriced and checkout is previewed through the backend before an
  order is placed; the storefront never computes or trusts client-side totals.

## Leakage guarantees

Customer-facing types and view-models never expose purchase cost, purchase-price overrides,
exact stock quantity, actual shipping cost, shipping margin, gross/net profit, provider response
payloads, or any internal secret. Customers see only **available / unavailable** and selling
prices. Returns/exchanges are handled via WhatsApp links (no internal return system). A leakage
contract test (`lib/api/leakage.test.ts`) fails the build if any forbidden identifier appears in
`lib/` or `features/`.

## Layout

```text
app/
  [locale]/      # localized pages (home, catalog, product, cart, checkout, account, auth, static)
  api/           # BFF route handlers (auth, cart, checkout, orders, analytics) — CSRF-guarded
  robots.ts, sitemap.ts
components/       # ui primitives + layout (header, bottom-nav, footer, locale-switcher)
features/        # auth, catalog, cart, checkout, account, analytics, home, shared
lib/             # api/ (client+server+typed), i18n, seo, security, env, format, utils
messages/        # ar.json, en.json (namespaced)
styles/          # globals.css (brand @theme tokens)
proxy.ts         # Next 16 proxy (locale routing + protected-route gate)
```

Deployment target: **Vercel**.
