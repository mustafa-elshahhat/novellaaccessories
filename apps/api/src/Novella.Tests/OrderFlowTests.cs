using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Cart;
using Novella.Application.Checkout;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Application.Orders;
using Novella.Application.Shipping;
using Novella.Application.WhatsApp;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

/// <summary>End-to-end cart → checkout → order → stock/cancel flows over in-memory SQLite.</summary>
public class OrderFlowTests
{
    private sealed class Graph
    {
        public required TestDatabase Db { get; init; }
        public required FakeClock Clock { get; init; }
        public required CartService Cart { get; init; }
        public required CheckoutService Checkout { get; init; }
        public required OrderService Orders { get; init; }
        public required FakeWhatsAppClient WhatsApp { get; init; }
    }

    private static Graph Build()
    {
        var db = new TestDatabase();
        var clock = new FakeClock();
        var wa = new FakeWhatsAppClient();
        var messenger = new WhatsAppMessenger(db.Db, wa, clock);
        var twoOrder = new TwoOrderCouponService(db.Db, clock, messenger);
        var orders = new OrderService(db.Db, clock, twoOrder, messenger);
        var assembler = new PricingAssembler(db.Db);
        var coupons = new CouponService(db.Db, clock);
        var shipping = new ShippingService(db.Db, clock);
        var checkout = new CheckoutService(db.Db, clock, assembler, coupons, shipping);
        var cart = new CartService(db.Db, clock, assembler);
        TestSeed.EnableWhatsApp(db.Db, clock);
        return new Graph { Db = db, Clock = clock, Cart = cart, Checkout = checkout, Orders = orders, WhatsApp = wa };
    }

    private static async Task<(Guid orderId, Guid variantId, Guid customerId)> PlaceOrderAsync(Graph g, int qty = 2)
    {
        var customer = TestSeed.AddCustomer(g.Db.Db, g.Clock);
        var (_, variant) = TestSeed.AddProduct(g.Db.Db, g.Clock, sellingPrice: 1000m, purchasePrice: 600m, stock: 10);
        var gov = TestSeed.AddGovernorate(g.Db.Db, g.Clock, fee: 50m, cost: 35m);

        await g.Cart.AddItemAsync(customer.Id, new AddCartItemRequest(variant.Id, qty), default);
        var preview = await g.Checkout.PreviewAsync(customer.Id, new CheckoutPreviewRequest(gov.Id, null), default);
        var orderId = await g.Checkout.CreateOrderAsync(customer.Id,
            new CreateOrderRequest(gov.Id, "Nasr City", "12 Street", null, PaymentMethod.CashOnDelivery, null), default);

        var order = await g.Db.Db.Orders.FirstAsync(o => o.Id == orderId);
        // Preview total must equal the created order total.
        preview.GrandTotal.Should().Be(order.GrandTotal);
        return (orderId, variant.Id, customer.Id);
    }

    [Fact]
    public async Task Preview_total_equals_created_order_total_and_shipping_margin_snapshot()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, _, _) = await PlaceOrderAsync(g, qty: 2);

        var order = await g.Db.Db.Orders.FirstAsync(o => o.Id == orderId);
        order.ProductSubtotalAfterDiscount.Should().Be(2000m);
        order.CustomerPaidShippingFee.Should().Be(50m);
        order.ActualShippingCost.Should().Be(35m);
        order.ShippingMargin.Should().Be(15m);
        order.GrandTotal.Should().Be(2050m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.StockDeducted.Should().BeFalse();
    }

    [Fact]
    public async Task Pending_does_not_deduct_stock_but_Confirmed_deducts_exactly_once()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, variantId, _) = await PlaceOrderAsync(g, qty: 2);

        (await g.Db.Db.ProductVariants.FirstAsync(v => v.Id == variantId)).StockQuantity.Should().Be(10);

        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Confirmed, default);
        (await g.Db.Db.ProductVariants.FirstAsync(v => v.Id == variantId)).StockQuantity.Should().Be(8);

        var movements = await g.Db.Db.InventoryMovements.CountAsync(m => m.OrderId == orderId && m.MovementType == MovementType.Deduct);
        movements.Should().Be(1);
    }

    [Fact]
    public async Task Cancelling_confirmed_order_restores_stock_exactly_once()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, variantId, _) = await PlaceOrderAsync(g, qty: 2);

        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Confirmed, default);
        await g.Orders.CancelByAdminEndpointAsync(orderId, new CancelOrderRequest("changed mind"), default);

        (await g.Db.Db.ProductVariants.FirstAsync(v => v.Id == variantId)).StockQuantity.Should().Be(10);
        var restores = await g.Db.Db.InventoryMovements.CountAsync(m => m.OrderId == orderId && m.MovementType == MovementType.Restore);
        restores.Should().Be(1);
    }

    [Fact]
    public async Task Customer_cannot_cancel_after_preparing()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, _, customerId) = await PlaceOrderAsync(g, qty: 1);

        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Confirmed, default);
        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Preparing, default);

        var orderNumber = (await g.Db.Db.Orders.FirstAsync(o => o.Id == orderId)).OrderNumber;
        var act = () => g.Orders.CancelMyOrderAsync(customerId, orderNumber, new CancelOrderRequest(null), default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OrderCannotBeCancelled);
    }

    [Fact]
    public async Task Invalid_forward_transition_is_blocked()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, _, _) = await PlaceOrderAsync(g, qty: 1);

        // Pending -> Shipped is not allowed.
        var act = () => g.Orders.UpdateStatusAsync(orderId, OrderStatus.Shipped, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OrderInvalidTransition);
    }

    [Fact]
    public async Task Inactive_governorate_is_rejected_at_checkout()
    {
        var g = Build();
        using var _ = g.Db;
        var customer = TestSeed.AddCustomer(g.Db.Db, g.Clock);
        var (_, variant) = TestSeed.AddProduct(g.Db.Db, g.Clock);
        var gov = TestSeed.AddGovernorate(g.Db.Db, g.Clock, active: false);
        await g.Cart.AddItemAsync(customer.Id, new AddCartItemRequest(variant.Id, 1), default);

        var act = () => g.Checkout.PreviewAsync(customer.Id, new CheckoutPreviewRequest(gov.Id, null), default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.ShippingGovernorateInactive);
    }

    [Fact]
    public async Task Delivered_order_profit_matches_hand_computed_value()
    {
        var g = Build();
        using var _ = g.Db;
        var (orderId, _, _) = await PlaceOrderAsync(g, qty: 2);

        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Confirmed, default);
        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Preparing, default);
        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Shipped, default);
        await g.Orders.UpdateStatusAsync(orderId, OrderStatus.Delivered, default);

        var items = await g.Db.Db.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();
        // revenue 2*1000=2000, cost 2*600=1200, gross 800.
        items.Sum(i => i.LineRevenue).Should().Be(2000m);
        items.Sum(i => i.LineCost).Should().Be(1200m);
        items.Sum(i => i.LineGrossProfit).Should().Be(800m);
    }
}
