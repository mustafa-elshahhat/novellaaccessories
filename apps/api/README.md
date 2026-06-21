# apps/api — Novella Accessories Backend

ASP.NET Core REST API for Novella Accessories, backed by **SQL Server** as the main
business database. Built with **Clean Architecture**.

> **Status:** folder/structure preparation only. This phase contains **no** solution
> file, project files, `DbContext`, entities, services, controllers, or migrations.
> Those are produced later by `docs/18_BACKEND_IMPLEMENTATION_PLAN.md`.

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

**Required to boot:**

- `ConnectionStrings__DefaultConnection` — SQL Server (Unicode/`nvarchar` for Arabic).
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` — auth token issuance/validation.
- `Cors__StorefrontOrigin`, `Cors__AdminOrigin` — allowed frontend origins (no wildcards).

**Required for WhatsApp sends (optional at boot):**

- `WhatsApp__BaseUrl` — base URL of the `apps/whatsapp` sidecar.
- `WhatsApp__InternalApiKey` — sent as `x-internal-api-key` on protected calls.

**Optional / feature-specific:**

- `Auth__CookieDomain`
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` — image storage.
- `Payment__ActiveProvider`, `Payment__WebhookSecret`.

## Local development

- Runs on `http://localhost:5000`.
- Migrations and seed run from this app; the connection string lives **only** in the
  API environment.
