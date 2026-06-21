using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Application.Catalog;
using Novella.Application.Content;
using Novella.Application.Seo;
using Novella.Application.Shipping;

namespace Novella.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicCatalogController : ControllerBase
{
    private readonly CatalogPublicService _catalog;
    public PublicCatalogController(CatalogPublicService catalog) => _catalog = catalog;

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) => Ok(await _catalog.GetCategoriesAsync(ct));

    [HttpGet("categories/{slug}")]
    public async Task<IActionResult> Category(string slug, CancellationToken ct) => Ok(await _catalog.GetCategoryBySlugAsync(slug, ct));

    [HttpGet("categories/{slug}/products")]
    public async Task<IActionResult> CategoryProducts(string slug, [FromQuery] ProductListQuery query, CancellationToken ct)
        => Ok(await _catalog.GetCategoryProductsAsync(slug, query, ct));

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] ProductListQuery query, CancellationToken ct)
        => Ok(await _catalog.GetProductsAsync(query, ct));

    [HttpGet("products/featured")]
    public async Task<IActionResult> Featured(CancellationToken ct) => Ok(await _catalog.GetFeaturedAsync(ct));

    [HttpGet("products/search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] ProductListQuery query, CancellationToken ct)
        => Ok(await _catalog.SearchAsync(q ?? string.Empty, query, ct));

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> Product(string slug, CancellationToken ct) => Ok(await _catalog.GetProductBySlugAsync(slug, ct));
}

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicContentController : ControllerBase
{
    private readonly ContentService _content;
    private readonly ShippingService _shipping;
    public PublicContentController(ContentService content, ShippingService shipping)
    {
        _content = content;
        _shipping = shipping;
    }

    [HttpGet("site-settings")]
    public async Task<IActionResult> SiteSettings(CancellationToken ct) => Ok(await _content.GetSiteSettingsAsync(ct));

    [HttpGet("home")]
    public async Task<IActionResult> Home(CancellationToken ct) => Ok(await _content.GetHomeAsync(ct));

    [HttpGet("hero")]
    public async Task<IActionResult> Hero(CancellationToken ct) => Ok(await _content.GetHeroesAsync(activeOnly: true, ct));

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> Page(string slug, CancellationToken ct) => Ok(await _content.GetPageBySlugAsync(slug, ct));

    [HttpGet("faq")]
    public async Task<IActionResult> Faq(CancellationToken ct) => Ok(await _content.GetFaqAsync(ct));

    // Public governorate list for checkout (actual shipping cost is never included).
    [HttpGet("shipping/governorates")]
    public async Task<IActionResult> Governorates(CancellationToken ct) => Ok(await _shipping.GetPublicAsync(ct));
}

[ApiController]
[AllowAnonymous]
[Route("api/public/seo")]
public sealed class PublicSeoController : ControllerBase
{
    private readonly SeoService _seo;
    public PublicSeoController(SeoService seo) => _seo = seo;

    [HttpGet("sitemap-data")]
    public async Task<IActionResult> Sitemap(CancellationToken ct) => Ok(await _seo.GetSitemapDataAsync(ct));

    [HttpGet("product/{slug}")]
    public async Task<IActionResult> Product(string slug, CancellationToken ct) => Ok(await _seo.GetProductSeoAsync(slug, ct));

    [HttpGet("category/{slug}")]
    public async Task<IActionResult> Category(string slug, CancellationToken ct) => Ok(await _seo.GetCategorySeoAsync(slug, ct));

    [HttpGet("page/{slug}")]
    public async Task<IActionResult> Page(string slug, CancellationToken ct) => Ok(await _seo.GetPageSeoAsync(slug, ct));
}
