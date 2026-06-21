using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Infrastructure.Payments;

/// <summary>Cash on Delivery — the only active payment method in the MVP.</summary>
public sealed class CashOnDeliveryPaymentProvider : IPaymentProvider
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;
    public string ProviderName => "CashOnDelivery";
    public bool IsActive => true;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, CancellationToken ct = default)
        => Task.FromResult(new PaymentInitiationResult(PaymentStatus.Pending, ProviderName, null, null, null));

    public Task<PaymentCallbackResult> HandleCallbackAsync(string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
        => Task.FromResult(new PaymentCallbackResult(null, PaymentStatus.Pending, null, rawPayload));

    public Task<PaymentStatus> GetPaymentStatusAsync(string providerReference, CancellationToken ct = default)
        => Task.FromResult(PaymentStatus.Pending);
}

/// <summary>
/// Base for prepared-but-inactive gateways (Bank card, Instapay, Wallets). Schema and method
/// listing are ready; activation requires a configured provider. Throws PAYMENT_PROVIDER_NOT_ACTIVE.
/// </summary>
public abstract class InactiveGatewayProvider : IPaymentProvider
{
    public abstract PaymentMethod Method { get; }
    public abstract string ProviderName { get; }
    public virtual bool IsActive => false;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, CancellationToken ct = default)
        => throw new AppException(ErrorCodes.PaymentProviderNotActive,
            $"Payment method '{Method}' is not active yet.", 409);

    public Task<PaymentCallbackResult> HandleCallbackAsync(string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
        => throw new AppException(ErrorCodes.PaymentProviderNotActive,
            $"Payment method '{Method}' is not active yet.", 409);

    public Task<PaymentStatus> GetPaymentStatusAsync(string providerReference, CancellationToken ct = default)
        => throw new AppException(ErrorCodes.PaymentProviderNotActive,
            $"Payment method '{Method}' is not active yet.", 409);
}

public sealed class BankCardPaymentProvider : InactiveGatewayProvider
{
    public override PaymentMethod Method => PaymentMethod.BankCard;
    public override string ProviderName => "BankCard";
}

public sealed class InstapayPaymentProvider : InactiveGatewayProvider
{
    public override PaymentMethod Method => PaymentMethod.Instapay;
    public override string ProviderName => "Instapay";
}

public sealed class WalletPaymentProvider : InactiveGatewayProvider
{
    public override PaymentMethod Method => PaymentMethod.Wallet;
    public override string ProviderName => "Wallet";
}

/// <summary>Resolves a payment provider by method or provider name.</summary>
public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    public IReadOnlyList<IPaymentProvider> All { get; }

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers) => All = providers.ToList();

    public IPaymentProvider? ForMethod(PaymentMethod method) => All.FirstOrDefault(p => p.Method == method);

    public IPaymentProvider? ForProvider(string providerName)
        => All.FirstOrDefault(p => string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
}
