using System.Reflection;
using FluentAssertions;
using Novella.Application.Cart;
using Novella.Application.Catalog;
using Novella.Application.Checkout;
using Novella.Application.Customers;
using Novella.Application.Orders;
using Novella.Application.Payments;
using Novella.Application.Shipping;
using Novella.Application.WhatsApp;
using Xunit;

namespace Novella.Tests;

/// <summary>
/// The highest-value invariant: customer-facing DTOs must never expose purchase cost, profit, the
/// actual shipping cost, or exact stock counts; and NO response DTO (customer or admin) may expose
/// raw secrets/hashes (password/OTP hashes, provider keys, signing keys, raw provider payloads).
/// This contract test fails if any such field appears.
/// </summary>
public class LeakageContractTests
{
    // Cost / profit / stock — forbidden on customer-facing DTOs only (admin DTOs legitimately carry cost).
    private static readonly string[] CostProfitStockTokens =
    {
        "purchaseprice", "purchasecost", "basepurchase", "purchasepriceoverride",
        "linecost", "grossprofit", "netprofit", "profit",
        "actualshippingcost", "shippingmargin",
        "stockquantity", "stockcount",
        "commission", "providerresponse"
    };

    // Secrets / hashes / raw provider payloads — forbidden on EVERY response DTO (customer and admin).
    private static readonly string[] SecretTokens =
    {
        "passwordhash", "codehash", "otphash",
        "internalapikey", "apisecret", "signingkey", "webhooksecret",
        "providerresponse"
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
        new object[] { typeof(PaymentStatusDto) },
        new object[] { typeof(PaymentMethodDto) },
        new object[] { typeof(PaymentInitiationDto) }
    };

    // Every response DTO, including admin surfaces, which must never carry raw secrets/hashes.
    public static IEnumerable<object[]> AllResponseDtos() =>
        CustomerFacingDtos().Concat(new[]
        {
            new object[] { typeof(AdminOrderDto) },
            new object[] { typeof(AdminOrderItemDto) },
            new object[] { typeof(AdminProductDto) },
            new object[] { typeof(AdminVariantDto) },
            new object[] { typeof(AdminCategoryDto) },
            new object[] { typeof(AdminCustomerListItemDto) },
            new object[] { typeof(AdminCustomerDetailDto) },
            new object[] { typeof(PaymentReadinessDto) },
            new object[] { typeof(WhatsAppSettingsDto) },
            new object[] { typeof(WhatsAppStatusDto) },
            new object[] { typeof(WhatsAppMessageLogDto) }
        });

    [Theory]
    [MemberData(nameof(CustomerFacingDtos))]
    public void Customer_dto_has_no_cost_profit_or_stock_fields(Type dtoType)
    {
        var offending = OffendingProperties(dtoType, CostProfitStockTokens);
        offending.Should().BeEmpty($"{dtoType.Name} must not expose cost/profit/stock fields, but exposes: {string.Join(", ", offending)}");
    }

    [Theory]
    [MemberData(nameof(AllResponseDtos))]
    public void Response_dto_never_exposes_secrets_or_hashes(Type dtoType)
    {
        var offending = OffendingProperties(dtoType, SecretTokens);
        offending.Should().BeEmpty($"{dtoType.Name} must not expose secrets/hashes, but exposes: {string.Join(", ", offending)}");
    }

    [Fact]
    public void Customer_order_dto_has_shipping_fee_but_not_actual_cost_or_margin()
    {
        var names = typeof(CustomerOrderDto).GetProperties().Select(p => p.Name).ToList();
        names.Should().Contain("ShippingFee");
        names.Should().NotContain("ActualShippingCost");
        names.Should().NotContain("ShippingMargin");
    }

    [Fact]
    public void WhatsApp_settings_dto_exposes_configured_flag_but_not_the_key()
    {
        var names = typeof(WhatsAppSettingsDto).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
        names.Should().Contain("serviceconfigured");
        names.Should().NotContain(n => n.Contains("apikey") || n.Contains("secret") || n.Contains("internalkey"));
    }

    private static List<string> OffendingProperties(Type dtoType, string[] tokens) =>
        dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(name => tokens.Any(token => name.ToLowerInvariant().Contains(token)))
            .ToList();
}
