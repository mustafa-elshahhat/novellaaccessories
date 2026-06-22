# Novella Accessories

A full-stack e-commerce **MVP** for Novella Accessories — a bilingual (Arabic/English)
online accessories store. This repository is a monorepo containing the API, customer
storefront, admin dashboard, and a WhatsApp messaging sidecar.

## Apps

| App | Path | Stack |
|---|---|---|
| API | `apps/api` | ASP.NET Core REST API + **SQL Server** (Clean Architecture) |
| Storefront | `apps/storefront` | Next.js customer storefront (App Router, `ar`/`en`) |
| Admin | `apps/admin` | React/Vite admin dashboard |
| WhatsApp | `apps/whatsapp` | Express + Baileys WhatsApp Web sidecar, adapted from [`whatsapp-service-template`](https://github.com/mustafa-elshahhat/whatsapp-service-template) |

```text
novella-accessories/
  apps/
    api/         # ASP.NET Core API + SQL Server
    storefront/  # Next.js customer storefront
    admin/       # React/Vite admin dashboard
    whatsapp/    # Express + Baileys WhatsApp Web sidecar
  docs/          # planning pack (01–20)
  .gitignore
  README.md
```

> **Service boundaries:** the storefront and admin call **only** `apps/api`. Only
> `apps/api` calls `apps/whatsapp`. `apps/api` owns all business logic (including OTP
> generation/verification and message-text rendering); `apps/whatsapp` only delivers
> messages and manages the WhatsApp Web session.

## Deployment targets

| Component | Target |
|---|---|
| Storefront | **Vercel** |
| Admin | **Vercel** |
| API | **SmarterASP.NET / MonsterASP.NET-compatible** hosting |
| WhatsApp | **Render** |
| Main database | **SQL Server** |
| WhatsApp sessions | **MongoDB** (Baileys auth/session state only) |
| Images | **Cloudinary** |

## Local ports

```text
API:        http://localhost:5000
Storefront: http://localhost:3000
Admin:      http://localhost:5173
WhatsApp:   http://localhost:4000
```

## Secrets policy

- **No real credentials are ever committed** — connection strings, JWT keys, Cloudinary
  secrets, payment secrets, and WhatsApp tokens stay out of the repository.
- Real `.env` files are **git-ignored**; only `.env.example` templates (with empty
  secret values) are committed.
- **Production secrets live in provider environment variables** (Vercel, SmarterASP.NET /
  MonsterASP.NET, Render).
- WhatsApp internal secrets (`INTERNAL_API_KEY`, `PAIRING_ADMIN_TOKEN`, `MONGODB_URI`)
  are server-side only and must never be exposed to any frontend (no `NEXT_PUBLIC_*`,
  no `VITE_*`).

## Cloudinary folder convention

```text
novella/products/{productId}/
novella/categories/{categoryId}/
novella/heroes/{heroId}/
novella/pages/{pageKey}/
```

Store the secure `Url` + `PublicId` + alt text in the database.

## SQL Server notes

- The **main business database** only.
- The connection string belongs **only** to the API environment (`apps/api`).
- Use **Unicode-capable storage** (`nvarchar` + appropriate collation) so Arabic content
  is stored and displayed correctly.

## MongoDB notes

- Used **only** by `apps/whatsapp` for Baileys auth/session storage.
- **Separate** from SQL Server.
- **No business data** is ever stored in MongoDB.

## Getting started

Each app has its own `README.md` and `.env.example`:

- [`apps/api/README.md`](apps/api/README.md)
- [`apps/storefront/README.md`](apps/storefront/README.md)
- [`apps/admin/README.md`](apps/admin/README.md)
- [`apps/whatsapp/README.md`](apps/whatsapp/README.md)

Copy each `.env.example` to a local untracked env file and fill in values. Real `.env`
files are git-ignored.

## Documentation

The full planning pack lives in [`docs/`](docs/) (plans `01`–`20`), indexed by
[`docs/README.md`](docs/README.md). The implemented system follows the connected
architecture in those documents: storefront BFF and admin call `apps/api`, `apps/api`
owns SQL Server business data and calls the WhatsApp sidecar, and MongoDB remains only
for Baileys session state in `apps/whatsapp`.
