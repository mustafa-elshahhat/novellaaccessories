# MVP Backlog — Novella Accessories

## P0 — Foundation

- Create monorepo.
- Create `apps/api` ASP.NET Core project.
- Create `apps/storefront` Next.js project.
- Create `apps/admin` React project.
- Add `apps/whatsapp` from `whatsapp-service-template` (Express + Baileys WhatsApp Web sidecar).
- Configure shared linting/formatting where useful.
- Configure environment templates.
- Configure SQL Server connection.
- Add base migrations.
- Add seed data.

## P1 — Authentication

- Customer registration with phone + password.
- WhatsApp OTP for phone verification.
- Customer login with phone + password.
- Forgot password with WhatsApp OTP.
- Change phone with WhatsApp OTP.
- Admin login.
- Auth middleware.
- Rate limiting for login/OTP flows.

## P2 — Catalog

- Categories model and admin CRUD.
- Products model and admin CRUD.
- Product images with Cloudinary.
- Product variants.
- Stock management.
- Inventory movement logs.
- Public category APIs.
- Public product APIs.
- Storefront category pages.
- Storefront product details pages.

## P3 — Pricing and Discounts

- Product-level discount logic.
- Discount active date validation.
- Product old/new price display.
- General coupons.
- Coupon validation service.
- Coupon usage tracking.
- Product discount + coupon stacking.
- Price snapshot in orders.

## P4 — Cart and Checkout

- Customer cart.
- Add/update/remove cart items.
- Cart reprice.
- Checkout preview.
- Governorate selection.
- Address fields.
- Order creation.
- Order success page.

## P5 — Orders and Stock

- Order statuses.
- Admin order list.
- Admin order details.
- Customer my orders.
- Customer cancellation before Preparing.
- Stock deduction on Confirmed.
- Stock restoration on eligible cancellation.
- Delivered status timestamp.

## P6 — Two-Delivered-Orders Coupon

- Settings page.
- Eligibility calculation.
- Coupon generation after second Delivered order.
- Customer-only one-time usage rule.
- WhatsApp message send.
- Message log.
- Tests.

## P7 — Shipping

- Shipping governorates table.
- Admin CRUD for governorates.
- Customer-paid shipping fee.
- Actual shipping cost.
- Shipping margin snapshots on order.
- Public checkout shipping calculation.
- Shipping provider abstraction.
- Tracking number fields.

## P8 — Payments Readiness

- Payment methods model/settings.
- COD active.
- Card/Instapay/wallet methods prepared.
- Payment transactions table.
- Payment provider abstraction.
- Placeholder provider implementation.
- Callback endpoint structure.

## P9 — WhatsApp Service

- Copy/adapt `whatsapp-service-template` to `apps/whatsapp` (Express + Baileys / WhatsApp Web sidecar).
- Provision external MongoDB and configure `MONGODB_URI` for Baileys session storage (separate from SQL Server).
- Configure internal API key (`INTERNAL_API_KEY`) and pairing token (`PAIRING_ADMIN_TOKEN`); keep `ENABLE_PAIRING_UI=false`.
- Endpoints: `GET /health` (public), `GET /status` (protected), `POST /send-message` (primary), `POST /send-template` (deprecated/optional), `GET /pair`, `GET /qr`, `POST /api/logout`.
- Pair the WhatsApp account (operational) and verify `/status` shows connected.
- API integration via `IWhatsAppClient` calling `/send-message` with the internal API key (storefront/admin never call the sidecar directly).
- OTP generation/verification and message-text rendering stay in `apps/api`.
- Admin WhatsApp settings (through `apps/api`).
- Admin WhatsApp logs (stored in SQL Server).
- Retry failed message (orchestrated by `apps/api`).

## P10 — Reminders

- Reminder settings.
- Track last visit.
- Track abandoned checkout.
- Abandoned checkout reminder job.
- Inactive customer reminder job.
- Reminder logs.
- Send once per event/absence cycle.

## P11 — Expenses and Profit

- Expenses model.
- Admin expenses CRUD.
- Product cost snapshots.
- Gross profit report.
- Net profit report.
- Shipping margin report.
- Payment commission support.

## P12 — Analytics

- Visitor/session tracking.
- Event ingestion.
- Page view events.
- Product view events.
- Add-to-cart events.
- Checkout-start events.
- Order conversion linkage.
- Analytics dashboard.
- Conversion-rate reports.

## P13 — SEO/AEO/GEO

- SEO fields for products.
- SEO fields for categories.
- SEO fields for static pages.
- Next.js metadata rendering.
- Structured data.
- sitemap.xml.
- robots.txt.
- hreflang.
- AEO answer blocks.
- GEO content blocks.

## P14 — Static Pages

- About Us.
- Contact Us.
- Privacy Policy.
- Terms and Conditions.
- Return and Exchange Policy.
- Shipping and Delivery Policy.
- FAQ.
- Admin editor.

## P15 — Hero Management

- Hero model.
- Hero admin CRUD.
- Image upload.
- Linked product.
- Active/sort order.
- Storefront hero rendering.

## P16 — Storefront Polish

- Mobile bottom navigation.
- RTL layout.
- Language switcher.
- Brand colors and typography.
- Product card polish.
- Checkout UX polish.
- Empty states.
- Error states.
- Loading states.

## P17 — Admin Polish

- Dashboard cards.
- Filters.
- Search.
- Status badges.
- Confirmation dialogs.
- Responsive admin layout.
- Validation messages.

## P18 — Testing

Critical backend tests:

- OTP verification.
- Phone change.
- Product discount validity.
- Coupon validation.
- Product discount + coupon stacking.
- Two-delivered-orders coupon generation.
- Customer-only coupon usage.
- Stock deduction on Confirmed.
- Stock restoration on cancellation.
- Shipping fee and actual cost snapshots.
- Profit calculation.
- Customer cancellation rules.

## P19 — Deployment

- Vercel storefront deployment.
- Vercel admin deployment.
- SmarterASP.NET API deployment.
- Render WhatsApp deployment (with external MongoDB for Baileys sessions, account paired).
- SQL Server production database.
- Cloudinary production credentials.
- Domain setup.
- Production smoke tests.

## P20 — Post-MVP Enhancements

- Real payment provider integration.
- Real shipping company integration.
- Advanced roles/permissions.
- Export reports to Excel.
- Wishlist if needed later.
- Internal return/exchange module if needed later.
- Campaign ROI analytics.
