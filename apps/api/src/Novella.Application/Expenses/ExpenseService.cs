using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Expenses;

public sealed record ExpenseDto(
    Guid Id, ExpenseCategory Category, decimal Amount, DateTime ExpenseDate, string? Notes,
    Guid? RelatedOrderId, string? RelatedCampaignName);

public sealed record ExpenseUpsertRequest(
    ExpenseCategory Category, decimal Amount, DateTime ExpenseDate, string? Notes,
    Guid? RelatedOrderId, string? RelatedCampaignName);

/// <summary>
/// Business expense CRUD feeding net-profit reporting. Shipping actual cost is NOT recorded here
/// (it lives on governorate/order snapshots) to avoid double-counting.
/// </summary>
public sealed class ExpenseService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ExpenseService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(DateTime? from, DateTime? to, ExpenseCategory? category, CancellationToken ct)
    {
        var q = _db.Expenses.AsNoTracking().AsQueryable();
        if (from is { } f) q = q.Where(e => e.ExpenseDate >= f);
        if (to is { } t) q = q.Where(e => e.ExpenseDate <= t);
        if (category is { } c) q = q.Where(e => e.Category == c);
        return await q.OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseDto(e.Id, e.Category, e.Amount, e.ExpenseDate, e.Notes, e.RelatedOrderId, e.RelatedCampaignName))
            .ToListAsync(ct);
    }

    public async Task<ExpenseDto> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Expense not found.");
        return Map(e);
    }

    public async Task<ExpenseDto> CreateAsync(ExpenseUpsertRequest req, CancellationToken ct)
    {
        var e = new Expense
        {
            Id = Guid.NewGuid(),
            Category = req.Category,
            Amount = req.Amount,
            ExpenseDate = req.ExpenseDate,
            Notes = req.Notes,
            RelatedOrderId = req.RelatedOrderId,
            RelatedCampaignName = req.RelatedCampaignName,
            CreatedAt = _clock.UtcNow
        };
        _db.Expenses.Add(e);
        await _db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<ExpenseDto> UpdateAsync(Guid id, ExpenseUpsertRequest req, CancellationToken ct)
    {
        var e = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Expense not found.");
        e.Category = req.Category; e.Amount = req.Amount; e.ExpenseDate = req.ExpenseDate;
        e.Notes = req.Notes; e.RelatedOrderId = req.RelatedOrderId; e.RelatedCampaignName = req.RelatedCampaignName;
        e.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Expense not found.");
        _db.Expenses.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    private static ExpenseDto Map(Expense e)
        => new(e.Id, e.Category, e.Amount, e.ExpenseDate, e.Notes, e.RelatedOrderId, e.RelatedCampaignName);
}
