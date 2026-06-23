using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Expenses;
using Novella.Application.Common;
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
    private readonly ReminderService _reminders;
    public AdminWhatsAppController(WhatsAppAdminService svc, ReminderService reminders)
    {
        _svc = svc;
        _reminders = reminders;
    }

    [HttpGet("settings")] public async Task<IActionResult> GetSettings(CancellationToken ct) => Ok(await _svc.GetSettingsAsync(ct));
    [HttpPut("settings")] public async Task<IActionResult> UpdateSettings([FromBody] WhatsAppSettingsUpdateRequest req, CancellationToken ct) => Ok(await _svc.UpdateSettingsAsync(req, ct));
    [HttpGet("status")] public async Task<IActionResult> Status(CancellationToken ct) => Ok(await _svc.GetStatusAsync(ct));
    [HttpGet("qr")] public async Task<IActionResult> Qr(CancellationToken ct) => Ok(await _svc.GetQrAsync(ct));
    [HttpGet("health")] public async Task<IActionResult> Health(CancellationToken ct) => Ok(await _svc.GetHealthAsync(ct));
    [HttpPost("logout")] public async Task<IActionResult> Logout(CancellationToken ct) => Ok(await _svc.LogoutAsync(ct));
    [HttpPost("reset-session")] public async Task<IActionResult> ResetSession(CancellationToken ct) => Ok(await _svc.LogoutAsync(ct));
    [HttpGet("messages")] public async Task<IActionResult> Messages([FromQuery] WhatsAppMessageQuery query, CancellationToken ct) => Ok(await _svc.GetMessagesAsync(query, ct));
    [HttpPost("messages/{id:guid}/retry")] public async Task<IActionResult> Retry(Guid id, CancellationToken ct) => Ok(await _svc.RetryAsync(id, ct));
    [HttpPost("test")] public async Task<IActionResult> Test([FromBody] WhatsAppTestRequest req, CancellationToken ct) => Ok(await _svc.SendTestAsync(req, ct));
    [HttpGet("automations")] public async Task<IActionResult> Automations(CancellationToken ct) => Ok(await _reminders.GetSettingsAsync(ct));
    [HttpPut("automations")] public async Task<IActionResult> UpdateAutomations([FromBody] ReminderSettingsDto req, CancellationToken ct) => Ok(await _reminders.UpdateSettingsAsync(req, ct));
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

    [HttpPost("image")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? entityType, [FromForm] string? entityId, CancellationToken ct)
    {
        if (file is null || file.Length <= 0)
            throw AppException.Validation("A valid image file is required.");
        if (file.Length > 15_000_000)
            throw AppException.Validation("Uploaded image is too large.");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw AppException.Validation("Uploaded file must be an image.");
        await using var stream = file.OpenReadStream();
        var result = await _svc.UploadAsync(stream, file.FileName, entityType, entityId, ct);
        return Ok(result);
    }
}
