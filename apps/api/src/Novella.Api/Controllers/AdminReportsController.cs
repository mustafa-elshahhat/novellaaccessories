using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Reports;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/reports")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly ReportService _reports;
    public AdminReportsController(ReportService reports) => _reports = reports;

    private DateWindow Win(ReportRange range, DateTime? from, DateTime? to) => _reports.Window(range, from, to);

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.SalesAsync(Win(range, from, to), ct));

    [HttpGet("profit")]
    public async Task<IActionResult> Profit([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.ProfitAsync(Win(range, from, to), ct));

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.ProductsAsync(Win(range, from, to), ct));

    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.CategoriesAsync(Win(range, from, to), ct));

    [HttpGet("coupons")]
    public async Task<IActionResult> Coupons([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.CouponsAsync(Win(range, from, to), ct));

    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.PaymentsAsync(Win(range, from, to), ct));

    [HttpGet("governorates")]
    public async Task<IActionResult> Governorates([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.GovernoratesAsync(Win(range, from, to), ct));

    [HttpGet("expenses")]
    public async Task<IActionResult> Expenses([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.ExpensesAsync(Win(range, from, to), ct));

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] ReportRange range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _reports.AnalyticsAsync(Win(range, from, to), ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;
    public AdminDashboardController(DashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct) => Ok(await _dashboard.GetSummaryAsync(ct));
    [HttpGet("recent-orders")] public async Task<IActionResult> RecentOrders([FromQuery] int take = 10, CancellationToken ct = default) => Ok(await _dashboard.GetRecentOrdersAsync(take, ct));
    [HttpGet("alerts")] public async Task<IActionResult> Alerts(CancellationToken ct) => Ok(await _dashboard.GetAlertsAsync(ct));
}
