using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Catalog;
using Novella.Application.Common;
using Novella.Application.Content;
using Novella.Application.Seo;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/heroes")]
public sealed class AdminHeroesController : ControllerBase
{
    private readonly ContentService _content;
    public AdminHeroesController(ContentService content) => _content = content;

    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await _content.GetHeroesAsync(activeOnly: false, ct));
    [HttpPost] public async Task<IActionResult> Create([FromBody] HeroUpsertRequest req, CancellationToken ct) => Ok(await _content.CreateHeroAsync(req, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] HeroUpsertRequest req, CancellationToken ct) => Ok(await _content.UpdateHeroAsync(id, req, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await _content.DeleteHeroAsync(id, ct); return Ok(new { success = true }); }
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] StatusRequest req, CancellationToken ct) { await _content.SetHeroStatusAsync(id, req.IsActive, ct); return Ok(new { success = true }); }
    [HttpPatch("reorder")] public async Task<IActionResult> Reorder([FromBody] ReorderRequest req, CancellationToken ct) { await _content.ReorderHeroesAsync(req, ct); return Ok(new { success = true }); }
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/pages")]
public sealed class AdminPagesController : ControllerBase
{
    private readonly ContentService _content;
    public AdminPagesController(ContentService content) => _content = content;

    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await _content.GetPagesAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _content.GetPageByIdAsync(id, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] StaticPageUpdateRequest req, CancellationToken ct) => Ok(await _content.UpdatePageAsync(id, req, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/seo")]
public sealed class AdminSeoController : ControllerBase
{
    private readonly SeoService _seo;
    public AdminSeoController(SeoService seo) => _seo = seo;

    [HttpGet("content")] public async Task<IActionResult> Content(CancellationToken ct) => Ok(await _seo.GetAdminContentAsync(ct));
}
