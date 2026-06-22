/**
 * Customer-safe TypeScript mirrors of the Novella backend DTOs.
 *
 * SECURITY: These types intentionally contain ONLY fields the backend exposes to customers.
 * They must never include cost/profit/stock/secret fields such as basePurchasePrice,
 * purchaseCostPerUnit, lineCost, grossProfit, netProfit, actualShippingCost, shippingMargin,
 * stockQuantity, providerResponse, passwordHash, codeHash, etc. (enforced by a leakage test).
 *
 * Backend wire format: camelCase properties; enums are PascalCase member-name strings.
 */

export type OrderStatus =
  | "Pending"
  | "Confirmed"
  | "Preparing"
  | "Shipped"
  | "Delivered"
  | "Cancelled";

export type PaymentMethod = "CashOnDelivery" | "BankCard" | "Instapay" | "Wallet";

export type PaymentStatus =
  | "Pending"
  | "Authorized"
  | "Paid"
  | "Failed"
  | "Cancelled"
  | "Refunded";

export type AnalyticsEventType =
  | "PageView"
  | "ProductView"
  | "AddToCart"
  | "CheckoutStarted"
  | "OrderPlaced";

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PublicCategory {
  id: string;
  nameAr: string;
  nameEn: string;
  slugAr: string;
  slugEn: string;
  imageUrl: string | null;
  sortOrder: number;
  seoTitleAr?: string | null;
  seoTitleEn?: string | null;
  seoDescriptionAr?: string | null;
  seoDescriptionEn?: string | null;
}

export interface PublicProductListItem {
  id: string;
  nameAr: string;
  nameEn: string;
  slugAr: string;
  slugEn: string;
  originalPrice: number;
  finalPrice: number;
  hasDiscount: boolean;
  discountPercentage: number | null;
  isAvailable: boolean;
  isFeatured: boolean;
  primaryImageUrl: string | null;
}

export interface PublicProductImage {
  id: string;
  url: string;
  altAr: string | null;
  altEn: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface PublicProductVariant {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  size: string | null;
  colorAr: string | null;
  colorEn: string | null;
  materialAr: string | null;
  materialEn: string | null;
  customOptionNameAr: string | null;
  customOptionNameEn: string | null;
  customOptionValueAr: string | null;
  customOptionValueEn: string | null;
  originalPrice: number;
  finalPrice: number;
  isAvailable: boolean;
}

export interface PublicProduct {
  id: string;
  categoryId: string;
  nameAr: string;
  nameEn: string;
  slugAr: string;
  slugEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  originalPrice: number;
  finalPrice: number;
  hasDiscount: boolean;
  discountPercentage: number | null;
  isAvailable: boolean;
  isFeatured: boolean;
  images: PublicProductImage[];
  variants: PublicProductVariant[];
  seoTitleAr: string | null;
  seoTitleEn: string | null;
  seoDescriptionAr: string | null;
  seoDescriptionEn: string | null;
  aeoSummaryAr: string | null;
  aeoSummaryEn: string | null;
  geoContentAr: string | null;
  geoContentEn: string | null;
}

export interface SiteSettings {
  siteNameAr: string;
  siteNameEn: string;
  domain: string;
  defaultSeoTitleAr: string | null;
  defaultSeoTitleEn: string | null;
  defaultSeoDescriptionAr: string | null;
  defaultSeoDescriptionEn: string | null;
  freeShippingThreshold: number | null;
  isFreeShippingEnabled: boolean;
}

export interface Hero {
  id: string;
  imageUrl: string;
  imagePublicId: string;
  titleAr: string;
  titleEn: string;
  subtitleAr: string | null;
  subtitleEn: string | null;
  ctaTextAr: string | null;
  ctaTextEn: string | null;
  ctaLink: string | null;
  linkedProductId: string | null;
  isActive: boolean;
  sortOrder: number;
}

export interface Home {
  siteSettings: SiteSettings;
  heroes: Hero[];
  categories: PublicCategory[];
  featuredProducts: PublicProductListItem[];
}

export interface StaticPage {
  id: string;
  key: string;
  titleAr: string;
  titleEn: string;
  slugAr: string;
  slugEn: string;
  contentAr: string;
  contentEn: string;
  seoTitleAr: string | null;
  seoTitleEn: string | null;
  seoDescriptionAr: string | null;
  seoDescriptionEn: string | null;
  aeoSummaryAr: string | null;
  aeoSummaryEn: string | null;
  geoContentAr: string | null;
  geoContentEn: string | null;
  isActive: boolean;
}

export interface PublicGovernorate {
  id: string;
  nameAr: string;
  nameEn: string;
  shippingFee: number;
  sortOrder: number;
}

export interface PaymentMethodInfo {
  method: PaymentMethod;
  providerName: string;
  isActive: boolean;
}

export interface CartItem {
  itemId: string;
  productId: string;
  productVariantId: string;
  productNameAr: string;
  productNameEn: string;
  productSlugAr: string;
  productSlugEn: string;
  primaryImageUrl: string | null;
  primaryImageAltAr: string | null;
  primaryImageAltEn: string | null;
  variantNameAr: string | null;
  variantNameEn: string | null;
  sku: string;
  quantity: number;
  originalUnitPrice: number;
  unitPrice: number;
  lineTotal: number;
  isAvailable: boolean;
  quantityAdjusted: boolean;
}

export interface Cart {
  id: string;
  items: CartItem[];
  productSubtotalBeforeDiscount: number;
  productDiscountTotal: number;
  subtotalAfterProductDiscount: number;
  hasUnavailableItems: boolean;
}

export interface CheckoutLine {
  productVariantId: string;
  productNameAr: string;
  productNameEn: string;
  sku: string;
  quantity: number;
  originalUnitPrice: number;
  finalUnitPrice: number;
  lineTotal: number;
}

export interface CheckoutPreview {
  items: CheckoutLine[];
  productSubtotalBeforeDiscount: number;
  productDiscountTotal: number;
  couponDiscountTotal: number;
  productSubtotalAfterDiscount: number;
  shippingFee: number;
  grandTotal: number;
  appliedCouponCode: string | null;
  couponApplied: boolean;
  warnings: string[];
}

export interface CustomerOrderItem {
  productNameAr: string;
  productNameEn: string;
  variantNameAr: string | null;
  variantNameEn: string | null;
  sku: string;
  quantity: number;
  originalUnitPrice: number;
  finalUnitPrice: number;
  lineTotal: number;
}

export interface CustomerOrder {
  orderNumber: string;
  status: OrderStatus;
  customerName: string;
  customerPhone: string;
  governorateNameAr: string;
  governorateNameEn: string;
  cityDistrict: string;
  detailedAddress: string;
  notes: string | null;
  productSubtotalBeforeDiscount: number;
  productDiscountTotal: number;
  couponDiscountTotal: number;
  productSubtotalAfterDiscount: number;
  shippingFee: number;
  grandTotal: number;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  couponCode: string | null;
  createdAt: string;
  confirmedAt: string | null;
  preparingAt: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  trackingNumber: string | null;
  items: CustomerOrderItem[];
}

export interface CustomerProfile {
  id: string;
  fullName: string;
  phoneNumber: string;
  isPhoneVerified: boolean;
  createdAt: string;
}

/** Backend auth response includes a token; the BFF strips it and only ever returns `customer`. */
export interface AuthTokenResponse {
  token: string;
  customer: CustomerProfile;
}

export interface RegisterResponse {
  requiresVerification: boolean;
  phoneNumber: string;
}

export interface SeoMetadata {
  entityType: string;
  entityId: string;
  slugAr: string;
  slugEn: string;
  seoTitleAr: string | null;
  seoTitleEn: string | null;
  seoDescriptionAr: string | null;
  seoDescriptionEn: string | null;
  aeoSummaryAr: string | null;
  aeoSummaryEn: string | null;
  geoContentAr: string | null;
  geoContentEn: string | null;
}

export interface ProductSeo {
  meta: SeoMetadata;
  product: PublicProduct;
}

export interface SitemapEntry {
  type: string;
  slugAr: string;
  slugEn: string;
  lastModified: string;
  indexable: boolean;
}

export interface SitemapData {
  domain: string;
  entries: SitemapEntry[];
}

// ---- Request payloads (customer-facing) ----

export interface ProductListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  categorySlug?: string;
  featured?: boolean;
  hasDiscount?: boolean;
}

export interface AddCartItemRequest {
  productVariantId: string;
  quantity: number;
}

export interface CheckoutPreviewRequest {
  governorateId: string;
  couponCode?: string | null;
}

export interface CreateOrderRequest {
  governorateId: string;
  cityDistrict: string;
  detailedAddress: string;
  notes?: string | null;
  paymentMethod: PaymentMethod;
  couponCode?: string | null;
  idempotencyKey?: string | null;
}

/** Create-order response (backend returns orderNumber alongside orderId — see backend change #1). */
export interface CreateOrderResponse {
  orderId: string;
  orderNumber: string;
}

export interface AnalyticsEvent {
  sessionId: string;
  visitorId: string;
  eventType: AnalyticsEventType;
  pageUrl?: string | null;
  productId?: string | null;
  orderId?: string | null;
  metadataJson?: string | null;
}
