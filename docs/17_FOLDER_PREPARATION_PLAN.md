# Folder Preparation Plan — Novella Accessories

> **Scope:** This document prepares the full monorepo folder structure, environment templates, and conventions **before** any implementation begins.
>
> **⚠️ Do not implement code in this step.** This phase only creates folders, empty placeholder structures, environment templates, ignore rules, and documentation. No business logic, no entities, no UI components, no migrations.

This plan is derived from:
`01_PRD.md`, `03_ARCHITECTURE.md`, `04_DATABASE_MODEL.md`, `10_WHATSAPP_SERVICE_PLAN.md`, `13_DEPLOYMENT_ENV.md`.

---

## Phase 0 — Pre-flight Checks

**Goal:** Confirm tooling exists before creating anything.

- [ ] Confirm Git is installed and the repository root is initialized.
- [ ] Confirm .NET SDK is available for `apps/api` (ASP.NET Core).
- [ ] Confirm Node.js LTS + a package manager (npm/pnpm) is available for `apps/storefront`, `apps/admin`, `apps/whatsapp`.
- [ ] Confirm access to the WhatsApp template repository: `https://github.com/mustafa-elshahhat/whatsapp-service-template`.
- [ ] Confirm a SQL Server instance is reachable for local development (main business database).
- [ ] Confirm a MongoDB instance/cluster is reachable for `apps/whatsapp` (Baileys session storage, separate from SQL Server).
- [ ] Confirm a Cloudinary account exists (for folder naming conventions only — no credentials committed).

**Acceptance criteria:**
- All required toolchains print a version without error.
- No credentials are written to any file.

---

## Phase 1 — Root Monorepo Structure

**Goal:** Create the top-level skeleton.

Target structure:

```text
novella-accessories/
  apps/
    api/
    storefront/
    admin/
    whatsapp/
  docs/
  .gitignore
  README.md
```

Tasks:
- [ ] Create the `apps/` directory.
- [ ] Create empty placeholder folders: `apps/api/`, `apps/storefront/`, `apps/admin/`, `apps/whatsapp/`.
- [ ] Keep the existing `docs/` directory untouched (it already holds plans `01`–`20`).
- [ ] Create a root `.gitignore` (see Phase 6).
- [ ] Update root `README.md` (see Phase 7).
- [ ] Add a `.gitkeep` to any folder that would otherwise be empty so the structure is committable.

**Acceptance criteria:**
- The four `apps/*` folders exist and are committed.
- No source code, only the directory skeleton.

---

## Phase 2 — `apps/api` Structure (ASP.NET Core, Clean Architecture)

**Goal:** Lay out the solution folders for a Clean Architecture backend.

Target structure:

```text
apps/api/
  src/
    Novella.Api/            # Endpoints, middleware, composition root, Program
    Novella.Application/    # Use cases, services, DTOs, validators, interfaces
    Novella.Domain/         # Entities, enums, value objects, domain services
    Novella.Infrastructure/ # EF Core persistence, Cloudinary, Payments, WhatsApp, jobs
    Novella.Tests/          # Unit/integration tests
```

Tasks:
- [ ] Create `apps/api/src/` and the five project folders above.
- [ ] Add placeholder subfolders matching `03_ARCHITECTURE.md`:
  - `Novella.Api/Endpoints/`, `Novella.Api/Middleware/`
  - `Novella.Application/` feature folders: `Auth/`, `Products/`, `Orders/`, `Discounts/`, `Shipping/`, `Payments/`, `WhatsApp/`, `Analytics/`, `Reports/`, `Seo/`
  - `Novella.Domain/` folders: `Entities/`, `Enums/`, `ValueObjects/`, `Services/`
  - `Novella.Infrastructure/` folders: `Persistence/`, `Cloudinary/`, `Payments/`, `WhatsApp/`, `BackgroundJobs/`
- [ ] Add `apps/api/README.md` describing the layering rule: `Api → Application → Domain`, `Infrastructure → Application/Domain`, and that Domain depends on nothing.
- [ ] Add `.gitkeep` files to all empty folders.
- [ ] **Do not** create a `.sln`, `.csproj`, entities, or `DbContext` yet — that belongs to `18_BACKEND_IMPLEMENTATION_PLAN.md`.

**Acceptance criteria:**
- Folder layout matches the architecture doc.
- Dependency-direction rule is documented.
- No `.cs` files with logic exist.

---

## Phase 3 — `apps/storefront` Structure (Next.js, App Router)

**Goal:** Lay out the localized Next.js storefront skeleton.

Target structure:

```text
apps/storefront/
  app/
    [locale]/
  components/
  features/
  lib/
  messages/
  styles/
  public/
```

Tasks:
- [ ] Create `apps/storefront/` with the folders above.
- [ ] Create `app/[locale]/` as the localized route root (locales: `ar`, `en`).
- [ ] Create `messages/` placeholder for translation files (`ar.json`, `en.json` to be filled later).
- [ ] Create `features/` for feature modules (e.g. `cart/`, `checkout/`, `auth/`, `product/`, `analytics/`).
- [ ] Create `lib/` for the API client, locale/direction helpers, and analytics client placeholders.
- [ ] Add `apps/storefront/README.md` noting: App Router, `ar` default locale, RTL for Arabic, mobile-first, bottom navigation on mobile/tablet.
- [ ] Add `.gitkeep` to empty folders.
- [ ] **Do not** scaffold pages, components, or `package.json` dependencies with logic yet.

**Acceptance criteria:**
- Localized route folder `app/[locale]/` exists.
- Direction/locale strategy is documented in the README.

---

## Phase 4 — `apps/admin` Structure (React Dashboard, Vite)

**Goal:** Lay out the React admin dashboard skeleton (Vite-based per `13_DEPLOYMENT_ENV.md`, `VITE_*` env vars).

Target structure:

```text
apps/admin/
  src/
    app/         # App shell, routing, providers, auth guard
    pages/       # Route-level screens
    components/  # Shared UI (tables, forms, badges, dialogs)
    features/    # Feature modules (products, orders, coupons, reports, ...)
    lib/         # API client, auth/token handling, formatters
    styles/      # Theme, brand tokens (used lightly)
```

Tasks:
- [ ] Create `apps/admin/src/` with the folders above.
- [ ] Create `features/` subfolders aligned to admin sections: `auth/`, `dashboard/`, `products/`, `categories/`, `orders/`, `customers/`, `coupons/`, `shipping/`, `heroes/`, `whatsapp/`, `payments/`, `expenses/`, `reports/`, `analytics/`, `pages/`, `seo/`, `settings/`.
- [ ] Add `apps/admin/README.md` noting: admin-only API integration, auth-protected routes, practical/utility-first UI, brand colors used lightly.
- [ ] Add `.gitkeep` to empty folders.
- [ ] **Do not** scaffold routes, components, or dependencies with logic yet.

**Acceptance criteria:**
- Folder layout matches `20_ADMIN_IMPLEMENTATION_PLAN.md` section map.
- README documents the auth-guard expectation.

---

## Phase 5 — `apps/whatsapp` Setup (Adapted Baileys Sidecar)

**Goal:** Place the production-ready Express + Baileys (WhatsApp Web) sidecar, adapted from the real template, ready for Render, with no Novella business logic inside it.

Source: `https://github.com/mustafa-elshahhat/whatsapp-service-template`
Destination: `apps/whatsapp`

Architecture this folder must reflect:

```text
apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web
```

Tasks:
- [ ] Copy/adapt the template into `apps/whatsapp` (Node.js + Express + Baileys + MongoDB session store + Pino logging + rate limiting + circuit breaker + pairing/QR flow). This is a **real working service**, not an empty placeholder.
- [ ] Remove any committed `.env` from the template; keep only `.env.example`.
- [ ] Keep the service **business-logic-free**: it only sends messages. It must **not** generate or verify OTPs, and must not know order/coupon/reminder rules. `apps/api` renders the final text and calls the sidecar.
- [ ] Confirm/prepare the endpoint contract (structure only):
  - `GET /health` — **public** liveness/readiness; status, connection up/down, version (no secrets).
  - `GET /status` — **protected** (internal API key or pairing token); session/connection state.
  - `POST /send-message` — **primary** send endpoint (final text from `apps/api`).
  - `POST /send-template` — kept for compatibility but **deprecated/optional**; prefer `/send-message`.
  - `GET /pair`, `GET /qr` — pairing; browser QR UI **disabled by default** (`ENABLE_PAIRING_UI=false`).
  - `POST /api/logout` — logs out WhatsApp and clears auth state.
  - Do **not** keep a `/send` endpoint — it is replaced by `/send-message`.
- [ ] Add **internal API key** handling: protected requests require `x-internal-api-key` or `Authorization: Bearer`; pairing endpoints may also use `PAIRING_ADMIN_TOKEN`. `GET /health` stays public.
- [ ] Ensure **MongoDB** is wired only for Baileys auth/session storage (`MONGODB_URI`), separate from the main SQL Server DB.
- [ ] Document in `apps/whatsapp/README.md`:
  - Render deployment target; runs locally on port `4000`.
  - It is called **only** by `apps/api`, never by the storefront or admin frontend.
  - The service does **not** own OTP logic — **OTP generation and verification belong to `apps/api`**. The WhatsApp service only sends.
  - Baileys is unofficial WhatsApp Web automation; account/ban risk is mitigated by throttling, rate limits, and the circuit breaker.
  - Do not log OTP values in plain text; mask phone numbers where possible.
- [ ] Add `.gitkeep`/README so the folder is committable.

**Acceptance criteria:**
- Service folder exists as a real Express + Baileys sidecar with internal-key gating documented.
- The contract `GET /health`, `GET /status`, `POST /send-message`, `POST /send-template` (deprecated), `GET /pair`, `GET /qr`, `POST /api/logout` is listed; no `/send`.
- MongoDB is used only for Baileys sessions; no business data in MongoDB.
- No OTP generation/verification or other business logic inside the WhatsApp service.
- No secrets committed.

---

## Phase 6 — Environment Templates

**Goal:** Provide `.env.example` files for all four apps. **Templates only — never real values.**

Files to create:

```text
apps/api/.env.example
apps/storefront/.env.example
apps/admin/.env.example
apps/whatsapp/.env.example
```

### `apps/api/.env.example` (keys, empty values)
```text
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=
Jwt__Issuer=
Jwt__Audience=
Jwt__SigningKey=
Auth__CookieDomain=
Cloudinary__CloudName=
Cloudinary__ApiKey=
Cloudinary__ApiSecret=
WhatsApp__BaseUrl=
WhatsApp__InternalApiKey=
Payment__ActiveProvider=
Payment__WebhookSecret=
Cors__StorefrontOrigin=
Cors__AdminOrigin=
```

### `apps/storefront/.env.example`
```text
API_BASE_URL=
NEXT_PUBLIC_SITE_URL=https://novellaaccessories.store
NEXT_PUBLIC_DEFAULT_LOCALE=ar
NEXT_PUBLIC_SUPPORTED_LOCALES=ar,en
NEXT_PUBLIC_ANALYTICS_ENABLED=true
NEXT_PUBLIC_WHATSAPP_SUPPORT_URL=
```

### `apps/admin/.env.example`
```text
VITE_API_BASE_URL=
VITE_APP_NAME=Novella Admin
```

### `apps/whatsapp/.env.example`
```text
NODE_ENV=development
PORT=4000
MONGODB_URI=
INTERNAL_API_KEY=
PAIRING_ADMIN_TOKEN=
ENABLE_PAIRING_UI=false
SEND_DELAY_MIN_MS=5000
SEND_DELAY_MAX_MS=15000
DAILY_PHONE_LIMIT=10
GLOBAL_SEND_LIMIT_PER_MINUTE=60
SEND_TIMEOUT_MS=30000
CIRCUIT_BREAKER_THRESHOLD=3
CIRCUIT_BREAKER_COOLDOWN_MS=30000
LOG_LEVEL=info
```

> `MONGODB_URI` (Baileys session storage, separate from SQL Server) is required to boot. The official WhatsApp Business API variables are future-only and not part of this Baileys-based contract.

Tasks:
- [ ] Create the four `.env.example` files with **empty values** for secrets (keep non-secret defaults like locale/port).
- [ ] Ensure the real `.env` variants are listed in `.gitignore`.
- [ ] Note in each app README which variables are required to boot vs. optional.

**Acceptance criteria:**
- All four `.env.example` files exist with keys and no secret values.
- Real `.env` files are git-ignored.

---

## Phase 7 — Root-Level Preparation (Ignore, README, Conventions)

**Goal:** Set repo-wide conventions and documentation.

### `.gitignore`
- [ ] Ignore .NET build output: `bin/`, `obj/`.
- [ ] Ignore Node output: `node_modules/`, `.next/`, `dist/`, build caches.
- [ ] Ignore env files: `.env`, `.env.local`, `.env.*.local` (keep `.env.example`).
- [ ] Ignore IDE/OS noise: `.vs/`, `.idea/`, `.vscode/` (selectively), `*.user`, `.DS_Store`, `Thumbs.db`.
- [ ] Ignore logs and local SQL artifacts.

### README update
- [ ] Update root `README.md` to describe the four apps, deployment targets, local ports, and a "Getting Started" stub.
- [ ] Link to the `docs/` planning pack.

### Local ports (from `13_DEPLOYMENT_ENV.md`)
```text
API:        http://localhost:5000
Storefront: http://localhost:3000
Admin:      http://localhost:5173
WhatsApp:   http://localhost:4000
```
- [ ] Document these ports in the root README.

### Naming conventions
- [ ] Backend: C# PascalCase for types, project prefix `Novella.*`.
- [ ] Frontend: kebab-case route folders, PascalCase React components, camelCase variables.
- [ ] Database: PascalCase tables/columns, money as `decimal(18,2)`, UTC datetimes (per `04_DATABASE_MODEL.md`).
- [ ] Bilingual fields use `...Ar` / `...En` suffixes; SEO fields use `Seo*`, `Aeo*`, `Geo*` prefixes.

### Secrets policy
- [ ] No committed credentials anywhere (connection strings, JWT keys, Cloudinary secrets, payment secrets, WhatsApp tokens, admin password).
- [ ] Secrets live only in provider environment settings (Vercel / SmarterASP.NET (MonsterASP.NET-compatible) / Render) or local untracked `.env`.
- [ ] Document a secrets-rotation note (where each secret is stored, who can rotate).

### Cloudinary folder naming
- [ ] Define a folder convention, e.g.:
  ```text
  novella/products/{productId}/
  novella/categories/{categoryId}/
  novella/heroes/{heroId}/
  novella/pages/{pageKey}/
  ```
- [ ] Store `Url` (secure URL) + `PublicId` + alt text in the DB (per `13_DEPLOYMENT_ENV.md`).

### SQL Server connection setup notes
- [ ] Document local connection string format (placeholder only).
- [ ] Note that migrations + seed run from `apps/api`; connection string lives only in API environment.
- [ ] Note collation/Unicode requirement for Arabic content (`nvarchar`).

### Vercel / SmarterASP.NET / Render environment notes
- [ ] Vercel (storefront + admin): set `NEXT_PUBLIC_*` / `VITE_*` API URLs, configure domains, ensure sitemap/robots generation.
- [ ] SmarterASP.NET / MonsterASP.NET (API): set connection string, JWT, Cloudinary, WhatsApp URL + key, CORS origins; run migrations after deploy.
- [ ] Render (WhatsApp): set `MONGODB_URI`, `INTERNAL_API_KEY`, `PAIRING_ADMIN_TOKEN`, `ENABLE_PAIRING_UI=false`, `PORT`, send-throttle + circuit-breaker vars; verify `/health`; pair the account and confirm `/status`.
- [ ] No wildcard CORS in production.

**Acceptance criteria:**
- `.gitignore` blocks all build output and secrets but keeps `.env.example`.
- Root README documents apps, ports, conventions, and secrets policy.
- Cloudinary folder convention and SQL notes are written down.

---

## Phase 8 — Verification & Sign-off

- [ ] Confirm the full tree matches the target layout for all four apps.
- [ ] Confirm every `.env.example` exists with empty secret values.
- [ ] Confirm no real secret is present anywhere (`git grep` for known key patterns returns nothing sensitive).
- [ ] Confirm all empty folders are committable (`.gitkeep`).
- [ ] Confirm the WhatsApp folder is the real Express + Baileys sidecar, free of Novella business logic and OTP generation/verification, exposing `/send-message` (not `/send`).
- [ ] Confirm existing `docs/01`–`16` files are unchanged.

---

## Dependencies
- None upstream (this is the first executable phase).
- **Downstream:** `18_BACKEND_IMPLEMENTATION_PLAN.md`, `19_STOREFRONT_IMPLEMENTATION_PLAN.md`, and `20_ADMIN_IMPLEMENTATION_PLAN.md` all depend on this folder structure and the `.env.example` contracts existing first.
- Requires accounts/instances referenced (SQL Server, MongoDB for WhatsApp sessions, Cloudinary, Vercel, Render, SmarterASP.NET/MonsterASP.NET) to exist conceptually — but **no credentials** are used here.

## Acceptance Criteria
- The monorepo skeleton (`apps/api`, `apps/storefront`, `apps/admin`, `apps/whatsapp`, `docs/`, `.gitignore`, `README.md`) exists and is committable.
- Each app's internal folder structure matches this document.
- `apps/whatsapp` is the adapted Express + Baileys sidecar, exposes the `GET /health`, `GET /status`, `POST /send-message`, `POST /send-template` (deprecated), `GET /pair`, `GET /qr`, `POST /api/logout` contract, gates protected routes on an internal API key, uses MongoDB only for Baileys sessions, and contains no OTP or business logic.
- Four `.env.example` files exist with keys and **no** secret values.
- Conventions for naming, secrets, Cloudinary folders, ports, and SQL setup are documented.
- No application code, entities, migrations, or UI were implemented.

## Risks / Notes
- **Template drift:** the WhatsApp template (Express + Baileys) may include sample secrets or upstream defaults (for example `PORT=3005`) — review, set `PORT=4000`, and strip any committed secrets before committing.
- **Hosting note:** the API target is SmarterASP.NET, treated as MonsterASP.NET-compatible; keep deployment notes provider-neutral where possible.
- **Admin tooling:** env vars use the `VITE_*` prefix, implying a Vite-based React app; confirm this before scaffolding in the backend/admin plans.
- **Arabic data:** ensure `nvarchar` + appropriate collation is documented so RTL/Arabic content is not corrupted.
- **Secrets leakage:** the biggest risk in this phase is accidentally committing a real secret from the template or a local `.env`; the `.gitignore` and the verification grep mitigate this.

## Completion Checklist
- [ ] Root skeleton + `.gitignore` + README created.
- [ ] `apps/api` Clean Architecture folder layout created (no code).
- [ ] `apps/storefront` Next.js localized layout created (no code).
- [ ] `apps/admin` React/Vite layout created (no code).
- [ ] `apps/whatsapp` adapted Express + Baileys sidecar, internal-key gated, MongoDB sessions only, `/send-message` contract, no OTP/business logic.
- [ ] Four `.env.example` files created with empty secret values.
- [ ] Conventions documented: naming, secrets policy, Cloudinary folders, ports, SQL, hosting.
- [ ] No credentials committed; real `.env` ignored.
- [ ] Existing `docs/01`–`16` untouched.
- [ ] **No application code implemented in this phase.**
