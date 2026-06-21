# apps/admin — Novella Accessories Admin Dashboard

Internal admin dashboard for Novella Accessories.

> **Status:** folder/structure preparation only. This phase contains **no** routes, UI
> components, dependencies, or API client logic. Those are produced later by
> `docs/20_ADMIN_IMPLEMENTATION_PLAN.md`.

## Planned stack & conventions

- **React + Vite** admin dashboard is planned (`VITE_*` environment variables).
- **Admin-only API integration** — talks to `apps/api` admin endpoints.
- **Auth-protected routes** are expected (an auth guard wraps the app shell).
- **Practical, utility-first dashboard UI** — tables, forms, badges, dialogs.
- **Brand colors used lightly** — function over decoration.

## Project layout

```text
src/
  app/         # App shell, routing, providers, auth guard
  pages/       # Route-level screens
  components/  # Shared UI (tables, forms, badges, dialogs)
  features/    # Feature modules:
               #   auth, dashboard, products, categories, orders, customers,
               #   coupons, shipping, heroes, whatsapp, payments, expenses,
               #   reports, analytics, pages, seo, settings
  lib/         # API client, auth/token handling, formatters
  styles/      # Theme, brand tokens (used lightly)
```

## API access boundary

The admin calls **only `apps/api`**. It must **never** call `apps/whatsapp` directly.
Any WhatsApp connection/status or pairing visibility is proxied through `apps/api`.

**Raw secrets must never be shown in the admin.** The admin only ever sees
configured/not-configured status — never raw WhatsApp internal secrets, connection
strings, or payment secrets.

## Environment variables

Copy `.env.example` to `.env.local` (git-ignored). All admin variables are public
(`VITE_*`) and must contain **no secrets**.

- `VITE_API_BASE_URL` — base URL of `apps/api` (**required to boot**).
- `VITE_APP_NAME` — display name (default `Novella Admin`).

## Local development

- Runs on `http://localhost:5173`.
- Deployment target: **Vercel**.
