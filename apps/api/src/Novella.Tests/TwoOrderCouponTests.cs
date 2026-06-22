using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Discounts;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

public class TwoOrderCouponTests
{
    private static void EnableSettings(TestDatabase db, FakeClock clock)
    {
        db.Db.TwoOrderCouponSettings.Add(new TwoOrderCouponSettings
        {
            Id = Guid.NewGuid(), IsEnabled = true, DiscountPercentage = 15m, ValidityDays = 30, SendWhatsAppMessage = false, UpdatedAt = clock.UtcNow
        });
        db.Db.SaveChanges();
    }

    private static Order AddOrder(TestDatabase db, FakeClock clock, Guid customerId, OrderStatus status)
    {
        return TestSeed.AddOrder(db.Db, clock, customerId, status);
    }

    private static TwoOrderCouponService NewService(TestDatabase db, FakeClock clock)
        => new(db.Db, clock, new WhatsAppMessenger(db.Db, new FakeWhatsAppClient(), clock));

    [Fact]
    public async Task Reward_generated_once_on_second_delivered_order()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        EnableSettings(db, clock);
        var customer = TestSeed.AddCustomer(db.Db, clock);

        AddOrder(db, clock, customer.Id, OrderStatus.Delivered);
        AddOrder(db, clock, customer.Id, OrderStatus.Delivered);

        var svc = NewService(db, clock);
        await svc.EvaluateOnDeliveredAsync(customer.Id, default);
        await svc.EvaluateOnDeliveredAsync(customer.Id, default); // idempotent

        var coupons = await db.Db.Coupons.Where(c => c.Source == CouponSource.TwoDeliveredOrders).ToListAsync();
        coupons.Should().HaveCount(1);
        coupons[0].IsCustomerSpecific.Should().BeTrue();
        coupons[0].CustomerId.Should().Be(customer.Id);
        coupons[0].PerCustomerUsageLimit.Should().Be(1);
        coupons[0].Value.Should().Be(15m);
    }

    [Fact]
    public async Task Reward_not_generated_with_only_one_delivered_order()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        EnableSettings(db, clock);
        var customer = TestSeed.AddCustomer(db.Db, clock);

        AddOrder(db, clock, customer.Id, OrderStatus.Delivered);
        AddOrder(db, clock, customer.Id, OrderStatus.Confirmed); // not delivered

        var svc = NewService(db, clock);
        await svc.EvaluateOnDeliveredAsync(customer.Id, default);

        (await db.Db.Coupons.CountAsync(c => c.Source == CouponSource.TwoDeliveredOrders)).Should().Be(0);
    }

    [Fact]
    public async Task Reward_disabled_settings_generate_nothing()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        // settings not enabled
        var customer = TestSeed.AddCustomer(db.Db, clock);
        AddOrder(db, clock, customer.Id, OrderStatus.Delivered);
        AddOrder(db, clock, customer.Id, OrderStatus.Delivered);

        var svc = NewService(db, clock);
        await svc.EvaluateOnDeliveredAsync(customer.Id, default);
        (await db.Db.Coupons.CountAsync()).Should().Be(0);
    }
}
