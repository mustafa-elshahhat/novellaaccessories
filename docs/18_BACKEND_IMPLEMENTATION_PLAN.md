# Backend Implementation Plan — `apps/api`

> **Scope:** Complete, phased backend implementation plan for the Novella Accessories API. Covers every backend module, not just CRUD.
>
> **Source of truth:** `01_PRD.md`, `02_BUSINESS_RULES.md`, `03_ARCHITECTURE.md`, `04_DATABASE_MODEL.md`, `05_API_PLAN.md`, `11_PAYMENTS_SHIPPING_PLAN.md`, `12_REPORTS_ANALYTICS_PROFIT.md`, `13_DEPLOYMENT_ENV.md`.
>
> **Hard rules (apply to every phase):**
> - Backend is the **single authority** for pricing, discounts, shipping, stock, checkout, orders, and reporting.
> - Frontends are **never** trusted for final totals — everything is recalculated server-side.
> - **Customer-facing APIs must never expose purchase cost** (`BasePurchasePrice`, `PurchasePriceOverride`, `PurchaseCostPerUnit`, `LineCost`, profit fields).
> - Money is `decimal(18,2)`; datetimes are UTC.
> - No Redis, no heavy queues, no extra microservices (only `apps/whatsapp` is separate).

---

## 1. Backend Stack

- **ASP.NET Core** Web API (`Novella.Api`).
- **SQL Server** via **Entity Framework Core** (code-first migrations).
- **Clean Architecture** projects:
  - `Novella.Domain` — entities, enums, value objects, domain services (no external deps).
  - `Novella.Application` — use cases, DTOs, validators, provider interfaces.
  - `Novella.Infrastructure` — EF Core persistence, Cloudinary, payments, WhatsApp HTTP client, background jobs.
  - `Novella.Api` — endpoints, middleware, auth, composition root.
  - `Novella.Tests` — unit + integration tests.
- Dependency direction: `Api → Application → Domain`; `Infrastructure → Application/Domain`; Domain depends on nothing.

---

## Phase B0 — Solution Bootstrap

**Depends on:** `17_FOLDER_PREPARATION_PLAN.md` complete.

- [ ] Create solution `Novella.sln` and the five projects under `apps/api/src/`.
- [ ] Wire project references per the dependency direction above.
- [ ] Add EF Core + SQL Server provider to Infrastructure.
- [ ] Add a validation library (e.g. FluentValidation) and a mapping approach to Application.
- [ ] Configure `appsettings.json` + environment binding for the keys in `apps/api/.env.example`.
- [ ] Add global exception-handling middleware producing the standard error model (`{ code, message, details }`).
- [ ] Add structured logging (no sensitive data: never log OTP values, passwords, full tokens).
- [ ] Add health endpoint `GET /health`.
- [ ] Configure CORS for storefront origin, admin origin, and local dev (no wildcard in production).

**Acceptance criteria:**
- Solution builds; `GET /health` returns OK.
- Errors return the consistent error model.
- CORS rejects unknown origins.

---

## Phase B1 — Database, Entities & Migrations

**Depends on:** B0.

Implement EF Core entities + configurations + the initial migration for **all** tables defined in `04_DATABASE_MODEL.md`:

- [ ] `Customers`, `AdminUsers`
- [ ] `OtpCodes`, `CustomerPhoneChangeRequests`
- [ ] `Categories`, `Products`, `ProductImages`, `ProductVariants`, `InventoryMovements`
- [ ] `Carts`, `CartItems`
- [ ] `Orders`, `OrderItems`
- [ ] `Coupons`, `CouponUsages`, `TwoOrderCouponSettings`
- [ ] `ShippingGovernorates`
- [ ] `PaymentTransactions`
- [ ] `Expenses`
- [ ] `HeroSections`, `StaticPages`
- [ ] `WhatsAppSettings`, `WhatsAppMessageLogs`
- [ ] `ReminderSettings`, `ReminderLogs`
- [ ] `AnalyticsVisitors`, `AnalyticsSessions`, `AnalyticsEvents`
- [ ] `SiteSettings`

Tasks:
- [ ] Use `decimal(18,2)` for money, `nvarchar` for bilingual content, UTC datetimes.
- [ ] Add indexes from `04_DATABASE_MODEL.md` §3 (unique `PhoneNumberNormalized`, unique slugs, unique `Sku`, `Orders` composite indexes, `AnalyticsEvents.EventType+CreatedAt`, `WhatsAppMessageLogs.Status+CreatedAt`, etc.).
- [ ] Enforce uniqueness: `Customers.PhoneNumberNormalized`, `Products.SlugAr/SlugEn`, `Categories.SlugAr/SlugEn`, `ProductVariants.Sku`, `Coupons.Code`, `Orders.OrderNumber`, `StaticPages.Key/SlugAr/SlugEn`.
- [ ] Model status fields as enums in Domain, persisted as strings.
- [ ] Create the initial migration; verify it applies cleanly to a fresh DB.

**Acceptance criteria:**
- All listed tables exist after migration with correct types, keys, and indexes.
- Re-running migrations on a clean DB succeeds.

---

## Phase B2 — Seed Data

**Depends on:** B1.

- [ ] Seed one **Admin user** (password from environment/secret, never committed; force-change note documented).
- [ ] Seed default categories: **Rings, Necklaces, Earrings, Bracelets** (Ar/En names + slugs).
- [ ] Seed **Egyptian governorates** with placeholder `CustomerPaidShippingFee` and `ActualShippingCost`.
- [ ] Seed **static pages** keys: `about`, `contact`, `privacy`, `terms`, `returns`, `shipping`, `faq` (Ar/En titles/slugs/content placeholders).
- [ ] Seed **SiteSettings** (site name Ar/En, domain `novellaaccessories.store`, default SEO, free-shipping disabled).
- [ ] Seed **WhatsAppSettings** with `IsEnabled = false`.
- [ ] Seed **ReminderSettings** with both reminders disabled.
- [ ] Seed **TwoOrderCouponSettings** disabled by default.
- [ ] Make seeding idempotent (safe to run repeatedly).

**Acceptance criteria:**
- Fresh DB after seed contains admin, 4 categories, governorates, static page keys, site settings, and disabled WhatsApp/reminder/two-order settings.
- Seeding twice does not duplicate rows.

---

## Phase B3 — Authentication & OTP

**Depends on:** B1, B2; **WhatsApp send** (B12) needed for live OTP delivery (can stub provider until then).

Customer + admin auth, all OTP delivered via `apps/whatsapp`; **OTP verification logic lives here, not in the WhatsApp service.**

- [ ] Customer **register**: name + phone + password → create unverified customer, send OTP (purpose `Register`).
- [ ] **Verify phone**: validate OTP, mark `IsPhoneVerified = true`.
- [ ] **Login**: phone + password → issue secure session/token.
- [ ] **Logout**.
- [ ] **Forgot password**: request OTP (purpose `ResetPassword`) → reset with verified OTP.
- [ ] **Change phone**: request OTP to the **new** number (purpose `ChangePhone`) via `CustomerPhoneChangeRequests`; save new number only after verification.
- [ ] **Admin login / logout / me**.
- [ ] **Password hashing** with a strong algorithm (e.g. PBKDF2/BCrypt/Argon2).
- [ ] **Rate limiting** on login, register, and all OTP endpoints (per phone + per IP).
- [ ] **OTP policy**: expiry, resend cooldown, attempt limit, temporary lockout (`LockedUntil`), purpose scoping.
- [ ] Store **OTP hash** (`CodeHash`), not plain OTP, where practical.
- [ ] Phone normalization (`PhoneNumberNormalized`) for uniqueness and lookups.

Endpoints (from `05_API_PLAN.md` §3):
```text
POST /api/auth/register
POST /api/auth/verify-phone
POST /api/auth/login
POST /api/auth/logout
POST /api/auth/forgot-password/request-otp
POST /api/auth/forgot-password/reset
POST /api/auth/change-phone/request-otp
POST /api/auth/change-phone/verify
GET  /api/auth/me
POST /api/admin/auth/login
POST /api/admin/auth/logout
GET  /api/admin/auth/me
```

**Acceptance criteria:**
- Full register → OTP → verify → login cycle works.
- Forgot-password and change-phone require valid OTP on the correct number.
- OTP expiry/resend/attempts/lockout enforced; OTP stored hashed.
- Auth + OTP endpoints are rate-limited; admin endpoints protected.

---

## Phase B4 — Catalog (Categories, Products, Variants, Images)

**Depends on:** B1, B2; Cloudinary (B11/Upload) for images.

- [ ] **Categories CRUD** (admin) with Ar/En name + slug, image, sort order, active flag, SEO/AEO/GEO; reorder.
- [ ] **Products CRUD** (admin): Ar/En name/description, category, base purchase price, base selling price, discount %, discount start/end, featured, active, SEO/AEO/GEO.
- [ ] **Variants CRUD** (admin): SKU, size/color/material/custom options (Ar/En), stock, optional purchase/selling overrides, active.
- [ ] **Cloudinary image upload/delete** for product images (store `Url`, `PublicId`, alt, sort, primary).
- [ ] **Active/inactive** flags honored everywhere.
- [ ] **Product availability calculation**: derive available/unavailable from active variants with stock > 0.
- [ ] **Customer projection** exposes only available/unavailable — **never stock counts, never purchase cost.**
- [ ] Public read APIs for storefront (categories, products, featured, search, by slug).

Endpoints (from `05_API_PLAN.md` §2, §10–§12):
```text
GET  /api/public/categories
GET  /api/public/categories/{slug}
GET  /api/public/categories/{slug}/products
GET  /api/public/products
GET  /api/public/products/{slug}
GET  /api/public/products/featured
GET  /api/public/products/search
# Admin
GET/POST/GET{id}/PUT/DELETE/PATCH status + reorder for categories
GET/POST/GET{id}/PUT/DELETE/PATCH status + images for products
GET/POST/PUT/DELETE/PATCH stock + status for variants
```

**Acceptance criteria:**
- Admin can manage categories/products/variants/images end to end.
- Public product responses show availability + prices, no stock count, no purchase cost.
- Availability reflects variant stock and active flags.

---

## Phase B5 — Pricing & Discounts

**Depends on:** B4.

- [ ] **Product-level discount**: percentage with start/end window validity check.
- [ ] **General coupons**: percentage/fixed, date range, total + per-customer usage limits, minimum subtotal, active flag.
- [ ] **Two-delivered-orders coupon**: customer-specific, single-use (logic in B7/B8; validation here).
- [ ] **Stacking order**: apply product discount first → then coupon to the resulting **product subtotal only** (never shipping).
- [ ] **Backend recalculates all totals**; ignore any client-sent prices.
- [ ] **Coupon validation service**: existence, active, date window, usage limits, per-customer limit, min subtotal, customer-specific ownership, single-use for two-order coupons.
- [ ] **Order item snapshots** persisted on order creation:
  - `OriginalUnitSellingPrice`
  - `ProductDiscountPercentage`, `ProductDiscountAmountPerUnit`, `UnitPriceAfterProductDiscount`
  - `CouponDiscountAmountPerUnit`, `FinalUnitPrice`
  - `PurchaseCostPerUnit` (admin-only), `LineRevenue`, `LineCost`, `LineGrossProfit`

**Acceptance criteria:**
- Product discount applies only within its active window.
- Coupon applies to product subtotal only, never shipping.
- Stacking computes product discount then coupon correctly.
- All snapshots stored and used by reports (stable after later price changes).

---

## Phase B6 — Cart & Checkout

**Depends on:** B4, B5, B9 (shipping fees).

- [ ] **Cart**: get cart, add/update/remove items, clear cart (per logged-in customer).
- [ ] **Cart reprice**: recompute with current prices/discounts; flag unavailable items and changed prices.
- [ ] **Checkout preview**: subtotal before discount, product discount total, coupon discount total, shipping fee, grand total, applied-coupon status, availability warnings.
- [ ] **Governorate validation**: reject inactive governorate (`SHIPPING_GOVERNORATE_INACTIVE`).
- [ ] **Shipping fee calculation** from the selected governorate's `CustomerPaidShippingFee`.
- [ ] **Order creation**: revalidate everything server-side, create **Pending** order with full snapshots (address, governorate name snapshot, totals, shipping fee + actual cost + margin).
- [ ] **Payment method selection** (COD active; others prepared — see B10).
- [ ] **Backend validation** for stock availability and pricing before order is created.

Endpoints:
```text
GET    /api/cart
POST   /api/cart/items
PATCH  /api/cart/items/{itemId}
DELETE /api/cart/items/{itemId}
DELETE /api/cart
POST   /api/cart/reprice
POST   /api/checkout/preview
POST   /api/orders
```

**Acceptance criteria:**
- Checkout preview totals exactly match the created order's totals.
- Unavailable items / inactive governorate are blocked with clear error codes.
- Created order is Pending with all price + shipping snapshots stored.

---

## Phase B7 — Orders & Stock

**Depends on:** B6.

Order statuses: **Pending → Confirmed → Preparing → Shipped → Delivered**, plus **Cancelled**.

- [ ] Status transition rules enforced server-side (valid transitions only).
- [ ] **Stock deducted on Confirmed** (not on Pending), with `InventoryMovements` (`Deduct`).
- [ ] **Stock restored** if a Confirmed order is cancelled before fulfillment (`Restore` movement).
- [ ] **Customer cancellation**: allowed only while **Pending or Confirmed**; blocked at Preparing+ (`ORDER_CANNOT_BE_CANCELLED`).
- [ ] **Delivered and Cancelled are terminal.**
- [ ] **Status timeline timestamps**: `ConfirmedAt`, `PreparingAt`, `ShippedAt`, `DeliveredAt`, `CancelledAt` (+ cancellation reason).
- [ ] Delivered transition triggers two-order coupon evaluation (B8) and order-confirmation/coupon messaging hooks (B12).
- [ ] **Inventory movement logs** for every stock change.

Endpoints:
```text
GET  /api/orders/my
GET  /api/orders/my/{orderNumber}
POST /api/orders/my/{orderNumber}/cancel
# Admin
GET   /api/admin/orders
GET   /api/admin/orders/{id}
PATCH /api/admin/orders/{id}/status
POST  /api/admin/orders/{id}/cancel
PATCH /api/admin/orders/{id}/shipping
```

**Acceptance criteria:**
- Stock decremented exactly on Confirmed; restored on eligible cancellation.
- Customer cannot cancel at Preparing or later.
- All timeline timestamps set on transitions; movements logged.

---

## Phase B8 — Two-Delivered-Orders Coupon

**Depends on:** B5, B7, B12.

- [ ] `TwoOrderCouponSettings`: enable/disable, discount %, validity days, minimum subtotal, send-WhatsApp flag.
- [ ] **Eligibility**: count only **Delivered** orders; trigger when the customer reaches **two** delivered orders.
- [ ] Generate a **customer-specific, single-use** coupon (`IsCustomerSpecific = true`, `Source = TwoDeliveredOrders`, `PerCustomerUsageLimit = 1`, validity = settings days).
- [ ] Send coupon via WhatsApp (template `TwoOrderCoupon`); log success/failure.
- [ ] Idempotency: do not generate duplicate coupons for the same milestone.

**Acceptance criteria:**
- Coupon generated once when the second Delivered order occurs.
- Coupon is bound to that customer and usable once.
- WhatsApp send attempt logged.

---

## Phase B9 — Shipping

**Depends on:** B1, B2.

- [ ] **Governorate shipping fee management** (admin CRUD + status).
- [ ] Store **customer-paid shipping fee** and **actual shipping cost** per governorate.
- [ ] On order: snapshot `CustomerPaidShippingFee`, `ActualShippingCost`, and computed `ShippingMargin` (= paid − actual).
- [ ] **Shipping provider abstraction** `IShippingProvider` (future integration).
- [ ] **Manual** tracking number + external status in MVP (`ExternalTrackingNumber`, `ExternalShippingStatus`, `ShippingProviderName`).
- [ ] Actual shipping cost is **never** exposed to customers.

Endpoints:
```text
GET   /api/admin/shipping/governorates
POST  /api/admin/shipping/governorates
PUT   /api/admin/shipping/governorates/{id}
PATCH /api/admin/shipping/governorates/{id}/status
```

**Acceptance criteria:**
- Order stores shipping fee, actual cost, and margin snapshots.
- Customer responses never reveal actual shipping cost.

---

## Phase B10 — Payments Readiness

**Depends on:** B6.

- [ ] **COD active** in MVP.
- [ ] **Bank card, Instapay, electronic wallets** prepared in schema + method list, inactive until provider chosen.
- [ ] **Payment provider abstraction** `IPaymentProvider`: `initiatePayment`, `handleCallback`, `getPaymentStatus`.
- [ ] **PaymentTransactions** persistence (method, provider, status, amount, reference, response payload, commission).
- [ ] **Callback endpoint** structure with provider routing + webhook secret validation.
- [ ] **Disabled/inactive provider** returns `PAYMENT_PROVIDER_NOT_ACTIVE`.

Endpoints:
```text
GET  /api/payments/methods
POST /api/payments/initiate
POST /api/payments/callback/{provider}
GET  /api/payments/order/{orderNumber}
```

**Acceptance criteria:**
- COD orders complete without a provider.
- Non-COD methods return a clear "not active" error until configured.
- Callback structure exists and validates the webhook secret.

---

## Phase B11 — Cloudinary / Uploads

**Depends on:** B0.

- [ ] `IImageStorageProvider` with a Cloudinary implementation.
- [ ] Backend-signed or backend-mediated upload (admin only).
- [ ] Upload + delete endpoints; store secure URL + public ID + alt + sort.
- [ ] Follow Cloudinary folder convention from `17_FOLDER_PREPARATION_PLAN.md`.

Endpoints:
```text
POST   /api/admin/uploads/image
DELETE /api/admin/uploads/image
```

**Acceptance criteria:**
- Admin can upload/delete images; deletes remove the Cloudinary asset and DB row.
- No Cloudinary secret reaches the client.

---

## Phase B12 — WhatsApp Integration

**Depends on:** B0; `apps/whatsapp` deployed/healthy; B2 (settings).

- [ ] `IWhatsAppClient` HTTP client calling the `apps/whatsapp` sidecar at `WhatsApp__BaseUrl`, primarily `POST /send-message`, with the **internal API key** (`x-internal-api-key` / `Authorization: Bearer`, from `WhatsApp__InternalApiKey`). `POST /send-template` is deprecated/optional; prefer rendering final text in `apps/api`.
- [ ] All sends recorded in `WhatsAppMessageLogs` (type, template, body, status, failure reason, retry count, sent/created).
- [ ] Message types: **OTP**, **order confirmation**, **two-order coupon**, **abandoned checkout reminder**, **inactive customer reminder**.
- [ ] **Retry failed message** (admin-triggered), incrementing `RetryCount`; bounded retries (no endless loop).
- [ ] Respect `WhatsAppSettings.IsEnabled` (no sends when disabled).
- [ ] Optionally proxy the sidecar's `GET /status` (using the internal API key) so admin can see connection/session state — admin never calls `apps/whatsapp` directly.
- [ ] `apps/api` stores business message logs in SQL Server only; it never stores or reads Baileys session data (that lives in the sidecar's MongoDB).
- [ ] Never log OTP values in plain text; mask phone numbers where possible.

Endpoints (admin):
```text
GET  /api/admin/whatsapp/settings
PUT  /api/admin/whatsapp/settings
GET  /api/admin/whatsapp/status
GET  /api/admin/whatsapp/messages
POST /api/admin/whatsapp/messages/{id}/retry
POST /api/admin/whatsapp/test
```

**Acceptance criteria:**
- Every send produces a log row with correct status.
- Retry increments count and updates status; bounded.
- Sends suppressed when WhatsApp disabled.

---

## Phase B13 — Reminders

**Depends on:** B7, B12.

- [ ] **Registered customers only.**
- [ ] **Abandoned checkout reminder** after admin-configured delay (`AbandonedCheckoutDelayHours`).
- [ ] **Inactive customer reminder** after admin-configured days (`InactiveCustomerDelayDays`), based on `LastVisitAt`.
- [ ] Admin-configurable enable/disable + delays (`ReminderSettings`).
- [ ] **Send once per event/absence cycle** (dedupe via `ReminderLogs`).
- [ ] Trigger mechanism: hosted background service or admin-protected manual job endpoint (no Redis/queues — per `03_ARCHITECTURE.md` §9).
- [ ] `ReminderLogs` for every attempt (Sent/Failed/Skipped) linked to `WhatsAppMessageLogs`.

**Acceptance criteria:**
- Reminders fire only after configured delays and only once per cycle.
- Disabled reminders never send.
- Every attempt logged.

---

## Phase B14 — Analytics Ingestion

**Depends on:** B1.

- [ ] **Anonymous visitor ID** capture → `AnalyticsVisitors`.
- [ ] **Sessions** with landing page, referrer, UTM source/medium/campaign, device type, language.
- [ ] **Customer identification after login** (attach `CustomerId` to visitor + session).
- [ ] **Events**: `PageView`, `ProductView`, `AddToCart`, `CheckoutStarted`, `OrderPlaced`.
- [ ] **Delivered-order association** for true conversion (`ConvertedOrderId`).
- [ ] Lightweight ingestion endpoints (no heavy processing inline).

Endpoints:
```text
POST /api/analytics/session/start
POST /api/analytics/events
POST /api/analytics/session/identify
```

**Acceptance criteria:**
- Visitor/session/event rows created from storefront calls.
- Login identifies the visitor; orders link to sessions.
- UTM/referrer/device/language captured on session start.

---

## Phase B15 — Reports & Profit

**Depends on:** B5, B7, B9, B10, B14, and Expenses (B16).

- [ ] **Profit based on Delivered orders.**
- [ ] Reports: **sales, products, categories, coupons, payments, governorates, expenses, analytics**.
- [ ] **Gross profit** = product revenue after discounts − purchase cost (from order item snapshots).
- [ ] **Net profit** = gross profit + shipping margin − expenses − payment commissions.
- [ ] **Avoid double-counting payment commissions**: count commissions from **either** `PaymentTransactions.CommissionAmount` **or** `Expenses` (category `PaymentGatewayCommission`), never both — document the chosen single source.
- [ ] Filters: today, this week, this month, custom range.
- [ ] All cost/profit data is admin-only.

Endpoints:
```text
GET /api/admin/reports/sales
GET /api/admin/reports/profit
GET /api/admin/reports/products
GET /api/admin/reports/categories
GET /api/admin/reports/coupons
GET /api/admin/reports/payments
GET /api/admin/reports/governorates
GET /api/admin/reports/analytics
```

**Acceptance criteria:**
- Gross/net profit match a hand-computed example from seeded delivered orders.
- Payment commissions counted exactly once (documented source).
- Filters return correct ranges.

---

## Phase B16 — Expenses

**Depends on:** B1.

- [ ] Expenses CRUD: categories `Packaging`, `Ads`, `PaymentGatewayCommission`, `Operating`, `Other`; amount, date, notes, optional related order/campaign.
- [ ] Feeds the net-profit calculation (B15).
- [ ] Shipping actual cost is **not** in general expenses (it lives on shipping governorates / order snapshots).

Endpoints:
```text
GET    /api/admin/expenses
POST   /api/admin/expenses
GET    /api/admin/expenses/{id}
PUT    /api/admin/expenses/{id}
DELETE /api/admin/expenses/{id}
```

**Acceptance criteria:**
- Expenses CRUD works and is reflected in net profit.

---

## Phase B17 — SEO/AEO/GEO Backend Support

**Depends on:** B4 (catalog), Static pages.

- [ ] Localized slugs (Ar/En) for products, categories, static pages.
- [ ] SEO title/description, AEO summary, GEO content fields surfaced via APIs.
- [ ] Metadata APIs for product/category/static page.
- [ ] **Sitemap data API** (URLs + lastmod + locales).
- [ ] **Structured-data-ready responses** (fields the storefront needs for Product/Breadcrumb/FAQ/Organization JSON-LD).

Endpoints:
```text
GET /api/public/seo/sitemap-data
GET /api/public/seo/product/{slug}
GET /api/public/seo/category/{slug}
GET /api/public/seo/page/{slug}
# Admin
GET /api/admin/seo/content
PUT /api/admin/seo/content/{entityType}/{entityId}
GET /api/admin/pages ; GET/PUT /api/admin/pages/{id}
```

**Acceptance criteria:**
- SEO/AEO/GEO fields are editable (admin) and readable (public) per entity.
- Sitemap-data API returns all indexable URLs with locales.

---

## Phase B18 — Hero, Static Pages, Site Settings, Dashboard

**Depends on:** B2, B11.

- [ ] **Hero** CRUD (image, Ar/En title/subtitle/CTA, CTA link, linked product, active, sort order).
- [ ] **Static pages** read (public) + admin edit.
- [ ] **Site settings** read/update (admin).
- [ ] **Admin dashboard summary** (today orders/revenue, delivered this month, pending, low stock, failed WhatsApp, conversion, net profit) + recent orders + alerts.

Endpoints:
```text
GET /api/public/site-settings ; GET /api/public/home ; GET /api/public/hero
GET /api/public/pages/{slug} ; GET /api/public/faq
GET    /api/admin/heroes ; POST ; PUT{id} ; DELETE{id} ; PATCH status ; PATCH reorder
GET /api/admin/dashboard/summary ; /recent-orders ; /alerts
```

**Acceptance criteria:**
- Storefront home/hero/pages data served from admin-managed content.
- Dashboard summary returns the documented KPIs.

---

## API Endpoint Groups (consolidated, from `05_API_PLAN.md`)

- [ ] Public storefront APIs (`/api/public/*`)
- [ ] Customer auth APIs (`/api/auth/*`)
- [ ] Cart APIs (`/api/cart/*`)
- [ ] Checkout APIs (`/api/checkout/*`)
- [ ] Customer order APIs (`/api/orders/my/*`)
- [ ] Payment APIs (`/api/payments/*`)
- [ ] Analytics APIs (`/api/analytics/*`)
- [ ] Admin auth APIs (`/api/admin/auth/*`)
- [ ] Admin dashboard APIs (`/api/admin/dashboard/*`)
- [ ] Admin catalog APIs (categories/products/variants)
- [ ] Admin order APIs (`/api/admin/orders/*`)
- [ ] Admin coupon APIs (`/api/admin/coupons/*`)
- [ ] Admin shipping APIs (`/api/admin/shipping/*`)
- [ ] Admin hero APIs (`/api/admin/heroes/*`)
- [ ] Admin WhatsApp APIs (`/api/admin/whatsapp/*`)
- [ ] Admin expense APIs (`/api/admin/expenses/*`)
- [ ] Admin report APIs (`/api/admin/reports/*`)
- [ ] Admin pages/SEO APIs (`/api/admin/pages/*`, `/api/admin/seo/*`)
- [ ] Upload APIs (`/api/admin/uploads/*`)

---

## Phase B19 — Backend Tests

**Depends on:** the feature it covers.

- [ ] OTP flows (expiry, resend cooldown, attempts, lockout, hash).
- [ ] Phone change (OTP on new number, only saved after verify).
- [ ] Product discount validity (active window honored).
- [ ] Coupon validation (active, dates, limits, min subtotal, ownership).
- [ ] Discount stacking (product discount then coupon, shipping excluded).
- [ ] Two-delivered-orders coupon generation (only on 2nd Delivered, once).
- [ ] Customer-only coupon usage (single-use, bound to customer).
- [ ] Stock deduction on Confirmed.
- [ ] Stock restoration on eligible cancellation.
- [ ] Shipping fee + actual cost + margin snapshots.
- [ ] Profit calculation (gross + net, commission counted once).
- [ ] Customer cancellation rules (allowed Pending/Confirmed only).
- [ ] **Customer API must not expose purchase cost** (assert no cost/profit fields in any public/customer response).

**Acceptance criteria:**
- All critical tests pass.
- A contract test guarantees no purchase cost leaks via customer-facing endpoints.

---

## Dependencies
- **Upstream:** `17_FOLDER_PREPARATION_PLAN.md` (folders + `.env.example`).
- **Cross-app:** `apps/whatsapp` healthy for B3 (live OTP), B8, B12, B13; Cloudinary for B4/B11; SQL Server for B1+.
- **Internal phase order:** B0 → B1 → B2 → B3 → B4 → B5 → B6 → B7 → {B8, B9, B10, B11, B12} → B13 → B14 → B16 → B15 → B17 → B18 → B19.
- **Downstream:** `19_STOREFRONT_IMPLEMENTATION_PLAN.md` and `20_ADMIN_IMPLEMENTATION_PLAN.md` consume these APIs.

## Acceptance Criteria
- All tables migrated and seeded; admin + catalog + governorates + static pages + settings present.
- Full auth + OTP lifecycle works with rate limiting and hashed OTPs.
- Catalog, pricing/discounts, cart/checkout, orders/stock fully functional and backend-authoritative.
- Two-order coupon, shipping snapshots, payments readiness, Cloudinary, WhatsApp, reminders, analytics, reports/profit, SEO/AEO/GEO, hero/pages/settings/dashboard all implemented per phase.
- All endpoint groups exist; error model is consistent.
- Critical test suite passes, including the no-purchase-cost-leak contract test.

## Risks / Notes
- **Profit double-counting:** payment commissions can appear in both `PaymentTransactions` and `Expenses`. Pick one authoritative source and document it (B15).
- **Stock race conditions:** concurrent Confirm/cancel must be transactional to avoid negative stock.
- **OTP abuse:** without strict rate limiting + lockout, OTP/WhatsApp can be abused; enforce per-phone and per-IP limits.
- **Background jobs on shared hosting:** SmarterASP.NET may recycle the app pool; if hosted background services are unreliable, fall back to an admin-protected manual job endpoint or external cron (no Redis/queue).
- **Purchase-cost leakage:** the single highest-value invariant — keep cost fields out of all customer DTOs and assert with tests.
- **WhatsApp availability:** if `apps/whatsapp` is down, OTP-dependent auth degrades; surface clear errors and allow retry.
- **Time zones:** store UTC; convert only at the reporting/presentation edge to keep "today/this week/this month" filters correct.

## Completion Checklist
- [ ] B0 Solution bootstrap (build, health, errors, CORS).
- [ ] B1 Entities + migration for all tables + indexes.
- [ ] B2 Seed data (admin, categories, governorates, pages, settings).
- [ ] B3 Auth + OTP (customer + admin) with rate limiting.
- [ ] B4 Catalog (categories/products/variants/images, availability).
- [ ] B5 Pricing & discounts + snapshots.
- [ ] B6 Cart & checkout (preview == order totals).
- [ ] B7 Orders & stock (deduct/restore, cancellation rules, timeline).
- [ ] B8 Two-delivered-orders coupon.
- [ ] B9 Shipping (fees, snapshots, abstraction).
- [ ] B10 Payments readiness (COD active, others prepared, callbacks).
- [ ] B11 Cloudinary uploads.
- [ ] B12 WhatsApp integration + logs + retry.
- [ ] B13 Reminders (once per cycle).
- [ ] B14 Analytics ingestion.
- [ ] B15 Reports & profit (gross/net, no double-count).
- [ ] B16 Expenses.
- [ ] B17 SEO/AEO/GEO backend support + sitemap data.
- [ ] B18 Hero/static pages/site settings/dashboard.
- [ ] B19 Tests pass, incl. no-purchase-cost-leak contract test.
- [ ] **Planning only — no production code written in this document.**
