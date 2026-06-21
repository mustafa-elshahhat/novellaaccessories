using Novella.Application.Abstractions;
using Novella.Domain.Entities;

namespace Novella.Infrastructure.Shipping;

/// <summary>
/// MVP shipping: manual tracking only. A real carrier integration would implement
/// <see cref="IShippingProvider"/> behind this same abstraction in the future.
/// </summary>
public sealed class ManualShippingProvider : IShippingProvider
{
    public string ProviderName => "Manual";
    public bool IsActive => false;

    public Task<ShipmentResult> CreateShipmentAsync(Order order, CancellationToken ct = default)
        => Task.FromResult(new ShipmentResult(order.ExternalTrackingNumber, order.ExternalShippingStatus, order.ShippingProviderName ?? ProviderName));

    public Task<ShipmentResult> GetShipmentStatusAsync(string trackingNumber, CancellationToken ct = default)
        => Task.FromResult(new ShipmentResult(trackingNumber, null, ProviderName));

    public Task<bool> CancelShipmentAsync(string trackingNumber, CancellationToken ct = default)
        => Task.FromResult(true);
}
