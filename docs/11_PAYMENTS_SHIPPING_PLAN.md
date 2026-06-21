# Payments & Shipping Plan — Novella Accessories

## 1. Payment Goal

The MVP must support Cash on Delivery immediately and prepare the system for Egyptian payment gateway integration.

Required payment methods in the system:

- Cash on Delivery.
- Bank card.
- Instapay.
- Electronic wallets.

## 2. Payment Provider Strategy

Do not hardcode business logic to one provider.

Use provider abstraction:

```text
IPaymentProvider
- initiatePayment(order)
- handleCallback(payload)
- getPaymentStatus(reference)
```

Potential providers to evaluate:

- Paymob.
- Fawry.
- Geidea.
- Kashier.
- Other Egyptian-compatible providers.

Final provider choice must be verified with the provider contract/dashboard before activation.

## 3. MVP Payment Behavior

Cash on Delivery:

- Active from day one.
- Payment status can remain Pending/Unpaid until delivery if needed.

Bank card, Instapay, wallets:

- Present in system design.
- Can be hidden, disabled, or marked as not active until provider is configured.
- Schema must support them.
- UI should be ready for activation.

## 4. Instapay and Wallets Note

Instapay and electronic wallet support depends on the selected payment provider and agreement. The system should be prepared for them, but production activation must happen only after provider verification.

## 5. Payment Statuses

Suggested statuses:

- Pending.
- Authorized.
- Paid.
- Failed.
- Cancelled.
- Refunded.

## 6. Payment Transaction Storage

Store:

- Order ID.
- Payment method.
- Provider name.
- Status.
- Amount.
- Provider transaction reference.
- Provider response.
- Commission amount if known.
- Created/updated timestamps.

## 7. Payment Commissions

Payment gateway commissions should be tracked either:

- Automatically from provider data if available.
- Manually as expenses if not available.

For reports, commissions reduce net profit.

## 8. Shipping Goal

Shipping must be managed by Egyptian governorate.

Each governorate stores:

- Arabic name.
- English name.
- Customer-paid shipping fee.
- Actual shipping cost.
- Active/inactive.
- Sort order.

## 9. Shipping Calculation

At checkout:

1. Customer selects governorate.
2. Backend validates governorate is active.
3. Backend applies customer-paid shipping fee.
4. Backend stores actual shipping cost snapshot on the order.
5. Backend calculates shipping margin.

Formula:

```text
Shipping margin = customer-paid shipping fee - actual shipping cost
```

## 10. Shipping and Profit

Shipping actual cost is managed on the shipping-fees page, not general expenses, when it is tied to a governorate/order.

Reports should show:

- Shipping collected from customers.
- Actual shipping cost.
- Shipping margin.

## 11. Shipping Company Integration Readiness

Prepare shipping provider abstraction:

```text
IShippingProvider
- createShipment(order)
- getShipmentStatus(trackingNumber)
- cancelShipment(trackingNumber)
```

MVP behavior:

- Manual shipping updates from admin.
- Store provider name.
- Store tracking number.
- Store external shipping status.

Future behavior:

- Create shipment through provider API.
- Auto-fetch status.
- Sync tracking updates.

## 12. Order Shipping Fields

Store snapshots:

- Governorate ID.
- Governorate Arabic name.
- Governorate English name.
- Customer-paid shipping fee.
- Actual shipping cost.
- Shipping margin.
- Provider name.
- Tracking number.
- External status.

## 13. Free Shipping Optional Setting

Optional future setting:

- Enable free shipping threshold.
- Threshold amount.

If enabled:

- Customer-paid shipping fee becomes 0 after threshold.
- Actual shipping cost still applies to profit.

## 14. Admin Shipping UI

Admin can:

- Add governorate.
- Edit governorate.
- Set customer-paid fee.
- Set actual shipping cost.
- Activate/deactivate governorate.
- Sort governorates.

## 15. Checkout UX

Checkout must show:

- Selected governorate.
- Shipping fee customer will pay.
- Grand total.

Do not show actual shipping cost to the customer.
