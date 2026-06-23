# Database Model — SQL Server

## 1. General Notes

- Use SQL Server.
- Monetary values use `decimal(18,2)`.
- Dates use UTC where practical.
- Customer-facing localized content should support Arabic and English.
- Use soft-delete or active/inactive where business history matters.
- Never expose purchase cost through customer APIs.
- This SQL Server model holds **business data only**, including WhatsApp settings, templates, and message logs. WhatsApp Web **session/auth state is NOT stored here** — it lives in a separate external MongoDB managed by `apps/whatsapp` (Baileys). SQL Server never stores WhatsApp session data.

## 2. Core Tables

## Customers

```text
Customers
- Id uniqueidentifier PK
- FullName nvarchar(200) not null
- PhoneNumber nvarchar(30) not null unique
- PhoneNumberNormalized nvarchar(30) not null unique
- PasswordHash nvarchar(max) not null
- IsPhoneVerified bit not null
- LastLoginAt datetime2 null
- LastVisitAt datetime2 null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
- IsActive bit not null
```

## CustomerPhoneChangeRequests

```text
CustomerPhoneChangeRequests
- Id uniqueidentifier PK
- CustomerId uniqueidentifier FK Customers
- OldPhoneNumber nvarchar(30) not null
- NewPhoneNumber nvarchar(30) not null
- NewPhoneNumberNormalized nvarchar(30) not null
- Status nvarchar(30) not null -- Pending, Verified, Cancelled, Expired
- CreatedAt datetime2 not null
- VerifiedAt datetime2 null
```

## AdminUsers

```text
AdminUsers
- Id uniqueidentifier PK
- Username nvarchar(100) not null unique
- PasswordHash nvarchar(max) not null
- DisplayName nvarchar(200) not null
- IsActive bit not null
- CreatedAt datetime2 not null
- LastLoginAt datetime2 null
```

## OtpCodes

```text
OtpCodes
- Id uniqueidentifier PK
- PhoneNumber nvarchar(30) not null
- PhoneNumberNormalized nvarchar(30) not null
- Purpose nvarchar(50) not null -- Register, ResetPassword, ChangePhone
- CodeHash nvarchar(max) not null
- ExpiresAt datetime2 not null
- UsedAt datetime2 null
- AttemptCount int not null
- ResendCount int not null
- LastSentAt datetime2 not null
- LockedUntil datetime2 null
- RelatedCustomerId uniqueidentifier null
- CreatedAt datetime2 not null
```

## Categories

```text
Categories
- Id uniqueidentifier PK
- NameAr nvarchar(200) not null
- NameEn nvarchar(200) not null
- SlugAr nvarchar(220) not null unique
- SlugEn nvarchar(220) not null unique
- ImageUrl nvarchar(max) null
- ImagePublicId nvarchar(300) null
- SortOrder int not null
- IsActive bit not null
- SeoTitleAr nvarchar(300) null
- SeoTitleEn nvarchar(300) null
- SeoDescriptionAr nvarchar(500) null
- SeoDescriptionEn nvarchar(500) null
- AeoSummaryAr nvarchar(max) null
- AeoSummaryEn nvarchar(max) null
- GeoContentAr nvarchar(max) null
- GeoContentEn nvarchar(max) null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## Products

```text
Products
- Id uniqueidentifier PK
- CategoryId uniqueidentifier FK Categories
- NameAr nvarchar(300) not null
- NameEn nvarchar(300) not null
- SlugAr nvarchar(320) not null unique
- SlugEn nvarchar(320) not null unique
- DescriptionAr nvarchar(max) null
- DescriptionEn nvarchar(max) null
- BasePurchasePrice decimal(18,2) not null
- BaseSellingPrice decimal(18,2) not null
- ProductDiscountPercentage decimal(5,2) null
- ProductDiscountStartAt datetime2 null
- ProductDiscountEndAt datetime2 null
- IsFeatured bit not null
- IsActive bit not null
- SeoTitleAr nvarchar(300) null
- SeoTitleEn nvarchar(300) null
- SeoDescriptionAr nvarchar(500) null
- SeoDescriptionEn nvarchar(500) null
- AeoSummaryAr nvarchar(max) null
- AeoSummaryEn nvarchar(max) null
- GeoContentAr nvarchar(max) null
- GeoContentEn nvarchar(max) null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## ProductImages

```text
ProductImages
- Id uniqueidentifier PK
- ProductId uniqueidentifier FK Products
- Url nvarchar(max) not null
- PublicId nvarchar(300) not null
- AltAr nvarchar(300) null
- AltEn nvarchar(300) null
- SortOrder int not null
- IsPrimary bit not null
- CreatedAt datetime2 not null
```

## ProductVariants

```text
ProductVariants
- Id uniqueidentifier PK
- ProductId uniqueidentifier FK Products
- Sku nvarchar(100) not null unique
- NameAr nvarchar(300) null
- NameEn nvarchar(300) null
- Size nvarchar(100) null
- ColorAr nvarchar(100) null
- ColorEn nvarchar(100) null
- MaterialAr nvarchar(100) null
- MaterialEn nvarchar(100) null
- CustomOptionNameAr nvarchar(100) null
- CustomOptionNameEn nvarchar(100) null
- CustomOptionValueAr nvarchar(100) null
- CustomOptionValueEn nvarchar(100) null
- StockQuantity int not null
- PurchasePriceOverride decimal(18,2) null
- SellingPriceOverride decimal(18,2) null
- IsActive bit not null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
- RowVersion rowversion not null -- optimistic concurrency for stock changes
```

## InventoryMovements

```text
InventoryMovements
- Id uniqueidentifier PK
- ProductVariantId uniqueidentifier FK ProductVariants
- OrderId uniqueidentifier null
- MovementType nvarchar(50) not null -- Deduct, Restore, ManualAdjustment
- Quantity int not null
- Reason nvarchar(500) null
- CreatedAt datetime2 not null
- CreatedByAdminId uniqueidentifier null
```

## Carts

```text
Carts
- Id uniqueidentifier PK
- CustomerId uniqueidentifier FK Customers
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## CartItems

```text
CartItems
- Id uniqueidentifier PK
- CartId uniqueidentifier FK Carts
- ProductId uniqueidentifier FK Products
- ProductVariantId uniqueidentifier FK ProductVariants
- Quantity int not null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## Orders

```text
Orders
- Id uniqueidentifier PK
- OrderNumber nvarchar(50) not null unique
- IdempotencyKey nvarchar(128) null -- duplicate submit guard, unique per customer when present
- CustomerId uniqueidentifier FK Customers
- Status nvarchar(50) not null -- Pending, Confirmed, Preparing, Shipped, Delivered, Cancelled
- CustomerName nvarchar(200) not null
- CustomerPhone nvarchar(30) not null
- GovernorateId uniqueidentifier FK ShippingGovernorates
- GovernorateNameAr nvarchar(200) not null
- GovernorateNameEn nvarchar(200) not null
- CityDistrict nvarchar(200) not null
- DetailedAddress nvarchar(max) not null
- Notes nvarchar(max) null
- ProductSubtotalBeforeDiscount decimal(18,2) not null
- ProductDiscountTotal decimal(18,2) not null
- CouponDiscountTotal decimal(18,2) not null
- ProductSubtotalAfterDiscount decimal(18,2) not null
- CustomerPaidShippingFee decimal(18,2) not null
- ActualShippingCost decimal(18,2) not null
- ShippingMargin decimal(18,2) not null
- GrandTotal decimal(18,2) not null
- PaymentMethod nvarchar(50) not null
- PaymentStatus nvarchar(50) not null
- ShippingProviderName nvarchar(100) null
- ExternalTrackingNumber nvarchar(200) null
- ExternalShippingStatus nvarchar(200) null
- ConfirmedAt datetime2 null
- PreparingAt datetime2 null
- ShippedAt datetime2 null
- DeliveredAt datetime2 null
- CancelledAt datetime2 null
- CancellationReason nvarchar(500) null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
- RowVersion rowversion not null -- optimistic concurrency for status/stock transitions
```

## OrderItems

```text
OrderItems
- Id uniqueidentifier PK
- OrderId uniqueidentifier FK Orders
- ProductId uniqueidentifier FK Products
- ProductVariantId uniqueidentifier FK ProductVariants
- ProductNameAr nvarchar(300) not null
- ProductNameEn nvarchar(300) not null
- VariantNameAr nvarchar(300) null
- VariantNameEn nvarchar(300) null
- Sku nvarchar(100) not null
- Quantity int not null
- OriginalUnitSellingPrice decimal(18,2) not null
- ProductDiscountPercentage decimal(5,2) null
- ProductDiscountAmountPerUnit decimal(18,2) not null
- UnitPriceAfterProductDiscount decimal(18,2) not null
- CouponDiscountAmountPerUnit decimal(18,2) not null
- FinalUnitPrice decimal(18,2) not null
- PurchaseCostPerUnit decimal(18,2) not null
- LineRevenue decimal(18,2) not null
- LineCost decimal(18,2) not null
- LineGrossProfit decimal(18,2) not null
```

## Coupons

```text
Coupons
- Id uniqueidentifier PK
- Code nvarchar(100) not null unique
- Type nvarchar(50) not null -- Percentage, FixedAmount
- Value decimal(18,2) not null
- StartAt datetime2 null
- EndAt datetime2 null
- TotalUsageLimit int null
- PerCustomerUsageLimit int null
- MinimumOrderSubtotal decimal(18,2) null
- IsActive bit not null
- IsCustomerSpecific bit not null
- CustomerId uniqueidentifier null FK Customers
- Source nvarchar(50) not null -- General, TwoDeliveredOrders
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## CouponUsages

```text
CouponUsages
- Id uniqueidentifier PK
- CouponId uniqueidentifier FK Coupons
- CustomerId uniqueidentifier FK Customers
- OrderId uniqueidentifier FK Orders
- DiscountAmount decimal(18,2) not null
- UsedAt datetime2 not null
```

## TwoOrderCouponSettings

```text
TwoOrderCouponSettings
- Id uniqueidentifier PK
- IsEnabled bit not null
- DiscountPercentage decimal(5,2) not null
- ValidityDays int not null
- MinimumOrderSubtotal decimal(18,2) null
- SendWhatsAppMessage bit not null
- UpdatedAt datetime2 not null
```

## ShippingGovernorates

```text
ShippingGovernorates
- Id uniqueidentifier PK
- NameAr nvarchar(200) not null
- NameEn nvarchar(200) not null
- CustomerPaidShippingFee decimal(18,2) not null
- ActualShippingCost decimal(18,2) not null
- IsActive bit not null
- SortOrder int not null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## PaymentTransactions

```text
PaymentTransactions
- Id uniqueidentifier PK
- OrderId uniqueidentifier FK Orders
- PaymentMethod nvarchar(50) not null
- ProviderName nvarchar(100) null
- Status nvarchar(50) not null
- Amount decimal(18,2) not null
- ProviderTransactionReference nvarchar(200) null
- ProviderResponse nvarchar(max) null
- CommissionAmount decimal(18,2) null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## Expenses

```text
Expenses
- Id uniqueidentifier PK
- Category nvarchar(100) not null -- Packaging, Ads, PaymentGatewayCommission, Operating, Other
- Amount decimal(18,2) not null
- ExpenseDate date not null
- Notes nvarchar(max) null
- RelatedOrderId uniqueidentifier null
- RelatedCampaignName nvarchar(200) null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## HeroSections

```text
HeroSections
- Id uniqueidentifier PK
- ImageUrl nvarchar(max) not null
- ImagePublicId nvarchar(300) not null
- TitleAr nvarchar(300) not null
- TitleEn nvarchar(300) not null
- SubtitleAr nvarchar(max) null
- SubtitleEn nvarchar(max) null
- CtaTextAr nvarchar(100) null
- CtaTextEn nvarchar(100) null
- CtaLink nvarchar(max) null
- LinkedProductId uniqueidentifier null
- IsActive bit not null
- SortOrder int not null
- CreatedAt datetime2 not null
- UpdatedAt datetime2 null
```

## StaticPages

```text
StaticPages
- Id uniqueidentifier PK
- Key nvarchar(100) not null unique -- about, contact, privacy, terms, returns, shipping, faq
- TitleAr nvarchar(300) not null
- TitleEn nvarchar(300) not null
- SlugAr nvarchar(300) not null unique
- SlugEn nvarchar(300) not null unique
- ContentAr nvarchar(max) not null
- ContentEn nvarchar(max) not null
- SeoTitleAr nvarchar(300) null
- SeoTitleEn nvarchar(300) null
- SeoDescriptionAr nvarchar(500) null
- SeoDescriptionEn nvarchar(500) null
- AeoSummaryAr nvarchar(max) null
- AeoSummaryEn nvarchar(max) null
- GeoContentAr nvarchar(max) null
- GeoContentEn nvarchar(max) null
- IsActive bit not null
- UpdatedAt datetime2 null
```

## WhatsAppSettings

```text
WhatsAppSettings
- Id uniqueidentifier PK
- IsEnabled bit not null
- TransportName nvarchar(100) not null -- WhatsApp transport, default "BaileysWhatsAppWeb"
- TwoOrderCouponTemplate nvarchar(max) null
- AbandonedCheckoutTemplate nvarchar(max) null
- InactiveCustomerTemplate nvarchar(max) null
- UpdatedAt datetime2 not null
```

Notes:

- `TransportName` identifies the WhatsApp transport used by the sidecar. For the MVP this is Baileys / WhatsApp Web (`BaileysWhatsAppWeb`); a future official WhatsApp Business API transport would change this value.
- The sidecar base URL and internal API key are **not** stored in this table — they live only in `apps/api` environment variables (`WhatsApp__BaseUrl`, `WhatsApp__InternalApiKey`). No raw secrets are persisted in the database.
- Registration OTP, password-reset OTP, phone-change OTP, and order-confirmation templates are fixed in code. This table stores only editable business/marketing templates. Baileys auth/session state is stored externally in MongoDB by `apps/whatsapp`, never in SQL Server.

## WhatsAppMessageLogs

```text
WhatsAppMessageLogs
- Id uniqueidentifier PK
- CustomerId uniqueidentifier null FK Customers
- PhoneNumber nvarchar(30) not null
- MessageType nvarchar(100) not null
- TemplateKey nvarchar(100) null
- MessageBody nvarchar(max) null
- Status nvarchar(50) not null -- Pending, Sent, Failed
- FailureReason nvarchar(max) null
- RetryCount int not null
- SentAt datetime2 null
- CreatedAt datetime2 not null
```

## ReminderSettings

```text
ReminderSettings
- Id uniqueidentifier PK
- AbandonedCheckoutEnabled bit not null
- AbandonedCheckoutDelayHours int not null
- InactiveCustomerEnabled bit not null
- InactiveCustomerDelayDays int not null
- UpdatedAt datetime2 not null
```

## ReminderLogs

```text
ReminderLogs
- Id uniqueidentifier PK
- CustomerId uniqueidentifier FK Customers
- ReminderType nvarchar(100) not null -- AbandonedCheckout, InactiveCustomer
- RelatedCartId uniqueidentifier null
- RelatedVisitSessionId uniqueidentifier null
- Status nvarchar(50) not null -- Sent, Failed, Skipped
- WhatsAppMessageLogId uniqueidentifier null
- SentAt datetime2 null
- CreatedAt datetime2 not null
```

## AnalyticsVisitors

```text
AnalyticsVisitors
- Id uniqueidentifier PK
- AnonymousId nvarchar(100) not null unique
- CustomerId uniqueidentifier null FK Customers
- FirstSeenAt datetime2 not null
- LastSeenAt datetime2 not null
```

## AnalyticsSessions

```text
AnalyticsSessions
- Id uniqueidentifier PK
- VisitorId uniqueidentifier FK AnalyticsVisitors
- CustomerId uniqueidentifier null FK Customers
- StartedAt datetime2 not null
- LastActivityAt datetime2 not null
- LandingPage nvarchar(max) null
- Referrer nvarchar(max) null
- UtmSource nvarchar(200) null
- UtmMedium nvarchar(200) null
- UtmCampaign nvarchar(200) null
- DeviceType nvarchar(50) null
- Language nvarchar(20) null
- ConvertedOrderId uniqueidentifier null
```

## AnalyticsEvents

```text
AnalyticsEvents
- Id uniqueidentifier PK
- SessionId uniqueidentifier FK AnalyticsSessions
- VisitorId uniqueidentifier FK AnalyticsVisitors
- CustomerId uniqueidentifier null FK Customers
- EventType nvarchar(100) not null -- PageView, ProductView, AddToCart, CheckoutStarted, OrderPlaced
- PageUrl nvarchar(max) null
- ProductId uniqueidentifier null
- OrderId uniqueidentifier null
- MetadataJson nvarchar(2048) null -- server-sanitized, PII keys stripped
- CreatedAt datetime2 not null
```

## ShippingSettings

```text
ShippingSettings
- Id uniqueidentifier PK
- FreeShippingThreshold decimal(18,2) null
- IsFreeShippingEnabled bit not null
- UpdatedAt datetime2 not null
```

Brand/domain/default SEO fallback values are environment/code-managed for the single-brand store, not database-managed admin settings.

## 3. Suggested Indexes

- Customers.PhoneNumberNormalized unique.
- Products.SlugAr unique.
- Products.SlugEn unique.
- ProductVariants.Sku unique.
- ProductVariants.RowVersion optimistic concurrency token.
- Orders.RowVersion optimistic concurrency token.
- Orders.CustomerId + IdempotencyKey unique filtered where IdempotencyKey is not null.
- Orders.CustomerId + CreatedAt.
- Orders.Status + CreatedAt.
- Orders.DeliveredAt.
- Coupons.CustomerId + Source unique filtered for Source = TwoDeliveredOrders.
- CouponUsages.CouponId.
- CouponUsages.CustomerId.
- AnalyticsSessions.StartedAt.
- AnalyticsSessions.UtmSource.
- AnalyticsEvents.EventType + CreatedAt.
- WhatsAppMessageLogs.Status + CreatedAt.

## 4. Seed Data

Seed:

- Admin user.
- Default categories: Rings, Necklaces, Earrings, Bracelets.
- Egyptian governorates with customer-paid shipping fee and actual cost placeholders.
- Static pages keys.
- Default WhatsApp settings disabled.
- Default reminder settings disabled.
- Default site settings.
