using System.Reflection;
using FluentAssertions;
using Novella.Application.Cart;
using Novella.Application.Catalog;
using Novella.Application.Checkout;
using Novella.Application.Orders;
using Novella.Application.Payments;
using Novella.Application.Shipping;
using Xunit;

namespace Novella.Tests;

/// <summary>
/// The highest-value invariant: customer-facing DTOs must never expose purchase cost, profit, the
/// actual shipping cost, or exact stock counts. This contract test fails if any such field appears.
/// </summary>
public class LeakageContractTests
{
    private static readonly string[] ForbiddenTokens =
    {
        "purchaseprice", "purchasecost", "basepurchase", "purchasepriceoverride",
        "linecost", "grossprofit", "netprofit", "profit",
        "actualshippingcost", "shippingmargin",
        "stockquantity", "stockcount"
    };

    // Every DTO returned to customers / the storefront.
    public static IEnumerable<object[]> CustomerFacingDtos() => new[]
    {
        new object[] { typeof(PublicProductDto) },
        new object[] { typeof(PublicVariantDto) },
        new object[] { typeof(PublicProductListItemDto) },
        new object[] { typeof(PublicProductImageDto) },
        new object[] { typeof(PublicCategoryDto) },
        new object[] { typeof(CartDto) },
        new object[] { typeof(CartItemDto) },
        new object[] { typeof(CheckoutPreviewDto) },
        new object[] { typeof(CheckoutLineDto) },
        new object[] { typeof(CustomerOrderDto) },
        new object[] { typeof(CustomerOrderItemDto) },
        new object[] { typeof(PublicGovernorateDto) },
        new object[] { typeof(PaymentStatusDto) }
    };

    [Theory]
    [MemberData(nameof(CustomerFacingDtos))]
    public void Customer_dto_has_no_cost_profit_or_stock_fields(Type dtoType)
    {
        var offending = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(name => ForbiddenTokens.Any(token => name.ToLowerInvariant().Contains(token)))
            .ToList();

        offending.Should().BeEmpty($"{dtoType.Name} must not expose cost/profit/stock fields, but exposes: {string.Join(", ", offending)}");
    }

    [Fact]
    public void Customer_order_dto_has_shipping_fee_but_not_actual_cost_or_margin()
    {
        var names = typeof(CustomerOrderDto).GetProperties().Select(p => p.Name).ToList();
        names.Should().Contain("ShippingFee");
        names.Should().NotContain("ActualShippingCost");
        names.Should().NotContain("ShippingMargin");
    }
}
