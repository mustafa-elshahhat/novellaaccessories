# apps/api — Novella Accessories Backend

ASP.NET Core REST API for Novella Accessories, backed by **SQL Server** as the main
business database. Built with **Clean Architecture**.

> **Status:** backend implemented per `docs/18_BACKEND_IMPLEMENTATION_PLAN.md`
> (solution, `DbContext`, entities, services, controllers, `InitialCreate` migration,
> idempotent seed, and tests). Target framework: **.NET 10**.

## Project layout

```text
src/
  Novella.Api/            # Endpoints, middleware, composition root, Program
    Endpoints/
    Middleware/
  Novella.Application/    # Use cases, services, DTOs, validators, interfaces
    Auth/  Products/  Orders/  Discounts/  Shipping/
    Payments/  WhatsApp/  Analytics/  Reports/  Seo/
  Novella.Domain/         # Entities, enums, value objects, domain services
    Entities/  Enums/  ValueObjects/  Services/
  Novella.Infrastructure/ # EF Core persistence, Cloudinary, Payments, WhatsApp, jobs
    Persistence/  Cloudinary/  Payments/  WhatsApp/  BackgroundJobs/
  Novella.Tests/          # Unit/integration tests
```

## Clean Architecture dependency direction

```text
Novella.Api            ->  Novella.Application  ->  Novella.Domain
Novella.Infrastructure ->  Novella.Application / Novella.Domain
Novella.Domain         ->  (depends on nothing)
```

- `Novella.Domain` is the innermost layer and depends on **nothing**.
- `Novella.Application` depends only on `Novella.Domain` and defines interfaces
  (ports) that infrastructure implements.
- `Novella.Infrastructure` implements those interfaces and depends on
  `Novella.Application` and `Novella.Domain`.
- `Novella.Api` is the composition root: it wires everything together and depends on
  `Novella.Application` (and `Novella.Infrastructure` for DI registration only).

## WhatsApp integration boundary

The backend will talk to the WhatsApp sidecar (`apps/whatsapp`) over HTTP through a
thin **`IWhatsAppClient`** abstraction (defined in `Novella.Application`, implemented in
`Novella.Infrastructure/WhatsApp`). `apps/api` owns all business logic — OTP generation
and verification, message-text rendering, send decisions, and message logs (stored in
SQL Server). `apps/whatsapp` only delivers messages. This abstraction keeps a future
transport swap (e.g. official WhatsApp Business API) contained.

## Environment variables

Copy `.env.example` to a local untracked configuration source. Real `.env` files are
git-ignored; production secrets live in the hosting provider's environment settings.

**Required in Production (startup fails fast if missing/unsafe):**

- `ConnectionStrings__DefaultConnection` — SQL Server (Unicode/`nvarchar` for Arabic).
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` — auth token issuance/validation.
  The signing key must be **≥ 32 chars** and must not be a placeholder/dev value.
- `Cors__StorefrontOrigin`, `Cors__AdminOrigin` — allowed frontend origins (no wildcards;
  must be valid absolute `http(s)` URLs).

Production startup runs `StartupValidation` and throws a clear error listing every missing
or unsafe value. Development/Testing keep clearly-labelled local fallbacks.

**Required for WhatsApp sends (optional at boot):**

- `WhatsApp__BaseUrl` — base URL of the `apps/whatsapp` sidecar.
- `WhatsApp__InternalApiKey` — sent as `x-internal-api-key` on protected calls.

**Optional / feature-specific:**

- `Auth__CookieDomain`
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` — image storage.
- `Payment__ActiveProvider`, `Payment__WebhookSecret`.

## Database migration & seeding

Startup migration and seeding are **opt-in and independent**, both default `false`, so
Production never migrates or seeds implicitly:

- `Database__AutoMigrate` — apply pending EF migrations on startup (default `false`).
- `Database__AutoSeed` — run the idempotent seed on startup (default `false`).

Each step logs its lifecycle (`enabled` / `skipped` / `started` / `completed` / `failed`);
the connection string and seed password are never logged. Seeding is idempotent (admin,
4 categories, Egyptian governorates, 7 static pages, site/WhatsApp/reminder/two-order
settings — the latter three disabled by default). When `Database__AutoSeed=true` and an
admin must be created, `Seed__AdminPassword` is required (no production fallback password).

**Manual/controlled deployment** (recommended for Production — do not auto-migrate):

```bash
# Apply migrations explicitly against the target database:
dotnet ef database update \
  --project src/Novella.Infrastructure --startup-project src/Novella.Api
# (connection string supplied via ConnectionStrings__DefaultConnection in the environment)
```

`EnsureCreated` is never used for the relational database — migrations are the source of truth.

## Local development

- Runs on `http://localhost:5000` (see `Properties/launchSettings.json`).
- `appsettings.Development.json` enables `Database__AutoMigrate`/`AutoSeed` for convenience
  and supplies clearly-labelled local-only fallbacks (rejected in Production).
- The connection string lives **only** in the API environment, never in tracked config.
