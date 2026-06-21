using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Expenses;
using Novella.Application.Reminders;
using Novella.Application.Uploads;
using Novella.Application.WhatsApp;
using Novella.Domain.Enums;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/whatsapp")]
public sealed class AdminWhatsAppController : ControllerBase
{
    private readonly WhatsAppAdminService _svc;
    public AdminWhatsAppController(WhatsAppAdminService svc) => _svc = svc;

    [HttpGet("settings")] public async Task<IActionResult> GetSettings(CancellationToken ct) => Ok(await _svc.GetSettingsAsync(ct));
    [HttpPut("settings")] public async Task<IActionResult> UpdateSettings([FromBody] WhatsAppSettingsUpdateRequest req, CancellationToken ct) => Ok(await _svc.UpdateSettingsAsync(req, ct));
    [HttpGet("status")] public async Task<IActionResult> Status(CancellationToken ct) => Ok(await _svc.GetStatusAsync(ct));
    [HttpGet("messages")] public async Task<IActionResult> Messages([FromQuery] WhatsAppMessageQuery query, CancellationToken ct) => Ok(await _svc.GetMessagesAsync(query, ct));
    [HttpPost("messages/{id:guid}/retry")] public async Task<IActionResult> Retry(Guid id, CancellationToken ct) => Ok(await _svc.RetryAsync(id, ct));
    [HttpPost("test")] public async Task<IActionResult> Test([FromBody] WhatsAppTestRequest req, CancellationToken ct) => Ok(await _svc.SendTestAsync(req, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/expenses")]
public sealed class AdminExpensesController : ControllerBase
{
    private readonly ExpenseService _svc;
    public AdminExpensesController(ExpenseService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] ExpenseCategory? category, CancellationToken ct) => Ok(await _svc.ListAsync(from, to, category, ct));
    [HttpPost] public async Task<IActionResult> Create([FromBody] ExpenseUpsertRequest req, CancellationToken ct) => Ok(await _svc.CreateAsync(req, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _svc.GetAsync(id, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] ExpenseUpsertRequest req, CancellationToken ct) => Ok(await _svc.UpdateAsync(id, req, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await _svc.DeleteAsync(id, ct); return Ok(new { success = true }); }
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/uploads")]
public sealed class AdminUploadsController : ControllerBase
{
    private readonly UploadService _svc;
    public AdminUploadsController(UploadService svc) => _svc = svc;

    public sealed record DeleteImageRequest(string PublicId);

    [HttpPost("image")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? entityType, [FromForm] string? entityId, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await _svc.UploadAsync(stream, file.FileName, entityType, entityId, ct);
        return Ok(result);
    }

    [HttpDelete("image")]
    public async Task<IActionResult> Delete([FromBody] DeleteImageRequest req, CancellationToken ct)
    {
        await _svc.DeleteAsync(req.PublicId, ct);
        return Ok(new { success = true });
    }
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/reminders")]
public sealed class AdminRemindersController : ControllerBase
{
    private readonly ReminderService _svc;
    public AdminRemindersController(ReminderService svc) => _svc = svc;

    [HttpGet("settings")] public async Task<IActionResult> GetSettings(CancellationToken ct) => Ok(await _svc.GetSettingsAsync(ct));
    [HttpPut("settings")] public async Task<IActionResult> UpdateSettings([FromBody] ReminderSettingsDto req, CancellationToken ct) => Ok(await _svc.UpdateSettingsAsync(req, ct));
    [HttpPost("run")] public async Task<IActionResult> Run(CancellationToken ct) => Ok(await _svc.RunAsync(ct));
}
