using FluentAssertions;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

public class CouponServiceTests
{
    private static Coupon NewCoupon(FakeClock clock, Action<Coupon>? configure = null)
    {
        var c = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = "SAVE10",
            Type = CouponType.Percentage,
            Value = 10m,
            IsActive = true,
            Source = CouponSource.General,
            CreatedAt = clock.UtcNow
        };
        configure?.Invoke(c);
        return c;
    }

    [Fact]
    public async Task Valid_coupon_passes()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        db.Db.Coupons.Add(NewCoupon(clock));
        db.Db.SaveChanges();

        var svc = new CouponService(db.Db, clock);
        var result = await svc.ValidateAsync("SAVE10", Guid.NewGuid(), 500m, default);
        result.Coupon.Code.Should().Be("SAVE10");
        result.Input.Value.Should().Be(10m);
    }

    [Fact]
    public async Task Expired_coupon_throws_expired()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        db.Db.Coupons.Add(NewCoupon(clock, c => c.EndAt = clock.UtcNow.AddDays(-1)));
        db.Db.SaveChanges();

        var svc = new CouponService(db.Db, clock);
        var act = () => svc.ValidateAsync("SAVE10", Guid.NewGuid(), 500m, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.CouponExpired);
    }

    [Fact]
    public async Task Minimum_subtotal_not_met_throws()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        db.Db.Coupons.Add(NewCoupon(clock, c => c.MinimumOrderSubtotal = 1000m));
        db.Db.SaveChanges();

        var svc = new CouponService(db.Db, clock);
        var act = () => svc.ValidateAsync("SAVE10", Guid.NewGuid(), 500m, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.CouponMinSubtotal);
    }

    [Fact]
    public async Task Customer_specific_coupon_rejects_other_customer()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var owner = TestSeed.AddCustomer(db.Db, clock).Id;
        db.Db.Coupons.Add(NewCoupon(clock, c => { c.IsCustomerSpecific = true; c.CustomerId = owner; }));
        db.Db.SaveChanges();

        var svc = new CouponService(db.Db, clock);

        // Owner can use it.
        (await svc.ValidateAsync("SAVE10", owner, 500m, default)).Coupon.Should().NotBeNull();

        // Another customer cannot.
        var act = () => svc.ValidateAsync("SAVE10", Guid.NewGuid(), 500m, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.CouponInvalid);
    }

    [Fact]
    public async Task Per_customer_usage_limit_is_enforced()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var customer = Guid.NewGuid();
        var coupon = NewCoupon(clock, c => c.PerCustomerUsageLimit = 1);
        db.Db.Coupons.Add(coupon);
        db.Db.CouponUsages.Add(new CouponUsage { Id = Guid.NewGuid(), CouponId = coupon.Id, CustomerId = customer, OrderId = Guid.NewGuid(), DiscountAmount = 10m, UsedAt = clock.UtcNow });
        db.Db.SaveChanges();

        var svc = new CouponService(db.Db, clock);
        var act = () => svc.ValidateAsync("SAVE10", customer, 500m, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.CouponUsageLimitReached);
    }
}
