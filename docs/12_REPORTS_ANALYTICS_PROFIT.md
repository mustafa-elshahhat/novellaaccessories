# Reports, Analytics & Profit — Novella Accessories

## 1. Reporting Goal

Admin needs accurate operational visibility:

- Sales.
- Orders.
- Profit.
- Expenses.
- Shipping margin.
- Products performance.
- Coupon usage.
- Payment methods.
- Real conversion rates.

Reports should prefer Delivered orders for final revenue/profit.

## 2. Date Filters

All reports should support:

- Today.
- This week.
- This month.
- Custom date range.

## 3. Sales Metrics

- Total orders.
- Pending orders.
- Confirmed orders.
- Preparing orders.
- Shipped orders.
- Delivered orders.
- Cancelled orders.
- Total product revenue.
- Grand total collected/expected.

## 4. Discount Metrics

- Product discount total.
- Coupon discount total.
- Total discount amount.
- Most used coupons.
- Coupon revenue impact.
- Two-delivered-orders coupon usage.

## 5. Shipping Metrics

- Customer-paid shipping total.
- Actual shipping cost total.
- Shipping margin.
- Orders by governorate.
- Highest revenue governorates.
- Highest shipping-margin governorates.

Formula:

```text
Shipping margin = customer-paid shipping - actual shipping cost
```

## 6. Product Cost Metrics

- Product purchase cost total.
- Cost per product.
- Cost per category.
- Cost per order.

Use order item purchase-cost snapshots, not current product cost.

## 7. Gross Profit

Formula:

```text
Gross profit = product revenue after discounts - product purchase cost
```

Where:

```text
Product revenue after discounts = final item prices after product discounts and coupon allocation
```

## 8. Net Profit

Formula:

```text
Net profit = gross profit + shipping margin - expenses - payment commissions
```

Expenses include:

- Packaging.
- Ads.
- Payment gateway commissions if entered as expense.
- Operating expenses.
- Other expenses.

Avoid double-counting payment commissions if already stored in payment transactions and also entered as expenses.

## 9. Product Reports

- Best-selling products by quantity.
- Best-selling products by revenue.
- Most profitable products.
- Low-stock variants.
- Out-of-stock variants.
- Discounted product performance.

## 10. Category Reports

- Best-selling categories.
- Category revenue.
- Category gross profit.
- Category net contribution if expense allocation is later supported.

## 11. Customer Reports

- New customers.
- Returning customers.
- Customers with two Delivered orders.
- Customers who received reward coupon.
- Customers who used reward coupon.
- Inactive customers.

## 12. Payment Reports

- Orders by payment method.
- Revenue by payment method.
- Failed payment attempts.
- Payment gateway commissions.
- COD orders.
- Card/Instapay/wallet readiness when activated.

## 13. Expense Reports

- Expenses by category.
- Expenses by date.
- Ad spend.
- Packaging spend.
- Operating spend.
- Related order/campaign when available.

## 14. Analytics Goal

The admin needs a real page showing:

- Visit volume.
- Actual conversion rate.
- Visits that purchased.
- Checkout conversion.
- Delivered-order conversion.

## 15. First-Party Analytics Tracking

Track:

- Anonymous visitor ID.
- Session ID.
- Customer ID after login.
- Referrer.
- UTM source.
- UTM medium.
- UTM campaign.
- Device type.
- Language.
- Landing page.
- Page views.
- Product views.
- Add to cart.
- Checkout started.
- Order placed.
- Delivered order association.

## 16. Conversion Metrics

Visit-to-order conversion:

```text
Sessions with placed order / total sessions
```

Visitor-to-delivered conversion:

```text
Unique visitors with Delivered order / total unique visitors
```

Checkout-to-order conversion:

```text
Sessions with order placed / sessions with checkout started
```

Order-to-delivered conversion:

```text
Delivered orders / placed orders
```

Use both session-based and visitor-based metrics when possible.

## 17. Source Reports

Show conversion by:

- UTM source.
- UTM medium.
- UTM campaign.
- Referrer.
- Device type.
- Language.

## 18. Admin Analytics Widgets

Suggested widgets:

- Visits today.
- Unique visitors today.
- Orders today.
- Delivered orders this month.
- Conversion rate this month.
- Top traffic source.
- Top converting product.
- Abandoned checkout count.

## 19. Accuracy Rules

- Use Delivered orders for final profit.
- Use order snapshots for historical prices and costs.
- Do not use current product prices to calculate old order profit.
- Do not count Cancelled orders as revenue.
- Keep analytics separate from external tools; first-party data is the source of truth for dashboard conversion rates.

## 20. Future Enhancements

- Export reports to Excel.
- Email/WhatsApp daily report to admin.
- Campaign ROI if ad spend is tied to UTM campaigns.
- Cohort retention.
- Customer lifetime value.
