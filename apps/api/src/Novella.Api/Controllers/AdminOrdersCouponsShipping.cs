using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Catalog;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Application.Orders;
using Novella.Application.Shipping;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/orders")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly OrderService _orders;
    public AdminOrdersController(OrderService orders) => _orders = orders;

    [HttpGet] public async Task<IActionResult> List([FromQuery] AdminOrderListQuery query, CancellationToken ct) => Ok(await _orders.GetAdminOrdersAsync(query, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _orders.GetAdminOrderAsync(id, ct));
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] UpdateOrderStatusRequest req, CancellationToken ct) => Ok(await _orders.UpdateStatusAsync(id, req.Status, ct));
    [HttpPost("{id:guid}/cancel")] public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest req, CancellationToken ct) => Ok(await _orders.CancelByAdminEndpointAsync(id, req, ct));
    [HttpPatch("{id:guid}/shipping")] public async Task<IActionResult> Shipping(Guid id, [FromBody] UpdateShippingRequest req, CancellationToken ct) => Ok(await _orders.UpdateShippingAsync(id, req, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/coupons")]
public sealed class AdminCouponsController : ControllerBase
{
    private readonly CouponService _coupons;
    public AdminCouponsController(CouponService coupons) => _coupons = coupons;

    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await _coupons.ListAsync(ct));
    [HttpPost] public async Task<IActionResult> Create([FromBody] CouponUpsertRequest req, CancellationToken ct) => Ok(await _coupons.CreateAsync(req, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _coupons.GetAsync(id, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] CouponUpsertRequest req, CancellationToken ct) => Ok(await _coupons.UpdateAsync(id, req, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await _coupons.DeleteAsync(id, ct); return Ok(new { success = true }); }
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] StatusRequest req, CancellationToken ct) { await _coupons.SetStatusAsync(id, req.IsActive, ct); return Ok(new { success = true }); }
    [HttpGet("{id:guid}/usage")] public async Task<IActionResult> Usage(Guid id, CancellationToken ct) => Ok(await _coupons.GetUsageAsync(id, ct));

    [HttpGet("two-order/settings")] public async Task<IActionResult> GetTwoOrder(CancellationToken ct) => Ok(await _coupons.GetTwoOrderSettingsAsync(ct));
    [HttpPut("two-order/settings")] public async Task<IActionResult> UpdateTwoOrder([FromBody] TwoOrderSettingsDto req, CancellationToken ct) => Ok(await _coupons.UpdateTwoOrderSettingsAsync(req, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/shipping")]
public sealed class AdminShippingController : ControllerBase
{
    private readonly ShippingService _shipping;
    public AdminShippingController(ShippingService shipping) => _shipping = shipping;

    [HttpGet("governorates")] public async Task<IActionResult> List(CancellationToken ct) => Ok(await _shipping.GetAdminAsync(ct));
    [HttpPost("governorates")] public async Task<IActionResult> Create([FromBody] GovernorateUpsertRequest req, CancellationToken ct) => Ok(await _shipping.CreateAsync(req, ct));
    [HttpPut("governorates/{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] GovernorateUpsertRequest req, CancellationToken ct) => Ok(await _shipping.UpdateAsync(id, req, ct));
    [HttpPatch("governorates/{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] StatusRequest req, CancellationToken ct) { await _shipping.SetStatusAsync(id, req.IsActive, ct); return Ok(new { success = true }); }
    [HttpGet("settings")] public async Task<IActionResult> Settings(CancellationToken ct) => Ok(await _shipping.GetSettingsAsync(ct));
    [HttpPut("settings")] public async Task<IActionResult> UpdateSettings([FromBody] ShippingSettingsDto req, CancellationToken ct) => Ok(await _shipping.UpdateSettingsAsync(req, ct));
}
