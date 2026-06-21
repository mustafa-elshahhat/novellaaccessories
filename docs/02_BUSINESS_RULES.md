# Business Rules — Novella Accessories

## 1. Customer Accounts

- Customer accounts use phone number + password.
- Customer email is not required.
- Phone number must be unique.
- Phone number must be verified using WhatsApp OTP during account creation.
- Login uses phone number and password.
- Forgot password uses WhatsApp OTP.
- Changing a phone number requires WhatsApp OTP verification on the new phone number.
- The new phone number must not replace the existing phone number until verification succeeds.
- Admin must never see plain-text passwords.

## 2. OTP Rules

- OTP codes are **generated and verified by `apps/api`.** The WhatsApp sidecar (`apps/whatsapp`) never generates or verifies OTPs — it only delivers the rendered message.
- OTP messages are delivered through `apps/whatsapp` (Baileys / WhatsApp Web); `apps/api` calls its `POST /send-message` endpoint with the final text.
- OTP must have an expiry time.
- OTP resend must have a cooldown.
- OTP attempts must be limited.
- Failed attempts should lock the OTP flow temporarily after repeated failures.
- OTP purpose must be stored: register, reset_password, change_phone.
- Used OTP codes must not be reusable.
- OTP codes are stored hashed (not plain text) and must never be logged in plain text by either service.

## 3. Product Availability

- Customer must not see exact stock quantity.
- Customer sees only available or unavailable.
- Product availability is based on active variants with stock greater than zero.
- Variant availability is based on its own stock.
- Inactive products and inactive variants are not purchasable.
- Out-of-stock products may remain visible, but the add-to-cart action must be disabled.

## 4. Product Variants

- Products can have variants.
- Variants may represent size, color, material, or a combination.
- Variant stock is independent.
- Variant price can optionally override the product base price.
- Variant purchase price can optionally override the product purchase cost.
- If no override exists, product base price/cost is used.

## 5. Price Authority

- Frontend prices are display-only.
- Backend is the only pricing authority.
- Cart, checkout, coupon validation, shipping, and final order totals must be recalculated by backend.
- Order item snapshots must store the final prices used at order time.

## 6. Product Discount Rules

- Product discount is internal, not a coupon code.
- Product discount is configured by admin with percentage, start date, and end date.
- Product discount applies only during its active date range.
- Product discount is applied before coupon discount.
- Storefront must show original price and discounted price when active.
- Inactive or expired product discounts must not affect the final price.

## 7. General Coupon Rules

- Coupon can be percentage-based or fixed-amount.
- Coupon must have active status.
- Coupon must respect start and end dates.
- Coupon must respect total usage limit.
- Coupon must respect per-customer usage limit.
- Coupon must respect minimum order subtotal.
- Coupon applies after product discounts.
- Coupon applies to product subtotal only, not shipping, unless a future setting explicitly allows shipping discounts.
- Coupon usage must be recorded per order and per customer.

## 8. Two-Delivered-Orders Coupon Rules

- Only Delivered orders count.
- Pending, Confirmed, Preparing, Shipped, and Cancelled orders do not count for generation eligibility.
- Coupon is generated after the customer's second Delivered order.
- Coupon is tied to that exact customer.
- Coupon can be used once.
- Coupon cannot be used by another customer.
- Coupon percentage and validity duration are controlled by admin.
- Coupon is sent through WhatsApp.
- Success/failure must be logged.
- In MVP, each customer receives this reward only once unless admin later enables repeat cycles.

## 9. Discount Stacking

Allowed stacking:

1. Product discount.
2. Coupon discount.

Example:

```text
Original product price: 1000
Product discount 20%: 800
Coupon 10%: 720 final price
```

Required snapshots:

- Original unit selling price.
- Product discount percentage.
- Product discount amount.
- Coupon code.
- Coupon discount amount.
- Final unit price.
- Purchase cost.

## 10. Cart Rules

- Cart can contain only purchasable active variants.
- Cart quantity cannot exceed available stock.
- Cart prices must be refreshed during checkout.
- Cart items that become unavailable must be blocked at checkout.

## 11. Order Status Rules

Statuses:

- Pending.
- Confirmed.
- Preparing.
- Shipped.
- Delivered.
- Cancelled.

Customer cancellation:

- Allowed when order is Pending.
- Allowed when order is Confirmed.
- Not allowed when order is Preparing, Shipped, Delivered, or Cancelled.

Admin status movement:

- Admin can move orders forward.
- Admin can cancel before delivery when business logic allows it.
- Delivered and Cancelled are terminal for normal MVP workflows.

## 12. Stock Rules

- Stock is deducted when order status becomes Confirmed.
- Stock is not deducted when order is Pending.
- If a Confirmed order is cancelled before delivery, stock must be restored.
- Delivered orders do not restore stock.
- Stock movements should be logged for auditing.

## 13. Shipping Rules

Each governorate stores:

- Customer-paid shipping fee.
- Actual shipping cost.

At checkout:

- Customer selects governorate.
- Backend applies customer-paid shipping fee.
- Actual shipping cost is stored for reporting.

Profit reporting:

- Shipping margin = customer-paid shipping fee - actual shipping cost.
- Shipping actual cost is not a general expense when it is already tied to the governorate/order.

## 14. Payment Rules

Supported payment methods in the system:

- Cash on Delivery.
- Bank card.
- Instapay.
- Electronic wallets.

MVP:

- Cash on Delivery works first.
- Other methods are prepared for payment-gateway activation.
- Provider support must be verified before activating bank card, Instapay, and wallets.

Payment status examples:

- Pending.
- Authorized.
- Paid.
- Failed.
- Refunded.
- Cancelled.

## 15. Expenses Rules

General expenses include:

- Packaging.
- Ads.
- Payment gateway commissions.
- Operating expenses.
- Other.

Shipping actual cost is handled in shipping fee management and should not be duplicated as a general expense for the same order.

## 16. Profit Rules

Use Delivered orders for final profit reports.

Revenue:

```text
Product revenue after product discounts and coupon allocations
```

Product cost:

```text
Purchase cost snapshot × quantity
```

Gross profit:

```text
Product revenue - product cost
```

Shipping margin:

```text
Customer-paid shipping - actual shipping cost
```

Net profit:

```text
Gross profit + shipping margin - operating expenses - payment commissions
```

## 17. Customer Reminders

- Reminders apply only to registered customers.
- Abandoned checkout reminders are sent when a customer starts checkout or has cart activity but does not complete an order within admin-defined duration.
- Inactive customer reminders are sent when a customer does not visit the website within admin-defined duration.
- Each reminder must be sent once per event/absence cycle.
- Reminder logs must prevent repeated spam.

## 18. Returns and Exchanges

- MVP return/exchange flow is handled through WhatsApp.
- No internal return-request module is required in MVP.
- Policy pages must clearly explain return and exchange rules.
- Future return modules should affect stock and reports if implemented later.

## 19. Admin Rules

- One admin account is enough for MVP.
- Admin can manage all content and operations.
- Purchase cost is visible only to admin.
- Purchase cost must never be exposed to customer APIs.

## 20. SEO/AEO/GEO Rules

- Every product, category, and static page should have localized SEO metadata.
- Arabic pages use RTL and Arabic slugs when appropriate.
- English pages use LTR and English slugs.
- Canonical and hreflang tags must be correct.
- Structured data must be generated from backend/storefront models.
