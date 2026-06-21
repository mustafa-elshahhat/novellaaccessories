using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Reports;

public enum ReportRange { Today, ThisWeek, ThisMonth, Custom }

/// <summary>Resolved UTC date window for a report request.</summary>
public sealed record DateWindow(DateTime FromUtc, DateTime ToUtc)
{
    /// <summary>
    /// Resolves a named range (or custom from/to) into a UTC window. "Today/week/month" are
    /// computed at the presentation edge; storage stays UTC to keep filters correct.
    /// </summary>
    public static DateWindow Resolve(ReportRange range, DateTime nowUtc, DateTime? from, DateTime? to)
    {
        var today = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        return range switch
        {
            ReportRange.Today => new DateWindow(today, today.AddDays(1)),
            ReportRange.ThisWeek => new DateWindow(today.AddDays(-(int)today.DayOfWeek), today.AddDays(7 - (int)today.DayOfWeek)),
            ReportRange.ThisMonth => new DateWindow(new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            _ => new DateWindow(from ?? today, to ?? today.AddDays(1))
        };
    }
}

public sealed record SalesReportDto(
    int TotalOrders, int PendingOrders, int ConfirmedOrders, int PreparingOrders, int ShippedOrders,
    int DeliveredOrders, int CancelledOrders, decimal DeliveredProductRevenue, decimal DeliveredGrandTotal,
    decimal ProductDiscountTotal, decimal CouponDiscountTotal);

public sealed record ProfitReportDto(
    decimal ProductRevenueAfterDiscounts, decimal ProductPurchaseCost, decimal GrossProfit,
    decimal ShippingCollected, decimal ActualShippingCost, decimal ShippingMargin,
    decimal Expenses, decimal PaymentCommissions, decimal NetProfit, string CommissionSource);

public sealed record ProductReportRowDto(Guid ProductId, string ProductNameEn, int QuantitySold, decimal Revenue, decimal GrossProfit);
public sealed record CategoryReportRowDto(Guid CategoryId, string CategoryNameEn, int QuantitySold, decimal Revenue, decimal GrossProfit);
public sealed record CouponReportRowDto(Guid CouponId, string Code, int TimesUsed, decimal TotalDiscount);
public sealed record PaymentReportRowDto(PaymentMethod Method, int Orders, decimal Revenue, decimal Commissions);
public sealed record GovernorateReportRowDto(string GovernorateNameEn, int Orders, decimal ShippingCollected, decimal ActualShippingCost, decimal ShippingMargin);
public sealed record ExpenseReportRowDto(ExpenseCategory Category, decimal Amount, int Count);
public sealed record AnalyticsBreakdownRowDto(string Label, int Count);
public sealed record AnalyticsReportDto(
    int Sessions, int UniqueVisitors, int CheckoutStartedSessions, int OrderPlacedSessions, int DeliveredVisitors,
    decimal VisitToOrderConversion, decimal CheckoutToOrderConversion, decimal OrderToDeliveredConversion,
    int ProductViews, int AddToCartEvents,
    IReadOnlyList<AnalyticsBreakdownRowDto> TrafficSources,
    IReadOnlyList<AnalyticsBreakdownRowDto> UtmMediums,
    IReadOnlyList<AnalyticsBreakdownRowDto> UtmCampaigns,
    IReadOnlyList<AnalyticsBreakdownRowDto> Referrers,
    IReadOnlyList<AnalyticsBreakdownRowDto> DeviceTypes,
    IReadOnlyList<AnalyticsBreakdownRowDto> Languages);

/// <summary>
/// Admin reports. Final profit prefers Delivered orders and uses order-item snapshots (never
/// current prices). NET PROFIT counts payment commissions from PaymentTransactions only (the
/// single authoritative source) to avoid double-counting with PaymentGatewayCommission expenses.
/// All cost/profit data is admin-only.
/// </summary>
public sealed class ReportService
{
    public const string CommissionSource = "PaymentTransactions.CommissionAmount";

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ReportService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public DateWindow Window(ReportRange range, DateTime? from, DateTime? to)
        => DateWindow.Resolve(range, _clock.UtcNow, from, to);

    public async Task<SalesReportDto> SalesAsync(DateWindow w, CancellationToken ct)
    {
        var orders = _db.Orders.AsNoTracking().Where(o => o.CreatedAt >= w.FromUtc && o.CreatedAt < w.ToUtc);
        var delivered = _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= w.FromUtc && o.DeliveredAt < w.ToUtc);

        return new SalesReportDto(
            await orders.CountAsync(ct),
            await orders.CountAsync(o => o.Status == OrderStatus.Pending, ct),
            await orders.CountAsync(o => o.Status == OrderStatus.Confirmed, ct),
            await orders.CountAsync(o => o.Status == OrderStatus.Preparing, ct),
            await orders.CountAsync(o => o.Status == OrderStatus.Shipped, ct),
            await delivered.CountAsync(ct),
            await orders.CountAsync(o => o.Status == OrderStatus.Cancelled, ct),
            await delivered.SumAsync(o => (decimal?)o.ProductSubtotalAfterDiscount, ct) ?? 0m,
            await delivered.SumAsync(o => (decimal?)o.GrandTotal, ct) ?? 0m,
            await delivered.SumAsync(o => (decimal?)o.ProductDiscountTotal, ct) ?? 0m,
            await delivered.SumAsync(o => (decimal?)o.CouponDiscountTotal, ct) ?? 0m);
    }

    public async Task<ProfitReportDto> ProfitAsync(DateWindow w, CancellationToken ct)
    {
        var delivered = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= w.FromUtc && o.DeliveredAt < w.ToUtc)
            .ToListAsync(ct);

        var revenue = delivered.SelectMany(o => o.Items).Sum(i => i.LineRevenue);
        var cost = delivered.SelectMany(o => o.Items).Sum(i => i.LineCost);
        var grossProfit = revenue - cost;

        var shippingCollected = delivered.Sum(o => o.CustomerPaidShippingFee);
        var actualShipping = delivered.Sum(o => o.ActualShippingCost);
        var shippingMargin = delivered.Sum(o => o.ShippingMargin);

        // Expenses in the window — but EXCLUDE PaymentGatewayCommission to avoid double counting
        // with the authoritative PaymentTransactions.CommissionAmount source.
        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= w.FromUtc && e.ExpenseDate < w.ToUtc && e.Category != ExpenseCategory.PaymentGatewayCommission)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var deliveredIds = delivered.Select(o => o.Id).ToList();
        var commissions = await _db.PaymentTransactions.AsNoTracking()
            .Where(t => deliveredIds.Contains(t.OrderId) && t.CommissionAmount != null)
            .SumAsync(t => (decimal?)t.CommissionAmount, ct) ?? 0m;

        var netProfit = grossProfit + shippingMargin - expenses - commissions;

        return new ProfitReportDto(revenue, cost, grossProfit, shippingCollected, actualShipping, shippingMargin,
            expenses, commissions, netProfit, CommissionSource);
    }

    public async Task<IReadOnlyList<ProductReportRowDto>> ProductsAsync(DateWindow w, CancellationToken ct)
    {
        var rows = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order!.Status == OrderStatus.Delivered && i.Order.DeliveredAt >= w.FromUtc && i.Order.DeliveredAt < w.ToUtc)
            .GroupBy(i => new { i.ProductId, i.ProductNameEn })
            .Select(g => new ProductReportRowDto(g.Key.ProductId, g.Key.ProductNameEn,
                g.Sum(x => x.Quantity), g.Sum(x => x.LineRevenue), g.Sum(x => x.LineGrossProfit)))
            .OrderByDescending(r => r.Revenue).ToListAsync(ct);
        return rows;
    }

    public async Task<IReadOnlyList<CategoryReportRowDto>> CategoriesAsync(DateWindow w, CancellationToken ct)
    {
        var query = from i in _db.OrderItems.AsNoTracking()
                    join p in _db.Products.AsNoTracking() on i.ProductId equals p.Id
                    join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
                    where i.Order!.Status == OrderStatus.Delivered && i.Order.DeliveredAt >= w.FromUtc && i.Order.DeliveredAt < w.ToUtc
                    group new { i, c } by new { c.Id, c.NameEn } into g
                    select new CategoryReportRowDto(g.Key.Id, g.Key.NameEn,
                        g.Sum(x => x.i.Quantity), g.Sum(x => x.i.LineRevenue), g.Sum(x => x.i.LineGrossProfit));
        return await query.OrderByDescending(r => r.Revenue).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CouponReportRowDto>> CouponsAsync(DateWindow w, CancellationToken ct)
        => await _db.CouponUsages.AsNoTracking()
            .Where(u => u.UsedAt >= w.FromUtc && u.UsedAt < w.ToUtc)
            .GroupBy(u => new { u.CouponId, u.Coupon!.Code })
            .Select(g => new CouponReportRowDto(g.Key.CouponId, g.Key.Code, g.Count(), g.Sum(x => x.DiscountAmount)))
            .OrderByDescending(r => r.TimesUsed).ToListAsync(ct);

    public async Task<IReadOnlyList<PaymentReportRowDto>> PaymentsAsync(DateWindow w, CancellationToken ct)
        => await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= w.FromUtc && o.CreatedAt < w.ToUtc)
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new PaymentReportRowDto(g.Key, g.Count(),
                g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.GrandTotal),
                g.SelectMany(o => _db.PaymentTransactions.Where(t => t.OrderId == o.Id))
                    .Sum(t => (decimal?)t.CommissionAmount) ?? 0m))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GovernorateReportRowDto>> GovernoratesAsync(DateWindow w, CancellationToken ct)
        => await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= w.FromUtc && o.DeliveredAt < w.ToUtc)
            .GroupBy(o => o.GovernorateNameEn)
            .Select(g => new GovernorateReportRowDto(g.Key, g.Count(),
                g.Sum(o => o.CustomerPaidShippingFee), g.Sum(o => o.ActualShippingCost), g.Sum(o => o.ShippingMargin)))
            .OrderByDescending(r => r.ShippingMargin).ToListAsync(ct);

    public async Task<IReadOnlyList<ExpenseReportRowDto>> ExpensesAsync(DateWindow w, CancellationToken ct)
    {
        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= w.FromUtc && e.ExpenseDate < w.ToUtc)
            .Select(e => new { e.Category, e.Amount })
            .ToListAsync(ct);

        return expenses.GroupBy(e => e.Category)
            .Select(g => new ExpenseReportRowDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .OrderByDescending(r => r.Amount).ToList();
    }

    public async Task<AnalyticsReportDto> AnalyticsAsync(DateWindow w, CancellationToken ct)
    {
        var sessions = _db.AnalyticsSessions.AsNoTracking().Where(s => s.StartedAt >= w.FromUtc && s.StartedAt < w.ToUtc);
        var totalSessions = await sessions.CountAsync(ct);
        var uniqueVisitors = await sessions.Select(s => s.VisitorId).Distinct().CountAsync(ct);

        var sessionIds = await sessions.Select(s => s.Id).ToListAsync(ct);
        var events = _db.AnalyticsEvents.AsNoTracking().Where(e => sessionIds.Contains(e.SessionId));
        var checkoutSessions = await events.Where(e => e.EventType == AnalyticsEventType.CheckoutStarted).Select(e => e.SessionId).Distinct().CountAsync(ct);
        var orderSessions = await sessions.CountAsync(s => s.ConvertedOrderId != null, ct);
        var productViews = await events.CountAsync(e => e.EventType == AnalyticsEventType.ProductView, ct);
        var addToCart = await events.CountAsync(e => e.EventType == AnalyticsEventType.AddToCart, ct);

        var deliveredVisitors = await (from s in sessions
                                       join o in _db.Orders.AsNoTracking() on s.ConvertedOrderId equals o.Id
                                       where o.Status == OrderStatus.Delivered
                                       select s.VisitorId).Distinct().CountAsync(ct);

        decimal Ratio(int num, int den) => den == 0 ? 0m : Math.Round((decimal)num / den, 4);

        async Task<IReadOnlyList<AnalyticsBreakdownRowDto>> BreakdownAsync(string field)
        {
            IQueryable<IGrouping<string, AnalyticsSession>> grouped = field switch
            {
                "medium" => sessions.Where(s => s.UtmMedium != null).GroupBy(s => s.UtmMedium!),
                "campaign" => sessions.Where(s => s.UtmCampaign != null).GroupBy(s => s.UtmCampaign!),
                "referrer" => sessions.Where(s => s.Referrer != null).GroupBy(s => s.Referrer!),
                "device" => sessions.Where(s => s.DeviceType != null).GroupBy(s => s.DeviceType!),
                "language" => sessions.Where(s => s.Language != null).GroupBy(s => s.Language!),
                _ => sessions.Where(s => s.UtmSource != null).GroupBy(s => s.UtmSource!)
            };
            return await grouped.Select(g => new AnalyticsBreakdownRowDto(g.Key, g.Count()))
                .OrderByDescending(r => r.Count).Take(20).ToListAsync(ct);
        }

        return new AnalyticsReportDto(totalSessions, uniqueVisitors, checkoutSessions, orderSessions, deliveredVisitors,
            Ratio(orderSessions, totalSessions), Ratio(orderSessions, checkoutSessions), Ratio(deliveredVisitors, uniqueVisitors),
            productViews, addToCart,
            await BreakdownAsync("source"), await BreakdownAsync("medium"), await BreakdownAsync("campaign"),
            await BreakdownAsync("referrer"), await BreakdownAsync("device"), await BreakdownAsync("language"));
    }
}
