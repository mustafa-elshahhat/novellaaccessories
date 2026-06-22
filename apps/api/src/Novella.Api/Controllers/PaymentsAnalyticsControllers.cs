using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Novella.Api.Auth;
using Novella.Application.Abstractions;
using Novella.Application.Analytics;
using Novella.Application.Payments;

namespace Novella.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentService _payments;
    private readonly ICurrentUser _user;

    public PaymentsController(PaymentService payments, ICurrentUser user)
    {
        _payments = payments;
        _user = user;
    }

    [HttpGet("methods")]
    [AllowAnonymous]
    public IActionResult Methods() => Ok(_payments.GetMethods());

    [HttpPost("initiate")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest req, CancellationToken ct)
        => Ok(await _payments.InitiateAsync(req, _user.RequireCustomerId(), ct));

    [HttpPost("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        await _payments.HandleCallbackAsync(provider, payload, headers, ct);
        return Ok(new { received = true });
    }

    [HttpGet("order/{orderNumber}")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> ByOrder(string orderNumber, CancellationToken ct)
        => Ok(await _payments.GetByOrderAsync(orderNumber, _user.RequireCustomerId(), ct));
}

[ApiController]
[Route("api/analytics")]
[EnableRateLimiting("analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;
    private readonly ICurrentUser _user;

    public AnalyticsController(AnalyticsService analytics, ICurrentUser user)
    {
        _analytics = analytics;
        _user = user;
    }

    // Anonymous visitors must be able to start sessions and emit events; the customer id is read
    // opportunistically when a token is present.
    [HttpPost("session/start")]
    [AllowAnonymous]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest req, CancellationToken ct)
        => Ok(await _analytics.StartSessionAsync(req, _user.CustomerId, ct));

    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<IActionResult> Events([FromBody] TrackEventsRequest req, CancellationToken ct)
    {
        await _analytics.TrackEventsAsync(req, _user.CustomerId, ct);
        return Ok(new { received = true });
    }

    // Identify links the current session to a signed-in customer, so it requires authentication.
    [HttpPost("session/identify")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Identify([FromBody] IdentifyRequest req, CancellationToken ct)
    {
        await _analytics.IdentifyAsync(req, _user.RequireCustomerId(), ct);
        return Ok(new { success = true });
    }
}
