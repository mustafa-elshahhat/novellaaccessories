namespace Novella.Domain.Enums;

/// <summary>Lifecycle status of an order. Persisted as string.</summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>Payment lifecycle status. Persisted as string.</summary>
public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Cancelled,
    Refunded
}

/// <summary>Supported payment methods. Only COD is active in the MVP.</summary>
public enum PaymentMethod
{
    CashOnDelivery,
    BankCard,
    Instapay,
    Wallet
}

/// <summary>Purpose scope for an OTP code.</summary>
public enum OtpPurpose
{
    Register,
    ResetPassword,
    ChangePhone
}

/// <summary>Status of a customer phone-change request.</summary>
public enum PhoneChangeStatus
{
    Pending,
    Verified,
    Cancelled,
    Expired
}

/// <summary>Coupon discount type.</summary>
public enum CouponType
{
    Percentage,
    FixedAmount
}

/// <summary>Origin of a coupon.</summary>
public enum CouponSource
{
    General,
    TwoDeliveredOrders
}

/// <summary>Inventory movement reason.</summary>
public enum MovementType
{
    Deduct,
    Restore,
    ManualAdjustment
}

/// <summary>Expense classification.</summary>
public enum ExpenseCategory
{
    Packaging,
    Ads,
    PaymentGatewayCommission,
    Operating,
    Other
}

/// <summary>Status of a WhatsApp business message log row.</summary>
public enum WhatsAppMessageStatus
{
    Pending,
    Sent,
    Failed
}

/// <summary>Business message types rendered by apps/api.</summary>
public enum WhatsAppMessageType
{
    Otp,
    OrderConfirmation,
    TwoOrderCoupon,
    AbandonedCheckout,
    InactiveCustomer,
    AdminTest
}

/// <summary>Reminder type.</summary>
public enum ReminderType
{
    AbandonedCheckout,
    InactiveCustomer
}

/// <summary>Reminder attempt outcome.</summary>
public enum ReminderStatus
{
    Sent,
    Failed,
    Skipped
}

/// <summary>First-party analytics event types.</summary>
public enum AnalyticsEventType
{
    PageView,
    ProductView,
    AddToCart,
    CheckoutStarted,
    OrderPlaced
}
