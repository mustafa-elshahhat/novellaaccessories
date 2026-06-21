# apps/storefront — Novella Accessories Storefront

Customer-facing storefront for Novella Accessories.

> **Status:** folder/structure preparation only. This phase contains **no** pages,
> components, dependencies, or API client logic. Those are produced later by
> `docs/19_STOREFRONT_IMPLEMENTATION_PLAN.md`.

## Planned stack & conventions

- **Next.js App Router** is planned.
- **Localization:** Arabic `ar` is the **default** locale.
  - Arabic `ar` is **RTL**.
  - English `en` is **LTR**.
- **Mobile-first** design.
- **Bottom navigation** on mobile and tablet.

## Project layout

```text
app/
  [locale]/   # localized route root (ar, en)
components/    # shared UI components
features/      # feature modules (cart, checkout, auth, product, analytics, ...)
lib/           # API client, locale/direction helpers, analytics client
messages/      # translation files (ar.json, en.json) — filled later
styles/        # global styles / theme
public/        # static assets
```

## API access boundary

The storefront calls **only `apps/api`**. It must **never** call `apps/whatsapp`
directly. WhatsApp internal secrets and any other server-side secret must never appear
in storefront environment variables — only `apps/api` holds those.

## Environment variables

Copy `.env.example` to `.env.local` (git-ignored). All storefront variables are
public (`NEXT_PUBLIC_*`) and must contain **no secrets**.

- `NEXT_PUBLIC_API_BASE_URL` — base URL of `apps/api` (**required to boot**).
- `NEXT_PUBLIC_SITE_URL` — canonical site URL (default `https://novellaaccessories.store`).
- `NEXT_PUBLIC_DEFAULT_LOCALE` — default locale (`ar`).
- `NEXT_PUBLIC_SUPPORTED_LOCALES` — supported locales (`ar,en`).
- `NEXT_PUBLIC_ANALYTICS_ENABLED` — toggle analytics (optional).

## Local development

- Runs on `http://localhost:3000`.
- Deployment target: **Vercel**.
