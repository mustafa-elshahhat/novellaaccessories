# Storefront Implementation Plan — `apps/storefront`

> **Scope:** Complete, phased implementation plan for the Novella Accessories customer storefront (Next.js App Router).
>
> **Source of truth:** `01_PRD.md`, `06_STOREFRONT_UX.md`, `08_BRAND_UI_GUIDELINES.md`, `09_SEO_AEO_GEO_PLAN.md`, `05_API_PLAN.md`, `13_DEPLOYMENT_ENV.md`.
>
> **Hard rules (apply to every phase):**
> - The storefront is **never** the authority for prices/totals — it displays what `apps/api` returns and re-fetches/reprices before checkout.
> - The storefront **never** receives or renders purchase cost or stock counts — only available/unavailable.
> - Arabic (`ar`, RTL, default) and English (`en`, LTR); mobile-first; bottom navigation on mobile/tablet.
> - Deploy target: **Vercel**.

---

## 1. Storefront Stack

- **Next.js** (App Router), TypeScript.
- **i18n**: `ar` (RTL, default) + `en` (LTR) via `app/[locale]/`.
- Server-rendered SEO metadata + structured data.
- API integration through same-origin Next.js BFF route handlers. The BFF reads server-only `API_BASE_URL` and forwards to `apps/api`; browser code never receives the backend origin.
- Mobile-first responsive design; Cloudinary-optimized images.

---

## Phase S0 — Project Bootstrap

**Depends on:** `17_FOLDER_PREPARATION_PLAN.md` + backend public APIs (`18_BACKEND_IMPLEMENTATION_PLAN.md` B4/B18) available (can mock initially).

- [ ] Initialize Next.js App Router app under `apps/storefront`.
- [ ] Configure i18n routing: `app/[locale]/` with `ar` default, `en` secondary; reject unknown locales.
- [ ] Add `messages/ar.json` + `messages/en.json` translation scaffolding.
- [ ] Build a typed server API client in `lib/` reading server-only `API_BASE_URL`, plus BFF route handlers under `app/api/**` for browser calls.
- [ ] Configure `next/image` for Cloudinary remote patterns.
- [ ] Wire env from `apps/storefront/.env.example`.

**Acceptance criteria:**
- `/ar` and `/en` resolve; unknown locale 404s/redirects.
- Server API client reads `API_BASE_URL` from env; no `NEXT_PUBLIC_API_BASE_URL` is required; image domain configured.

---

## Phase S1 — Layout, Direction & Navigation

**Depends on:** S0.

- [ ] **Locale-aware root layout** sets `<html lang dir>` (`rtl` for `ar`, `ltr` for `en`).
- [ ] **Header** for desktop (logo, nav, language switcher, cart indicator, account link).
- [ ] **Bottom navigation** on mobile and tablet (not a sidebar).
- [ ] **Language switcher** preserving current route + locale.
- [ ] **Cart indicator** with item count.
- [ ] **Account link** reflecting auth state (login vs. account).
- [ ] **Auth state handling** (session/token from `apps/api`, `GET /api/auth/me`).

**Acceptance criteria:**
- Arabic renders fully RTL; English fully LTR.
- Bottom nav shows on mobile/tablet; header on desktop.
- Cart count + auth state reflect real data.

---

## Phase S2 — Brand UI Foundation

**Depends on:** S1; `08_BRAND_UI_GUIDELINES.md`.

- [ ] Define brand tokens: warm ivory backgrounds, champagne/rose-gold accents, mocha-brown text.
- [ ] Thin elegant borders, rounded cards, soft shadows.
- [ ] Delicate jewelry/palm/leaf motifs as subtle decoration.
- [ ] Calm premium spacing; **no loud sales-banner style.**
- [ ] Typography scale + RTL/LTR-aware spacing utilities.
- [ ] Reusable primitives: button, card, badge (incl. discount badge), input, price block.

**Acceptance criteria:**
- Shared components match the soft-luxury brand direction.
- Discount badge + price block components ready for product surfaces.

---

## Phase S3 — Route Structure

**Depends on:** S1.

Implement the route tree (placeholders first, filled by later phases):

```text
/[locale]
/[locale]/category/[slug]
/[locale]/product/[slug]
/[locale]/cart
/[locale]/checkout
/[locale]/order-success/[orderNumber]
/[locale]/account
/[locale]/account/orders
/[locale]/account/orders/[orderNumber]
/[locale]/login
/[locale]/register
/[locale]/verify-phone
/[locale]/forgot-password
/[locale]/change-phone
/[locale]/page/[slug]
```

- [ ] Create all routes with loading/empty/error boundaries.
- [ ] Protect `account/*`, `checkout` behind auth (redirect to login).

**Acceptance criteria:**
- Every route resolves under both locales.
- Protected routes redirect unauthenticated users to login.

---

## Phase S4 — Home Page

**Depends on:** S2, S3; API `GET /api/public/home`, `/hero`, `/site-settings`.

- [ ] **Admin-managed hero** (image, Ar/En title/subtitle/CTA, CTA link/linked product).
- [ ] **Featured categories**.
- [ ] **Discounted products** strip (old/new price).
- [ ] **Featured products** grid.
- [ ] **Brand story block** ("Jewelry That Tells Your Story").
- [ ] **Trust blocks** (shipping, authenticity, support).
- [ ] **SEO/AEO content block**.
- [ ] **FAQ preview** linking to FAQ page.

**Acceptance criteria:**
- Hero + sections render from admin-managed API data.
- Discounted products show original + discounted price.

---

## Phase S5 — Category & Product Listing

**Depends on:** S2; API `GET /api/public/categories/{slug}`, `/categories/{slug}/products`, `/products`.

- [ ] Localized category title/content.
- [ ] Product grid + product cards.
- [ ] **Availability only** (available/unavailable) — **no stock count.**
- [ ] Original price + discounted price when discount active; discount badge.
- [ ] Sort/filter if needed (e.g. price, newest).
- [ ] SEO/AEO content block at the bottom.

**Acceptance criteria:**
- Cards show availability and correct pricing; never a stock number.
- Category pages render localized content + SEO blocks.

---

## Phase S6 — Product Details

**Depends on:** S5; API `GET /api/public/products/{slug}`, `/seo/product/{slug}`.

- [ ] Image gallery (Cloudinary, primary first).
- [ ] Product name, price, discount display (old/new + badge).
- [ ] **Variant selector** (size/color/material/custom).
- [ ] Availability per selected variant (available/unavailable only).
- [ ] Quantity selector + add to cart.
- [ ] Description, care/material notes, shipping/return note.
- [ ] Related products.
- [ ] FAQ/AEO answer block.
- [ ] **Product structured data** (JSON-LD).

**Acceptance criteria:**
- Variant selection updates availability + price.
- Add to cart fires the AddToCart analytics event.
- Product JSON-LD validates.

---

## Phase S7 — Cart

**Depends on:** S6; API `GET /api/cart`, item endpoints, `POST /api/cart/reprice`.

- [ ] List cart items (name, variant, image, qty, line price).
- [ ] Quantity update + remove item.
- [ ] Coupon field (validation deferred to backend).
- [ ] **Backend reprice before checkout** (`/api/cart/reprice`).
- [ ] Summary (subtotal, product discount, coupon, shipping placeholder).
- [ ] Handle **unavailable items** (block/flag).
- [ ] Handle **changed prices** (show notice, use server values).

**Acceptance criteria:**
- Totals always come from backend reprice; client never trusted.
- Unavailable/changed items surfaced clearly before checkout.

---

## Phase S8 — Checkout

**Depends on:** S7; auth (S9); API `POST /api/checkout/preview`, `POST /api/orders`, `GET /api/payments/methods`.

- [ ] Requires **logged-in, phone-verified** customer (redirect otherwise).
- [ ] Fields: name, governorate, city/district, detailed address, notes.
- [ ] Phone comes from the verified account (read-only; change requires OTP flow).
- [ ] Payment methods: **COD**, bank card, Instapay, electronic wallets — show only methods the API/admin marks active.
- [ ] **Checkout preview** (server totals: subtotal, product discount, coupon, shipping, grand total, availability warnings).
- [ ] Place order → create Pending order.
- [ ] Redirect to order success page.

**Acceptance criteria:**
- Inactive payment methods are hidden/disabled per API.
- Displayed totals exactly match `checkout/preview`.
- Order placement fires the OrderPlaced analytics event.

---

## Phase S9 — Auth UX

**Depends on:** S3; API `/api/auth/*`.

- [ ] Register (name + phone + password) → triggers OTP.
- [ ] **OTP verification** page (`verify-phone`) with resend cooldown + attempts feedback.
- [ ] Login (phone + password).
- [ ] Forgot password (request OTP → reset).
- [ ] Change phone with OTP (new number verification).
- [ ] Logout.
- [ ] **Localized validation** + clear error messages mapped from API error codes.

**Acceptance criteria:**
- Full register → OTP → verified → login works.
- Forgot-password and change-phone require OTP.
- Errors are localized and actionable.

---

## Phase S10 — Account Pages

**Depends on:** S9; API `/api/orders/my*`, `/api/auth/me`.

- [ ] Profile view.
- [ ] Change phone (links to OTP flow).
- [ ] My orders list.
- [ ] Order details (items, snapshots, status timeline).
- [ ] **Cancel order button only when Pending or Confirmed** (hidden otherwise).

**Acceptance criteria:**
- Customers see only their own orders.
- Cancel control appears strictly for Pending/Confirmed.

---

## Phase S11 — Static Pages

**Depends on:** S3; API `GET /api/public/pages/{slug}`, `/faq`.

- [ ] About Us, Contact Us, Privacy Policy, Terms and Conditions.
- [ ] Return and Exchange Policy, Shipping and Delivery Policy, FAQ.
- [ ] Returns/exchanges direct users to **WhatsApp** (no internal return system in MVP).
- [ ] Localized content from admin-managed pages.

**Acceptance criteria:**
- All seven pages render localized content.
- Returns/exchange guidance points to WhatsApp.

---

## Phase S12 — SEO/AEO/GEO

**Depends on:** S4–S6, S11; API `/api/public/seo/*`.

- [ ] Server-rendered metadata (title/description per locale).
- [ ] Canonical URLs + **hreflang** (ar/en).
- [ ] robots rules; `sitemap.xml` from sitemap-data API.
- [ ] Open Graph + Twitter/X cards.
- [ ] **Structured data**: Product, Breadcrumb, FAQ, Organization (JSON-LD).
- [ ] **AEO answer blocks** + **GEO content blocks** on product/category/home.
- [ ] **Noindex** cart, checkout, account, and auth pages.

**Acceptance criteria:**
- Metadata + JSON-LD render server-side and validate.
- Sitemap/robots correct; transactional/auth pages noindexed.
- hreflang links ar↔en correctly.

---

## Phase S13 — First-Party Analytics

**Depends on:** S1; API `/api/analytics/*`.

- [ ] Anonymous visitor ID (persistent client-side).
- [ ] Session start (capture UTM/referrer/device/language).
- [ ] Events: PageView, ProductView, AddToCart, CheckoutStarted, OrderPlaced.
- [ ] Identify visitor after login.
- [ ] Respect `NEXT_PUBLIC_ANALYTICS_ENABLED`.

**Acceptance criteria:**
- Each key interaction emits the correct event to the API.
- Login identifies the visitor; UTM/referrer captured on first visit.

---

## Phase S14 — Performance

**Depends on:** all rendering phases.

- [ ] Cloudinary image optimization + `next/image` sizing.
- [ ] Lazy-load below-the-fold content.
- [ ] Avoid heavy animations.
- [ ] Prevent layout shift (reserve image/space dimensions).
- [ ] Strong mobile performance budget.

**Acceptance criteria:**
- Good mobile Core Web Vitals (LCP/CLS/INP) on key pages.
- No significant layout shift on product/category/home.

---

## Phase S15 — Accessibility

**Depends on:** all rendering phases.

- [ ] Labels for all inputs.
- [ ] Keyboard navigation across interactive elements.
- [ ] Sufficient color contrast (brand palette validated).
- [ ] Alt text for images.
- [ ] Loading, empty, and error states for every async surface.

**Acceptance criteria:**
- Key flows operable by keyboard; inputs labeled.
- Loading/empty/error states present throughout.

---

## Phase S16 — Storefront Tests / Checks

**Depends on:** the feature covered.

- [ ] RTL/LTR route rendering (ar/en).
- [ ] Auth flows (register/OTP/login/forgot/change-phone).
- [ ] Product discount display (old/new + badge).
- [ ] Cart reprice behavior (server-authoritative).
- [ ] Checkout validation (required fields, active payment methods, totals match preview).
- [ ] Order cancellation visibility (Pending/Confirmed only).
- [ ] SEO metadata presence per page.
- [ ] Structured data validity.
- [ ] Analytics events firing.
- [ ] **Assert no purchase cost / stock count appears anywhere in the UI or network responses consumed.**

**Acceptance criteria:**
- All checks pass for both locales.
- No cost/stock-count leakage observed.

---

## Dependencies
- **Upstream:** `17_FOLDER_PREPARATION_PLAN.md` (folders/env); `18_BACKEND_IMPLEMENTATION_PLAN.md` public + auth + cart + checkout + order + analytics + SEO APIs.
- **Cross-app:** Cloudinary-hosted images; WhatsApp OTP via backend (storefront only calls `apps/api`, never the WhatsApp service directly).
- **Internal phase order:** S0 → S1 → S2 → S3 → {S4, S5, S6} → S7 → S9 → S8 → S10 → S11 → S12 → S13 → S14 → S15 → S16.
- **Downstream:** Vercel deployment (domain, sitemap, robots) per `13_DEPLOYMENT_ENV.md`.

## Acceptance Criteria
- Full localized (ar RTL / en LTR), mobile-first storefront with bottom nav on mobile/tablet.
- Browse → product → cart → checkout → order success works end to end against backend totals.
- Auth (register/OTP/login/forgot/change-phone) and account/orders with correct cancel visibility.
- All seven static pages; returns route to WhatsApp.
- SEO/AEO/GEO metadata + structured data + sitemap/robots + hreflang; transactional/auth pages noindexed.
- First-party analytics events fire and identify on login.
- No purchase cost or stock count is ever exposed.

## Risks / Notes
- **Price-trust violations:** any client-side total computation is a defect — always reprice/preview on the server.
- **RTL bugs:** icons, paddings, and carousels often break in RTL; test Arabic explicitly, not just mirrored CSS.
- **Auth on Vercel + separate API:** cookie/token strategy must work cross-origin (CORS + cookie domain per `13_DEPLOYMENT_ENV.md`); validate early.
- **SEO of localized slugs:** ensure canonical + hreflang use the correct per-locale slug from the API, not a transliteration.
- **Analytics consent/perf:** keep analytics lightweight; gate via `NEXT_PUBLIC_ANALYTICS_ENABLED`.
- **Stale availability:** product pages can show available then fail at checkout; rely on checkout preview as the final gate and message gracefully.

## Completion Checklist
- [ ] S0 Bootstrap + i18n + API client.
- [ ] S1 Layout, direction, header + bottom nav, auth state.
- [ ] S2 Brand UI foundation.
- [ ] S3 Route tree + protected routes.
- [ ] S4 Home page (admin hero + sections).
- [ ] S5 Category/product listing (availability + pricing, no stock).
- [ ] S6 Product details + variant selector + structured data.
- [ ] S7 Cart with backend reprice.
- [ ] S8 Checkout (verified customer, active payment methods, totals == preview).
- [ ] S9 Auth UX (register/OTP/login/forgot/change-phone).
- [ ] S10 Account + orders (cancel only Pending/Confirmed).
- [ ] S11 Static pages (returns → WhatsApp).
- [ ] S12 SEO/AEO/GEO + sitemap/robots/hreflang + noindex rules.
- [ ] S13 First-party analytics events + identify.
- [ ] S14 Performance budget met.
- [ ] S15 Accessibility (labels, keyboard, states).
- [ ] S16 Tests/checks pass; no cost/stock leakage.
- [ ] **Planning only — no production code written in this document.**
