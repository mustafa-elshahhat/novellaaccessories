# API Plan — Novella Accessories

## 1. API Principles

- Backend owns all critical business logic.
- Frontend never sends trusted final prices.
- All price, discount, coupon, shipping, stock, and profit calculations are done on the backend.
- Purchase prices are exposed only through admin APIs.
- Customer APIs must never include purchase cost.
- APIs should be localized where needed.
- Use clear validation errors.

## 2. Public Storefront APIs

### Site and Home

```text
GET /api/public/site-settings
GET /api/public/home
GET /api/public/hero
```

### Categories

```text
GET /api/public/categories
GET /api/public/categories/{slug}
GET /api/public/categories/{slug}/products
```

### Products

```text
GET /api/public/products
GET /api/public/products/{slug}
GET /api/public/products/featured
GET /api/public/products/search
```

Product responses include:

- Localized name and description.
- Images.
- Available/unavailable flag.
- Original price.
- Final price after active product discount.
- Discount badge.
- Variants visible to customer without exact stock quantity.
- SEO/AEO/GEO fields.

### Static Pages

```text
GET /api/public/pages/{slug}
GET /api/public/faq
```

### SEO

```text
GET /api/public/seo/sitemap-data
GET /api/public/seo/product/{slug}
GET /api/public/seo/category/{slug}
GET /api/public/seo/page/{slug}
```

## 3. Customer Auth APIs

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
```

Rules:

- Register requires phone + password + name.
- OTP is sent through WhatsApp.
- Login uses phone + password.
- Password reset uses WhatsApp OTP.
- Phone change requires OTP verification on the new number.

## 4. Customer Cart APIs

```text
GET    /api/cart
POST   /api/cart/items
PATCH  /api/cart/items/{itemId}
DELETE /api/cart/items/{itemId}
DELETE /api/cart
POST   /api/cart/reprice
```

Backend validates:

- Product active.
- Variant active.
- Quantity available.
- Prices recalculated.

## 5. Checkout and Orders APIs

```text
POST /api/checkout/preview
POST /api/orders
GET  /api/orders/my
GET  /api/orders/my/{orderNumber}
POST /api/orders/my/{orderNumber}/cancel
```

Checkout preview returns:

- Product subtotal before discount.
- Product discount total.
- Coupon discount total.
- Shipping fee.
- Grand total.
- Applied coupon status.
- Availability warnings.

Order creation:

- Revalidates everything.
- Creates Pending order.
- Stores all price snapshots.

Cancellation:

- Allowed only while Pending or Confirmed.

## 6. Payment APIs

```text
GET  /api/payments/methods
POST /api/payments/initiate
POST /api/payments/callback/{provider}
GET  /api/payments/order/{orderNumber}
```

MVP:

- COD is active.
- Card, Instapay, and wallets are prepared until provider is selected.

## 7. Analytics APIs

```text
POST /api/analytics/session/start
POST /api/analytics/events
POST /api/analytics/session/identify
```

Events:

- PageView.
- ProductView.
- AddToCart.
- CheckoutStarted.
- OrderPlaced.

## 8. Admin Auth APIs

```text
POST /api/admin/auth/login
POST /api/admin/auth/logout
GET  /api/admin/auth/me
```

## 9. Admin Dashboard APIs

```text
GET /api/admin/dashboard/summary
GET /api/admin/dashboard/recent-orders
GET /api/admin/dashboard/alerts
```

## 10. Admin Category APIs

```text
GET    /api/admin/categories
POST   /api/admin/categories
GET    /api/admin/categories/{id}
PUT    /api/admin/categories/{id}
DELETE /api/admin/categories/{id}
PATCH  /api/admin/categories/{id}/status
PATCH  /api/admin/categories/reorder
```

## 11. Admin Product APIs

```text
GET    /api/admin/products
POST   /api/admin/products
GET    /api/admin/products/{id}
PUT    /api/admin/products/{id}
DELETE /api/admin/products/{id}
PATCH  /api/admin/products/{id}/status
POST   /api/admin/products/{id}/images
DELETE /api/admin/products/{id}/images/{imageId}
PATCH  /api/admin/products/{id}/images/reorder
```

## 12. Admin Variant APIs

```text
GET    /api/admin/products/{productId}/variants
POST   /api/admin/products/{productId}/variants
PUT    /api/admin/variants/{variantId}
DELETE /api/admin/variants/{variantId}
PATCH  /api/admin/variants/{variantId}/stock
PATCH  /api/admin/variants/{variantId}/status
```

## 13. Admin Order APIs

```text
GET   /api/admin/orders
GET   /api/admin/orders/{id}
PATCH /api/admin/orders/{id}/status
POST  /api/admin/orders/{id}/cancel
PATCH /api/admin/orders/{id}/shipping
```

Status update must handle:

- Stock deduction on Confirmed.
- Stock restoration on eligible cancellation.
- Delivered timestamp.
- Two-delivered-orders coupon trigger.

## 14. Admin Coupon APIs

```text
GET    /api/admin/coupons
POST   /api/admin/coupons
GET    /api/admin/coupons/{id}
PUT    /api/admin/coupons/{id}
DELETE /api/admin/coupons/{id}
PATCH  /api/admin/coupons/{id}/status
GET    /api/admin/coupons/{id}/usage
GET    /api/admin/coupons/two-order/settings
PUT    /api/admin/coupons/two-order/settings
```

## 15. Admin Shipping APIs

```text
GET   /api/admin/shipping/governorates
POST  /api/admin/shipping/governorates
PUT   /api/admin/shipping/governorates/{id}
PATCH /api/admin/shipping/governorates/{id}/status
```

Each governorate stores both customer-paid shipping fee and actual shipping cost.

## 16. Admin Hero APIs

```text
GET    /api/admin/heroes
POST   /api/admin/heroes
PUT    /api/admin/heroes/{id}
DELETE /api/admin/heroes/{id}
PATCH  /api/admin/heroes/{id}/status
PATCH  /api/admin/heroes/reorder
```

## 17. Admin WhatsApp APIs

```text
GET  /api/admin/whatsapp/settings
PUT  /api/admin/whatsapp/settings
GET  /api/admin/whatsapp/status
GET  /api/admin/whatsapp/messages
POST /api/admin/whatsapp/messages/{id}/retry
POST /api/admin/whatsapp/test
```

These are `apps/api` admin endpoints. The API integrates with the standalone `apps/whatsapp`
sidecar (Express + Baileys / WhatsApp Web) over HTTP REST:

- `apps/api` sends messages by calling the sidecar's `POST /send-message` with the internal API
  key (`x-internal-api-key` / `Authorization: Bearer`); `POST /send-template` is deprecated.
- `GET /api/admin/whatsapp/status` proxies the sidecar's protected `GET /status` so admin can see
  connection/session state without calling the sidecar directly.
- Message logs are stored in SQL Server; OTP generation/verification stays in `apps/api`; the
  sidecar only sends and stores Baileys sessions in its own MongoDB.
- The storefront and admin never call `apps/whatsapp` directly, and no WhatsApp secret is returned
  to any frontend.

## 18. Admin Expense APIs

```text
GET    /api/admin/expenses
POST   /api/admin/expenses
GET    /api/admin/expenses/{id}
PUT    /api/admin/expenses/{id}
DELETE /api/admin/expenses/{id}
```

## 19. Admin Report APIs

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

Filters:

- Today.
- This week.
- This month.
- Custom date range.

## 20. Admin Static Pages and SEO APIs

```text
GET /api/admin/pages
GET /api/admin/pages/{id}
PUT /api/admin/pages/{id}
GET /api/admin/seo/content
PUT /api/admin/seo/content/{entityType}/{entityId}
```

## 21. Cloudinary APIs

```text
POST   /api/admin/uploads/image
DELETE /api/admin/uploads/image
```

Prefer backend-signed uploads or backend-mediated upload strategy.

## 22. Error Model

Use consistent error response:

```json
{
  "code": "COUPON_EXPIRED",
  "message": "Coupon is expired.",
  "details": {}
}
```

Important codes:

- AUTH_INVALID_CREDENTIALS.
- OTP_EXPIRED.
- OTP_INVALID.
- PHONE_ALREADY_USED.
- PRODUCT_UNAVAILABLE.
- VARIANT_OUT_OF_STOCK.
- COUPON_INVALID.
- COUPON_EXPIRED.
- COUPON_USAGE_LIMIT_REACHED.
- ORDER_CANNOT_BE_CANCELLED.
- PAYMENT_PROVIDER_NOT_ACTIVE.
- SHIPPING_GOVERNORATE_INACTIVE.
