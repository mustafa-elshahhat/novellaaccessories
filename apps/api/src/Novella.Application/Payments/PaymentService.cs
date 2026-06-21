using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Payments;

/// <summary>Runtime payment configuration (webhook secret, active provider) supplied from config.</summary>
public sealed record PaymentRuntimeOptions(string? WebhookSecret, string? ActiveProvider);

public sealed record PaymentMethodDto(PaymentMethod Method, string ProviderName, bool IsActive);
public sealed record InitiatePaymentRequest(string OrderNumber, PaymentMethod Method);
public sealed record PaymentInitiationDto(PaymentMethod Method, PaymentStatus Status, string? ProviderName, string? RedirectUrl);
public sealed record PaymentStatusDto(Guid OrderId, string OrderNumber, PaymentMethod PaymentMethod, PaymentStatus Status, decimal Amount, string? ProviderReference);

/// <summary>
/// Payments readiness. COD is active in the MVP; bank card / Instapay / wallets are prepared but
/// inactive and return PAYMENT_PROVIDER_NOT_ACTIVE. Webhook callbacks validate the shared secret.
/// </summary>
public sealed class PaymentService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IPaymentProviderFactory _factory;
    private readonly PaymentRuntimeOptions _options;

    public PaymentService(IAppDbContext db, IClock clock, IPaymentProviderFactory factory, PaymentRuntimeOptions options)
    {
        _db = db;
        _clock = clock;
        _factory = factory;
        _options = options;
    }

    public IReadOnlyList<PaymentMethodDto> GetMethods()
        => _factory.All.Select(p => new PaymentMethodDto(p.Method, p.ProviderName, p.IsActive)).ToList();

    public async Task<PaymentInitiationDto> InitiateAsync(InitiatePaymentRequest req, Guid customerId, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == req.OrderNumber && o.CustomerId == customerId, ct)
            ?? throw AppException.NotFound("Order not found.");

        var provider = _factory.ForMethod(req.Method)
            ?? throw new AppException(ErrorCodes.PaymentProviderNotActive, $"Payment method '{req.Method}' is not supported.", 409);

        if (!provider.IsActive)
            throw new AppException(ErrorCodes.PaymentProviderNotActive, $"Payment method '{req.Method}' is not active yet.", 409);

        var result = await provider.InitiatePaymentAsync(order, ct);

        var txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.OrderId == order.Id && t.PaymentMethod == req.Method, ct);
        if (txn is null)
        {
            txn = new PaymentTransaction { Id = Guid.NewGuid(), OrderId = order.Id, PaymentMethod = req.Method, Amount = order.GrandTotal, CreatedAt = _clock.UtcNow };
            _db.PaymentTransactions.Add(txn);
        }
        txn.ProviderName = result.ProviderName;
        txn.Status = result.Status;
        txn.ProviderTransactionReference = result.ProviderReference;
        txn.ProviderResponse = result.RawResponse;
        txn.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new PaymentInitiationDto(req.Method, result.Status, result.ProviderName, result.RedirectUrl);
    }

    public async Task HandleCallbackAsync(string providerName, string rawPayload, IDictionary<string, string> headers, CancellationToken ct)
    {
        // Webhook secret validation (header or query token must match configured secret).
        if (!string.IsNullOrEmpty(_options.WebhookSecret))
        {
            var provided = headers.TryGetValue("x-webhook-secret", out var h) ? h
                : headers.TryGetValue("X-Webhook-Secret", out var h2) ? h2 : null;
            if (!string.Equals(provided, _options.WebhookSecret, StringComparison.Ordinal))
                throw AppException.Unauthorized("Invalid webhook signature.");
        }

        var provider = _factory.ForProvider(providerName)
            ?? throw new AppException(ErrorCodes.PaymentProviderNotActive, $"Unknown payment provider '{providerName}'.", 404);

        var result = await provider.HandleCallbackAsync(rawPayload, headers, ct);
        if (string.IsNullOrEmpty(result.ProviderReference)) return;

        var txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderTransactionReference == result.ProviderReference, ct);
        if (txn is null) return;

        txn.Status = result.Status;
        txn.ProviderResponse = result.RawResponse;
        txn.CommissionAmount = result.CommissionAmount ?? txn.CommissionAmount;
        txn.UpdatedAt = _clock.UtcNow;

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == txn.OrderId, ct);
        if (order is not null)
        {
            order.PaymentStatus = result.Status;
            order.UpdatedAt = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PaymentStatusDto>> GetByOrderAsync(string orderNumber, Guid customerId, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.CustomerId == customerId, ct)
            ?? throw AppException.NotFound("Order not found.");

        // Customer-safe: method/status/amount/reference only — no commission.
        return await _db.PaymentTransactions.AsNoTracking().Where(t => t.OrderId == order.Id)
            .Select(t => new PaymentStatusDto(t.OrderId, order.OrderNumber, t.PaymentMethod, t.Status, t.Amount, t.ProviderTransactionReference))
            .ToListAsync(ct);
    }
}
