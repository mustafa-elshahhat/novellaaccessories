# Storefront UX Plan — Novella Accessories

## 1. UX Goal

The storefront must feel premium, soft, feminine, simple, and fast. It should match the supplied Novella visual references: warm ivory backgrounds, champagne/rose-gold accents, refined typography, delicate borders, subtle palm/leaf shadows, and a calm jewelry-brand tone.

The storefront is mobile-first.

## 2. Languages

- Arabic.
- English.

Arabic:

- Full RTL.
- Arabic localized content.
- Arabic slugs where appropriate.

English:

- LTR.
- English localized content.
- English slugs.

## 3. Navigation

### Mobile and Tablet

Use bottom navigation bar, not sidebar.

Suggested bottom tabs:

- Home.
- Categories.
- Cart.
- Orders/Account.
- Menu.

### Desktop

Use elegant header navigation:

- Logo.
- Home.
- Categories.
- Offers.
- About.
- Contact.
- Cart.
- Account.
- Language switcher.

## 4. Home Page

Home sections:

1. Hero section managed by admin.
2. Featured categories.
3. Discounted products.
4. Featured products.
5. Brand story block.
6. Trust blocks: shipping, secure checkout, WhatsApp support.
7. SEO/AEO content block.
8. FAQ preview.

Hero:

- Uses admin-managed image.
- Promotes discounted products when applicable.
- Supports Arabic and English title/subtitle/CTA.
- Must match brand colors and spacing.

## 5. Category Page

Category page includes:

- Category title.
- Category description.
- Product grid.
- Sort options.
- Availability filter.
- Price filter if needed.
- SEO/AEO guidance content at bottom.

## 6. Product Listing Card

Product card shows:

- Product image.
- Product name.
- Available/unavailable status.
- Original price if discounted.
- Discounted price.
- Product discount badge.
- Add to cart or view details.

Product card must not show exact stock quantity.

## 7. Product Details Page

Product details include:

- Image gallery.
- Product name.
- Price.
- Original price when product discount is active.
- Discount badge.
- Variant selector.
- Availability.
- Quantity selector.
- Add to cart.
- Description.
- Care/material notes if available.
- Shipping/return short note.
- Related products.
- FAQ/answer block for AEO.

Variant selector:

- Shows options like size, color, material.
- Disables unavailable variants.
- Does not show stock count.

## 8. Cart Page

Cart shows:

- Product image.
- Product name.
- Variant details.
- Quantity.
- Unit price.
- Product discount if active.
- Line total.
- Remove item.
- Coupon entry field.
- Summary.

Cart must call backend reprice before checkout.

## 9. Checkout Page

Checkout requires logged-in customer.

Fields:

- Name.
- Governorate.
- City/district.
- Detailed address.
- Notes.

Phone number comes from verified customer account.

Checkout summary shows:

- Product subtotal before discount.
- Product discount total.
- Coupon discount.
- Shipping fee.
- Grand total.
- Payment method.

Payment methods displayed:

- Cash on Delivery.
- Bank card.
- Instapay.
- Electronic wallets.

Methods not active yet should appear only if admin enables them or should show as coming soon depending on implementation decision.

## 10. Account Pages

Customer account includes:

- Profile.
- Change password.
- Change phone number with WhatsApp OTP.
- My orders.
- Order details.

## 11. Authentication UX

Register:

- Name.
- Phone.
- Password.
- Confirm password.
- WhatsApp OTP verification.

Login:

- Phone.
- Password.

Forgot password:

- Phone.
- WhatsApp OTP.
- New password.

Change phone:

- New phone.
- WhatsApp OTP.

## 12. Order UX

My orders page shows:

- Order number.
- Date.
- Status.
- Grand total.
- Payment method.
- Cancel button only when status is Pending or Confirmed.

Order details show:

- Items.
- Variants.
- Address.
- Shipping fee.
- Discounts.
- Current status.
- Tracking number if available.

## 13. Static Pages

Required pages:

- About Us.
- Contact Us.
- Privacy Policy.
- Terms and Conditions.
- Return and Exchange Policy.
- Shipping and Delivery Policy.
- FAQ.

Returns and exchange page must direct users to WhatsApp.

## 14. Accessibility

- Maintain readable contrast.
- Buttons must have clear labels.
- Inputs must have labels.
- Keyboard navigation should work.
- Use alt text for images.
- Loading and error states must be clear.

## 15. Performance

- Optimize images through Cloudinary transformations and Next.js image handling.
- Use server-rendered metadata.
- Avoid heavy animations.
- Keep mobile fast.
- Lazy-load below-the-fold sections.

## 16. Empty States

Empty cart:

- Show a soft message and CTA to categories.

No products:

- Show category-specific empty message.

Unavailable product:

- Show unavailable status and related products.

## 17. Error States

Common errors:

- Invalid login.
- OTP expired.
- Product no longer available.
- Coupon expired.
- Quantity unavailable.
- Payment method inactive.
- Shipping governorate unavailable.

Errors should be clear, concise, and localized.
