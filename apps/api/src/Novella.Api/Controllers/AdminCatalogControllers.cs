using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Api.Auth;
using Novella.Application.Abstractions;
using Novella.Application.Catalog;
using Novella.Application.Common;

namespace Novella.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/categories")]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly CatalogAdminService _svc;
    public AdminCategoriesController(CatalogAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await _svc.GetCategoriesAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _svc.GetCategoryAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create([FromBody] CategoryUpsertRequest req, CancellationToken ct) => Ok(await _svc.CreateCategoryAsync(req, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] CategoryUpsertRequest req, CancellationToken ct) => Ok(await _svc.UpdateCategoryAsync(id, req, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await _svc.DeleteCategoryAsync(id, ct); return Ok(new { success = true }); }
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] StatusRequest req, CancellationToken ct) { await _svc.SetCategoryStatusAsync(id, req.IsActive, ct); return Ok(new { success = true }); }
    [HttpPatch("reorder")] public async Task<IActionResult> Reorder([FromBody] ReorderRequest req, CancellationToken ct) { await _svc.ReorderCategoriesAsync(req, ct); return Ok(new { success = true }); }
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly CatalogAdminService _svc;
    public AdminProductsController(CatalogAdminService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> List([FromQuery] PageQuery query, [FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] bool? isActive, [FromQuery] bool? isFeatured, CancellationToken ct) => Ok(await _svc.GetProductsAsync(query, search, categoryId, isActive, isFeatured, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await _svc.GetProductAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create([FromBody] ProductUpsertRequest req, CancellationToken ct) => Ok(await _svc.CreateProductAsync(req, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpsertRequest req, CancellationToken ct) => Ok(await _svc.UpdateProductAsync(id, req, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await _svc.DeleteProductAsync(id, ct); return Ok(new { success = true }); }
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, [FromBody] StatusRequest req, CancellationToken ct) { await _svc.SetProductStatusAsync(id, req.IsActive, ct); return Ok(new { success = true }); }

    [HttpPost("{id:guid}/images")] public async Task<IActionResult> AddImage(Guid id, [FromBody] AddImageRequest req, CancellationToken ct) => Ok(await _svc.AddProductImageAsync(id, req, ct));
    [HttpDelete("{id:guid}/images/{imageId:guid}")] public async Task<IActionResult> RemoveImage(Guid id, Guid imageId, CancellationToken ct) { var publicId = await _svc.RemoveProductImageAsync(id, imageId, ct); return Ok(new { success = true, publicId }); }
    [HttpPatch("{id:guid}/images/reorder")] public async Task<IActionResult> ReorderImages(Guid id, [FromBody] ReorderRequest req, CancellationToken ct) { await _svc.ReorderProductImagesAsync(id, req, ct); return Ok(new { success = true }); }

    // Variant routes nested under a product.
    [HttpGet("{productId:guid}/variants")] public async Task<IActionResult> Variants(Guid productId, CancellationToken ct) => Ok(await _svc.GetVariantsAsync(productId, ct));
    [HttpPost("{productId:guid}/variants")] public async Task<IActionResult> CreateVariant(Guid productId, [FromBody] VariantUpsertRequest req, CancellationToken ct) => Ok(await _svc.CreateVariantAsync(productId, req, ct));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/variants")]
public sealed class AdminVariantsController : ControllerBase
{
    private readonly CatalogAdminService _svc;
    private readonly ICurrentUser _user;
    public AdminVariantsController(CatalogAdminService svc, ICurrentUser user) { _svc = svc; _user = user; }

    [HttpPut("{variantId:guid}")] public async Task<IActionResult> Update(Guid variantId, [FromBody] VariantUpsertRequest req, CancellationToken ct) => Ok(await _svc.UpdateVariantAsync(variantId, req, ct));
    [HttpDelete("{variantId:guid}")] public async Task<IActionResult> Delete(Guid variantId, CancellationToken ct) { await _svc.DeleteVariantAsync(variantId, ct); return Ok(new { success = true }); }
    [HttpPatch("{variantId:guid}/stock")] public async Task<IActionResult> Stock(Guid variantId, [FromBody] StockAdjustRequest req, CancellationToken ct) => Ok(await _svc.AdjustStockAsync(variantId, req, _user.AdminId, ct));
    [HttpPatch("{variantId:guid}/status")] public async Task<IActionResult> Status(Guid variantId, [FromBody] StatusRequest req, CancellationToken ct) { await _svc.SetVariantStatusAsync(variantId, req.IsActive, ct); return Ok(new { success = true }); }
    [HttpGet("{variantId:guid}/movements")] public async Task<IActionResult> Movements(Guid variantId, CancellationToken ct) => Ok(await _svc.GetInventoryMovementsAsync(variantId, ct));
}
