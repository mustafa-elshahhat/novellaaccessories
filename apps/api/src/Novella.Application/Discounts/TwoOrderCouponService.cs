using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.Discounts;

/// <summary>
/// Generates the customer-specific, single-use reward coupon once a customer reaches their second
/// Delivered order. Idempotent: a customer receives at most one reward coupon (MVP). Sends a
/// WhatsApp message when enabled and logs the attempt.
/// </summary>
public sealed class TwoOrderCouponService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly WhatsAppMessenger _whatsApp;

    public TwoOrderCouponService(IAppDbContext db, IClock clock, WhatsAppMessenger whatsApp)
    {
        _db = db;
        _clock = clock;
        _whatsApp = whatsApp;
    }

    /// <summary>
    /// Call after an order has transitioned to Delivered. Generates the reward coupon when the
    /// customer's Delivered count reaches the threshold and no reward coupon exists yet.
    /// </summary>
    public async Task EvaluateOnDeliveredAsync(Guid customerId, CancellationToken ct)
    {
        var settings = await _db.TwoOrderCouponSettings.FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsEnabled) return;

        var deliveredCount = await _db.Orders.CountAsync(o => o.CustomerId == customerId && o.Status == OrderStatus.Delivered, ct);
        if (deliveredCount < RewardPolicy.DeliveredOrdersForReward) return;

        // Idempotency — one reward coupon per customer (MVP).
        var alreadyRewarded = await _db.Coupons.AnyAsync(c => c.IsCustomerSpecific && c.CustomerId == customerId
            && c.Source == CouponSource.TwoDeliveredOrders, ct);
        if (alreadyRewarded) return;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer is null) return;

        var now = _clock.UtcNow;
        var expiry = now.AddDays(settings.ValidityDays);
        var code = await GenerateUniqueCodeAsync(ct);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code,
            Type = CouponType.Percentage,
            Value = settings.DiscountPercentage,
            StartAt = now,
            EndAt = expiry,
            TotalUsageLimit = 1,
            PerCustomerUsageLimit = 1,
            MinimumOrderSubtotal = settings.MinimumOrderSubtotal,
            IsActive = true,
            IsCustomerSpecific = true,
            CustomerId = customerId,
            Source = CouponSource.TwoDeliveredOrders,
            CreatedAt = now
        };
        _db.Coupons.Add(coupon);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The database enforces one reward coupon per customer; concurrent deliveries may race here.
            return;
        }

        if (settings.SendWhatsAppMessage)
        {
            var waSettings = await _whatsApp.GetSettingsAsync(ct);
            var template = waSettings.TwoOrderCouponTemplate ?? DefaultTemplates.TwoOrderCoupon;
            var body = TemplateRenderer.Render(template, new Dictionary<string, string>
            {
                ["name"] = customer.FullName,
                ["coupon_code"] = code,
                ["discount"] = settings.DiscountPercentage.ToString("0.##"),
                ["expiry_date"] = expiry.ToString("yyyy-MM-dd")
            });
            await _whatsApp.SendAsync(WhatsAppMessageType.TwoOrderCoupon, "two_order_coupon", customer.PhoneNumber, customerId, body, ct);
        }
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = "RWD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await _db.Coupons.AnyAsync(c => c.Code == code, ct))
                return code;
        }
        return "RWD-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }
}
