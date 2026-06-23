# Novella Admin

Internal authenticated dashboard for Novella Accessories. The app is a static React/Vite dashboard that talks only to `apps/api` admin endpoints.

## Stack

- React 19, Vite 8, TypeScript strict mode.
- React Router protected routes.
- TanStack Query for server state.
- React Hook Form and Zod for forms and validation.
- Vitest and Testing Library.
- ESLint flat config.

## Environment

Required public variables:

```text
VITE_API_BASE_URL=
VITE_APP_NAME=Novella Admin
```

`VITE_API_BASE_URL` is the public base URL for `apps/api` only. Every `VITE_*` variable is visible in the browser, so do not put connection strings, JWT signing keys, admin passwords, Cloudinary secrets, WhatsApp internal keys, MongoDB URIs, pairing tokens, payment webhook secrets, or provider secret keys here.

## Local Development

```powershell
npm install
npm run dev
```

The Vite dev server runs on `http://localhost:5173`. The backend must allow this origin through CORS.

## Build, Test, Audit

```powershell
npm run lint
npm run typecheck
npm test
npm run build
npm audit
npm audit --omit=dev
```

Production output is written to `dist`.

## API Boundary

- Admin calls only `apps/api`.
- Admin never calls `apps/whatsapp` directly.
- Admin never connects directly to SQL Server, MongoDB, Cloudinary, payment providers, shipping providers, or the WhatsApp sidecar.
- WhatsApp status, send test, retry, logs, and settings are proxied through `/api/admin/whatsapp/*`.
- Cloudinary uploads go through `/api/admin/uploads/image`; secrets stay server-side.

## Authentication

- Login uses `POST /api/admin/auth/login`.
- The access token is attached as `Authorization: Bearer <token>` by the centralized API client.
- Token state is kept in memory and mirrored to `sessionStorage` only for refresh survival.
- `localStorage` is not used for admin tokens.
- `/api/admin/auth/me` validates the session on startup.
- 401 clears the token, clears query cache, and returns to `/login`.
- 403 shows access denied without redirect loops.
- Logout calls `/api/admin/auth/logout`, clears token state, clears query cache, and returns to login.

Admin role is mandatory. Customer tokens cannot pass `/api/admin/auth/me` or the protected route guard.

## Modules

- Dashboard KPIs and alerts.
- Products, images, variants, stock adjustment, inventory movements.
- Categories with bilingual name and visible customer-facing descriptions.
- Orders, shipping updates, status transitions, cancellation.
- Customers and safe admin-only customer detail aggregates.
- Coupons and two-delivered-orders settings.
- Shipping governorates with customer fee and actual business cost.
- Heroes.
- WhatsApp settings, status, logs, retry, and test send.
- Payment readiness with status-only secret indicators.
- Expenses.
- Sales, profit, products, categories, coupons, payments, governorates, expenses, and analytics reports.
- Static pages (title, content, visibility only).
- Site and reminder settings.

> SEO, AEO, and GEO are generated automatically from normal business content and code; the admin
> never edits technical optimization fields. Product/category descriptions and static-page content
> drive the storefront's metadata and visible guidance. Slugs are system-generated and stable, and
> are never shown or edited in the admin. There is no standalone SEO model or admin SEO workflow.

## Security Policy

- No raw secrets are rendered.
- Secret fields show configured/not-configured status only.
- OTP WhatsApp message bodies are redacted.
- Purchase cost, exact stock, actual shipping cost, and profit fields live only inside authenticated admin screens.
- API errors are sanitized before display.
- Rich content is edited as text and must be sanitized by the storefront rendering pipeline.
- Unsafe direct provider/sidecar/database calls are prohibited.

## Vercel

`vercel.json` configures:

- Build command: `npm run build`.
- Output directory: `dist`.
- SPA rewrites to `index.html` for direct route refresh.
- No-index and security headers.
- No-store for `index.html` and immutable caching for assets.

Set `VITE_API_BASE_URL` and `VITE_APP_NAME` in Vercel project settings. Configure `apps/api` production CORS with the final admin Vercel/custom domain.

## Credential-Dependent Features

- Live Cloudinary upload requires backend Cloudinary credentials.
- Live WhatsApp connection/send requires configured backend sidecar settings and reachable sidecar.
- Online payment provider activation requires provider credentials/configuration in backend environment.
- The admin never stores or submits raw operational secrets from the browser.
