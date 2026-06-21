namespace Novella.Application.Common;

/// <summary>Canonical error codes returned in the standard error model.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string Internal = "INTERNAL_ERROR";

    // Auth / OTP
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string OtpExpired = "OTP_EXPIRED";
    public const string OtpInvalid = "OTP_INVALID";
    public const string OtpLocked = "OTP_LOCKED";
    public const string OtpCooldown = "OTP_RESEND_COOLDOWN";
    public const string OtpResendLimit = "OTP_RESEND_LIMIT_REACHED";
    public const string PhoneAlreadyUsed = "PHONE_ALREADY_USED";
    public const string PhoneNotVerified = "PHONE_NOT_VERIFIED";

    // Catalog / cart
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";
    public const string VariantOutOfStock = "VARIANT_OUT_OF_STOCK";

    // Coupons
    public const string CouponInvalid = "COUPON_INVALID";
    public const string CouponExpired = "COUPON_EXPIRED";
    public const string CouponUsageLimitReached = "COUPON_USAGE_LIMIT_REACHED";
    public const string CouponMinSubtotal = "COUPON_MIN_SUBTOTAL_NOT_MET";

    // Orders
    public const string OrderCannotBeCancelled = "ORDER_CANNOT_BE_CANCELLED";
    public const string OrderInvalidTransition = "ORDER_INVALID_STATUS_TRANSITION";

    // Payments / shipping
    public const string PaymentProviderNotActive = "PAYMENT_PROVIDER_NOT_ACTIVE";
    public const string ShippingGovernorateInactive = "SHIPPING_GOVERNORATE_INACTIVE";

    // WhatsApp
    public const string WhatsAppDisabled = "WHATSAPP_DISABLED";
    public const string WhatsAppSendFailed = "WHATSAPP_SEND_FAILED";
    public const string WhatsAppRetryLimit = "WHATSAPP_RETRY_LIMIT_REACHED";

    // Uploads
    public const string UploadFailed = "UPLOAD_FAILED";
}
