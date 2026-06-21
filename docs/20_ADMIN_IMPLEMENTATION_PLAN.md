# Admin Dashboard Implementation Plan — `apps/admin`

> **Scope:** Complete, phased implementation plan for the Novella Accessories admin dashboard (React, Vite-based per `VITE_*` env vars).
>
> **Source of truth:** `07_ADMIN_DASHBOARD.md`, `05_API_PLAN.md`, `11_PAYMENTS_SHIPPING_PLAN.md`, `12_REPORTS_ANALYTICS_PROFIT.md`, `10_WHATSAPP_SERVICE_PLAN.md`, `13_DEPLOYMENT_ENV.md`.
>
> **Hard rules (apply to every phase):**
> - Admin talks **only** to admin-scoped APIs (`/api/admin/*`), behind admin auth.
> - **Never display raw secrets** (Cloudinary/payment/WhatsApp keys) — show configured/not-configured status only.
> - **Purchase cost / profit data is admin-only** and must never be proxied to non-admin surfaces.
> - Practical, utility-first UI; brand colors used lightly.
> - Deploy target: **Vercel**.

---

## 1. Admin Stack

- **React** (Vite), TypeScript.
- Admin-only API integration via `VITE_API_BASE_URL`.
- Auth-protected routing with a global guard.
- Practical dashboard layout: data tables, forms, filters, detail pages.
- Reusable table/form/filter/badge/dialog components.

---

## Phase A0 — Bootstrap & App Shell

**Depends on:** `17_FOLDER_PREPARATION_PLAN.md` + admin APIs from `18_BACKEND_IMPLEMENTATION_PLAN.md`.

- [ ] Initialize React + Vite app under `apps/admin`.
- [ ] Routing + layout shell (sidebar/topbar, content area).
- [ ] Typed API client reading `VITE_API_BASE_URL`; attach auth token/session.
- [ ] Shared UI kit: data table (sortable/searchable), form controls, status badges, confirmation dialog, toast.
- [ ] Global loading/empty/error states.
- [ ] Wire env from `apps/admin/.env.example`.

**Acceptance criteria:**
- App shell renders; API client reads base URL + auth.
- Shared table/form/badge/dialog components exist.

---

## Phase A1 — Authentication & Guard

**Depends on:** A0; API `/api/admin/auth/*`.

- [ ] Login page (`/login`).
- [ ] Logout.
- [ ] **Auth guard** on all routes except login (redirect unauthenticated → login).
- [ ] Session/token handling (storage + refresh/expiry behavior).
- [ ] Unauthorized (401/403) handling → redirect + message.

**Acceptance criteria:**
- All non-login routes are blocked until authenticated.
- Expired/invalid session redirects to login cleanly.

---

## Phase A2 — Route/Section Map

**Depends on:** A1.

Implement the route tree (placeholders first):

```text
/login
/dashboard
/products
/products/:id
/categories
/orders
/orders/:id
/customers
/customers/:id
/coupons
/coupons/two-order-settings
/shipping
/heroes
/whatsapp/settings
/whatsapp/logs
/payments
/expenses
/reports
/analytics
/pages
/seo
/settings
```

- [ ] Create all routes with guard + loading/empty/error boundaries.
- [ ] Navigation menu grouping sections logically.

**Acceptance criteria:**
- Every section route resolves behind the guard.
- Navigation reflects the full section map.

---

## Phase A3 — Dashboard Overview

**Depends on:** A2; API `/api/admin/dashboard/summary`, `/recent-orders`, `/alerts`.

- [ ] KPI cards: today orders, today revenue, delivered orders this month, pending orders.
- [ ] Alerts: low-stock variants, failed WhatsApp messages.
- [ ] Conversion rate; net profit this month.
- [ ] Recent orders list.
- [ ] Recent failed messages.
- [ ] Recent stock changes.

**Acceptance criteria:**
- All documented KPIs + lists render from the summary/alerts APIs.

---

## Phase A4 — Product Management

**Depends on:** A2; API `/api/admin/products/*`, `/api/admin/uploads/image`.

- [ ] Product table (search/filter, status, featured).
- [ ] Create/edit product form.
- [ ] Images via Cloudinary (upload/delete/reorder/primary).
- [ ] Category selection.
- [ ] Base purchase price + base selling price.
- [ ] Product discount %, discount start/end.
- [ ] Featured flag; active status.
- [ ] SEO/AEO/GEO fields (Ar/En).
- [ ] **Purchase cost visible only in admin** (clearly admin-internal).

**Acceptance criteria:**
- Full product CRUD + images works.
- Purchase price editable in admin only; never surfaced to public.

---

## Phase A5 — Variant Management

**Depends on:** A4; API `/api/admin/products/{id}/variants`, `/api/admin/variants/*`.

- [ ] Variants managed inside product details.
- [ ] Fields: SKU, size, color, material, custom options (Ar/En).
- [ ] Stock quantity.
- [ ] Purchase price override + selling price override (optional).
- [ ] Active/inactive.
- [ ] Stock adjustment action (with reason).
- [ ] Inventory movement history view.

**Acceptance criteria:**
- Variant CRUD + stock adjustment works; movements logged and shown.

---

## Phase A6 — Category Management

**Depends on:** A2; API `/api/admin/categories/*`.

- [ ] Arabic/English name; Arabic/English slug.
- [ ] Image (Cloudinary).
- [ ] Sort order + reorder.
- [ ] Active/inactive.
- [ ] SEO/AEO/GEO fields.

**Acceptance criteria:**
- Category CRUD + reorder + status works with bilingual fields.

---

## Phase A7 — Orders Management

**Depends on:** A2; API `/api/admin/orders/*`.

- [ ] Orders table with filters: status, date, payment method, governorate.
- [ ] Order details: customer details, address, items + variants.
- [ ] Pricing snapshot: product discounts, coupon discounts, customer-paid shipping, actual shipping cost, shipping margin.
- [ ] Payment details; status timeline; WhatsApp message history.
- [ ] Actions: Confirm, Mark Preparing, Mark Shipped, Add tracking number, Mark Delivered, Cancel (when allowed).
- [ ] Status actions reflect backend rules (stock deduct on Confirmed, restore on eligible cancel, terminal states).

**Acceptance criteria:**
- Filters work; details show full snapshot incl. shipping margin + actual cost (admin-only).
- Status actions invoke backend transitions correctly.

---

## Phase A8 — Customers

**Depends on:** A2; API customer admin endpoints.

- [ ] Customer table: verified status, total orders, delivered orders, last visit, last order.
- [ ] Customer details: orders, coupons, reminder logs, WhatsApp messages, analytics sessions.

**Acceptance criteria:**
- Customer list + detail aggregates render from admin APIs.

---

## Phase A9 — Coupons

**Depends on:** A2; API `/api/admin/coupons/*`.

- [ ] General coupons CRUD: percentage/fixed amount, start/end dates, total usage limit, per-customer usage limit, minimum subtotal, active/inactive.
- [ ] Usage report per coupon.
- [ ] Two-delivered-orders coupon settings: enable/disable, discount %, validity days, minimum subtotal, message template.

**Acceptance criteria:**
- Coupon CRUD + usage report works.
- Two-order settings editable and persisted.

---

## Phase A10 — Shipping Fees

**Depends on:** A2; API `/api/admin/shipping/governorates/*`.

- [ ] Egyptian governorates CRUD: Arabic/English name.
- [ ] Customer-paid shipping fee + actual shipping cost.
- [ ] Active/inactive; sort order.
- [ ] UI makes clear **actual cost is not shown to customers**.
- [ ] Note that shipping margin feeds reports.

**Acceptance criteria:**
- Governorate CRUD works with both fee fields.
- Actual-cost field is clearly labeled admin-internal.

---

## Phase A11 — Hero Management

**Depends on:** A2; API `/api/admin/heroes/*`.

- [ ] Image (Cloudinary).
- [ ] Arabic/English title, subtitle, CTA text.
- [ ] CTA link; linked product.
- [ ] Active/inactive; sort order.
- [ ] Mobile/desktop preview.

**Acceptance criteria:**
- Hero CRUD + reorder + status works with bilingual fields and preview.

---

## Phase A12 — WhatsApp Admin

**Depends on:** A2; API `/api/admin/whatsapp/*`.

- [ ] Settings: enable/disable, transport (Baileys / WhatsApp Web), service base URL, internal API key **status only**, and connection/session status (proxied from the sidecar's `/status` via `apps/api`).
- [ ] Templates: OTP, order confirmation, two-order coupon, abandoned checkout reminder, inactive customer reminder.
- [ ] Logs table: phone, customer, message type, status, failure reason, retry count, created date, sent date.
- [ ] Retry failed messages.
- [ ] Send test message.
- [ ] **Do not expose raw secrets.**

**Acceptance criteria:**
- Settings + templates editable; logs filterable; retry/test work.
- Only configured/not-configured status shown for keys/secrets.

---

## Phase A13 — Payment Settings / Readiness

**Depends on:** A2; API payment settings endpoints.

- [ ] Methods: COD, bank card, Instapay, electronic wallets.
- [ ] Active/inactive status per method.
- [ ] Provider name; environment; public key status; secret key status.
- [ ] Webhook URL display.
- [ ] **Do not expose raw secrets.**

**Acceptance criteria:**
- Method readiness configurable; secrets shown as status only.
- Webhook URL displayed for provider setup.

---

## Phase A14 — Expenses

**Depends on:** A2; API `/api/admin/expenses/*`.

- [ ] Categories: packaging, ads, payment gateway commissions, operating, other.
- [ ] Amount, date, notes.
- [ ] Related order (optional), related campaign (optional).

**Acceptance criteria:**
- Expense CRUD works and feeds net-profit reports.

---

## Phase A15 — Reports

**Depends on:** A2; API `/api/admin/reports/*`.

- [ ] Reports: sales, profit, products, categories, coupons, payments, governorates, expenses.
- [ ] Filters: today, this week, this month, custom range.
- [ ] Show: product revenue, product cost, product discount total, coupon discount total, customer-paid shipping, actual shipping cost, shipping margin, expenses, **gross profit**, **net profit**.

**Acceptance criteria:**
- All report views render with correct filters.
- Gross/net profit match backend calculations; commissions not double-counted.

---

## Phase A16 — Analytics

**Depends on:** A2; API `/api/admin/reports/analytics`.

- [ ] Visits, unique visitors, sessions.
- [ ] Product views, add-to-cart, checkout started, orders, delivered orders.
- [ ] Conversions: visit-to-order, visitor-to-delivered, checkout-to-order.
- [ ] Traffic sources; device breakdown; language breakdown.

**Acceptance criteria:**
- Analytics widgets render real first-party metrics + conversion rates.

---

## Phase A17 — Static Pages & SEO

**Depends on:** A2; API `/api/admin/pages/*`, `/api/admin/seo/*`.

- [ ] Editor for: About Us, Contact Us, Privacy Policy, Terms and Conditions, Return and Exchange Policy, Shipping and Delivery Policy, FAQ.
- [ ] SEO/AEO/GEO editor for: products, categories, static pages, home/site settings.
- [ ] Metadata warnings; character counters.
- [ ] Search-result preview if practical.

**Acceptance criteria:**
- All static pages editable (bilingual).
- SEO/AEO/GEO editable per entity with length warnings/counters.

---

## Phase A18 — Settings

**Depends on:** A2; API site-settings endpoints.

- [ ] Site name; domain.
- [ ] Default SEO metadata.
- [ ] Free shipping threshold (optional).
- [ ] Reminder durations.
- [ ] Language settings.
- [ ] Contact/WhatsApp links.

**Acceptance criteria:**
- Site settings + reminder durations editable and persisted.

---

## Phase A19 — Admin UX Rules

**Depends on:** all feature phases.

- [ ] Simple, practical UI; searchable tables; clear filters.
- [ ] Status badges; confirmation dialogs for destructive/irreversible actions.
- [ ] Validation messages; loading/empty/error states everywhere.
- [ ] Responsive for tablet/laptop.
- [ ] Practical over decorative; brand colors used lightly.

**Acceptance criteria:**
- Consistent table/filter/badge/dialog patterns across sections.
- Destructive actions require confirmation.

---

## Phase A20 — Admin Tests / Checks

**Depends on:** the feature covered.

- [ ] Auth guard (no access without login).
- [ ] Products CRUD; variant CRUD.
- [ ] Order status actions (Confirm/Preparing/Shipped/Delivered/Cancel rules).
- [ ] Shipping fee editing (both fee fields).
- [ ] Coupon settings (general + two-order).
- [ ] WhatsApp logs/retry.
- [ ] Report filters.
- [ ] Analytics widgets.
- [ ] Static page editing; SEO fields.
- [ ] **No raw secrets visible** (keys shown as status only).
- [ ] **Purchase cost never appears outside admin** surfaces.

**Acceptance criteria:**
- All checks pass.
- No raw secret rendered; purchase cost stays admin-only.

---

## Dependencies
- **Upstream:** `17_FOLDER_PREPARATION_PLAN.md` (folders/env); `18_BACKEND_IMPLEMENTATION_PLAN.md` admin APIs (auth, dashboard, catalog, orders, coupons, shipping, hero, WhatsApp, payments, expenses, reports, analytics, pages/SEO, settings, uploads).
- **Cross-app:** Cloudinary for image uploads; WhatsApp service status surfaced via backend (admin never calls `apps/whatsapp` directly).
- **Internal phase order:** A0 → A1 → A2 → A3 → {A4, A5, A6} → A7 → A8 → A9 → A10 → A11 → A12 → A13 → A14 → A15 → A16 → A17 → A18 → A19 → A20.
- **Downstream:** Vercel deployment per `13_DEPLOYMENT_ENV.md`.

## Acceptance Criteria
- Auth-guarded admin covering every section in the route map.
- Dashboard KPIs/alerts; full product/variant/category/order/customer management.
- Coupons (general + two-order), shipping fees (both fields), hero, WhatsApp settings/logs/retry/test.
- Payment readiness, expenses, reports (gross/net profit, correct filters), analytics with conversions.
- Static pages + SEO/AEO/GEO editors; settings.
- No raw secrets shown; purchase cost stays admin-only; destructive actions confirmed.

## Risks / Notes
- **Secret exposure:** the cardinal admin risk — render only configured/not-configured status for all keys/secrets; never echo stored secret values.
- **Cost/profit leakage:** admin reuses components that may later feed other apps; keep cost/profit DTOs admin-scoped.
- **Order action correctness:** admin status buttons must mirror backend transition + stock rules; don't replicate business logic client-side — call the API and reflect results.
- **Large tables:** orders/customers/logs can grow; implement server-side pagination/filtering to stay responsive.
- **Vite env exposure:** `VITE_*` vars are bundled into the client — never put secrets there; only the API base URL + app name.
- **Two commission sources:** ensure the reports UI presents commissions consistently with the backend's single-source decision (avoid implying double-count).

## Completion Checklist
- [ ] A0 Bootstrap + app shell + UI kit.
- [ ] A1 Auth + guard + unauthorized handling.
- [ ] A2 Full route/section map.
- [ ] A3 Dashboard overview (KPIs + alerts + recent lists).
- [ ] A4 Product management (+ Cloudinary, admin-only cost).
- [ ] A5 Variant management (+ stock adjustment + history).
- [ ] A6 Category management (bilingual + reorder).
- [ ] A7 Orders management (filters, snapshot, status actions).
- [ ] A8 Customers (list + aggregated detail).
- [ ] A9 Coupons (general + two-order settings + usage).
- [ ] A10 Shipping fees (both fee fields, admin-only actual cost).
- [ ] A11 Hero management (bilingual + preview).
- [ ] A12 WhatsApp admin (settings/templates/logs/retry/test, status-only secrets).
- [ ] A13 Payment readiness (status-only secrets, webhook URL).
- [ ] A14 Expenses.
- [ ] A15 Reports (gross/net profit, filters).
- [ ] A16 Analytics (metrics + conversions + breakdowns).
- [ ] A17 Static pages + SEO/AEO/GEO editors.
- [ ] A18 Settings.
- [ ] A19 Admin UX rules applied consistently.
- [ ] A20 Tests/checks pass; no raw secrets; cost admin-only.
- [ ] **Planning only — no production code written in this document.**
