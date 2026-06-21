# Implementation Prompt — Novella Accessories

Use this as a direct implementation prompt for a coding agent.

## Prompt

Build a full-stack e-commerce MVP for **Novella Accessories**.

Target domain:

```text
novellaaccessories.store
```

Create a single monorepo with:

```text
apps/api          ASP.NET Core API + SQL Server
apps/storefront   Next.js customer storefront
apps/admin        React admin dashboard
apps/whatsapp     Express + Baileys WhatsApp Web sidecar, adapted from whatsapp-service-template
```

Deployment targets:

```text
Storefront:        Vercel
Admin:             Vercel
API:               SmarterASP.NET
WhatsApp:          Render
Main database:     SQL Server (all business data)
WhatsApp sessions: MongoDB (Baileys auth/session storage only)
Images:            Cloudinary
```

Keep the first version simple. Do not add Redis, queues, or unnecessary microservices; the only separate service is the `apps/whatsapp` Baileys sidecar. Use clean provider abstractions for payment, shipping, and image storage, and a thin `IWhatsAppClient` in `apps/api` for the HTTP call to the WhatsApp sidecar.

Customer accounts:

- Customer registers with phone number, password, and name.
- Phone verification is done using WhatsApp OTP.
- Customer login uses phone number + password.
- Forgot password uses WhatsApp OTP.
- Changing phone number requires WhatsApp OTP confirmation on the new number.
- No customer email dependency.

Storefront:

- Arabic and English.
- Full RTL for Arabic.
- Mobile-first.
- Bottom navigation on mobile and tablet.
- Premium soft luxury UI based on the Novella reference: warm ivory, champagne/rose gold, mocha text, thin elegant borders, delicate jewelry style.

Default categories:

- Rings.
- Necklaces.
- Earrings.
- Bracelets.

Products:

- Products have Arabic/English names and descriptions.
- Products have variants.
- Variants may include size, color, material, or custom options.
- Each variant has stock.
- Variant may optionally override purchase price and selling price.
- Customer sees only available/unavailable, never exact stock.
- Stock is deducted when order status becomes Confirmed.
- Product images use Cloudinary.

Discounts:

- Product-level discount is internal, not a coupon. It displays old and new price to the customer.
- Product discount has percentage, start date, and end date.
- General coupon system supports percentage/fixed amount, date range, usage limits, per-customer limits, minimum order subtotal, active/inactive.
- If a product has discount and the customer also applies a valid coupon, both apply.
- Apply product discount first, then coupon.
- Coupon discount applies to product subtotal only, not shipping.
- Store order item snapshots for prices, discounts, coupon allocation, and purchase cost.

Two-delivered-orders coupon:

- When customer has two Delivered orders, generate a customer-specific coupon.
- Coupon is tied to that customer only.
- Coupon can be used once only.
- Coupon percentage and validity duration are controlled by admin.
- Send coupon through WhatsApp.
- Log success/failure.

Orders:

- Statuses: Pending, Confirmed, Preparing, Shipped, Delivered, Cancelled.
- Customer can cancel while Pending or Confirmed.
- Customer cannot cancel once Preparing starts.
- Admin manages status changes.
- Delivered orders are used for final profit and two-order coupon eligibility.

Checkout:

Required fields:

- Name.
- Governorate.
- City/district.
- Detailed address.
- Notes.

Phone comes from the verified customer account.

Payments:

- Required methods in system: Cash on Delivery, bank card, Instapay, electronic wallets.
- COD works in MVP.
- Other methods are prepared through payment provider abstraction until the selected Egyptian provider is confirmed.
- Store payment method, status, provider name, transaction reference, provider response, and commission if available.

Shipping:

- Admin manages shipping fees by Egyptian governorate.
- Each governorate has Arabic name, English name, customer-paid shipping fee, actual shipping cost, active/inactive.
- Store customer-paid fee and actual shipping cost snapshots on the order.
- Prepare shipping provider abstraction for future shipping company integration.
- MVP uses manual shipping status/tracking updates.

WhatsApp:

- Copy/adapt `https://github.com/mustafa-elshahhat/whatsapp-service-template` as `apps/whatsapp` — a production-ready Express + Baileys (WhatsApp Web) sidecar (Node.js, Pino logging, rate limiting, circuit breaker, pairing/QR flow).
- Deploy it separately on Render; run locally on port `4000`.
- Give it its own external MongoDB (`MONGODB_URI`) for Baileys auth/session storage only — never the main SQL Server DB.
- Architecture: `apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web`. It is called **only** by `apps/api`, never by the storefront or admin.
- Endpoint contract: `GET /health` (public), `GET /status` (protected), `POST /send-message` (primary), `POST /send-template` (deprecated/optional), `GET /pair`, `GET /qr`, `POST /api/logout`. Do not use `/send`.
- Protected routes require `x-internal-api-key` / `Authorization: Bearer`; pairing may use `PAIRING_ADMIN_TOKEN`; keep `ENABLE_PAIRING_UI=false` in production. Never expose these secrets to any frontend.
- The sidecar only **sends** messages. **OTP generation and verification stay in `apps/api`**; order/coupon/reminder/template-decision logic also stays in `apps/api`, which renders the final text and calls `/send-message`.
- `apps/api` stores all business message logs in SQL Server; the sidecar stores only Baileys sessions in MongoDB.
- Use it for OTP delivery, order confirmation, two-order coupon, abandoned checkout reminder, inactive customer reminder, and failed message retry (retry orchestration in `apps/api`).
- Note the risk: Baileys is unofficial WhatsApp Web automation (account/ban risk, mitigated by throttling/limits/circuit breaker); the official WhatsApp Business API is a possible future replacement.
- Admin page (served through `apps/api`) includes WhatsApp settings, templates, logs, failed messages, retry button, and connection/session status; no raw secrets shown.

Reminders:

- Registered customers only.
- Admin configures delay for abandoned checkout and inactive customer reminders.
- Send once per event/absence cycle.
- Log reminders.

Analytics:

- Build first-party analytics.
- Track visitors, sessions, UTM/referrer, device, language, page views, product views, add-to-cart, checkout-start, orders, delivered orders.
- Admin analytics page shows actual visit-to-purchase conversion and checkout conversion.

Expenses and reports:

- Add expenses page: packaging, ads, payment gateway commissions, operating expenses, other.
- Shipping actual cost is managed in shipping governorate fees, not general expenses.
- Reports show sales, delivered orders, cancelled orders, product revenue, product cost, product discount total, coupon discount total, customer-paid shipping, actual shipping cost, shipping margin, expenses, gross profit, net profit, best-selling products, most profitable products, category performance, coupons, payments, governorates, conversion rates.

SEO/AEO/GEO:

- Full SEO/AEO/GEO for customer storefront and backend content.
- Products, categories, and static pages have localized SEO fields.
- Implement metadata, canonical URLs, hreflang, sitemap, robots, Open Graph, Product structured data, Breadcrumb structured data, FAQ structured data, Organization structured data, AEO answer blocks, and GEO content blocks.

Static pages:

- About Us.
- Contact Us.
- Privacy Policy.
- Terms and Conditions.
- Return and Exchange Policy.
- Shipping and Delivery Policy.
- FAQ.

Returns/exchanges are handled through WhatsApp in MVP, with clear policy pages.

Admin:

- One admin in MVP.
- Admin pages: dashboard, products, variants, categories, orders, customers, coupons, two-order coupon settings, shipping fees, hero management, WhatsApp settings/logs, payments settings, expenses, reports, analytics, static pages, SEO/AEO/GEO content, settings.

Testing:

Add tests for:

- OTP flows.
- Phone change.
- Product discount validity.
- Coupon validation.
- Product discount + coupon stacking.
- Two Delivered orders coupon generation.
- Customer-only coupon usage.
- Stock deduction on Confirmed.
- Stock restoration on eligible cancellation.
- Shipping fee and actual cost snapshots.
- Profit calculation.
- Customer cancellation rules.

After implementation, report:

- Files changed.
- Features implemented.
- Pending provider credentials.
- How to run each app locally.
- Deployment notes for Vercel, SmarterASP.NET, Render, SQL Server, MongoDB (WhatsApp sessions), and Cloudinary.
