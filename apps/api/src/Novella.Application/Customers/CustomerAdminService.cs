using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Application.Orders;
using Novella.Application.WhatsApp;
using Novella.Domain.Enums;

namespace Novella.Application.Customers;

public sealed record AdminCustomerListItemDto(
    Guid Id, string FullName, string PhoneNumber, bool IsPhoneVerified, bool IsActive,
    int TotalOrders, int DeliveredOrders, DateTime? LastVisitAt, DateTime? LastOrderAt, DateTime CreatedAt);

public sealed record AdminReminderLogDto(Guid Id, ReminderType ReminderType, ReminderStatus Status, DateTime? SentAt, DateTime CreatedAt);

public sealed record AdminCustomerDetailDto(
    Guid Id, string FullName, string PhoneNumber, bool IsPhoneVerified, bool IsActive,
    int TotalOrders, int DeliveredOrders, DateTime? LastVisitAt, DateTime? LastOrderAt, DateTime CreatedAt,
    IReadOnlyList<AdminOrderListItemDto> Orders,
    IReadOnlyList<AdminCouponDto> Coupons,
    IReadOnlyList<AdminReminderLogDto> ReminderLogs,
    IReadOnlyList<WhatsAppMessageLogDto> WhatsAppMessages,
    object AnalyticsSummary);

public sealed class AdminCustomerQuery : PageQuery
{
    public string? Search { get; set; }
}

/// <summary>Safe admin customer read model. Never exposes password hashes, OTP hashes, or tokens.</summary>
public sealed class CustomerAdminService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CustomerAdminService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AdminCustomerListItemDto>> ListAsync(AdminCustomerQuery query, CancellationToken ct)
    {
        var q = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(c => c.FullName.Contains(s) || c.PhoneNumber.Contains(s));
        }

        var total = await q.CountAsync(ct);
        var page = await q.OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(c => new AdminCustomerListItemDto(
                c.Id, c.FullName, c.PhoneNumber, c.IsPhoneVerified, c.IsActive,
                _db.Orders.Count(o => o.CustomerId == c.Id),
                _db.Orders.Count(o => o.CustomerId == c.Id && o.Status == OrderStatus.Delivered),
                c.LastVisitAt,
                _db.Orders.Where(o => o.CustomerId == c.Id).OrderByDescending(o => o.CreatedAt).Select(o => (DateTime?)o.CreatedAt).FirstOrDefault(),
                c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminCustomerListItemDto> { Items = page, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<AdminCustomerDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Customer not found.");

        var orders = await _db.Orders.AsNoTracking().Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.CreatedAt).Take(50)
            .Select(o => new AdminOrderListItemDto(o.Id, o.OrderNumber, o.Status, o.CustomerName, o.CustomerPhone, o.GrandTotal, o.PaymentMethod, o.PaymentStatus, o.CreatedAt))
            .ToListAsync(ct);

        var coupons = await _db.Coupons.AsNoTracking().Where(x => x.CustomerId == id || _db.CouponUsages.Any(u => u.CouponId == x.Id && u.CustomerId == id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminCouponDto(x.Id, x.Code, x.Type, x.Value, x.StartAt, x.EndAt, x.TotalUsageLimit, x.PerCustomerUsageLimit, x.MinimumOrderSubtotal, x.IsActive, x.IsCustomerSpecific, x.CustomerId, x.Source, x.Usages.Count))
            .ToListAsync(ct);

        var reminders = await _db.ReminderLogs.AsNoTracking().Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.CreatedAt).Take(50)
            .Select(r => new AdminReminderLogDto(r.Id, r.ReminderType, r.Status, r.SentAt, r.CreatedAt))
            .ToListAsync(ct);

        var messages = await _db.WhatsAppMessageLogs.AsNoTracking().Where(m => m.CustomerId == id)
            .OrderByDescending(m => m.CreatedAt).Take(50)
            .Select(m => new WhatsAppMessageLogDto(m.Id, m.CustomerId, m.PhoneNumber, m.MessageType, m.TemplateKey,
                m.MessageType == WhatsAppMessageType.Otp ? null : m.MessageBody,
                m.Status, m.FailureReason, m.RetryCount, m.SentAt, m.CreatedAt))
            .ToListAsync(ct);

        var sessions = await _db.AnalyticsSessions.AsNoTracking().CountAsync(s => s.CustomerId == id, ct);
        var lastSession = await _db.AnalyticsSessions.AsNoTracking().Where(s => s.CustomerId == id)
            .OrderByDescending(s => s.LastActivityAt).Select(s => (DateTime?)s.LastActivityAt).FirstOrDefaultAsync(ct);

        return new AdminCustomerDetailDto(c.Id, c.FullName, c.PhoneNumber, c.IsPhoneVerified, c.IsActive,
            orders.Count, orders.Count(o => o.Status == OrderStatus.Delivered), c.LastVisitAt, orders.FirstOrDefault()?.CreatedAt, c.CreatedAt,
            orders, coupons, reminders, messages, new { sessions, lastSession });
    }

    public async Task<AdminCustomerDetailDto> SetStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Customer not found.");
        customer.IsActive = isActive;
        customer.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }
}
