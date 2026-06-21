# Architecture Plan — Novella Accessories

## 1. Monorepo

```text
novella-accessories/
  apps/
    api/
    storefront/
    admin/
    whatsapp/
  docs/
  README.md
```

## 2. Applications

### apps/api

Backend API using ASP.NET Core and SQL Server.

Responsibilities:

- Authentication and authorization.
- Customer accounts.
- Admin account.
- Product, category, variant, and inventory management.
- Cart and checkout.
- Orders.
- Discounts and coupons.
- Shipping-fee management.
- Payment-provider abstraction.
- WhatsApp integration calls.
- Cloudinary integration.
- Analytics event ingestion.
- Reports and profit calculations.
- SEO/AEO/GEO data APIs.

### apps/storefront

Customer storefront using Next.js.

Responsibilities:

- Public pages.
- Product and category browsing.
- Product details.
- Cart.
- Checkout.
- Customer authentication.
- My orders.
- SEO metadata rendering.
- Structured data rendering.
- First-party analytics events.

### apps/admin

Admin dashboard using React.

Responsibilities:

- Product and variant management.
- Category management.
- Order management.
- Customer management.
- Coupon management.
- Hero management.
- Shipping fees.
- WhatsApp settings and logs.
- Payments readiness settings.
- Expenses.
- Reports and analytics.
- Static pages and SEO/AEO/GEO content.

### apps/whatsapp

Standalone Express + Baileys (WhatsApp Web) sidecar adapted from `whatsapp-service-template`.

Architecture:

```text
apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web
```

Responsibilities:

- Deliver outbound WhatsApp messages via Baileys (`POST /send-message`).
- Maintain the WhatsApp Web session/auth, persisted in its own external MongoDB.
- Expose `GET /health` (public) and `GET /status` (protected) for liveness and connection state.
- Handle WhatsApp pairing (`GET /pair`, `GET /qr`) and logout (`POST /api/logout`); browser QR UI disabled by default in production.
- Throttle and protect sending (rate limits, randomized delays, circuit breaker).

Boundaries:

- Called **only** by `apps/api` via HTTP REST using an internal API key — never by the storefront or admin frontend.
- Owns **no** business logic: no OTP generation/verification, no order/coupon/reminder logic. `apps/api` decides what and when to send and renders the final text.
- Stores **only** Baileys session/auth data in MongoDB; all business message logs live in `apps/api`'s SQL Server.

Deployment target: Render (deployed separately from `apps/api`).

## 3. Infrastructure

```text
Storefront:        Vercel
Admin:             Vercel
API:               SmarterASP.NET
WhatsApp:          Render
Main database:     SQL Server (all business data)
WhatsApp sessions: MongoDB (Baileys auth/session storage only, separate from SQL Server)
Images:            Cloudinary
```

## 4. Design Principles

- Backend owns all business logic.
- Frontends never calculate final prices as trusted source.
- Provider integrations are abstracted.
- MVP stays simple.
- Avoid unnecessary infrastructure.
- Keep modules separated by feature.
- Use clear domain services for critical calculations.

## 5. Suggested Backend Structure

```text
apps/api/
  src/
    Novella.Api/
      Endpoints/
      Middleware/
      Program.cs
    Novella.Application/
      Auth/
      Products/
      Orders/
      Discounts/
      Shipping/
      Payments/
      WhatsApp/
      Analytics/
      Reports/
      Seo/
    Novella.Domain/
      Entities/
      Enums/
      ValueObjects/
      Services/
    Novella.Infrastructure/
      Persistence/
      Cloudinary/
      Payments/
      WhatsApp/
      BackgroundJobs/
    Novella.Tests/
```

## 6. Suggested Frontend Structure

### Storefront

```text
apps/storefront/
  app/
    [locale]/
      page.tsx
      category/[slug]/page.tsx
      product/[slug]/page.tsx
      cart/page.tsx
      checkout/page.tsx
      account/orders/page.tsx
      login/page.tsx
      register/page.tsx
      forgot-password/page.tsx
      policy/[slug]/page.tsx
  components/
  lib/
  messages/
  styles/
```

### Admin

```text
apps/admin/
  src/
    pages/
    components/
    features/
      auth/
      dashboard/
      products/
      categories/
      orders/
      customers/
      coupons/
      shipping/
      whatsapp/
      reports/
      analytics/
      pages/
      settings/
    lib/
```

## 7. Authentication Architecture

Customer:

- Phone number + password.
- WhatsApp OTP for registration, password reset, and phone changes.
- Customer role.

Admin:

- Single admin account for MVP.
- Admin-protected APIs.

Security requirements:

- Hash passwords securely.
- Rate-limit login and OTP flows.
- Store OTP hashes, not plain codes if practical.
- Use secure cookies or token strategy suitable for Vercel + API hosting.
- Protect admin endpoints.

## 8. Provider Abstractions

Use interfaces to avoid hardcoded providers.

```text
IImageStorageProvider      Cloudinary implementation
IWhatsAppClient            HTTP client in apps/api that calls the apps/whatsapp sidecar
IPaymentProvider           future Paymob/Fawry/Geidea/etc.
IShippingProvider          future shipping company integration
```

Note: `IWhatsAppClient` abstracts only the HTTP call from `apps/api` to the `apps/whatsapp`
sidecar (currently Baileys / WhatsApp Web). It is **not** a generic "any WhatsApp provider"
abstraction — `apps/whatsapp` is a real Baileys service, and moving to the official WhatsApp
Business API later would be a deliberate adaptation behind this client, not a drop-in swap.

## 9. Background Processing

Avoid heavy infrastructure in MVP.

Acceptable options:

- ASP.NET hosted background service if hosting allows it.
- Admin-protected manual job endpoint.
- Simple scheduled call from external cron if needed later.

Jobs:

- Abandoned checkout reminders.
- Inactive customer reminders.
- Two-delivered-orders coupon message.
- Failed WhatsApp message retry.

## 10. Analytics Architecture

The storefront sends first-party analytics events to the API.

Events:

- Page view.
- Product view.
- Add to cart.
- Checkout started.
- Order placed.
- Order delivered association.

Visitor identity:

- Anonymous visitor ID stored client-side.
- Customer ID attached after login.
- UTM and referrer stored on first visit.

## 11. SEO Architecture

Next.js storefront renders SEO metadata using API data.

SEO data sources:

- Product fields.
- Category fields.
- Static page fields.
- Site settings.
- Structured data builders.

## 12. Deployment Architecture

Vercel apps call the API through environment-based API URL.

The API calls:

- SQL Server.
- Cloudinary.
- WhatsApp service on Render.
- Payment provider later.
- Shipping provider later.

The WhatsApp service should accept requests only from authorized API credentials (internal API key) and connects to its own external MongoDB for Baileys session/auth storage. It is reachable only by `apps/api`, not by the public frontends.

## 13. Observability

MVP logging should include:

- Authentication errors.
- OTP failures.
- Payment callback failures.
- WhatsApp send failures.
- Order status changes.
- Stock movement logs.
- Unexpected API errors.

Avoid leaking sensitive data in logs.
