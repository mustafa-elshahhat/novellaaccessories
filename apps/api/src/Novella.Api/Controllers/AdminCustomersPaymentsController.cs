using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Customers;
using Novella.Application.Payments;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/customers")]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly CustomerAdminService _svc;
    public AdminCustomersController(CustomerAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> List([FromQuery] AdminCustomerQuery query, CancellationToken ct) => Ok(await _svc.ListAsync(query, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _svc.GetAsync(id, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/payments")]
public sealed class AdminPaymentsController : ControllerBase
{
    private readonly PaymentAdminService _svc;
    private readonly IConfiguration _configuration;

    public AdminPaymentsController(PaymentAdminService svc, IConfiguration configuration)
    {
        _svc = svc;
        _configuration = configuration;
    }

    [HttpGet("readiness")]
    public IActionResult Readiness()
    {
        var publicBaseUrl = _configuration["PublicApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        return Ok(_svc.GetReadiness(publicBaseUrl));
    }
}
