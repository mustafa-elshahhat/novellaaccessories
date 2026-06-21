using Microsoft.Extensions.Options;
using Novella.Application.WhatsApp;
using Novella.Infrastructure.Configuration;

namespace Novella.Infrastructure.WhatsApp;

/// <summary>Reports configured/not-configured status of the sidecar credentials (boolean only).</summary>
public sealed class WhatsAppConfigStatus : IWhatsAppConfigStatus
{
    public WhatsAppConfigStatus(IOptions<WhatsAppOptions> options) => IsConfigured = options.Value.IsConfigured;
    public bool IsConfigured { get; }
}
