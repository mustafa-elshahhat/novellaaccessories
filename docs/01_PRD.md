# PRD — Novella Accessories

## 1. Product Overview

**Novella Accessories** is a full-stack e-commerce system for a soft luxury jewelry/accessories brand selling rings, necklaces, earrings, and bracelets.

The project targets:

```text
novellaaccessories.store
```

The store must support Arabic and English, be mobile-first, use a premium visual identity, and provide a practical admin dashboard for managing products, variants, orders, discounts, shipping, payments readiness, WhatsApp communication, analytics, SEO/AEO/GEO content, and profitability.

This is not only a frontend/backend prototype. It is a complete MVP foundation for a real online store.

## 2. Goals

The MVP must deliver:

- A customer storefront that can sell products online.
- Customer accounts using phone number + password.
- WhatsApp OTP for phone verification, phone changes, and password reset.
- Full product and product-variant management.
- Product-level discounts visible to customers as old/new prices.
- General coupon codes.
- Automatic customer-only coupon after two delivered orders.
- Cart and checkout flow.
- Shipping fees per Egyptian governorate with customer-paid fee and actual shipping cost.
- Payment-method readiness for Egypt.
- Cloudinary image handling.
- WhatsApp service integration for OTP and customer messages.
- First-party analytics with real conversion-rate reporting.
- Profit and expense reporting.
- SEO/AEO/GEO-ready storefront content.
- Static legal and trust pages.
- Admin control over homepage hero content.
- Simple deployment to Vercel, SmarterASP.NET, Render, SQL Server, MongoDB (WhatsApp sessions), and Cloudinary.

## 3. Non-Goals for MVP

The MVP must not include unnecessary complexity:

- No Redis at the start.
- No heavy background queue system unless required later.
- No microservices except the dedicated WhatsApp side app already planned.
- No complex role/permission system; one admin is enough initially.
- No wishlist.
- No internal return-request system in the first version; returns are handled through WhatsApp and policy pages.
- No advanced ERP/accounting system.
- No complex marketplace/vendor features.

## 4. Target Users

### Customers

Customers browse products, create accounts, verify phone numbers, add items to cart, apply coupons, choose a governorate, place orders, and track their own order status.

### Admin

A single admin manages products, variants, categories, orders, customers, coupons, shipping fees, hero content, WhatsApp logs, static pages, SEO/AEO/GEO content, analytics, expenses, and reports.

## 5. Platform Structure

```text
apps/api          ASP.NET Core API
apps/storefront   Next.js customer-facing storefront
apps/admin        React admin dashboard
apps/whatsapp     Express + Baileys WhatsApp Web sidecar (adapted from whatsapp-service-template)
```

## 6. Customer Authentication

Customers must have a password.

Account creation flow:

1. Customer enters name, phone number, and password.
2. System sends OTP through WhatsApp.
3. Customer verifies the phone number.
4. Account becomes verified.

Login flow:

1. Customer enters phone number and password.
2. Backend validates credentials.
3. Backend issues secure session/token.

Forgot password flow:

1. Customer enters phone number.
2. System sends OTP through WhatsApp.
3. Customer verifies OTP.
4. Customer sets a new password.

Phone-number change flow:

1. Customer requests phone-number change.
2. System sends OTP to the new phone number through WhatsApp.
3. New phone number is saved only after successful verification.

No customer email is required anywhere.

## 7. Storefront Requirements

The storefront must include:

- Home page.
- Category listing page.
- Product listing page.
- Product details page.
- Cart page.
- Checkout page.
- Order success page.
- Login/register page.
- OTP verification page.
- Forgot password page.
- My orders page.
- Static pages:
  - About Us.
  - Contact Us.
  - Privacy Policy.
  - Terms and Conditions.
  - Return and Exchange Policy.
  - Shipping and Delivery Policy.
  - FAQ.
  - SEO/AEO/GEO content pages as needed.

## 8. Storefront UX Requirements

- Arabic and English support.
- Full RTL support for Arabic.
- Mobile-first layout.
- Bottom navigation bar on mobile and tablet, not a sidebar.
- Clean desktop layout.
- Premium soft luxury design matching the supplied Novella references.
- Product cards must show availability as available/unavailable only, not exact stock quantity.
- Product cards must show original price and discounted price when product discount is active.
- Product details must display active product discount clearly.
- Cart and checkout must never trust frontend prices; backend recalculates all pricing.

## 9. Brand Requirements

The visual identity is inspired by the supplied Novella images:

- Warm ivory backgrounds.
- Champagne/rose-gold accents.
- Soft brown typography.
- Thin luxury lines.
- Feminine jewelry motifs.
- Calm, elegant, premium spacing.
- Rounded elegant cards.
- Soft shadows and subtle leaf/palm decorative elements.

The brand should feel:

```text
soft, feminine, elegant, warm, premium, calm, delicate, story-driven
```

Core tagline from the reference:

```text
Jewelry That Tells Your Story
```

## 10. Categories

Default categories:

- Rings
- Necklaces
- Earrings
- Bracelets

Admin must be able to add, edit, deactivate/delete, reorder, and localize categories.

Category fields:

- Arabic name.
- English name.
- Arabic slug.
- English slug.
- Optional image.
- Sort order.
- Active/inactive status.
- SEO/AEO/GEO fields.

## 11. Products and Variants

Products must support variants.

Product fields:

- Arabic name.
- English name.
- Arabic description.
- English description.
- Category.
- Images via Cloudinary.
- Base purchase price.
- Base selling price.
- Active/inactive status.
- Featured flag.
- Product-level discount percentage.
- Product discount start date.
- Product discount end date.
- SEO/AEO/GEO fields.

Variant fields:

- SKU.
- Option values such as size, color, material, or custom attributes.
- Stock quantity.
- Purchase price override, optional.
- Selling price override, optional.
- Active/inactive status.

The customer must not see exact stock quantity. The customer sees only:

- Available.
- Unavailable.

Stock is deducted when the order becomes **Confirmed**, not when it is still Pending.

## 12. Discount Systems

The system has three discount types.

### 12.1 Product-Level Discount

Product discount is not a coupon. It is an internal price discount configured by admin.

If active, the storefront shows:

- Original price.
- Discounted price.
- Discount percentage badge.

Product-level discount is applied first.

### 12.2 General Coupons

Admin can create coupon codes for general campaigns.

Coupon properties:

- Code.
- Percentage or fixed amount.
- Start date.
- End date.
- Total usage limit.
- Per-customer usage limit.
- Minimum order subtotal.
- Active/inactive status.

### 12.3 Automatic Two-Delivered-Orders Coupon

When a customer has two actual **Delivered** orders, the system generates a coupon and sends it through WhatsApp.

Rules:

- Only Delivered orders count.
- The coupon is tied to that customer only.
- It can be used one time only.
- It has a validity duration controlled by admin.
- It has a discount percentage controlled by admin.
- It is sent automatically through WhatsApp.
- The message success/failure is logged.

## 13. Discount Stacking

Product discount and coupon discount can both apply.

Calculation order:

1. Apply product discount.
2. Apply coupon discount to the resulting product subtotal.
3. Do not apply coupon discount to shipping fee unless a future setting explicitly allows it.

The order must store snapshots of:

- Original product price.
- Product discount amount.
- Coupon discount allocation.
- Final item price.
- Purchase cost at order time.

This is required for accurate reporting after product prices change.

## 14. Orders

Order statuses:

- Pending.
- Confirmed.
- Preparing.
- Shipped.
- Delivered.
- Cancelled.

Customer cancellation rules:

- Customer can cancel while order is Pending or Confirmed.
- Customer cannot cancel once order status is Preparing.

Admin can manage order status manually in the MVP.

Stock deduction:

- Deduct stock when status becomes Confirmed.
- Avoid deducting stock for Pending orders.
- If a Confirmed order is cancelled before fulfillment, stock must be restored.

## 15. Checkout

Checkout fields:

- Customer name.
- Governorate.
- City/district.
- Detailed address.
- Notes.

Phone number comes from the verified customer account. If the customer wants to change the phone number, the new number must be verified via WhatsApp OTP.

## 16. Payment Methods

Required methods in the system:

- Cash on Delivery.
- Bank card.
- Instapay.
- Electronic wallets.

MVP behavior:

- Cash on Delivery works immediately.
- Bank card, Instapay, and wallets are prepared in schema and UI, but final activation depends on the chosen Egyptian payment provider.
- Payment-provider abstraction must exist so Paymob, Fawry, Geidea, Kashier, or another provider can be integrated later.

Payment fields:

- Payment method.
- Payment status.
- Provider name.
- Transaction reference.
- Provider response payload.
- Payment fee/commission if available.

## 17. Shipping

Admin manages shipping fees per Egyptian governorate.

Each governorate must include:

- Arabic name.
- English name.
- Customer-paid shipping fee.
- Actual shipping cost.
- Active/inactive status.

The difference between customer-paid shipping and actual shipping cost contributes to profit reporting.

Shipping company integration is prepared, not hardcoded:

- Shipping provider abstraction.
- Provider name field.
- External tracking number.
- External status.
- Manual status update in MVP.

## 18. Expenses

Admin must manage operational expenses.

Expense categories:

- Packaging.
- Ads.
- Payment gateway commissions.
- Operating expenses.
- Other expenses.

Shipping actual cost is managed in the shipping-fees page, not in general expenses, unless a special manual shipping expense is required later.

## 19. WhatsApp Service

The WhatsApp service is a production-ready standalone sidecar copied/adapted from:

```text
https://github.com/mustafa-elshahhat/whatsapp-service-template
```

Planned location:

```text
apps/whatsapp
```

Architecture:

```text
apps/api -> HTTP REST -> apps/whatsapp -> Baileys -> WhatsApp Web
```

Key facts:

- Built with Node.js, Express, and **Baileys / WhatsApp Web** as the message transport.
- Requires a **dedicated MongoDB** (separate from the main SQL Server database) for Baileys session/auth persistence.
- It is **backend-only**: deployed separately on Render and called **only** by `apps/api`, never by the storefront or admin frontend.
- It only **sends** messages; all decisions (when/what to send) and all business logic stay in `apps/api`.

Use cases (triggered and rendered by `apps/api`):

- Registration OTP delivery.
- Password reset OTP delivery.
- Phone-change OTP delivery.
- Order confirmation messages.
- Two-delivered-orders coupon messages.
- Abandoned checkout reminders.
- Inactive customer reminders.
- Failed-message retry (retry orchestration lives in `apps/api`).

OTP rules:

- OTP **generation and verification both remain in `apps/api`.**
- `apps/whatsapp` never validates OTPs — it only delivers the rendered message text.

Admin must have WhatsApp settings, templates, and message logs (all served through `apps/api`).

Risk note:

- Baileys is **unofficial WhatsApp Web automation.** Account reliability and ban risk must be considered; the service mitigates this with send throttling, per-phone/global rate limits, and a circuit breaker.
- The official **WhatsApp Business API** is kept as a possible future replacement for the Baileys transport if needed.

## 20. Customer Reminders

Reminders apply only to registered customers.

Reminder types:

- Abandoned checkout/order reminder.
- Inactive customer reminder.

Admin can configure:

- Enable/disable.
- Delay duration.
- Message template.

Each reminder is sent once per event/absence cycle to avoid spam.

## 21. Analytics

The admin dashboard must include real first-party analytics.

Track:

- Visits.
- Unique visitors.
- Sessions.
- Customer identity when logged in.
- Anonymous visitor ID before login.
- Referrer.
- UTM source, medium, campaign.
- Device type.
- Language.
- Product views.
- Add-to-cart events.
- Checkout-start events.
- Orders.
- Delivered orders.

Required metrics:

- Visit-to-purchase conversion rate.
- Visitor-to-delivered-order conversion rate.
- Checkout-to-order conversion rate.
- Order-to-delivered conversion rate.
- Best traffic sources by purchase.

The actual purchase conversion should preferably be based on Delivered orders.

## 22. Reports and Profit

Reports must include:

- Total sales.
- Total orders.
- Delivered orders.
- Cancelled orders.
- Product revenue after discounts.
- Product purchase cost.
- Product discount total.
- Coupon discount total.
- Customer-paid shipping total.
- Actual shipping cost total.
- Shipping margin.
- Expenses total.
- Gross profit.
- Net profit.
- Best-selling products.
- Most profitable products.
- Best-selling categories.
- Coupon usage.
- Payment-method breakdown.
- Governorate breakdown.

Profit should be calculated mainly from Delivered orders.

## 23. Hero Management

Admin controls the storefront hero.

Fields:

- Image.
- Arabic title.
- English title.
- Arabic subtitle.
- English subtitle.
- Arabic CTA text.
- English CTA text.
- CTA link.
- Optional linked product.
- Active/inactive status.
- Sort order.

Hero content is commonly used to promote a discounted product.

## 24. SEO/AEO/GEO

The customer storefront and backend content model must support full SEO, AEO, and GEO readiness.

Requirements:

- Localized meta title and description.
- Localized slugs.
- Canonical URLs.
- hreflang.
- Open Graph data.
- Twitter/X card data.
- Product structured data.
- Breadcrumb structured data.
- FAQ structured data.
- Organization structured data.
- Sitemap.
- robots.txt.
- Product and category answer blocks.
- Static pages with optimized content.

## 25. Admin Dashboard

Admin sections:

- Dashboard overview.
- Products.
- Product variants.
- Categories.
- Orders.
- Customers.
- Coupons.
- Two-delivered-orders coupon settings.
- Shipping governorates and fees.
- Hero management.
- WhatsApp settings.
- WhatsApp message logs.
- Payment settings/readiness.
- Expenses.
- Reports.
- Analytics and conversion rate.
- Static pages.
- SEO/AEO/GEO content management.
- Settings.

## 26. Acceptance Criteria

The MVP is acceptable when:

- Customers can register using phone + password and verify through WhatsApp OTP.
- Customers can log in using phone + password.
- Password reset works through WhatsApp OTP.
- Phone-number change requires WhatsApp OTP confirmation.
- Admin can manage categories, products, variants, discounts, and images.
- Customers can browse, add to cart, checkout, and place orders.
- Backend recalculates all prices securely.
- Product discounts and coupons stack correctly.
- Two Delivered orders trigger a one-time customer-only coupon.
- Shipping fees and actual shipping costs are handled per governorate.
- Reports show profit and conversion metrics.
- WhatsApp logs display sent and failed messages with retry.
- SEO/AEO/GEO fields exist and render correctly.
- Deployment paths are ready for Vercel, SmarterASP.NET, Render, SQL Server, and Cloudinary.
