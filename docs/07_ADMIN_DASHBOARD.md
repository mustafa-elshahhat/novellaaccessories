# Admin Dashboard Plan — Novella Accessories

## 1. Goal

The admin dashboard must be practical, fast, and clear for one admin in the MVP. It should manage the full store without complex permissions.

## 2. Main Sections

- Dashboard overview.
- Products.
- Product variants.
- Categories.
- Orders.
- Customers.
- Coupons.
- Two-delivered-orders coupon settings.
- Shipping fees.
- Hero management.
- WhatsApp settings.
- WhatsApp message logs.
- Payments settings/readiness.
- Expenses.
- Reports.
- Analytics.
- Static pages.
- SEO/AEO/GEO content.
- Site settings.

## 3. Dashboard Overview

Widgets:

- Today's orders.
- Today's revenue.
- Delivered orders this month.
- Pending orders.
- Low-stock variants.
- Failed WhatsApp messages.
- Conversion rate.
- Net profit this month.

Recent activity:

- Recent orders.
- Recent failed messages.
- Recent product stock changes.

## 4. Products Page

Product table columns:

- Image.
- Name.
- Category.
- Base selling price.
- Base purchase price.
- Active discount.
- Availability.
- Active/inactive.
- Featured.
- Actions.

Product form:

- Arabic name.
- English name.
- Arabic description.
- English description.
- Category.
- Base purchase price.
- Base selling price.
- Images.
- Featured flag.
- Active status.
- Product discount percentage.
- Discount start/end.
- SEO/AEO/GEO fields.

## 5. Product Variants

Variant management should be available inside product details.

Variant fields:

- SKU.
- Arabic/English display name.
- Size.
- Color Arabic/English.
- Material Arabic/English.
- Custom option.
- Stock quantity.
- Purchase price override.
- Selling price override.
- Active/inactive.

Variant actions:

- Add.
- Edit.
- Deactivate.
- Adjust stock.
- View inventory movements.

## 6. Categories Page

Category fields:

- Arabic name.
- English name.
- Arabic slug.
- English slug.
- Image.
- Sort order.
- Active/inactive.
- SEO/AEO/GEO fields.

Actions:

- Add.
- Edit.
- Deactivate/delete.
- Reorder.

## 7. Orders Page

Order table columns:

- Order number.
- Customer.
- Phone.
- Governorate.
- Status.
- Payment method.
- Payment status.
- Grand total.
- Created date.
- Actions.

Order details:

- Customer details.
- Address.
- Items and variants.
- Pricing snapshot.
- Product discounts.
- Coupon discounts.
- Customer-paid shipping.
- Actual shipping cost.
- Shipping margin.
- Payment details.
- Status timeline.
- WhatsApp message history if related.

Status actions:

- Confirm.
- Mark preparing.
- Mark shipped.
- Add tracking number.
- Mark delivered.
- Cancel when allowed.

## 8. Customers Page

Customer table:

- Name.
- Phone.
- Verified status.
- Total orders.
- Delivered orders.
- Last visit.
- Last order.
- Created date.

Customer details:

- Profile.
- Orders.
- Coupons.
- Reminder logs.
- WhatsApp messages.
- Analytics sessions.

## 9. Coupons Page

General coupons:

- Code.
- Type.
- Value.
- Start/end.
- Usage limit.
- Per-customer limit.
- Minimum subtotal.
- Active status.

Coupon details:

- Usage count.
- Customers who used it.
- Orders.
- Total discount amount.

Two-delivered-orders coupon settings:

- Enable/disable.
- Discount percentage.
- Validity days.
- Minimum subtotal.
- Message template.

## 10. Shipping Fees Page

Manage Egyptian governorates.

Fields:

- Arabic name.
- English name.
- Customer-paid shipping fee.
- Actual shipping cost.
- Active/inactive.
- Sort order.

Important:

- Customer-paid fee is what the customer pays.
- Actual cost is what the business pays.
- Shipping margin is used in reporting.

## 11. Hero Management

Hero fields:

- Image.
- Arabic title.
- English title.
- Arabic subtitle.
- English subtitle.
- Arabic CTA.
- English CTA.
- CTA link.
- Linked product.
- Active/inactive.
- Sort order.

Hero preview should show how it appears on mobile and desktop.

## 12. WhatsApp Settings

All WhatsApp admin screens talk to `apps/api` only. The admin never calls the `apps/whatsapp`
sidecar directly and never sees raw secrets.

Settings:

- Enable/disable WhatsApp sending.
- Transport (Baileys / WhatsApp Web) — informational.
- Service base URL of the `apps/whatsapp` sidecar.
- Internal API key status (configured / not configured) — **status only, never the value**.
- Connection/session status (connected, needs pairing) — proxied from the sidecar's `/status` via `apps/api`.
- Message templates.

Templates:

- OTP.
- Order confirmation.
- Two-order coupon.
- Abandoned checkout reminder.
- Inactive customer reminder.

## 13. WhatsApp Logs

Message log columns:

- Phone.
- Customer.
- Message type.
- Status.
- Failure reason.
- Retry count.
- Created date.
- Sent date.
- Actions.

Actions:

- View body.
- Retry failed message.
- Filter by status/type/date.

## 14. Payments Settings

Payment methods:

- Cash on Delivery.
- Bank card.
- Instapay.
- Electronic wallets.

Admin should control active/inactive status where appropriate.

Provider settings:

- Provider name.
- Environment.
- Public key status.
- Secret key status.
- Webhook URL display.

Do not expose raw secrets in UI.

## 15. Expenses Page

Expense fields:

- Category.
- Amount.
- Date.
- Notes.
- Related order optional.
- Related campaign optional.

Categories:

- Packaging.
- Ads.
- Payment gateway commissions.
- Operating.
- Other.

## 16. Reports Page

Reports:

- Sales.
- Profit.
- Products.
- Categories.
- Coupons.
- Payments.
- Governorates.
- Expenses.

Filters:

- Today.
- This week.
- This month.
- Custom range.

## 17. Analytics Page

Analytics sections:

- Visits.
- Unique visitors.
- Sessions.
- Product views.
- Add-to-cart events.
- Checkout started.
- Orders.
- Delivered orders.
- Visit-to-purchase conversion.
- Checkout-to-order conversion.
- Traffic sources.
- Device breakdown.
- Language breakdown.

## 18. Static Pages and SEO

Admin edits:

- About Us.
- Contact Us.
- Privacy Policy.
- Terms and Conditions.
- Return and Exchange Policy.
- Shipping and Delivery Policy.
- FAQ.

Each page includes:

- Arabic title/content.
- English title/content.
- SEO title/description.
- AEO summary.
- GEO content.

## 19. Settings

Settings:

- Site name.
- Domain.
- Default SEO metadata.
- Free shipping threshold optional.
- Reminder durations.
- Language settings.
- Contact/WhatsApp links.

## 20. Admin UX Requirements

- Keep tables simple and searchable.
- Add filters for order status, date, category, active status.
- Show clear validation messages.
- Use confirmation dialogs for destructive actions.
- Use safe defaults.
- Keep purchase-price fields visible only in admin.
