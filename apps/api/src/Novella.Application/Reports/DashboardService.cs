using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Orders;
using Novella.Domain.Enums;

namespace Novella.Application.Reports;

public sealed record DashboardSummaryDto(
    int TodayOrders, decimal TodayRevenue, int DeliveredOrdersThisMonth, int PendingOrders,
    int LowStockVariants, int FailedWhatsAppMessages, decimal ConversionRateThisMonth, decimal NetProfitThisMonth);

public sealed record DashboardAlertDto(string Type, string Message, int Count);

/// <summary>Admin dashboard KPIs, recent orders, and operational alerts.</summary>
public sealed class DashboardService
{
    private const int LowStockThreshold = 5;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly ReportService _reports;

    public DashboardService(IAppDbContext db, IClock clock, ReportService reports)
    {
        _db = db;
        _clock = clock;
        _reports = reports;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var todayWindow = _reports.Window(ReportRange.Today, null, null);
        var monthWindow = _reports.Window(ReportRange.ThisMonth, null, null);

        var todayOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.CreatedAt >= todayWindow.FromUtc && o.CreatedAt < todayWindow.ToUtc, ct);
        var todayRevenue = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= todayWindow.FromUtc && o.CreatedAt < todayWindow.ToUtc && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.GrandTotal, ct) ?? 0m;

        var deliveredThisMonth = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= monthWindow.FromUtc && o.DeliveredAt < monthWindow.ToUtc, ct);
        var pending = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var lowStock = await _db.ProductVariants.AsNoTracking().CountAsync(v => v.IsActive && v.StockQuantity <= LowStockThreshold, ct);
        var failedWa = await _db.WhatsAppMessageLogs.AsNoTracking().CountAsync(m => m.Status == WhatsAppMessageStatus.Failed, ct);

        var analytics = await _reports.AnalyticsAsync(monthWindow, ct);
        var profit = await _reports.ProfitAsync(monthWindow, ct);

        return new DashboardSummaryDto(todayOrders, todayRevenue, deliveredThisMonth, pending, lowStock, failedWa,
            analytics.VisitToOrderConversion, profit.NetProfit);
    }

    public async Task<IReadOnlyList<AdminOrderListItemDto>> GetRecentOrdersAsync(int take, CancellationToken ct)
        => await _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).Take(take <= 0 ? 10 : take)
            .Select(o => new AdminOrderListItemDto(o.Id, o.OrderNumber, o.Status, o.CustomerName, o.CustomerPhone,
                o.GrandTotal, o.PaymentMethod, o.PaymentStatus, o.CreatedAt))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DashboardAlertDto>> GetAlertsAsync(CancellationToken ct)
    {
        var alerts = new List<DashboardAlertDto>();

        var pending = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == OrderStatus.Pending, ct);
        if (pending > 0) alerts.Add(new DashboardAlertDto("PendingOrders", "Orders awaiting confirmation.", pending));

        var lowStock = await _db.ProductVariants.AsNoTracking().CountAsync(v => v.IsActive && v.StockQuantity <= LowStockThreshold, ct);
        if (lowStock > 0) alerts.Add(new DashboardAlertDto("LowStock", "Variants low on stock.", lowStock));

        var outOfStock = await _db.ProductVariants.AsNoTracking().CountAsync(v => v.IsActive && v.StockQuantity <= 0, ct);
        if (outOfStock > 0) alerts.Add(new DashboardAlertDto("OutOfStock", "Active variants out of stock.", outOfStock));

        var failedWa = await _db.WhatsAppMessageLogs.AsNoTracking().CountAsync(m => m.Status == WhatsAppMessageStatus.Failed, ct);
        if (failedWa > 0) alerts.Add(new DashboardAlertDto("FailedWhatsApp", "Failed WhatsApp messages.", failedWa));

        return alerts;
    }
}
