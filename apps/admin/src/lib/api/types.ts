export type Id = string;

export type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };
export type Success = { success: boolean };

export type AdminProfile = { id: Id; username: string; displayName: string };
export type AdminLoginResponse = { token: string; admin: AdminProfile };

export type StatusRequest = { isActive: boolean };
export type ReorderRequest = { items: { id: Id; sortOrder: number }[] };
export type ImageDto = { id: Id; url: string; altAr?: string | null; altEn?: string | null; sortOrder: number; isPrimary: boolean };
export type UploadedImageDto = { url: string; publicId: string };

export type Category = {
  id: Id; nameAr: string; nameEn: string; slugAr: string; slugEn: string; imageUrl?: string | null; imagePublicId?: string | null;
  imageAltAr?: string | null; imageAltEn?: string | null;
  sortOrder: number; isActive: boolean; productCount: number;
  seoTitleAr?: string | null; seoTitleEn?: string | null; seoDescriptionAr?: string | null; seoDescriptionEn?: string | null;
  aeoSummaryAr?: string | null; aeoSummaryEn?: string | null; geoContentAr?: string | null; geoContentEn?: string | null;
};

export type Product = {
  id: Id; categoryId: Id; nameAr: string; nameEn: string; slugAr: string; slugEn: string; descriptionAr?: string | null; descriptionEn?: string | null;
  basePurchasePrice: number; baseSellingPrice: number; productDiscountPercentage?: number | null; productDiscountStartAt?: string | null; productDiscountEndAt?: string | null;
  isFeatured: boolean; isActive: boolean; variants: Variant[]; images: ImageDto[];
  seoTitleAr?: string | null; seoTitleEn?: string | null; seoDescriptionAr?: string | null; seoDescriptionEn?: string | null;
  aeoSummaryAr?: string | null; aeoSummaryEn?: string | null; geoContentAr?: string | null; geoContentEn?: string | null;
};

export type Variant = {
  id: Id; productId: Id; sku: string; nameAr?: string | null; nameEn?: string | null; size?: string | null;
  colorAr?: string | null; colorEn?: string | null; materialAr?: string | null; materialEn?: string | null;
  customOptionNameAr?: string | null; customOptionNameEn?: string | null; customOptionValueAr?: string | null; customOptionValueEn?: string | null;
  stockQuantity: number; purchasePriceOverride?: number | null; sellingPriceOverride?: number | null; isActive: boolean;
};

export type InventoryMovement = { id: Id; productVariantId: Id; orderId?: Id | null; movementType: string; quantity: number; reason?: string | null; createdAt: string; createdByAdminId?: Id | null };

export type OrderListItem = { id: Id; orderNumber: string; status: string; customerName: string; customerPhone: string; grandTotal: number; paymentMethod: string; paymentStatus: string; createdAt: string };
export type OrderItem = { productNameAr: string; productNameEn: string; sku: string; quantity: number; originalUnitSellingPrice: number; productDiscountAmountPerUnit: number; unitPriceAfterProductDiscount: number; couponDiscountAmountPerUnit: number; finalUnitPrice: number; purchaseCostPerUnit: number; lineRevenue: number; lineCost: number; lineGrossProfit: number };
export type Order = OrderListItem & { customerId: Id; governorateNameAr: string; governorateNameEn: string; cityDistrict: string; detailedAddress: string; notes?: string | null; productSubtotalBeforeDiscount: number; productDiscountTotal: number; couponDiscountTotal: number; productSubtotalAfterDiscount: number; customerPaidShippingFee: number; actualShippingCost: number; shippingMargin: number; couponCode?: string | null; shippingProviderName?: string | null; externalTrackingNumber?: string | null; externalShippingStatus?: string | null; confirmedAt?: string | null; preparingAt?: string | null; shippedAt?: string | null; deliveredAt?: string | null; cancelledAt?: string | null; cancellationReason?: string | null; items: OrderItem[] };

export type CustomerListItem = { id: Id; fullName: string; phoneNumber: string; isPhoneVerified: boolean; isActive: boolean; totalOrders: number; deliveredOrders: number; lastVisitAt?: string | null; lastOrderAt?: string | null; createdAt: string };
export type CustomerDetail = CustomerListItem & { orders: OrderListItem[]; coupons: Coupon[]; reminderLogs: ReminderLog[]; whatsAppMessages: WhatsAppMessage[]; analyticsSummary?: Record<string, unknown> | null };
export type ReminderLog = { id: Id; reminderType: string; status: string; sentAt?: string | null; createdAt: string };

export type Coupon = { id: Id; code: string; type: string; value: number; startAt?: string | null; endAt?: string | null; totalUsageLimit?: number | null; perCustomerUsageLimit?: number | null; minimumOrderSubtotal?: number | null; isActive: boolean; isCustomerSpecific: boolean; customerId?: Id | null; source: string; timesUsed: number };
export type CouponUsage = { id: Id; couponId: Id; customerId: Id; orderId: Id; discountAmount: number; usedAt: string };
export type TwoOrderSettings = { isEnabled: boolean; discountPercentage: number; validityDays: number; minimumOrderSubtotal?: number | null; sendWhatsAppMessage: boolean };

export type Governorate = { id: Id; nameAr: string; nameEn: string; customerPaidShippingFee: number; actualShippingCost: number; isActive: boolean; sortOrder: number };
export type Hero = { id: Id; imageUrl: string; imagePublicId: string; titleAr: string; titleEn: string; subtitleAr?: string | null; subtitleEn?: string | null; ctaTextAr?: string | null; ctaTextEn?: string | null; ctaLink?: string | null; linkedProductId?: Id | null; isActive: boolean; sortOrder: number };

export type WhatsAppSettings = { isEnabled: boolean; transportName: string; serviceBaseUrl?: string | null; otpTemplate?: string | null; orderConfirmationTemplate?: string | null; twoOrderCouponTemplate?: string | null; abandonedCheckoutTemplate?: string | null; inactiveCustomerTemplate?: string | null; serviceConfigured: boolean };
export type WhatsAppStatus = { reachable: boolean; connected: boolean; keyConfigured: boolean; detail?: string | null };
export type WhatsAppMessage = { id: Id; customerId?: Id | null; phoneNumber: string; messageType: string; templateKey?: string | null; messageBody?: string | null; status: string; failureReason?: string | null; retryCount: number; sentAt?: string | null; createdAt: string };

export type PaymentMethodReadiness = { method: string; providerName: string; isActive: boolean; environment: string; publicKeyConfigured: boolean; secretKeyConfigured: boolean; webhookUrl?: string | null; readinessStatus: string };
export type Expense = { id: Id; category: string; amount: number; expenseDate: string; notes?: string | null; relatedOrderId?: Id | null; relatedCampaignName?: string | null };

export type SalesReport = { totalOrders: number; pendingOrders: number; confirmedOrders: number; preparingOrders: number; shippedOrders: number; deliveredOrders: number; cancelledOrders: number; deliveredProductRevenue: number; deliveredGrandTotal: number; productDiscountTotal: number; couponDiscountTotal: number };
export type ProfitReport = { productRevenueAfterDiscounts: number; productPurchaseCost: number; grossProfit: number; shippingCollected: number; actualShippingCost: number; shippingMargin: number; expenses: number; paymentCommissions: number; netProfit: number; commissionSource: string };
export type RowReport = Record<string, string | number | null>;
export type AnalyticsReport = { sessions: number; uniqueVisitors: number; checkoutStartedSessions: number; orderPlacedSessions: number; deliveredVisitors: number; visitToOrderConversion: number; checkoutToOrderConversion: number; orderToDeliveredConversion: number; productViews?: number; addToCartEvents?: number; trafficSources?: RowReport[]; deviceTypes?: RowReport[]; languages?: RowReport[] };

export type StaticPage = { id: Id; key: string; titleAr: string; titleEn: string; slugAr: string; slugEn: string; contentAr: string; contentEn: string; seoTitleAr?: string | null; seoTitleEn?: string | null; seoDescriptionAr?: string | null; seoDescriptionEn?: string | null; aeoSummaryAr?: string | null; aeoSummaryEn?: string | null; geoContentAr?: string | null; geoContentEn?: string | null; isActive: boolean };
export type SeoMetadata = { entityType: string; entityId: Id; slugAr: string; slugEn: string; seoTitleAr?: string | null; seoTitleEn?: string | null; seoDescriptionAr?: string | null; seoDescriptionEn?: string | null; aeoSummaryAr?: string | null; aeoSummaryEn?: string | null; geoContentAr?: string | null; geoContentEn?: string | null };
export type SiteSettings = { siteNameAr: string; siteNameEn: string; domain: string; defaultSeoTitleAr?: string | null; defaultSeoTitleEn?: string | null; defaultSeoDescriptionAr?: string | null; defaultSeoDescriptionEn?: string | null; freeShippingThreshold?: number | null; isFreeShippingEnabled: boolean };
export type ReminderSettings = { abandonedCheckoutEnabled: boolean; abandonedCheckoutDelayHours: number; inactiveCustomerEnabled: boolean; inactiveCustomerDelayDays: number };
export type DashboardSummary = { todayOrders: number; todayRevenue: number; deliveredOrdersThisMonth: number; pendingOrders: number; lowStockVariants: number; failedWhatsAppMessages: number; conversionRateThisMonth: number; netProfitThisMonth: number };
export type DashboardAlert = { type: string; message: string; count: number };
